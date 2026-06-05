// Assets/_Project/Scripts/UI/InventorySlotUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OilGame
{
    /// <summary>
    /// InventorySlotUI - Đại diện một ô trong Inventory ScrollView.
    /// 
    /// Trách nhiệm:
    /// - Hiển thị icon, tên, số lượng của một BuildingData.
    /// - Xử lý sự kiện click.
    /// - Hỗ trợ Object Pooling (có hàm Clear để reset trạng thái).
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("=== UI Elements ===")]
        [Tooltip("Image hiển thị icon công trình.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Text hiển thị tên công trình.")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Tooltip("Text hiển thị số lượng.")]
        [SerializeField] private TextMeshProUGUI quantityText;

        [Tooltip("Button để click chọn.")]
        [SerializeField] private Button button;

        [Tooltip("Background image (có thể đổi màu khi hover).")]
        [SerializeField] private Image backgroundImage;

        // Dữ liệu hiện tại
        private BuildingData currentData;
        private int currentQuantity;
        private Action<BuildingData> onClickCallback;

        #region Khởi tạo

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        #endregion

        #region Setup

        /// <summary>
        /// Gán dữ liệu cho slot.
        /// </summary>
        /// <param name="data">BuildingData của item.</param>
        /// <param name="quantity">Số lượng hiện có.</param>
        /// <param name="callback">Hàm gọi khi click vào slot.</param>
        public void Setup(BuildingData data, int quantity, Action<BuildingData> callback)
        {
            currentData = data;
            currentQuantity = quantity;
            onClickCallback = callback;

            // Cập nhật UI
            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = true;
            }

            if (nameText != null)
            {
                nameText.text = data.buildingName;
            }

            if (quantityText != null)
            {
                quantityText.text = $"x{quantity}";
            }

            // Đảm bảo slot active
            gameObject.SetActive(true);

            // Đảm bảo button tương tác được
            if (button != null)
            {
                button.interactable = quantity > 0;
            }
        }

        /// <summary>
        /// Reset slot về trạng thái rỗng (dùng cho Object Pooling).
        /// </summary>
        public void Clear()
        {
            currentData = null;
            currentQuantity = 0;
            onClickCallback = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = "";
            }

            if (quantityText != null)
            {
                quantityText.text = "";
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }

        #endregion

        #region Sự kiện

        /// <summary>
        /// Xử lý khi click vào slot.
        /// </summary>
        private void OnClick()
        {
            if (currentData != null && currentQuantity > 0 && onClickCallback != null)
            {
                onClickCallback.Invoke(currentData);
            }
        }

        #endregion

        #region Helper

        /// <summary>
        /// Cập nhật số lượng (không cần setup lại toàn bộ).
        /// </summary>
        public void UpdateQuantity(int newQuantity)
        {
            currentQuantity = newQuantity;

            if (quantityText != null)
            {
                quantityText.text = $"x{newQuantity}";
            }

            if (button != null)
            {
                button.interactable = newQuantity > 0;
            }

            // Nếu hết hàng, ẩn slot
            if (newQuantity <= 0)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}