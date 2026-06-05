// Assets/_Project/Scripts/Buildings/BuildingPreview.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// BuildingPreview - MonoBehaviour điều khiển đối tượng preview khi đang ở Placement Mode.
    /// 
    /// Trách nhiệm:
    /// - Hiển thị model công trình ở dạng trong suốt (hoặc wireframe).
    /// - Đổi màu xanh (có thể đặt) hoặc đỏ (không thể đặt) dựa trên trạng thái.
    /// - Di chuyển theo vị trí chuột/raycast.
    /// - Snap vào vị trí grid gần nhất.
    /// 
    /// Lưu ý: Preview không chứa logic kiểm tra hợp lệ. Nó chỉ nhận lệnh từ BuildingPlacer.
    /// </summary>
    public class BuildingPreview : MonoBehaviour
    {
        [Header("Tham chiếu Renderer")]
        [Tooltip("Renderer chính của model preview (dùng để đổi màu).")]
        [SerializeField] private Renderer[] renderers;

        [Header("Màu sắc")]
        [Tooltip("Màu khi vị trí hợp lệ (có thể đặt).")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("Màu khi vị trí không hợp lệ (không thể đặt).")]
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        // Trạng thái hiện tại
        private bool isCurrentlyValid = true;
        private bool isActive = false;

        // Material instance để tránh thay đổi material gốc
        private Material[] materialInstances;

        #region Unity Lifecycle

        private void Awake()
        {
            // Nếu không gán renderer, tự tìm
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            // Tạo material instance để thay đổi màu không ảnh hưởng đến prefab gốc
            CreateMaterialInstances();
        }

        private void OnDestroy()
        {
            // Dọn dẹp material instances
            if (materialInstances != null)
            {
                foreach (var mat in materialInstances)
                {
                    if (mat != null)
                        Destroy(mat);
                }
            }
        }

        #endregion

        #region Khởi tạo

        /// <summary>
        /// Tạo material instances từ renderer gốc.
        /// </summary>
        private void CreateMaterialInstances()
        {
            if (renderers == null) return;

            materialInstances = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    materialInstances[i] = new Material(renderers[i].material);
                    renderers[i].material = materialInstances[i];

                    // Thiết lập chế độ transparent
                    materialInstances[i].SetFloat("_Mode", 2); // Fade mode
                    materialInstances[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    materialInstances[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    materialInstances[i].SetInt("_ZWrite", 0);
                    materialInstances[i].DisableKeyword("_ALPHATEST_ON");
                    materialInstances[i].EnableKeyword("_ALPHABLEND_ON");
                    materialInstances[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    materialInstances[i].renderQueue = 3000;
                }
            }
        }

        /// <summary>
        /// Kích hoạt preview với model từ BuildingData.
        /// </summary>
        /// <param name="prefab">Prefab gốc của công trình.</param>
        public void Activate(GameObject prefab)
        {
            if (prefab == null) return;

            // Nếu chưa có model con, spawn một bản sao của prefab làm con
            // (chỉ lấy phần visual, không lấy script)
            if (transform.childCount == 0)
            {
                GameObject visual = Instantiate(prefab, transform);
                visual.name = "Preview_Visual";

                // Xóa script Building nếu có
                Building buildingScript = visual.GetComponent<Building>();
                if (buildingScript != null)
                {
                    Destroy(buildingScript);
                }

                // Xóa Collider để tránh ảnh hưởng raycast
                Collider[] colliders = visual.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }

                // Cập nhật renderers
                renderers = visual.GetComponentsInChildren<Renderer>();
                CreateMaterialInstances();
            }

            gameObject.SetActive(true);
            isActive = true;
            SetValid(true); // Mặc định màu xanh
        }

        /// <summary>
        /// Vô hiệu hóa preview.
        /// </summary>
        public void Deactivate()
        {
            gameObject.SetActive(false);
            isActive = false;
        }

        #endregion

        #region Cập nhật trạng thái

        /// <summary>
        /// Cập nhật vị trí của preview (snap vào grid).
        /// </summary>
        /// <param name="worldPosition">Vị trí world cần đặt (đã được tính toán bởi BuildingPlacer).</param>
        public void UpdatePosition(Vector3 worldPosition)
        {
            Debug.Log($"[BuildingPreview] Set position from {transform.position} to {worldPosition}");
            transform.position = worldPosition;
        }

        /// <summary>
        /// Đặt trạng thái hợp lệ/không hợp lệ.
        /// Thay đổi màu sắc của preview.
        /// </summary>
        /// <param name="valid">True = xanh (có thể đặt), False = đỏ (không thể đặt).</param>
        public void SetValid(bool valid)
        {
            // Chỉ cập nhật nếu trạng thái thay đổi (tối ưu)
            if (isCurrentlyValid == valid && isActive) return;

            isCurrentlyValid = valid;
            Color targetColor = valid ? validColor : invalidColor;

            if (materialInstances != null)
            {
                foreach (var mat in materialInstances)
                {
                    if (mat != null)
                    {
                        mat.color = targetColor;
                    }
                }
            }
        }

        #endregion
    }
}