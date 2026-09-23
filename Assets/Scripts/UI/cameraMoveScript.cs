using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class cameraMoveScript : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("The Transform that your Cinemachine Camera follows.")]
    [SerializeField]
    private Transform cameraTarget;

    [Header("Cinemachine 3")]
    [Tooltip("Your Cinemachine Camera.")]
    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    [Header("Camera Zoom")]
    [Tooltip("How fast the mouse wheel zooms.")]
    [SerializeField]
    private float zoomSpeed = 0.5f;

    [Tooltip("Minimum orthographic size. Smaller = more zoomed in.")]
    [SerializeField]
    private float minZoom = 3f;

    [Tooltip("Maximum orthographic size. Larger = more zoomed out.")]
    [SerializeField]
    private float maxZoom = 15f;

    [Header("Free Camera Movement")]
    [Tooltip("Camera movement speed.")]
    [SerializeField]
    private float moveSpeed = 15f;

    [Tooltip("Percentage of the screen that counts as an edge.")]
    [SerializeField, Range(0.01f, 0.5f)]
    private float edgeSize = 0.1f;

    [Header("Movement Bounds")]
    [Tooltip("Enable X/Y movement limits.")]
    [SerializeField]
    private bool useBounds = false;

    [Tooltip("Minimum X and Y position.")]
    [SerializeField]
    private Vector2 minBounds;

    [Tooltip("Maximum X and Y position.")]
    [SerializeField]
    private Vector2 maxBounds;

    // =========================================================
    // INTERNAL
    // =========================================================

    private bool freeMode = true;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (cameraTarget == null)
        {
            Debug.LogWarning(
                "cameraMoveScript: Camera Target is not assigned.",
                this
            );
        }

        if (cinemachineCamera == null)
        {
            Debug.LogWarning(
                "cameraMoveScript: Cinemachine Camera is not assigned.",
                this
            );
        }
    }


    private void Update()
    {
        if (!freeMode)
            return;

        if (cameraTarget == null)
            return;

        HandleEdgeScrolling();
        HandleZoom();
    }


    // =========================================================
    // EDGE SCROLLING
    // =========================================================

    private void HandleEdgeScrolling()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        Vector3 movement = Vector3.zero;


        // LEFT
        if (mousePosition.x <= screenWidth * edgeSize)
        {
            movement.x -= 1f;
        }


        // RIGHT
        if (mousePosition.x >= screenWidth * (1f - edgeSize))
        {
            movement.x += 1f;
        }


        // BOTTOM
        if (mousePosition.y <= screenHeight * edgeSize)
        {
            movement.y -= 1f;
        }


        // TOP
        if (mousePosition.y >= screenHeight * (1f - edgeSize))
        {
            movement.y += 1f;
        }


        // No movement
        if (movement == Vector3.zero)
            return;


        // Prevent diagonal movement
        // from being faster
        movement.Normalize();


        Vector3 newPosition =
            cameraTarget.position +
            movement *
            moveSpeed *
            Time.deltaTime;


        // Apply bounds
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


        // Keep Z exactly the same
        newPosition.z =
            cameraTarget.position.z;


        cameraTarget.position =
            newPosition;
    }


    // =========================================================
    // MOUSE WHEEL ZOOM
    // =========================================================

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        if (cinemachineCamera == null)
            return;


        Vector2 scroll =
            Mouse.current.scroll.ReadValue();


        if (Mathf.Abs(scroll.y) < 0.01f)
            return;


        float currentSize =
            cinemachineCamera.Lens.OrthographicSize;


        // Scroll up = zoom in
        // Scroll down = zoom out
        float newSize =
            currentSize -
            scroll.y *
            zoomSpeed *
            0.01f;


        newSize = Mathf.Clamp(
            newSize,
            minZoom,
            maxZoom
        );


        cinemachineCamera.Lens.OrthographicSize =
            newSize;
    }


    // =========================================================
    // FREE CAMERA MODE
    // =========================================================

    public void EnableFreeMode()
    {
        freeMode = true;
    }


    public void DisableFreeMode()
    {
        freeMode = false;
    }


    public bool IsFreeMode()
    {
        return freeMode;
    }


    // =========================================================
    // RESET ZOOM
    // =========================================================

    public void ResetZoom()
    {
        if (cinemachineCamera == null)
            return;


        cinemachineCamera.Lens.OrthographicSize =
            Mathf.Clamp(
                maxZoom,
                minZoom,
                maxZoom
            );
    }
}