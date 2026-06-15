// Assets/_Project/Scripts/UI/MarketUI.cs

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OilGame
{
    public class MarketUI : MonoBehaviour
    {
        [Header("=== Hiển thị ===")]
        [SerializeField] private TextMeshProUGUI currentPriceText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI sellInfoText;

        [Header("=== Slider ===")]
        [SerializeField] private Slider priceSlider;

        [Header("=== Nút ===")]
        [SerializeField] private Button sellAllButton;

        private IMarketService marketService;
        private IPlayerDataService playerDataService;

        private void Start()
        {
            marketService = ServiceLocator.Get<IMarketService>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            if (sellAllButton != null)
                sellAllButton.onClick.AddListener(OnSellAllClicked);

            EventBus.Subscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Subscribe<OnOilChanged>(OnOilChangedHandler);

            UpdateAll();
        }

        private void Update()
        {
            if (marketService != null)
                UpdateCountdown(marketService.GetTimeUntilNextPriceUpdate());
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Unsubscribe<OnOilChanged>(OnOilChangedHandler);
        }

        // === Cập nhật ===
        private void UpdateAll()
        {
            UpdatePrice();
            UpdateSlider();
            UpdateSellInfo();
        }

        private void UpdatePrice()
        {
            long price = marketService?.CurrentOilPrice ?? 0;
            if (currentPriceText != null)
                currentPriceText.text = $"Giá hiện tại: ${price}";
        }

        private void UpdateCountdown(float timeLeft)
        {
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                countdownText.text = $"Giá tiếp theo sau: {seconds}s";
            }
        }

        private void UpdateSlider()
        {
            long price = marketService?.CurrentOilPrice ?? 0;
            if (priceSlider != null)
            {
                priceSlider.minValue = 1;
                priceSlider.maxValue = 15;
                priceSlider.DOValue(price, 1f).SetEase(Ease.OutBack); // Mượt
            }
        }

        private void UpdateSellInfo()
        {
            long oil = playerDataService != null ? (long)playerDataService.OilHeld : 0;
            long price = marketService?.CurrentOilPrice ?? 0;
            long total = oil * price;

            if (sellInfoText != null)
                sellInfoText.text = $"Bán {oil} dầu (+${total:N0})";
        }

        // === Event ===
        private void OnOilPriceChangedHandler(OnOilPriceChanged evt) => UpdateAll();
        private void OnOilChangedHandler(OnOilChanged evt) => UpdateSellInfo();

        // === Bán ===
        private void OnSellAllClicked()
        {
            if (marketService == null || playerDataService == null) return;
            long oil = (long)playerDataService.OilHeld;
            if (oil <= 0) return;
            marketService.SellOil(oil);
            UpdateSellInfo();
        }
    }
}