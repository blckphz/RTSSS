using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static HoverInfoTrigger CurrentSelection { get; private set; }

    [Header("Click Raycast")]
    [SerializeField]
    private Camera clickCamera;

    [SerializeField]
    private LayerMask clickLayers = ~0;

    [SerializeField]
    private float raycastDistance = 1000f;

    [Header("Movement")]
    [SerializeField]
    private bool allowPlayerMovement = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugClick = true;

    [SerializeField]
    private bool drawRay = true;


    // =============================================================
    // UNITY
    // =============================================================

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


    // =============================================================
    // MOUSE CLICK
    // =============================================================

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


        // =========================================================
        // 2D RAYCAST
        // =========================================================

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                raycastDistance,
                clickLayers
            );


        // =========================================================
        // TRY MOVING SELECTED UNIT
        // =========================================================

        if (allowPlayerMovement &&
            CurrentSelection != null)
        {
            if (TryMoveSelectedUnit(mousePosition))
            {
                return;
            }
        }


        // =========================================================
        // CLICKED OBJECT
        // =========================================================

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


        // =========================================================
        // EMPTY SPACE
        // =========================================================

        if (CurrentSelection != null)
        {
            ClearSelection();
        }
    }


    // =============================================================
    // PLAYER MOVEMENT
    // =============================================================

    private bool TryMoveSelectedUnit(
        Vector2 mousePosition)
    {
        if (CurrentSelection == null)
        {
            return false;
        }

        AttackUnit attackUnit =
            CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
        {
            return false;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            if (debugClick)
            {
                Debug.LogWarning(
                    "[UIManager] Selected unit has no UnitMoveBrain.",
                    attackUnit
                );
            }

            return false;
        }


        // =========================================================
        // UNIT ALREADY MOVING
        // =========================================================

        if (moveBrain.IsMoving())
        {
            return true;
        }


        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return false;
        }


        // =========================================================
        // SCREEN -> WORLD
        // =========================================================

        Vector3 worldPosition =
            clickCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    Mathf.Abs(
                        clickCamera.transform.position.z
                    )
                )
            );


        // =========================================================
        // WORLD -> GRID
        // =========================================================

        Vector2Int destination =
            gridManager.WorldToGridPosition(
                worldPosition
            );

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                attackUnit.transform.position
            );


        // =========================================================
        // SAME CELL
        // =========================================================

        if (destination == currentPosition)
        {
            return false;
        }


        // =========================================================
        // INSIDE GRID
        // =========================================================

        if (!gridManager.IsInsideGrid(destination))
        {
            return false;
        }


        // =========================================================
        // HIGHLIGHT MANAGER
        // =========================================================

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
        {
            return false;
        }


        // =========================================================
        // ONLY ALLOW HIGHLIGHTED CELLS
        // =========================================================

        if (!highlightManager.IsMovementCell(destination))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cell {destination} is outside the selected unit's movement range."
                );
            }

            return false;
        }


        // =========================================================
        // OCCUPIED
        // =========================================================

        if (gridManager.IsCellOccupied(destination))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cannot move to {destination}. Cell is occupied."
                );
            }

            return false;
        }


        // =========================================================
        // RANGE CHECK
        // =========================================================

        int moveRange =
            moveBrain.GetMoveRange();

        int distance =
            gridManager.GetDistance(
                currentPosition,
                destination
            );

        if (distance > moveRange)
        {
            return false;
        }


        // =========================================================
        // ACTUAL MOVEMENT
        // =========================================================

        bool started =
            moveBrain.TryMoveTo(destination);

        if (!started)
        {
            return false;
        }


        // =========================================================
        // CLEAR MOVEMENT RANGE
        // =========================================================

        highlightManager.ClearMovementRange();


        if (debugClick)
        {
            Debug.Log(
                $"[UIManager] Player movement: {currentPosition} -> {destination}",
                attackUnit
            );
        }

        return true;
    }


    // =============================================================
    // SELECT OBJECT
    // =============================================================

    public static void SelectObject(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }


        // Clicking already selected object does nothing.
        if (CurrentSelection == trigger)
        {
            return;
        }


        // =========================================================
        // CLEAR OLD SELECTION
        // =========================================================

        if (CurrentSelection != null)
        {
            ClearSelection();
        }


        // =========================================================
        // NEW SELECTION
        // =========================================================

        CurrentSelection = trigger;

        trigger.SetSelected(true);


        Debug.Log(
            $"[UIManager] SELECTED -> {trigger.gameObject.name} | Message: {trigger.HoverMessage}",
            trigger
        );


        // =========================================================
        // SHOW MOVEMENT RANGE
        // =========================================================

        ShowMovementRange(trigger);


        // =========================================================
        // UI
        // =========================================================

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


    // =============================================================
    // SHOW MOVEMENT RANGE
    // =============================================================

    private static void ShowMovementRange(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }


        AttackUnit attackUnit =
            trigger.GetAttackUnit();

        if (attackUnit == null)
        {
            return;
        }


        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            Debug.LogWarning(
                "[UIManager] Selected unit has no UnitMoveBrain.",
                attackUnit
            );

            return;
        }


        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return;
        }


        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
        {
            Debug.LogWarning(
                "[UIManager] GridHighlightManager is missing.",
                gridManager
            );

            return;
        }


        // =========================================================
        // UNIT POSITION
        // =========================================================

        Vector2Int position =
            gridManager.WorldToGridPosition(
                trigger.transform.position
            );


        // =========================================================
        // MOVEMENT RANGE
        // =========================================================

        int moveRange =
            moveBrain.GetMoveRange();


        highlightManager.ShowMovementRange(
            position,
            moveRange,
            trigger.gameObject
        );
    }


    // =============================================================
    // CLEAR SELECTION
    // =============================================================

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
        {
            return;
        }


        HoverInfoTrigger previousSelection =
            CurrentSelection;


        previousSelection.SetSelected(false);


        ClearMovementRange(previousSelection);


        CurrentSelection = null;


        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance.HideHoverInfo();
        }
    }


    // =============================================================
    // CLEAR MOVEMENT RANGE
    // =============================================================

    private static void ClearMovementRange(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }


        AttackUnit attackUnit =
            trigger.GetAttackUnit();

        if (attackUnit == null)
        {
            return;
        }


        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            return;
        }


        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return;
        }


        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager != null)
        {
            highlightManager.ClearMovementRange();
        }
    }


    // =============================================================
    // CLEAR SPECIFIC SELECTION
    // =============================================================

    public static void ClearSelection(
        HoverInfoTrigger trigger)
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