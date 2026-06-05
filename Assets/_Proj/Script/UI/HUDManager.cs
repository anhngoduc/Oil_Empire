// Assets/_Project/Scripts/UI/HUDManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro; // Sử dụng TextMeshPro (nếu không có, thay bằng UnityEngine.UI.Text)

namespace OilGame
{
    /// <summary>
    /// HUDManager - Quản lý giao diện chính (Heads-Up Display).
    /// 
    /// Trách nhiệm:
    /// - Hiển thị tiền, dầu đang có của người chơi.
    /// - Hiển thị tổng sản lượng dầu/giây.
    /// - Các nút: Mở Shop, Mở Market, Mở Inventory, Mở Land Unlock, Save.
    /// - Cập nhật real-time thông qua EventBus.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("=== Hiển thị tiền và dầu ===")]
        [Tooltip("Text hiển thị số tiền.")]
        [SerializeField] private TextMeshProUGUI moneyText;

        [Tooltip("Text hiển thị số dầu đang có.")]
        [SerializeField] private TextMeshProUGUI oilText;

        [Tooltip("Text hiển thị tổng sản lượng/giây.")]
        [SerializeField] private TextMeshProUGUI productionRateText;

        [Tooltip("Text hiển thị giá dầu hiện tại.")]
        [SerializeField] private TextMeshProUGUI oilPriceText;

        [Header("=== Panel UI ===")]
        [Tooltip("Panel Inventory UI.")]
        [SerializeField] private GameObject inventoryPanel;

        [Tooltip("Panel Shop UI.")]
        [SerializeField] private GameObject shopPanel;

        [Tooltip("Panel Market UI.")]
        [SerializeField] private GameObject marketPanel;

        [Tooltip("Panel Land Unlock UI.")]
        [SerializeField] private GameObject landUnlockPanel;

        [Header("=== Nút chức năng ===")]
        [Tooltip("Nút đặt công trình")]
        [SerializeField] private Button placeButton;

        [Tooltip("Nút mở/tắt Inventory.")]
        [SerializeField] private Button inventoryButton;

        [Tooltip("Nút mở/tắt Shop.")]
        [SerializeField] private Button shopButton;

        [Tooltip("Nút mở/tắt Market.")]
        [SerializeField] private Button marketButton;

        [Tooltip("Nút mở/tắt Land Unlock.")]
        [SerializeField] private Button landUnlockButton;

        [Tooltip("Nút Save thủ công.")]
        [SerializeField] private Button saveButton;

        [Header("=== Nút đóng Panel ===")]
        [SerializeField] private Button closeInventoryButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Button closeMarketButton;
        [SerializeField] private Button closeLandUnlockButton;

        // Tham chiếu service
        private IPlayerDataService playerDataService;
        private IMarketService marketService;
        private IProductionService productionService;
        private IBuildingService buildingService;

        // Tham chiếu GameManager
        private GameManager gameManager;

        #region Khởi tạo

        /// <summary>
        /// Khởi tạo HUD. Được gọi bởi GameManager sau khi game sẵn sàng.
        /// </summary>
        public void Initialize()
        {
            // Lấy service
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            marketService = ServiceLocator.Get<IMarketService>();
            productionService = ServiceLocator.Get<IProductionService>();

            gameManager = FindObjectOfType<GameManager>();

            // Đăng ký sự kiện
            EventBus.Subscribe<OnMoneyChanged>(OnMoneyChangedHandler);
            EventBus.Subscribe<OnOilChanged>(OnOilChangedHandler);
            EventBus.Subscribe<OnOilProductionUpdated>(OnProductionUpdatedHandler);
            EventBus.Subscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Subscribe<OnPlacementStarted>(OnPlacementStart);
            EventBus.Subscribe<OnPlacementEnded>(OnPlacementEnd);

            // Gán sự kiện cho nút
            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(ToggleInventory);
            if (shopButton != null)
                shopButton.onClick.AddListener(ToggleShop);
            if (marketButton != null)
                marketButton.onClick.AddListener(ToggleMarket);
            if (landUnlockButton != null)
                landUnlockButton.onClick.AddListener(ToggleLandUnlock);
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);

            if (closeInventoryButton != null)
                closeInventoryButton.onClick.AddListener(() => { if (inventoryPanel != null) inventoryPanel.SetActive(false); });
            if (closeShopButton != null)
                closeShopButton.onClick.AddListener(() => { if (shopPanel != null) shopPanel.SetActive(false); });
            if (closeMarketButton != null)
                closeMarketButton.onClick.AddListener(() => { if (marketPanel != null) marketPanel.SetActive(false); });
            if (closeLandUnlockButton != null)
                closeLandUnlockButton.onClick.AddListener(() => { if (landUnlockPanel != null) landUnlockPanel.SetActive(false); });

            if (placeButton != null)
            {
                placeButton.gameObject.SetActive(false);
                placeButton.onClick.AddListener(OnPlaceClicked);
            }

            // Cập nhật giá trị ban đầu
            UpdateMoneyDisplay(playerDataService?.Money ?? 0);
            UpdateOilDisplay(playerDataService?.OilHeld ?? 0);
            UpdateOilPriceDisplay(marketService?.CurrentOilPrice ?? 0f);

            // Mặc định ẩn tất cả panel
            CloseAllPanels();

            Debug.Log("[HUDManager] Đã khởi tạo.");
        }

        private void OnDestroy()
        {
            // Hủy đăng ký sự kiện
            EventBus.Unsubscribe<OnMoneyChanged>(OnMoneyChangedHandler);
            EventBus.Unsubscribe<OnOilChanged>(OnOilChangedHandler);
            EventBus.Unsubscribe<OnOilProductionUpdated>(OnProductionUpdatedHandler);
            EventBus.Unsubscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Unsubscribe<OnPlacementStarted>(OnPlacementStart);
            EventBus.Unsubscribe<OnPlacementEnded>(OnPlacementEnd);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Xử lý khi tiền thay đổi.
        /// </summary>
        private void OnMoneyChangedHandler(OnMoneyChanged evt)
        {
            UpdateMoneyDisplay(evt.newAmount);
        }

        /// <summary>
        /// Xử lý khi dầu thay đổi.
        /// </summary>
        private void OnOilChangedHandler(OnOilChanged evt)
        {
            UpdateOilDisplay(evt.newAmount);
        }

        /// <summary>
        /// Xử lý khi có cập nhật sản xuất.
        /// </summary>
        private void OnProductionUpdatedHandler(OnOilProductionUpdated evt)
        {
            UpdateProductionRateDisplay(evt.currentTotalProductionRate);
        }

        /// <summary>
        /// Xử lý khi giá dầu thay đổi.
        /// </summary>
        private void OnOilPriceChangedHandler(OnOilPriceChanged evt)
        {
            UpdateOilPriceDisplay(evt.newPrice);
        }

        private void OnPlacementStart(OnPlacementStarted e)
        {
            if (placeButton != null) placeButton.gameObject.SetActive(true);
        }

        private void OnPlacementEnd(OnPlacementEnded e)
        {
            if (placeButton != null) placeButton.gameObject.SetActive(false);
        }

       
          
        #endregion

        #region Cập nhật Hiển thị

        /// <summary>
        /// Cập nhật text tiền.
        /// </summary>
        private void UpdateMoneyDisplay(double amount)
        {
            if (moneyText != null)
            {
                moneyText.text = $"${amount:N0}";
            }
        }

        /// <summary>
        /// Cập nhật text dầu.
        /// </summary>
        private void UpdateOilDisplay(double amount)
        {
            if (oilText != null)
            {
                oilText.text = $"Oil: {amount:N0}";
            }
        }

        /// <summary>
        /// Cập nhật text sản lượng.
        /// </summary>
        private void UpdateProductionRateDisplay(float rate)
        {
            if (productionRateText != null)
            {
                productionRateText.text = $"{rate:F1} Oil/sec";
            }
        }

        /// <summary>
        /// Cập nhật text giá dầu.
        /// </summary>
        private void UpdateOilPriceDisplay(float price)
        {
            if (oilPriceText != null)
            {
                oilPriceText.text = $"Price: ${price:F2}/Oil";
            }
        }

        #endregion

        #region Nút chức năng

        /// <summary>
        /// Đóng tất cả panel.
        /// </summary>
        private void CloseAllPanels()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (marketPanel != null) marketPanel.SetActive(false);
            if (landUnlockPanel != null) landUnlockPanel.SetActive(false);
        }

        private void ToggleInventory()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }

        private void ToggleShop()
        {
            if (shopPanel != null)
                shopPanel.SetActive(!shopPanel.activeSelf);
        }

        private void ToggleMarket()
        {
            if (marketPanel != null)
                marketPanel.SetActive(!marketPanel.activeSelf);
        }

        private void ToggleLandUnlock()
        {
            if (landUnlockPanel != null)
                landUnlockPanel.SetActive(!landUnlockPanel.activeSelf);
        }

        private void OnSaveClicked()
        {
            if (gameManager != null)
            {
                gameManager.ManualSave();
                Debug.Log("[HUDManager] Manual Save được gọi.");
            }
        }

        private void OnPlaceClicked()
        {
            Debug.Log("[HUDManager] NÚT ĐẶT ĐƯỢC BẤM!");

            IBuildingService bs = ServiceLocator.Get<IBuildingService>();
            Debug.Log($"[HUDManager] buildingService = {bs}");

            if (bs != null)
            {
                Debug.Log($"[HUDManager] IsInPlacementMode = {bs.IsInPlacementMode}");
                bs.TryPlaceFromUI();
            }
            else
            {
                Debug.LogError("[HUDManager] buildingService NULL!");
            }
        }

        #endregion
    }
}