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

        [Header("=== Màu chữ ===")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color fullColor = Color.green;

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
            if (building == null || building.BuildingData == null || infoText == null) return;

            if (building.Type == BuildingType.Drill)
            {
                infoText.text = $"{building.BuildingData.productionRate}/s";
                infoText.color = normalColor;
            }
            else if (building.Type == BuildingType.Bucket)
            {
                long current = building.GetCurrentOil();
                long max = building.GetCapacity();
                infoText.text = $"{current}/{max}";
                infoText.color = current >= max ? fullColor : normalColor;
            }
        }
    }
}