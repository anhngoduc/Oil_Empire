// Assets/_Project/Scripts/Bot/BotMovement.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    public class BotMovement : MonoBehaviour
    {
        [Header("=== Di chuyển ===")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float stoppingDistance = 1.5f;

        [Header("=== Nhảy ===")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float rayDistance = 1f;
        [SerializeField] private float rayHeight = 0.5f;
        [SerializeField] private int maxJump = 1;

        [Header("=== Nghỉ ===")]
        [SerializeField] private float minRestTime = 1f;
        [SerializeField] private float maxRestTime = 3f;

        [Header("=== Thu dầu ===")]
        [SerializeField] private float collectCooldown = 5f;

        [Header("=== Animation ===")]
        [SerializeField] private Animator anim;

        public System.Action<BotBuildingInfo> OnCollectOil;

        private Rigidbody rb;
        private ZoneManager zoneManager;
        private int botZoneID = -1;
        private Vector3 zoneMin, zoneMax;
        private float zoneMargin = 0.5f;

        // Trạng thái
        private enum BotState { Wandering, MovingToBucket, Resting }
        private BotState currentState = BotState.Wandering;
        private Vector3 targetPosition;
        private float restTimer;

        // Bucket
        private float lastCollectTime = -999f;
        private bool pendingCollect = false;
        private BotBuildingInfo pendingBucketInfo;
        private Transform pendingBucketTransform;

        // Nhảy
        private int jumpCount = 0;
        private bool isGrounded = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.useGravity = true;

            zoneManager = FindObjectOfType<ZoneManager>();
            if (botZoneID < 0) TryGetZoneFromData();
            CalculateZoneBounds();
            PickNewWanderTarget();
        }

        private void TryGetZoneFromData()
        {
            BotData data = GetComponent<BotData>();
            if (data != null) botZoneID = data.zoneID;
        }

        private void Update()
        {
            CheckObstacle();

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
                    MoveTowardsTarget();
                else
                    rb.velocity = Vector3.zero;
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
            if (pendingBucketTransform == null)
            {
                pendingCollect = false;
                currentState = BotState.Wandering;
                PickNewWanderTarget();
                return;
            }
            targetPosition = pendingBucketTransform.position;
        }

        public void OnBucketFull(Transform bucketTransform, BotBuildingInfo bucketInfo)
        {
            if (Time.time - lastCollectTime < collectCooldown) return;
            if (pendingCollect) return;

            pendingBucketInfo = bucketInfo;
            pendingBucketTransform = bucketTransform;
            pendingCollect = true;
        }

        // ==================== RESTING ====================
        private void StartResting()
        {
            currentState = BotState.Resting;
            restTimer = Random.Range(minRestTime, maxRestTime);
            if (anim != null) anim.SetBool("Run", false);
        }

        private void RestUpdate()
        {
            restTimer -= Time.deltaTime;
            if (restTimer <= 0f)
            {
                if (pendingCollect && pendingBucketTransform != null)
                    currentState = BotState.MovingToBucket;
                else
                {
                    currentState = BotState.Wandering;
                    PickNewWanderTarget();
                }
            }
        }

        // ==================== TRIGGER ====================
        private void OnTriggerEnter(Collider other)
        {
            if (pendingCollect && pendingBucketTransform != null && other.transform == pendingBucketTransform)
            {
                OnCollectOil?.Invoke(pendingBucketInfo);
                lastCollectTime = Time.time;
                pendingCollect = false;
                pendingBucketInfo = null;
                pendingBucketTransform = null;
                StartResting();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.transform.IsChildOf(transform) || other.gameObject == gameObject) return;
            isGrounded = true;
            jumpCount = 0;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.IsChildOf(transform) || other.gameObject == gameObject) return;
            isGrounded = false;
            if (jumpCount == 0) jumpCount = 1;
        }

        // ==================== NHẢY ====================
        private void CheckObstacle()
        {
            if (currentState != BotState.Wandering && currentState != BotState.MovingToBucket) return;
            if (rb.velocity.magnitude < 0.1f) return;
            if (jumpCount >= maxJump) return;

            Vector3 rayOrigin = transform.position + Vector3.up * rayHeight;
            Ray ray = new Ray(rayOrigin, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                if (!hit.collider.isTrigger)
                {
                    rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
                    jumpCount++;
                    if (anim != null) anim.SetTrigger("Jump");
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

        // ==================== ZONE ====================
        public void SetZoneID(int zoneID)
        {
            botZoneID = zoneID;
            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
            CalculateZoneBounds();
            PickNewWanderTarget();
        }

        private void CalculateZoneBounds()
        {
            if (zoneManager == null) return;
            Transform t = zoneManager.GetZoneTransform(botZoneID);
            ZoneData zd = zoneManager.GetZone(botZoneID)?.zoneData;
            if (t == null || zd == null) return;

            float w = zd.TotalCellsX * 1f;
            float h = zd.TotalCellsZ * 1f;
            Vector3 p = t.position;
            Vector3 r = t.right;
            Vector3 f = t.forward;

            Vector3 c1 = p;
            Vector3 c2 = p + r * w;
            Vector3 c3 = p + f * h;
            Vector3 c4 = p + r * w + f * h;

            zoneMin = new Vector3(Mathf.Min(c1.x, c2.x, c3.x, c4.x), p.y, Mathf.Min(c1.z, c2.z, c3.z, c4.z));
            zoneMax = new Vector3(Mathf.Max(c1.x, c2.x, c3.x, c4.x), p.y, Mathf.Max(c1.z, c2.z, c3.z, c4.z));
        }

        private float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((zoneMin + zoneMax) / 2f, new Vector3(zoneMax.x - zoneMin.x, 1f, zoneMax.z - zoneMin.z));
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPosition, 0.3f);

            // Vẽ ray obstacle
            Gizmos.color = Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * rayHeight;
            Gizmos.DrawRay(rayOrigin, transform.forward * rayDistance);
        }
    }
}