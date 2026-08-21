using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static HoverInfoTrigger CurrentSelection
    {
        get;
        private set;
    }


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
    private bool drawRay = false;


    private CanvasInfoManager canvasInfoManager;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (clickCamera == null)
        {
            clickCamera =
                Camera.main;
        }

        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();
    }


    private void Update()
    {
        CheckMouseClick();
    }


    // ============================================================
    // CLICK
    // ============================================================

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


        // ========================================================
        // ABILITY UI
        // ========================================================

        if (canvasInfoManager != null)
        {
            if (
                canvasInfoManager
                    .TrySelectAbilityUnderMouse()
            )
            {
                return;
            }
        }


        // ========================================================
        // RAYCAST
        // ========================================================

        Ray ray =
            clickCamera.ScreenPointToRay(
                mousePosition
            );

        if (drawRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction *
                raycastDistance,
                Color.green,
                1f
            );
        }

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                raycastDistance,
                clickLayers
            );


        // ========================================================
        // ABILITY
        // ========================================================

        if (HasSelectedAbility())
        {
            if (
                TryUseSelectedAbility(
                    mousePosition,
                    hit
                )
            )
            {
                return;
            }

            return;
        }


        // ========================================================
        // MOVEMENT
        // ========================================================

        if (
            allowPlayerMovement &&
            CurrentSelection != null
        )
        {
            if (
                TryMoveSelectedUnit(
                    mousePosition
                )
            )
            {
                return;
            }
        }


        // ========================================================
        // OBJECT
        // ========================================================

        if (hit.collider != null)
        {
            HoverInfoTrigger trigger =
                hit.collider
                    .GetComponentInParent<
                        HoverInfoTrigger>();

            if (trigger != null)
            {
                SelectObject(
                    trigger
                );

                return;
            }
        }


        // ========================================================
        // EMPTY
        // ========================================================

        if (CurrentSelection != null)
        {
            ClearSelection();
        }
    }


    // ============================================================
    // ABILITY STATE
    // ============================================================

    private bool HasSelectedAbility()
    {
        if (canvasInfoManager == null)
        {
            return false;
        }

        return
            canvasInfoManager
                .HasSelectedAbility();
    }


    // ============================================================
    // PLAYER UNIT
    // ============================================================

    private bool IsPlayerControlledUnit(
        GameObject unit
    )
    {
        if (unit == null)
        {
            return false;
        }

        AttackUnit attackUnit =
            unit.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            return false;
        }

        Team team =
            attackUnit.GetTeam();

        return
            team == Team.Player ||
            team == Team.Ally;
    }


    // ============================================================
    // USE SELECTED ABILITY
    // ============================================================

    private bool TryUseSelectedAbility(
        Vector2 mousePosition,
        RaycastHit2D hit
    )
    {
        if (CurrentSelection == null)
        {
            return false;
        }

        if (!IsPlayerControlledUnit(
                CurrentSelection.gameObject))
        {
            return false;
        }

        if (canvasInfoManager == null)
        {
            return false;
        }

        AbilitySO ability =
            canvasInfoManager
                .GetSelectedAbility();

        if (ability == null)
        {
            return false;
        }


        // ========================================================
        // ATTACK UNIT
        // ========================================================

        AttackUnit attackUnit =
            CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
        {
            return false;
        }


        // ========================================================
        // MOVEMENT BRAIN
        // ========================================================

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<
                UnitMoveBrain>();

        if (moveBrain == null)
        {
            return false;
        }


        // ========================================================
        // MOVED THIS TURN CHECK
        // ========================================================

        if (
            moveBrain.HasConsumedMovement() &&
            !ability.CanAttackWithThisAfterMove()
        )
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] ATTACK BLOCKED -> " +
                    $"{attackUnit.gameObject.name} already moved. " +
                    $"'{ability.GetAbilityName()}' cannot be used after moving.",
                    attackUnit
                );
            }

            return false;
        }


        // ========================================================
        // GRID
        // ========================================================

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return false;
        }


        // ========================================================
        // SCREEN -> WORLD
        // ========================================================

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


        // ========================================================
        // WORLD -> GRID
        // ========================================================

        Vector2Int targetTile =
            gridManager.WorldToGridPosition(
                worldPosition
            );


        // ========================================================
        // GRID
        // ========================================================

        if (
            !gridManager.IsInsideGrid(
                targetTile
            )
        )
        {
            return false;
        }


        // ========================================================
        // RANGE
        // ========================================================

        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                CurrentSelection.gameObject
            );

        if (
            rangeTiles == null ||
            !rangeTiles.Contains(
                targetTile
            )
        )
        {
            return false;
        }


        // ========================================================
        // BOMB
        // ========================================================

        BombAttack bombAttack =
            ability as BombAttack;

        if (bombAttack != null)
        {
            bool bombUsed =
                bombAttack.UseAtTile(
                    CurrentSelection.gameObject,
                    gridManager,
                    targetTile
                );

            if (!bombUsed)
            {
                return false;
            }

            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] ABILITY USED -> " +
                    $"{ability.GetAbilityName()} | " +
                    $"Tile: {targetTile} | " +
                    $"AfterMove: {ability.CanAttackWithThisAfterMove()}",
                    CurrentSelection
                );
            }

            ClearSelectedAbility();

            return true;
        }


        // ========================================================
        // NORMAL TARGET
        // ========================================================

        GameObject targetObject =
            FindObjectOnTile(
                targetTile,
                gridManager
            );

        if (targetObject == null)
        {
            return false;
        }


        // ========================================================
        // HIT CHECK
        // ========================================================

        if (!ability.CanHit(
                gridManager,
                CurrentSelection.gameObject,
                targetObject
            ))
        {
            return false;
        }


        // ========================================================
        // USE
        // ========================================================

        bool used =
            ability.Use(
                CurrentSelection.gameObject,
                targetObject
            );

        if (!used)
        {
            return false;
        }


        // ========================================================
        // SUCCESS
        // ========================================================

        if (debugClick)
        {
            Debug.Log(
                $"[UIManager] ABILITY USED -> " +
                $"{ability.GetAbilityName()} | " +
                $"Target: {targetObject.name} | " +
                $"Tile: {targetTile} | " +
                $"AfterMove: {ability.CanAttackWithThisAfterMove()}",
                CurrentSelection
            );
        }

        ClearSelectedAbility();

        return true;
    }


    // ============================================================
    // FIND OBJECT
    // ============================================================

    private GameObject FindObjectOnTile(
        Vector2Int tile,
        GridManager gridManager
    )
    {
        if (gridManager == null)
        {
            return null;
        }

        Vector3 worldPosition =
            GetTileWorldPosition(
                gridManager,
                tile
            );

        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                worldPosition,
                0.35f,
                clickLayers
            );

        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            if (colliders[i] == null)
            {
                continue;
            }

            HoverInfoTrigger trigger =
                colliders[i]
                    .GetComponentInParent<
                        HoverInfoTrigger>();

            if (trigger != null)
            {
                return trigger.gameObject;
            }
        }

        return null;
    }


    // ============================================================
    // TILE POSITION
    // ============================================================

    private Vector3 GetTileWorldPosition(
        GridManager gridManager,
        Vector2Int tile
    )
    {
        if (gridManager == null)
        {
            return Vector3.zero;
        }

        return
            gridManager.GridToWorldPosition(
                tile
            );
    }


    // ============================================================
    // CLEAR ABILITY
    // ============================================================

    private void ClearSelectedAbility()
    {
        if (canvasInfoManager == null)
        {
            return;
        }

        canvasInfoManager
            .ClearSelectedAbility();
    }


    // ============================================================
    // PLAYER MOVEMENT
    // ============================================================

    private bool TryMoveSelectedUnit(
        Vector2 mousePosition
    )
    {
        if (CurrentSelection == null)
        {
            return false;
        }

        if (!IsPlayerControlledUnit(
                CurrentSelection.gameObject))
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
            attackUnit.GetComponent<
                UnitMoveBrain>();

        if (moveBrain == null)
        {
            return false;
        }

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

        Vector2Int destination =
            gridManager.WorldToGridPosition(
                worldPosition
            );

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                attackUnit.transform.position
            );

        if (
            destination ==
            currentPosition
        )
        {
            return false;
        }

        if (
            !gridManager.IsInsideGrid(
                destination
            )
        )
        {
            return false;
        }

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
        {
            return false;
        }

        if (!highlightManager.IsMovementCell(
                destination
            ))
        {
            return false;
        }

        if (
            gridManager.IsCellOccupied(
                destination
            )
        )
        {
            return false;
        }

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

        bool started =
            moveBrain.TryMoveTo(
                destination
            );

        if (!started)
        {
            return false;
        }

        highlightManager
            .ClearMovementRange();

        if (debugClick)
        {
            Debug.Log(
                $"[UIManager] MOVE -> " +
                $"{attackUnit.gameObject.name}: " +
                $"{currentPosition} -> {destination} | " +
                $"MovementConsumed: TRUE",
                attackUnit
            );
        }

        return true;
    }


    // ============================================================
    // SELECT
    // ============================================================

    public static void SelectObject(
        HoverInfoTrigger trigger
    )
    {
        if (trigger == null)
        {
            return;
        }

        if (CurrentSelection == trigger)
        {
            return;
        }

        if (CurrentSelection != null)
        {
            ClearSelection();
        }

        CurrentSelection =
            trigger;

        trigger.SetSelected(true);

        ShowMovementRange(
            trigger
        );

        if (
            CanvasJuiceManager.Instance != null
        )
        {
            CanvasJuiceManager.Instance
                .ShowHoverInfo();
        }
    }


    // ============================================================
    // MOVEMENT RANGE
    // ============================================================

    private static void ShowMovementRange(
        HoverInfoTrigger trigger
    )
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

        if (
            attackUnit.GetTeam() ==
            Team.Enemy
        )
        {
            return;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<
                UnitMoveBrain>();

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

        if (highlightManager == null)
        {
            return;
        }

        Vector2Int position =
            gridManager.WorldToGridPosition(
                trigger.transform.position
            );

        int moveRange =
            moveBrain.GetMoveRange();

        highlightManager.ShowMovementRange(
            position,
            moveRange,
            trigger.gameObject
        );
    }


    // ============================================================
    // CLEAR SELECTION
    // ============================================================

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
        {
            return;
        }

        HoverInfoTrigger previousSelection =
            CurrentSelection;

        previousSelection.SetSelected(
            false
        );

        ClearMovementRange(
            previousSelection
        );

        CurrentSelection = null;

        if (
            Instance != null &&
            Instance.canvasInfoManager != null
        )
        {
            Instance.canvasInfoManager
                .ClearInfo();
        }

        if (
            CanvasJuiceManager.Instance != null
        )
        {
            CanvasJuiceManager.Instance
                .HideHoverInfo();
        }
    }


    // ============================================================
    // CLEAR MOVEMENT RANGE
    // ============================================================

    private static void ClearMovementRange(
        HoverInfoTrigger trigger
    )
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
            attackUnit.GetComponent<
                UnitMoveBrain>();

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
            highlightManager
                .ClearMovementRange();
        }
    }


    // ============================================================
    // CLEAR SPECIFIC
    // ============================================================

    public static void ClearSelection(
        HoverInfoTrigger trigger
    )
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