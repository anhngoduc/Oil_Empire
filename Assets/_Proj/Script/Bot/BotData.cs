// Assets/_Project/Scripts/Bot/BotData.cs

using System.Collections.Generic;

namespace OilGame
{
    public class BotData
    {
        public int botID;
        public int zoneID;
        public string botName;
        public List<int> unlockedPlotIDs;
        public List<BotBuildingInfo> buildings;
        public double money;
        public float totalOilInBuckets;
        public float totalProductionRate;
        public float totalBucketCapacity;
        public BotMovement botMovement;

        public BotData()
        {
            botID = 0;
            zoneID = -1;
            botName = "Bot";
            unlockedPlotIDs = new List<int>();
            buildings = new List<BotBuildingInfo>();
            money = 0;
            totalOilInBuckets = 0f;
            totalProductionRate = 0f;
            totalBucketCapacity = 0f;
        }

        public void RecalculateStats(BuildingDatabase database)
        {
            totalProductionRate = 0f;
            totalBucketCapacity = 0f;
            foreach (var building in buildings)
            {
                BuildingData data = database.GetByID(building.buildingDataID);
                if (data == null) continue;
                if (data.buildingType == BuildingType.Drill)
                    totalProductionRate += data.productionRate;
                else if (data.buildingType == BuildingType.Bucket)
                    totalBucketCapacity += data.capacity;
            }
        }
    }

    [System.Serializable]
    public class BotBuildingInfo
    {
        public int buildingDataID;
        public int plotID;
        public int gridX;
        public int gridZ;
        public float currentOil;
    }
}