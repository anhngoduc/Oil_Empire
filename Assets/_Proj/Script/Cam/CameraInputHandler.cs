using UnityEngine;
using UnityEngine.EventSystems;

public class CameraInputHandler : MonoBehaviour
{
    public static CameraInputHandler Instance { get; private set; }

    [Header("Sensitivity")]
    [SerializeField] float touchSensitivity = 0.15f;
    [SerializeField] float joystickSensitivity = 3f;

    [Header("Touch Zone")]
    [SerializeField] float touchZoneStartX = 0.5f;
    [SerializeField] float touchZoneEndX = 1f;
    [SerializeField] float touchZoneStartY = 0f;
    [SerializeField] float touchZoneEndY = 1f;

    [Header("Joystick camera (optional)")]
    [SerializeField] Joystick cameraJoystick;

    public float DeltaX { get; private set; }
    public float DeltaY { get; private set; }

    int activeTouchId = -1;
    Vector2 lastTouchPos;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        DeltaX = 0;
        DeltaY = 0;
        HandleJoystick();
        HandleTouch();
    }

    void HandleJoystick()
    {
        if (cameraJoystick == null) return;
        float h = cameraJoystick.Horizontal;
        float v = cameraJoystick.Vertical;
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            DeltaX += h * joystickSensitivity;
            DeltaY += v * joystickSensitivity;
        }
    }

    void HandleTouch()
    {
        foreach (Touch touch in Input.touches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (activeTouchId == -1 && IsInTouchZone(touch.position))
                    {
                        activeTouchId = touch.fingerId;
                        lastTouchPos = touch.position;
                    }
                    break;

                case TouchPhase.Moved:
                    if (touch.fingerId == activeTouchId)
                    {
                        Vector2 delta = touch.position - lastTouchPos;
                        DeltaX += delta.x * touchSensitivity;
                        DeltaY += delta.y * touchSensitivity;
                        lastTouchPos = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeTouchId)
                        activeTouchId = -1;
                    break;
            }
        }
    }

    bool IsInTouchZone(Vector2 screenPos)
    {
        float normalizedX = screenPos.x / Screen.width;
        float normalizedY = screenPos.y / Screen.height;
        return normalizedX >= touchZoneStartX && normalizedX <= touchZoneEndX &&
               normalizedY >= touchZoneStartY && normalizedY <= touchZoneEndY;
    }

   
}