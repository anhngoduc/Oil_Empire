// Assets/_Project/Scripts/Buildings/Building.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Building - MonoBehaviour gắn trên mỗi GameObject công trình (Drill hoặc Bucket).
    /// 
    /// Trách nhiệm:
    /// - Lưu trữ thông tin định danh của công trình (uniqueID, BuildingData reference, vị trí grid).
    /// - Cung cấp API để truy xuất thông tin từ bên ngoài.
    /// - Hỗ trợ tương tác (được PlayerController click vào).
    /// - Với Bucket: có thể hiển thị thanh dầu (tùy chọn).
    /// </summary>
    public class Building : MonoBehaviour
    {
        [Header("Thông tin công trình")]
        [Tooltip("ID duy nhất của công trình này (do BuildingManager cấp).")]
        [SerializeField] private int uniqueID;

        [Tooltip("ID của BuildingData (ScriptableObject).")]
        [SerializeField] private int buildingDataID;

        [Tooltip("Zone chứa công trình.")]
        [SerializeField] private int zoneID;

        [Tooltip("Plot chứa công trình.")]
        [SerializeField] private int plotID;

        [Tooltip("Tọa độ X trên grid.")]
        [SerializeField] private int gridX;

        [Tooltip("Tọa độ Z trên grid.")]
        [SerializeField] private int gridZ;

        // Tham chiếu đến BuildingData (cache)
        private BuildingData buildingData;

        // Tham chiếu đến dữ liệu runtime
        private BuildingRuntimeData runtimeData;

        #region Properties

        /// <summary>ID duy nhất của công trình.</summary>
        public int UniqueID => uniqueID;

        /// <summary>ID của BuildingData.</summary>
        public int BuildingDataID => buildingDataID;

        /// <summary>Zone chứa công trình.</summary>
        public int ZoneID => zoneID;

        /// <summary>Plot chứa công trình.</summary>
        public int PlotID => plotID;

        /// <summary>Tọa độ X trên grid.</summary>
        public int GridX => gridX;

        /// <summary>Tọa độ Z trên grid.</summary>
        public int GridZ => gridZ;

        /// <summary>Tham chiếu đến BuildingData (ScriptableObject).</summary>
        public BuildingData BuildingData
        {
            get
            {
                if (buildingData == null)
                {
                    // Lấy từ BuildingDatabase qua ServiceLocator
                    BuildingDatabase database = ServiceLocator.Get<BuildingDatabase>();
                    if (database != null)
                    {
                        buildingData = database.GetByID(buildingDataID);
                    }
                }
                return buildingData;
            }
        }

        /// <summary>Dữ liệu runtime của công trình (được gán bởi BuildingManager).</summary>
        public BuildingRuntimeData RuntimeData
        {
            get => runtimeData;
            set => runtimeData = value;
        }

        /// <summary>Loại công trình (Drill hoặc Bucket).</summary>
        public BuildingType Type => BuildingData != null ? BuildingData.buildingType : BuildingType.Drill;

        #endregion

        #region Khởi tạo

        /// <summary>
        /// Khởi tạo thông tin cho công trình.
        /// Được gọi bởi BuildingManager sau khi Instantiate.
        /// </summary>
        /// <param name="uniqueID">ID duy nhất.</param>
        /// <param name="data">BuildingData tương ứng.</param>
        /// <param name="zoneID">Zone chứa công trình.</param>
        /// <param name="plotID">Plot chứa công trình.</param>
        /// <param name="gridX">Tọa độ X trên grid.</param>
        /// <param name="gridZ">Tọa độ Z trên grid.</param>
        public void Initialize(int uniqueID, BuildingData data, int zoneID, int plotID, int gridX, int gridZ)
        {
            this.uniqueID = uniqueID;
            this.buildingDataID = data.buildingID;
            this.buildingData = data;
            this.zoneID = zoneID;
            this.plotID = plotID;
            this.gridX = gridX;
            this.gridZ = gridZ;

            // Chỉ đổi tên nếu là công trình Player (ID dương)
            if (uniqueID > 0)
                gameObject.name = $"{data.buildingName}_{uniqueID}";
        }

        #endregion

        #region Bucket Logic

        /// <summary>
        /// Lấy lượng dầu hiện tại trong bucket (chỉ có nghĩa nếu là Bucket).
        /// </summary>
        public float GetCurrentOil()
        {
            if (runtimeData != null && BuildingData.buildingType == BuildingType.Bucket)
            {
                return runtimeData.currentOilInBucket;
            }
            return 0f;
        }

        /// <summary>
        /// Cập nhật lượng dầu trong bucket (gọi từ BucketSystem).
        /// </summary>
        public void SetCurrentOil(float amount)
        {
            if (runtimeData != null && BuildingData.buildingType == BuildingType.Bucket)
            {
                runtimeData.currentOilInBucket = Mathf.Clamp(amount, 0f, BuildingData.capacity);
            }
        }

        /// <summary>
        /// Lấy dung tích tối đa của bucket.
        /// </summary>
        public float GetCapacity()
        {
            if (BuildingData != null && BuildingData.buildingType == BuildingType.Bucket)
            {
                return BuildingData.capacity;
            }
            return 0f;
        }

        /// <summary>
        /// Lấy trạng thái hiện tại của bucket (Empty/Partial/Full).
        /// </summary>
        public BucketState GetBucketState()
        {
            if (BuildingData == null || BuildingData.buildingType != BuildingType.Bucket)
                return BucketState.Empty;

            float current = GetCurrentOil();
            float capacity = GetCapacity();

            if (current <= 0f) return BucketState.Empty;
            if (current >= capacity) return BucketState.Full;
            return BucketState.Partial;
        }

        #endregion
    }
}