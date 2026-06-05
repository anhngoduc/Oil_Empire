// Assets/_Project/Scripts/Player/PlayerInteractor.cs

using UnityEngine;

namespace OilGame
{
    public class PlayerInteractor : MonoBehaviour
    {

        private void OnTriggerEnter(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null) return;

            interactable.OnInteract(gameObject, true);
        }
    }
}