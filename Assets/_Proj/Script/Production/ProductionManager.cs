// Assets/_Project/Scripts/Production/ProductionManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// ProductionManager - Service quản lý sản xuất dầu tự động mỗi giây.
    /// Implement IProductionService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Mỗi giây: tính tổng sản lượng từ tất cả Drill của Player.
    /// - Áp dụng hệ số nhân từ mảnh đất (Plot) cho từng Drill.
    /// - Gửi dầu đã sản xuất vào BucketSystem để phân phối vào bucket.
    /// - Nếu tất cả bucket đầy: tạm dừng sản xuất.
    /// - Nếu có bucket được thu dầu (trống trở lại): tiếp tục sản xuất.
    /// - Phát sự kiện OnOilProductionUpdated mỗi giây.
    /// 
    /// Công thức:
    ///   Tổng sản lượng/giây = SUM(Drill.productionRate * Plot.oilMultiplier)
    /// </summary>
    public class ProductionManager : MonoBehaviour, IProductionService
    {
        [Header("Cấu hình")]
        [Tooltip("Khoảng thời gian giữa các tick sản xuất (giây). Mặc định = 1.")]
        [SerializeField] private float tickInterval = 1f;

        // === Dữ liệu runtime ===

        /// <summary>Sản xuất có đang bị tạm dừng không? (tất cả bucket đầy).</summary>
        private bool isPaused;

        /// <summary>Tổng tốc độ sản xuất hiện tại (đã áp hệ số).</summary>
        private long totalProductionRate;

        /// <summary>Coroutine đang chạy vòng lặp sản xuất.</summary>
        private IEnumerator productionCoroutine;

        // Tham chiếu đến các service
        private IBuildingService buildingService;
        private ILandService landService;
        private IBucketService bucketService;

        #region IProductionService Properties

        /// <summary>Sản xuất có đang bị tạm dừng không?</summary>
        public bool IsProductionPaused => isPaused;

        /// <summary>Tổng tốc độ sản xuất hiện tại (đã áp hệ số).</summary>
        public long TotalProductionRate => totalProductionRate;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IProductionService>(this);
        }

        private void Start()
        {
            // Lấy các service
            buildingService = ServiceLocator.Get<IBuildingService>();
            landService = ServiceLocator.Get<ILandService>();
            bucketService = ServiceLocator.Get<IBucketService>();

            if (buildingService == null)
                Debug.LogError("[ProductionManager] IBuildingService chưa được đăng ký!");
            if (landService == null)
                Debug.LogError("[ProductionManager] ILandService chưa được đăng ký!");
            if (bucketService == null)
                Debug.LogError("[ProductionManager] IBucketService chưa được đăng ký!");

            // Đăng ký lắng nghe sự kiện bucket được thu dầu
            EventBus.Subscribe<OnBucketEmptied>(OnBucketEmptiedHandler);
            EventBus.Subscribe<OnBuildingPlaced>(OnBuildingChangedHandler);
            EventBus.Subscribe<OnBuildingRemoved>(OnBuildingChangedHandler);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IProductionService>();
            EventBus.Unsubscribe<OnBucketEmptied>(OnBucketEmptiedHandler);
            EventBus.Unsubscribe<OnBuildingPlaced>(OnBuildingChangedHandler);
            EventBus.Unsubscribe<OnBuildingRemoved>(OnBuildingChangedHandler);
            StopProduction();
        }

        #endregion

        #region Vòng lặp sản xuất

        /// <summary>
        /// Bắt đầu vòng lặp sản xuất.
        /// Gọi bởi GameManager khi game bắt đầu.
        /// </summary>
        public void StartProduction()
        {
            StopProduction();

            productionCoroutine = ProductionRoutine();
            CoroutineRunner.Instance.Run(productionCoroutine);

        }

        /// <summary>
        /// Dừng vòng lặp sản xuất.
        /// </summary>
        public void StopProduction()
        {
            if (productionCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(productionCoroutine);
                productionCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine chạy vòng lặp sản xuất.
        /// </summary>
        private IEnumerator ProductionRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(tickInterval);

            while (true)
            {
                yield return wait;

                // Nếu không bị tạm dừng, thực hiện tick sản xuất
                if (!isPaused)
                {
                    ProductionTick();
                }
            }
        }

        /// <summary>
        /// Thực hiện một tick sản xuất:
        /// 1. Tính tổng sản lượng từ tất cả Drill.
        /// 2. Gửi dầu vào BucketSystem.
        /// 3. Phát sự kiện cập nhật UI.
        /// </summary>
        private void ProductionTick()
        {
            if (buildingService == null || landService == null || bucketService == null)
                return;

            // 1. Lấy tất cả Drill của Player
            List<Building> drills = buildingService.GetBuildingsOfType(BuildingType.Drill);

            // 2. Tính tổng sản lượng (có áp hệ số mảnh)
            float totalProducedThisTick = 0f;
            totalProductionRate = 0;

            foreach (Building drill in drills)
            {
                if (drill.BuildingData == null) continue;

                // Lấy tốc độ cơ bản của Drill
                float baseRate = drill.BuildingData.productionRate;

                // Lấy hệ số nhân từ mảnh đất
                float multiplier = landService.GetPlotMultiplier(drill.ZoneID, drill.PlotID);

                // Sản lượng thực tế của Drill này trong 1 giây
                float drillProduction = baseRate * multiplier;

                totalProducedThisTick += drillProduction;
                totalProductionRate += (long)drillProduction;
            }

            // 3. Lấy thông tin bucket để báo cáo
            List<Building> buckets = buildingService.GetBuildingsOfType(BuildingType.Bucket);
            int totalBuckets = buckets.Count;
            int filledBuckets = 0;

            foreach (Building bucket in buckets)
            {
                if (bucket.GetBucketState() == BucketState.Full)
                {
                    filledBuckets++;
                }
            }

            // 4. Gửi dầu vào BucketSystem
            if (totalProducedThisTick > 0f)
            {
                bucketService.FillOil((long)totalProducedThisTick);
            }

            // 5. Phát sự kiện cập nhật UI
            EventBus.Publish(new OnOilProductionUpdated(
                totalProducedThisTick,
                totalProductionRate,
                drills.Count,
                totalBuckets,
                filledBuckets
            ));
        }

        #endregion

        #region Điều khiển Tạm dừng / Tiếp tục

        /// <summary>
        /// Tạm dừng sản xuất (khi tất cả bucket đầy).
        /// Được gọi bởi BucketSystem khi phát hiện tất cả bucket đầy.
        /// </summary>
        public void PauseProduction()
        {
            if (!isPaused)
            {
                isPaused = true;
                Debug.Log("[ProductionManager] SẢN XUẤT TẠM DỪNG: Tất cả bucket đã đầy.");
            }
        }

        /// <summary>
        /// Tiếp tục sản xuất (khi có bucket được thu dầu).
        /// </summary>
        public void ResumeProduction()
        {
            if (isPaused)
            {
                isPaused = false;
                Debug.Log("[ProductionManager] SẢN XUẤT TIẾP TỤC: Có bucket trống.");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Xử lý khi một bucket được thu dầu (trở về rỗng).
        /// Nếu sản xuất đang tạm dừng, tiếp tục sản xuất.
        /// </summary>
        private void OnBucketEmptiedHandler(OnBucketEmptied evt)
        {
            if (isPaused)
            {
                ResumeProduction();
            }
        }

        /// <summary>
        /// Xử lý khi công trình được đặt hoặc xóa.
        /// Cập nhật lại tổng sản lượng và kiểm tra trạng thái bucket.
        /// </summary>
        private void OnBuildingChangedHandler<T>(T evt) where T : struct
        {
            // Ép kiểu để lấy thông tin (nếu cần)
            // Với OnBuildingPlaced: kiểm tra nếu là Drill thì cập nhật tổng sản lượng
            // Với OnBuildingRemoved: tương tự

            // Buộc ProductionTick chạy lại ở frame tiếp theo để cập nhật
            // (Không cần làm gì vì ProductionTick tự chạy mỗi giây)
        }

        #endregion

        #region Helper

        /// <summary>
        /// Tính toán sản lượng hiện tại mà không cần đợi tick.
        /// Dùng cho UI hiển thị real-time (nếu cần).
        /// </summary>
        /// <returns>Tổng sản lượng/giây hiện tại.</returns>
        public float CalculateCurrentProductionRate()
        {
            if (buildingService == null || landService == null) return 0f;

            float rate = 0f;
            List<Building> drills = buildingService.GetBuildingsOfType(BuildingType.Drill);

            foreach (Building drill in drills)
            {
                if (drill.BuildingData == null) continue;
                float baseRate = drill.BuildingData.productionRate;
                float multiplier = landService.GetPlotMultiplier(drill.ZoneID, drill.PlotID);
                rate += baseRate * multiplier;
            }

            return rate;
        }

        #endregion
    }
}