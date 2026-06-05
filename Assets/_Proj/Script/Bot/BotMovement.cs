// Assets/_Project/Scripts/Bot/BotMovement.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// BotMovement - Gắn vào Prefab Bot. Tự động di chuyển trong Zone và thu dầu khi Bucket đầy.
    /// </summary>
    public class BotMovement : MonoBehaviour
    {
        [Header("=== Di chuyển ===")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float stoppingDistance = 1.5f;

        [Header("=== Nghỉ ===")]
        [SerializeField] private float minRestTime = 1f;
        [SerializeField] private float maxRestTime = 3f;

        [Header("=== Zone ===")]
        [SerializeField] private int botZoneID = -1;

        [Header("=== Animation ===")]
        [SerializeField] private Animator anim;
        public System.Action<BotBuildingInfo> OnCollectOil;

        // Trạng thái
        private enum BotState { Wandering, MovingToBucket, Resting }
        private BotState currentState = BotState.Wandering;

        // Di chuyển
        private Rigidbody rb;
        private Vector3 targetPosition;
        private float restTimer;

        // Bucket
        private List<Transform> fullBuckets = new List<Transform>();
        private Dictionary<Transform, BotBuildingInfo> currentBucketInfoMap = new Dictionary<Transform, BotBuildingInfo>();
        private Transform currentBucketTarget;

        // Zone giới hạn
        private ZoneManager zoneManager;
        private Vector3 zoneMin;
        private Vector3 zoneMax;
        private float zoneMargin = 0.5f;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.useGravity = true;

            zoneManager = FindObjectOfType<ZoneManager>();

            if (botZoneID < 0)
            {
                // Tự tìm Zone từ BotData
                BotData botData = GetComponent<BotData>();
                if (botData != null) botZoneID = botData.zoneID;
            }

            CalculateZoneBounds();
            PickNewWanderTarget();
            Debug.Log($"[BotMovement] Bot {botZoneID} STARTED - zoneMin={zoneMin}, zoneMax={zoneMax}");
        }

        private void Update()
        {
            switch (currentState)
            {
                case BotState.Wandering:
                    WanderUpdate();
                    break;
                case BotState.MovingToBucket:
                    MoveToBucketUpdate();
                    break;
                case BotState.Resting:
                    RestUpdate();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (currentState == BotState.Wandering || currentState == BotState.MovingToBucket)
            {
                float dist = DistanceXZ(transform.position, targetPosition);
                if (dist > stoppingDistance)
                {
                    MoveTowardsTarget();
                }
                else
                {
                    rb.velocity = Vector3.zero;
                }
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }

        // ==================== WANDERING ====================

        private void WanderUpdate()
        {
            float dist = DistanceXZ(transform.position, targetPosition);
            if (dist <= stoppingDistance)
            {
                rb.velocity = Vector3.zero; 
                StartResting();
            }
        }
        private void PickNewWanderTarget()
        {
            if (zoneMin == zoneMax) return;

            float x = Random.Range(zoneMin.x + zoneMargin, zoneMax.x - zoneMargin);
            float z = Random.Range(zoneMin.z + zoneMargin, zoneMax.z - zoneMargin);
            targetPosition = new Vector3(x, transform.position.y, z);
        }

        // ==================== MOVE TO BUCKET ====================

        private void MoveToBucketUpdate()
        {
            if (currentBucketTarget == null)
            {
                rb.velocity = Vector3.zero; // ✅ dừng ngay
                fullBuckets.Remove(null);
                currentState = BotState.Wandering;
                PickNewWanderTarget();
                return;
            }

            targetPosition = currentBucketTarget.position;
        }

        private Transform GetNearestFullBucket()
        {
            fullBuckets.RemoveAll(b => b == null);

            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var bucket in fullBuckets)
            {
                float d = Vector3.Distance(transform.position, bucket.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = bucket;
                }
            }
            return nearest;
        }

        // ==================== RESTING ====================

        private void StartResting()
        {
            currentState = BotState.Resting;
            restTimer = Random.Range(minRestTime, maxRestTime);

            // ANIM: đứng
            if (anim != null) anim.SetBool("Run", false);
        }

        private void RestUpdate()
        {
            restTimer -= Time.deltaTime;
            if (restTimer <= 0f)
            {
                // ✅ Ưu tiên đi lấy bucket trước khi wander
                currentBucketTarget = GetNearestFullBucket();
                if (currentBucketTarget != null)
                {
                    currentState = BotState.MovingToBucket;
                }
                else
                {
                    currentState = BotState.Wandering;
                    PickNewWanderTarget();
                }
            }
        }

        // ==================== DI CHUYỂN ====================

        private void MoveTowardsTarget()
        {
            Vector3 dir = (targetPosition - transform.position);
            dir.y = 0;
            dir.Normalize();

            rb.velocity = new Vector3(dir.x * moveSpeed, rb.velocity.y, dir.z * moveSpeed);

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

            if (anim != null) anim.SetBool("Run", true);
        }

        // ==================== ZONE GIỚI HẠN ====================
        public void SetZoneID(int zoneID)
        {
            this.botZoneID = zoneID;

            if (zoneManager == null)
                zoneManager = FindObjectOfType<ZoneManager>();

            CalculateZoneBounds();

            PickNewWanderTarget();
        }
        private void CalculateZoneBounds()
        {
            if (zoneManager == null) return;

            Transform zonePoint = zoneManager.GetZoneTransform(botZoneID);
            ZoneData zd = zoneManager.GetZone(botZoneID)?.zoneData;
            if (zonePoint == null || zd == null) return;

            float totalW = zd.TotalCellsX * 1f;
            float totalH = zd.TotalCellsZ * 1f;

            Vector3 corner1 = zonePoint.position;
            Vector3 corner2 = zonePoint.position + zonePoint.right * totalW;
            Vector3 corner3 = zonePoint.position + zonePoint.forward * totalH;
            Vector3 corner4 = zonePoint.position + zonePoint.right * totalW + zonePoint.forward * totalH;

            zoneMin = new Vector3(
                Mathf.Min(corner1.x, corner2.x, corner3.x, corner4.x),
                corner1.y,
                Mathf.Min(corner1.z, corner2.z, corner3.z, corner4.z)
            );
            zoneMax = new Vector3(
                Mathf.Max(corner1.x, corner2.x, corner3.x, corner4.x),
                corner1.y,
                Mathf.Max(corner1.z, corner2.z, corner3.z, corner4.z)
            );
        }

        // ==================== BUCKET ĐẦY ====================

        /// <summary>
        /// Gọi từ BotSimulationManager khi Bucket của Bot này đầy.
        /// </summary>
        // ✅ Thêm tham số BotBuildingInfo
        public void OnBucketFull(Transform bucketTransform, BotBuildingInfo info)
        {
            if (!fullBuckets.Contains(bucketTransform))
            {
                fullBuckets.Add(bucketTransform);
                currentBucketInfoMap[bucketTransform] = info; // ✅ lưu map
            }

            if (currentState == BotState.Wandering)
            {
                currentBucketTarget = GetNearestFullBucket();
                if (currentBucketTarget != null)
                    currentState = BotState.MovingToBucket;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentBucketTarget != null && other.transform == currentBucketTarget)
            {
                // ✅ Chạm trigger bucket → thu dầu
                if (currentBucketInfoMap.ContainsKey(currentBucketTarget))
                    OnCollectOil?.Invoke(currentBucketInfoMap[currentBucketTarget]);

                currentBucketInfoMap.Remove(currentBucketTarget);
                fullBuckets.Remove(currentBucketTarget);
                currentBucketTarget = null;
                StartResting();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (zoneManager == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                (zoneMin + zoneMax) / 2f,
                new Vector3(zoneMax.x - zoneMin.x, 1f, zoneMax.z - zoneMin.z)
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPosition, 0.3f);
        }
        private float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}