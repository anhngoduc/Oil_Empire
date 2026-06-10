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
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLandUnlocked>(OnLandUnlocked);
        }

        private void OnLandUnlocked(OnLandUnlocked evt)
        {
            // Xóa nút mở khóa
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
            // Xóa nút cũ
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("BTN_Unlock_"))
                    Destroy(child.gameObject);
            }

            // Xóa Cube cũ
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
                cube.transform.localScale = new Vector3(plotWidth, plotCubePrefab.transform.localScale.y, plotHeight);

                // Gán texture từ PlotInfo
                PlotInfo plotInfo = zd.GetPlot(plotID);
                if (plotInfo != null && plotInfo.plotTexture != null)
                {
                    Renderer r = cube.GetComponent<Renderer>();
                    if (r == null) r = cube.GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        MaterialPropertyBlock block = new MaterialPropertyBlock();
                        r.GetPropertyBlock(block);
                        block.SetTexture("_MainTex", plotInfo.plotTexture);
                        r.SetPropertyBlock(block);
                    }
                }

                // Tạo nút mở khóa cho Player, chưa mở
                if (zone.zoneID == playerDataService?.PlayerZoneID && !IsPlotUnlocked(zone.zoneID, plotID))
                {
                    CreateUnlockButton(centerWorld, zone.zoneID, plotID);
                }
            }
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

            Button btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.GetComponentInChildren<Button>();

            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    ILandService landService = ServiceLocator.Get<ILandService>();
                    if (landService != null)
                    {
                        landService.UnlockPlot(zoneID, plotID);
                    }
                });
            }
        }
    }
}