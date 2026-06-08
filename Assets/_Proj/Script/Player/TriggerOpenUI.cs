// Assets/_Project/Scripts/Player/TriggerOpenUI.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Gắn vào GameObject có Collider Trigger.
    /// Khi Player đi vào → mở UI qua HUDManager, đi ra → đóng.
    /// </summary>
    public class TriggerOpenUI : MonoBehaviour
    {
        public enum UIType { Shop, Market }

        [Header("=== Loại UI ===")]
        [SerializeField] private UIType uiType;

        private HUDManager hudManager;

        private void Start()
        {
            hudManager = FindObjectOfType<HUDManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) OpenUI();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) CloseUI();
        }

        private void OpenUI()
        {
            if (hudManager == null) return;

            switch (uiType)
            {
                case UIType.Shop: hudManager.ShowShop(true); break;
                case UIType.Market: hudManager.ShowMarket(true); break;
            }
        }

        private void CloseUI()
        {
            if (hudManager == null) return;

            switch (uiType)
            {
                case UIType.Shop: hudManager.ShowShop(false); break;
                case UIType.Market: hudManager.ShowMarket(false); break;
            }
        }
    }
}