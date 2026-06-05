// Assets/_Project/Scripts/UI/PlotItemSlot.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    public class PlotItemSlot : MonoBehaviour
    {
        [Header("=== UI Elements ===")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI gridSizeText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Image backgroundImage;

        [Header("=== Màu sắc ===")]
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color lockedCanAffordColor = new Color(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color lockedCannotAffordColor = new Color(0.8f, 0.2f, 0.2f);

        private PlotInfo currentPlot;
        private bool isUnlocked;
        private double unlockCost;
        private System.Action onClickCallback;
        private int cellsX;
        private int cellsZ;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnClick);
        }

        public void Setup(PlotInfo plot, bool unlocked, bool canAfford, int cellsPerPlotX, int cellsPerPlotZ, System.Action callback)
        {
            currentPlot = plot;
            isUnlocked = unlocked;
            unlockCost = plot.unlockCost;
            onClickCallback = callback;
            cellsX = cellsPerPlotX;
            cellsZ = cellsPerPlotZ;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(plot.plotName) ? $"Mảnh {plot.plotID}" : plot.plotName;

            if (multiplierText != null)
                multiplierText.text = $"Hệ số: x{plot.oilMultiplier}";

            if (gridSizeText != null)
                gridSizeText.text = $"Ô: {cellsX}x{cellsZ}";

            if (statusText != null)
                statusText.text = unlocked ? "ĐÃ MỞ" : "KHÓA";

            if (costText != null)
                costText.text = unlocked ? "" : $"Giá: ${unlockCost:N0}";

            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(!unlocked);
                unlockButton.interactable = canAfford;
            }

            UpdateBackground(unlocked, canAfford);
        }

        public void UpdateAffordability(double currentMoney)
        {
            if (isUnlocked) return;
            bool canAfford = currentMoney >= unlockCost;
            if (unlockButton != null) unlockButton.interactable = canAfford;
            UpdateBackground(false, canAfford);
        }

        private void UpdateBackground(bool unlocked, bool canAfford)
        {
            if (backgroundImage != null)
            {
                if (unlocked) backgroundImage.color = unlockedColor;
                else if (canAfford) backgroundImage.color = lockedCanAffordColor;
                else backgroundImage.color = lockedCannotAffordColor;
            }
        }

        private void OnClick() => onClickCallback?.Invoke();
    }
}