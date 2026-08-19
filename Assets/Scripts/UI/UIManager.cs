using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static HoverInfoTrigger CurrentSelection { get; private set; }

    [Header("Click Raycast")]
    [SerializeField] private Camera clickCamera;
    [SerializeField] private LayerMask clickLayers = ~0;
    [SerializeField] private float raycastDistance = 1000f;

    [Header("Debug")]
    [SerializeField] private bool debugClick = true;
    [SerializeField] private bool drawRay = true;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[UIManager] Duplicate UIManager found. Destroying duplicate.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
        }

        if (clickCamera == null)
        {
            Debug.LogError(
                "[UIManager] NO CAMERA FOUND!",
                this
            );
        }
    }

    private void Update()
    {
        CheckMouseClick();
    }

    private void CheckMouseClick()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (clickCamera == null)
        {
            return;
        }

        // Only raycast when the left mouse button
        // is actually clicked.
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            clickCamera.ScreenPointToRay(mousePosition);

        if (drawRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * raycastDistance,
                Color.green,
                1f
            );
        }

        // 2D raycast because the objects use Collider2D.
        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                raycastDistance,
                clickLayers
            );

        if (hit.collider != null)
        {
            HoverInfoTrigger trigger =
                hit.collider.GetComponentInParent<HoverInfoTrigger>();

          

            if (trigger != null)
            {
                SelectObject(trigger);
            }

            return;
        }

        // Clicked empty space.
        if (CurrentSelection != null)
        {
            ClearSelection();
        }
    }

    public static void SelectObject(HoverInfoTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }

        // Clicking the already selected object
        // does nothing.
        if (CurrentSelection == trigger)
        {
            return;
        }

        // Remove selection from previous object.
        if (CurrentSelection != null)
        {
            ClearSelection();
        }

        CurrentSelection = trigger;

        // Tell the object it has been selected.
        trigger.SetSelected(true);

        Debug.Log(
            $"[UIManager] SELECTED -> " +
            $"{trigger.gameObject.name} | " +
            $"Message: {trigger.HoverMessage}",
            trigger
        );

        // Show UI / move camera.
        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance.ShowHoverInfo();
        }
        else
        {
            Debug.LogError(
                "[UIManager] CanvasJuiceManager.Instance is NULL!"
            );
        }
    }

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
        {
            return;
        }

        // Tell the object to return to normal.
        CurrentSelection.SetSelected(false);

        CurrentSelection = null;

        // Hide UI / return camera.
        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance.HideHoverInfo();
        }
    }

    public static void ClearSelection(HoverInfoTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }

        if (CurrentSelection == trigger)
        {
            ClearSelection();
        }
    }
}