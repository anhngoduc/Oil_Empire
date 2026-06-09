// Assets/_Project/Scripts/Core/GameManager.cs

using UnityEngine;
using System.Collections;

namespace OilGame
{
    /// <summary>
    /// GameManager - Script điều phối toàn bộ game.
    /// Đây là script trung tâm, chịu trách nhiệm:
    /// 
    /// 1. KHỞI TẠO: Đảm bảo tất cả Manager được khởi tạo đúng thứ tự.
    /// 2. NEW GAME: Thiết lập dữ liệu ban đầu cho người chơi mới.
    /// 3. LOAD GAME: Khôi phục trạng thái từ file save.
    /// 4. VÒNG ĐỜI: Quản lý pause/resume, thoát game.
    /// 
    /// THỨ TỰ KHỞI TẠO QUAN TRỌNG:
    /// 1. CoroutineRunner (tạo trước để các service khác dùng)
    /// 2. Service Locator đã sẵn sàng (static class)
    /// 3. EventBus đã sẵn sàng (static class)
    /// 4. Data Layer (BuildingDatabase.Initialize)
    /// 5. PlayerDataManager (đăng ký IPlayerDataService)
    /// 6. LandManager (đăng ký ILandService)
    /// 7. GridManager (đăng ký IGridService)
    /// 8. InventoryManager (đăng ký IInventoryService)
    /// 9. ShopManager (đăng ký IShopService)
    /// 10. MarketManager (đăng ký IMarketService)
    /// 11. BuildingManager (đăng ký IBuildingService) - cần GridManager và InventoryManager
    /// 12. ProductionManager (đăng ký IProductionService) - cần BuildingManager
    /// 13. BucketSystem (đăng ký IBucketService) - cần BuildingManager
    /// 14. BotSimulationManager - cần LandManager và MarketManager
    /// 15. SaveLoadManager - cần tất cả Manager để lưu/load
    /// 
    /// Sau khi tất cả Manager sẵn sàng:
    /// - Nếu có file save -> Load Game
    /// - Nếu không có -> New Game
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("=== Tham chiếu Manager (gán từ Editor) ===")]
        [Tooltip("PlayerDataManager - quản lý dữ liệu người chơi.")]
        [SerializeField] private PlayerDataManager playerDataManager;

        [Tooltip("LandManager - quản lý đất đai.")]
        [SerializeField] private LandManager landManager;

        [Tooltip("GridManager - quản lý lưới ô.")]
        [SerializeField] private GridManager gridManager;

        [Tooltip("InventoryManager - quản lý túi đồ.")]
        [SerializeField] private InventoryManager inventoryManager;

        [Tooltip("ShopManager - quản lý cửa hàng.")]
        [SerializeField] private ShopManager shopManager;

        [Tooltip("MarketManager - quản lý chợ dầu.")]
        [SerializeField] private MarketManager marketManager;

        [Tooltip("BuildingManager - quản lý công trình.")]
        [SerializeField] private BuildingManager buildingManager;

        [Tooltip("ProductionManager - quản lý sản xuất dầu.")]
        [SerializeField] private ProductionManager productionManager;

        [Tooltip("BucketSystem - quản lý xô chứa dầu.")]
        [SerializeField] private BucketSystem bucketSystem;

        [Tooltip("BotSimulationManager - giả lập người chơi ảo.")]
        [SerializeField] private BotSimulationManager botSimulationManager;

        [Tooltip("SaveLoadManager - lưu và load game.")]
        [SerializeField] private SaveLoadManager saveLoadManager;

        [Header("=== Cấu hình ===")]
        [Tooltip("GameConfig - cấu hình tổng game.")]
        [SerializeField] private GameConfig gameConfig;

        [Header("=== UI (gán từ Editor) ===")]
        [Tooltip("HUDManager - giao diện chính trong game.")]
        [SerializeField] private HUDManager hudManager;


        // === Trạng thái game ===
        private bool isGameInitialized = false;
        private bool isNewGame = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // === Bước 0: Đảm bảo CoroutineRunner tồn tại ===
            if (CoroutineRunner.Instance == null)
            {
                GameObject runnerGO = new GameObject("[CoroutineRunner]");
                runnerGO.AddComponent<CoroutineRunner>();
                Debug.Log("[GameManager] Đã tạo CoroutineRunner.");
            }

            // === Bước 1: Khởi tạo Database ===
            if (gameConfig != null && gameConfig.buildingDatabase != null)
            {
                gameConfig.buildingDatabase.Initialize();
            }
            else
            {
                Debug.LogError("[GameManager] GameConfig hoặc BuildingDatabase chưa được gán!");
            }

            // === Bước 2: Kiểm tra các Manager đã được gán ===
            ValidateManagers();

            // === Bước 3: Đảm bảo tất cả Manager đã Awake() và đăng ký Service ===
            // (Unity tự động gọi Awake() của tất cả MonoBehaviour được gán trong scene)
            // Nếu có Manager nào chưa được gán, log lỗi
        }

        private IEnumerator Start()
        {
            // Đợi 1 frame để tất cả Manager hoàn thành Awake() và Start()
            yield return null;

            // === Bước 4: Kiểm tra file save ===
            if (saveLoadManager != null && saveLoadManager.HasSaveFile())
            {
                // Có file save -> Load Game
                LoadGame();
            }
            else
            {
                // Không có file save -> New Game
                NewGame();
            }

            // === Bước 5: Bắt đầu các hệ thống tự động ===
            StartAllSystems();

            // === Bước 6: Khởi tạo UI ===
            InitializeUI();

            isGameInitialized = true;

            Debug.Log($"[GameManager] Game đã sẵn sàng! ({(isNewGame ? "New Game" : "Load Game")})");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Auto Save khi mất focus (được xử lý trong SaveLoadManager)
            // Không cần làm gì thêm ở đây
        }

        private void OnApplicationQuit()
        {
            // Save khi thoát (được xử lý trong SaveLoadManager)
            Debug.Log("[GameManager] Ứng dụng đang thoát...");
        }

        private void OnDestroy()
        {
            // Dọn dẹp
            StopAllCoroutines();
        }

        #endregion

        #region Kiểm tra Manager

        /// <summary>
        /// Kiểm tra tất cả Manager đã được gán trong Editor chưa.
        /// Nếu thiếu, log cảnh báo.
        /// </summary>
        private void ValidateManagers()
        {
            if (playerDataManager == null)
                Debug.LogError("[GameManager] PlayerDataManager chưa được gán!");
            if (landManager == null)
                Debug.LogError("[GameManager] LandManager chưa được gán!");
            if (gridManager == null)
                Debug.LogError("[GameManager] GridManager chưa được gán!");
            if (inventoryManager == null)
                Debug.LogError("[GameManager] InventoryManager chưa được gán!");
            if (shopManager == null)
                Debug.LogError("[GameManager] ShopManager chưa được gán!");
            if (marketManager == null)
                Debug.LogError("[GameManager] MarketManager chưa được gán!");
            if (buildingManager == null)
                Debug.LogError("[GameManager] BuildingManager chưa được gán!");
            if (productionManager == null)
                Debug.LogError("[GameManager] ProductionManager chưa được gán!");
            if (bucketSystem == null)
                Debug.LogError("[GameManager] BucketSystem chưa được gán!");
            if (botSimulationManager == null)
                Debug.LogError("[GameManager] BotSimulationManager chưa được gán!");
            if (saveLoadManager == null)
                Debug.LogError("[GameManager] SaveLoadManager chưa được gán!");
            if (gameConfig == null)
                Debug.LogError("[GameManager] GameConfig chưa được gán!");
        }

        #endregion

        #region New Game

        /// <summary>
        /// Thiết lập game mới cho người chơi lần đầu.
        /// 
        /// Luồng New Game:
        /// 1. LandManager random Zone cho Player.
        /// 2. PlayerDataManager khởi tạo dữ liệu mới (1 Drill Lv1, 1 Bucket Lv1, plot 1 mở khóa).
        /// 3. GridManager khởi tạo grid cho plot đã mở.
        /// 4. MarketManager khởi tạo giá dầu.
        /// 5. BotSimulationManager random bot cho các Zone trống.
        /// </summary>
        private void NewGame()
        {
            isNewGame = true;
            Debug.Log("[GameManager] ===== BẮT ĐẦU NEW GAME =====");

            // 1. Khởi tạo Land (random Zone cho Player)
            int playerZoneID = landManager.InitializeNewGame();

            if (playerZoneID < 0)
            {
                Debug.LogError("[GameManager] Không thể khởi tạo New Game - không có Zone!");
                return;
            }

            // 2. Lấy BuildingData mặc định (Drill Lv1, Bucket Lv1)
            BuildingData defaultDrill = GetDefaultBuildingData(BuildingType.Drill, 1);
            BuildingData defaultBucket = GetDefaultBuildingData(BuildingType.Bucket, 1);

            if (defaultDrill == null || defaultBucket == null)
            {
                Debug.LogError("[GameManager] Không tìm thấy BuildingData mặc định (Drill Lv1 hoặc Bucket Lv1)!");
                return;
            }

            // 3. Khởi tạo PlayerData
            playerDataManager.InitializeNewGame(playerZoneID, defaultDrill, defaultBucket);

            // 4. Khởi tạo Grid cho plot đầu tiên
            gridManager.InitializeGridForPlot(playerZoneID, 1);

            // 5. Khởi tạo Bot Simulation
            botSimulationManager.InitializeNewSession();

            // 6. Bắt đầu Auto Save (nếu được cấu hình)
            saveLoadManager.StartAutoSave();

            Debug.Log($"[GameManager] New Game: Player Zone={playerZoneID}, Drill Lv1 + Bucket Lv1 đã được cấp.");

            EventBus.Publish(new OnGameReady());
        }

        #endregion

        #region Load Game

        /// <summary>
        /// Load game từ file save.
        /// 
        /// Luồng Load Game:
        /// 1. SaveLoadManager đọc file, trả về SaveData.
        /// 2. LandManager khôi phục Zone cho Player và danh sách plot đã mở.
        /// 3. PlayerDataManager khôi phục tiền, dầu, inventory, danh sách công trình.
        /// 4. GridManager khởi tạo grid cho các plot đã mở.
        /// 5. BuildingManager khôi phục GameObject công trình từ danh sách.
        /// 6. MarketManager khôi phục giá dầu.
        /// 7. BotSimulationManager random bot mới (không lưu bot cũ).
        /// </summary>
        private void LoadGame()
        {
            isNewGame = false;
            Debug.Log("[GameManager] ===== BẮT ĐẦU LOAD GAME =====");

            // 1. Load SaveData
            SaveData saveData = saveLoadManager.LoadGame();

            if (saveData == null)
            {
                Debug.LogError("[GameManager] Load game thất bại - chuyển sang New Game.");
                NewGame();
                return;
            }

            // 2. Khởi tạo Land và khôi phục
            landManager.Initialize();
            landManager.ApplySaveData(saveData);

            // 3. Khôi phục PlayerData
            playerDataManager.ApplySaveData(saveData);

            // 4. Khởi tạo Grid cho các plot đã mở
            InitializeGridsFromSaveData(saveData);

            // 5. Khôi phục công trình (spawn GameObject)
            buildingManager.RestoreBuildingsFromSave(saveData.placedBuildings);

            // 6. Khôi phục giá dầu
            marketManager.SetPriceFromSave(saveData.currentOilPrice);

            // 7. Random bot mới
            botSimulationManager.InitializeNewSession();

            // 8. Bắt đầu Auto Save
            saveLoadManager.StartAutoSave();

            EventBus.Publish(new OnGameReady());
        }

        /// <summary>
        /// Khởi tạo grid cho tất cả plot đã mở khóa từ SaveData.
        /// </summary>
        private void InitializeGridsFromSaveData(SaveData saveData)
        {
            if (saveData.unlockedPlots == null) return;

            foreach (var pair in saveData.unlockedPlots)
            {
                // Kiểm tra grid đã được khởi tạo chưa (tránh trùng lặp)
                // GridManager.InitializeGridForPlot sẽ tự kiểm tra
                gridManager.InitializeGridForPlot(pair.zoneID, pair.plotID);
            }

        }

        #endregion

        #region Khởi động Hệ thống Tự động

        /// <summary>
        /// Bắt đầu tất cả hệ thống chạy tự động sau khi game đã sẵn sàng.
        /// </summary>
        private void StartAllSystems()
        {
            // 1. Bắt đầu cập nhật giá dầu
            marketManager.StartPriceUpdate();

            // 2. Bắt đầu sản xuất dầu
            productionManager.StartProduction();

            // 3. Bot simulation đã bắt đầu trong InitializeNewSession()

            Debug.Log("[GameManager] Tất cả hệ thống tự động đã khởi động.");
        }

        #endregion

        #region Khởi tạo UI

        /// <summary>
        /// Khởi tạo giao diện người chơi.
        /// </summary>
        private void InitializeUI()
        {
            if (hudManager != null)
            {
                hudManager.Initialize();
            }
            else
            {
                Debug.LogWarning("[GameManager] HUDManager chưa được gán! UI sẽ không hiển thị.");
            }

        }

        #endregion

        #region Helper

        /// <summary>
        /// Lấy BuildingData mặc định theo loại và cấp.
        /// Dùng để cấp item khởi đầu cho người chơi mới.
        /// </summary>
        /// <param name="type">Loại công trình (Drill/Bucket).</param>
        /// <param name="level">Cấp độ (1).</param>
        /// <returns>BuildingData tương ứng hoặc null.</returns>
        private BuildingData GetDefaultBuildingData(BuildingType type, int level)
        {
            if (gameConfig == null || gameConfig.buildingDatabase == null) return null;
            gameConfig.buildingDatabase.Initialize();

            foreach (var data in gameConfig.buildingDatabase.allBuildings)
            {
                if (data != null && data.buildingType == type && data.level == level)
                    return data;
            }
            return null;
        }

        /// <summary>
        /// Lưu game thủ công (gọi từ UI hoặc phím tắt).
        /// </summary>
        public void ManualSave()
        {
            if (saveLoadManager != null)
            {
                saveLoadManager.SaveGame();
            }
        }

        /// <summary>
        /// Thoát game về menu chính.
        /// </summary>
        public void ReturnToMainMenu()
        {
            // Lưu game trước khi thoát
            if (saveLoadManager != null)
            {
                saveLoadManager.SaveGame();
            }

            // Load scene menu (nếu có)
            // SceneManager.LoadScene("MainMenu");
            Debug.Log("[GameManager] Quay về menu chính.");
        }

        #endregion
    }
}