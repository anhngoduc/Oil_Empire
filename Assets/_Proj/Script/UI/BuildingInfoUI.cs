// Assets/_Project/Scripts/UI/BuildingInfoUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// BuildingInfoUI - Popup hiển thị thông tin chi tiết của một công trình.
    /// 
    /// Trách nhiệm:
    /// - Hiển thị khi người chơi click vào một Building trên map.
    /// - Hiển thị: tên, cấp, loại, chỉ số (tốc độ/dung tích).
    /// - Với Bucket: hiển thị lượng dầu hiện tại, thanh tiến trình, nút "Thu dầu".
    /// - Với Drill: hiển thị tốc độ sản xuất (đã áp hệ số mảnh).
    /// - Nút "Xóa" công trình (tùy chọn, có thể thêm sau).
    /// - Tự động ẩn khi click ra ngoài.
    /// </summary>
    public class BuildingInfoUI : MonoBehaviour
    {
        [Header("=== Panel chính ===")]
        [Tooltip("GameObject panel (có thể set active/inactive).")]
        [SerializeField] private GameObject panel;

        [Header("=== Thông tin chung ===")]
        [Tooltip("Text tên công trình.")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Tooltip("Text loại công trình (Drill/Bucket).")]
        [SerializeField] private TextMeshProUGUI typeText;

        [Tooltip("Text cấp độ.")]
        [SerializeField] private TextMeshProUGUI levelText;

        [Tooltip("Text chỉ số (tốc độ hoặc dung tích).")]
        [SerializeField] private TextMeshProUGUI statText;

        [Tooltip("Icon công trình.")]
        [SerializeField] private Image iconImage;

        [Header("=== Thông tin Bucket ===")]
        [Tooltip("Panel bucket (chỉ hiện khi là Bucket).")]
        [SerializeField] private GameObject bucketPanel;

        [Tooltip("Text lượng dầu hiện tại / tối đa.")]
        [SerializeField] private TextMeshProUGUI bucketOilText;

        [Tooltip("Slider hiển thị % dầu.")]
        [SerializeField] private Slider bucketOilSlider;

        [Tooltip("Nút Thu dầu.")]
        [SerializeField] private Button collectButton;

        [Header("=== Nút khác ===")]
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;

        // Dữ liệu công trình đang hiển thị
        private Building currentBuilding;

        // Tham chiếu service
        private IBucketService bucketService;
        private IBuildingService buildingService;

        #region Khởi tạo

        private void Start()
        {
            // Lấy service
            bucketService = ServiceLocator.Get<IBucketService>();
            buildingService = ServiceLocator.Get<IBuildingService>();

            // Gán sự kiện nút
            if (collectButton != null)
                collectButton.onClick.AddListener(OnCollectClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // Đăng ký sự kiện cập nhật bucket
            EventBus.Subscribe<OnBucketUpdated>(OnBucketUpdatedHandler);

            // Mặc định ẩn popup
            Hide();

            Debug.Log("[BuildingInfoUI] Đã khởi tạo.");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnBucketUpdated>(OnBucketUpdatedHandler);
        }

        #endregion

        #region Hiển thị / Ẩn

        /// <summary>
        /// Hiển thị thông tin của một công trình.
        /// Gọi từ PlayerController khi click vào Building.
        /// </summary>
        /// <param name="building">Công trình được click.</param>
        public void Show(Building building)
        {
            if (building == null || building.BuildingData == null) return;

            currentBuilding = building;
            BuildingData data = building.BuildingData;

            // Cập nhật thông tin chung
            if (nameText != null)
                nameText.text = data.buildingName;

            if (typeText != null)
                typeText.text = data.buildingType == BuildingType.Drill ? "Dàn Khoan" : "Xô Chứa";

            if (levelText != null)
                levelText.text = $"Cấp {data.level}";

            if (statText != null)
                statText.text = data.GetStatDisplay();

            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;

            // Xử lý riêng cho Bucket
            if (data.buildingType == BuildingType.Bucket)
            {
                if (bucketPanel != null)
                    bucketPanel.SetActive(true);

                UpdateBucketInfo(building);
            }
            else
            {
                if (bucketPanel != null)
                    bucketPanel.SetActive(false);
            }

            // Hiển thị panel
            if (panel != null)
                panel.SetActive(true);

            Debug.Log($"[BuildingInfoUI] Hiển thị thông tin: {data.buildingName} (ID={building.UniqueID}).");
        }

        /// <summary>
        /// Ẩn popup.
        /// </summary>
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);

            currentBuilding = null;
        }

        /// <summary>
        /// Cập nhật thông tin bucket (dầu hiện tại).
        /// </summary>
        private void UpdateBucketInfo(Building building)
        {
            if (building == null || building.Type != BuildingType.Bucket) return;

            float currentOil = building.GetCurrentOil();
            float capacity = building.GetCapacity();

            if (bucketOilText != null)
                bucketOilText.text = $"{currentOil:F0} / {capacity:F0} Oil";

            if (bucketOilSlider != null)
            {
                bucketOilSlider.maxValue = capacity;
                bucketOilSlider.value = currentOil;
            }
        }

        #endregion

        #region Sự kiện

        /// <summary>
        /// Xử lý khi click nút Thu dầu.
        /// </summary>
        private void OnCollectClicked()
        {
            if (currentBuilding == null || bucketService == null) return;

            float collected = bucketService.CollectOil(currentBuilding.UniqueID);

            if (collected > 0f)
            {
                Debug.Log($"[BuildingInfoUI] Đã thu {collected} Oil từ Bucket ID={currentBuilding.UniqueID}.");

                // Cập nhật lại thông tin bucket
                UpdateBucketInfo(currentBuilding);
            }
        }

        /// <summary>
        /// Xử lý khi bucket được cập nhật (từ ProductionManager).
        /// </summary>
        private void OnBucketUpdatedHandler(OnBucketUpdated evt)
        {
            // Nếu popup đang hiển thị và đúng bucket này, cập nhật UI
            if (currentBuilding != null && currentBuilding.UniqueID == evt.bucketUniqueID)
            {
                UpdateBucketInfo(currentBuilding);
            }
        }

        #endregion
    }
}