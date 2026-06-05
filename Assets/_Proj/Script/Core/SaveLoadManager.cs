// Assets/_Project/Scripts/Core/SaveLoadManager.cs

using UnityEngine;
using System.IO;
using System.Collections;

namespace OilGame
{
    /// <summary>
    /// SaveLoadManager - Service quản lý lưu và load dữ liệu game.
    /// 
    /// Trách nhiệm:
    /// - Serialize/Deserialize SaveData ra file JSON.
    /// - Hỗ trợ Auto Save (định kỳ), Manual Save, Save khi thoát game.
    /// - Khi Load: đọc file, trả về SaveData để GameManager phân phối.
    /// - KHÔNG lưu dữ liệu Bot Simulation.
    /// 
    /// Đường dẫn file save: Application.persistentDataPath + "/" + saveFileName
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        [Header("Cấu hình")]
        [Tooltip("GameConfig chứa tham số save.")]
        [SerializeField] private GameConfig gameConfig;

        [Tooltip("Có tự động save khi ứng dụng mất focus không?")]
        [SerializeField] private bool autoSaveOnPause = true;

        // === Dữ liệu runtime ===

        /// <summary>Đường dẫn đầy đủ đến file save.</summary>
        private string saveFilePath;

        /// <summary>Coroutine auto save.</summary>
        private IEnumerator autoSaveCoroutine;

        /// <summary>Đang trong quá trình save? (tránh save chồng lấn).</summary>
        private bool isSaving;

        // Tham chiếu service
        private IPlayerDataService playerDataService;
        private ILandService landService;
        private IMarketService marketService;

        #region Unity Lifecycle

        private void Awake()
        {
            // Xác định đường dẫn file save
            string fileName = gameConfig != null ? gameConfig.saveFileName : "oilgame_save.dat";
            saveFilePath = Path.Combine(Application.persistentDataPath, fileName);

            Debug.Log($"[SaveLoadManager] File save: {saveFilePath}");
        }

        private void Start()
        {
            // Lấy service
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            landService = ServiceLocator.Get<ILandService>();
            marketService = ServiceLocator.Get<IMarketService>();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Khi ứng dụng bị pause (mất focus, về home...)
            if (pauseStatus && autoSaveOnPause && gameConfig != null && gameConfig.saveOnQuit)
            {
                Debug.Log("[SaveLoadManager] Ứng dụng mất focus - Auto Save...");
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            // Khi thoát ứng dụng
            if (gameConfig != null && gameConfig.saveOnQuit)
            {
                Debug.Log("[SaveLoadManager] Ứng dụng thoát - Auto Save...");
                SaveGame();
            }
        }

        private void OnDestroy()
        {
            StopAutoSave();
        }

        #endregion

        #region Kiểm tra file save

        /// <summary>
        /// Kiểm tra xem có file save tồn tại không.
        /// </summary>
        /// <returns>True nếu có file save.</returns>
        public bool HasSaveFile()
        {
            return File.Exists(saveFilePath);
        }

        /// <summary>
        /// Xóa file save hiện tại.
        /// </summary>
        public void DeleteSaveFile()
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[SaveLoadManager] Đã xóa file save.");
            }
        }

        #endregion

        #region Save Game

        /// <summary>
        /// Lưu game (Manual Save).
        /// Gọi từ UI hoặc GameManager.
        /// </summary>
        public void SaveGame()
        {
            if (isSaving)
            {
                Debug.LogWarning("[SaveLoadManager] Đang trong quá trình save, bỏ qua.");
                return;
            }

            if (playerDataService == null)
            {
                Debug.LogError("[SaveLoadManager] PlayerDataService chưa sẵn sàng, không thể save!");
                return;
            }

            isSaving = true;

            try
            {
                // Tạo SaveData từ PlayerDataManager
                SaveData saveData = playerDataService.CreateSaveData();

                // Thêm giá dầu hiện tại
                if (marketService != null)
                {
                    saveData.currentOilPrice = marketService.CurrentOilPrice;
                }

                // Serialize ra JSON
                string json = JsonUtility.ToJson(saveData, true);

                // Ghi file
                File.WriteAllText(saveFilePath, json);

                Debug.Log($"[SaveLoadManager] Đã lưu game thành công! File: {saveFilePath}, Size: {json.Length} bytes.");

                // Phát sự kiện
                EventBus.Publish(new OnGameSaved(saveFilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Lỗi khi lưu game: {e.Message}");
            }
            finally
            {
                isSaving = false;
            }
        }

        #endregion

        #region Load Game

        /// <summary>
        /// Load game từ file save.
        /// Trả về SaveData để GameManager phân phối cho các Manager.
        /// </summary>
        /// <returns>SaveData nếu load thành công, null nếu thất bại.</returns>
        public SaveData LoadGame()
        {
            if (!HasSaveFile())
            {
                Debug.Log("[SaveLoadManager] Không tìm thấy file save.");
                return null;
            }

            try
            {
                // Đọc file
                string json = File.ReadAllText(saveFilePath);

                // Deserialize
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null)
                {
                    Debug.LogError("[SaveLoadManager] Deserialize SaveData thất bại - dữ liệu null.");
                    return null;
                }


                // Phát sự kiện
                EventBus.Publish(new OnGameLoaded(saveData));

                return saveData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Lỗi khi load game: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Auto Save

        /// <summary>
        /// Bắt đầu Auto Save định kỳ.
        /// </summary>
        public void StartAutoSave()
        {
            StopAutoSave();

            if (gameConfig == null || gameConfig.autoSaveInterval <= 0f)
            {
                return;
            }

            autoSaveCoroutine = AutoSaveRoutine();
            CoroutineRunner.Instance.Run(autoSaveCoroutine);

        }

        /// <summary>
        /// Dừng Auto Save.
        /// </summary>
        public void StopAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(autoSaveCoroutine);
                autoSaveCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine Auto Save.
        /// </summary>
        private IEnumerator AutoSaveRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(gameConfig.autoSaveInterval);

            while (true)
            {
                yield return wait;
                Debug.Log("[SaveLoadManager] Auto Save định kỳ...");
                SaveGame();
            }
        }

        #endregion
    }
}