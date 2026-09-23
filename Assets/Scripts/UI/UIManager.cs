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

    [Header("Chain Lightning")]
    [SerializeField]
    private GridChainHighlight gridChainHighlight;

    private CanvasInfoManager canvasInfoManager;

    // =========================================================
    // UNITY
    // =========================================================

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
            clickCamera = Camera.main;
        }

        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();

        if (gridChainHighlight == null)
        {
            gridChainHighlight =
                FindFirstObjectByType<GridChainHighlight>();
        }
    }

    private void Update()
    {
        CheckRightClick();
        CheckMouseClick();
        UpdateChainLightningPreview();
    }

    // =========================================================
    // RIGHT CLICK
    // =========================================================

    private void CheckRightClick()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (
            !Mouse.current
                .rightButton
                .wasPressedThisFrame
        )
        {
            return;
        }

        // Clear ability first.
        if (HasSelectedAbility())
        {
            ClearSelectedAbility(true);
            return;
        }

        // Otherwise clear unit selection.
        if (CurrentSelection != null)
        {
            ClearSelection();
        }
    }

    // =========================================================
    // LEFT CLICK
    // =========================================================

    private void CheckMouseClick()
    {
        if (
            Mouse.current == null ||
            clickCamera == null
        )
        {
            return;
        }

        if (
            !Mouse.current
                .leftButton
                .wasPressedThisFrame
        )
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current
                .position
                .ReadValue();

        // Ability UI gets checked first.
        if (TryHandleAbilityUI())
        {
            return;
        }

        Ray ray =
            clickCamera.ScreenPointToRay(
                mousePosition
            );

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                raycastDistance,
                clickLayers
            );

        HoverInfoTrigger clickedTrigger =
            GetClickedTrigger(hit);

        // Ability is currently selected.
        if (HasSelectedAbility())
        {
            TryUseSelectedAbility(
                mousePosition,
                clickedTrigger
            );

            return;
        }

        // Clicked a unit.
        if (clickedTrigger != null)
        {
            SelectObject(clickedTrigger);
            return;
        }

        // Try moving the selected unit.
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

        // Nothing useful was clicked.
        ClearSelection();
    }

    // =========================================================
    // ABILITY UI
    // =========================================================

    private bool TryHandleAbilityUI()
    {
        if (canvasInfoManager == null)
        {
            return false;
        }

        bool clickedAbility =
            canvasInfoManager
                .TrySelectAbilityUnderMouse();

        if (!clickedAbility)
        {
            return false;
        }

        AbilitySO selectedAbility =
            canvasInfoManager
                .GetSelectedAbility();

        if (selectedAbility == null)
        {
            return true;
        }

        if (CurrentSelection == null)
        {
            return true;
        }

        AttackUnit attackUnit =
            CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
        {
            return true;
        }

        Team team =
            attackUnit.GetTeam();

        if (team == Team.Enemy)
        {
            return true;
        }

        if (
            team != Team.Player &&
            team != Team.Ally
        )
        {
            return true;
        }

        // =====================================================
        // IMPORTANT CAMERA RULE
        // =====================================================
        //
        // If the camera is FREE:
        //     DO NOT MOVE IT.
        //
        // If the camera is LOCKED:
        //     Move to the ability camera position.
        //

        if (
            CanvasJuiceManager.Instance != null &&
            CanvasJuiceManager.Instance.IsInspectMode()
        )
        {
            CanvasJuiceManager.Instance
                .MoveCameraToAbilityPosition();
        }

        return true;
    }

    // =========================================================
    // CLICKED UNIT
    // =========================================================

    private HoverInfoTrigger GetClickedTrigger(
        RaycastHit2D hit)
    {
        if (hit.collider == null)
        {
            return null;
        }

        HoverInfoTrigger trigger =
            hit.collider
                .GetComponentInParent<
                    HoverInfoTrigger
                >();

        return trigger;
    }

    // =========================================================
    // CHAIN LIGHTNING
    // =========================================================

    private void UpdateChainLightningPreview()
    {
        if (gridChainHighlight == null)
        {
            return;
        }

        if (
            CurrentSelection == null ||
            canvasInfoManager == null ||
            !canvasInfoManager
                .HasSelectedAbility()
        )
        {
            gridChainHighlight.EndPreview();
            return;
        }

        AbilitySO ability =
            canvasInfoManager
                .GetSelectedAbility();

        if (
            ability is not ChainLightning
            chainLightning
        )
        {
            gridChainHighlight.EndPreview();
            return;
        }

        AttackUnit attackUnit =
            CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            gridChainHighlight.EndPreview();
            return;
        }

        Team team =
            attackUnit.GetTeam();

        if (
            team != Team.Player &&
            team != Team.Ally
        )
        {
            gridChainHighlight.EndPreview();
            return;
        }

        bool ready =
            attackUnit.IsAbilityReady(
                chainLightning
            );

        if (!ready)
        {
            gridChainHighlight.EndPreview();
            return;
        }

        gridChainHighlight.BeginPreview(
            attackUnit.gameObject,
            chainLightning
        );
    }

    // =========================================================
    // ABILITY STATE
    // =========================================================

    public bool HasSelectedAbility()
    {
        return
            canvasInfoManager != null &&
            canvasInfoManager
                .HasSelectedAbility();
    }

    private void ClearSelectedAbility(
        bool returnCameraToUnit = true)
    {
        if (gridChainHighlight != null)
        {
            gridChainHighlight
                .EndPreview();
        }

        if (canvasInfoManager != null)
        {
            canvasInfoManager
                .ClearSelectedAbility();
        }

        // =====================================================
        // IMPORTANT CAMERA RULE
        // =====================================================
        //
        // Only return to the unit if LOCK MODE is active.
        //
        // In FREE MODE:
        //     Do not touch the camera.
        //

        if (
            returnCameraToUnit &&
            CurrentSelection != null &&
            CanvasJuiceManager.Instance != null &&
            CanvasJuiceManager.Instance.IsInspectMode()
        )
        {
            CanvasJuiceManager.Instance
                .MoveCameraToUnit(
                    CurrentSelection.transform
                );
        }
    }

    // =========================================================
    // USE SELECTED ABILITY
    // =========================================================

    private bool TryUseSelectedAbility(
        Vector2 mousePosition,
        HoverInfoTrigger clickedTrigger)
    {
        if (
            CurrentSelection == null ||
            canvasInfoManager == null
        )
        {
            return false;
        }

        GameObject selectedObject =
            CurrentSelection.gameObject;

        if (
            !IsPlayerControlledUnit(
                selectedObject
            )
        )
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

        AttackUnit attackUnit =
            CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            return false;
        }

        bool abilityReady =
            attackUnit.IsAbilityReady(
                ability
            );

        if (!abilityReady)
        {
            return false;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<
                UnitMoveBrain
            >();

        if (moveBrain == null)
        {
            return false;
        }

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return false;
        }

        // =====================================================
        // TARGETED UNIT
        // =====================================================

        if (clickedTrigger != null)
        {
            GameObject targetObject =
                clickedTrigger.gameObject;

            if (targetObject == null)
            {
                return false;
            }

            AttackUnit targetUnit =
                targetObject.GetComponent<
                    AttackUnit
                >();

            if (targetUnit == null)
            {
                targetUnit =
                    targetObject.GetComponentInParent<
                        AttackUnit
                    >();
            }

            if (targetUnit == null)
            {
                return false;
            }

            Vector2Int targetTile =
                gridManager.GetUnitGridPosition(
                    targetObject
                );

            if (
                !gridManager
                    .IsInsideGrid(
                        targetTile
                    )
            )
            {
                return false;
            }

            List<Vector2Int> rangeTiles =
                ability.GetRangeTiles(
                    gridManager,
                    selectedObject
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

            if (
                !ability.CanHit(
                    gridManager,
                    selectedObject,
                    targetObject
                )
            )
            {
                return false;
            }

            return UseNormalAbility(
                attackUnit,
                ability,
                targetObject,
                gridManager
            );
        }

        // =====================================================
        // TARGET TILE
        // =====================================================

        Vector2Int targetTileFromMouse =
            ScreenToGridPosition(
                mousePosition,
                gridManager
            );

        if (
            !gridManager
                .IsInsideGrid(
                    targetTileFromMouse
                )
        )
        {
            return false;
        }

        List<Vector2Int> abilityTiles =
            ability.GetRangeTiles(
                gridManager,
                selectedObject
            );

        if (
            abilityTiles == null ||
            !abilityTiles.Contains(
                targetTileFromMouse
            )
        )
        {
            return false;
        }

        if (ability is BombAttack)
        {
            return UseBombAbility(
                attackUnit,
                ability,
                targetTileFromMouse
            );
        }

        return false;
    }

    // =========================================================
    // BOMB
    // =========================================================

    private bool UseBombAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        Vector2Int targetTile)
    {
        bool used =
            attackUnit.AttackAtTile(
                targetTile,
                ability
            );

        if (!used)
        {
            return false;
        }

        // Ability clears without moving camera.
        ClearSelectedAbility(false);

        return true;
    }

    // =========================================================
    // NORMAL ABILITY
    // =========================================================

    private bool UseNormalAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        GameObject targetObject,
        GridManager gridManager)
    {
        if (
            attackUnit == null ||
            ability == null ||
            targetObject == null ||
            gridManager == null
        )
        {
            return false;
        }

        if (
            !ability.CanHit(
                gridManager,
                attackUnit.gameObject,
                targetObject
            )
        )
        {
            return false;
        }

        bool used =
            attackUnit.Attack(
                targetObject,
                ability
            );

        if (!used)
        {
            return false;
        }

        // Ability clears without moving camera.
        ClearSelectedAbility(false);

        return true;
    }

    // =========================================================
    // SCREEN TO GRID
    // =========================================================

    private Vector2Int ScreenToGridPosition(
        Vector2 screenPosition,
        GridManager gridManager)
    {
        Vector3 worldPosition =
            clickCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    Mathf.Abs(
                        clickCamera
                            .transform
                            .position
                            .z
                    )
                )
            );

        return
            gridManager
                .WorldToGridPosition(
                    worldPosition
                );
    }

    // =========================================================
    // PLAYER CONTROL CHECK
    // =========================================================

    private bool IsPlayerControlledUnit(
        GameObject unit)
    {
        if (unit == null)
        {
            return false;
        }

        AttackUnit attackUnit =
            unit.GetComponent<
                AttackUnit
            >();

        if (attackUnit == null)
        {
            attackUnit =
                unit.GetComponentInParent<
                    AttackUnit
                >();
        }

        if (attackUnit == null)
        {
            return false;
        }

        Team team =
            attackUnit.GetTeam();

        bool controlled =
            team == Team.Player ||
            team == Team.Ally;

        return controlled;
    }

    // =========================================================
    // MOVE SELECTED UNIT
    // =========================================================

    private bool TryMoveSelectedUnit(
        Vector2 mousePosition)
    {
        if (CurrentSelection == null)
        {
            return false;
        }

        GameObject selectedObject =
            CurrentSelection.gameObject;

        if (
            !IsPlayerControlledUnit(
                selectedObject
            )
        )
        {
            return false;
        }

        AttackUnit attackUnit =
            CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            return false;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<
                UnitMoveBrain
            >();

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

        Vector2Int destination =
            ScreenToGridPosition(
                mousePosition,
                gridManager
            );

        Vector2Int currentPosition =
            gridManager
                .WorldToGridPosition(
                    attackUnit.transform.position
                );

        if (destination == currentPosition)
        {
            return false;
        }

        if (
            !gridManager
                .IsInsideGrid(
                    destination
                )
        )
        {
            return false;
        }

        GridHighlightManager
            highlightManager =
                gridManager
                    .GetHighlightManager();

        if (highlightManager == null)
        {
            return false;
        }

        if (
            !highlightManager
                .IsMovementCell(
                    destination
                )
        )
        {
            return false;
        }

        if (
            gridManager
                .IsCellOccupied(
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

        return true;
    }

    // =========================================================
    // SELECT OBJECT
    // =========================================================

    public static void SelectObject(
        HoverInfoTrigger trigger)
    {
        if (
            trigger == null ||
            CurrentSelection == trigger
        )
        {
            return;
        }

        if (CurrentSelection != null)
        {
            HoverInfoTrigger previousSelection =
                CurrentSelection;

            previousSelection.SetSelected(
                false,
                false
            );

            ClearMovementRange(
                previousSelection
            );
        }

        CurrentSelection =
            trigger;

        trigger.SetSelected(
            true
        );

        ShowMovementRange(
            trigger
        );

        // CanvasJuiceManager itself handles whether
        // the camera is locked or in free mode.
        //
        // In free mode this will NOT move the camera.
        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance
                .ShowUnitInfo(
                    trigger.transform
                );
        }
    }

    // =========================================================
    // CLEAR SELECTION
    // =========================================================

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
        {
            return;
        }

        HoverInfoTrigger previousSelection =
            CurrentSelection;

        previousSelection.SetSelected(
            false,
            true
        );

        ClearMovementRange(
            previousSelection
        );

        CurrentSelection = null;

        if (Instance != null)
        {
            if (
                Instance.gridChainHighlight != null
            )
            {
                Instance.gridChainHighlight
                    .EndPreview();
            }

            if (
                Instance.canvasInfoManager != null
            )
            {
                Instance.canvasInfoManager
                    .ClearInfo();
            }
        }

        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance
                .HideHoverInfo();
        }
    }

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

    // =========================================================
    // MOVEMENT RANGE
    // =========================================================

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

        Team team =
            attackUnit.GetTeam();

        if (team == Team.Enemy)
        {
            return;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<
                UnitMoveBrain
            >();

        if (moveBrain == null)
        {
            return;
        }

        if (!moveBrain.CanMoveThisTurn())
        {
            return;
        }

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
        {
            return;
        }

        GridHighlightManager
            highlightManager =
                gridManager
                    .GetHighlightManager();

        if (highlightManager == null)
        {
            return;
        }

        Vector2Int position =
            gridManager
                .WorldToGridPosition(
                    trigger.transform.position
                );

        int moveRange =
            moveBrain.GetMoveRange();

        highlightManager
            .ShowMovementRange(
                position,
                moveRange,
                trigger.gameObject
            );
    }

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
            attackUnit.GetComponent<
                UnitMoveBrain
            >();

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

        GridHighlightManager
            highlightManager =
                gridManager
                    .GetHighlightManager();

        if (highlightManager != null)
        {
            highlightManager
                .ClearMovementRange();
        }
    }
}