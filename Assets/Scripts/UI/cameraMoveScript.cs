
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class cameraMoveScript : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("The Transform that your Cinemachine Camera follows.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Cinemachine 3")]
    [Tooltip("Your Cinemachine Camera.")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Camera Zoom")]
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;

    [Header("Free Camera Movement")]
    [SerializeField] private float moveSpeed = 15f;

    [SerializeField, Range(0.01f, 0.5f)]
    private float edgeSize = 0.1f;

    [Header("Movement Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Tooltip("Minimum interval between free-camera movement logs.")]
    [SerializeField] private float movementLogInterval = 0.5f;

    private bool freeMode = true;
    private float nextMovementLogTime;

    private void Awake()
    {
        if (cameraTarget == null)
        {
            Debug.LogWarning(
                "[CameraDebug] Camera Target is not assigned.",
                this
            );
        }
        else
        {
            LogCameraTarget("Awake - Initial camera target");
        }

        if (cinemachineCamera == null)
        {
            Debug.LogWarning(
                "[CameraDebug] Cinemachine Camera is not assigned.",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[CameraDebug] Cinemachine Camera: {cinemachineCamera.name}",
                cinemachineCamera
            );

            if (cinemachineCamera.Follow != cameraTarget)
            {
                Debug.LogWarning(
                    "[CameraDebug] Cinemachine Follow target differs " +
                    "from this script's cameraTarget. Check both references.",
                    cinemachineCamera
                );
            }
        }
    }

    private void Update()
    {
        if (!freeMode || cameraTarget == null)
            return;

        HandleEdgeScrolling();
        HandleZoom();
    }

    public void SetCameraTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning(
                "[CameraDebug] SetCameraTarget received NULL.",
                this
            );
            return;
        }

        string previousTarget = cameraTarget != null
            ? cameraTarget.name
            : "NULL";

        Vector3 previousPosition = cameraTarget != null
            ? cameraTarget.position
            : Vector3.zero;

        cameraTarget = newTarget;

        LogCameraTarget(
            $"SetCameraTarget: {previousTarget} -> {newTarget.name}; " +
            $"previous position = {previousPosition}"
        );
    }

    public Transform GetCameraTarget()
    {
        return cameraTarget;
    }

    private void LogCameraTarget(string reason)
    {
        if (!enableDebugLogs)
            return;

        if (cameraTarget == null)
        {
            Debug.LogWarning(
                $"[CameraDebug] {reason}: target is NULL.",
                this
            );
            return;
        }

        Debug.Log(
            $"[CameraDebug] {reason}\n" +
            $"Target name: {cameraTarget.name}\n" +
            $"Target position: {cameraTarget.position}\n" +
            $"Free mode: {freeMode}\n" +
            $"Frame: {Time.frameCount}",
            cameraTarget
        );
    }

    private void HandleEdgeScrolling()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        Vector3 movement = Vector3.zero;

        if (mousePosition.x <= screenWidth * edgeSize)
            movement.x -= 1f;

        if (mousePosition.x >= screenWidth * (1f - edgeSize))
            movement.x += 1f;

        if (mousePosition.y <= screenHeight * edgeSize)
            movement.y -= 1f;

        if (mousePosition.y >= screenHeight * (1f - edgeSize))
            movement.y += 1f;

        if (movement == Vector3.zero)
            return;

        Vector3 oldPosition = cameraTarget.position;

        movement.Normalize();

        Vector3 newPosition =
            cameraTarget.position +
            movement * moveSpeed * Time.deltaTime;

        if (useBounds)
        {
            newPosition.x = Mathf.Clamp(
                newPosition.x,
                minBounds.x,
                maxBounds.x
            );

            newPosition.y = Mathf.Clamp(
                newPosition.y,
                minBounds.y,
                maxBounds.y
            );
        }

        newPosition.z = cameraTarget.position.z;
        cameraTarget.position = newPosition;

        if (enableDebugLogs && Time.time >= nextMovementLogTime)
        {
            nextMovementLogTime =
                Time.time + Mathf.Max(0.05f, movementLogInterval);

            Debug.Log(
                $"[CameraDebug] EDGE SCROLL\n" +
                $"From: {oldPosition}\n" +
                $"To: {newPosition}\n" +
                $"Mouse: {mousePosition}\n" +
                $"Frame: {Time.frameCount}",
                cameraTarget
            );
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current == null || cinemachineCamera == null)
            return;

        Vector2 scroll = Mouse.current.scroll.ReadValue();

        if (Mathf.Abs(scroll.y) < 0.01f)
            return;

        float oldSize =
            cinemachineCamera.Lens.OrthographicSize;

        float newSize =
            oldSize - scroll.y * zoomSpeed * 0.01f;

        newSize = Mathf.Clamp(newSize, minZoom, maxZoom);

        cinemachineCamera.Lens.OrthographicSize = newSize;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[CameraDebug] ZOOM: {oldSize:F2} -> {newSize:F2}",
                cinemachineCamera
            );
        }
    }

    public void EnableFreeMode()
    {
        bool wasFreeMode = freeMode;
        freeMode = true;

        if (!wasFreeMode)
            LogCameraTarget("Free camera mode ENABLED");
    }

    public void DisableFreeMode()
    {
        bool wasFreeMode = freeMode;
        freeMode = false;

        if (wasFreeMode)
            LogCameraTarget("Free camera mode DISABLED");
    }

    public bool IsFreeMode()
    {
        return freeMode;
    }

    public void ResetZoom()
    {
        if (cinemachineCamera == null)
            return;

        float oldSize =
            cinemachineCamera.Lens.OrthographicSize;

        cinemachineCamera.Lens.OrthographicSize =
            Mathf.Clamp(maxZoom, minZoom, maxZoom);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[CameraDebug] ZOOM RESET: {oldSize:F2} -> " +
                $"{cinemachineCamera.Lens.OrthographicSize:F2}",
                this
            );
        }
    }
}
