// Assets/_Project/Scripts/Land/GridManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    public class GridManager : MonoBehaviour, IGridService
    {
        [Header("Tham chiếu")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private ZoneManager zoneManager;

        private Dictionary<string, GridCell[,]> gridData;
        private float cellSize;

        private void Awake()
        {
            ServiceLocator.Register<IGridService>(this);
            gridData = new Dictionary<string, GridCell[,]>();
        }

        private void Start()
        {
            cellSize = gameConfig != null ? gameConfig.cellSize : 1f;
            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();


            if (zoneManager != null && zoneManager.AllZones.Count == 0)
            {
                zoneManager.Initialize();
            }
        }

        private void OnDestroy() => ServiceLocator.Unregister<IGridService>();

        public void InitializeGridForPlot(int zoneID, int plotID)
        {
            string key = $"{zoneID}_{plotID}";
            if (gridData.ContainsKey(key)) return;

            ZoneData zd = zoneManager.GetZone(zoneID)?.zoneData;
            if (zd == null) return;

            GridCell[,] grid = new GridCell[zd.cellsPerPlotX, zd.cellsPerPlotZ];
            for (int x = 0; x < zd.cellsPerPlotX; x++)
                for (int z = 0; z < zd.cellsPerPlotZ; z++)
                    grid[x, z] = new GridCell();

            gridData[key] = grid;
        }

        private string MakeKey(int z, int p) => $"{z}_{p}";

        public Vector3 GridToWorld(int zoneID, int plotID, int gridX, int gridZ)
        {
            Transform t = zoneManager.GetZoneTransform(zoneID);
            ZoneData zd = zoneManager.GetZone(zoneID)?.zoneData;
            if (t == null || zd == null) return Vector3.zero;

            float totalW = zd.TotalCellsX * cellSize;
            float startX = -totalW / 2f;

            int plotIndex = plotID - 1;
            int col = plotIndex % zd.columns;
            int row = plotIndex / zd.columns;

            float offsetX = startX + col * zd.cellsPerPlotX * cellSize + gridX * cellSize + cellSize * 0.5f;
            float offsetZ = row * zd.cellsPerPlotZ * cellSize + gridZ * cellSize + cellSize * 0.5f;

            return t.position + t.right * offsetX + t.forward * offsetZ;
        }

        public bool WorldToGrid(Vector3 worldPos, out int zoneID, out int plotID, out int gridX, out int gridZ)
        {
            zoneID = -1; plotID = -1; gridX = -1; gridZ = -1;

            foreach (var zone in zoneManager.AllZones)
            {
                Transform t = zone.zoneTransform;
                ZoneData zd = zone.zoneData;

                // TÍNH startX TRƯỚC KHI DÙNG
                float totalW = zd.TotalCellsX * cellSize;
                float totalH = zd.TotalCellsZ * cellSize;
                float startX = -totalW / 2f;

                Vector3 localPos = worldPos - t.position;
                float localX = Vector3.Dot(localPos, t.right) - startX;
                float localZ = Vector3.Dot(localPos, t.forward);

                if (localX >= 0 && localX <= totalW && localZ >= 0 && localZ <= totalH)
                {
                    int globalX = Mathf.FloorToInt(localX / cellSize);
                    int globalZ = Mathf.FloorToInt(localZ / cellSize);
                    globalX = Mathf.Clamp(globalX, 0, zd.TotalCellsX - 1);
                    globalZ = Mathf.Clamp(globalZ, 0, zd.TotalCellsZ - 1);

                    int col = globalX / zd.cellsPerPlotX;
                    int row = globalZ / zd.cellsPerPlotZ;
                    plotID = row * zd.columns + col + 1;
                    gridX = globalX % zd.cellsPerPlotX;
                    gridZ = globalZ % zd.cellsPerPlotZ;
                    zoneID = zone.zoneID;
                    return true;
                }
            }
            return false;
        }

        public bool CanPlace(int zoneID, int plotID, int gridX, int gridZ)
        {
            string key = MakeKey(zoneID, plotID);

            if (!gridData.TryGetValue(key, out GridCell[,] grid))
            {
                InitializeGridForPlot(zoneID, plotID);
                bool hasGrid = gridData.TryGetValue(key, out grid);
            }

            if (grid == null)
            {
                Debug.LogError($"[CanPlace] Grid {key} NULL!");
                return false;
            }

            int maxX = grid.GetLength(0);
            int maxZ = grid.GetLength(1);

            if (gridX < 0 || gridX >= maxX || gridZ < 0 || gridZ >= maxZ)
            {
                Debug.LogError($"[CanPlace] NGOÀI LƯỚI! gridX={gridX}/{maxX}, gridZ={gridZ}/{maxZ}");
                return false;
            }

            bool occupied = grid[gridX, gridZ].state == CellState.Occupied;
            return !occupied;
        }

        public void OccupyCell(int zoneID, int plotID, int gridX, int gridZ, int buildingID, BuildingType type)
        {
            string key = MakeKey(zoneID, plotID);
            if (!gridData.TryGetValue(key, out GridCell[,] grid)) return;
            grid[gridX, gridZ].state = CellState.Occupied;
            grid[gridX, gridZ].occupyingBuildingID = buildingID;
        }

        public void FreeCell(int zoneID, int plotID, int gridX, int gridZ)
        {
            string key = MakeKey(zoneID, plotID);
            if (!gridData.TryGetValue(key, out GridCell[,] grid)) return;
            grid[gridX, gridZ].state = CellState.Empty;
            grid[gridX, gridZ].occupyingBuildingID = -1;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (zoneManager == null || zoneManager.AllZones == null) return;
            if (gameConfig == null) return;

            float cs = gameConfig.cellSize;

            IPlayerDataService playerData = ServiceLocator.Get<IPlayerDataService>();
            int playerZoneID = playerData != null ? playerData.PlayerZoneID : -1;

            foreach (var zone in zoneManager.AllZones)
            {
                if (zone == null || zone.zoneData == null || zone.zoneTransform == null) continue;

                ZoneData zd = zone.zoneData;
                Transform t = zone.zoneTransform;
                bool isPlayerZone = zone.zoneID == playerZoneID;

                float totalW = zd.TotalCellsX * cs;
                float startX = -totalW / 2f;

                for (int row = 0; row < zd.rows; row++)
                {
                    for (int col = 0; col < zd.columns; col++)
                    {
                        int plotIndex = row * zd.columns + col;
                        int plotID = plotIndex + 1;

                        bool isUnlocked = false;
                        if (isPlayerZone && playerData != null)
                            isUnlocked = playerData.IsPlotUnlocked(zone.zoneID, plotID);
                        else if (zone.unlockedPlotIDs != null)
                            isUnlocked = zone.unlockedPlotIDs.Contains(plotID);

                        for (int x = 0; x < zd.cellsPerPlotX; x++)
                        {
                            for (int z = 0; z < zd.cellsPerPlotZ; z++)
                            {
                                float wx = startX + col * zd.cellsPerPlotX * cs + x * cs + cs * 0.5f;
                                float wz = row * zd.cellsPerPlotZ * cs + z * cs + cs * 0.5f;
                                Vector3 worldPos = t.position + t.right * wx + t.forward * wz;

                                if (isPlayerZone && isUnlocked)
                                    Gizmos.color = Color.green;
                                else if (isPlayerZone && !isUnlocked)
                                    Gizmos.color = Color.yellow;
                                else
                                    Gizmos.color = Color.red;

                                Gizmos.DrawSphere(worldPos, cs * 0.15f);
                            }
                        }
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class GridCell
    {
        public CellState state;
        public int occupyingBuildingID = -1;
    }
}