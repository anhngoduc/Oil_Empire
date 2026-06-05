// Assets/_Project/Scripts/Land/PlotVisualizer.cs

using UnityEngine;
using System.Collections;

namespace OilGame
{
    public class PlotVisualizer : MonoBehaviour
    {
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

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);

            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            GameConfig config = FindObjectOfType<GameConfig>();
            cellSize = config != null ? config.cellSize : 1f;

            if (zoneManager != null && zoneManager.AllZones.Count == 0)
                zoneManager.Initialize();

            CreateAllPlotCubes();

            EventBus.Subscribe<OnLandUnlocked>(OnLandUnlocked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLandUnlocked>(OnLandUnlocked);
        }

        private void OnLandUnlocked(OnLandUnlocked evt)
        {
            Debug.Log($"[PlotVisualizer] OnLandUnlocked: Zone={evt.zoneID}, Plot={evt.plotID}");

            foreach (Transform child in transform)
            {
                if (child.name == $"Plot_{evt.zoneID}_{evt.plotID}")
                {
                    Renderer r = child.GetComponent<Renderer>();
                    if (r == null) r = child.GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        r.material = playerUnlockedMat;
                        Debug.Log($"[PlotVisualizer] ĐÃ ĐỔI MÀU: {child.name}");
                    }
                    return;
                }
            }
        }

        public void CreateAllPlotCubes()
        {
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

            for (int i = 0; i < totalPlots; i++)
            {
                int plotID = i + 1;
                int col = i % zd.columns;
                int row = i / zd.columns;

                float cornerX = col * plotWidth;
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
    }
}