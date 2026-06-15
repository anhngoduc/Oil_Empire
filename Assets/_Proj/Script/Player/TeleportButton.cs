// Assets/_Project/Scripts/Player/TeleportButton.cs

using UnityEngine;
using UnityEngine.UI;

namespace OilGame
{
    /// <summary>
    /// Gắn vào mỗi nút dịch chuyển.
    /// </summary>
    public class TeleportButton : MonoBehaviour
    {
        [Header("=== Cấu hình ===")]
        [Tooltip("Index trong TeleportManager (0 = Nhà, 1,2,3... = custom)")]
        [SerializeField] private int pointIndex = 0;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                TeleportManager.Instance?.TeleportPlayerTo(pointIndex);
            });
        }
    }
}