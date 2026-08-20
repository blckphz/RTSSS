using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField]
    private bool enableDebugLogs = true;


    [Header("Grid Reference")]
    [SerializeField]
    private GridManager gridManager;


    [Header("Brain Reference")]
    [SerializeField]
    private GridHighlightBrain brain;


    [Header("Enemy Hover")]
    [SerializeField]
    private Color enemyHoverColor = Color.red;

    [SerializeField]
    private bool enableEnemyHoverShader = true;

    [SerializeField]
    private string enemyHoverObjectName =
        "HoverShaderSprite";


    [Header("Animations")]
    [SerializeField, Min(0.01f)]
    private float explosionPulseDuration = 0.12f;


    // ============================================================
    // TILE VISUAL DATA
    // ============================================================

    private readonly Dictionary<Vector2Int, GridHighlightVisuals>
        tileVisuals =
        new Dictionary<Vector2Int, GridHighlightVisuals>();


    // ============================================================
    // ABILITY CELLS
    // ============================================================

    private readonly HashSet<Vector2Int>
        abilityCells =
        new HashSet<Vector2Int>();


    // ============================================================
    // MOVEMENT CELLS
    // ============================================================

    private readonly HashSet<Vector2Int>
        movementCells =
        new HashSet<Vector2Int>();


    // ============================================================
    // EXPLOSION CELLS
    // ============================================================

    private readonly HashSet<Vector2Int>
        explosionCells =
        new HashSet<Vector2Int>();


    // ============================================================
    // ENEMY HOVER
    // ============================================================

    private readonly HashSet<SpriteRenderer>
        activeEnemyHoverRenderers =
        new HashSet<SpriteRenderer>();


    // ============================================================
    // PLACEMENT
    // ============================================================

    private Vector2Int placementPosition;

    private bool hasPlacementPosition;


    // ============================================================
    // RANGE USER
    // ============================================================

    private GameObject currentRangeUser;


    // ============================================================
    // MOVEMENT VISIBILITY
    // ============================================================

    private bool suppressMovementHighlight;


    // ============================================================
    // SHADER
    // ============================================================

    private static readonly int OutlineColorID =
        Shader.PropertyToID("_OutlineColor");


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
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
        {
            gridManager =
                GetComponent<GridManager>();
        }


        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (brain == null)
        {
            brain =
                GetComponent<GridHighlightBrain>();
        }


        if (brain == null)
        {
            brain =
                FindFirstObjectByType<GridHighlightBrain>();
        }


        if (gridManager == null &&
            enableDebugLogs)
        {
            Debug.LogError(
                "[GridHighlightManager] GridManager reference is missing!"
            );
        }


        if (brain == null &&
            enableDebugLogs)
        {
            Debug.LogWarning(
                "[GridHighlightManager] GridHighlightBrain reference is missing."
            );
        }
    }


    // ============================================================
    // TILE CACHE
    // ============================================================

    private void CacheTiles()
    {
        if (gridManager == null)
        {
            return;
        }


        tileVisuals.Clear();


        int width =
            gridManager.GetWidth();


        int height =
            gridManager.GetHeight();


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CacheTile(
                    new Vector2Int(x, y)
                );
            }
        }


        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GridHighlightManager] Cached {tileVisuals.Count} tile visuals."
            );
        }
    }


    private void CacheTile(
        Vector2Int position)
    {
        if (gridManager == null ||
            tileVisuals.ContainsKey(position))
        {
            return;
        }


        GameObject tile =
            gridManager.GetFloorTile(position);


        if (tile == null)
        {
            return;
        }


        GridHighlightVisuals visuals =
            tile.GetComponent<GridHighlightVisuals>();


        if (visuals == null)
        {
            visuals =
                tile.AddComponent<GridHighlightVisuals>();
        }


        visuals.Initialize(
            tile
        );


        tileVisuals[position] =
            visuals;
    }


    public void RebuildTileCache()
    {
        CacheTiles();
    }


    // ============================================================
    // PLACEMENT
    // ============================================================

    public void SetPlacementTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }


        if (!gridManager.IsInsideGrid(position))
        {
            ClearPlacementTile();
            return;
        }


        if (hasPlacementPosition &&
            placementPosition == position)
        {
            return;
        }


        ClearPlacementTile();


        placementPosition =
            position;


        hasPlacementPosition =
            true;


        RefreshTile(
            position
        );
    }


    public void ClearPlacementTile()
    {
        if (!hasPlacementPosition)
        {
            return;
        }


        Vector2Int oldPosition =
            placementPosition;


        hasPlacementPosition =
            false;


        RefreshTile(
            oldPosition
        );
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
        return
            hasPlacementPosition &&
            placementPosition == position;
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
        {
            if (gridManager != null)
            {
                brain =
                    gridManager.GetComponent<GridHighlightBrain>();
            }
        }


        if (brain == null)
        {
            brain =
                FindFirstObjectByType<GridHighlightBrain>();
        }


        if (brain == null)
        {
            Debug.LogWarning(
                "[GridHighlightManager] GridHighlightBrain is missing."
            );


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
        ClearMovementRange();


        currentRangeUser =
            user;


        if (cells == null)
        {
            return;
        }


        foreach (Vector2Int position in cells)
        {
            if (gridManager == null ||
                !gridManager.IsInsideGrid(position))
            {
                continue;
            }


            movementCells.Add(
                position
            );
        }


        RefreshCells(
            movementCells
        );
    }


    public void ClearMovementRange()
    {
        if (movementCells.Count == 0)
        {
            currentRangeUser = null;
            return;
        }


        List<Vector2Int> cells =
            new List<Vector2Int>(
                movementCells
            );


        movementCells.Clear();


        foreach (Vector2Int position in cells)
        {
            RefreshTile(
                position
            );
        }


        currentRangeUser = null;
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


    public void SetMovementHighlightSuppressed(
        bool suppressed)
    {
        if (suppressMovementHighlight ==
            suppressed)
        {
            return;
        }


        suppressMovementHighlight =
            suppressed;


        RefreshCells(
            movementCells
        );
    }


    public bool IsMovementHighlightSuppressed()
    {
        return suppressMovementHighlight;
    }


    // ============================================================
    // ABILITY
    // ============================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ClearAbilityRange();


        SetMovementHighlightSuppressed(
            true
        );


        currentRangeUser =
            user;


        if (positions == null)
        {
            return;
        }


        foreach (Vector2Int position in positions)
        {
            if (gridManager == null ||
                !gridManager.IsInsideGrid(position))
            {
                continue;
            }


            abilityCells.Add(
                position
            );
        }


        RefreshCells(
            abilityCells
        );


        RefreshAllEnemyHovers();
    }


    public void ShowAbilityCell(
        Vector2Int position)
    {
        SetMovementHighlightSuppressed(
            true
        );


        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            return;
        }


        if (abilityCells.Add(position))
        {
            RefreshTile(
                position
            );


            RefreshEnemyHoverForTile(
                position
            );
        }
    }


    public void ClearAbilityRange()
    {
        ClearAllEnemyHovers();


        currentRangeUser =
            null;


        SetMovementHighlightSuppressed(
            false
        );


        if (abilityCells.Count == 0)
        {
            return;
        }


        List<Vector2Int> cells =
            new List<Vector2Int>(
                abilityCells
            );


        abilityCells.Clear();


        foreach (Vector2Int position in cells)
        {
            RefreshTile(
                position
            );
        }
    }


    public bool IsAbilityCell(
        Vector2Int position)
    {
        return abilityCells.Contains(
            position
        );
    }


    // ============================================================
    // TILE REFRESH
    // ============================================================

    private void RefreshCells(
        HashSet<Vector2Int> cells)
    {
        foreach (Vector2Int position in cells)
        {
            RefreshTile(
                position
            );
        }
    }


    private void RefreshTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }


        CacheTile(
            position
        );


        if (!tileVisuals.TryGetValue(
                position,
                out GridHighlightVisuals visual))
        {
            return;
        }


        if (visual == null)
        {
            return;
        }


        bool isPlacement =
            hasPlacementPosition &&
            placementPosition == position;


        bool isAbility =
            abilityCells.Contains(
                position
            );


        bool isMovement =
            movementCells.Contains(
                position
            );


        bool isExplosion =
            explosionCells.Contains(
                position
            );


        // ========================================================
        // EXPLOSION
        // ========================================================

        if (isExplosion)
        {
            return;
        }


        // ========================================================
        // PLACEMENT
        // ========================================================

        if (isPlacement)
        {
            visual.ShowPlacement();
            return;
        }


        // ========================================================
        // ABILITY
        // ========================================================

        if (isAbility)
        {
            GameObject unit =
                gridManager.GetUnitAt(
                    position
                );


            bool isEnemy =
                brain != null &&
                brain.IsEnemyUnit(
                    unit,
                    currentRangeUser
                );


            if (isEnemy)
            {
                visual.ShowEnemy();
            }
            else
            {
                visual.ShowAbility();
            }


            return;
        }


        // ========================================================
        // MOVEMENT
        // ========================================================

        if (isMovement &&
            !suppressMovementHighlight)
        {
            visual.ShowMovement();
            return;
        }


        // ========================================================
        // DEFAULT
        // ========================================================

        visual.Reset();
    }


    // ============================================================
    // ENEMY HOVER
    // ============================================================

    private void RefreshAllEnemyHovers()
    {
        ClearAllEnemyHovers();


        if (!enableEnemyHoverShader ||
            gridManager == null)
        {
            return;
        }


        foreach (Vector2Int position in abilityCells)
        {
            RefreshEnemyHoverForTile(
                position
            );
        }
    }


    private void RefreshEnemyHoverForTile(
        Vector2Int position)
    {
        if (!enableEnemyHoverShader ||
            gridManager == null ||
            brain == null ||
            !abilityCells.Contains(position))
        {
            return;
        }


        GameObject unit =
            gridManager.GetUnitAt(
                position
            );


        if (unit == null)
        {
            return;
        }


        if (brain.IsEnemyUnit(
                unit,
                currentRangeUser))
        {
            SetEnemyHover(
                unit,
                true
            );
        }
    }


    private void SetEnemyHover(
        GameObject targetUnit,
        bool enabled)
    {
        if (targetUnit == null)
        {
            return;
        }


        Transform hoverShader =
            targetUnit.transform.Find(
                enemyHoverObjectName
            );


        if (hoverShader == null)
        {
            SpriteRenderer sr =
                targetUnit.GetComponentInChildren<SpriteRenderer>();


            if (sr != null &&
                sr.gameObject != targetUnit)
            {
                hoverShader =
                    sr.transform;
            }
        }


        if (hoverShader == null)
        {
            return;
        }


        SpriteRenderer renderer =
            hoverShader.GetComponent<SpriteRenderer>();


        if (renderer == null)
        {
            return;
        }


        if (enabled)
        {
            MaterialPropertyBlock block =
                new MaterialPropertyBlock();


            renderer.GetPropertyBlock(
                block
            );


            block.SetColor(
                OutlineColorID,
                enemyHoverColor
            );


            renderer.SetPropertyBlock(
                block
            );


            renderer.enabled =
                true;


            activeEnemyHoverRenderers.Add(
                renderer
            );
        }
        else
        {
            renderer.enabled =
                false;


            renderer.SetPropertyBlock(
                null
            );


            activeEnemyHoverRenderers.Remove(
                renderer
            );
        }
    }


    private void ClearAllEnemyHovers()
    {
        foreach (
            SpriteRenderer renderer
            in activeEnemyHoverRenderers)
        {
            if (renderer == null)
            {
                continue;
            }


            renderer.enabled =
                false;


            renderer.SetPropertyBlock(
                null
            );
        }


        activeEnemyHoverRenderers.Clear();
    }


    // ============================================================
    // EXPLOSION
    // ============================================================

    public void FlashExplosionTile(
        Vector2Int position)
    {
        if (gridManager == null ||
            !gridManager.IsInsideGrid(position))
        {
            return;
        }


        CacheTile(
            position
        );


        if (!tileVisuals.TryGetValue(
                position,
                out GridHighlightVisuals visual))
        {
            return;
        }


        if (visual == null)
        {
            return;
        }


        explosionCells.Add(
            position
        );


        visual.PlayExplosion();


        StartCoroutine(
            RemoveExplosionCellAfterDelay(
                position
            )
        );
    }


    private IEnumerator
        RemoveExplosionCellAfterDelay(
            Vector2Int position)
    {
        yield return new WaitForSeconds(
            explosionPulseDuration * 2f
        );


        explosionCells.Remove(
            position
        );


        RefreshTile(
            position
        );
    }


    // ============================================================
    // CLEAR EVERYTHING
    // ============================================================

    public void ClearAllHighlights()
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightManager] Clearing all highlights."
            );
        }


        ClearPlacementTile();


        ClearMovementRange();


        ClearAbilityRange();


        ClearAllEnemyHovers();


        explosionCells.Clear();


        SetMovementHighlightSuppressed(
            false
        );


        foreach (
            KeyValuePair<
                Vector2Int,
                GridHighlightVisuals
            > pair
            in tileVisuals)
        {
            if (pair.Value == null)
            {
                continue;
            }


            pair.Value.Reset();
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
}