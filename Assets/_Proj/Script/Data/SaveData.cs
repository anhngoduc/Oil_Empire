// Assets/_Project/Scripts/Data/SaveData.cs

using System;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// Class gốc chứa toàn bộ dữ liệu save của người chơi.
    /// KHÔNG chứa dữ liệu Bot Simulation.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public double money;
        public double oilHeld;
        public int playerZoneID;
        public List<ZonePlotPair> unlockedPlots;
        public List<InventorySaveItem> inventoryItems;
        public List<PlacedBuildingSaveData> placedBuildings;
        public float currentOilPrice;
        public int saveVersion;
        public long saveTimestamp;

        public SaveData()
        {
            money = 0;
            oilHeld = 0;
            playerZoneID = -1;
            unlockedPlots = new List<ZonePlotPair>();
            inventoryItems = new List<InventorySaveItem>();
            placedBuildings = new List<PlacedBuildingSaveData>();
            currentOilPrice = 10f;
            saveVersion = 1;
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void UpdateTimestamp()
        {
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public DateTime GetSaveDateTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(saveTimestamp).UtcDateTime;
        }
    }

    [Serializable]
    public struct ZonePlotPair
    {
        public int zoneID;
        public int plotID;

        public ZonePlotPair(int zoneID, int plotID)
        {
            this.zoneID = zoneID;
            this.plotID = plotID;
        }
    }

    [Serializable]
    public struct InventorySaveItem
    {
        public int buildingID;
        public int quantity;

        public InventorySaveItem(int buildingID, int quantity)
        {
            this.buildingID = buildingID;
            this.quantity = quantity;
        }
    }

    [Serializable]
    public class PlacedBuildingSaveData
    {
        public int uniqueBuildingID;
        public int buildingDataID;
        public int zoneID;
        public int plotID;
        public int gridX;
        public int gridZ;
        public long currentOilInBucket;
    }
}