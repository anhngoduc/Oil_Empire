// Assets/_Project/Scripts/Inventory/InventoryManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// InventoryManager - Service quản lý túi đồ (Inventory) của người chơi.
    /// Implement IInventoryService để các Manager khác truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Quản lý danh sách item (BuildingData ID -> số lượng).
    /// - Thêm/Xóa item, cộng dồn số lượng.
    /// - Phát sự kiện OnInventoryChanged mỗi khi có thay đổi.
    /// - Đồng bộ với PlayerDataManager.
    /// 
    /// Lưu ý: Dữ liệu gốc nằm trong PlayerDataManager.
    /// InventoryManager hoạt động như một lớp trung gian để:
    /// - Tập trung logic kiểm tra (ví dụ: giới hạn slot, điều kiện đặc biệt).
    /// - Phát sự kiện đầy đủ.
    /// </summary>
    public class InventoryManager : MonoBehaviour, IInventoryService
    {
        [Header("Cấu hình")]
        [Tooltip("Số lượng slot tối đa trong inventory. 0 = không giới hạn.")]
        [SerializeField] private int maxSlots = 0;

        [Tooltip("Số lượng tối đa mỗi loại item. 0 = không giới hạn.")]
        [SerializeField] private int maxPerItem = 999;

        // Tham chiếu đến PlayerDataManager
        private IPlayerDataService playerDataService;

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IInventoryService>(this);
        }

        private void Start()
        {
            // Lấy PlayerDataService
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            if (playerDataService == null)
            {
                Debug.LogError("[InventoryManager] IPlayerDataService chưa được đăng ký!");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IInventoryService>();
        }

        #endregion

        #region IInventoryService Implementation

        /// <summary>
        /// Lấy số lượng của một item trong inventory.
        /// </summary>
        /// <param name="buildingID">ID của BuildingData.</param>
        /// <returns>Số lượng hiện có (0 nếu không có).</returns>
        public int GetCount(int buildingID)
        {
            if (playerDataService == null) return 0;
            return playerDataService.GetInventoryCount(buildingID);
        }

        /// <summary>
        /// Thêm item vào inventory.
        /// Tự động cộng dồn nếu đã có item cùng loại.
        /// </summary>
        /// <param name="buildingID">ID của BuildingData cần thêm.</param>
        /// <param name="count">Số lượng thêm (phải > 0).</param>
        public void AddItem(int buildingID, int count)
        {
            if (count <= 0)
            {
                Debug.LogWarning("[InventoryManager] Số lượng thêm phải > 0!");
                return;
            }

            if (playerDataService == null)
            {
                Debug.LogError("[InventoryManager] PlayerDataService chưa sẵn sàng!");
                return;
            }

            // Lấy số lượng hiện tại
            int currentCount = playerDataService.GetInventoryCount(buildingID);
            int newCount = currentCount + count;

            // Kiểm tra giới hạn mỗi loại
            if (maxPerItem > 0 && newCount > maxPerItem)
            {
                Debug.LogWarning($"[InventoryManager] Vượt quá giới hạn {maxPerItem} cho item {buildingID}. Sẽ cắt về {maxPerItem}.");
                newCount = maxPerItem;
                count = maxPerItem - currentCount;
            }

            // Cập nhật vào PlayerData
            playerDataService.SetInventoryItem(buildingID, newCount);

            // Phát sự kiện
            EventBus.Publish(new OnInventoryChanged(buildingID, newCount, count));

            Debug.Log($"[InventoryManager] Thêm {count}x BuildingID={buildingID}. Tổng: {newCount}.");
        }

        /// <summary>
        /// Xóa item khỏi inventory.
        /// </summary>
        /// <param name="buildingID">ID của BuildingData cần xóa.</param>
        /// <param name="count">Số lượng cần xóa (phải > 0).</param>
        /// <returns>True nếu xóa thành công (đủ số lượng).</returns>
        public bool RemoveItem(int buildingID, int count)
        {
            if (count <= 0)
            {
                Debug.LogWarning("[InventoryManager] Số lượng xóa phải > 0!");
                return false;
            }

            if (playerDataService == null)
            {
                Debug.LogError("[InventoryManager] PlayerDataService chưa sẵn sàng!");
                return false;
            }

            // Lấy số lượng hiện tại
            int currentCount = playerDataService.GetInventoryCount(buildingID);

            // Kiểm tra đủ số lượng không
            if (currentCount < count)
            {
                Debug.LogWarning($"[InventoryManager] Không đủ item! Cần {count}, hiện có {currentCount}.");
                return false;
            }

            // Tính số lượng mới
            int newCount = currentCount - count;

            // Cập nhật vào PlayerData
            playerDataService.SetInventoryItem(buildingID, newCount);

            // Phát sự kiện (changeAmount là số âm để biểu thị giảm)
            EventBus.Publish(new OnInventoryChanged(buildingID, newCount, -count));

            Debug.Log($"[InventoryManager] Xóa {count}x BuildingID={buildingID}. Còn lại: {newCount}.");

            return true;
        }

        /// <summary>
        /// Lấy toàn bộ inventory dạng Dictionary.
        /// Trả về bản sao để tránh thay đổi trực tiếp.
        /// </summary>
        public Dictionary<int, int> GetAllItems()
        {
            if (playerDataService == null) return new Dictionary<int, int>();
            return playerDataService.GetInventory();
        }

        #endregion

        #region Helper

        /// <summary>
        /// Kiểm tra inventory có chứa ít nhất một item loại này không.
        /// </summary>
        /// <param name="buildingID">ID cần kiểm tra.</param>
        /// <returns>True nếu có ít nhất 1.</returns>
        public bool HasItem(int buildingID)
        {
            return GetCount(buildingID) > 0;
        }

        /// <summary>
        /// Lấy tổng số loại item khác nhau trong inventory.
        /// </summary>
        public int GetTotalItemTypes()
        {
            if (playerDataService == null) return 0;
            return playerDataService.GetInventory().Count;
        }

        /// <summary>
        /// Xóa toàn bộ inventory (dùng khi cần reset).
        /// </summary>
        public void ClearAll()
        {
            if (playerDataService == null) return;

            var allItems = playerDataService.GetInventory();
            foreach (var kvp in allItems)
            {
                playerDataService.SetInventoryItem(kvp.Key, 0);
                EventBus.Publish(new OnInventoryChanged(kvp.Key, 0, -kvp.Value));
            }

            Debug.Log("[InventoryManager] Đã xóa toàn bộ inventory.");
        }

        #endregion
    }
}