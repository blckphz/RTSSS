using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Grid Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Brain Reference")]
    [SerializeField] private GridHighlightBrain brain;

    [Header("Enemy Hover")]
    [SerializeField] private Color enemyHoverColor = Color.red;
    [SerializeField] private bool enableEnemyHoverShader = true;
    [SerializeField] private string enemyHoverObjectName = "HoverShaderSprite";

    [Header("Animations")]
    [SerializeField, Min(0.01f)] private float explosionPulseDuration = 0.12f;

    // Data structures optimized for lookup & GC
    private readonly Dictionary<Vector2Int, GridHighlightVisuals> tileVisuals = new();
    private readonly HashSet<Vector2Int> abilityCells = new();
    private readonly HashSet<Vector2Int> movementCells = new();
    private readonly HashSet<Vector2Int> explosionCells = new();
    private readonly HashSet<SpriteRenderer> activeEnemyHoverRenderers = new();

    // Reusable buffers to eliminate GC allocations in loops
    private readonly List<Vector2Int> tempCellList = new List<Vector2Int>(64);
    private readonly List<SpriteRenderer> tempRendererList = new List<SpriteRenderer>(16);

    // Reusable Property Block for shader modification
    private MaterialPropertyBlock hoverPropertyBlock;

    // Placement
    private Vector2Int placementPosition;
    private bool hasPlacementPosition;

    // Range User
    private GameObject currentRangeUser;

    // Movement Visibility
    private bool suppressMovementHighlight;

    // Shader IDs
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        hoverPropertyBlock = new MaterialPropertyBlock();
        FindReferences();
    }

    private void Start()
    {
        CacheTiles();
    }

    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = GetComponent<GridManager>() ?? FindFirstObjectByType<GridManager>();

        if (brain == null)
            brain = GetComponent<GridHighlightBrain>() ?? FindFirstObjectByType<GridHighlightBrain>();

        if (enableDebugLogs)
        {
            if (gridManager == null)
                Debug.LogError("[GridHighlightManager] GridManager reference is missing!");
            if (brain == null)
                Debug.LogWarning("[GridHighlightManager] GridHighlightBrain reference is missing.");
        }
    }

    // ============================================================
    // TILE CACHE
    // ============================================================

    private void CacheTiles()
    {
        if (gridManager == null) return;

        tileVisuals.Clear();

        int width = gridManager.GetWidth();
        int height = gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CacheTile(new Vector2Int(x, y));
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[GridHighlightManager] Cached {tileVisuals.Count} tile visuals.");
        }
    }

    private GridHighlightVisuals CacheTile(Vector2Int position)
    {
        if (gridManager == null) return null;

        if (tileVisuals.TryGetValue(position, out GridHighlightVisuals existingVisual))
        {
            return existingVisual;
        }

        GameObject tile = gridManager.GetFloorTile(position);
        if (tile == null) return null;

        if (!tile.TryGetComponent(out GridHighlightVisuals visuals))
        {
            visuals = tile.AddComponent<GridHighlightVisuals>();
        }

        visuals.Initialize(tile);
        tileVisuals[position] = visuals;
        return visuals;
    }

    public void RebuildTileCache()
    {
        CacheTiles();
    }

    // ============================================================
    // PLACEMENT
    // ============================================================

    public void SetPlacementTile(Vector2Int position)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position))
        {
            ClearPlacementTile();
            return;
        }

        if (hasPlacementPosition && placementPosition == position)
            return;

        Vector2Int oldPos = placementPosition;
        bool hadOldPos = hasPlacementPosition;

        placementPosition = position;
        hasPlacementPosition = true;

        if (hadOldPos) RefreshTile(oldPos);
        RefreshTile(position);
    }

    public void ClearPlacementTile()
    {
        if (!hasPlacementPosition) return;

        Vector2Int oldPosition = placementPosition;
        hasPlacementPosition = false;

        RefreshTile(oldPosition);
    }

    public bool HasPlacementTile() => hasPlacementPosition;
    public Vector2Int GetPlacementTile() => placementPosition;
    public bool IsPlacementCell(Vector2Int position) => hasPlacementPosition && placementPosition == position;

    // ============================================================
    // MOVEMENT
    // ============================================================

    public void ShowMovementRange(Vector2Int centerPosition, int range, GameObject user = null)
    {
        if (brain == null)
            brain = (gridManager != null) ? gridManager.GetComponent<GridHighlightBrain>() : FindFirstObjectByType<GridHighlightBrain>();

        if (brain == null)
        {
            Debug.LogWarning("[GridHighlightManager] GridHighlightBrain is missing.");
            return;
        }

        brain.ShowMovementRange(centerPosition, range, user);
    }

    public void ShowMovementTiles(List<Vector2Int> cells, GameObject user = null)
    {
        ClearMovementRange();
        currentRangeUser = user;

        if (cells == null) return;

        int count = cells.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = cells[i];
            if (gridManager != null && gridManager.IsInsideGrid(pos))
            {
                movementCells.Add(pos);
            }
        }

        RefreshCells(movementCells);
    }

    public void ClearMovementRange()
    {
        currentRangeUser = null;
        if (movementCells.Count == 0) return;

        tempCellList.Clear();
        tempCellList.AddRange(movementCells);
        movementCells.Clear();

        int count = tempCellList.Count;
        for (int i = 0; i < count; i++)
        {
            RefreshTile(tempCellList[i]);
        }
    }

    public bool IsMovementCell(Vector2Int position) => movementCells.Contains(position);
    public bool HasMovementRange() => movementCells.Count > 0;

    public void SetMovementHighlightSuppressed(bool suppressed)
    {
        if (suppressMovementHighlight == suppressed) return;

        suppressMovementHighlight = suppressed;
        RefreshCells(movementCells);
    }

    public bool IsMovementHighlightSuppressed() => suppressMovementHighlight;

    // ============================================================
    // ABILITY
    // ============================================================

    public void ShowAbilityTiles(List<Vector2Int> positions, GameObject user = null)
    {
        ClearAbilityRange();
        SetMovementHighlightSuppressed(true);
        currentRangeUser = user;

        if (positions == null) return;

        int count = positions.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = positions[i];
            if (gridManager != null && gridManager.IsInsideGrid(pos))
            {
                abilityCells.Add(pos);
            }
        }

        RefreshCells(abilityCells);
        RefreshAllEnemyHovers();
    }

    public void ShowAbilityCell(Vector2Int position)
    {
        SetMovementHighlightSuppressed(true);

        if (gridManager == null || !gridManager.IsInsideGrid(position)) return;

        if (abilityCells.Add(position))
        {
            RefreshTile(position);
            RefreshEnemyHoverForTile(position);
        }
    }

    public void ClearAbilityRange()
    {
        ClearAllEnemyHovers();
        currentRangeUser = null;
        SetMovementHighlightSuppressed(false);

        if (abilityCells.Count == 0) return;

        tempCellList.Clear();
        tempCellList.AddRange(abilityCells);
        abilityCells.Clear();

        int count = tempCellList.Count;
        for (int i = 0; i < count; i++)
        {
            RefreshTile(tempCellList[i]);
        }
    }

    public bool IsAbilityCell(Vector2Int position) => abilityCells.Contains(position);

    // ============================================================
    // TILE REFRESH
    // ============================================================

    private void RefreshCells(HashSet<Vector2Int> cells)
    {
        foreach (Vector2Int position in cells)
        {
            RefreshTile(position);
        }
    }

    private void RefreshTile(Vector2Int position)
    {
        GridHighlightVisuals visual = CacheTile(position);
        if (visual == null) return;

        // Priority 1: Explosion
        if (explosionCells.Contains(position)) return;

        // Priority 2: Placement
        if (hasPlacementPosition && placementPosition == position)
        {
            visual.ShowPlacement();
            return;
        }

        // Priority 3: Ability
        if (abilityCells.Contains(position))
        {
            GameObject unit = gridManager.GetUnitAt(position);
            bool isEnemy = unit != null && brain != null && brain.IsEnemyUnit(unit, currentRangeUser);

            if (isEnemy) visual.ShowEnemy();
            else visual.ShowAbility();
            return;
        }

        // Priority 4: Movement
        if (!suppressMovementHighlight && movementCells.Contains(position))
        {
            visual.ShowMovement();
            return;
        }

        // Priority 5: Default
        visual.Reset();
    }

    // ============================================================
    // ENEMY HOVER
    // ============================================================

    private void RefreshAllEnemyHovers()
    {
        ClearAllEnemyHovers();
        if (!enableEnemyHoverShader || gridManager == null) return;

        foreach (Vector2Int position in abilityCells)
        {
            RefreshEnemyHoverForTile(position);
        }
    }

    private void RefreshEnemyHoverForTile(Vector2Int position)
    {
        if (!enableEnemyHoverShader || gridManager == null || brain == null || !abilityCells.Contains(position))
            return;

        GameObject unit = gridManager.GetUnitAt(position);
        if (unit != null && brain.IsEnemyUnit(unit, currentRangeUser))
        {
            SetEnemyHover(unit, true);
        }
    }

    private void SetEnemyHover(GameObject targetUnit, bool enabled)
    {
        if (targetUnit == null) return;

        Transform hoverShader = targetUnit.transform.Find(enemyHoverObjectName);

        if (hoverShader == null)
        {
            SpriteRenderer sr = targetUnit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.gameObject != targetUnit)
            {
                hoverShader = sr.transform;
            }
        }

        if (hoverShader == null || !hoverShader.TryGetComponent(out SpriteRenderer renderer)) return;

        if (enabled)
        {
            renderer.GetPropertyBlock(hoverPropertyBlock);
            hoverPropertyBlock.SetColor(OutlineColorID, enemyHoverColor);
            renderer.SetPropertyBlock(hoverPropertyBlock);

            renderer.enabled = true;
            activeEnemyHoverRenderers.Add(renderer);
        }
        else
        {
            renderer.enabled = false;
            renderer.SetPropertyBlock(null);
            activeEnemyHoverRenderers.Remove(renderer);
        }
    }

    private void ClearAllEnemyHovers()
    {
        if (activeEnemyHoverRenderers.Count == 0) return;

        tempRendererList.Clear();
        tempRendererList.AddRange(activeEnemyHoverRenderers);
        activeEnemyHoverRenderers.Clear();

        int count = tempRendererList.Count;
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer renderer = tempRendererList[i];
            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.SetPropertyBlock(null);
            }
        }
    }

    // ============================================================
    // EXPLOSION
    // ============================================================

    public void FlashExplosionTile(Vector2Int position)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position)) return;

        GridHighlightVisuals visual = CacheTile(position);
        if (visual == null) return;

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

    // ============================================================
    // CLEAR EVERYTHING
    // ============================================================

    public void ClearAllHighlights()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[GridHighlightManager] Clearing all highlights.");
        }

        ClearPlacementTile();
        ClearMovementRange();
        ClearAbilityRange();
        ClearAllEnemyHovers();

        explosionCells.Clear();
        suppressMovementHighlight = false;

        foreach (var pair in tileVisuals)
        {
            if (pair.Value != null)
            {
                pair.Value.Reset();
            }
        }
    }

    // ============================================================
    // GETTERS
    // ============================================================

    public GridManager GetGridManager() => gridManager;
    public GridHighlightBrain GetBrain() => brain;
}