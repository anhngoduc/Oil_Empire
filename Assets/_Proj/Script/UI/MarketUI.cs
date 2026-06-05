// Assets/_Project/Scripts/UI/MarketUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// MarketUI - Quản lý giao diện bán dầu.
    /// Phiên bản đơn giản: chỉ có nút Bán tất cả.
    /// </summary>
    public class MarketUI : MonoBehaviour
    {
        [Header("=== Hiển thị ===")]
        [Tooltip("Text hiển thị giá dầu hiện tại.")]
        [SerializeField] private TextMeshProUGUI currentPriceText;

        [Tooltip("Text hiển thị thời gian đến lần đổi giá tiếp.")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Tooltip("Text hiển thị số dầu đang có.")]
        [SerializeField] private TextMeshProUGUI oilHeldText;

        [Tooltip("Text hiển thị số tiền ước tính nhận được.")]
        [SerializeField] private TextMeshProUGUI estimatedMoneyText;

        [Header("=== Nút ===")]
        [Tooltip("Nút Bán tất cả.")]
        [SerializeField] private Button sellAllButton;

        // Tham chiếu service
        private IMarketService marketService;
        private IPlayerDataService playerDataService;

        // Giá trị hiện tại
        private float currentPrice;

        #region Khởi tạo

        private void Start()
        {
            // Lấy service
            marketService = ServiceLocator.Get<IMarketService>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            // Gán sự kiện nút
            if (sellAllButton != null)
                sellAllButton.onClick.AddListener(OnSellAllClicked);

            // Đăng ký sự kiện
            EventBus.Subscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Subscribe<OnOilChanged>(OnOilChangedHandler);

            // Cập nhật giá trị ban đầu
            UpdatePriceDisplay();
            UpdateOilDisplay();

            Debug.Log("[MarketUI] Đã khởi tạo.");
        }

        private void Update()
        {
            // Cập nhật countdown
            if (marketService != null)
            {
                float timeLeft = marketService.GetTimeUntilNextPriceUpdate();
                UpdateCountdownDisplay(timeLeft);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnOilPriceChanged>(OnOilPriceChangedHandler);
            EventBus.Unsubscribe<OnOilChanged>(OnOilChangedHandler);
        }

        #endregion

        #region Cập nhật Hiển thị

        private void UpdatePriceDisplay()
        {
            if (marketService != null)
            {
                currentPrice = marketService.CurrentOilPrice;
                if (currentPriceText != null)
                {
                    currentPriceText.text = $"Giá: ${currentPrice:F2}/Oil";
                }
            }
            UpdateEstimatedMoney();
        }

        private void UpdateOilDisplay()
        {
            if (playerDataService != null && oilHeldText != null)
            {
                oilHeldText.text = $"Dầu đang có: {playerDataService.OilHeld:N0}";
            }
            UpdateEstimatedMoney();
        }

        private void UpdateCountdownDisplay(float timeLeft)
        {
            if (countdownText != null)
            {
                if (timeLeft > 0f)
                {
                    int minutes = Mathf.FloorToInt(timeLeft / 60f);
                    int seconds = Mathf.FloorToInt(timeLeft % 60f);
                    countdownText.text = $"Đổi giá sau: {minutes:D2}:{seconds:D2}";
                }
                else
                {
                    countdownText.text = "Đang đổi giá...";
                }
            }
        }

        private void UpdateEstimatedMoney()
        {
            if (estimatedMoneyText != null && playerDataService != null)
            {
                double totalOil = playerDataService.OilHeld;
                double estimatedMoney = totalOil * currentPrice;
                estimatedMoneyText.text = $"Sẽ nhận: ${estimatedMoney:F2}";
            }
        }

        #endregion

        #region Sự kiện

        private void OnSellAllClicked()
        {
            if (marketService == null || playerDataService == null) return;

            float totalOil = (float)playerDataService.OilHeld;
            if (totalOil <= 0f)
            {
                Debug.Log("[MarketUI] Không có dầu để bán!");
                return;
            }

            float earned = marketService.SellOil(totalOil);

            if (earned > 0f)
            {
                Debug.Log($"[MarketUI] Đã bán hết {totalOil} Oil, nhận ${earned:F2}.");
                UpdateOilDisplay();
                UpdateEstimatedMoney();
            }
        }

        private void OnOilPriceChangedHandler(OnOilPriceChanged evt)
        {
            UpdatePriceDisplay();
        }

        private void OnOilChangedHandler(OnOilChanged evt)
        {
            UpdateOilDisplay();
        }

        #endregion
    }
}