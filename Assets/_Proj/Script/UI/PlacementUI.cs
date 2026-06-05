// Assets/_Project/Scripts/UI/PlacementUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// PlacementUI - Giao diện khi đang ở Placement Mode.
    /// 
    /// Trách nhiệm:
    /// - Hiển thị thông tin công trình đang đặt.
    /// - Nút "Đặt" (xác nhận) và "Hủy".
    /// - Hiển thị hướng dẫn (click để đặt, chuột phải để hủy).
    /// - Tự động ẩn/hiện khi vào/ra Placement Mode.
    /// </summary>
    public class PlacementUI : MonoBehaviour
    {
        [Header("=== Panel ===")]
        [Tooltip("GameObject panel chính.")]
        [SerializeField] private GameObject panel;

        [Header("=== Thông tin ===")]
        [Tooltip("Text tên công trình đang đặt.")]
        [SerializeField] private TextMeshProUGUI buildingNameText;

        [Tooltip("Text số lượng còn lại trong inventory.")]
        [SerializeField] private TextMeshProUGUI remainingText;

        [Tooltip("Icon công trình.")]
        [SerializeField] private Image iconImage;

        [Header("=== Nút ===")]
        [Tooltip("Nút Đặt (xác nhận).")]
        [SerializeField] private Button placeButton;

        [Tooltip("Nút Hủy.")]
        [SerializeField] private Button cancelButton;

        [Header("=== Hướng dẫn ===")]
        [Tooltip("Text hướng dẫn.")]
        [SerializeField] private TextMeshProUGUI instructionText;

        // Tham chiếu service
        private IBuildingService buildingService;
        private IInventoryService inventoryService;

        #region Khởi tạo

        private void Start()
        {
            // Lấy service
            buildingService = ServiceLocator.Get<IBuildingService>();
            inventoryService = ServiceLocator.Get<IInventoryService>();

            // Gán sự kiện nút
            if (placeButton != null)
                placeButton.onClick.AddListener(OnPlaceClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            // Đăng ký sự kiện
            EventBus.Subscribe<OnPlacementStarted>(OnPlacementStartedHandler);
            EventBus.Subscribe<OnPlacementEnded>(OnPlacementEndedHandler);
            EventBus.Subscribe<OnInventoryChanged>(OnInventoryChangedHandler);

            // Mặc định ẩn
            if (panel != null)
                panel.SetActive(false);

            Debug.Log("[PlacementUI] Đã khởi tạo.");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnPlacementStarted>(OnPlacementStartedHandler);
            EventBus.Unsubscribe<OnPlacementEnded>(OnPlacementEndedHandler);
            EventBus.Unsubscribe<OnInventoryChanged>(OnInventoryChangedHandler);
        }

        #endregion

        #region Hiển thị / Ẩn

        /// <summary>
        /// Hiển thị UI placement.
        /// </summary>
        private void Show(BuildingData data)
        {
            if (data == null) return;

            if (buildingNameText != null)
                buildingNameText.text = $"Đang đặt: {data.buildingName}";

            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;

            UpdateRemainingCount(data.buildingID);

            if (instructionText != null)
                instructionText.text = "Click trái: Đặt | Click phải / Esc: Hủy";

            if (panel != null)
                panel.SetActive(true);

            Debug.Log($"[PlacementUI] Hiển thị placement: {data.buildingName}.");
        }

        /// <summary>
        /// Ẩn UI placement.
        /// </summary>
        private void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>
        /// Cập nhật số lượng còn lại.
        /// </summary>
        private void UpdateRemainingCount(int buildingID)
        {
            if (inventoryService != null && remainingText != null)
            {
                int count = inventoryService.GetCount(buildingID);
                remainingText.text = $"Còn lại: {count}";
            }
        }

        #endregion

        #region Sự kiện

        /// <summary>
        /// Xử lý khi vào Placement Mode.
        /// </summary>
        private void OnPlacementStartedHandler(OnPlacementStarted evt)
        {
            Show(evt.buildingData);
        }

        /// <summary>
        /// Xử lý khi thoát Placement Mode.
        /// </summary>
        private void OnPlacementEndedHandler(OnPlacementEnded evt)
        {
            Hide();
        }

        /// <summary>
        /// Xử lý khi inventory thay đổi (cập nhật số lượng còn lại).
        /// </summary>
        private void OnInventoryChangedHandler(OnInventoryChanged evt)
        {
            if (buildingService != null && buildingService.IsInPlacementMode)
            {
                BuildingData currentData = buildingService.CurrentPlacementData;
                if (currentData != null && currentData.buildingID == evt.buildingID)
                {
                    UpdateRemainingCount(evt.buildingID);
                }
            }
        }

        /// <summary>
        /// Nút Đặt - gọi BuildingPlacer.TryPlaceBuilding().
        /// </summary>
        private void OnPlaceClicked()
        {
            // Việc đặt được xử lý bởi BuildingPlacer khi click chuột trái
            // Nút này chỉ là gợi ý, có thể thêm logic nếu muốn
            Debug.Log("[PlacementUI] Nút Đặt được nhấn - hãy click xuống đất để đặt.");
        }

        /// <summary>
        /// Nút Hủy - gọi BuildingManager.CancelPlacement().
        /// </summary>
        private void OnCancelClicked()
        {
            if (buildingService != null)
            {
                buildingService.CancelPlacement();
            }
        }

        #endregion
    }
}