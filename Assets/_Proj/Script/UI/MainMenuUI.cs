// Assets/_Project/Scripts/UI/MainMenuUI.cs

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace OilGame
{
    /// <summary>
    /// MainMenuUI - Quản lý giao diện màn hình chính.
    /// 
    /// Trách nhiệm:
    /// - Hiển thị nút: New Game, Continue (Load Game), Settings, Exit.
    /// - Nút Continue chỉ hiển thị nếu có file save.
    /// - Nút Settings: mở panel cài đặt (âm thanh, đồ họa...).
    /// - Chuyển scene sang Game.unity khi bắt đầu chơi.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("=== Panel ===")]
        [Tooltip("Panel menu chính.")]
        [SerializeField] private GameObject mainMenuPanel;

        [Tooltip("Panel settings.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("=== Nút Menu Chính ===")]
        [Tooltip("Nút New Game.")]
        [SerializeField] private Button newGameButton;

        [Tooltip("Nút Continue (Load Game).")]
        [SerializeField] private Button continueButton;

        [Tooltip("Nút Settings.")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Nút Exit.")]
        [SerializeField] private Button exitButton;

        [Header("=== Nút Settings ===")]
        [Tooltip("Nút Back (quay lại menu chính).")]
        [SerializeField] private Button backButton;

        [Header("=== Thông tin ===")]
        [Tooltip("Text phiên bản game.")]
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("=== Cấu hình ===")]
        [Tooltip("Tên scene game chính.")]
        [SerializeField] private string gameSceneName = "Game";

        // Đường dẫn file save (giống SaveLoadManager)
        private string saveFilePath;

        #region Unity Lifecycle

        private void Start()
        {
            // Xác định đường dẫn file save
            saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "oilgame_save.dat");

            // Gán sự kiện nút
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                // Kiểm tra có file save không
                bool hasSave = System.IO.File.Exists(saveFilePath);
                continueButton.interactable = hasSave;

                if (!hasSave && continueButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "No Save Found";
                }
            }

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Hiển thị phiên bản
            if (versionText != null)
                versionText.text = $"v{Application.version}";

            // Mặc định hiển thị menu chính, ẩn settings
            ShowMainMenu();

            Debug.Log("[MainMenuUI] Đã khởi tạo.");
        }

        #endregion

        #region Điều hướng Panel

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void ShowSettings()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        #endregion

        #region Sự kiện Nút

        /// <summary>
        /// Nút New Game: Xóa file save cũ (nếu có) và vào game.
        /// </summary>
        private void OnNewGameClicked()
        {
            // Xóa file save cũ để đảm bảo New Game
            if (System.IO.File.Exists(saveFilePath))
            {
                System.IO.File.Delete(saveFilePath);
                Debug.Log("[MainMenuUI] Đã xóa file save cũ để bắt đầu New Game.");
            }

            LoadGameScene();
        }

        /// <summary>
        /// Nút Continue: Load game từ file save.
        /// </summary>
        private void OnContinueClicked()
        {
            if (!System.IO.File.Exists(saveFilePath))
            {
                Debug.LogWarning("[MainMenuUI] Không tìm thấy file save!");
                return;
            }

            LoadGameScene();
        }

        /// <summary>
        /// Nút Settings: Mở panel cài đặt.
        /// </summary>
        private void OnSettingsClicked()
        {
            ShowSettings();
        }

        /// <summary>
        /// Nút Back: Quay lại menu chính từ Settings.
        /// </summary>
        private void OnBackClicked()
        {
            ShowMainMenu();
        }

        /// <summary>
        /// Nút Exit: Thoát game.
        /// </summary>
        private void OnExitClicked()
        {
            Debug.Log("[MainMenuUI] Thoát game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Load Scene

        /// <summary>
        /// Load scene game chính.
        /// </summary>
        private void LoadGameScene()
        {
            Debug.Log($"[MainMenuUI] Đang load scene: {gameSceneName}...");
            SceneManager.LoadScene(gameSceneName);
        }

        #endregion
    }
}