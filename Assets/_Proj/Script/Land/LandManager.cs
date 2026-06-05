// Assets/_Project/Scripts/Land/LandManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// LandManager - Service quản lý toàn bộ đất đai trong game.
    /// Implement ILandService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Khởi tạo map từ danh sách ZoneData (ScriptableObject).
    /// - Quản lý quyền sở hữu của từng Zone (Player / Bot / Empty).
    /// - Quản lý trạng thái mở khóa của từng Plot trong mỗi Zone.
    /// - Cung cấp API kiểm tra và mở khóa Plot.
    /// - Đồng bộ với SaveData (lưu danh sách plot đã mở, load lại khi load game).
    /// </summary>
    public class LandManager : MonoBehaviour, ILandService
    {
        [Header("Cấu hình")]
        [Tooltip("Danh sách tất cả ZoneData trong game (gán từ Editor).")]
        [SerializeField] private List<ZoneData> allZoneDataList;
        [SerializeField] private ZoneManager zoneManager;

        [Tooltip("GameConfig để lấy cellSize và các tham số khác.")]
        [SerializeField] private GameConfig gameConfig;

        // === Dữ liệu runtime ===

        /// <summary>
        /// Danh sách tất cả Zone đang có trong game (dữ liệu runtime).
        /// Mỗi ZoneRuntime chứa thông tin về quyền sở hữu, trạng thái các plot.
        /// </summary>
        private List<ZoneRuntime> allZones;

        /// <summary>
        /// Dictionary tra cứu nhanh ZoneRuntime theo zoneID.
        /// </summary>
        private Dictionary<int, ZoneRuntime> zoneLookup;

        // Cache tham chiếu PlayerDataManager để kiểm tra plot đã mở
        private IPlayerDataService playerDataService;

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<ILandService>(this);

            // Khởi tạo danh sách zone
            allZones = new List<ZoneRuntime>();
            zoneLookup = new Dictionary<int, ZoneRuntime>();
        }

        private void Start()
        {
            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
            // Lấy PlayerDataService (phải được đăng ký trước)
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            if (playerDataService == null)
            {
                Debug.LogError("[LandManager] IPlayerDataService chưa được đăng ký! Đảm bảo PlayerDataManager chạy trước.");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ILandService>();
        }

        #endregion

        #region Khởi tạo

        /// <summary>
        /// Khởi tạo toàn bộ Zone từ danh sách ZoneData.
        /// Gọi một lần khi bắt đầu game (New Game hoặc Load Game).
        /// </summary>
        public void Initialize()
        {
            allZones.Clear();
            zoneLookup.Clear();

            foreach (ZoneData zoneData in allZoneDataList)
            {
                if (zoneData == null) continue;

                // Khởi tạo cache tra cứu trong ZoneData
                zoneData.Initialize();

                // Tạo ZoneRuntime
                ZoneRuntime zoneRuntime = new ZoneRuntime
                {
                    zoneID = zoneData.zoneID,
                    zoneData = zoneData,
                    owner = ZoneOwner.Empty, // Mặc định chưa có chủ
                    unlockedPlotIDs = new HashSet<int>()
                };

                allZones.Add(zoneRuntime);
                zoneLookup[zoneRuntime.zoneID] = zoneRuntime;
            }

        }

        /// <summary>
        /// Gán Zone cho Player. Chỉ gọi một lần khi New Game.
        /// </summary>
        /// <param name="zoneID">ZoneID được chọn cho Player.</param>
        public void AssignPlayerZone(int zoneID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                zone.owner = ZoneOwner.Player;
            }
        }

        /// <summary>
        /// Khởi tạo dữ liệu cho New Game.
        /// Chọn ngẫu nhiên một Zone cho Player, đánh dấu các Zone còn lại là Empty.
        /// </summary>
        /// <returns>ZoneID được gán cho Player.</returns>
        public int InitializeNewGame()
        {
            Initialize();

            if (allZones.Count == 0)
            {
                Debug.LogError("[LandManager] Không có Zone nào được cấu hình!");
                return -1;
            }

            // Chọn ngẫu nhiên một Zone cho Player (theo index, nhưng lấy zoneID)
            int randomIndex = Random.Range(0, allZones.Count);
            int playerZoneID = allZones[randomIndex].zoneID; // LẤY zoneID, không phải index

            AssignPlayerZone(playerZoneID);

            // Các Zone còn lại giữ nguyên Empty (sẽ được BotSimulationManager gán sau)
            Debug.Log($"[LandManager] New Game: Player được gán Zone {playerZoneID}. Tổng Zone: {allZones.Count}.");

            return playerZoneID;
        }

        #endregion

        #region ILandService Implementation

        /// <summary>
        /// Kiểm tra Zone có thuộc về Player không.
        /// </summary>
        public bool IsPlayerZone(int zoneID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                return zone.owner == ZoneOwner.Player;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra một Plot đã được mở khóa chưa.
        /// Kiểm tra cả trong PlayerData (nếu là zone của Player) và ZoneRuntime.
        /// </summary>
        public bool IsPlotUnlocked(int zoneID, int plotID)
        {
            // Nếu là zone của Player, kiểm tra trong PlayerData
            if (IsPlayerZone(zoneID) && playerDataService != null)
            {
                return playerDataService.IsPlotUnlocked(zoneID, plotID);
            }

            // Nếu là zone của Bot, kiểm tra trong ZoneRuntime
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                return zone.unlockedPlotIDs.Contains(plotID);
            }

            return false;
        }

        /// <summary>
        /// Lấy hệ số nhân dầu của một Plot.
        /// </summary>
        /// <param name="zoneID">Zone chứa Plot.</param>
        /// <param name="plotID">ID của Plot.</param>
        /// <returns>Hệ số nhân (mặc định 1 nếu không tìm thấy).</returns>
        public float GetPlotMultiplier(int zoneID, int plotID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                PlotInfo plot = zone.zoneData.GetPlot(plotID);
                if (plot != null)
                {
                    return plot.oilMultiplier;
                }
            }
            Debug.LogWarning($"[LandManager] Không tìm thấy Plot {plotID} trong Zone {zoneID}. Trả về multiplier mặc định = 1.");
            return 1f;
        }

        /// <summary>
        /// Lấy giá mở khóa của một Plot.
        /// </summary>
        public double GetPlotUnlockCost(int zoneID, int plotID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                PlotInfo plot = zone.zoneData.GetPlot(plotID);
                if (plot != null)
                {
                    return plot.unlockCost;
                }
            }
            Debug.LogWarning($"[LandManager] Không tìm thấy Plot {plotID} trong Zone {zoneID}.");
            return double.MaxValue; // Trả về giá cực lớn nếu không tìm thấy
        }

        /// <summary>
        /// Mở khóa một Plot.
        /// Đối với Player: gọi PlayerDataService.UnlockPlot().
        /// Đối với Bot: cập nhật trực tiếp trong ZoneRuntime.
        /// </summary>
        /// <param name="zoneID">Zone chứa Plot.</param>
        /// <param name="plotID">ID của Plot cần mở.</param>
        /// <returns>True nếu mở khóa thành công.</returns>
        public bool UnlockPlot(int zoneID, int plotID)
        {
            if (!zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                Debug.LogError($"[LandManager] Zone {zoneID} không tồn tại!");
                return false;
            }

            // Kiểm tra Plot có tồn tại trong Zone không
            if (!zone.zoneData.HasPlot(plotID))
            {
                Debug.LogError($"[LandManager] Plot {plotID} không tồn tại trong Zone {zoneID}!");
                return false;
            }

            // Kiểm tra đã mở khóa chưa
            if (IsPlotUnlocked(zoneID, plotID))
            {
                Debug.LogWarning($"[LandManager] Plot {plotID} trong Zone {zoneID} đã mở khóa rồi.");
                return false;
            }

            // Nếu là zone của Player
            if (zone.owner == ZoneOwner.Player && playerDataService != null)
            {
                double cost = GetPlotUnlockCost(zoneID, plotID);
                if (!playerDataService.SubtractMoney(cost, MoneyChangeReason.UnlockLand))
                {
                    Debug.LogWarning($"[LandManager] Player không đủ tiền mở khóa Plot {plotID}. Cần ${cost}.");
                    return false;
                }
                playerDataService.UnlockPlot(zoneID, plotID);
            }

            // Cập nhật ZoneRuntime
            zone.unlockedPlotIDs.Add(plotID);


            // Sự kiện OnLandUnlocked sẽ được phát bởi PlayerDataManager (nếu là Player)
            // hoặc có thể phát ở đây cho bot (nếu cần UI hiển thị)

            return true;
        }

        /// <summary>
        /// Lấy chủ sở hữu của một Zone.
        /// </summary>
        public ZoneOwner GetZoneOwner(int zoneID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                return zone.owner;
            }
            return ZoneOwner.Empty;
        }

        /// <summary>
        /// Tổng số Zone trong game.
        /// </summary>
        public int TotalZones => allZones.Count;

        /// <summary>
        /// Gán chủ sở hữu cho một Zone.
        /// Dùng bởi BotSimulationManager để gán Bot vào Zone.
        /// </summary>
        /// <param name="zoneID">Zone cần gán.</param>
        /// <param name="owner">Chủ sở hữu mới.</param>
        public void SetZoneOwner(int zoneID, ZoneOwner owner)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                // Không cho phép ghi đè Player
                if (zone.owner == ZoneOwner.Player && owner != ZoneOwner.Player)
                {
                    Debug.LogError($"[LandManager] Không thể thay đổi chủ sở hữu Zone {zoneID} vì đang thuộc về Player!");
                    return;
                }
                zone.owner = owner;
            }
        }

        #endregion

        #region Helper

        /// <summary>
        /// Lấy ZoneRuntime theo zoneID.
        /// </summary>
        public ZoneRuntime GetZone(int zoneID)
        {
            zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone);
            return zone;
        }

        /// <summary>
        /// Lấy ZoneData theo zoneID.
        /// </summary>
        public ZoneData GetZoneData(int zoneID)
        {
            return zoneManager.GetZone(zoneID)?.zoneData;
        }

        /// <summary>
        /// Lấy danh sách tất cả ZoneID chưa có chủ (Empty).
        /// Dùng bởi BotSimulationManager.
        /// </summary>
        public List<int> GetEmptyZoneIDs()
        {
            List<int> emptyZones = new List<int>();
            foreach (var zone in allZones)
            {
                if (zone.owner == ZoneOwner.Empty)
                {
                    emptyZones.Add(zone.zoneID);
                }
            }
            return emptyZones;
        }

        /// <summary>
        /// Lấy danh sách tất cả PlotID trong một Zone.
        /// </summary>
        public List<int> GetAllPlotIDs(int zoneID)
        {
            List<int> plotIDs = new List<int>();
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                foreach (var plot in zone.zoneData.plots)
                {
                    if (plot != null)
                        plotIDs.Add(plot.plotID);
                }
            }
            return plotIDs;
        }

        /// <summary>
        /// Lấy PlotInfo của một Plot cụ thể.
        /// </summary>
        public PlotInfo GetPlotInfo(int zoneID, int plotID)
        {
            if (zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone))
            {
                return zone.zoneData.GetPlot(plotID);
            }
            return null;
        }

        /// <summary>
        /// Áp dụng dữ liệu từ SaveData (danh sách plot đã mở của Player).
        /// </summary>
        public void ApplySaveData(SaveData saveData)
        {
            if (saveData == null) return;

            Initialize();

            // Khôi phục zone cho Player
            AssignPlayerZone(saveData.playerZoneID);

            // Mở khóa các plot đã lưu (cập nhật ZoneRuntime)
            if (saveData.unlockedPlots != null)
            {
                foreach (var pair in saveData.unlockedPlots)
                {
                    if (zoneLookup.TryGetValue(pair.zoneID, out ZoneRuntime zone))
                    {
                        zone.unlockedPlotIDs.Add(pair.plotID);
                    }
                }
            }

        }

        /// <summary>
        /// Tạo dữ liệu save từ trạng thái hiện tại (cho Player).
        /// </summary>
        public void PopulateSaveData(SaveData saveData)
        {
            // Việc lưu danh sách plot đã mở được PlayerDataManager đảm nhiệm
            // LandManager chỉ cần lưu playerZoneID (đã có trong saveData)
            // Giữ hàm này để đồng bộ với kiến trúc
        }

        #endregion

    }

    /// <summary>
    /// Dữ liệu runtime của một Zone.
    /// Lưu trạng thái sở hữu và danh sách plot đã mở khóa.
    /// </summary>
    [System.Serializable]
    public class ZoneRuntime
    {
        /// <summary>ID của Zone (khớp với ZoneData.zoneID).</summary>
        public int zoneID;

        /// <summary>Tham chiếu đến ZoneData (ScriptableObject).</summary>
        public ZoneData zoneData;

        public Transform zoneTransform;

        /// <summary>Chủ sở hữu hiện tại của Zone.</summary>
        public ZoneOwner owner;

        /// <summary>Danh sách plotID đã mở khóa trong Zone này.</summary>
        public HashSet<int> unlockedPlotIDs;
    }
}