// Assets/_Project/Scripts/Land/PlotTrigger.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Gắn vào Cube mảnh đất (bằng code). Xử lý hiện/ẩn nút mở khóa khi Player vào/ra.
    /// </summary>
    public class PlotTrigger : MonoBehaviour, IInteractable
    {
        private UnlockButtonSetup buttonSetup;

        public void Setup(UnlockButtonSetup setup) { this.buttonSetup = setup; }
        public string GetInteractName() => "Mảnh đất";
        public void OnInteract(GameObject player, bool isRealPlayer) { }

        private bool IsPlayerZone()
        {
            IPlayerDataService playerData = ServiceLocator.Get<IPlayerDataService>();
            if (playerData == null) return false;
            string[] parts = gameObject.name.Split('_');
            if (parts.Length < 2) return false;
            int zoneID = int.Parse(parts[1]);
            return zoneID == playerData.PlayerZoneID;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerInteractor>() != null && IsPlayerZone())
                buttonSetup?.ShowButton();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerInteractor>() != null && IsPlayerZone())
                buttonSetup?.HideButton();
        }
    }
}