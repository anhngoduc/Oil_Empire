// Assets/_Project/Scripts/UI/UnlockButtonSetup.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace OilGame
{
    /// <summary>
    /// Gắn vào Prefab UnlockButtonPrefab. Quản lý hiển thị giá, hệ số, và animation nút.
    /// </summary>
    public class UnlockButtonSetup : MonoBehaviour
    {
        [Header("=== Kéo thả từ Prefab ===")]
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Transform buttonScaler;
        [SerializeField] private Button unlockButton;

        public void Setup(long unlockCost, long oilMultiplier, int zoneID, int plotID)
        {
            if (priceText != null)
                priceText.text = $"${unlockCost:N0} - x{oilMultiplier}";

            if (buttonScaler != null)
                buttonScaler.localScale = Vector3.zero;

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(() =>
                {
                    ILandService landService = ServiceLocator.Get<ILandService>();
                    landService?.UnlockPlot(zoneID, plotID);
                });
            }
        }

        public void ShowButton()
        {
            if (buttonScaler != null)
            {
                buttonScaler.DOKill();
                buttonScaler.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }

        public void HideButton()
        {
            if (buttonScaler != null)
            {
                buttonScaler.DOKill();
                buttonScaler.DOScale(0f, 0.2f).SetEase(Ease.InBack);
            }
        }
    }
}