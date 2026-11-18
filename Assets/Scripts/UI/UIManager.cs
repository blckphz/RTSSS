
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

    [Header("Debug")]
    [SerializeField] private bool debugTurretHealing = true;

    private CanvasInfoManager canvasInfoManager;

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
        UpdateAbilityPreview();
    }

    private void LogTurret(string message)
    {
        if (debugTurretHealing)
            Debug.Log("[Turret Healing UI] " + message);
    }

    private void CheckRightClick()
    {
        if (Mouse.current == null ||
            !Mouse.current.rightButton.wasPressedThisFrame)
            return;

        if (HasSelectedAbility())
        {
            ClearSelectedAbility(true);
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
            AbilitySO selectedAbility =
                canvasInfoManager != null
                    ? canvasInfoManager.GetSelectedAbility()
                    : null;

            if (clickedTrigger != null &&
                IsPlayerControlledUnit(clickedTrigger.gameObject))
            {
                if (selectedAbility is FrontAttack)
                {
                    LogTurret(
                        "Friendly target clicked with FrontAttack. " +
                        "Attempting turret healing."
                    );

                    if (!TryUseFriendlyTurretHeal(
                            clickedTrigger.gameObject,
                            selectedAbility))
                    {
                        LogTurret(
                            "Friendly turret attack was rejected. " +
                            "The target was not selected."
                        );
                    }

                    return;
                }

                if (selectedAbility is ChainLightning)
                {
                    TryUseSelectedAbility(mousePosition, clickedTrigger);
                    return;
                }

                if (selectedAbility is HealAbilitySO)
                {
                    TryUseSelectedAbility(mousePosition, clickedTrigger);
                    return;
                }

                SelectObject(clickedTrigger);
                return;
            }

            TryUseSelectedAbility(mousePosition, clickedTrigger);
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

    private bool TryUseFriendlyTurretHeal(
        GameObject targetObject,
        AbilitySO ability)
    {
        if (targetObject == null || ability == null)
            return false;

        if (CurrentSelection == null)
        {
            LogTurret("No Engineer is selected.");
            return false;
        }

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
        {
            LogTurret("Selected unit has no AttackUnit.");
            return false;
        }

        if (attackUnit.GetTeam() != Team.Player &&
            attackUnit.GetTeam() != Team.Ally)
        {
            LogTurret("Selected attacker is not player-controlled.");
            return false;
        }

        UnitData unitData = attackUnit.GetUnitData();

        if (unitData == null)
        {
            LogTurret("Selected attacker has no UnitData.");
            return false;
        }

        if (!unitData.HasMeleeTurretHealUpgrade())
        {
            LogTurret("Engineer does not have the turret healing upgrade.");
            return false;
        }

        turretbehav turret = FindTurret(targetObject);

        if (turret == null)
        {
            LogTurret("Clicked friendly target has no turretbehav.");
            return false;
        }

        HealthManager turretHealth = FindHealthManager(targetObject);

        if (turretHealth == null)
        {
            LogTurret("Turret has no HealthManager.");
            return false;
        }

        if (turretHealth.IsDead())
        {
            LogTurret("Turret is dead.");
            return false;
        }

        if (turretHealth.GetTeam() != attackUnit.GetTeam())
        {
            LogTurret("Turret is not on the Engineer's team.");
            return false;
        }

        if (!attackUnit.IsAbilityReady(ability))
        {
            LogTurret("FrontAttack is not ready.");
            return false;
        }

        LogTurret(
            $"Calling Attack() from {attackUnit.name} " +
            $"on friendly turret {targetObject.name}."
        );

        bool used = attackUnit.PlayerAttack(targetObject, ability);

        if (!used)
        {
            LogTurret(
                "AttackUnit rejected the turret attack. " +
                "Check IsValidTarget() and FrontAttack.CanHit()."
            );

            return false;
        }

        LogTurret(
            $"Attack accepted. Attacker: {attackUnit.name}, " +
            $"target: {targetObject.name}."
        );

        ClearSelectedAbility(false);

        return true;
    }

    private turretbehav FindTurret(GameObject target)
    {
        if (target == null)
            return null;

        turretbehav turret = target.GetComponent<turretbehav>();

        if (turret == null)
            turret = target.GetComponentInParent<turretbehav>();

        if (turret == null)
            turret = target.GetComponentInChildren<turretbehav>();

        return turret;
    }

    private HealthManager FindHealthManager(GameObject target)
    {
        if (target == null)
            return null;

        HealthManager health = target.GetComponent<HealthManager>();

        if (health == null)
            health = target.GetComponentInParent<HealthManager>();

        if (health == null)
            health = target.GetComponentInChildren<HealthManager>();

        return health;
    }

    private bool TryHandleAbilityUI()
    {
        if (canvasInfoManager == null)
            return false;

        if (!canvasInfoManager.TrySelectAbilityUnderMouse())
            return false;

        AbilitySO selectedAbility = canvasInfoManager.GetSelectedAbility();

        if (selectedAbility == null || CurrentSelection == null)
            return true;

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
            return true;

        Team team = attackUnit.GetTeam();

        if (team != Team.Player && team != Team.Ally)
            return true;

        if (CanvasJuiceManager.Instance != null &&
            CanvasJuiceManager.Instance.IsInspectMode())
        {
            CanvasJuiceManager.Instance.MoveCameraToAbilityPosition();
        }

        return true;
    }

    private HoverInfoTrigger GetClickedTrigger(RaycastHit2D hit)
    {
        if (hit.collider == null)
            return null;

        return hit.collider.GetComponentInParent<HoverInfoTrigger>();
    }

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

    private void UpdateAbilityPreview()
    {
        if (canvasInfoManager == null ||
            CurrentSelection == null ||
            !canvasInfoManager.HasSelectedAbility())
            return;

        AbilitySO ability = canvasInfoManager.GetSelectedAbility();

        if (ability == null)
            return;

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
            return;

        Team team = attackUnit.GetTeam();

        if (team != Team.Player && team != Team.Ally)
            return;

        if (!attackUnit.IsAbilityReady(ability))
            return;

        UnitMoveBrain moveBrain = attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
            return;

        highlightManager.RefreshAbilityPreview();
    }

    public bool HasSelectedAbility()
    {
        return canvasInfoManager != null &&
               canvasInfoManager.HasSelectedAbility();
    }

    private void ClearSelectedAbility(bool returnCameraToUnit = true)
    {
        if (gridChainHighlight != null)
            gridChainHighlight.EndPreview();

        if (canvasInfoManager != null)
            canvasInfoManager.ClearSelectedAbility();

        if (returnCameraToUnit &&
            CurrentSelection != null &&
            CanvasJuiceManager.Instance != null &&
            CanvasJuiceManager.Instance.IsInspectMode())
        {
            CanvasJuiceManager.Instance.MoveCameraToUnit(
                CurrentSelection.transform
            );
        }
    }

    private bool TryUseSelectedAbility(
        Vector2 mousePosition,
        HoverInfoTrigger clickedTrigger)
    {
        if (CurrentSelection == null || canvasInfoManager == null)
            return false;

        GameObject selectedObject = CurrentSelection.gameObject;

        if (!IsPlayerControlledUnit(selectedObject))
            return false;

        AbilitySO ability = canvasInfoManager.GetSelectedAbility();

        if (ability == null)
            return false;

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null || !attackUnit.IsAbilityReady(ability))
            return false;

        UnitMoveBrain moveBrain = attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return false;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return false;

        if (ability is BearTrapAttack)
        {
            Vector2Int bearTrapTile =
                ScreenToGridPosition(mousePosition, gridManager);

            if (!gridManager.IsInsideGrid(bearTrapTile))
                return false;

            List<Vector2Int> bearTrapRange =
                ability.GetRangeTiles(gridManager, selectedObject);

            if (bearTrapRange == null ||
                !bearTrapRange.Contains(bearTrapTile))
                return false;

            return UseBearTrapAbility(
                attackUnit,
                ability,
                bearTrapTile
            );
        }

        if (clickedTrigger != null)
        {
            GameObject targetObject = clickedTrigger.gameObject;

            if (targetObject == null)
                return false;

            AttackUnit targetUnit =
                targetObject.GetComponent<AttackUnit>();

            if (targetUnit == null)
                targetUnit = targetObject.GetComponentInParent<AttackUnit>();

            if (targetUnit == null)
            {
                Debug.LogWarning(
                    $"[UIManager] Ability target '{targetObject.name}' " +
                    "has no AttackUnit."
                );

                return false;
            }

            if (ability is not ChainLightning)
            {
                List<Vector2Int> rangeTiles =
                    ability.GetRangeTiles(gridManager, selectedObject);

                if (rangeTiles == null ||
                    !IsAnyTargetFootprintTileInRange(
                        gridManager,
                        targetUnit.gameObject,
                        rangeTiles))
                {
                    return false;
                }
            }

            if (!ability.CanHit(
                    gridManager,
                    selectedObject,
                    targetUnit.gameObject))
            {
                return false;
            }

            return UseNormalAbility(
                attackUnit,
                ability,
                targetUnit.gameObject,
                gridManager
            );
        }

        Vector2Int targetTileFromMouse =
            ScreenToGridPosition(mousePosition, gridManager);

        if (!gridManager.IsInsideGrid(targetTileFromMouse))
            return false;

        List<Vector2Int> abilityTiles =
            ability.GetRangeTiles(gridManager, selectedObject);

        if (abilityTiles == null ||
            !abilityTiles.Contains(targetTileFromMouse))
            return false;

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

    private bool IsAnyTargetFootprintTileInRange(
        GridManager gridManager,
        GameObject targetObject,
        List<Vector2Int> rangeTiles)
    {
        if (gridManager == null ||
            targetObject == null ||
            rangeTiles == null)
            return false;

        AttackUnit targetUnit = targetObject.GetComponent<AttackUnit>();

        if (targetUnit == null)
            targetUnit = targetObject.GetComponentInParent<AttackUnit>();

        if (targetUnit == null)
            return false;

        Vector2Int anchor = targetUnit.GetLogicalGridPosition();

        CharacterSO character = targetUnit.GetCharacterData();

        int footprintSize = character != null
            ? Mathf.Max(1, character.GetFootprintSize())
            : Mathf.Max(
                1,
                gridManager.GetUnitFootprintSize(targetUnit.gameObject)
            );

        int minOffset = -(footprintSize / 2);

        for (int x = 0; x < footprintSize; x++)
        {
            for (int y = 0; y < footprintSize; y++)
            {
                Vector2Int tile = anchor + new Vector2Int(
                    minOffset + x,
                    minOffset + y
                );

                if (!gridManager.IsInsideGrid(tile))
                    continue;

                if (rangeTiles.Contains(tile))
                    return true;
            }
        }

        return false;
    }

    private bool UseBombAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        Vector2Int targetTile)
    {
        bool used = attackUnit.AttackAtTile(targetTile, ability);

        if (!used)
            return false;

        ClearSelectedAbility(false);
        return true;
    }

    private bool UseBearTrapAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        Vector2Int targetTile)
    {
        if (attackUnit == null || ability == null)
            return false;

        UnitMoveBrain moveBrain =
            attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return false;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return false;

        if (!ability.CanHitTile(
                gridManager,
                attackUnit.gameObject,
                targetTile))
            return false;

        bool used = attackUnit.AttackAtTile(targetTile, ability);

        if (!used)
            return false;

        ClearSelectedAbility(false);
        return true;
    }

    private bool UseNormalAbility(
        AttackUnit attackUnit,
        AbilitySO ability,
        GameObject targetObject,
        GridManager gridManager)
    {
        if (attackUnit == null ||
            ability == null ||
            targetObject == null ||
            gridManager == null)
            return false;

        bool used = attackUnit.PlayerAttack(targetObject, ability);

        if (!used)
            return false;

        ClearSelectedAbility(false);
        return true;
    }

    private Vector2Int ScreenToGridPosition(
        Vector2 screenPosition,
        GridManager gridManager)
    {
        Vector3 worldPosition = clickCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                Mathf.Abs(clickCamera.transform.position.z)
            )
        );

        return gridManager.WorldToGridPosition(worldPosition);
    }

    private bool IsPlayerControlledUnit(GameObject unit)
    {
        if (unit == null)
            return false;

        AttackUnit attackUnit = unit.GetComponent<AttackUnit>();

        if (attackUnit == null)
            attackUnit = unit.GetComponentInParent<AttackUnit>();

        if (attackUnit == null)
            return false;

        Team team = attackUnit.GetTeam();

        return team == Team.Player || team == Team.Ally;
    }

    private bool TryMoveSelectedUnit(Vector2 mousePosition)
    {
        if (CurrentSelection == null)
            return false;

        GameObject selectedObject = CurrentSelection.gameObject;

        if (!IsPlayerControlledUnit(selectedObject))
            return false;

        AttackUnit attackUnit = CurrentSelection.GetAttackUnit();

        if (attackUnit == null)
            return false;

        UnitMoveBrain moveBrain = attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return false;

        if (moveBrain.IsMoving())
            return true;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return false;

        Vector2Int destination =
            ScreenToGridPosition(mousePosition, gridManager);

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(attackUnit.transform.position);

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

        return moveBrain.TryMoveTo(destination);
    }

    public static void SelectObject(HoverInfoTrigger trigger)
    {
        if (trigger == null || CurrentSelection == trigger)
            return;

        if (CurrentSelection != null)
        {
            HoverInfoTrigger previousSelection = CurrentSelection;

            previousSelection.SetSelected(false, false);
            ClearMovementRange(previousSelection);
        }

        CurrentSelection = trigger;

        trigger.SetSelected(true);
        ShowMovementRange(trigger);

        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance.ShowUnitInfo(
                trigger.transform
            );
        }
    }

    public static void ClearSelection()
    {
        if (CurrentSelection == null)
            return;

        HoverInfoTrigger previousSelection = CurrentSelection;

        previousSelection.SetSelected(false, true);
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

    public static void ClearSelection(HoverInfoTrigger trigger)
    {
        if (trigger != null && CurrentSelection == trigger)
            ClearSelection();
    }

    private static void ShowMovementRange(HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return;

        AttackUnit attackUnit = trigger.GetAttackUnit();

        if (attackUnit == null || attackUnit.GetTeam() == Team.Enemy)
            return;

        UnitMoveBrain moveBrain = attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null || !moveBrain.CanMoveThisTurn())
            return;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager == null)
            return;

        Vector2Int position =
            gridManager.WorldToGridPosition(trigger.transform.position);

        int moveRange = moveBrain.GetMoveRange();

        highlightManager.ShowMovementRange(
            position,
            moveRange,
            trigger.gameObject
        );
    }

    private static void ClearMovementRange(HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return;

        AttackUnit attackUnit = trigger.GetAttackUnit();

        if (attackUnit == null)
            return;

        UnitMoveBrain moveBrain = attackUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            return;

        GridManager gridManager = moveBrain.GetGridManager();

        if (gridManager == null)
            return;

        GridHighlightManager highlightManager =
            gridManager.GetHighlightManager();

        if (highlightManager != null)
            highlightManager.ClearMovementRange();
    }
}
