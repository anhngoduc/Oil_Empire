// Assets/_Project/Scripts/Buildings/OilPipe.cs

using UnityEngine;

namespace OilGame
{
    public class OilPipe : MonoBehaviour
    {
        [Header("Cấu hình")]
        [SerializeField] private float pipeWidth = 0.1f;
        [SerializeField] private float flowSpeed = 2f;
        [SerializeField] private Color pipeColor = Color.black;
        [SerializeField] private Color flowColor = Color.white;
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private float lineHeigh = 0.05f;


        private LineRenderer lineRenderer;
        private Building myBuilding;
        private Transform targetBucket;
        private int activeBucketID = -1;

        private void Awake()
        {
            myBuilding = GetComponent<Building>();
            if (myBuilding == null || myBuilding.Type != BuildingType.Drill) return;

            // Tạo GameObject con riêng cho LineRenderer
            GameObject pipeObj = new GameObject("PipeRenderer");
            pipeObj.transform.SetParent(transform);
            pipeObj.transform.localPosition = Vector3.zero;
            pipeObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            lineRenderer = pipeObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = segmentCount;
            lineRenderer.startWidth = pipeWidth;
            lineRenderer.endWidth = pipeWidth;
            lineRenderer.alignment = LineAlignment.TransformZ;
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

            IBuildingService bs = ServiceLocator.Get<IBuildingService>();
            Building bucket = bs?.GetBuildingByID(activeBucketID);
            if (bucket != null)
            {
                targetBucket = bucket.transform;
                lineRenderer.enabled = true;
                return;
            }

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

            Vector3 start = transform.position + Vector3.up * lineHeigh;
            Vector3 end = targetBucket.position + Vector3.up * lineHeigh;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                lineRenderer.SetPosition(i, pos);
            }

            Gradient gradient = new Gradient();

            // Chạy 1 chiều: từ 0 → 1, rồi quay lại 0 (loop)
            float flow = Mathf.Repeat(Time.time * flowSpeed, 1f);

            int keyCount = Mathf.Min(segmentCount, 8);
            GradientColorKey[] colorKeys = new GradientColorKey[keyCount];

            for (int i = 0; i < keyCount; i++)
            {
                float t = i / (float)(keyCount - 1);
                // Khoảng cách theo 1 chiều, wrap around
                float dist = Mathf.Abs(t - flow);
                if (dist > 0.5f) dist = 1f - dist; // Wrap around
                Color color = Color.Lerp(flowColor, pipeColor, dist * 5f);
                colorKeys[i] = new GradientColorKey(color, t);
            }

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            lineRenderer.colorGradient = gradient;
        }
    }
}