using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightBrain brain;

    [Header("Refresh")]
    [SerializeField] private bool refreshGridOnEnable = true;

    [Header("Movement")]
    [SerializeField] private Color movementRangeColor = Color.cyan;

    [SerializeField, Range(0f, 1f)]
    private float movementRangeAlpha = 0.35f;

    [Header("Enemy Hover")]
    [SerializeField] private Color enemyHoverColor = Color.red;
    [SerializeField] private bool enableEnemyHoverShader = true;

    [SerializeField]
    private string enemyHoverObjectName = "HoverShaderSprite";

    [Header("Heal Hover")]
    [SerializeField] private Color healHoverColor = Color.green;
    [SerializeField] private bool enableHealHoverShader = true;

    [SerializeField]
    private string healHoverObjectName = "HoverShaderSprite";

    [Header("Target Pulse")]
    [SerializeField] private bool enableTargetPulse = true;

    [SerializeField, Range(0f, 0.25f)]
    private float targetPulseAmount = 0.06f;

    [SerializeField, Min(0.01f)]
    private float targetPulseSpeed = 5f;

    [SerializeField, Min(0.01f)]
    private float targetPulseSmoothSpeed = 12f;

    [Header("Explosion")]
    [SerializeField, Min(0.01f)]
    private float explosionPulseDuration = 0.12f;


    // ============================================================
    // PLAYER INTERACTION LOCK
    // ============================================================

    /*
     * This is different from CombatUtility.SetPlayerInputLocked().
     *
     * CombatUtility prevents the player from performing actions.
     *
     * This flag prevents player movement/ability highlights
     * from being created again while the enemy is taking its turn.
     */
    private bool playerHighlightsDisabled;


    // ============================================================
    // TILE DATA
    // ============================================================

    private readonly Dictionary<Vector2Int, GridHighlightVisuals> tileVisuals =
        new Dictionary<Vector2Int, GridHighlightVisuals>(256);

    private readonly HashSet<Vector2Int> abilityCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int> movementCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int> explosionCells =
        new HashSet<Vector2Int>();


    // ============================================================
    // ABILITY OUTLINES
    // ============================================================

    private readonly HashSet<HoverInfoTrigger> activeAbilityOutlines =
        new HashSet<HoverInfoTrigger>();


    // ============================================================
    // UNIT CACHE
    // ============================================================

    private readonly Dictionary<GameObject, AttackUnit> unitComponentCache =
        new Dictionary<GameObject, AttackUnit>(32);


    // ============================================================
    // TEMP
    // ============================================================

    private readonly List<Vector2Int> tempCellList =
        new List<Vector2Int>(64);

    private readonly Dictionary<Transform, Vector3> targetOriginalScales =
        new Dictionary<Transform, Vector3>(16);

    private readonly HashSet<Transform> activeTargetPulseTransforms =
        new HashSet<Transform>();

    private readonly List<Transform> tempTargetTransformList =
        new List<Transform>(16);

    private readonly List<HoverInfoTrigger> tempHoverList =
        new List<HoverInfoTrigger>(16);


    // ============================================================
    // CURRENT STATE
    // ============================================================

    private Vector2Int placementPosition;
    private bool hasPlacementPosition;

    private GameObject currentRangeUser;
    private AttackUnit currentRangeUserUnit;

    private AbilitySO currentAbility;

    private bool suppressMovementHighlight;
    private bool currentAbilityIsHeal;


    // ============================================================
    // SHOTGUN REFRESH STATE
    // ============================================================

    private Vector2Int lastShotgunDirection;
    private bool hasLastShotgunDirection;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        RefreshGrid();
    }

    private void OnEnable()
    {
        if (refreshGridOnEnable)
        {
            StartCoroutine(RefreshGridDelayed());
        }
    }

    private IEnumerator RefreshGridDelayed()
    {
        yield return null;
        RefreshGrid();
    }

    private void Update()
    {
        RefreshAbilityPreview();
        UpdateTargetPulse();
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (brain == null)
        {
            brain = FindFirstObjectByType<GridHighlightBrain>();
        }
    }


    // ============================================================
    // PLAYER HIGHLIGHT LOCK
    // ============================================================

    public void SetPlayerHighlightsDisabled(bool disabled)
    {
        playerHighlightsDisabled = disabled;

        if (disabled)
        {
            ClearPlayerHighlights();
        }
    }

    public bool ArePlayerHighlightsDisabled()
    {
        return playerHighlightsDisabled;
    }

    private bool IsPlayerUnit(GameObject unit)
    {
        if (unit == null)
        {
            return false;
        }

        AttackUnit attackUnit =
            GetCachedAttackUnit(unit);

        if (attackUnit == null)
        {
            return false;
        }

        Team team =
            attackUnit.GetTeam();

        return team == Team.Player ||
               team == Team.Ally;
    }

    private bool IsCurrentUserPlayerUnit()
    {
        return IsPlayerUnit(currentRangeUser);
    }

    private void ClearPlayerHighlights()
    {
        // Clear movement.
        ClearMovementRange();

        // Clear ability/attack range.
        ClearAbilityRange();

        // Clear placement.
        ClearPlacementTile();

        // Clear target visuals.
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        // Make sure the visual tiles are actually reset.
        foreach (
            GridHighlightVisuals visual
            in tileVisuals.Values)
        {
            if (visual != null)
            {
                visual.Reset();
            }
        }

        // Reset current player highlight owner.
        currentRangeUser = null;
        currentRangeUserUnit = null;

        currentAbility = null;
        currentAbilityIsHeal = false;

        suppressMovementHighlight = false;

        ResetShotgunDirection();
    }


    // ============================================================
    // GRID REFRESH
    // ============================================================

    public void RefreshGrid()
    {
        FindReferences();

        if (gridManager == null)
        {
            return;
        }

        tileVisuals.Clear();
        unitComponentCache.Clear();

        CacheTiles();
        RefreshAllVisibleCells();
    }

    private void CacheTiles()
    {
        int minX = gridManager.GetMinX();
        int maxX = gridManager.GetMaxX();
        int minY = gridManager.GetMinY();
        int maxY = gridManager.GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                CacheTile(
                    new Vector2Int(x, y)
                );
            }
        }
    }

    private GridHighlightVisuals CacheTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return null;
        }

        if (
            tileVisuals.TryGetValue(
                position,
                out GridHighlightVisuals existing
            )
        )
        {
            if (existing != null)
            {
                return existing;
            }

            tileVisuals.Remove(position);
        }

        GameObject tile =
            gridManager.GetFloorTile(position);

        if (tile == null)
        {
            return null;
        }

        if (
            !tile.TryGetComponent(
                out GridHighlightVisuals visuals
            )
        )
        {
            visuals =
                tile.AddComponent<GridHighlightVisuals>();
        }

        visuals.Initialize(tile);

        tileVisuals[position] = visuals;

        return visuals;
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private void RefreshAllVisibleCells()
    {
        foreach (
            GridHighlightVisuals visual
            in tileVisuals.Values)
        {
            if (visual != null)
            {
                visual.Reset();
            }
        }

        if (!playerHighlightsDisabled)
        {
            RefreshCells(movementCells);
            RefreshCells(abilityCells);

            if (hasPlacementPosition)
            {
                RefreshTile(placementPosition);
            }

            RefreshAllTargetHovers();
        }
    }

    private void RefreshCells(
        HashSet<Vector2Int> cells)
    {
        if (
            cells == null ||
            cells.Count == 0
        )
        {
            return;
        }

        foreach (Vector2Int position in cells)
        {
            RefreshTile(position);
        }
    }

    private void RefreshTile(
        Vector2Int position)
    {
        GridHighlightVisuals visual =
            CacheTile(position);

        if (visual == null)
        {
            return;
        }

        if (explosionCells.Contains(position))
        {
            return;
        }

        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            visual.Reset();
            return;
        }

        if (
            hasPlacementPosition &&
            placementPosition == position)
        {
            visual.ShowPlacement();
            return;
        }

        if (abilityCells.Contains(position))
        {
            GameObject unit =
                gridManager.GetUnitAt(position);

            if (IsValidAbilityTarget(unit))
            {
                if (currentAbilityIsHeal)
                {
                    visual.ShowHeal();
                }
                else
                {
                    visual.ShowEnemy();
                }
            }
            else
            {
                visual.ShowAbility();
            }

            return;
        }

        if (
            !suppressMovementHighlight &&
            movementCells.Contains(position))
        {
            visual.ShowMovement(
                movementRangeColor,
                movementRangeAlpha
            );

            return;
        }

        visual.Reset();
    }


    // ============================================================
    // MOVEMENT
    // ============================================================

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        FindReferences();

        if (brain == null)
        {
            return;
        }

        /*
         * If the enemy turn is active, completely reject
         * movement highlighting for player/ally units.
         */
        if (
            playerHighlightsDisabled &&
            IsPlayerUnit(user)
        )
        {
            return;
        }

        brain.ShowMovementRange(
            centerPosition,
            range,
            user
        );
    }

    public void ShowMovementTiles(
        List<Vector2Int> cells,
        GameObject user = null)
    {
        /*
         * This is especially important because GridHighlightBrain
         * can call this again when movement events fire.
         */
        if (
            playerHighlightsDisabled &&
            IsPlayerUnit(user)
        )
        {
            ClearMovementRange();
            return;
        }

        ClearMovementRange();

        SetCurrentRangeUser(user);

        if (
            cells == null ||
            cells.Count == 0
        )
        {
            return;
        }

        for (
            int i = 0;
            i < cells.Count;
            i++)
        {
            Vector2Int position = cells[i];

            if (
                gridManager != null &&
                gridManager.IsInsideGrid(position)
            )
            {
                movementCells.Add(position);
            }
        }

        RefreshCells(movementCells);
    }

    public void ClearMovementRange()
    {
        if (movementCells.Count == 0)
        {
            return;
        }

        tempCellList.Clear();
        tempCellList.AddRange(movementCells);

        movementCells.Clear();

        for (
            int i = 0;
            i < tempCellList.Count;
            i++)
        {
            RefreshTile(
                tempCellList[i]
            );
        }

        tempCellList.Clear();
    }

    public bool IsMovementCell(
        Vector2Int position)
    {
        return movementCells.Contains(
            position
        );
    }

    public bool HasMovementRange()
    {
        return movementCells.Count > 0;
    }


    // ============================================================
    // MOVEMENT SUPPRESSION
    // ============================================================

    public void SetMovementHighlightSuppressed(
        bool suppressed)
    {
        if (
            suppressMovementHighlight ==
            suppressed
        )
        {
            return;
        }

        suppressMovementHighlight =
            suppressed;

        RefreshCells(movementCells);
    }

    public bool IsMovementHighlightSuppressed()
    {
        return suppressMovementHighlight;
    }


    // ============================================================
    // ABILITY TILES
    // ============================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ShowAbilityTiles(
            positions,
            user,
            false
        );
    }

    public void ShowHealTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ShowAbilityTiles(
            positions,
            user,
            true
        );
    }

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user,
        bool isHealAbility)
    {
        /*
         * Prevent player/ally ability highlighting during
         * the enemy turn.
         */
        if (
            playerHighlightsDisabled &&
            IsPlayerUnit(user)
        )
        {
            ClearAbilityRange();
            return;
        }

        ClearAbilityRange();

        currentAbilityIsHeal =
            isHealAbility;

        SetCurrentRangeUser(user);

        SetMovementHighlightSuppressed(true);

        if (positions != null)
        {
            for (
                int i = 0;
                i < positions.Count;
                i++)
            {
                Vector2Int position =
                    positions[i];

                if (
                    gridManager != null &&
                    gridManager.IsInsideGrid(position)
                )
                {
                    abilityCells.Add(position);
                }
            }
        }

        RefreshCells(abilityCells);
        RefreshAllTargetHovers();

        ResetShotgunDirection();
    }


    // ============================================================
    // SHOTGUN LIVE REFRESH
    // ============================================================

    public void RefreshAbilityPreview()
    {
        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return;
        }

        AbilitySO ability =
            currentAbility;

        if (
            ability == null ||
            currentRangeUser == null ||
            gridManager == null
        )
        {
            return;
        }

        if (
            ability.GetRangeShape() !=
            AbilitySO.RangeShape.Shotgun
        )
        {
            return;
        }

        Vector2Int newDirection =
            GetCurrentMouseDirection();

        if (
            hasLastShotgunDirection &&
            newDirection ==
            lastShotgunDirection
        )
        {
            return;
        }

        lastShotgunDirection =
            newDirection;

        hasLastShotgunDirection = true;

        RefreshShotgunTiles(
            newDirection
        );
    }

    private Vector2Int GetCurrentMouseDirection()
    {
        GameObject user =
            currentRangeUser;

        if (
            user == null ||
            Camera.main == null ||
            UnityEngine.InputSystem.Mouse.current == null
        )
        {
            return Vector2Int.up;
        }

        Vector2 mouseScreenPosition =
            UnityEngine.InputSystem.Mouse.current
                .position
                .ReadValue();

        Camera cam =
            Camera.main;

        Vector3 mouseWorldPosition =
            cam.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    Mathf.Abs(
                        cam.transform.position.z
                    )
                )
            );

        Vector2Int mouseTile =
            gridManager.WorldToGridPosition(
                mouseWorldPosition
            );

        Vector2Int userTile =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        Vector2Int difference =
            mouseTile - userTile;

        if (
            difference ==
            Vector2Int.zero
        )
        {
            return Vector2Int.up;
        }

        if (
            Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y)
        )
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        return difference.y > 0
            ? Vector2Int.up
            : Vector2Int.down;
    }

    private void RefreshShotgunTiles(
        Vector2Int direction)
    {
        if (
            currentAbility == null ||
            currentRangeUser == null ||
            gridManager == null
        )
        {
            return;
        }

        tempCellList.Clear();
        tempCellList.AddRange(
            abilityCells
        );

        abilityCells.Clear();

        for (
            int i = 0;
            i < tempCellList.Count;
            i++)
        {
            RefreshTile(
                tempCellList[i]
            );
        }

        tempCellList.Clear();

        List<Vector2Int> newTiles =
            currentAbility.GetRangeTiles(
                gridManager,
                currentRangeUser
            );

        if (newTiles != null)
        {
            for (
                int i = 0;
                i < newTiles.Count;
                i++)
            {
                Vector2Int position =
                    newTiles[i];

                if (
                    gridManager.IsInsideGrid(
                        position
                    )
                )
                {
                    abilityCells.Add(
                        position
                    );
                }
            }
        }

        RefreshCells(abilityCells);
        RefreshAllTargetHovers();
    }

    private void ResetShotgunDirection()
    {
        hasLastShotgunDirection = false;
        lastShotgunDirection =
            Vector2Int.zero;
    }

    public void ShowAbilityCell(
        Vector2Int position)
    {
        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return;
        }

        if (
            gridManager == null ||
            !gridManager.IsInsideGrid(position)
        )
        {
            return;
        }

        SetMovementHighlightSuppressed(true);

        abilityCells.Add(position);

        RefreshTile(position);
        RefreshTargetHoverForTile(position);
    }

    public void ClearAbilityRange()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        currentAbility = null;
        currentAbilityIsHeal = false;

        ResetShotgunDirection();

        if (abilityCells.Count == 0)
        {
            SetMovementHighlightSuppressed(false);
            return;
        }

        tempCellList.Clear();
        tempCellList.AddRange(
            abilityCells
        );

        abilityCells.Clear();

        for (
            int i = 0;
            i < tempCellList.Count;
            i++)
        {
            RefreshTile(
                tempCellList[i]
            );
        }

        tempCellList.Clear();

        SetMovementHighlightSuppressed(false);
    }

    public bool IsAbilityCell(
        Vector2Int position)
    {
        return abilityCells.Contains(
            position
        );
    }


    // ============================================================
    // CURRENT ABILITY
    // ============================================================

    public void SetCurrentAbility(
        AbilitySO ability)
    {
        if (currentAbility == ability)
        {
            return;
        }

        currentAbility = ability;

        ResetShotgunDirection();
        RefreshAllTargetHovers();
    }

    public AbilitySO GetCurrentAbility()
    {
        return currentAbility;
    }

    public bool IsCurrentAbilityHeal()
    {
        return currentAbilityIsHeal;
    }


    // ============================================================
    // ACTIVATE ABILITY
    // ============================================================

    public bool TryActivateAbilityOnUnit(
        GameObject target)
    {
        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return false;
        }

        if (
            target == null ||
            currentAbility == null ||
            currentRangeUserUnit == null
        )
        {
            return false;
        }

        if (gridManager == null)
        {
            FindReferences();
        }

        if (gridManager == null)
        {
            return false;
        }

        Vector2Int targetPosition =
            gridManager.GetUnitGridPosition(
                target
            );

        if (
            !abilityCells.Contains(
                targetPosition
            ) ||
            !IsValidAbilityTarget(target)
        )
        {
            return false;
        }

        AttackUnit userUnit =
            currentRangeUserUnit;

        if (
            !userUnit.IsAbilityReady(
                currentAbility
            )
        )
        {
            return false;
        }

        bool started =
            userUnit.PlayerAttack(
                target,
                currentAbility
            );

        if (!started)
        {
            return false;
        }

        ClearAbilityTargetVisualsOnly();

        return true;
    }


    // ============================================================
    // CLEAR TARGET VISUALS
    // ============================================================

    private void ClearAbilityTargetVisualsOnly()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        RefreshCells(abilityCells);
    }


    // ============================================================
    // CURRENT RANGE USER
    // ============================================================

    private void SetCurrentRangeUser(
        GameObject user)
    {
        currentRangeUser = user;

        currentRangeUserUnit =
            GetCachedAttackUnit(user);
    }

    private AttackUnit GetCachedAttackUnit(
        GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        if (
            unitComponentCache.TryGetValue(
                target,
                out AttackUnit cached
            )
        )
        {
            if (cached != null)
            {
                return cached;
            }

            unitComponentCache.Remove(
                target
            );
        }

        if (
            !target.TryGetComponent(
                out AttackUnit unit
            )
        )
        {
            return null;
        }

        unitComponentCache[target] =
            unit;

        return unit;
    }

    public AttackUnit GetCurrentRangeUserUnit()
    {
        return currentRangeUserUnit;
    }

    public GameObject GetCurrentRangeUser()
    {
        return currentRangeUser;
    }


    // ============================================================
    // TARGET VALIDATION
    // ============================================================

    private bool IsValidAbilityTarget(
        GameObject target)
    {
        AttackUnit userUnit =
            currentRangeUserUnit;

        if (
            userUnit == null ||
            target == null
        )
        {
            return false;
        }

        AttackUnit targetUnit =
            GetCachedAttackUnit(target);

        if (
            targetUnit == null ||
            targetUnit.IsDead()
        )
        {
            return false;
        }

        Team userTeam =
            userUnit.GetTeam();

        Team targetTeam =
            targetUnit.GetTeam();

        if (currentAbility != null)
        {
            switch (
                currentAbility.GetTargetType()
            )
            {
                case AbilitySO.TargetType.Enemy:

                    return IsEnemyTeam(
                        userTeam,
                        targetTeam
                    );

                case AbilitySO.TargetType.Ally:

                    return IsAllyTeam(
                        userTeam,
                        targetTeam
                    );

                case AbilitySO.TargetType.Any:

                    return true;
            }
        }

        return currentAbilityIsHeal
            ? IsAllyTeam(
                userTeam,
                targetTeam
            )
            : IsEnemyTeam(
                userTeam,
                targetTeam
            );
    }

    private bool IsEnemyTeam(
        Team userTeam,
        Team targetTeam)
    {
        if (
            userTeam == Team.Player ||
            userTeam == Team.Ally
        )
        {
            return targetTeam ==
                   Team.Enemy;
        }

        if (userTeam == Team.Enemy)
        {
            return targetTeam == Team.Player ||
                   targetTeam == Team.Ally;
        }

        return false;
    }

    private bool IsAllyTeam(
        Team userTeam,
        Team targetTeam)
    {
        if (
            userTeam == Team.Player ||
            userTeam == Team.Ally
        )
        {
            return targetTeam == Team.Player ||
                   targetTeam == Team.Ally;
        }

        if (userTeam == Team.Enemy)
        {
            return targetTeam ==
                   Team.Enemy;
        }

        return false;
    }

    public bool IsValidCurrentAbilityTarget(
        GameObject target)
    {
        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return false;
        }

        if (
            target == null ||
            currentRangeUserUnit == null ||
            currentAbility == null ||
            abilityCells.Count == 0 ||
            gridManager == null
        )
        {
            return false;
        }

        Vector2Int position =
            gridManager.GetUnitGridPosition(
                target
            );

        return abilityCells.Contains(
            position
        ) &&
        IsValidAbilityTarget(target);
    }


    // ============================================================
    // TARGET HOVERS
    // ============================================================

    private void RefreshAllTargetHovers()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return;
        }

        if (
            gridManager == null ||
            abilityCells.Count == 0
        )
        {
            return;
        }

        foreach (
            Vector2Int position
            in abilityCells)
        {
            RefreshTargetHoverForTile(
                position
            );
        }
    }

    private void RefreshTargetHoverForTile(
        Vector2Int position)
    {
        if (
            gridManager == null ||
            !abilityCells.Contains(position)
        )
        {
            return;
        }

        GameObject unit =
            gridManager.GetUnitAt(position);

        if (
            unit == null ||
            !IsValidAbilityTarget(unit)
        )
        {
            return;
        }

        if (currentAbilityIsHeal)
        {
            if (enableHealHoverShader)
            {
                SetTargetHover(
                    unit,
                    healHoverColor,
                    healHoverObjectName
                );
            }
        }
        else
        {
            if (enableEnemyHoverShader)
            {
                SetTargetHover(
                    unit,
                    enemyHoverColor,
                    enemyHoverObjectName
                );
            }
        }

        if (enableTargetPulse)
        {
            AddTargetPulse(
                unit.transform
            );
        }
    }

    private void SetTargetHover(
        GameObject targetUnit,
        Color outlineColor,
        string hoverObjectName)
    {
        if (targetUnit == null)
        {
            return;
        }

        if (
            !targetUnit.TryGetComponent(
                out HoverInfoTrigger hoverInfo
            )
        )
        {
            return;
        }

        hoverInfo.SetAbilityRangeOutline(
            true,
            outlineColor
        );

        activeAbilityOutlines.Add(
            hoverInfo
        );
    }

    private void ClearAllTargetHovers()
    {
        if (activeAbilityOutlines.Count == 0)
        {
            return;
        }

        foreach (
            HoverInfoTrigger hoverInfo
            in activeAbilityOutlines)
        {
            if (hoverInfo != null)
            {
                hoverInfo.SetAbilityRangeOutline(
                    false,
                    Color.white
                );
            }
        }

        activeAbilityOutlines.Clear();
    }


    // ============================================================
    // TARGET PULSE
    // ============================================================

    private void AddTargetPulse(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (
            !targetOriginalScales.ContainsKey(
                target
            )
        )
        {
            targetOriginalScales[target] =
                target.localScale;
        }

        activeTargetPulseTransforms.Add(
            target
        );
    }

    private void UpdateTargetPulse()
    {
        if (!enableTargetPulse)
        {
            if (
                activeTargetPulseTransforms.Count > 0 ||
                targetOriginalScales.Count > 0
            )
            {
                ResetTargetPulseScales();

                activeTargetPulseTransforms.Clear();
            }

            return;
        }

        if (
            activeTargetPulseTransforms.Count == 0
        )
        {
            return;
        }

        float pulse =
            (Mathf.Sin(
                Time.time * targetPulseSpeed
            ) + 1f) * 0.5f;

        float multiplier =
            1f +
            pulse *
            targetPulseAmount;

        float lerpFactor =
            Mathf.Clamp01(
                Time.deltaTime *
                targetPulseSmoothSpeed
            );

        tempTargetTransformList.Clear();

        foreach (
            Transform target
            in activeTargetPulseTransforms)
        {
            if (target != null)
            {
                tempTargetTransformList.Add(
                    target
                );
            }
        }

        for (
            int i = 0;
            i < tempTargetTransformList.Count;
            i++)
        {
            Transform target =
                tempTargetTransformList[i];

            if (
                !targetOriginalScales.TryGetValue(
                    target,
                    out Vector3 originalScale
                )
            )
            {
                originalScale =
                    target.localScale;

                targetOriginalScales[target] =
                    originalScale;
            }

            Vector3 desiredScale =
                originalScale * multiplier;

            target.localScale =
                Vector3.Lerp(
                    target.localScale,
                    desiredScale,
                    lerpFactor
                );
        }

        tempTargetTransformList.Clear();
    }

    private void ClearAllTargetPulse()
    {
        ResetTargetPulseScales();

        activeTargetPulseTransforms.Clear();
    }

    private void ResetTargetPulseScales()
    {
        if (targetOriginalScales.Count == 0)
        {
            return;
        }

        foreach (
            KeyValuePair<
                Transform,
                Vector3
            > pair
            in targetOriginalScales)
        {
            if (pair.Key != null)
            {
                pair.Key.localScale =
                    pair.Value;
            }
        }

        targetOriginalScales.Clear();
    }


    // ============================================================
    // PLACEMENT
    // ============================================================

    public void SetPlacementTile(
        Vector2Int position)
    {
        if (
            playerHighlightsDisabled &&
            IsCurrentUserPlayerUnit()
        )
        {
            return;
        }

        if (
            gridManager == null ||
            !gridManager.IsInsideGrid(position)
        )
        {
            ClearPlacementTile();
            return;
        }

        if (
            hasPlacementPosition &&
            placementPosition == position
        )
        {
            return;
        }

        Vector2Int oldPosition =
            placementPosition;

        bool hadOldPosition =
            hasPlacementPosition;

        placementPosition =
            position;

        hasPlacementPosition = true;

        if (hadOldPosition)
        {
            RefreshTile(oldPosition);
        }

        RefreshTile(position);
    }

    public void ClearPlacementTile()
    {
        if (!hasPlacementPosition)
        {
            return;
        }

        Vector2Int oldPosition =
            placementPosition;

        hasPlacementPosition = false;

        RefreshTile(oldPosition);
    }

    public bool HasPlacementTile()
    {
        return hasPlacementPosition;
    }

    public Vector2Int GetPlacementTile()
    {
        return placementPosition;
    }

    public bool IsPlacementCell(
        Vector2Int position)
    {
        return hasPlacementPosition &&
               placementPosition == position;
    }


    // ============================================================
    // EXPLOSION
    // ============================================================

    public void FlashExplosionTile(
        Vector2Int position)
    {
        if (
            gridManager == null ||
            !gridManager.IsInsideGrid(position)
        )
        {
            return;
        }

        GridHighlightVisuals visual =
            CacheTile(position);

        if (visual == null)
        {
            return;
        }

        explosionCells.Add(position);

        visual.PlayExplosion();

        StartCoroutine(
            RemoveExplosionCellAfterDelay(
                position
            )
        );
    }

    private IEnumerator RemoveExplosionCellAfterDelay(
        Vector2Int position)
    {
        yield return new WaitForSeconds(
            explosionPulseDuration * 2f
        );

        explosionCells.Remove(
            position
        );

        RefreshTile(position);
    }


    // ============================================================
    // CLEAR
    // ============================================================

    public void ClearAllHighlights()
    {
        ClearPlacementTile();
        ClearMovementRange();
        ClearAbilityRange();

        ClearAllTargetHovers();
        ClearAllTargetPulse();

        explosionCells.Clear();

        suppressMovementHighlight = false;

        currentAbility = null;
        currentAbilityIsHeal = false;

        currentRangeUser = null;
        currentRangeUserUnit = null;

        ResetShotgunDirection();

        foreach (
            GridHighlightVisuals visual
            in tileVisuals.Values)
        {
            if (visual != null)
            {
                visual.Reset();
            }
        }
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public GridHighlightBrain GetBrain()
    {
        return brain;
    }

    public Color GetMovementRangeColor()
    {
        return movementRangeColor;
    }

    public float GetMovementRangeAlpha()
    {
        return movementRangeAlpha;
    }


    // ============================================================
    // ROTATION
    // ============================================================

    public IEnumerator RefreshAfterBoardRotation()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        RefreshGrid();
    }
}