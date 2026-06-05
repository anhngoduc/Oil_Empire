// Assets/_Project/Scripts/Production/BucketSystem.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// BucketSystem - Service quản lý logic đổ dầu vào bucket và thu dầu.
    /// Implement IBucketService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Nhận dầu từ ProductionManager và phân phối vào các bucket.
    /// - Ưu tiên đổ đầy từng bucket (bucket chưa đầy được đổ trước).
    /// - Phát hiện khi tất cả bucket đầy -> gọi ProductionManager.PauseProduction().
    /// - Xử lý thu dầu từ bucket khi người chơi click.
    /// - Phát sự kiện OnBucketFilled, OnBucketEmptied, OnBucketUpdated, OnOilCollected.
    /// 
    /// Thuật toán phân phối dầu:
    /// 1. Lấy danh sách tất cả Bucket của Player.
    /// 2. Sắp xếp theo thứ tự ưu tiên (theo uniqueID hoặc grid position).
    /// 3. Duyệt từng bucket, đổ dầu cho đến khi bucket đầy hoặc hết dầu.
    /// 4. Nếu tất cả bucket đầy và vẫn còn dầu -> tạm dừng sản xuất.
    /// </summary>
    public class BucketSystem : MonoBehaviour, IBucketService
    {
        [Header("Cấu hình")]
        [Tooltip("Thứ tự ưu tiên đổ dầu: OldestFirst (bucket cũ nhất trước) hoặc LowestID (ID nhỏ nhất trước).")]
        [SerializeField] private BucketFillPriority fillPriority = BucketFillPriority.OldestFirst;

        // Tham chiếu đến các service
        private IBuildingService buildingService;
        private IPlayerDataService playerDataService;
        private IProductionService productionService;

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IBucketService>(this);
        }

        private void Start()
        {
            // Lấy các service
            buildingService = ServiceLocator.Get<IBuildingService>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            productionService = ServiceLocator.Get<IProductionService>();

            if (buildingService == null)
                Debug.LogError("[BucketSystem] IBuildingService chưa được đăng ký!");
            if (playerDataService == null)
                Debug.LogError("[BucketSystem] IPlayerDataService chưa được đăng ký!");
            if (productionService == null)
                Debug.LogError("[BucketSystem] IProductionService chưa được đăng ký!");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IBucketService>();
        }

        #endregion

        #region IBucketService - Đổ dầu

        /// <summary>
        /// Đổ dầu vào các bucket.
        /// Được gọi bởi ProductionManager mỗi tick sản xuất.
        /// </summary>
        /// <param name="amount">Tổng lượng dầu cần phân phối.</param>
        public void FillOil(float amount)
        {
            if (amount <= 0f) return;
            if (buildingService == null) return;

            // Lấy tất cả Bucket của Player
            List<Building> buckets = buildingService.GetBuildingsOfType(BuildingType.Bucket);

            if (buckets.Count == 0)
            {
                // Không có bucket nào, không thể chứa dầu
                // Dầu sản xuất ra sẽ bị mất (có thể thông báo cho người chơi)
                Debug.LogWarning("[BucketSystem] Không có Bucket nào! Dầu sản xuất bị mất.");
                return;
            }

            // Sắp xếp bucket theo thứ tự ưu tiên
            SortBuckets(buckets);

            // Lượng dầu còn lại cần phân phối
            float remainingOil = amount;
            bool allFull = true;

            // Duyệt từng bucket để đổ dầu
            foreach (Building bucket in buckets)
            {
                if (bucket.BuildingData == null) continue;
                if (bucket.BuildingData.buildingType != BuildingType.Bucket) continue;

                // Lấy thông tin bucket
                float currentOil = bucket.GetCurrentOil();
                float capacity = bucket.GetCapacity();

                // Nếu bucket đã đầy, bỏ qua
                if (currentOil >= capacity)
                {
                    continue;
                }

                allFull = false;

                // Tính lượng dầu có thể đổ vào bucket này
                float spaceAvailable = capacity - currentOil;
                float oilToFill = Mathf.Min(spaceAvailable, remainingOil);

                // Đổ dầu vào bucket
                float newOil = currentOil + oilToFill;
                bucket.SetCurrentOil(newOil);

                // Cập nhật PlayerData
                if (playerDataService != null)
                {
                    playerDataService.UpdateBucketOil(bucket.UniqueID, newOil);
                }

                // Giảm lượng dầu còn lại
                remainingOil -= oilToFill;

                // Phát sự kiện cập nhật bucket
                BucketState newState = bucket.GetBucketState();
                EventBus.Publish(new OnBucketUpdated(bucket.UniqueID, newOil, capacity, newState));

                // Kiểm tra bucket vừa được đổ đầy
                if (newOil >= capacity)
                {
                    EventBus.Publish(new OnBucketFilled(bucket.UniqueID, capacity));
                    Debug.Log($"[BucketSystem] Bucket ID={bucket.UniqueID} ĐÃ ĐẦY ({capacity} Oil).");
                }

                // Nếu đã phân phối hết dầu, thoát vòng lặp
                if (remainingOil <= 0f)
                {
                    break;
                }
            }

            // Nếu tất cả bucket đều đầy sau khi đổ, tạm dừng sản xuất
            if (allFull && remainingOil > 0f && productionService != null)
            {
                productionService.PauseProduction();
                Debug.Log("[BucketSystem] Tất cả bucket đã đầy! Dầu dư bị mất: " + remainingOil);
            }
        }

        #endregion

        #region IBucketService - Thu dầu

        /// <summary>
        /// Thu toàn bộ dầu từ một bucket.
        /// Được gọi khi người chơi click vào bucket.
        /// </summary>
        /// <param name="bucketUniqueID">ID duy nhất của bucket.</param>
        /// <returns>Lượng dầu thu được (0 nếu thất bại).</returns>
        public float CollectOil(int bucketUniqueID)
        {
            if (buildingService == null || playerDataService == null)
            {
                Debug.LogError("[BucketSystem] Service chưa sẵn sàng!");
                return 0f;
            }

            // Tìm bucket
            Building bucket = buildingService.GetBuildingByID(bucketUniqueID);
            if (bucket == null)
            {
                Debug.LogWarning($"[BucketSystem] Không tìm thấy Bucket ID={bucketUniqueID}!");
                return 0f;
            }

            // Kiểm tra có phải bucket không
            if (bucket.Type != BuildingType.Bucket)
            {
                Debug.LogWarning($"[BucketSystem] Building ID={bucketUniqueID} không phải Bucket!");
                return 0f;
            }

            // Lấy lượng dầu hiện tại
            float collectedAmount = bucket.GetCurrentOil();

            if (collectedAmount <= 0f)
            {
                Debug.Log($"[BucketSystem] Bucket ID={bucketUniqueID} rỗng, không có dầu để thu.");
                return 0f;
            }

            // Reset bucket về 0
            bucket.SetCurrentOil(0f);

            // Cập nhật PlayerData
            playerDataService.UpdateBucketOil(bucketUniqueID, 0f);

            // Thêm dầu vào kho người chơi
            playerDataService.AddOil(collectedAmount, OilChangeReason.Collect);

            // Phát sự kiện
            EventBus.Publish(new OnBucketEmptied(bucketUniqueID, collectedAmount));
            EventBus.Publish(new OnBucketUpdated(bucketUniqueID, 0f, bucket.GetCapacity(), BucketState.Empty));
            EventBus.Publish(new OnOilCollected(collectedAmount, (float)playerDataService.OilHeld));

            Debug.Log($"[BucketSystem] Thu {collectedAmount} Oil từ Bucket ID={bucketUniqueID}. Tổng dầu Player: {playerDataService.OilHeld}.");

            return collectedAmount;
        }

        #endregion

        #region Truy vấn trạng thái

        /// <summary>
        /// Lấy trạng thái của một bucket.
        /// </summary>
        /// <param name="bucketUniqueID">ID của bucket.</param>
        /// <returns>BucketState (Empty/Partial/Full).</returns>
        public BucketState GetBucketState(int bucketUniqueID)
        {
            if (buildingService == null) return BucketState.Empty;

            Building bucket = buildingService.GetBuildingByID(bucketUniqueID);
            if (bucket == null) return BucketState.Empty;

            return bucket.GetBucketState();
        }

        /// <summary>
        /// Lấy lượng dầu hiện tại trong bucket.
        /// </summary>
        /// <param name="bucketUniqueID">ID của bucket.</param>
        /// <returns>Lượng dầu (0 nếu không tìm thấy).</returns>
        public float GetBucketCurrentOil(int bucketUniqueID)
        {
            if (buildingService == null) return 0f;

            Building bucket = buildingService.GetBuildingByID(bucketUniqueID);
            if (bucket == null) return 0f;

            return bucket.GetCurrentOil();
        }

        #endregion

        #region Helper

        /// <summary>
        /// Sắp xếp danh sách bucket theo thứ tự ưu tiên đổ dầu.
        /// </summary>
        /// <param name="buckets">Danh sách bucket cần sắp xếp (in-place).</param>
        private void SortBuckets(List<Building> buckets)
        {
            switch (fillPriority)
            {
                case BucketFillPriority.OldestFirst:
                    // Bucket có uniqueID nhỏ nhất (được đặt trước) sẽ được đổ trước
                    buckets.Sort((a, b) => a.UniqueID.CompareTo(b.UniqueID));
                    break;

                case BucketFillPriority.NewestFirst:
                    // Bucket có uniqueID lớn nhất (đặt sau) được đổ trước
                    buckets.Sort((a, b) => b.UniqueID.CompareTo(a.UniqueID));
                    break;

                case BucketFillPriority.LargestCapacity:
                    // Bucket có dung tích lớn nhất được đổ trước
                    buckets.Sort((a, b) => b.GetCapacity().CompareTo(a.GetCapacity()));
                    break;

                case BucketFillPriority.SmallestCapacity:
                    // Bucket có dung tích nhỏ nhất được đổ trước
                    buckets.Sort((a, b) => a.GetCapacity().CompareTo(b.GetCapacity()));
                    break;

                default:
                    buckets.Sort((a, b) => a.UniqueID.CompareTo(b.UniqueID));
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Thứ tự ưu tiên khi đổ dầu vào bucket.
    /// </summary>
    public enum BucketFillPriority
    {
        /// <summary>Bucket được đặt trước (uniqueID nhỏ nhất) được đổ trước.</summary>
        OldestFirst,

        /// <summary>Bucket mới đặt (uniqueID lớn nhất) được đổ trước.</summary>
        NewestFirst,

        /// <summary>Bucket có dung tích lớn nhất được đổ trước.</summary>
        LargestCapacity,

        /// <summary>Bucket có dung tích nhỏ nhất được đổ trước.</summary>
        SmallestCapacity
    }
}