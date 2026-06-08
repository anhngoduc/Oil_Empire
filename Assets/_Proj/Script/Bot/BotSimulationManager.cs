// Assets/_Project/Scripts/Bot/BotSimulationManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// BotSimulationManager - Service giả lập người chơi ảo (Bot).
    /// 
    /// Trách nhiệm:
    /// - Khi New Game hoặc Load Game: random lại toàn bộ bot trên các Zone trống.
    /// - Random số lượng bot, Zone chiếm đóng, mảnh đất mở khóa, công trình sở hữu.
    /// - Mô phỏng khai thác dầu cho bot bằng timer nền (Coroutine).
    /// - Bot tự động "thu dầu" khi bucket đầy và "bán" với giá hiện tại.
    /// - Tùy chọn: bot tự phát triển (mua công trình mới) theo thời gian.
    /// 
    /// NGUYÊN TẮC HIỆU NĂNG:
    /// - KHÔNG spawn GameObject cho bot.
    /// - KHÔNG dùng Update().
    /// - Chạy hoàn toàn bằng dữ liệu (Data Simulation).
    /// - Mỗi tick mô phỏng xử lý tất cả bot trong một vòng lặp.
    /// - Hỗ trợ hàng trăm bot mà không ảnh hưởng FPS.
    /// 
    /// LƯU Ý QUAN TRỌNG:
    /// - Dữ liệu bot KHÔNG được lưu vào SaveData.
    /// - Mỗi lần Load Game, bot được random lại hoàn toàn.
    /// </summary>
    public class BotSimulationManager : MonoBehaviour
    {
        [Header("Cấu hình")]
        [Tooltip("BotSimulationConfig (ScriptableObject) - chứa tỉ lệ bot, cấp công trình...")]
        [SerializeField] private BotSimulationConfig config;

        [Tooltip("BuildingDatabase - để tra cứu BuildingData khi random công trình.")]
        [SerializeField] private BuildingDatabase buildingDatabase;

        [Tooltip("Có bật log chi tiết cho bot không? (tắt khi build release).")]
        [SerializeField] private bool verboseLogging = false;

        [Header("=== Bot Prefab ===")]
        [SerializeField] private GameObject botPrefab;

        [Header("=== Spawn công trình Bot ===")]
        [SerializeField] private Transform botBuildingsParent;

        // === Dữ liệu runtime ===

        /// <summary>
        /// Danh sách tất cả bot đang tồn tại trong phiên chơi hiện tại.
        /// </summary>
        private List<BotData> allBots;

        /// <summary>
        /// Coroutine mô phỏng định kỳ.
        /// </summary>
        private IEnumerator simulationCoroutine;

        /// <summary>
        /// Coroutine phát triển bot (tùy chọn).
        /// </summary>
        private IEnumerator progressionCoroutine;

        // Tham chiếu service
        private ILandService landService;
        private IMarketService marketService;

        // ID tự tăng cho bot
        private int nextBotID;

        #region Unity Lifecycle

        private void Awake()
        {
            allBots = new List<BotData>();
            nextBotID = 1;
        }

        private void Start()
        {
            // Lấy service
            landService = ServiceLocator.Get<ILandService>();
            marketService = ServiceLocator.Get<IMarketService>();

            if (landService == null)
                Debug.LogError("[BotSimulationManager] ILandService chưa được đăng ký!");
            if (marketService == null)
                Debug.LogError("[BotSimulationManager] IMarketService chưa được đăng ký!");
            if (buildingDatabase == null)
                Debug.LogError("[BotSimulationManager] BuildingDatabase chưa được gán!");
            if (config == null)
                Debug.LogError("[BotSimulationManager] BotSimulationConfig chưa được gán!");
        }

        private void OnDestroy()
        {
            StopSimulation();
            StopProgression();
        }

        #endregion

        #region Khởi tạo Phiên Chơi Mới

        /// <summary>
        /// Khởi tạo bot cho phiên chơi mới (New Game hoặc Load Game).
        /// Random lại toàn bộ bot trên các Zone trống.
        /// </summary>
        public void InitializeNewSession()
        {
            // Xóa toàn bộ bot cũ
            ClearAllBots();

            if (landService == null || config == null)
            {
                Debug.LogError("[BotSimulationManager] Không thể khởi tạo session mới - thiếu service hoặc config!");
                return;
            }

            // Lấy danh sách Zone trống (chưa có Player hoặc Bot)
            List<int> emptyZoneIDs = landService.GetEmptyZoneIDs();

            if (emptyZoneIDs.Count == 0)
            {
                Debug.Log("[BotSimulationManager] Không có Zone trống để gán bot.");
                return;
            }

            int botCount = 0;

            // Duyệt qua từng Zone trống, quyết định có gán bot không
            foreach (int zoneID in emptyZoneIDs)
            {
                // Random tỉ lệ xuất hiện bot
                float chance = Random.Range(0f, 100f);
                if (chance <= config.botAppearanceChance)
                {
                    // Tạo bot cho Zone này
                    CreateBotForZone(zoneID);
                    botCount++;
                }
            }


            // Bắt đầu mô phỏng
            StartSimulation();

            // Bắt đầu phát triển bot (nếu bật)
            if (config.enableBotProgression)
            {
                StartProgression();
            }
        }

        /// <summary>
        /// Tạo một bot cho một Zone cụ thể.
        /// </summary>
        /// <param name="zoneID">Zone gán cho bot.</param>
        private void CreateBotForZone(int zoneID)
        {
            Debug.Log($"[BOT] CreateBotForZone: zoneID={zoneID}");
            if (landService == null || config == null || buildingDatabase == null) return;

            // Đánh dấu Zone thuộc về Bot
            landService.SetZoneOwner(zoneID, ZoneOwner.Bot);

            // Tạo dữ liệu bot
            BotData bot = new BotData();
            bot.botID = nextBotID++;
            bot.zoneID = zoneID;
            bot.botName = GenerateBotName(bot.botID);
            bot.money = 0; // Bot bắt đầu với 0 tiền (có thể random sau)

            // === Random số mảnh đất mở khóa ===
            List<int> allPlotIDs = landService.GetAllPlotIDs(zoneID);
            int totalPlots = allPlotIDs.Count;

            // Số mảnh tối đa bot có thể mở
            int maxPlots = config.maxPlotsUnlocked > 0
                ? Mathf.Min(config.maxPlotsUnlocked, totalPlots)
                : totalPlots;

            // Số mảnh tối thiểu
            int minPlots = Mathf.Min(config.minPlotsUnlocked, maxPlots);

            // Random số mảnh bot mở khóa
            int plotsToUnlock = Random.Range(minPlots, maxPlots + 1);

            // Mở khóa các mảnh đầu tiên (theo thứ tự plotID)
            for (int i = 0; i < plotsToUnlock && i < allPlotIDs.Count; i++)
            {
                int plotID = allPlotIDs[i];
                bot.unlockedPlotIDs.Add(plotID);
                landService.UnlockPlot(zoneID, plotID);
            }

            if (verboseLogging)
                Debug.Log($"[BotSimulationManager] Bot {bot.botName}: Mở {plotsToUnlock}/{totalPlots} mảnh.");

            // Khởi tạo grid cho tất cả plot của Bot trước khi random công trình
            IGridService gridService = ServiceLocator.Get<IGridService>();
            if (gridService != null)
            {
                foreach (int plotID in bot.unlockedPlotIDs)
                {
                    // Gọi CanPlace để tự động khởi tạo grid
                    gridService.CanPlace(zoneID, plotID, 0, 0);
                }
            }
            // === Random công trình ===
            RandomizeBotBuildings(bot);

            // Tính toán chỉ số
            bot.RecalculateStats(buildingDatabase);

            // Thêm vào danh sách
            allBots.Add(bot);
            // Spawn Bot GameObject
            if (botPrefab != null)
            {
                GameObject botGO = Instantiate(botPrefab, GetRandomPositionInZone(zoneID), Quaternion.identity);
                BotMovement botMovement = botGO.GetComponent<BotMovement>();
                if (botMovement != null)
                {
                    botMovement.SetZoneID(zoneID);
                    bot.botMovement = botMovement;
                    BotData capturedBot = bot; // ✅ capture để dùng trong lambda
                    botMovement.OnCollectOil += (buildingInfo) =>
                    {
                        BuildingData data = buildingDatabase.GetByID(buildingInfo.buildingDataID);
                        if (data == null) return;

                        float oil = buildingInfo.currentOil;
                        buildingInfo.currentOil = 0f; // ✅ reset dầu

                        float price = marketService?.CurrentOilPrice ?? 0f;
                        capturedBot.money += oil * price;

                        UpdateBotBuildingVisual(capturedBot, buildingInfo);
                    };
                }
            }

            SpawnBotBuildings(bot);
            Debug.Log($"[BOT] Bot {bot.botID}: {bot.buildings.Count} công trình");
        }

        /// <summary>
        /// Random công trình cho bot.
        /// </summary>
        /// <param name="bot">Bot cần random công trình.</param>

        private void RandomizeBotBuildings(BotData bot)
        {
            if (config == null || buildingDatabase == null || landService == null) return;

            bot.buildings.Clear();

            ZoneData zoneData = landService.GetZoneData(bot.zoneID);
            if (zoneData == null) return;

            int cellsX = zoneData.cellsPerPlotX;
            int cellsZ = zoneData.cellsPerPlotZ;

            int totalAvailableCells = bot.unlockedPlotIDs.Count * cellsX * cellsZ;

            int cellsToUse = Mathf.RoundToInt(totalAvailableCells * config.cellUsagePercentage / 100f);
            cellsToUse = Mathf.Max(cellsToUse, config.minDrillCount + config.minBucketCount);

            int drillCount = Random.Range(config.minDrillCount, cellsToUse - config.minBucketCount + 1);
            int bucketCount = cellsToUse - drillCount;

            // DEBUG
            Debug.Log($"[BOT DEBUG] Bot {bot.botID} Zone {bot.zoneID}: plots={bot.unlockedPlotIDs.Count}, cellsX={cellsX}, cellsZ={cellsZ}, totalCells={totalAvailableCells}, cellsToUse={cellsToUse}, drill={drillCount}, bucket={bucketCount}");

            List<GridPosition> availableCells = GetAllAvailableCells(bot);
            Debug.Log($"[BOT DEBUG] Bot {bot.botID}: availableCells={availableCells.Count}");

            ShuffleList(availableCells);

            for (int i = 0; i < drillCount && availableCells.Count > 0; i++)
            {
                GridPosition pos = availableCells[0];
                availableCells.RemoveAt(0);
                int level = config.GetRandomLevel(config.drillLevelWeights);
                BuildingData drillData = GetBuildingDataByTypeAndLevel(BuildingType.Drill, level);
                if (drillData != null)
                {
                    bot.buildings.Add(new BotBuildingInfo
                    {
                        buildingDataID = drillData.buildingID,
                        plotID = pos.plotID,
                        gridX = pos.gridX,
                        gridZ = pos.gridZ,
                        currentOil = 0f
                    });
                }
            }

            for (int i = 0; i < bucketCount && availableCells.Count > 0; i++)
            {
                GridPosition pos = availableCells[0];
                availableCells.RemoveAt(0);
                int level = config.GetRandomLevel(config.bucketLevelWeights);
                BuildingData bucketData = GetBuildingDataByTypeAndLevel(BuildingType.Bucket, level);
                if (bucketData != null)
                {
                    bot.buildings.Add(new BotBuildingInfo
                    {
                        buildingDataID = bucketData.buildingID,
                        plotID = pos.plotID,
                        gridX = pos.gridX,
                        gridZ = pos.gridZ,
                        currentOil = 0f
                    });
                }
            }

            Debug.Log($"[BOT DEBUG] Bot {bot.botID}: KẾT QUẢ drill={bot.buildings.FindAll(b => GetBuildingDataByTypeAndLevel(BuildingType.Drill, 1) != null).Count}/{drillCount}, bucket={bot.buildings.FindAll(b => GetBuildingDataByTypeAndLevel(BuildingType.Bucket, 1) != null).Count}/{bucketCount}");
        }

        /// <summary>
        /// Lấy tất cả vị trí ô trống trong các mảnh bot đã mở.
        /// </summary>
        private List<GridPosition> GetAllAvailableCells(BotData bot)
        {
            List<GridPosition> cells = new List<GridPosition>();
            ZoneData zoneData = landService.GetZoneData(bot.zoneID);
            if (zoneData == null) return cells;

            IGridService gridService = ServiceLocator.Get<IGridService>();
            int cellsX = zoneData.cellsPerPlotX;
            int cellsZ = zoneData.cellsPerPlotZ;

            foreach (int plotID in bot.unlockedPlotIDs)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    for (int z = 0; z < cellsZ; z++)
                    {

                        // Kiểm tra không trùng với công trình hiện có của bot
                        bool occupied = false;
                        foreach (var building in bot.buildings)
                        {
                            if (building.plotID == plotID && building.gridX == x && building.gridZ == z)
                            {
                                occupied = true;
                                break;
                            }
                        }
                        if (!occupied)
                            cells.Add(new GridPosition { plotID = plotID, gridX = x, gridZ = z });
                    }
                }
            }
            return cells;
        }

        /// <summary>
        /// Lấy BuildingData theo loại và cấp độ.
        /// </summary>
        private BuildingData GetBuildingDataByTypeAndLevel(BuildingType type, int level)
        {
            if (buildingDatabase == null) return null;


            foreach (var data in buildingDatabase.allBuildings)
            {
                if (data != null && data.buildingType == type && data.level == level)
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        #region Mô phỏng (Simulation Tick)

        /// <summary>
        /// Bắt đầu Coroutine mô phỏng định kỳ.
        /// </summary>
        private void StartSimulation()
        {
            StopSimulation();

            simulationCoroutine = SimulationRoutine();
            CoroutineRunner.Instance.Run(simulationCoroutine);

        }

        /// <summary>
        /// Dừng Coroutine mô phỏng.
        /// </summary>
        private void StopSimulation()
        {
            if (simulationCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(simulationCoroutine);
                simulationCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine mô phỏng bot.
        /// Mỗi tick: sản xuất dầu, đổ vào bucket, thu dầu nếu bucket đầy.
        /// </summary>
        private IEnumerator SimulationRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(config.simulationTickInterval);

            while (true)
            {
                yield return wait;
                SimulateAllBots();
            }
        }

        /// <summary>
        /// Thực hiện một tick mô phỏng cho tất cả bot.
        /// </summary>
        private void SimulateAllBots()
        {
            if (marketService == null || buildingDatabase == null) return;

            float currentPrice = marketService.CurrentOilPrice;

            foreach (BotData bot in allBots)
            {
                // === Sản xuất dầu ===
                float producedThisTick = bot.totalProductionRate * config.simulationTickInterval;

                // Đổ dầu vào bucket
                float remainingOil = producedThisTick;

                foreach (var building in bot.buildings)
                {
                    BuildingData data = buildingDatabase.GetByID(building.buildingDataID);
                    if (data == null || data.buildingType != BuildingType.Bucket) continue;

                    float capacity = data.capacity;
                    float currentOil = building.currentOil;

                    if (currentOil >= capacity) continue;

                    float spaceAvailable = capacity - currentOil;
                    float oilToFill = Mathf.Min(spaceAvailable, remainingOil);

                    building.currentOil += oilToFill;

                    UpdateBotBuildingVisual(bot, building);

                    remainingOil -= oilToFill;

                    if (remainingOil <= 0f) break;
                }

                // === Khi bucket đầy ===
                foreach (var building in bot.buildings)
                {
                    BuildingData data = buildingDatabase.GetByID(building.buildingDataID);
                    if (data == null || data.buildingType != BuildingType.Bucket) continue;

                    float capacity = data.capacity;
                    if (building.currentOil >= capacity)
                    {
                        // Gọi Bot di chuyển đến Bucket này
                        if (bot.botMovement != null)
                        {
                            Transform bucketTransform = FindBucketTransform(bot.botID, building);
                            if (bucketTransform != null)
                                bot.botMovement.OnBucketFull(bucketTransform, building);
                        }
                    }
                }

                // Cập nhật tổng dầu trong bucket
                bot.totalOilInBuckets = 0f;
                foreach (var building in bot.buildings)
                {
                    BuildingData data = buildingDatabase.GetByID(building.buildingDataID);
                    if (data != null && data.buildingType == BuildingType.Bucket)
                    {
                        bot.totalOilInBuckets += building.currentOil;
                    }

                }

            }
            // === Gửi event ống dầu (gộp tất cả Bot vào 1 event) ===
            var pipeDict = new Dictionary<int, int?>();
            foreach (BotData b in allBots)
            {
                int? activeID = null;
                foreach (var binfo in b.buildings)
                {
                    BuildingData bdata = buildingDatabase.GetByID(binfo.buildingDataID);
                    if (bdata != null && bdata.buildingType == BuildingType.Bucket)
                    {
                        if (binfo.currentOil < bdata.capacity)
                        {
                            activeID = GetBotBucketUniqueID(b, binfo);
                            break;
                        }
                    }
                }
                pipeDict[b.zoneID] = activeID;
            }
            EventBus.Publish(new OnActiveBucketChanged { zoneActiveBuckets = pipeDict });
        }

        #endregion

        #region Phát triển Bot (Tùy chọn)

        /// <summary>
        /// Bắt đầu Coroutine phát triển bot.
        /// </summary>
        private void StartProgression()
        {
            StopProgression();

            progressionCoroutine = ProgressionRoutine();
            CoroutineRunner.Instance.Run(progressionCoroutine);

            Debug.Log($"[BotSimulationManager] Bắt đầu phát triển bot mỗi {config.progressionCheckInterval}s.");
        }

        /// <summary>
        /// Dừng Coroutine phát triển bot.
        /// </summary>
        private void StopProgression()
        {
            if (progressionCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(progressionCoroutine);
                progressionCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine kiểm tra và mua công trình mới cho bot (nếu đủ tiền).
        /// </summary>
        private IEnumerator ProgressionRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(config.progressionCheckInterval);

            while (true)
            {
                yield return wait;
                ProgressAllBots();
            }
        }

        /// <summary>
        /// Kiểm tra tất cả bot và mua công trình nếu đủ điều kiện.
        /// </summary>
        private void ProgressAllBots()
        {
            foreach (BotData bot in allBots)
            {
                TryProgressBot(bot);
            }
        }

        /// <summary>
        /// Thử mua công trình mới cho một bot.
        /// </summary>
        private void TryProgressBot(BotData bot)
        {
            if (buildingDatabase == null) return;

            ZoneData zoneData = landService.GetZoneData(bot.zoneID);
            if (zoneData == null) return;

            int cellsX = zoneData.cellsPerPlotX;
            int cellsZ = zoneData.cellsPerPlotZ;

            List<GridPosition> availableCells = GetAllAvailableCells(bot);
            if (availableCells.Count == 0) return;

            bool buyDrill = Random.value > 0.5f;
            List<BuildingData> affordableItems = new List<BuildingData>();
            BuildingType targetType = buyDrill ? BuildingType.Drill : BuildingType.Bucket;

            foreach (var data in buildingDatabase.allBuildings)
            {
                if (data != null && data.buildingType == targetType && data.price <= bot.money)
                    affordableItems.Add(data);
            }

            if (affordableItems.Count == 0) return;

            BuildingData chosenData = affordableItems[Random.Range(0, affordableItems.Count)];
            bot.money -= chosenData.price;

            GridPosition pos = availableCells[0];
            bot.buildings.Add(new BotBuildingInfo
            {
                buildingDataID = chosenData.buildingID,
                plotID = pos.plotID,
                gridX = pos.gridX,
                gridZ = pos.gridZ,
                currentOil = 0f
            });

            bot.RecalculateStats(buildingDatabase);

            if (verboseLogging)
                Debug.Log($"[BotSimulationManager] Bot {bot.botName}: Mua {chosenData.buildingName} giá ${chosenData.price}. Tiền còn: ${bot.money:F2}.");
        }

        #endregion

        #region Helper

        /// <summary>
        /// Xóa toàn bộ bot và giải phóng Zone.
        /// </summary>
        private void ClearAllBots()
        {
            if (botBuildingsParent != null)
            {
                foreach (Transform child in botBuildingsParent)
                {
                    Destroy(child.gameObject);
                }
            }

            if (landService != null)
            {
                foreach (BotData bot in allBots)
                {
                    // Trả Zone về Empty (nhưng không xóa plot đã mở trong LandManager)
                    // Thực tế bot chỉ mượn Zone, khi reset thì Zone trở về Empty
                    landService.SetZoneOwner(bot.zoneID, ZoneOwner.Empty);
                }
            }

            allBots.Clear();
            nextBotID = 1;

        }

        /// <summary>
        /// Tạo tên bot ngẫu nhiên.
        /// </summary>
        private string GenerateBotName(int id)
        {
            string[] prefixes = { "OilKing", "DrillMaster", "PetroPro", "BlackGold", "Roughneck", "Wildcatter", "Derrick", "Pipeline" };
            string prefix = prefixes[Random.Range(0, prefixes.Length)];
            return $"{prefix}_{id}";
        }

        /// <summary>
        /// Xáo trộn danh sách (Fisher-Yates shuffle).
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Lấy số lượng bot hiện tại (dùng cho debug hoặc UI).
        /// </summary>
        public int GetBotCount()
        {
            return allBots.Count;
        }

        /// <summary>
        /// Lấy danh sách bot (read-only, dùng cho debug).
        /// </summary>
        public List<BotData> GetAllBots()
        {
            return new List<BotData>(allBots);
        }

        #endregion

        /// <summary>
        /// Struct tạm để lưu vị trí grid.
        /// </summary>
        private struct GridPosition
        {
            public int plotID;
            public int gridX;
            public int gridZ;
        }
        private Vector3 GetRandomPositionInZone(int zoneID)
        {
            ZoneManager zoneManager = FindObjectOfType<ZoneManager>();
            Transform t = zoneManager.GetZoneTransform(zoneID);
            ZoneData zd = zoneManager.GetZone(zoneID)?.zoneData;
            if (t == null || zd == null) return Vector3.zero;

            float totalW = zd.TotalCellsX * 1f;
            float totalH = zd.TotalCellsZ * 1f;

            float rx = Random.Range(0.5f, totalW - 0.5f);
            float rz = Random.Range(0.5f, totalH - 0.5f);

            return t.position + t.right * rx + t.forward * rz + Vector3.up * 1f;
        }

        private void SpawnBotBuildings(BotData bot)
        {
            if (buildingDatabase == null) return;

            IGridService gridService = ServiceLocator.Get<IGridService>();
            if (gridService == null) return;

            foreach (var buildingInfo in bot.buildings)
            {
                BuildingData data = buildingDatabase.GetByID(buildingInfo.buildingDataID);
                if (data == null || data.prefab == null) continue;

                Vector3 worldPos = gridService.GridToWorld(bot.zoneID, buildingInfo.plotID, buildingInfo.gridX, buildingInfo.gridZ);

                GameObject buildingGO = Instantiate(data.prefab, worldPos, Quaternion.identity, botBuildingsParent);
                buildingGO.name = $"B{bot.botID}_{buildingInfo.plotID}_{buildingInfo.gridX}_{buildingInfo.gridZ}";

                Building building = buildingGO.GetComponent<Building>();
                if (building != null)
                {
                    int fakeID = -(bot.botID * 10000 + buildingInfo.plotID * 100 + buildingInfo.gridX * 10 + buildingInfo.gridZ);
                    building.Initialize(fakeID, data, bot.zoneID, buildingInfo.plotID, buildingInfo.gridX, buildingInfo.gridZ);

                    BuildingRuntimeData runtimeData = new BuildingRuntimeData
                    {
                        uniqueID = fakeID,
                        buildingDataID = data.buildingID,
                        zoneID = bot.zoneID,
                        plotID = buildingInfo.plotID,
                        gridX = buildingInfo.gridX,
                        gridZ = buildingInfo.gridZ,
                        currentOilInBucket = buildingInfo.currentOil,
                        buildingObject = buildingGO
                    };
                    building.RuntimeData = runtimeData;
                }

                gridService.OccupyCell(bot.zoneID, buildingInfo.plotID, buildingInfo.gridX, buildingInfo.gridZ, 0, data.buildingType);
            }
        }

        private void UpdateBotBuildingVisual(BotData bot, BotBuildingInfo buildingInfo)
        {
            if (botBuildingsParent == null) return;

            string searchName = $"B{bot.botID}_{buildingInfo.plotID}_{buildingInfo.gridX}_{buildingInfo.gridZ}";

            Transform found = botBuildingsParent.Find(searchName);
            if (found != null)
            {
                Building building = found.GetComponent<Building>();
                if (building != null && building.RuntimeData != null)
                {
                    building.RuntimeData.currentOilInBucket = buildingInfo.currentOil;
                    building.SetCurrentOil(buildingInfo.currentOil);
                }
            }
        }

        private Transform FindBucketTransform(int botID, BotBuildingInfo building)
        {
            if (botBuildingsParent == null) return null;
            string searchName = $"B{botID}_{building.plotID}_{building.gridX}_{building.gridZ}";
            return botBuildingsParent.Find(searchName);
        }
        private int? GetBotBucketUniqueID(BotData bot, BotBuildingInfo buildingInfo)
        {
            if (botBuildingsParent == null) return null;
            string searchName = $"B{bot.botID}_{buildingInfo.plotID}_{buildingInfo.gridX}_{buildingInfo.gridZ}";
            Transform found = botBuildingsParent.Find(searchName);
            if (found != null)
            {
                Building building = found.GetComponent<Building>();
                if (building != null) return building.UniqueID;
            }
            return null;
        }
    }
}