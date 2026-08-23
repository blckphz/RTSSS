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
    private bool drawRay = true;


    // ============================================================
    // REFERENCES
    // ============================================================

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
            Debug.LogWarning(
                "[UIManager] Duplicate UIManager found. " +
                "Destroying duplicate.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;


        if (clickCamera == null)
        {
            clickCamera =
                Camera.main;
        }


        if (clickCamera == null)
        {
            Debug.LogError(
                "[UIManager] NO CAMERA FOUND!",
                this
            );
        }


        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();
    }


    private void Update()
    {
        CheckRightClick();
        CheckMouseClick();
    }


    // ============================================================
    // RIGHT CLICK
    // ============================================================

    private void CheckRightClick()
    {
        if (Mouse.current == null)
        {
            return;
        }


        if (!Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }


        // ========================================================
        // ABILITY SELECTED
        // ========================================================

        if (HasSelectedAbility())
        {
            if (debugClick)
            {
                Debug.Log(
                    "[UIManager] Ability deselected with RIGHT CLICK."
                );
            }


            ClearSelectedAbility();

            return;
        }


        // ========================================================
        // UNIT SELECTED
        // ========================================================

        if (CurrentSelection != null)
        {
            if (debugClick)
            {
                Debug.Log(
                    "[UIManager] Unit deselected with RIGHT CLICK -> " +
                    $"{CurrentSelection.gameObject.name}",
                    CurrentSelection
                );
            }


            ClearSelection();

            return;
        }


        // ========================================================
        // NOTHING SELECTED
        // ========================================================

        if (debugClick)
        {
            Debug.Log(
                "[UIManager] Right click pressed, " +
                "but nothing was selected."
            );
        }
    }


    // ============================================================
    // GLOBAL LEFT CLICK
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
        // 1. UI ABILITY CLICK
        // ========================================================

        if (canvasInfoManager != null)
        {
            if (
                canvasInfoManager
                    .TrySelectAbilityUnderMouse()
            )
            {
                if (debugClick)
                {
                    Debug.Log(
                        "[UIManager] " +
                        "Click consumed by ability UI."
                    );
                }

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
                ray.direction * raycastDistance,
                Color.green,
                1f
            );
        }


        // ========================================================
        // 2D RAYCAST
        // ========================================================

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                raycastDistance,
                clickLayers
            );


        // ========================================================
        // IMPORTANT:
        //
        // Check what was actually clicked BEFORE attempting
        // to move the currently selected unit.
        //
        // This prevents:
        //
        // Selected Unit A
        //        ↓
        // Click Unit B
        //
        // from accidentally moving Unit A instead of selecting B.
        // ========================================================

        HoverInfoTrigger clickedTrigger = null;


        if (hit.collider != null)
        {
            clickedTrigger =
                hit.collider
                    .GetComponentInParent<
                        HoverInfoTrigger
                    >();
        }


        // ========================================================
        // 3. ABILITY TARGETING
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


            /*
             * If an ability is selected,
             * clicking an invalid target should
             * NOT move the unit.
             */

            return;
        }


        // ========================================================
        // 4. CLICKED UNIT
        // ========================================================

        if (clickedTrigger != null)
        {
            SelectObject(
                clickedTrigger
            );

            return;
        }


        // ========================================================
        // 5. MOVEMENT
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
        // 6. EMPTY SPACE
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


        return canvasInfoManager.HasSelectedAbility();
    }


    // ============================================================
    // PLAYER CONTROL CHECK
    // ============================================================

    private bool IsPlayerControlledUnit(
        GameObject unit)
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
        RaycastHit2D hit)
    {
        // ========================================================
        // VALIDATION
        // ========================================================

        if (CurrentSelection == null)
        {
            return false;
        }


        // ========================================================
        // ONLY PLAYER / ALLY
        // ========================================================

        if (!IsPlayerControlledUnit(
                CurrentSelection.gameObject))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Enemy ability use blocked -> " +
                    $"{CurrentSelection.gameObject.name}",
                    CurrentSelection
                );
            }


            return false;
        }


        if (canvasInfoManager == null)
        {
            return false;
        }


        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();


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
        // ABILITY READY CHECK
        // ========================================================

        if (!attackUnit.IsAbilityReady(ability))
        {
            return false;
        }


        // ========================================================
        // MOVEMENT BRAIN
        // ========================================================

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();


        if (moveBrain == null)
        {
            if (debugClick)
            {
                Debug.LogWarning(
                    "[UIManager] Selected unit has no " +
                    "UnitMoveBrain.",
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
        // GRID CHECK
        // ========================================================

        if (!gridManager.IsInsideGrid(
                targetTile))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cannot use " +
                    $"{ability.GetAbilityName()} at " +
                    $"{targetTile}. " +
                    $"Tile is outside grid."
                );
            }


            return false;
        }


        // ========================================================
        // RANGE CHECK
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
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cannot use " +
                    $"{ability.GetAbilityName()} at " +
                    $"{targetTile}. " +
                    $"Tile is outside ability range."
                );
            }


            return false;
        }


        // ========================================================
        // BOMB / TILE ABILITY
        // ========================================================

        BombAttack bombAttack =
            ability as BombAttack;


        if (bombAttack != null)
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Bomb target tile -> " +
                    $"{targetTile}"
                );
            }


            bool bombUsed =
                attackUnit.AttackAtTile(
                    targetTile,
                    ability
                );


            if (!bombUsed)
            {
                if (debugClick)
                {
                    Debug.Log(
                        $"[UIManager] Bomb " +
                        $"{ability.GetAbilityName()} " +
                        $"failed at {targetTile}."
                    );
                }


                return false;
            }


            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Bomb USED -> " +
                    $"{ability.GetAbilityName()} " +
                    $"at tile {targetTile}. " +
                    $"Uses remaining: " +
                    $"{attackUnit.GetAbilityUsesRemaining(ability)}",
                    CurrentSelection
                );
            }


            ClearSelectedAbility();


            return true;
        }


        // ========================================================
        // NORMAL ABILITY
        // ========================================================

        GameObject targetObject =
            FindObjectOnTile(
                targetTile,
                gridManager
            );


        // ========================================================
        // NORMAL ABILITIES REQUIRE TARGET OBJECT
        // ========================================================

        if (targetObject == null)
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] " +
                    $"{ability.GetAbilityName()} " +
                    $"requires a target at {targetTile}."
                );
            }


            return false;
        }


        // ========================================================
        // NORMAL ABILITY HIT CHECK
        // ========================================================

        if (!ability.CanHit(
                gridManager,
                CurrentSelection.gameObject,
                targetObject))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] " +
                    $"{ability.GetAbilityName()} " +
                    $"cannot hit target " +
                    $"'{targetObject.name}'."
                );
            }


            return false;
        }


        // ========================================================
        // USE THROUGH ATTACK UNIT
        // ========================================================

        bool used =
            attackUnit.Attack(
                targetObject,
                ability
            );


        if (!used)
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Ability " +
                    $"{ability.GetAbilityName()} " +
                    $"failed at {targetTile}."
                );
            }


            return false;
        }


        // ========================================================
        // SUCCESS
        // ========================================================

        if (debugClick)
        {
            Debug.Log(
                $"[UIManager] Ability USED -> " +
                $"{ability.GetAbilityName()} " +
                $"at tile {targetTile}. " +
                $"Uses remaining: " +
                $"{attackUnit.GetAbilityUsesRemaining(ability)}",
                CurrentSelection
            );
        }


        ClearSelectedAbility();


        return true;
    }


    // ============================================================
    // FIND OBJECT ON TILE
    // ============================================================

    private GameObject FindObjectOnTile(
        Vector2Int tile,
        GridManager gridManager)
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
                        HoverInfoTrigger
                    >();


            if (trigger != null)
            {
                return trigger.gameObject;
            }
        }


        return null;
    }


    // ============================================================
    // TILE WORLD POSITION
    // ============================================================

    private Vector3 GetTileWorldPosition(
        GridManager gridManager,
        Vector2Int tile)
    {
        if (gridManager == null)
        {
            return Vector3.zero;
        }


        return gridManager.GridToWorldPosition(
            tile
        );
    }


    // ============================================================
    // CLEAR SELECTED ABILITY
    // ============================================================

    private void ClearSelectedAbility()
    {
        if (canvasInfoManager == null)
        {
            return;
        }


        canvasInfoManager.ClearSelectedAbility();
    }


    // ============================================================
    // PLAYER MOVEMENT
    // ============================================================

    private bool TryMoveSelectedUnit(
        Vector2 mousePosition)
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
            attackUnit.GetComponent<UnitMoveBrain>();


        if (moveBrain == null)
        {
            if (debugClick)
            {
                Debug.LogWarning(
                    "[UIManager] Selected unit has no " +
                    "UnitMoveBrain.",
                    attackUnit
                );
            }


            return false;
        }


        // ========================================================
        // DON'T ALLOW SECOND MOVE
        // ========================================================

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


        if (!gridManager.IsInsideGrid(
                destination))
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
                destination))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cell {destination} " +
                    $"is outside movement range."
                );
            }


            return false;
        }


        if (gridManager.IsCellOccupied(
                destination))
        {
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cannot move to " +
                    $"{destination}. " +
                    $"Cell is occupied."
                );
            }


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
            if (debugClick)
            {
                Debug.Log(
                    $"[UIManager] Cannot move to " +
                    $"{destination}. " +
                    $"Distance {distance} > " +
                    $"move range {moveRange}."
                );
            }


            return false;
        }


        // ========================================================
        // START MOVEMENT
        // ========================================================

        bool started =
            moveBrain.TryMoveTo(
                destination
            );


        if (!started)
        {
            return false;
        }


        highlightManager.ClearMovementRange();


        if (debugClick)
        {
            Debug.Log(
                $"[UIManager] Player movement: " +
                $"{currentPosition} -> {destination}",
                attackUnit
            );
        }


        return true;
    }


    // ============================================================
    // SELECT OBJECT
    // ============================================================

    public static void SelectObject(
        HoverInfoTrigger trigger)
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


        trigger.SetSelected(
            true
        );


        Debug.Log(
            $"[UIManager] SELECTED -> " +
            $"{trigger.gameObject.name} | " +
            $"Message: {trigger.HoverMessage}",
            trigger
        );


        ShowMovementRange(
            trigger
        );


        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance
                .ShowHoverInfo();
        }
    }


    // ============================================================
    // SHOW MOVEMENT RANGE
    // ============================================================

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


        if (
            attackUnit.GetTeam() ==
            Team.Enemy
        )
        {
            return;
        }


        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();


        if (moveBrain == null)
        {
            return;
        }


        if (!moveBrain.CanMoveThisTurn())
        {
            if (
                Instance != null &&
                Instance.debugClick
            )
            {
                Debug.Log(
                    $"[UIManager] Movement range blocked for " +
                    $"{trigger.gameObject.name}. " +
                    $"Movement already consumed."
                );
            }


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
            Instance.canvasInfoManager.ClearInfo();
        }


        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance
                .HideHoverInfo();
        }
    }


    // ============================================================
    // CLEAR MOVEMENT RANGE
    // ============================================================

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


    // ============================================================
    // CLEAR SPECIFIC
    // ============================================================

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