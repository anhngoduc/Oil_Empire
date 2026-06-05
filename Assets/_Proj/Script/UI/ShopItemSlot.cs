// Assets/_Project/Scripts/UI/ShopItemSlot.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// ShopItemSlot - Đại diện một item trong Shop ScrollView.
    /// </summary>
    public class ShopItemSlot : MonoBehaviour
    {
        [Header("=== UI Elements ===")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI statText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buttonBackground;

        [Header("=== Màu sắc ===")]
        [SerializeField] private Color canAffordColor = Color.green;
        [SerializeField] private Color cannotAffordColor = Color.red;

        public BuildingData CurrentData { get; private set; }
        private System.Action<BuildingData> onBuyCallback;

        private void Awake()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyClick);
        }

        public void Setup(BuildingData data, bool canAfford, System.Action<BuildingData> callback)
        {
            CurrentData = data;
            onBuyCallback = callback;

            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;

            if (nameText != null)
                nameText.text = data.buildingName;

            if (priceText != null)
                priceText.text = data.GetPriceDisplay();

            if (statText != null)
                statText.text = data.GetStatDisplay();

            SetAffordability(canAfford);
        }

        public void SetAffordability(bool canAfford)
        {
            if (buyButton != null)
                buyButton.interactable = canAfford;

            if (buttonBackground != null)
                buttonBackground.color = canAfford ? canAffordColor : cannotAffordColor;
        }

        private void OnBuyClick()
        {
            if (CurrentData != null && onBuyCallback != null)
                onBuyCallback.Invoke(CurrentData);
        }
    }
}