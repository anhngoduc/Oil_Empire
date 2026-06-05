// Assets/_Project/Scripts/Data/GameConfig.cs

using UnityEngine;

namespace OilGame
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "OilGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== Save/Load ===")]
        public float autoSaveInterval = 60f;
        public bool saveOnQuit = true;
        public string saveFileName = "oilgame_save.dat";

        [Header("=== Market ===")]
        public float minOilPrice = 5f;
        public float maxOilPrice = 20f;
        public float priceUpdateInterval = 30f;

        [Header("=== Grid & Camera ===")]
        public float cellSize = 1f;
        public float raycastDistance = 200f;

        [Header("=== References ===")]
        public LayerMask groundLayer;
        public BuildingDatabase buildingDatabase;
    }
}