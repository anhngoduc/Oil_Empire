// Assets/_Project/Scripts/Camera/CameraZoom.cs

using UnityEngine;
using Cinemachine;
using DG.Tweening;

namespace OilGame
{
    public class CameraZoom : MonoBehaviour
    {
        public static CameraZoom Instance { get; private set; }

        [Header("=== Cấu hình Zoom ===")]
        [SerializeField] private float zoomScale = 0.5f;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        private CinemachineFreeLook freeLook;
        private Tween currentTween;
        private bool isZoomed = false;
        private float[] originalHeights;
        private float[] originalRadii;

        private void Awake()
        {
            Instance = this;
            freeLook = GetComponent<CinemachineFreeLook>();
            if (freeLook == null) freeLook = GetComponentInChildren<CinemachineFreeLook>();

            if (freeLook != null)
            {
                originalHeights = new float[3];
                originalRadii = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    originalHeights[i] = freeLook.m_Orbits[i].m_Height;
                    originalRadii[i] = freeLook.m_Orbits[i].m_Radius;
                }
            }
        }

        public void ZoomIn()
        {
            if (isZoomed || freeLook == null) return;
            isZoomed = true;
            currentTween?.Kill();

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                DOTween.To(
                    () => freeLook.m_Orbits[index].m_Height,
                    x => freeLook.m_Orbits[index].m_Height = x,
                    originalHeights[index] * zoomScale,
                    duration
                ).SetEase(ease);

                DOTween.To(
                    () => freeLook.m_Orbits[index].m_Radius,
                    x => freeLook.m_Orbits[index].m_Radius = x,
                    originalRadii[index] * zoomScale,
                    duration
                ).SetEase(ease);
            }
        }

        public void ZoomOut()
        {
            if (!isZoomed || freeLook == null) return;
            isZoomed = false;
            currentTween?.Kill();

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                DOTween.To(
                    () => freeLook.m_Orbits[index].m_Height,
                    x => freeLook.m_Orbits[index].m_Height = x,
                    originalHeights[index],
                    duration
                ).SetEase(ease);

                DOTween.To(
                    () => freeLook.m_Orbits[index].m_Radius,
                    x => freeLook.m_Orbits[index].m_Radius = x,
                    originalRadii[index],
                    duration
                ).SetEase(ease);
            }
        }

        private void OnDestroy()
        {
            currentTween?.Kill();
            if (Instance == this) Instance = null;
        }
    }
}