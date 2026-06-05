// Assets/_Project/Scripts/Data/BuildingData.cs

using UnityEngine;

namespace OilGame
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "OilGame/BuildingData")]
    public class BuildingData : ScriptableObject
    {
        [Header("=== Định danh ===")]
        public int buildingID;
        public string buildingName;
        public BuildingType buildingType;
        [Range(1, 5)] public int level;

        [Header("=== Kinh tế ===")]
        public double price;

        [Header("=== Chỉ số ===")]
        public float productionRate;
        public float capacity;

        [Header("=== Hiển thị ===")]
        public GameObject prefab;
        public Sprite icon;
        [TextArea(2, 4)] public string description;

        public string GetStatDisplay()
        {
            if (buildingType == BuildingType.Drill)
                return $"{productionRate} Oil/sec";
            else
                return $"Capacity: {capacity:F0} Oil";
        }

        public string GetPriceDisplay()
        {
            return $"${price:N0}";
        }

        public BuildingRuntimeData CreateRuntimeData(int uniqueID, int zoneID, int plotID, int gridX, int gridZ)
        {
            return new BuildingRuntimeData
            {
                uniqueID = uniqueID,
                buildingDataID = this.buildingID,
                zoneID = zoneID,
                plotID = plotID,
                gridX = gridX,
                gridZ = gridZ,
                currentOilInBucket = 0f
            };
        }
    }
}