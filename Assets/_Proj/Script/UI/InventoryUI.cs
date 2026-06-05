// Assets/_Project/Scripts/UI/InventoryUI.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OilGame
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("=== Tham chiếu UI ===")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("=== Cấu hình ===")]
        [SerializeField] private int maxPoolSize = 20;
        [SerializeField] private BuildingDatabase buildingDatabase;

        private Queue<InventorySlotUI> slotPool;
        private List<InventorySlotUI> activeSlots;

        private IPlayerDataService playerDataService;
        private IBuildingService buildingService;

        private void Awake()
        {
            slotPool = new Queue<InventorySlotUI>();
            activeSlots = new List<InventorySlotUI>();
        }

        private void Start()
        {
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            buildingService = ServiceLocator.Get<IBuildingService>();

            GameConfig config = FindObjectOfType<GameConfig>();
            if (config != null)
                buildingDatabase = config.buildingDatabase;

            EventBus.Subscribe<OnInventoryChanged>(OnInventoryChangedHandler);

            // Đợi 1.5 giây rồi refresh
            Invoke(nameof(RefreshInventory), 1.5f);

            Debug.Log("[InventoryUI] Đã khởi tạo.");
        }

        private void OnEnable()
        {
            if (playerDataService != null)
                RefreshInventory();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnInventoryChanged>(OnInventoryChangedHandler);
        }

        private InventorySlotUI GetSlotFromPool()
        {
            if (slotPool.Count > 0)
            {
                var slot = slotPool.Dequeue();
                slot.gameObject.SetActive(true);
                return slot;
            }
            else
            {
                GameObject go = Instantiate(slotPrefab, contentParent);
                return go.GetComponent<InventorySlotUI>();
            }
        }

        private void ReturnSlotToPool(InventorySlotUI slot)
        {
            if (slot == null) return;
            slot.gameObject.SetActive(false);
            slot.Clear();
            if (slotPool.Count < maxPoolSize)
                slotPool.Enqueue(slot);
            else
                Destroy(slot.gameObject);
        }

        public void RefreshInventory()
        {
            if (playerDataService == null)
            {
                Debug.LogError("[InventoryUI] PlayerDataService NULL!");
                return;
            }

            foreach (var slot in activeSlots)
                ReturnSlotToPool(slot);
            activeSlots.Clear();

            // Đảm bảo database sẵn sàng
            if (buildingDatabase != null)
                buildingDatabase.Initialize();

            Dictionary<int, int> allItems = playerDataService.GetInventory();

            foreach (var kvp in allItems)
            {
                if (kvp.Value <= 0) continue;

                BuildingData data = buildingDatabase != null ? buildingDatabase.GetByID(kvp.Key) : null;

                if (data == null)
                {
                    Debug.LogWarning($"[InventoryUI] Không tìm thấy BuildingData ID={kvp.Key}");
                    continue;
                }

                var slot = GetSlotFromPool();
                if (slot == null) continue;

                slot.Setup(data, kvp.Value, OnSlotClicked);
                activeSlots.Add(slot);
            }

        }

        private void OnSlotClicked(BuildingData data)
        {
            if (data == null) return;
            if (buildingService == null)
            {
                Debug.LogError("[InventoryUI] IBuildingService chưa được đăng ký!");
                return;
            }

            // Nếu ĐANG trong chế độ đặt VÀ đang đặt đúng item này → HỦY
            if (buildingService.IsInPlacementMode && buildingService.CurrentPlacementData == data)
            {
                buildingService.CancelPlacement();
                Debug.Log($"[InventoryUI] Hủy đặt {data.buildingName}.");
                return;
            }

            // Nếu đang đặt item khác → hủy item cũ, đặt item mới
            if (buildingService.IsInPlacementMode)
            {
                buildingService.CancelPlacement();
            }

            // Kiểm tra còn hàng không
            if (playerDataService.GetInventoryCount(data.buildingID) <= 0)
            {
                Debug.LogWarning($"[InventoryUI] Không còn {data.buildingName}!");
                return;
            }

            // Vào chế độ đặt
            buildingService.EnterPlacementMode(data);
            Debug.Log($"[InventoryUI] Chọn {data.buildingName} để đặt.");
        }

        private void OnInventoryChangedHandler(OnInventoryChanged evt)
        {
            RefreshInventory();
        }

        private BuildingData GetBuildingData(int buildingID)
        {
            if (buildingDatabase == null)
            {
                GameConfig config = FindObjectOfType<GameConfig>();
                if (config != null) buildingDatabase = config.buildingDatabase;
            }

            if (buildingDatabase != null)
            {
                buildingDatabase.Initialize(); // THÊM DÒNG NÀY
                return buildingDatabase.GetByID(buildingID);
            }

            return null;
        }
    }
}