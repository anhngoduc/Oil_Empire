// Assets/_Project/Scripts/Buildings/OilPipe.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Gắn vào Prefab Drill. Vẽ đường ống dầu đến Bucket đang được bơm.
    /// </summary>
    public class OilPipe : MonoBehaviour
    {
        [Header("Cấu hình")]
        [SerializeField] private float pipeWidth = 0.1f;
        [SerializeField] private float flowSpeed = 2f;
        [SerializeField] private Color pipeColor = Color.black;
        [SerializeField] private Color flowColor = Color.yellow;

        private LineRenderer lineRenderer;
        private Building myBuilding;
        private Transform targetBucket;
        private int activeBucketID = -1;
        private Material pipeMaterial;

        private void Awake()
        {
            myBuilding = GetComponent<Building>();
            if (myBuilding == null || myBuilding.Type != BuildingType.Drill) return;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = pipeWidth;
            lineRenderer.endWidth = pipeWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.enabled = false;
        }

        private void Start()
        {
            EventBus.Subscribe<OnActiveBucketChanged>(OnActiveBucketChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnActiveBucketChanged>(OnActiveBucketChanged);
        }

        private void OnActiveBucketChanged(OnActiveBucketChanged evt)
        {
            if (myBuilding == null) return;

            if (evt.zoneActiveBuckets != null && evt.zoneActiveBuckets.TryGetValue(myBuilding.ZoneID, out int? bucketID))
            {
                if (bucketID == null || bucketID == 0)
                {
                    lineRenderer.enabled = false;
                    targetBucket = null;
                    return;
                }
                activeBucketID = bucketID.Value;
                UpdateTarget();
            }
        }

        private void UpdateTarget()
        {
            if (activeBucketID == 0)
            {
                lineRenderer.enabled = false;
                targetBucket = null;
                return;
            }

            // Thử tìm trong BuildingService (building player)
            IBuildingService bs = ServiceLocator.Get<IBuildingService>();
            Building bucket = bs?.GetBuildingByID(activeBucketID);
            if (bucket != null)
            {
                targetBucket = bucket.transform;
                lineRenderer.enabled = true;
                return;
            }

            // Tìm building bot (fakeID âm, không có trong BuildingService)
            Building[] allBuildings = FindObjectsOfType<Building>();
            foreach (var b in allBuildings)
            {
                if (b.UniqueID == activeBucketID)
                {
                    targetBucket = b.transform;
                    lineRenderer.enabled = true;
                    return;
                }
            }

            lineRenderer.enabled = false;
            targetBucket = null;
        }

        private void Update()
        {
            if (targetBucket == null || !lineRenderer.enabled) return;

            // Vẽ ống
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Vector3 end = targetBucket.position + Vector3.up * 0.5f;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Tạo hiệu ứng dòng chảy bằng Gradient
            Gradient gradient = new Gradient();
            float t = Mathf.PingPong(Time.time * flowSpeed, 1f);

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(pipeColor, 0f);
            colorKeys[1] = new GradientColorKey(flowColor, t);
            colorKeys[2] = new GradientColorKey(pipeColor, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            lineRenderer.colorGradient = gradient;
        }
    }
}