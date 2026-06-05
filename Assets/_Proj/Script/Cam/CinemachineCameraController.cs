using UnityEngine;
using Cinemachine;

public class CinemachineCameraController : MonoBehaviour
{
    [SerializeField] CinemachineFreeLook freeLookCamera;
    [SerializeField] bool invertX = false;
    [SerializeField] bool invertY = true;
    [SerializeField] float speedY = 0.005f; 
    void Start()
    {
        freeLookCamera.m_XAxis.m_InputAxisName = "";
        freeLookCamera.m_YAxis.m_InputAxisName = "";
    }

    void Update()
    {
        if (CameraInputHandler.Instance == null) return;

        float deltaX = CameraInputHandler.Instance.DeltaX * (invertX ? -1 : 1);
        float deltaY = CameraInputHandler.Instance.DeltaY * (invertY ? -1 : 1);

        freeLookCamera.m_XAxis.Value += deltaX;
        freeLookCamera.m_YAxis.Value += deltaY * speedY;
    }
}