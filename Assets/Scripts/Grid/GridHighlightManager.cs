using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightBrain brain;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool enableWarningLogs = true;
    [SerializeField] private bool enableErrorLogs = true;

    [Header("Refresh")]
    [SerializeField] private bool refreshGridOnEnable = true;

    [Header("Movement")]
    [SerializeField] private Color movementRangeColor = Color.cyan;
    [SerializeField, Range(0f, 1f)] private float movementRangeAlpha = 0.35f;

    [Header("Enemy Hover")]
    [SerializeField] private Color enemyHoverColor = Color.red;
    [SerializeField] private bool enableEnemyHoverShader = true;
    [SerializeField] private string enemyHoverObjectName = "HoverShaderSprite";

    [Header("Heal Hover")]
    [SerializeField] private Color healHoverColor = Color.green;
    [SerializeField] private bool enableHealHoverShader = true;
    [SerializeField] private string healHoverObjectName = "HoverShaderSprite";

    [Header("Target Pulse")]
    [SerializeField] private bool enableTargetPulse = true;
    [SerializeField, Min(0f)] private float targetPulseAmount = 0.06f;
    [SerializeField, Min(0.01f)] private float targetPulseSpeed = 5f;
    [SerializeField, Min(0.01f)] private float targetPulseSmoothSpeed = 12f;

    [Header("Explosion")]
    [SerializeField, Min(0.01f)] private float explosionPulseDuration = 0.12f;

    private readonly Dictionary<Vector2Int, GridHighlightVisuals> tileVisuals =
        new Dictionary<Vector2Int, GridHighlightVisuals>(256);

    private readonly HashSet<Vector2Int> abilityCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int> movementCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int> explosionCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<SpriteRenderer> activeTargetHoverRenderers =
        new HashSet<SpriteRenderer>();

    private readonly Dictionary<GameObject, AttackUnit> unitComponentCache =
        new Dictionary<GameObject, AttackUnit>();

    private readonly List<Vector2Int> tempCellList =
        new List<Vector2Int>(64);

    private readonly List<SpriteRenderer> tempRendererList =
        new List<SpriteRenderer>(16);

    private readonly Dictionary<Transform, Vector3> targetOriginalScales =
        new Dictionary<Transform, Vector3>(16);

    private readonly HashSet<Transform> activeTargetPulseTransforms =
        new HashSet<Transform>();

    private readonly List<Transform> tempTargetTransformList =
        new List<Transform>(16);

    private MaterialPropertyBlock hoverPropertyBlock;

    private static readonly int OutlineColorID =
        Shader.PropertyToID("_OutlineColor");

    private Vector2Int placementPosition;
    private bool hasPlacementPosition;

    private GameObject currentRangeUser;
    private AttackUnit currentRangeUserUnit;

    private AbilitySO currentAbility;

    private bool suppressMovementHighlight;
    private bool currentAbilityIsHeal;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        hoverPropertyBlock = new MaterialPropertyBlock();
        FindReferences();
        DebugLog("Awake completed.");
    }

    private void Start()
    {
        RefreshGrid();
    }

    private void OnEnable()
    {
        if (refreshGridOnEnable)
            StartCoroutine(RefreshGridDelayed());
    }

    private IEnumerator RefreshGridDelayed()
    {
        yield return null;
        RefreshGrid();
    }

    private void Update()
    {
        UpdateTargetPulse();
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[GridHighlightManager] " + message, this);
    }

    private void DebugWarning(string message)
    {
        if (enableWarningLogs)
            Debug.LogWarning("[GridHighlightManager] " + message, this);
    }

    private void DebugError(string message)
    {
        if (enableErrorLogs)
            Debug.LogError("[GridHighlightManager] " + message, this);
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (brain == null)
            brain = FindFirstObjectByType<GridHighlightBrain>();

        if (gridManager == null)
            DebugError("GridManager reference could not be found.");

        if (brain == null)
            DebugWarning("GridHighlightBrain reference could not be found.");
    }


    // ============================================================
    // GRID REFRESH
    // ============================================================

    public void RefreshGrid()
    {
        FindReferences();

        if (gridManager == null)
        {
            DebugError("RefreshGrid failed because GridManager is missing.");
            return;
        }

        tileVisuals.Clear();
        CacheTiles();
        unitComponentCache.Clear();
        RefreshAllVisibleCells();

        DebugLog("Grid refreshed. Cached tiles: " + tileVisuals.Count);
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
                CacheTile(new Vector2Int(x, y));
        }
    }

    private GridHighlightVisuals CacheTile(Vector2Int position)
    {
        if (gridManager == null)
            return null;

        if (tileVisuals.TryGetValue(position, out GridHighlightVisuals existing))
        {
            if (existing != null)
            {
                existing.Initialize(existing.gameObject);
                return existing;
            }

            tileVisuals.Remove(position);
        }

        GameObject tile = gridManager.GetFloorTile(position);

        if (tile == null)
        {
            DebugWarning("No floor tile found at " + position + ".");
            return null;
        }

        if (!tile.TryGetComponent(out GridHighlightVisuals visuals))
            visuals = tile.AddComponent<GridHighlightVisuals>();

        visuals.Initialize(tile);
        tileVisuals[position] = visuals;

        return visuals;
    }

    public void RebuildTileCache()
    {
        RefreshGrid();
    }

    public void RefreshHighlights()
    {
        RefreshGrid();
    }


    // ============================================================
    // REFRESH CELLS
    // ============================================================

    private void RefreshAllVisibleCells()
    {
        if (gridManager == null)
            return;

        foreach (GridHighlightVisuals visual in tileVisuals.Values)
        {
            if (visual != null)
                visual.Reset();
        }

        RefreshCells(movementCells);
        RefreshCells(abilityCells);

        if (hasPlacementPosition)
            RefreshTile(placementPosition);

        RefreshAllTargetHovers();
    }

    private void RefreshCells(HashSet<Vector2Int> cells)
    {
        if (cells == null)
            return;

        foreach (Vector2Int position in cells)
            RefreshTile(position);
    }

    private void RefreshTile(Vector2Int position)
    {
        GridHighlightVisuals visual = CacheTile(position);

        if (visual == null)
            return;

        if (explosionCells.Contains(position))
            return;

        if (IsPlacementCell(position))
        {
            visual.ShowPlacement();
            return;
        }

        if (abilityCells.Contains(position))
        {
            GameObject unit = gridManager.GetUnitAt(position);

            if (IsValidAbilityTarget(unit))
            {
                if (currentAbilityIsHeal)
                    visual.ShowHeal();
                else
                    visual.ShowEnemy();
            }
            else
            {
                visual.ShowAbility();
            }

            return;
        }

        if (!suppressMovementHighlight &&
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
        if (brain == null)
            FindReferences();

        if (brain == null)
        {
            DebugError(
                "ShowMovementRange failed because GridHighlightBrain is missing."
            );
            return;
        }

        brain.ShowMovementRange(centerPosition, range, user);

        DebugLog(
            "Movement range requested. Center: " +
            centerPosition +
            ", Range: " +
            range
        );
    }

    public void ShowMovementTiles(
        List<Vector2Int> cells,
        GameObject user = null)
    {
        ClearMovementRange();
        SetCurrentRangeUser(user);

        if (cells == null)
        {
            DebugWarning("ShowMovementTiles received a null cell list.");
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int position = cells[i];

            if (gridManager != null &&
                gridManager.IsInsideGrid(position))
            {
                movementCells.Add(position);
            }
        }

        RefreshCells(movementCells);

        DebugLog("Movement tiles shown: " + movementCells.Count);
    }

    public void ClearMovementRange()
    {
        if (movementCells.Count > 0)
        {
            tempCellList.Clear();
            tempCellList.AddRange(movementCells);
            movementCells.Clear();

            for (int i = 0; i < tempCellList.Count; i++)
                RefreshTile(tempCellList[i]);

            tempCellList.Clear();
        }

        if (brain != null)
            brain.ClearMovementHighlightState();

        DebugLog("Movement range cleared.");
    }

    public bool IsMovementCell(Vector2Int position)
    {
        return movementCells.Contains(position);
    }

    public bool HasMovementRange()
    {
        return movementCells.Count > 0;
    }


    // ============================================================
    // MOVEMENT SUPPRESSION
    // ============================================================

    public void SetMovementHighlightSuppressed(bool suppressed)
    {
        if (suppressMovementHighlight == suppressed)
            return;

        suppressMovementHighlight = suppressed;
        RefreshCells(movementCells);

        DebugLog(
            "Movement highlight suppression: " +
            suppressed
        );
    }

    public bool IsMovementHighlightSuppressed()
    {
        return suppressMovementHighlight;
    }


    // ============================================================
    // ABILITY / HEAL
    // ============================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ShowAbilityTiles(positions, user, false);
    }

    public void ShowHealTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ShowAbilityTiles(positions, user, true);
    }

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user,
        bool isHealAbility)
    {
        ClearAbilityRange();

        currentAbilityIsHeal = isHealAbility;
        SetCurrentRangeUser(user);
        SetMovementHighlightSuppressed(true);

        if (positions != null)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int position = positions[i];

                if (gridManager != null &&
                    gridManager.IsInsideGrid(position))
                {
                    abilityCells.Add(position);
                }
            }
        }

        RefreshCells(abilityCells);
        RefreshAllTargetHovers();

        DebugLog(
            "Ability tiles shown. Count: " +
            abilityCells.Count +
            ", Heal: " +
            currentAbilityIsHeal
        );
    }

    public void ShowAbilityCell(Vector2Int position)
    {
        SetMovementHighlightSuppressed(true);

        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            DebugWarning(
                "Attempted to show ability cell outside the grid: " +
                position
            );
            return;
        }

        if (!abilityCells.Add(position))
        {
            RefreshTile(position);
            RefreshTargetHoverForTile(position);
            return;
        }

        RefreshTile(position);
        RefreshTargetHoverForTile(position);
    }

    public void ClearAbilityRange()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        currentAbility = null;
        currentAbilityIsHeal = false;

        if (abilityCells.Count == 0)
        {
            SetMovementHighlightSuppressed(false);
            return;
        }

        tempCellList.Clear();
        tempCellList.AddRange(abilityCells);
        abilityCells.Clear();

        for (int i = 0; i < tempCellList.Count; i++)
            RefreshTile(tempCellList[i]);

        tempCellList.Clear();

        SetMovementHighlightSuppressed(false);

        DebugLog("Ability range cleared.");
    }

    public bool IsAbilityCell(Vector2Int position)
    {
        return abilityCells.Contains(position);
    }

    public bool IsCurrentAbilityHeal()
    {
        return currentAbilityIsHeal;
    }

    public void SetCurrentAbility(AbilitySO ability)
    {
        currentAbility = ability;
        RefreshAllTargetHovers();

        DebugLog(
            "Current ability changed: " +
            (ability != null ? ability.name : "None")
        );
    }

    public AbilitySO GetCurrentAbility()
    {
        return currentAbility;
    }


    // ============================================================
    // CURRENT RANGE USER
    // ============================================================

    private void SetCurrentRangeUser(GameObject user)
    {
        currentRangeUser = user;
        currentRangeUserUnit = GetCachedAttackUnit(user);
    }

    private AttackUnit GetCachedAttackUnit(GameObject target)
    {
        if (target == null)
            return null;

        if (unitComponentCache.TryGetValue(target, out AttackUnit cached))
        {
            if (cached != null)
                return cached;

            unitComponentCache.Remove(target);
        }

        if (!target.TryGetComponent(out AttackUnit unit))
            return null;

        unitComponentCache[target] = unit;
        return unit;
    }

    public void UncacheUnit(GameObject unit)
    {
        if (unit != null)
            unitComponentCache.Remove(unit);
    }


    // ============================================================
    // TARGET VALIDATION
    // ============================================================

    private bool IsValidAbilityTarget(GameObject target)
    {
        if (currentRangeUserUnit == null || target == null)
            return false;

        AttackUnit targetUnit = GetCachedAttackUnit(target);

        if (targetUnit == null || targetUnit.IsDead())
            return false;

        Team userTeam = currentRangeUserUnit.GetTeam();
        Team targetTeam = targetUnit.GetTeam();

        if (currentAbility != null)
        {
            switch (currentAbility.GetTargetType())
            {
                case AbilitySO.TargetType.Enemy:
                    return IsEnemyTeam(userTeam, targetTeam);

                case AbilitySO.TargetType.Ally:
                    return IsAllyTeam(userTeam, targetTeam);

                case AbilitySO.TargetType.Any:
                    return true;
            }
        }

        return currentAbilityIsHeal
            ? IsAllyTeam(userTeam, targetTeam)
            : IsEnemyTeam(userTeam, targetTeam);
    }

    private bool IsEnemyTeam(Team userTeam, Team targetTeam)
    {
        if (userTeam == Team.Player || userTeam == Team.Ally)
            return targetTeam == Team.Enemy;

        if (userTeam == Team.Enemy)
            return targetTeam == Team.Player ||
                   targetTeam == Team.Ally;

        return false;
    }

    private bool IsAllyTeam(Team userTeam, Team targetTeam)
    {
        if (userTeam == Team.Player || userTeam == Team.Ally)
            return targetTeam == Team.Player ||
                   targetTeam == Team.Ally;

        if (userTeam == Team.Enemy)
            return targetTeam == Team.Enemy;

        return false;
    }

    public bool IsValidCurrentAbilityTarget(GameObject target)
    {
        if (target == null ||
            currentRangeUserUnit == null ||
            abilityCells.Count == 0)
        {
            return false;
        }

        return IsValidAbilityTarget(target);
    }


    // ============================================================
    // TARGET HOVERS
    // ============================================================

    private void RefreshAllTargetHovers()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        if (gridManager == null)
            return;

        foreach (Vector2Int position in abilityCells)
            RefreshTargetHoverForTile(position);
    }

    private void RefreshTargetHoverForTile(Vector2Int position)
    {
        if (gridManager == null ||
            !abilityCells.Contains(position))
        {
            return;
        }

        GameObject unit = gridManager.GetUnitAt(position);

        if (unit == null || !IsValidAbilityTarget(unit))
            return;

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
        else if (enableEnemyHoverShader)
        {
            SetTargetHover(
                unit,
                enemyHoverColor,
                enemyHoverObjectName
            );
        }

        if (enableTargetPulse)
            AddTargetPulse(unit.transform);
    }

    private void SetTargetHover(
        GameObject targetUnit,
        Color outlineColor,
        string hoverObjectName)
    {
        if (targetUnit == null)
            return;

        Transform hoverShader =
            targetUnit.transform.Find(hoverObjectName);

        if (hoverShader == null)
        {
            SpriteRenderer childRenderer =
                targetUnit.GetComponentInChildren<SpriteRenderer>(true);

            if (childRenderer != null &&
                childRenderer.gameObject != targetUnit)
            {
                hoverShader = childRenderer.transform;
            }
        }

        if (hoverShader == null ||
            !hoverShader.TryGetComponent(out SpriteRenderer renderer))
        {
            DebugWarning(
                "Could not find hover SpriteRenderer on target: " +
                targetUnit.name
            );
            return;
        }

        renderer.GetPropertyBlock(hoverPropertyBlock);
        hoverPropertyBlock.SetColor(OutlineColorID, outlineColor);
        renderer.SetPropertyBlock(hoverPropertyBlock);
        renderer.enabled = true;

        activeTargetHoverRenderers.Add(renderer);
    }

    private void ClearAllTargetHovers()
    {
        if (activeTargetHoverRenderers.Count == 0)
            return;

        tempRendererList.Clear();
        tempRendererList.AddRange(activeTargetHoverRenderers);
        activeTargetHoverRenderers.Clear();

        for (int i = 0; i < tempRendererList.Count; i++)
        {
            SpriteRenderer renderer = tempRendererList[i];

            if (renderer == null)
                continue;

            renderer.enabled = false;
            renderer.SetPropertyBlock(null);
        }

        tempRendererList.Clear();
    }


    // ============================================================
    // TARGET PULSE
    // ============================================================

    private void AddTargetPulse(Transform target)
    {
        if (target == null)
            return;

        if (!targetOriginalScales.ContainsKey(target))
            targetOriginalScales[target] = target.localScale;

        activeTargetPulseTransforms.Add(target);
    }

    private void UpdateTargetPulse()
    {
        if (!enableTargetPulse)
        {
            ResetTargetPulseScales();
            return;
        }

        if (activeTargetPulseTransforms.Count == 0)
            return;

        float pulse =
            (Mathf.Sin(Time.time * targetPulseSpeed) + 1f) * 0.5f;

        float multiplier =
            1f + pulse * targetPulseAmount;

        float lerpFactor =
            Mathf.Clamp01(Time.deltaTime * targetPulseSmoothSpeed);

        tempTargetTransformList.Clear();
        tempTargetTransformList.AddRange(activeTargetPulseTransforms);

        for (int i = 0; i < tempTargetTransformList.Count; i++)
        {
            Transform target = tempTargetTransformList[i];

            if (target == null)
                continue;

            if (!targetOriginalScales.TryGetValue(
                    target,
                    out Vector3 originalScale))
            {
                originalScale = target.localScale;
                targetOriginalScales[target] = originalScale;
            }

            Vector3 desiredScale = originalScale * multiplier;

            target.localScale = Vector3.Lerp(
                target.localScale,
                desiredScale,
                lerpFactor
            );
        }

        tempTargetTransformList.Clear();
    }

    private void ClearAllTargetPulse()
    {
        if (activeTargetPulseTransforms.Count == 0 &&
            targetOriginalScales.Count == 0)
        {
            return;
        }

        ResetTargetPulseScales();
        activeTargetPulseTransforms.Clear();
    }

    private void ResetTargetPulseScales()
    {
        if (targetOriginalScales.Count == 0)
            return;

        float lerpFactor =
            Mathf.Clamp01(Time.deltaTime * targetPulseSmoothSpeed);

        tempTargetTransformList.Clear();

        foreach (KeyValuePair<Transform, Vector3> pair in targetOriginalScales)
            tempTargetTransformList.Add(pair.Key);

        for (int i = 0; i < tempTargetTransformList.Count; i++)
        {
            Transform target = tempTargetTransformList[i];

            if (target == null)
                continue;

            if (!targetOriginalScales.TryGetValue(
                    target,
                    out Vector3 originalScale))
            {
                continue;
            }

            target.localScale = Vector3.Lerp(
                target.localScale,
                originalScale,
                lerpFactor
            );

            if ((target.localScale - originalScale).sqrMagnitude < 0.000001f)
                target.localScale = originalScale;
        }

        tempTargetTransformList.Clear();

        foreach (KeyValuePair<Transform, Vector3> pair in targetOriginalScales)
        {
            Transform target = pair.Key;

            if (target == null ||
                (target.localScale - pair.Value).sqrMagnitude < 0.000001f)
            {
                tempTargetTransformList.Add(target);
            }
        }

        for (int i = 0; i < tempTargetTransformList.Count; i++)
            targetOriginalScales.Remove(tempTargetTransformList[i]);

        tempTargetTransformList.Clear();
    }


    // ============================================================
    // PLACEMENT
    // ============================================================

    public void SetPlacementTile(Vector2Int position)
    {
        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            ClearPlacementTile();
            return;
        }

        if (hasPlacementPosition &&
            placementPosition == position)
        {
            return;
        }

        Vector2Int oldPosition = placementPosition;
        bool hadOldPosition = hasPlacementPosition;

        placementPosition = position;
        hasPlacementPosition = true;

        if (hadOldPosition)
            RefreshTile(oldPosition);

        RefreshTile(position);

        DebugLog("Placement tile changed to " + position);
    }

    public void ClearPlacementTile()
    {
        if (!hasPlacementPosition)
            return;

        Vector2Int oldPosition = placementPosition;
        hasPlacementPosition = false;

        RefreshTile(oldPosition);

        DebugLog("Placement tile cleared.");
    }

    public bool HasPlacementTile()
    {
        return hasPlacementPosition;
    }

    public Vector2Int GetPlacementTile()
    {
        return placementPosition;
    }

    public bool IsPlacementCell(Vector2Int position)
    {
        return hasPlacementPosition &&
               placementPosition == position;
    }


    // ============================================================
    // EXPLOSION
    // ============================================================

    public void FlashExplosionTile(Vector2Int position)
    {
        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            DebugWarning(
                "Explosion requested outside the grid: " +
                position
            );
            return;
        }

        GridHighlightVisuals visual = CacheTile(position);

        if (visual == null)
            return;

        explosionCells.Add(position);
        visual.PlayExplosion();

        StartCoroutine(
            RemoveExplosionCellAfterDelay(position)
        );
    }

    private IEnumerator RemoveExplosionCellAfterDelay(Vector2Int position)
    {
        yield return new WaitForSeconds(explosionPulseDuration * 2f);

        explosionCells.Remove(position);
        RefreshTile(position);
    }


    // ============================================================
    // CLEAR ALL
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

        foreach (GridHighlightVisuals visual in tileVisuals.Values)
        {
            if (visual != null)
                visual.Reset();
        }

        DebugLog("All highlights cleared.");
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

    public GameObject GetCurrentRangeUser()
    {
        return currentRangeUser;
    }


    // ============================================================
    // BOARD ROTATION REFRESH
    // ============================================================

    public IEnumerator RefreshAfterBoardRotation()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        RefreshGrid();
    }
}