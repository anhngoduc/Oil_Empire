// Assets/_Project/Scripts/Player/PlayerDataManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// PlayerDataManager - Service quản lý toàn bộ dữ liệu runtime của người chơi.
    /// Implement IPlayerDataService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Lưu trữ và quản lý PlayerData (tiền, dầu, inventory, công trình, đất đai).
    /// - Cung cấp API an toàn để thay đổi dữ liệu (có kiểm tra điều kiện).
    /// - Phát sự kiện qua EventBus mỗi khi dữ liệu thay đổi.
    /// - Chuyển đổi giữa PlayerData (runtime) và SaveData (lưu file).
    /// </summary>
    public class PlayerDataManager : MonoBehaviour, IPlayerDataService
    {
        [Header("Tham chiếu")]
        [Tooltip("Tham chiếu đến BuildingDatabase (có thể lấy từ ServiceLocator hoặc gán trực tiếp).")]
        [SerializeField] private BuildingDatabase buildingDatabase;

        // Dữ liệu runtime của người chơi
        private PlayerData playerData;

        // Cache để truy xuất nhanh (tránh boxing/unboxing khi dùng ServiceLocator)
        public PlayerData CurrentData => playerData;

        #region IPlayerDataService Properties

        /// <summary>Số tiền hiện tại của người chơi.</summary>
        public double Money => playerData.money;

        /// <summary>Lượng dầu đang giữ (đã thu từ bucket, chưa bán).</summary>
        public double OilHeld => playerData.oilHeld;

        /// <summary>ZoneID mà người chơi sở hữu.</summary>
        public int PlayerZoneID => playerData.playerZoneID;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // KHỞI TẠO playerData TRƯỚC để tránh NullReferenceException
            playerData = new PlayerData();

            // Đăng ký service
            ServiceLocator.Register<IPlayerDataService>(this);
        }

        private void OnDestroy()
        {
            // Hủy đăng ký khi object bị hủy
            ServiceLocator.Unregister<IPlayerDataService>();
        }

        #endregion

        #region Khởi tạo / Load / Save

        /// <summary>
        /// Khởi tạo dữ liệu cho New Game.
        /// Gán zone ngẫu nhiên cho Player, mở khóa plot đầu tiên, cấp item khởi đầu.
        /// </summary>
        /// <param name="zoneID">ZoneID được gán cho Player.</param>
        /// <param name="drillData">BuildingData của Drill Lv1.</param>
        /// <param name="bucketData">BuildingData của Bucket Lv1.</param>
        public void InitializeNewGame(int zoneID, BuildingData drillData, BuildingData bucketData)
        {
            playerData = new PlayerData();
            playerData.playerZoneID = zoneID;

            // Mở khóa plot đầu tiên (plotID = 1) của zone
            playerData.UnlockPlot(zoneID, 1);

            // Cấp 1 Drill Lv1 và 1 Bucket Lv1 vào inventory
            if (drillData != null)
            {
                playerData.AddToInventory(drillData.buildingID, 1);
            }
            if (bucketData != null)
            {
                playerData.AddToInventory(bucketData.buildingID, 1);
            }


            // Phát sự kiện khởi tạo ban đầu
            PublishAllInitialEvents();
        }

        /// <summary>
        /// Áp dụng dữ liệu từ SaveData (khi Load Game).
        /// Khôi phục toàn bộ trạng thái người chơi từ file save.
        /// </summary>
        /// <param name="saveData">Dữ liệu save đã deserialize.</param>
        public void ApplySaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("[PlayerDataManager] SaveData null, không thể load!");
                return;
            }

            playerData = new PlayerData();

            // Khôi phục tiền và dầu
            playerData.money = saveData.money;
            playerData.oilHeld = saveData.oilHeld;

            // Khôi phục zone
            playerData.playerZoneID = saveData.playerZoneID;

            // Khôi phục danh sách mảnh đất đã mở
            if (saveData.unlockedPlots != null)
            {
                foreach (var pair in saveData.unlockedPlots)
                {
                    playerData.UnlockPlot(pair.zoneID, pair.plotID);
                }
            }

            // Khôi phục inventory
            if (saveData.inventoryItems != null)
            {
                foreach (var item in saveData.inventoryItems)
                {
                    playerData.inventory[item.buildingID] = item.quantity;
                }
            }

            // Khôi phục danh sách công trình đã đặt
            if (saveData.placedBuildings != null)
            {
                // Tìm nextBuildingID lớn nhất để tránh trùng lặp
                int maxID = 0;
                foreach (var saveBuilding in saveData.placedBuildings)
                {
                    BuildingRuntimeData runtimeData = BuildingRuntimeData.FromSaveData(saveBuilding);
                    playerData.placedBuildings.Add(runtimeData);
                    if (saveBuilding.uniqueBuildingID > maxID)
                    {
                        maxID = saveBuilding.uniqueBuildingID;
                    }
                }
                playerData.nextBuildingID = maxID + 1;
            }


            // Phát tất cả sự kiện ban đầu để UI cập nhật
            PublishAllInitialEvents();
        }

        /// <summary>
        /// Tạo SaveData từ PlayerData hiện tại.
        /// Gọi bởi SaveLoadManager khi lưu game.
        /// </summary>
        /// <returns>SaveData chứa toàn bộ dữ liệu cần lưu.</returns>
        public SaveData CreateSaveData()
        {
            SaveData saveData = new SaveData();

            // Lưu tiền và dầu
            saveData.money = playerData.money;
            saveData.oilHeld = playerData.oilHeld;

            // Lưu zone
            saveData.playerZoneID = playerData.playerZoneID;

            // Lưu danh sách mảnh đất đã mở
            saveData.unlockedPlots = new List<ZonePlotPair>();
            foreach (var kvp in playerData.unlockedPlots)
            {
                foreach (int plotID in kvp.Value)
                {
                    saveData.unlockedPlots.Add(new ZonePlotPair(kvp.Key, plotID));
                }
            }

            // Lưu inventory
            saveData.inventoryItems = new List<InventorySaveItem>();
            foreach (var kvp in playerData.inventory)
            {
                if (kvp.Value > 0)
                {
                    saveData.inventoryItems.Add(new InventorySaveItem(kvp.Key, kvp.Value));
                }
            }

            // Lưu công trình đã đặt
            saveData.placedBuildings = new List<PlacedBuildingSaveData>();
            foreach (var building in playerData.placedBuildings)
            {
                saveData.placedBuildings.Add(building.ToSaveData());
            }

            saveData.UpdateTimestamp();
            Debug.Log($"[PlayerDataManager] Tạo SaveData: Money={saveData.money}, Inventory Items={saveData.inventoryItems.Count}, Buildings={saveData.placedBuildings.Count}.");

            return saveData;
        }

        /// <summary>
        /// Phát tất cả sự kiện khởi tạo để UI và các hệ thống khác đồng bộ.
        /// Gọi sau khi New Game hoặc Load Game.
        /// </summary>
        private void PublishAllInitialEvents()
        {
            // Phát sự kiện tiền (từ 0 đến hiện tại)
            EventBus.Publish(new OnMoneyChanged(0, playerData.money, MoneyChangeReason.Debug));

            // Phát sự kiện dầu
            EventBus.Publish(new OnOilChanged(0, playerData.oilHeld, OilChangeReason.Debug));

            // Phát sự kiện inventory cho từng item
            foreach (var kvp in playerData.inventory)
            {
                EventBus.Publish(new OnInventoryChanged(kvp.Key, kvp.Value, kvp.Value));
            }

            // Phát sự kiện mở khóa đất
            foreach (var kvp in playerData.unlockedPlots)
            {
                foreach (int plotID in kvp.Value)
                {
                    EventBus.Publish(new OnLandUnlocked(kvp.Key, plotID, 0));
                }
            }
        }

        #endregion

        #region Tiền (Money)

        /// <summary>
        /// Thêm tiền cho người chơi.
        /// </summary>
        /// <param name="amount">Số tiền thêm (phải > 0).</param>
        /// <param name="reason">Lý do (dùng để log hoặc UI).</param>
        public void AddMoney(double amount, MoneyChangeReason reason)
        {
            if (amount <= 0) return;

            double oldAmount = playerData.money;
            playerData.money += amount;

            Debug.Log($"[PlayerDataManager] +${amount} (reason: {reason}). Tiền mới: ${playerData.money}");

            EventBus.Publish(new OnMoneyChanged(oldAmount, playerData.money, reason));
        }

        /// <summary>
        /// Trừ tiền người chơi. Kiểm tra đủ tiền trước khi trừ.
        /// </summary>
        /// <param name="amount">Số tiền cần trừ.</param>
        /// <param name="reason">Lý do trừ tiền.</param>
        /// <returns>True nếu trừ thành công, False nếu không đủ tiền.</returns>
        public bool SubtractMoney(double amount, MoneyChangeReason reason)
        {
            if (amount <= 0) return true;
            if (playerData.money < amount)
            {
                Debug.LogWarning($"[PlayerDataManager] Không đủ tiền! Cần ${amount}, hiện có ${playerData.money}");
                return false;
            }

            double oldAmount = playerData.money;
            playerData.money -= amount;

            Debug.Log($"[PlayerDataManager] -${amount} (reason: {reason}). Tiền còn: ${playerData.money}");

            EventBus.Publish(new OnMoneyChanged(oldAmount, playerData.money, reason));
            return true;
        }

        #endregion

        #region Dầu (Oil)

        /// <summary>
        /// Thêm dầu vào kho người chơi (sau khi thu từ bucket).
        /// </summary>
        /// <param name="amount">Lượng dầu thêm (phải > 0).</param>
        /// <param name="reason">Lý do.</param>
        public void AddOil(double amount, OilChangeReason reason)
        {
            if (amount <= 0) return;

            double oldAmount = playerData.oilHeld;
            playerData.oilHeld += amount;

            Debug.Log($"[PlayerDataManager] +{amount} Oil (reason: {reason}). Tổng dầu: {playerData.oilHeld}");

            EventBus.Publish(new OnOilChanged(oldAmount, playerData.oilHeld, reason));
        }

        /// <summary>
        /// Trừ dầu khỏi kho người chơi (khi bán).
        /// </summary>
        /// <param name="amount">Lượng dầu cần trừ.</param>
        /// <param name="reason">Lý do.</param>
        /// <returns>True nếu đủ dầu để trừ.</returns>
        public bool SubtractOil(double amount, OilChangeReason reason)
        {
            if (amount <= 0) return true;
            if (playerData.oilHeld < amount)
            {
                Debug.LogWarning($"[PlayerDataManager] Không đủ dầu! Cần {amount}, hiện có {playerData.oilHeld}");
                return false;
            }

            double oldAmount = playerData.oilHeld;
            playerData.oilHeld -= amount;

            Debug.Log($"[PlayerDataManager] -{amount} Oil (reason: {reason}). Dầu còn: {playerData.oilHeld}");

            EventBus.Publish(new OnOilChanged(oldAmount, playerData.oilHeld, reason));
            return true;
        }

        #endregion

        #region Đất đai

        /// <summary>
        /// Kiểm tra một mảnh đất đã được mở khóa chưa.
        /// </summary>
        public bool IsPlotUnlocked(int zoneID, int plotID)
        {
            return playerData.IsPlotUnlocked(zoneID, plotID);
        }

        /// <summary>
        /// Mở khóa một mảnh đất (đã kiểm tra điều kiện trước khi gọi).
        /// </summary>
        /// <param name="zoneID">Zone chứa mảnh.</param>
        /// <param name="plotID">ID mảnh cần mở.</param>
        public void UnlockPlot(int zoneID, int plotID)
        {
            if (playerData.IsPlotUnlocked(zoneID, plotID))
            {
                Debug.LogWarning($"[PlayerDataManager] Plot {plotID} trong Zone {zoneID} đã mở khóa rồi!");
                return;
            }

            playerData.UnlockPlot(zoneID, plotID);

            Debug.Log($"[PlayerDataManager] Đã mở khóa Zone {zoneID} - Plot {plotID}.");

            EventBus.Publish(new OnLandUnlocked(zoneID, plotID, 0));
        }

        #endregion

        #region Công trình (Buildings)

        /// <summary>
        /// Lấy danh sách tất cả công trình Player đã đặt.
        /// </summary>
        public List<BuildingRuntimeData> GetAllBuildings()
        {
            return playerData.placedBuildings;
        }

        /// <summary>
        /// Thêm một công trình vào danh sách.
        /// </summary>
        /// <param name="building">Dữ liệu công trình đã được tạo.</param>
        public void AddBuilding(BuildingRuntimeData building)
        {
            if (building == null) return;
            playerData.AddBuilding(building);
            Debug.Log($"[PlayerDataManager] Thêm building ID={building.uniqueID} tại ({building.gridX},{building.gridZ}).");
        }

        /// <summary>
        /// Xóa một công trình khỏi danh sách.
        /// </summary>
        /// <param name="uniqueID">ID duy nhất của công trình.</param>
        public void RemoveBuilding(int uniqueID)
        {
            if (playerData.RemoveBuilding(uniqueID))
            {
                Debug.Log($"[PlayerDataManager] Xóa building ID={uniqueID}.");
            }
            else
            {
                Debug.LogWarning($"[PlayerDataManager] Không tìm thấy building ID={uniqueID} để xóa.");
            }
        }

        /// <summary>
        /// Cập nhật lượng dầu trong bucket.
        /// </summary>
        /// <param name="uniqueID">ID duy nhất của bucket.</param>
        /// <param name="newAmount">Lượng dầu mới.</param>
        public void UpdateBucketOil(int uniqueID, float newAmount)
        {
            BuildingRuntimeData building = playerData.FindBuilding(uniqueID);
            if (building != null)
            {
                building.currentOilInBucket = newAmount;
            }
        }

        /// <summary>
        /// Cấp ID duy nhất mới cho công trình.
        /// </summary>
        public int GetNextBuildingID()
        {
            return playerData.GetNextBuildingID();
        }

        #endregion

        #region Inventory

        /// <summary>
        /// Lấy toàn bộ inventory dạng Dictionary.
        /// </summary>
        public Dictionary<int, int> GetInventory()
        {
            return new Dictionary<int, int>(playerData.inventory); // Trả về bản sao để tránh thay đổi trực tiếp
        }

        /// <summary>
        /// Lấy số lượng của một item trong inventory.
        /// </summary>
        public int GetInventoryCount(int buildingID)
        {
            return playerData.GetInventoryCount(buildingID);
        }

        /// <summary>
        /// Đặt số lượng cho một item (dùng khi load save).
        /// </summary>
        public void SetInventoryItem(int buildingID, int count)
        {
            if (count > 0)
            {
                playerData.inventory[buildingID] = count;
            }
            else
            {
                playerData.inventory.Remove(buildingID);
            }
        }

        #endregion
    }
}