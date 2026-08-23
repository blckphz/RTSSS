using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Brain Reference")]
    [SerializeField] private GridHighlightBrain brain;

    [Header("Movement Range")]
    [SerializeField] private Color movementRangeColor = Color.cyan;
    [SerializeField, Range(0f, 1f)] private float movementRangeAlpha = 0.35f;

    [Header("Enemy Target Hover")]
    [SerializeField] private Color enemyHoverColor = Color.red;
    [SerializeField] private bool enableEnemyHoverShader = true;
    [SerializeField] private string enemyHoverObjectName = "HoverShaderSprite";

    [Header("Friendly Heal Target Hover")]
    [SerializeField] private Color healHoverColor = Color.green;
    [SerializeField] private bool enableHealHoverShader = true;
    [SerializeField] private string healHoverObjectName = "HoverShaderSprite";

    [Header("Target Hover Pulse")]
    [SerializeField] private bool enableTargetPulse = true;
    [SerializeField, Min(0f)] private float targetPulseAmount = 0.06f;
    [SerializeField, Min(0.01f)] private float targetPulseSpeed = 5f;
    [SerializeField, Min(0.01f)] private float targetPulseSmoothSpeed = 12f;

    [Header("Animations")]
    [SerializeField, Min(0.01f)] private float explosionPulseDuration = 0.12f;

    private readonly Dictionary<Vector2Int, GridHighlightVisuals> tileVisuals = new(256);
    private readonly HashSet<Vector2Int> abilityCells = new();
    private readonly HashSet<Vector2Int> movementCells = new();
    private readonly HashSet<Vector2Int> explosionCells = new();
    private readonly HashSet<SpriteRenderer> activeTargetHoverRenderers = new();

    private readonly Dictionary<GameObject, AttackUnit> unitComponentCache = new();

    private readonly List<Vector2Int> tempCellList = new(64);
    private readonly List<SpriteRenderer> tempRendererList = new(16);

    private readonly Dictionary<Transform, Vector3> targetOriginalScales = new(16);
    private readonly HashSet<Transform> activeTargetPulseTransforms = new();
    private readonly List<Transform> tempTargetTransformList = new(16);

    private MaterialPropertyBlock hoverPropertyBlock;

    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");

    private Vector2Int placementPosition;
    private bool hasPlacementPosition;

    private GameObject currentRangeUser;
    private AttackUnit currentRangeUserUnit;

    private bool suppressMovementHighlight;
    private bool currentAbilityIsHeal;

    private void Awake()
    {
        hoverPropertyBlock = new MaterialPropertyBlock();
        FindReferences();
    }

    private void Start()
    {
        CacheTiles();
    }

    private void Update()
    {
        UpdateTargetPulse();
    }

    private void FindReferences()
    {
        if (gridManager == null)
        {
            if (!TryGetComponent(out gridManager))
                gridManager = FindFirstObjectByType<GridManager>();
        }

        if (brain == null)
        {
            if (!TryGetComponent(out brain))
                brain = FindFirstObjectByType<GridHighlightBrain>();
        }
    }

    private void CacheTiles()
    {
        if (gridManager == null)
            return;

        tileVisuals.Clear();

        int width = gridManager.GetWidth();
        int height = gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                CacheTile(new Vector2Int(x, y));
        }
    }

    private GridHighlightVisuals CacheTile(Vector2Int position)
    {
        if (gridManager == null)
            return null;

        if (tileVisuals.TryGetValue(position, out GridHighlightVisuals existing))
            return existing;

        GameObject tile = gridManager.GetFloorTile(position);

        if (tile == null)
            return null;

        if (!tile.TryGetComponent(out GridHighlightVisuals visuals))
            visuals = tile.AddComponent<GridHighlightVisuals>();

        visuals.Initialize(tile);
        tileVisuals[position] = visuals;

        return visuals;
    }

    public void RebuildTileCache()
    {
        unitComponentCache.Clear();
        CacheTiles();
    }

    public void UncacheUnit(GameObject unit)
    {
        if (unit != null)
            unitComponentCache.Remove(unit);
    }

    public void SetPlacementTile(Vector2Int position)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position))
        {
            ClearPlacementTile();
            return;
        }

        if (hasPlacementPosition && placementPosition == position)
            return;

        Vector2Int oldPosition = placementPosition;
        bool hadOldPosition = hasPlacementPosition;

        placementPosition = position;
        hasPlacementPosition = true;

        if (hadOldPosition)
            RefreshTile(oldPosition);

        RefreshTile(position);
    }

    public void ClearPlacementTile()
    {
        if (!hasPlacementPosition)
            return;

        Vector2Int oldPosition = placementPosition;
        hasPlacementPosition = false;

        RefreshTile(oldPosition);
    }

    public bool HasPlacementTile() => hasPlacementPosition;

    public Vector2Int GetPlacementTile() => placementPosition;

    public bool IsPlacementCell(Vector2Int position)
    {
        return hasPlacementPosition && placementPosition == position;
    }

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        if (brain == null)
            FindReferences();

        if (brain == null)
            return;

        brain.ShowMovementRange(centerPosition, range, user);
    }

    public void ShowMovementTiles(
        List<Vector2Int> cells,
        GameObject user = null)
    {
        ClearMovementRange();
        SetCurrentRangeUser(user);

        if (cells == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int position = cells[i];

            if (gridManager == null ||
                !gridManager.IsInsideGrid(position))
            {
                continue;
            }

            if (movementCells.Add(position))
                RefreshTile(position);
        }
    }

    public void ClearMovementRange()
    {
        SetCurrentRangeUser(null);

        if (movementCells.Count == 0)
            return;

        tempCellList.Clear();
        tempCellList.AddRange(movementCells);
        movementCells.Clear();

        for (int i = 0; i < tempCellList.Count; i++)
            RefreshTile(tempCellList[i]);

        tempCellList.Clear();
    }

    public bool IsMovementCell(Vector2Int position)
    {
        return movementCells.Contains(position);
    }

    public bool HasMovementRange()
    {
        return movementCells.Count > 0;
    }

    public void SetMovementHighlightSuppressed(bool suppressed)
    {
        if (suppressMovementHighlight == suppressed)
            return;

        suppressMovementHighlight = suppressed;
        RefreshCells(movementCells);
    }

    public bool IsMovementHighlightSuppressed()
    {
        return suppressMovementHighlight;
    }

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
    }

    public void ShowAbilityCell(Vector2Int position)
    {
        SetMovementHighlightSuppressed(true);

        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            return;
        }

        if (!abilityCells.Add(position))
            return;

        RefreshTile(position);
        RefreshTargetHoverForTile(position);
    }

    public void ClearAbilityRange()
    {
        ClearAllTargetHovers();
        ClearAllTargetPulse();

        SetCurrentRangeUser(null);
        currentAbilityIsHeal = false;
        SetMovementHighlightSuppressed(false);

        if (abilityCells.Count == 0)
            return;

        tempCellList.Clear();
        tempCellList.AddRange(abilityCells);
        abilityCells.Clear();

        for (int i = 0; i < tempCellList.Count; i++)
            RefreshTile(tempCellList[i]);

        tempCellList.Clear();
    }

    public bool IsAbilityCell(Vector2Int position)
    {
        return abilityCells.Contains(position);
    }

    public bool IsCurrentAbilityHeal()
    {
        return currentAbilityIsHeal;
    }

    private void RefreshCells(HashSet<Vector2Int> cells)
    {
        foreach (Vector2Int position in cells)
            RefreshTile(position);
    }

    private void RefreshTile(Vector2Int position)
    {
        GridHighlightVisuals visual = CacheTile(position);

        if (visual == null || explosionCells.Contains(position))
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
                movementRangeAlpha);

            return;
        }

        visual.Reset();
    }

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

    private bool IsValidAbilityTarget(GameObject target)
    {
        if (currentRangeUserUnit == null || target == null)
            return false;

        AttackUnit targetUnit = GetCachedAttackUnit(target);

        if (targetUnit == null || targetUnit.IsDead())
            return false;

        Team userTeam = currentRangeUserUnit.GetTeam();
        Team targetTeam = targetUnit.GetTeam();

        if (currentAbilityIsHeal)
        {
            if (userTeam == Team.Player || userTeam == Team.Ally)
                return targetTeam == Team.Player || targetTeam == Team.Ally;

            if (userTeam == Team.Enemy)
                return targetTeam == Team.Enemy;

            return false;
        }

        if (userTeam == Team.Player || userTeam == Team.Ally)
            return targetTeam == Team.Enemy;

        if (userTeam == Team.Enemy)
            return targetTeam == Team.Player || targetTeam == Team.Ally;

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
                    healHoverObjectName);
            }
        }
        else
        {
            if (enableEnemyHoverShader)
            {
                SetTargetHover(
                    unit,
                    enemyHoverColor,
                    enemyHoverObjectName);
            }
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

        Transform hoverShader = targetUnit.transform.Find(hoverObjectName);

        if (hoverShader == null)
        {
            SpriteRenderer childRenderer =
                targetUnit.GetComponentInChildren<SpriteRenderer>();

            if (childRenderer != null &&
                childRenderer.gameObject != targetUnit)
            {
                hoverShader = childRenderer.transform;
            }
        }

        if (hoverShader == null ||
            !hoverShader.TryGetComponent(out SpriteRenderer renderer))
        {
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
            Time.deltaTime * targetPulseSmoothSpeed;

        tempTargetTransformList.Clear();
        tempTargetTransformList.AddRange(activeTargetPulseTransforms);

        for (int i = 0; i < tempTargetTransformList.Count; i++)
        {
            Transform target = tempTargetTransformList[i];

            if (target == null)
            {
                activeTargetPulseTransforms.Remove(target);
                continue;
            }

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
                lerpFactor);
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
            Time.deltaTime * targetPulseSmoothSpeed;

        tempTargetTransformList.Clear();

        foreach (var pair in targetOriginalScales)
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
                lerpFactor);

            if ((target.localScale - originalScale).sqrMagnitude < 0.000001f)
                target.localScale = originalScale;
        }

        tempTargetTransformList.Clear();

        foreach (var pair in targetOriginalScales)
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

    public void FlashExplosionTile(Vector2Int position)
    {
        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            return;
        }

        GridHighlightVisuals visual = CacheTile(position);

        if (visual == null)
            return;

        explosionCells.Add(position);
        visual.PlayExplosion();

        StartCoroutine(RemoveExplosionCellAfterDelay(position));
    }

    private IEnumerator RemoveExplosionCellAfterDelay(Vector2Int position)
    {
        yield return new WaitForSeconds(explosionPulseDuration * 2f);

        explosionCells.Remove(position);
        RefreshTile(position);
    }

    public void ClearAllHighlights()
    {
        ClearPlacementTile();
        ClearMovementRange();
        ClearAbilityRange();

        ClearAllTargetHovers();
        ClearAllTargetPulse();

        explosionCells.Clear();

        suppressMovementHighlight = false;
        currentAbilityIsHeal = false;

        foreach (var pair in tileVisuals)
        {
            if (pair.Value != null)
                pair.Value.Reset();
        }
    }

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
}