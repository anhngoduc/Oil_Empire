// Assets/_Project/Scripts/Buildings/BuildingPlacer.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// BuildingPlacer - Xử lý logic placement mode khi người chơi đang đặt công trình.
    /// 
    /// Trách nhiệm:
    /// - Raycast từ camera xuống mặt đất để xác định vị trí đặt.
    /// - Chuyển đổi vị trí world sang grid (qua GridManager).
    /// - Kiểm tra tính hợp lệ của vị trí (qua GridManager và LandManager).
    /// - Điều khiển BuildingPreview (vị trí, màu sắc).
    /// - Xác nhận đặt công trình (gọi BuildingManager).
    /// 
    /// Lưu ý: Class này KHÔNG phải MonoBehaviour. Nó được BuildingManager tạo ra
    /// và nhận Update() từ BuildingManager mỗi frame.
    /// </summary>
    public class BuildingPlacer
    {
        private float snapDistance = 3f;

        // Tham chiếu đến các service
        private IGridService gridService;
        private ILandService landService;
        private BuildingManager buildingManager;
        private ZoneManager zoneManager;

        // Camera chính
        private Camera mainCamera;

        // Cấu hình
        private GameConfig gameConfig;
         
        // Dữ liệu placement hiện tại
        private BuildingData currentBuildingData;
        private BuildingPreview previewInstance;

        // Trạng thái
        private bool isPlacementActive;
        private bool currentPlacementValid;
        private Vector3 currentWorldPosition;
        private int currentZoneID;
        private int currentPlotID;
        private int currentGridX;
        private int currentGridZ;

        #region Properties

        /// <summary>Placement mode có đang active không?</summary>
        public bool IsActive => isPlacementActive;

        /// <summary>BuildingData đang được đặt.</summary>
        public BuildingData CurrentData => currentBuildingData;

        /// <summary>Vị trí world hiện tại của preview.</summary>
        public Vector3 CurrentWorldPosition => currentWorldPosition;

        #endregion

        #region Constructor

        /// <summary>
        /// Khởi tạo BuildingPlacer.
        /// </summary>
        /// <param name="buildingManager">Tham chiếu đến BuildingManager cha.</param>
        /// <param name="config">GameConfig.</param>
        public BuildingPlacer(BuildingManager buildingManager, GameConfig config)
        {
            this.buildingManager = buildingManager;
            this.gameConfig = config;
            this.mainCamera = Camera.main;

            // Lấy service từ ServiceLocator
            this.gridService = ServiceLocator.Get<IGridService>();
            this.landService = ServiceLocator.Get<ILandService>();

            this.zoneManager = GameObject.FindObjectOfType<ZoneManager>();

            if (gridService == null)
                Debug.LogError("[BuildingPlacer] IGridService chưa được đăng ký!");
            if (landService == null)
                Debug.LogError("[BuildingPlacer] ILandService chưa được đăng ký!");
        }

        #endregion

        #region Placement Mode

        /// <summary>
        /// Bắt đầu Placement Mode với BuildingData được chọn.
        /// </summary>
        /// <param name="data">BuildingData của công trình muốn đặt.</param>
        /// <param name="previewPrefab">Prefab cho preview (có thể là chính prefab gốc).</param>
        public void EnterPlacementMode(BuildingData data, GameObject previewPrefab)
        {
            if (data == null)
            {
                Debug.LogError("[BuildingPlacer] BuildingData null!");
                return;
            }

            if (isPlacementActive)
                ExitPlacementMode();

            currentBuildingData = data;
            isPlacementActive = true;

            CreatePreview(previewPrefab != null ? previewPrefab : data.prefab);

            Debug.Log($"[BuildingPlacer] Bắt đầu Placement Mode: {data.buildingName}.");
            EventBus.Publish(new OnPlacementStarted(data));
        }

        /// <summary>
        /// Thoát Placement Mode.
        /// </summary>
        public void ExitPlacementMode()
        {
            if (!isPlacementActive) return;

            isPlacementActive = false;

            if (previewInstance != null)
            {
                previewInstance.Deactivate();
                Object.Destroy(previewInstance.gameObject);
                previewInstance = null;
            }

            currentBuildingData = null;

            Debug.Log("[BuildingPlacer] Thoát Placement Mode.");
            EventBus.Publish(new OnPlacementEnded());
        }

        /// <summary>
        /// Tạo preview object.
        /// </summary>
        private void CreatePreview(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[BuildingPlacer] Prefab preview null!");
                return;
            }

            // Tạo GameObject rỗng làm container
            GameObject previewGO = new GameObject("BuildingPreview");
            previewInstance = previewGO.AddComponent<BuildingPreview>();

            // Kích hoạt preview với model
            previewInstance.Activate(prefab);
        }

        /// <summary>
        /// Gọi từ nút Đặt trên UI (mobile).
        /// </summary>
        public void PlaceButtonClicked()
        {
            if (currentPlacementValid)
                TryPlaceBuilding();
        }

        #endregion

        #region Update Loop (gọi từ BuildingManager.Update)

        /// <summary>
        /// Cập nhật mỗi frame khi đang trong Placement Mode.
        /// Được gọi bởi BuildingManager.Update().
        /// </summary>
        public void UpdatePlacement()
        {
            if (!isPlacementActive || previewInstance == null) return;

            // Kiểm tra còn hàng không - nếu hết thì tự động thoát
            IPlayerDataService playerData = ServiceLocator.Get<IPlayerDataService>();
            if (playerData != null && currentBuildingData != null)
            {
                if (playerData.GetInventoryCount(currentBuildingData.buildingID) <= 0)
                {
                    Debug.Log($"[BuildingPlacer] Hết {currentBuildingData.buildingName}, tự động thoát.");
                    ExitPlacementMode();
                    return;
                }
            }

            Camera cam = Camera.main;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = cam.ScreenPointToRay(screenCenter);

            float maxDistance = gameConfig.raycastDistance;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

            Vector3 hitPoint = ray.GetPoint(maxDistance);
            bool foundHit = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Player")) continue;

                hitPoint = hit.point;
                foundHit = true;
                break;
            }

            // Thử snap vào grid
            bool snapped = false;
            if (foundHit && gridService != null)
            {
                int playerZoneID = playerData.PlayerZoneID;
                bool inGrid = gridService.WorldToGrid(hitPoint, out currentZoneID, out currentPlotID, out currentGridX, out currentGridZ);

                if (inGrid && currentZoneID == playerZoneID)
                {
                    Vector3 snapPos = gridService.GridToWorld(currentZoneID, currentPlotID, currentGridX, currentGridZ);
                    float dist = Vector3.Distance(hitPoint, snapPos);

                    if (dist <= snapDistance)
                    {
                        currentWorldPosition = snapPos;

                        bool plotUnlocked = playerData.IsPlotUnlocked(currentZoneID, currentPlotID);
                        bool cellEmpty = gridService.CanPlace(currentZoneID, currentPlotID, currentGridX, currentGridZ);

                        currentPlacementValid = plotUnlocked && cellEmpty;
                        snapped = true;
                    }
                }
            }

            if (!snapped)
            {
                currentWorldPosition = hitPoint;
                currentPlacementValid = false;
            }

            float followSpeed = 30f;
            previewInstance.transform.position = Vector3.Lerp(
                previewInstance.transform.position,
                currentWorldPosition,
                followSpeed * Time.deltaTime
            );

            previewInstance.SetValid(currentPlacementValid);

        }

        #endregion

        #region Đặt Công trình

        /// <summary>
        /// Thử đặt công trình tại vị trí hiện tại.
        /// </summary>
        public void TryPlaceBuilding()
        {
            if (!isPlacementActive) return;
            if (!currentPlacementValid) return;
            if (currentBuildingData == null) return;

            bool success = buildingManager.PlaceBuilding(
                currentBuildingData,
                currentZoneID,
                currentPlotID,
                currentGridX,
                currentGridZ
            );

            if (success)
            {
                Debug.Log($"[BuildingPlacer] Đặt thành công!");

                // THÊM: Kiểm tra nếu hết hàng thì tự động thoát
                IPlayerDataService playerData = ServiceLocator.Get<IPlayerDataService>();
                if (playerData != null && playerData.GetInventoryCount(currentBuildingData.buildingID) <= 0)
                {
                    ExitPlacementMode();
                }
            }
            else
            {
                Debug.LogWarning("[BuildingPlacer] Đặt thất bại!");
            }
        }

        #endregion

        public void TryPlaceFromUI()
        {
            if (currentPlacementValid)
                TryPlaceBuilding();
        }
    }
}