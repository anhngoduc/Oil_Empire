// Assets/_Project/Scripts/Land/ZoneManager.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// ZoneManager - Quản lý vị trí các Zone trong scene.
    /// Gắn vào 1 GameObject duy nhất, kéo các điểm Zone vào list.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        [Header("=== Cấu hình Zone ===")]
        [Tooltip("Danh sách ZoneData cho từng Zone (theo thứ tự).")]
        public List<ZoneData> zoneDatas;

        [Tooltip("Danh sách Transform đánh dấu vị trí + hướng từng Zone.")]
        public List<Transform> zonePoints;

        [Header("=== Tham chiếu ===")]
        [Tooltip("GameConfig để lấy cellSize.")]
        public GameConfig gameConfig;

        /// <summary>Dữ liệu runtime của tất cả Zone.</summary>
        public List<ZoneRuntime> AllZones { get; private set; }

        /// <summary>Dictionary tra cứu nhanh Zone theo zoneID.</summary>
        private Dictionary<int, ZoneRuntime> zoneLookup;

        private void Awake()
        {
            AllZones = new List<ZoneRuntime>();
            zoneLookup = new Dictionary<int, ZoneRuntime>();
        }

        /// <summary>
        /// Khởi tạo tất cả Zone từ zonePoints và zoneDatas.
        /// </summary>
        public void Initialize()
        {
            AllZones.Clear();
            zoneLookup.Clear(); 

            int count = Mathf.Min(zoneDatas.Count, zonePoints.Count);

            for (int i = 0; i < count; i++)
            {
                if (zoneDatas[i] == null || zonePoints[i] == null) continue;

                ZoneRuntime zone = new ZoneRuntime
                {
                    zoneID = zoneDatas[i].zoneID,
                    zoneData = zoneDatas[i],
                    zoneTransform = zonePoints[i],
                    owner = ZoneOwner.Empty,
                    unlockedPlotIDs = new HashSet<int>()
                };

                AllZones.Add(zone);
                zoneLookup[zone.zoneID] = zone;
            }
        }

        public ZoneRuntime GetZone(int zoneID)
        {
            zoneLookup.TryGetValue(zoneID, out ZoneRuntime zone);
            return zone;
        }

        public Transform GetZoneTransform(int zoneID)
        {
            var zone = GetZone(zoneID);
            return zone?.zoneTransform;
        }

        public int TotalZones => AllZones.Count;
    }
}