// Assets/_Project/Scripts/Shop/ShopManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// ShopManager - Service quản lý cửa hàng mua công trình.
    /// Implement IShopService để UI và các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Cung cấp danh sách item có thể mua (từ BuildingDatabase).
    /// - Xử lý giao dịch mua: kiểm tra tiền, trừ tiền, thêm vào inventory.
    /// - Trả về PurchaseResult để UI hiển thị kết quả phù hợp.
    /// 
    /// Luồng mua hàng:
    /// 1. Người chơi chọn item trong ShopUI.
    /// 2. ShopUI gọi ShopManager.Purchase(buildingID).
    /// 3. ShopManager kiểm tra tiền, gọi PlayerDataManager.SubtractMoney().
    /// 4. Nếu đủ tiền, gọi InventoryManager.AddItem().
    /// 5. Trả về PurchaseResult.
    /// </summary>
    public class ShopManager : MonoBehaviour, IShopService
    {
        [Header("Tham chiếu")]
        [Tooltip("BuildingDatabase chứa tất cả item có thể bán.")]
        [SerializeField] private BuildingDatabase buildingDatabase;

        // Tham chiếu đến các service
        private IPlayerDataService playerDataService;
        private IInventoryService inventoryService;

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IShopService>(this);
        }

        private void Start()
        {
            // Lấy các service
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            inventoryService = ServiceLocator.Get<IInventoryService>();

            if (playerDataService == null)
                Debug.LogError("[ShopManager] IPlayerDataService chưa được đăng ký!");
            if (inventoryService == null)
                Debug.LogError("[ShopManager] IInventoryService chưa được đăng ký!");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IShopService>();
        }

        #endregion

        #region IShopService Implementation

        /// <summary>
        /// Lấy danh sách item có thể mua.
        /// Có thể lọc theo loại (Drill/Bucket) hoặc lấy tất cả.
        /// </summary>
        /// <param name="filterType">Lọc theo BuildingType. null = lấy tất cả.</param>
        /// <returns>Danh sách BuildingData có thể mua.</returns>
        public List<BuildingData> GetAvailableItems(BuildingType? filterType = null)
        {
            if (buildingDatabase == null)
            {
                Debug.LogError("[ShopManager] BuildingDatabase chưa được gán!");
                return new List<BuildingData>();
            }

            // Nếu không lọc, trả về tất cả
            if (!filterType.HasValue)
            {
                return new List<BuildingData>(buildingDatabase.allBuildings);
            }

            // Lọc theo loại
            return buildingDatabase.GetByType(filterType.Value);
        }

        /// <summary>
        /// Xử lý mua một item.
        /// </summary>
        /// <param name="buildingID">ID của BuildingData muốn mua.</param>
        /// <returns>PurchaseResult cho biết kết quả giao dịch.</returns>
        public PurchaseResult Purchase(int buildingID)
        {
            // 1. Tìm BuildingData
            BuildingData data = buildingDatabase.GetByID(buildingID);
            if (data == null)
            {
                Debug.LogError($"[ShopManager] Không tìm thấy BuildingData ID={buildingID}!");
                return PurchaseResult.UnknownError;
            }

            // 2. Kiểm tra tiền
            if (playerDataService == null)
            {
                Debug.LogError("[ShopManager] PlayerDataService chưa sẵn sàng!");
                return PurchaseResult.UnknownError;
            }

            if (playerDataService.Money < data.price)
            {
                Debug.Log($"[ShopManager] Không đủ tiền mua {data.buildingName}. Cần ${data.price}, hiện có ${playerDataService.Money}.");
                return PurchaseResult.NotEnoughMoney;
            }

            // 3. Kiểm tra inventory (nếu có giới hạn)
            // (Hiện tại InventoryManager tự xử lý giới hạn, nên không cần kiểm tra ở đây)

            // 4. Trừ tiền
            bool subtractSuccess = playerDataService.SubtractMoney(data.price, MoneyChangeReason.Purchase);
            if (!subtractSuccess)
            {
                Debug.LogError($"[ShopManager] Trừ tiền thất bại cho {data.buildingName}!");
                return PurchaseResult.UnknownError;
            }

            // 5. Thêm vào inventory
            if (inventoryService != null)
            {
                inventoryService.AddItem(buildingID, 1);
            }
            else
            {
                // Fallback: cập nhật trực tiếp PlayerData
                int currentCount = playerDataService.GetInventoryCount(buildingID);
                playerDataService.SetInventoryItem(buildingID, currentCount + 1);
                EventBus.Publish(new OnInventoryChanged(buildingID, currentCount + 1, 1));
            }

            Debug.Log($"[ShopManager] Mua thành công: {data.buildingName} - Giá: ${data.price}.");

            return PurchaseResult.Success;
        }

        #endregion

        #region Helper

        /// <summary>
        /// Kiểm tra người chơi có đủ tiền mua item không.
        /// Dùng để UI hiển thị trạng thái (ví dụ: làm mờ item không thể mua).
        /// </summary>
        /// <param name="buildingID">ID cần kiểm tra.</param>
        /// <returns>True nếu đủ tiền.</returns>
        public bool CanAfford(int buildingID)
        {
            BuildingData data = buildingDatabase.GetByID(buildingID);
            if (data == null || playerDataService == null) return false;
            return playerDataService.Money >= data.price;
        }

        /// <summary>
        /// Lấy giá của một item.
        /// </summary>
        /// <param name="buildingID">ID cần lấy giá.</param>
        /// <returns>Giá (0 nếu không tìm thấy).</returns>
        public double GetPrice(int buildingID)
        {
            BuildingData data = buildingDatabase.GetByID(buildingID);
            return data != null ? data.price : 0;
        }

        #endregion
    }
}