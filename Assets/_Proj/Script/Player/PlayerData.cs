// Assets/_Project/Scripts/Player/PlayerData.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Class chứa toàn bộ dữ liệu runtime của người chơi.
    /// Được quản lý bởi PlayerDataManager.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // === Tiền và Dầu ===
        public double money;
        public double oilHeld;

        // === Đất đai ===
        public int playerZoneID;
        public Dictionary<int, HashSet<int>> unlockedPlots;

        // === Inventory ===
        public Dictionary<int, int> inventory;

        // === Công trình đã đặt ===
        public List<BuildingRuntimeData> placedBuildings;
        public int nextBuildingID;

        public PlayerData()
        {
            money = 0;
            oilHeld = 0;
            playerZoneID = -1;
            unlockedPlots = new Dictionary<int, HashSet<int>>();
            inventory = new Dictionary<int, int>();
            placedBuildings = new List<BuildingRuntimeData>();
            nextBuildingID = 1;
        }

        public int GetNextBuildingID()
        {
            return nextBuildingID++;
        }

        public bool IsPlotUnlocked(int zoneID, int plotID)
        {
            return unlockedPlots.TryGetValue(zoneID, out HashSet<int> plots) && plots.Contains(plotID);
        }

        public void UnlockPlot(int zoneID, int plotID)
        {
            if (!unlockedPlots.ContainsKey(zoneID))
            {
                unlockedPlots[zoneID] = new HashSet<int>();
            }
            unlockedPlots[zoneID].Add(plotID);
        }

        public int GetInventoryCount(int buildingID)
        {
            inventory.TryGetValue(buildingID, out int count);
            return count;
        }

        public void AddToInventory(int buildingID, int count)
        {
            if (inventory.ContainsKey(buildingID))
            {
                inventory[buildingID] += count;
            }
            else
            {
                inventory[buildingID] = count;
            }
        }

        public bool RemoveFromInventory(int buildingID, int count)
        {
            if (!inventory.TryGetValue(buildingID, out int currentCount) || currentCount < count)
            {
                return false;
            }
            inventory[buildingID] -= count;
            if (inventory[buildingID] <= 0)
            {
                inventory.Remove(buildingID);
            }
            return true;
        }

        public void AddBuilding(BuildingRuntimeData building)
        {
            placedBuildings.Add(building);
        }

        public bool RemoveBuilding(int uniqueID)
        {
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                if (placedBuildings[i].uniqueID == uniqueID)
                {
                    placedBuildings.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public BuildingRuntimeData FindBuilding(int uniqueID)
        {
            return placedBuildings.Find(b => b.uniqueID == uniqueID);
        }
    }

    /// <summary>
    /// Dữ liệu runtime của một công trình đã đặt trên map.
    /// </summary>
    [Serializable]
    public class BuildingRuntimeData
    {
        public int uniqueID;
        public int buildingDataID;
        public int zoneID;
        public int plotID;
        public int gridX;
        public int gridZ;
        public float currentOilInBucket;

        [NonSerialized]
        public GameObject buildingObject;

        public PlacedBuildingSaveData ToSaveData()
        {
            return new PlacedBuildingSaveData
            {
                uniqueBuildingID = this.uniqueID,
                buildingDataID = this.buildingDataID,
                zoneID = this.zoneID,
                plotID = this.plotID,
                gridX = this.gridX,
                gridZ = this.gridZ,
                currentOilInBucket = this.currentOilInBucket
            };
        }

        public static BuildingRuntimeData FromSaveData(PlacedBuildingSaveData saveData)
        {
            return new BuildingRuntimeData
            {
                uniqueID = saveData.uniqueBuildingID,
                buildingDataID = saveData.buildingDataID,
                zoneID = saveData.zoneID,
                plotID = saveData.plotID,
                gridX = saveData.gridX,
                gridZ = saveData.gridZ,
                currentOilInBucket = saveData.currentOilInBucket
            };
        }
    }
}