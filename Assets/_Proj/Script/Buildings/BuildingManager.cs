// Assets/_Project/Scripts/Buildings/BuildingManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// BuildingManager - Service quản lý toàn bộ công trình của Player.
    /// Implement IBuildingService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Quản lý Placement Mode (vào/ra chế độ đặt công trình).
    /// - Spawn/Xóa công trình (GameObject).
    /// - Đồng bộ với GridManager (đánh dấu ô) và PlayerDataManager (cập nhật danh sách).
    /// - Quản lý danh sách Building runtime.
    /// - Hỗ trợ Save/Load (khôi phục công trình từ SaveData).
    /// </summary>
    public class BuildingManager : MonoBehaviour, IBuildingService
    {
        [Header("Tham chiếu")]
        [Tooltip("GameConfig (lấy cellSize, groundLayer...).")]
        [SerializeField] private GameConfig gameConfig;

        [Tooltip("Transform cha chứa tất cả công trình đã đặt (để tổ chức hierarchy).")]
        [SerializeField] private Transform buildingsParent;
        [SerializeField] private ZoneManager zoneManager;
        // === Dữ liệu runtime ===

        /// <summary>
        /// Danh sách tất cả Building (GameObject) đã đặt.
        /// Key: uniqueID, Value: Building component.
        /// </summary>
        private Dictionary<int, Building> activeBuildings;

        /// <summary>
        /// BuildingPlacer - xử lý logic placement.
        /// </summary>
        private BuildingPlacer buildingPlacer;

        // Tham chiếu service
        private IPlayerDataService playerDataService;
        private IInventoryService inventoryService;
        private IGridService gridService;

        #region IBuildingService Properties

        /// <summary>Đang trong Placement Mode?</summary>
        public bool IsInPlacementMode => buildingPlacer != null && buildingPlacer.IsActive;

        /// <summary>BuildingData đang được đặt (null nếu không trong Placement Mode).</summary>
        public BuildingData CurrentPlacementData => buildingPlacer != null ? buildingPlacer.CurrentData : null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IBuildingService>(this);

            // Khởi tạo dictionary
            activeBuildings = new Dictionary<int, Building>();

            // Tạo parent nếu chưa có
            if (buildingsParent == null)
            {
                GameObject parentGO = new GameObject("Buildings_Parent");
                buildingsParent = parentGO.transform;
            }
        }

        private void Start()
        {
            // Lấy các service
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            inventoryService = ServiceLocator.Get<IInventoryService>();
            gridService = ServiceLocator.Get<IGridService>();

            // Khởi tạo BuildingPlacer
            buildingPlacer = new BuildingPlacer(this, gameConfig);

            // Khởi tạo grid cho các plot đã mở (nếu load game)
            InitializeGridsForExistingBuildings();
        }

        private void Update()
        {
            // Cập nhật placement mode mỗi frame
            if (buildingPlacer != null && buildingPlacer.IsActive)
            {
                buildingPlacer.UpdatePlacement();
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IBuildingService>();
        }

        #endregion

        #region Khởi tạo

        /// <summary>
        /// Khởi tạo grid cho các plot có công trình đã lưu.
        /// Gọi sau khi Load Game.
        /// </summary>
        private void InitializeGridsForExistingBuildings()
        {
            if (playerDataService == null) return;

            List<BuildingRuntimeData> existingBuildings = playerDataService.GetAllBuildings();
            if (existingBuildings == null || existingBuildings.Count == 0) return;

            HashSet<string> initializedPlots = new HashSet<string>();

            foreach (var buildingData in existingBuildings)
            {
                string key = $"{buildingData.zoneID}_{buildingData.plotID}";
                if (!initializedPlots.Contains(key))
                {
                    // GridManager sẽ tự khởi tạo grid nếu cần
                    initializedPlots.Add(key);
                }
            }

            Debug.Log($"[BuildingManager] Đã kiểm tra {initializedPlots.Count} plot(s) có công trình.");
        }

        #endregion

        #region Placement Mode

        /// <summary>
        /// Vào Placement Mode với BuildingData được chọn.
        /// Gọi từ InventoryUI khi người chơi chọn item.
        /// </summary>
        /// <param name="data">BuildingData muốn đặt.</param>
        public void EnterPlacementMode(BuildingData data)
        {
            if (data == null)
            {
                Debug.LogError("[BuildingManager] BuildingData null!");
                return;
            }

            // Kiểm tra inventory còn item không
            if (inventoryService != null && inventoryService.GetCount(data.buildingID) <= 0)
            {
                Debug.LogWarning($"[BuildingManager] Không còn {data.buildingName} trong inventory!");
                return;
            }

            buildingPlacer.EnterPlacementMode(data, data.prefab);
        }

        /// <summary>
        /// Thoát Placement Mode.
        /// </summary>
        public void CancelPlacement()
        {
            buildingPlacer.ExitPlacementMode();
        }

        #endregion

        #region Đặt / Xóa Công trình

        /// <summary>
        /// Đặt một công trình mới tại vị trí chỉ định.
        /// Được gọi bởi BuildingPlacer khi người chơi xác nhận.
        /// </summary>
        /// <param name="data">BuildingData của công trình.</param>
        /// <param name="zoneID">Zone đích.</param>
        /// <param name="plotID">Plot đích.</param>
        /// <param name="gridX">Tọa độ X grid.</param>
        /// <param name="gridZ">Tọa độ Z grid.</param>
        /// <returns>True nếu đặt thành công.</returns>
        public bool PlaceBuilding(BuildingData data, int zoneID, int plotID, int gridX, int gridZ)
        {
            // === Kiểm tra điều kiện ===

            // 1. Kiểm tra inventory
            if (inventoryService != null && inventoryService.GetCount(data.buildingID) <= 0)
            {
                Debug.LogWarning("[BuildingManager] Không đủ item trong inventory!");
                return false;
            }

            // 2. Kiểm tra grid (ô trống, plot đã mở, zone của player)
            if (gridService != null && !gridService.CanPlace(zoneID, plotID, gridX, gridZ))
            {
                Debug.LogWarning("[BuildingManager] Không thể đặt tại vị trí này!");
                return false;
            }

            // === Thực hiện đặt ===

            // 3. Lấy vị trí world
            Vector3 worldPos = gridService.GridToWorld(zoneID, plotID, gridX, gridZ);

            // 4. Spawn công trình
            Transform zoneT = zoneManager.GetZoneTransform(zoneID);
            Quaternion rot = zoneT != null ? zoneT.rotation : Quaternion.identity;
            GameObject buildingGO = Instantiate(data.prefab, worldPos, rot, buildingsParent);

            Building building = buildingGO.GetComponent<Building>();

            if (building == null)
            {
                Debug.LogError($"[BuildingManager] Prefab {data.buildingName} không có script Building!");
                Destroy(buildingGO);
                return false;
            }

            // 5. Cấp unique ID và khởi tạo
            int uniqueID = playerDataService.GetNextBuildingID();
            building.Initialize(uniqueID, data, zoneID, plotID, gridX, gridZ);

            // 6. Tạo dữ liệu runtime
            BuildingRuntimeData runtimeData = data.CreateRuntimeData(uniqueID, zoneID, plotID, gridX, gridZ);
            runtimeData.buildingObject = buildingGO;
            building.RuntimeData = runtimeData;

            // 7. Cập nhật grid
            gridService.OccupyCell(zoneID, plotID, gridX, gridZ, uniqueID, data.buildingType);

            // 8. Cập nhật player data
            playerDataService.AddBuilding(runtimeData);

            // 9. Trừ inventory
            inventoryService.RemoveItem(data.buildingID, 1);

            // 10. Thêm vào dictionary active
            activeBuildings[uniqueID] = building;

            // 11. Phát sự kiện
            EventBus.Publish(new OnBuildingPlaced(uniqueID, data.buildingID, data.buildingType,
                zoneID, plotID, gridX, gridZ));

            Debug.Log($"[BuildingManager] Đã đặt {data.buildingName} (ID={uniqueID}) tại Zone={zoneID} Plot={plotID} ({gridX},{gridZ}).");

            return true;
        }

        /// <summary>
        /// Xóa một công trình đã đặt.
        /// </summary>
        /// <param name="uniqueID">ID duy nhất của công trình cần xóa.</param>
        public void RemoveBuilding(int uniqueID)
        {
            if (!activeBuildings.TryGetValue(uniqueID, out Building building))
            {
                Debug.LogWarning($"[BuildingManager] Không tìm thấy Building ID={uniqueID}!");
                return;
            }

            // Lấy thông tin trước khi xóa
            int zoneID = building.ZoneID;
            int plotID = building.PlotID;
            int gridX = building.GridX;
            int gridZ = building.GridZ;

            // Giải phóng ô grid
            if (gridService != null)
            {
                gridService.FreeCell(zoneID, plotID, gridX, gridZ);
            }

            // Xóa khỏi player data
            playerDataService.RemoveBuilding(uniqueID);

            // Xóa khỏi dictionary
            activeBuildings.Remove(uniqueID);

            // Hủy GameObject
            Destroy(building.gameObject);

            // Phát sự kiện
            EventBus.Publish(new OnBuildingRemoved(uniqueID, zoneID, plotID, gridX, gridZ));

            Debug.Log($"[BuildingManager] Đã xóa Building ID={uniqueID}.");
        }
        public void TryPlaceFromUI()
        {
            buildingPlacer.TryPlaceFromUI();
        }

        #endregion

        #region Truy xuất Công trình

        /// <summary>
        /// Lấy danh sách tất cả Building (GameObject) của Player.
        /// </summary>
        public List<Building> GetAllPlayerBuildings()
        {
            return new List<Building>(activeBuildings.Values);
        }

        /// <summary>
        /// Lọc danh sách Building theo loại (Drill hoặc Bucket).
        /// </summary>
        /// <param name="type">Loại cần lọc.</param>
        public List<Building> GetBuildingsOfType(BuildingType type)
        {
            List<Building> result = new List<Building>();
            foreach (var building in activeBuildings.Values)
            {
                if (building.Type == type)
                {
                    result.Add(building);
                }
            }
            return result;
        }

        /// <summary>
        /// Tìm Building theo unique ID.
        /// </summary>
        public Building GetBuildingByID(int uniqueID)
        {
            activeBuildings.TryGetValue(uniqueID, out Building building);
            return building;
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Khôi phục công trình từ SaveData khi Load Game.
        /// </summary>
        /// <param name="saveDataList">Danh sách PlacedBuildingSaveData từ file save.</param>
        public void RestoreBuildingsFromSave(List<PlacedBuildingSaveData> saveDataList)
        {
            if (saveDataList == null || saveDataList.Count == 0) return;

            BuildingDatabase database = gameConfig.buildingDatabase;
            if (database == null)
            {
                Debug.LogError("[BuildingManager] BuildingDatabase chưa được gán!");
                return;
            }

            int restoredCount = 0;

            foreach (var saveData in saveDataList)
            {
                // Lấy BuildingData từ database
                BuildingData data = database.GetByID(saveData.buildingDataID);
                if (data == null)
                {
                    Debug.LogError($"[BuildingManager] Không tìm thấy BuildingData ID={saveData.buildingDataID}!");
                    continue;
                }

                // Lấy vị trí world
                Vector3 worldPos = gridService.GridToWorld(saveData.zoneID, saveData.plotID, saveData.gridX, saveData.gridZ);

                // Spawn công trình
                Transform zoneT = zoneManager.GetZoneTransform(saveData.zoneID);
                Quaternion rot = zoneT != null ? zoneT.rotation : Quaternion.identity;
                GameObject buildingGO = Instantiate(data.prefab, worldPos, rot, buildingsParent);

                Building building = buildingGO.GetComponent<Building>();

                if (building == null)
                {
                    Debug.LogError($"[BuildingManager] Prefab {data.buildingName} không có script Building!");
                    Destroy(buildingGO);
                    continue;
                }

                // Khởi tạo với uniqueID từ save
                building.Initialize(saveData.uniqueBuildingID, data, saveData.zoneID, saveData.plotID, saveData.gridX, saveData.gridZ);

                // Tạo dữ liệu runtime
                BuildingRuntimeData runtimeData = BuildingRuntimeData.FromSaveData(saveData);
                runtimeData.buildingObject = buildingGO;
                building.RuntimeData = runtimeData;

                // Đánh dấu grid
                gridService.OccupyCell(saveData.zoneID, saveData.plotID, saveData.gridX, saveData.gridZ,
                    saveData.uniqueBuildingID, data.buildingType);

                // Thêm vào dictionary
                activeBuildings[saveData.uniqueBuildingID] = building;

                restoredCount++;
            }

            Debug.Log($"[BuildingManager] Đã khôi phục {restoredCount}/{saveDataList.Count} công trình từ save.");
        }

        #endregion
    }
}