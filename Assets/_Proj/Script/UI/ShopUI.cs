// Assets/_Project/Scripts/UI/ShopUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// ShopUI - Quản lý giao diện cửa hàng (Vertical ScrollView).
    /// 
    /// Trách nhiệm:
    /// - Hiển thị danh sách công trình có thể mua (Drill và Bucket).
    /// - Mỗi item hiển thị: icon, tên, giá, chỉ số (tốc độ/dung tích).
    /// - Nút mua: gọi ShopManager.Purchase().
    /// - Cập nhật trạng thái "có thể mua" dựa trên số tiền.
    /// - Hỗ trợ tab Drill và Bucket.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("=== Tham chiếu UI ===")]
        [Tooltip("Transform chứa các item (Content của ScrollView).")]
        [SerializeField] private Transform contentParent;

        [Tooltip("Prefab của một item trong shop.")]
        [SerializeField] private GameObject shopItemPrefab;

        [Header("=== Tab ===")]
        [Tooltip("Button tab Drill.")]
        [SerializeField] private Button drillTabButton;

        [Tooltip("Button tab Bucket.")]
        [SerializeField] private Button bucketTabButton;

        [Tooltip("Button tab Tất cả.")]
        [SerializeField] private Button allTabButton;

        [Header("=== Thông báo ===")]
        [Tooltip("Text hiển thị thông báo (vd: 'Không đủ tiền').")]
        [SerializeField] private TextMeshProUGUI notificationText;

        [Tooltip("Thời gian hiển thị thông báo (giây).")]
        [SerializeField] private float notificationDuration = 2f;

        // === Dữ liệu ===
        private List<ShopItemSlot> activeSlots;
        private BuildingType? currentFilter = null;
        private float notificationTimer;

        // Tham chiếu service
        private IShopService shopService;
        private IPlayerDataService playerDataService;

        #region Khởi tạo

        private void Awake()
        {
            activeSlots = new List<ShopItemSlot>();
        }

        private void Start()
        {
            // Lấy service
            shopService = ServiceLocator.Get<IShopService>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            // Gán sự kiện tab
            if (drillTabButton != null)
                drillTabButton.onClick.AddListener(() => FilterByType(BuildingType.Drill));
            if (bucketTabButton != null)
                bucketTabButton.onClick.AddListener(() => FilterByType(BuildingType.Bucket));
            if (allTabButton != null)
                allTabButton.onClick.AddListener(() => FilterByType(null));

            // Đăng ký sự kiện
            EventBus.Subscribe<OnMoneyChanged>(OnMoneyChangedHandler);

            // Ẩn thông báo ban đầu
            if (notificationText != null)
                notificationText.gameObject.SetActive(false);

            // Hiển thị shop
            RefreshShop();

            Debug.Log("[ShopUI] Đã khởi tạo.");
        }

        private void Update()
        {
            // Tự động ẩn thông báo sau thời gian
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f && notificationText != null)
                {
                    notificationText.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnMoneyChanged>(OnMoneyChangedHandler);
        }

        #endregion

        #region Hiển thị

        /// <summary>
        /// Lọc shop theo loại công trình.
        /// </summary>
        public void FilterByType(BuildingType? type)
        {
            currentFilter = type;
            RefreshShop();
        }

        /// <summary>
        /// Làm mới toàn bộ shop UI.
        /// </summary>
        private void RefreshShop()
        {
            if (shopService == null) return;

            // Xóa tất cả slot cũ
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            activeSlots.Clear();

            // Lấy danh sách item từ ShopService
            List<BuildingData> items = shopService.GetAvailableItems(currentFilter);

            // Tạo slot cho từng item
            foreach (var data in items)
            {
                if (data == null) continue;

                GameObject slotGO = Instantiate(shopItemPrefab, contentParent);
                ShopItemSlot slot = slotGO.GetComponent<ShopItemSlot>();

                if (slot == null)
                {
                    Debug.LogError("[ShopUI] Shop item prefab không có ShopItemSlot!");
                    Destroy(slotGO);
                    continue;
                }

                bool canAfford = playerDataService != null && playerDataService.Money >= data.price;
                slot.Setup(data, canAfford, OnBuyClicked);

                activeSlots.Add(slot);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái "có thể mua" cho tất cả slot khi tiền thay đổi.
        /// </summary>
        private void UpdateAffordability()
        {
            if (playerDataService == null) return;

            double currentMoney = playerDataService.Money;

            foreach (var slot in activeSlots)
            {
                if (slot != null && slot.CurrentData != null)
                {
                    bool canAfford = currentMoney >= slot.CurrentData.price;
                    slot.SetAffordability(canAfford);
                }
            }
        }

        #endregion

        #region Sự kiện

        /// <summary>
        /// Xử lý khi click nút Mua.
        /// </summary>
        private void OnBuyClicked(BuildingData data)
        {
            if (data == null || shopService == null) return;

            PurchaseResult result = shopService.Purchase(data.buildingID);

            switch (result)
            {
                case PurchaseResult.Success:
                    ShowNotification($"Đã mua {data.buildingName}!");
                    // Cập nhật affordability (vì tiền đã thay đổi)
                    UpdateAffordability();
                    break;

                case PurchaseResult.NotEnoughMoney:
                    ShowNotification("Không đủ tiền!");
                    break;

                case PurchaseResult.InventoryFull:
                    ShowNotification("Túi đồ đã đầy!");
                    break;

                default:
                    ShowNotification("Lỗi khi mua hàng!");
                    break;
            }
        }

        /// <summary>
        /// Xử lý khi tiền thay đổi -> cập nhật trạng thái nút mua.
        /// </summary>
        private void OnMoneyChangedHandler(OnMoneyChanged evt)
        {
            UpdateAffordability();
        }

        #endregion

        #region Thông báo

        /// <summary>
        /// Hiển thị thông báo tạm thời.
        /// </summary>
        private void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.gameObject.SetActive(true);
                notificationTimer = notificationDuration;
            }
            Debug.Log($"[ShopUI] {message}");
        }

        #endregion
    }
}

    