// Assets/_Project/Scripts/Data/BotSimulationConfig.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    [CreateAssetMenu(fileName = "BotSimulationConfig", menuName = "OilGame/BotSimulationConfig")]
    public class BotSimulationConfig : ScriptableObject
    {
        [Header("=== Tỉ lệ xuất hiện bot ===")]
        [Range(0, 100)] public float botAppearanceChance = 70f;

        [Header("=== Mảnh đất bot mở khóa ===")]
        [Min(1)] public int minPlotsUnlocked = 1;
        [Min(0)] public int maxPlotsUnlocked = 0;

        [Header("=== Công trình bot ===")]
        [Min(0)] public int minDrillCount = 2;
        [Min(0)] public int minBucketCount = 2;
        [Range(10, 100)] public float cellUsagePercentage = 50f;

        [Header("=== Cấp độ công trình bot ===")]
        public List<LevelWeight> drillLevelWeights;
        public List<LevelWeight> bucketLevelWeights;

        [Header("=== Thời gian mô phỏng ===")]
        [Range(0.5f, 5f)] public float simulationTickInterval = 1f;

        [Header("=== Phát triển bot ===")]
        public bool enableBotProgression = false;
        [Range(10f, 300f)] public float progressionCheckInterval = 60f;

        public int GetRandomLevel(List<LevelWeight> weights)
        {
            if (weights == null || weights.Count == 0) return 1;
            float totalWeight = 0f;
            foreach (var w in weights) totalWeight += w.weight;
            float rand = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var w in weights)
            {
                cumulative += w.weight;
                if (rand <= cumulative) return w.level;
            }
            return weights[weights.Count - 1].level;
        }
    }

    [System.Serializable]
    public class LevelWeight
    {
        [Range(1, 5)] public int level = 1;
        [Min(0f)] public float weight = 1f;
    }
}