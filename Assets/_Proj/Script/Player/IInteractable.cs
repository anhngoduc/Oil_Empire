// Assets/_Project/Scripts/Player/IInteractable.cs

using UnityEngine;

namespace OilGame
{
    public interface IInteractable
    {
        void OnInteract(GameObject player, bool isRealPlayer);
        string GetInteractName();
    }
}