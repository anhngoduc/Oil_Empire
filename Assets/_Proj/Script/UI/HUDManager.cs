// Assets/_Project/Scripts/UI/HUDManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    public class HUDManager : MonoBehaviour
    {
        [Header("=== Hiển thị ===")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI oilText;
        [SerializeField] private TextMeshProUGUI productionRateText;

        [Header("=== Panel UI ===")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject marketPanel;

        [Header("=== Nút đặt công trình ===")]
        [SerializeField] private Button placeButton;

        [Header("=== Nút đóng Panel ===")]
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Button closeMarketButton;

        private IPlayerDataService playerDataService;
        private IMarketService marketService;
        private IProductionService productionService;
        private IBuildingService buildingService;
        private GameManager gameManager;

        public void Initialize()
        {
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            marketService = ServiceLocator.Get<IMarketService>();
            productionService = ServiceLocator.Get<IProductionService>();
            buildingService = ServiceLocator.Get<IBuildingService>();
            gameManager = FindObjectOfType<GameManager>();

            EventBus.Subscribe<OnMoneyChanged>(OnMoneyChangedHandler);
            EventBus.Subscribe<OnOilChanged>(OnOilChangedHandler);
            EventBus.Subscribe<OnOilProductionUpdated>(OnProductionUpdatedHandler);
            EventBus.Subscribe<OnPlacementStarted>(OnPlacementStart);
            EventBus.Subscribe<OnPlacementEnded>(OnPlacementEnd);

            if (closeShopButton != null)
                closeShopButton.onClick.AddListener(() => ShowShop(false));
            if (closeMarketButton != null)
                closeMarketButton.onClick.AddListener(() => ShowMarket(false));

            if (placeButton != null)
            {
                placeButton.gameObject.SetActive(false);
                placeButton.onClick.AddListener(OnPlaceClicked);
            }

            UpdateMoneyDisplay(playerDataService?.Money ?? 0);
            UpdateOilDisplay(playerDataService?.OilHeld ?? 0);

            if (shopPanel != null) shopPanel.SetActive(false);
            if (marketPanel != null) marketPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(true);

            Debug.Log("[HUDManager] Đã khởi tạo.");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnMoneyChanged>(OnMoneyChangedHandler);
            EventBus.Unsubscribe<OnOilChanged>(OnOilChangedHandler);
            EventBus.Unsubscribe<OnOilProductionUpdated>(OnProductionUpdatedHandler);
            EventBus.Unsubscribe<OnPlacementStarted>(OnPlacementStart);
            EventBus.Unsubscribe<OnPlacementEnded>(OnPlacementEnd);
        }

        // === Event Handlers ===
        private void OnMoneyChangedHandler(OnMoneyChanged evt) => UpdateMoneyDisplay(evt.newAmount);
        private void OnOilChangedHandler(OnOilChanged evt) => UpdateOilDisplay((long)evt.newAmount);
        private void OnProductionUpdatedHandler(OnOilProductionUpdated evt) => UpdateProductionRateDisplay(evt.currentTotalProductionRate);
        private void OnPlacementStart(OnPlacementStarted e) { if (placeButton != null) placeButton.gameObject.SetActive(true); }
        private void OnPlacementEnd(OnPlacementEnded e) { if (placeButton != null) placeButton.gameObject.SetActive(false); }

        // === Hiển thị ===
        private void UpdateMoneyDisplay(long amount) { if (moneyText != null) moneyText.text = $"{amount} $"; }
        private void UpdateOilDisplay(long amount) { if (oilText != null) oilText.text = $"{amount}"; }
        private void UpdateProductionRateDisplay(float rate) { if (productionRateText != null) productionRateText.text = $"{rate} Oil/s"; }

        // === Mở/đóng panel ===  
        public void ShowShop(bool show, BuildingType? filterType = null)
        {
            if (shopPanel == null) return;

            if (show)
            {
                CameraZoom.Instance?.ZoomIn();
                if (filterType.HasValue)
                {
                    ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
                    if (shopUI != null) shopUI.FilterByType(filterType.Value);
                }
                shopPanel.SetActive(true);
                shopPanel.GetComponent<IUIAnimation>()?.PlayShow();
            }
            else
            {
                CameraZoom.Instance?.ZoomOut();
                shopPanel.GetComponent<IUIAnimation>()?.PlayHide(() =>
                {
                    shopPanel.SetActive(false);
                });
            }
        }

        public void ShowMarket(bool show)
        {
            if (marketPanel == null) return;

            if (show)
            {
                CameraZoom.Instance?.ZoomIn();
                marketPanel.SetActive(true);
                marketPanel.GetComponent<IUIAnimation>()?.PlayShow();
            }
            else
            {
                CameraZoom.Instance?.ZoomOut();
                marketPanel.GetComponent<IUIAnimation>()?.PlayHide(() =>
                {
                    marketPanel.SetActive(false);
                });
            }
        }

        // === Nút Đặt ===
        private void OnPlaceClicked()
        {
            IBuildingService bs = ServiceLocator.Get<IBuildingService>();
            if (bs != null) bs.TryPlaceFromUI();
        }
    }
}