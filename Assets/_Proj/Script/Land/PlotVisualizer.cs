// Assets/_Project/Scripts/Land/PlotVisualizer.cs

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace OilGame
{
    public class PlotVisualizer : MonoBehaviour
    {
        [Header("=== Nút mở khóa ===")]
        [SerializeField] private GameObject unlockButtonPrefab;

        [Header("=== Tham chiếu ===")]
        [SerializeField] private ZoneManager zoneManager;
        [SerializeField] private GameObject plotCubePrefab;

        [Header("=== Material ===")]
        [SerializeField] private Material playerUnlockedMat;
        [SerializeField] private Material playerLockedMat;
        [SerializeField] private Material botZoneMat;
        [SerializeField] private Material emptyZoneMat;

        private IPlayerDataService playerDataService;
        private float cellSize;

        private void Start()
        {
            zoneManager = FindObjectOfType<ZoneManager>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            GameConfig config = FindObjectOfType<GameConfig>();
            cellSize = config != null ? config.cellSize : 1f;

            EventBus.Subscribe<OnGameReady>(OnGameReady);
            EventBus.Subscribe<OnLandUnlocked>(OnLandUnlocked);
        }

        private void OnGameReady(OnGameReady evt)
        {
            if (zoneManager != null && zoneManager.AllZones.Count == 0)
                zoneManager.Initialize();

            CreateAllPlotCubes();
            Debug.Log("[PlotVisualizer] Đã tạo Cube (OnGameReady)");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLandUnlocked>(OnLandUnlocked);
        }

        private void OnLandUnlocked(OnLandUnlocked evt)
        {
            // Đổi màu Cube
            foreach (Transform child in transform)
            {
                if (child.name == $"Plot_{evt.zoneID}_{evt.plotID}")
                {
                    Renderer r = child.GetComponent<Renderer>();
                    if (r == null) r = child.GetComponentInChildren<Renderer>();
                    if (r != null) r.material = playerUnlockedMat;
                    break;
                }
            }

            foreach (Transform child in transform)
            {
                if (child.name == $"BTN_Unlock_{evt.zoneID}_{evt.plotID}")
                {
                    Destroy(child.gameObject);
                    break;
                }
            }
        }

        public void CreateAllPlotCubes()
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("BTN_Unlock_"))
                    Destroy(child.gameObject);
            }

            // Xóa hết Cube cũ
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Plot_"))
                    Destroy(child.gameObject);
            }

            if (zoneManager == null || zoneManager.AllZones == null) return;

            foreach (var zone in zoneManager.AllZones)
            {
                CreatePlotCubesForZone(zone);
            }

        }

        private void CreatePlotCubesForZone(ZoneRuntime zone)
        {
            ZoneData zd = zone.zoneData;
            Transform zoneTransform = zone.zoneTransform;
            int totalPlots = zd.columns * zd.rows;

            float plotWidth = zd.cellsPerPlotX * cellSize;
            float plotHeight = zd.cellsPerPlotZ * cellSize;

            float totalW = zd.columns * zd.cellsPerPlotX * cellSize;
            float startX = -totalW / 2f;

            for (int i = 0; i < totalPlots; i++)
            {
                int plotID = i + 1;
                int col = i % zd.columns;
                int row = i / zd.columns;

                float cornerX = startX + col * plotWidth;
                float cornerZ = row * plotHeight;

                Vector3 centerLocal = zoneTransform.right * (cornerX + plotWidth / 2f)
                                   + zoneTransform.forward * (cornerZ + plotHeight / 2f);
                Vector3 centerWorld = zoneTransform.position + centerLocal;
                centerWorld.y = 0.01f;

                GameObject cube = Instantiate(plotCubePrefab, centerWorld, zoneTransform.rotation, transform);
                cube.name = $"Plot_{zone.zoneID}_{plotID}";
                cube.transform.localScale = new Vector3(plotWidth, 0.1f, plotHeight);

                Material mat = GetMaterialForPlot(zone, plotID);
                Renderer r = cube.GetComponent<Renderer>();
                if (r == null) r = cube.GetComponentInChildren<Renderer>();
                if (r != null) r.material = mat;
                // Chỉ tạo nút cho mảnh của Player, chưa mở khóa
                if (zone.zoneID == playerDataService?.PlayerZoneID && !IsPlotUnlocked(zone.zoneID, plotID))
                {
                    CreateUnlockButton(centerWorld, zone.zoneID, plotID);
                }
            }
        }

        private Material GetMaterialForPlot(ZoneRuntime zone, int plotID)
        {
            int playerZoneID = playerDataService?.PlayerZoneID ?? -1;

            if (zone.zoneID == playerZoneID)
            {
                bool unlocked = playerDataService != null && playerDataService.IsPlotUnlocked(zone.zoneID, plotID);
                return unlocked ? playerUnlockedMat : playerLockedMat;
            }

            if (zone.owner == ZoneOwner.Bot)
                return botZoneMat;

            return emptyZoneMat;
        }

        private bool IsPlotUnlocked(int zoneID, int plotID)
        {
            return playerDataService != null && playerDataService.IsPlotUnlocked(zoneID, plotID);
        }

        private void CreateUnlockButton(Vector3 position, int zoneID, int plotID)
        {
            position.y = 0.15f;

            GameObject btnGO = Instantiate(unlockButtonPrefab, position, Quaternion.identity, transform);
            btnGO.name = $"BTN_Unlock_{zoneID}_{plotID}";
            btnGO.AddComponent<Billboard>();

            // Tìm Button ở chính nó HOẶC ở con
            Button btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.GetComponentInChildren<Button>();

            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[BTN] BẤM NÚT MỞ KHÓA: Zone={zoneID}, Plot={plotID}");

                    ILandService landService = ServiceLocator.Get<ILandService>();
                    if (landService != null)
                    {
                        bool result = landService.UnlockPlot(zoneID, plotID);
                        Debug.Log($"[BTN] Kết quả: {result}");
                    }
                    else
                    {
                        Debug.LogError("[BTN] landService NULL!");
                    }
                });
            }
            else
            {
                Debug.LogError($"[BTN] KHÔNG TÌM THẤY Button trong {btnGO.name}!");
            }
        }
    }
}