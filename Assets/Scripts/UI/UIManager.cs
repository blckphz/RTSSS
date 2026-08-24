using System.Collections.Generic;
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

    [Header("Movement")]
    [SerializeField] private bool allowPlayerMovement = true;

    [Header("Chain Lightning")]
    [SerializeField] private GridChainHighlight gridChainHighlight;

    private CanvasInfoManager canvasInfoManager;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (clickCamera == null)
            clickCamera = Camera.main;

        canvasInfoManager = FindFirstObjectByType<CanvasInfoManager>();

        if (gridChainHighlight == null)
            gridChainHighlight = FindFirstObjectByType<GridChainHighlight>();
    }

    private void Update()
    {
        CheckRightClick();
        CheckMouseClick();
        UpdateChainLightningPreview();
    }

    // ============================================================
    // INPUT
    // ============================================================

    private void CheckRightClick()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        if (HasSelectedAbility())
        {
            ClearSelectedAbility();
            return;
        }

        if (CurrentSelection != null)
            ClearSelection();
    }

    private void CheckMouseClick()
    {
        if (Mouse.current == null || clickCamera == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (TryHandleAbilityUI())
            return;

        Ray ray = clickCamera.ScreenPointToRay(mousePosition);

        RaycastHit2D hit = Physics2D.GetRayIntersection(
            ray,
            raycastDistance,
            clickLayers
        );

        HoverInfoTrigger clickedTrigger = GetClickedTrigger(hit);

        if (HasSelectedAbility())
        {
            TryUseSelectedAbility(mousePosition);
            return;
        }

        if (clickedTrigger != null)
        {
            SelectObject(clickedTrigger);
            return;
        }

        if (allowPlayerMovement && CurrentSelection != null)
        {
            if (TryMoveSelectedUnit(mousePosition))
                return;
        }

        ClearSelection();
    }

    private bool TryHandleAbilityUI()
    {
        return canvasInfoManager != null &&
               canvasInfoManager.TrySelectAbilityUnderMouse();
    }

    private HoverInfoTrigger GetClickedTrigger(RaycastHit2D hit)
    {
        if (hit.collider == null)
            return null;

        return hit.collider.GetComponentInParent<HoverInfoTrigger>();
    }

    // ============================================================
    // CHAIN LIGHTNING PREVIEW
    // ============================================================

    private void UpdateChainLightningPreview()
    {
        if (gridChainHighlight == null)
            return;

        if (CurrentSelection == null ||
            canvasInfoManager == null ||
            !canvasInfoManager.HasSelectedAbility())
        {
            gridChainHighlight.EndPreview();
            return;
        }

        AbilitySO ability = canvasInfoManager.GetSelectedAbility();

        if (ability is not ChainLightning chainLightning)
        {
            gridChainHighlight.EndPreview();
            return;
        }

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
        {
            gridChainHighlight.EndPreview();
            return;
        }

        Team team = attackUnit.GetTeam();

        if (team != Team.Player && team != Team.Ally)
        {
            gridChainHighlight.EndPreview();
            return;
        }

        if (!attackUnit.IsAbilityReady(chainLightning))
        {
            gridChainHighlight.EndPreview();
            return;
        }

        gridChainHighlight.BeginPreview(
            attackUnit.gameObject,
            chainLightning
        );
    }

    // ============================================================
    // ABILITY STATE
    // ============================================================

    private bool HasSelectedAbility()
    {
        return canvasInfoManager != null &&
               canvasInfoManager.HasSelectedAbility();
    }

    private void ClearSelectedAbility()
    {
        if (gridChainHighlight != null)
            gridChainHighlight.EndPreview();

        if (canvasInfoManager != null)
            canvasInfoManager.ClearSelectedAbility();
    }

    // ============================================================
    // ABILITY TARGETING
    // ============================================================

    private bool TryUseSelectedAbility(Vector2 mousePosition)
    {
        if (CurrentSelection == null ||
            canvasInfoManager == null)
        {
            return false;
        }

        GameObject selectedObject = CurrentSelection.gameObject;

        if (!IsPlayerControlledUnit(selectedObject))
            return false;

        AbilitySO ability = canvasInfoManager.GetSelectedAbility();

        if (ability == null)
            return false;

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null ||
            !attackUnit.IsAbilityReady(ability))
        {
            return false;
        }

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return false;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return false;

        Vector2Int targetTile = ScreenToGridPosition(
            mousePosition,
            gridManager
        );

        if (!gridManager.IsInsideGrid(targetTile))
            return false;

        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                selectedObject
            );

        if (rangeTiles == null ||
            !rangeTiles.Contains(targetTile))
        {
            return false;
        }

        if (ability is BombAttack)
        {
            return UseBombAbility(
                attackUnit,
                ability,
                targetTile
            );
        }

        return UseNormalAbility(
            attackUnit,
            ability,
            targetTile,
            gridManager
        );
    }

    private bool UseBombAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        Vector2Int targetTile)
    {
        bool used = attackUnit.AttackAtTile(
            targetTile,
            ability
        );

        if (!used)
            return false;

        ClearSelectedAbility();

        return true;
    }

    private bool UseNormalAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        Vector2Int targetTile,
        GridManager gridManager)
    {
        GameObject targetObject = FindObjectOnTile(
            targetTile,
            gridManager
        );

        if (targetObject == null)
            return false;

        if (!ability.CanHit(
                gridManager,
                CurrentSelection.gameObject,
                targetObject))
        {
            return false;
        }

        bool used = attackUnit.Attack(
            targetObject,
            ability
        );

        if (!used)
            return false;

        ClearSelectedAbility();

        return true;
    }

    // ============================================================
    // GRID / SCREEN CONVERSION
    // ============================================================

    private Vector2Int ScreenToGridPosition(
        Vector2 screenPosition,
        GridManager gridManager)
    {
        Vector3 worldPosition =
            clickCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    Mathf.Abs(clickCamera.transform.position.z)
                )
            );

        return gridManager.WorldToGridPosition(worldPosition);
    }

    private GameObject FindObjectOnTile(
        Vector2Int tile,
        GridManager gridManager)
    {
        if (gridManager == null)
            return null;

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(tile);

        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                worldPosition,
                0.35f,
                clickLayers
            );

        foreach (Collider2D collider in colliders)
        {
            if (collider == null)
                continue;

            HoverInfoTrigger trigger =
                collider.GetComponentInParent<HoverInfoTrigger>();

            if (trigger != null)
                return trigger.gameObject;
        }

        return null;
    }

    // ============================================================
    // UNIT CONTROL
    // ============================================================

    private bool IsPlayerControlledUnit(GameObject unit)
    {
        if (unit == null)
            return false;

        AttackUnit attackUnit =
            unit.GetComponent<AttackUnit>();

        if (attackUnit == null)
            return false;

        Team team = attackUnit.GetTeam();

        return team == Team.Player ||
               team == Team.Ally;
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private bool TryMoveSelectedUnit(Vector2 mousePosition)
    {
        if (CurrentSelection == null)
            return false;

        GameObject selectedObject =
            CurrentSelection.gameObject;

        if (!IsPlayerControlledUnit(selectedObject))
            return false;

        AttackUnit attackUnit =
            CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
            return false;

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return false;

        if (moveBrain.IsMoving())
            return true;

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
            return false;

        Vector2Int destination =
            ScreenToGridPosition(
                mousePosition,
                gridManager
            );

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                attackUnit.transform.position
            );

        if (destination == currentPosition)
            return false;

        if (!gridManager.IsInsideGrid(destination))
            return false;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
            return false;

        if (!highlightManager.IsMovementCell(destination))
            return false;

        if (gridManager.IsCellOccupied(destination))
            return false;

        int moveRange = moveBrain.GetMoveRange();

        int distance = gridManager.GetDistance(
            currentPosition,
            destination
        );

        if (distance > moveRange)
            return false;

        bool started =
            moveBrain.TryMoveTo(destination);

        if (!started)
            return false;

        highlightManager.ClearMovementRange();

        return true;
    }

    // ============================================================
    // SELECTION
    // ============================================================

    public static void SelectObject(
        HoverInfoTrigger trigger)
    {
        if (trigger == null ||
            CurrentSelection == trigger)
        {
            return;
        }

        if (CurrentSelection != null)
            ClearSelection();

        CurrentSelection = trigger;

        trigger.SetSelected(true);

        ShowMovementRange(trigger);

        if (CanvasJuiceManager.Instance != null)
            CanvasJuiceManager.Instance.ShowHoverInfo();
    }

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
            return;

        HoverInfoTrigger previousSelection =
            CurrentSelection;

        previousSelection.SetSelected(false);

        ClearMovementRange(previousSelection);

        CurrentSelection = null;

        if (Instance != null)
        {
            if (Instance.gridChainHighlight != null)
                Instance.gridChainHighlight.EndPreview();

            if (Instance.canvasInfoManager != null)
                Instance.canvasInfoManager.ClearInfo();
        }

        if (CanvasJuiceManager.Instance != null)
            CanvasJuiceManager.Instance.HideHoverInfo();
    }

    public static void ClearSelection(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return;

        if (CurrentSelection == trigger)
            ClearSelection();
    }

    // ============================================================
    // MOVEMENT RANGE
    // ============================================================

    private static void ShowMovementRange(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return;

        AttackUnit attackUnit =
            trigger.GetAttackUnit();

        if (attackUnit == null)
            return;

        Team team = attackUnit.GetTeam();

        if (team == Team.Enemy)
            return;

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return;

        if (!moveBrain.CanMoveThisTurn())
            return;

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
            return;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
            return;

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

    private static void ClearMovementRange(
        HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return;

        AttackUnit attackUnit =
            trigger.GetAttackUnit();

        if (attackUnit == null)
            return;

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return;

        GridManager gridManager =
            moveBrain.GetGridManager();

        if (gridManager == null)
            return;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager != null)
            highlightManager.ClearMovementRange();
    }
}