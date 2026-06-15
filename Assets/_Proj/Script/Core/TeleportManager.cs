// Assets/_Project/Scripts/Core/TeleportManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// Singleton quản lý danh sách điểm dịch chuyển cho Player và Bot.
    /// </summary>
    public class TeleportManager : MonoBehaviour
    {
        public static TeleportManager Instance { get; private set; }

        [Header("=== Điểm thêm bằng tay ===")]
        [SerializeField] private List<Transform> customPoints;
        [SerializeField] private ZoneManager zm;

        private List<Transform> allPoints;

        private void Awake()
        {
            Instance = this;
            allPoints = new List<Transform>();
        }

        private void Start()
        {
            EventBus.Subscribe<OnGameReady>(OnGameReady);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnGameReady>(OnGameReady);
        }

        private void OnGameReady(OnGameReady evt)
        {
            IPlayerDataService data = ServiceLocator.Get<IPlayerDataService>();
            ZoneManager zm = FindObjectOfType<ZoneManager>();

            if (data != null && zm != null)
            {
                Transform homePoint = zm.GetZoneTransform(data.PlayerZoneID);
                if (homePoint != null)
                    allPoints.Add(homePoint);
            }

            foreach (var p in customPoints)
            {
                if (p != null) allPoints.Add(p);
            }

            Debug.Log($"[TeleportManager] Tổng điểm: {allPoints.Count}");

            TeleportPlayerTo(0);
        }

        /// <summary>
        /// Lấy điểm theo index.
        /// </summary>
        public Transform GetPoint(int index)
        {
            if (index >= 0 && index < allPoints.Count)
                return allPoints[index];
            return null;
        }

        /// <summary>
        /// Random 1 điểm cho Bot.
        /// </summary>
        public Vector3? GetRandomPoint()
        {
            if (allPoints.Count == 0) return null;
            int index = Random.Range(0, allPoints.Count);
            return allPoints[index].position;
        }

        /// <summary>
        /// Dịch chuyển Player đến điểm index.
        /// </summary>
        public void TeleportPlayerTo(int index)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            Transform target = GetPoint(index);
            if (target != null)
                player.transform.position = target.position + Vector3.up * .1f;
        }
    }
}