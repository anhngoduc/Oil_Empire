// Assets/_Project/Scripts/Buildings/BuildingOverlay.cs

using UnityEngine;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// Gắn vào Prefab công trình. Hiển thị thông tin trên đầu công trình.
    /// </summary>
    public class BuildingOverlay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI infoText;

        private Building building;
        private Camera mainCamera;

        private void Start()
        {
            building = GetComponentInParent<Building>();
            if (infoText != null) infoText = GetComponentInChildren<TextMeshProUGUI>();
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
                transform.forward = mainCamera.transform.forward;
        }

        private void Update()
        {
            if (building == null || building.BuildingData == null) return;

            if (building.Type == BuildingType.Drill)
            {
                infoText.text = $"{building.BuildingData.productionRate}/s";
            }
            else if (building.Type == BuildingType.Bucket)
            {
                float current = building.GetCurrentOil();
                float max = building.GetCapacity();
                infoText.text = $"{current:F0}/{max:F0}";
            }
        }
    }
}