// Assets/_Project/Scripts/Data/BuildingDatabase.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "OilGame/BuildingDatabase")]
    public class BuildingDatabase : ScriptableObject
    {
        public List<BuildingData> allBuildings;

        private Dictionary<int, BuildingData> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, BuildingData>();
            foreach (var data in allBuildings)
            {
                if (data == null) continue;
                if (!lookupTable.ContainsKey(data.buildingID))
                {
                    lookupTable.Add(data.buildingID, data);
                }
            }
        }

        public BuildingData GetByID(int buildingID)
        {
            if (lookupTable == null) Initialize();
            lookupTable.TryGetValue(buildingID, out BuildingData data);
            return data;
        }

        public List<BuildingData> GetByType(BuildingType type)
        {
            List<BuildingData> result = new List<BuildingData>();
            foreach (var data in allBuildings)
            {
                if (data != null && data.buildingType == type)
                {
                    result.Add(data);
                }
            }
            result.Sort((a, b) => a.level.CompareTo(b.level));
            return result;
        }
    }
}