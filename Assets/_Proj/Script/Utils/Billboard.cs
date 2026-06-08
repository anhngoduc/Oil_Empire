// Assets/_Project/Scripts/Utils/Billboard.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Luôn quay GameObject về phía Camera chính.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start() 
        { 
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
                transform.forward = mainCamera.transform.forward;
        }
    }
}