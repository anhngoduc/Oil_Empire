// Assets/_Project/Scripts/Buildings/BucketInteractable.cs

using UnityEngine;
using DG.Tweening;

namespace OilGame
{
    public class BucketInteractable : MonoBehaviour, IInteractable
    {
        [Header("=== FX Thu Dầu ===")]
        [SerializeField] private GameObject collectFXPrefab;
        [SerializeField] private float punchScale = 0.3f;
        [SerializeField] private float punchDuration = 0.2f;

        private Building building;
        private IBucketService bucketService;
        private GameObject collectFXInstance;
        private Vector3 originalScale;

        private void Awake()
        {
            building = GetComponent<Building>();
            originalScale = transform.localScale;
        }

        private void Start()
        {
            bucketService = ServiceLocator.Get<IBucketService>();
        }

        public void OnInteract(GameObject player, bool isRealPlayer)
        {
            if (building == null || bucketService == null) return;
            if (building.Type != BuildingType.Bucket) return;

            if (isRealPlayer)
            {
                float collected = bucketService.CollectOil(building.UniqueID);
                if (collected > 0f)
                {
                    transform.DOKill();
                    transform.localScale = originalScale;  // Reset về scale gốc của Prefab
                    transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);

                    PlayCollectFX();

                    Debug.Log($"[BucketInteractable] Player thu {collected} Oil");
                }
            }
            else
            {
                building.SetCurrentOil(0f);
                Debug.Log($"[BucketInteractable] Bot reset dầu");
            }
        }

        private void PlayCollectFX()
        {
            if (collectFXPrefab == null) return;

            if (collectFXInstance == null)
            {
                collectFXInstance = Instantiate(collectFXPrefab, transform.position, collectFXPrefab.transform.rotation, transform);
            }
            else
            {
                collectFXInstance.transform.position = transform.position;
                collectFXInstance.SetActive(true);
            }

            collectFXInstance.GetComponent<ParticleSystem>()?.Play();
            CancelInvoke(nameof(HideFX));
            Invoke(nameof(HideFX), 1f);
        }

        private void HideFX()
        {
            if (collectFXInstance != null)
                collectFXInstance.SetActive(false);
        }

        public string GetInteractName()
        {
            return building != null ? building.BuildingData?.buildingName : "Bucket";
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(HideFX));
        }
    }
}