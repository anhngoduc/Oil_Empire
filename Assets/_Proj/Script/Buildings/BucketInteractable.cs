// Assets/_Project/Scripts/Buildings/BucketInteractable.cs

using UnityEngine;

namespace OilGame
{
    public class BucketInteractable : MonoBehaviour, IInteractable
    {
        private Building building;
        private IBucketService bucketService;

        private void Awake()
        {
            building = GetComponent<Building>();
        }

        private void Start()
        {
            bucketService = ServiceLocator.Get<IBucketService>();
        }

        public void OnInteract(GameObject player, bool isRealPlayer)
        {
            if (building == null || bucketService == null) return;
            if (building.Type != BuildingType.Bucket) return;

            // Nếu là Player thật VÀ ID âm (của Bot) → bỏ qua
            if (isRealPlayer && building.UniqueID < 0)
            {
                return;
            }

            if (isRealPlayer)
            {
                float collected = bucketService.CollectOil(building.UniqueID);
                if (collected > 0f)
                    Debug.Log($"[BucketInteractable] Player thu {collected} Oil");
            }
            else
            {
                building.SetCurrentOil(0f);
            }
        }

        public string GetInteractName()
        {
            return building != null ? building.BuildingData?.buildingName : "Bucket";
        }
    }
}