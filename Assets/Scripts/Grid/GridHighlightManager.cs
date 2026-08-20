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


    [Header("Placement Settings")]
    [SerializeField]
    private Color placementColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float placementAlpha = 0.5f;


    [Header("Movement Range Settings")]
    [SerializeField]
    private Color movementRangeColor = Color.cyan;

    [SerializeField, Range(0f, 1f)]
    private float movementRangeAlpha = 0.35f;


    [Header("Ability Range Settings")]
    [SerializeField]
    private Color abilityRangeColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float abilityRangeAlpha = 0.35f;


    [Header("Enemy Tile Settings")]
    [SerializeField]
    private Color enemyTileColor = Color.red;

    [SerializeField, Range(0f, 1f)]
    private float enemyTileAlpha = 0.85f;

    [SerializeField]
    private Color enemyHoverColor = Color.red;

    [SerializeField]
    private bool enableEnemyHoverShader = true;

    [SerializeField]
    private string enemyHoverObjectName = "HoverShaderSprite";


    [Header("Animations")]
    [SerializeField]
    private bool pulseAbility = true;

    [SerializeField, Min(0f)]
    private float pulseSpeed = 5f;

    [SerializeField, Range(0f, 1f)]
    private float pulseAmount = 0.2f;


    [SerializeField]
    private bool animateTileScale = true;

    [SerializeField, Min(0f)]
    private float scaleAmount = 0.06f;

    [SerializeField, Min(0f)]
    private float scaleSpeed = 5f;


    [Header("Explosion Settings")]
    [SerializeField]
    private Color explosionColor = Color.red;

    [SerializeField, Min(0f)]
    private float explosionPulseAmount = 0.12f;

    [SerializeField, Min(0.01f)]
    private float explosionPulseDuration = 0.12f;

    [SerializeField]
    private AnimationCurve explosionPulseCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =============================================================
    // TILE VISUAL DATA
    // =============================================================

    private class TileVisual
    {
        public SpriteRenderer[] renderers;
        public Color[] originalColors;
        public Vector3[] originalScales;
    }


    private readonly Dictionary<Vector2Int, TileVisual>
        tileVisuals =
        new Dictionary<Vector2Int, TileVisual>();


    // =============================================================
    // ABILITY CELLS
    // =============================================================

    private readonly HashSet<Vector2Int>
        abilityCells =
        new HashSet<Vector2Int>();


    // =============================================================
    // MOVEMENT / ATTACK CELLS
    // =============================================================
    //
    // Movement and attack share the same tile set.
    //
    // =============================================================

    private readonly HashSet<Vector2Int>
        movementCells =
        new HashSet<Vector2Int>();


    // =============================================================
    // EXPLOSION CELLS
    // =============================================================

    private readonly HashSet<Vector2Int>
        explosionCells =
        new HashSet<Vector2Int>();


    // =============================================================
    // ENEMY HOVER
    // =============================================================

    private readonly HashSet<SpriteRenderer>
        activeEnemyHoverRenderers =
        new HashSet<SpriteRenderer>();


    // =============================================================
    // PLACEMENT
    // =============================================================

    private Vector2Int placementPosition;

    private bool hasPlacementPosition;


    // =============================================================
    // RANGE USER
    // =============================================================

    private GameObject currentRangeUser;


    // =============================================================
    // MOVEMENT HIGHLIGHT VISIBILITY
    // =============================================================
    //
    // Movement/attack use the same tile set.
    //
    // When an ability/attack is being hovered, the movement
    // visualization is temporarily hidden.
    //
    // The actual movementCells are NOT cleared.
    //
    // =============================================================

    private bool suppressMovementHighlight;


    private static readonly int OutlineColorID =
        Shader.PropertyToID("_OutlineColor");


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
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

        if (gridManager == null &&
            enableDebugLogs)
        {
            Debug.LogError(
                "[GridHighlightManager] " +
                "GridManager reference is missing!"
            );
        }
    }


    private void Start()
    {
        CacheTiles();
    }


    private void Update()
    {
        if (abilityCells.Count > 0)
        {
            RefreshAbilityCells();
        }

        if (movementCells.Count > 0)
        {
            RefreshMovementCells();
        }
    }


    // =============================================================
    // TILE CACHE
    // =============================================================

    private void CacheTiles()
    {
        if (gridManager == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning(
                    "[GridHighlightManager] " +
                    "Cannot cache tiles: GridManager is null."
                );
            }

            return;
        }


        tileVisuals.Clear();


        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();


        for (int x = 0;
             x < width;
             x++)
        {
            for (int y = 0;
                 y < height;
                 y++)
            {
                CacheTile(
                    new Vector2Int(x, y)
                );
            }
        }


        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GridHighlightManager] " +
                $"Cached {tileVisuals.Count} tile visuals."
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


        SpriteRenderer[] renderers =
            tile.GetComponentsInChildren<SpriteRenderer>(
                true
            );


        if (renderers == null ||
            renderers.Length == 0)
        {
            return;
        }


        Color[] originalColors =
            new Color[renderers.Length];

        Vector3[] originalScales =
            new Vector3[renderers.Length];


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }


            originalColors[i] =
                renderers[i].color;

            originalScales[i] =
                renderers[i].transform.localScale;
        }


        tileVisuals[position] =
            new TileVisual
            {
                renderers = renderers,
                originalColors = originalColors,
                originalScales = originalScales
            };
    }


    public void RebuildTileCache()
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightManager] " +
                "Rebuilding Tile Cache..."
            );
        }

        CacheTiles();
    }


    // =============================================================
    // PLACEMENT
    // =============================================================

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


        hasPlacementPosition =
            false;


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


    // =============================================================
    // MOVEMENT / ATTACK RANGE
    // =============================================================

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        ClearMovementRange();


        if (gridManager == null)
        {
            return;
        }


        if (!gridManager.IsInsideGrid(
                centerPosition))
        {
            return;
        }


        currentRangeUser =
            user;


        range =
            Mathf.Max(
                0,
                range
            );


        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();


        for (int x = 0;
             x < width;
             x++)
        {
            for (int y = 0;
                 y < height;
                 y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


                // Do not highlight the unit's
                // current cell.
                if (position ==
                    centerPosition)
                {
                    continue;
                }


                int distance =
                    gridManager.GetDistance(
                        centerPosition,
                        position
                    );


                if (distance > range)
                {
                    continue;
                }


                // =================================================
                // OCCUPIED CELLS ARE NOT MOVEMENT DESTINATIONS
                // =================================================

                if (gridManager.IsCellOccupied(
                        position))
                {
                    continue;
                }


                movementCells.Add(
                    position
                );
            }
        }


        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GridHighlightManager] " +
                $"Movement range shown around {centerPosition}. " +
                $"Range: {range}. " +
                $"Available cells: {movementCells.Count}."
            );
        }


        RefreshMovementCells();
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


        foreach (Vector2Int position
                 in cells)
        {
            RefreshTile(position);
        }


        currentRangeUser = null;


        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightManager] " +
                "Movement range cleared."
            );
        }
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


    // =============================================================
    // TEMPORARILY HIDE MOVEMENT / ATTACK HIGHLIGHT
    // =============================================================

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


        RefreshMovementCells();
    }


    public bool IsMovementHighlightSuppressed()
    {
        return suppressMovementHighlight;
    }


    private void RefreshMovementCells()
    {
        foreach (Vector2Int position
                 in movementCells)
        {
            RefreshTile(position);
        }
    }


    // =============================================================
    // ABILITY RANGE
    // =============================================================

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        ClearAbilityRange();

        SetMovementHighlightSuppressed(true);

        currentRangeUser = null;


        if (gridManager == null ||
            !gridManager.IsInsideGrid(
                centerPosition))
        {
            return;
        }


        range =
            Mathf.Max(
                0,
                range
            );


        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();


        for (int x = 0;
             x < width;
             x++)
        {
            for (int y = 0;
                 y < height;
                 y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


                if (position ==
                    centerPosition)
                {
                    continue;
                }


                int distance =
                    Mathf.Max(
                        Mathf.Abs(
                            position.x -
                            centerPosition.x
                        ),
                        Mathf.Abs(
                            position.y -
                            centerPosition.y
                        )
                    );


                if (distance <= range)
                {
                    abilityCells.Add(
                        position
                    );
                }
            }
        }


        RefreshAbilityCells();
    }


    public void ShowAbilityRange(
        AbilitySO ability,
        GameObject user)
    {
        ClearAbilityRange();

        SetMovementHighlightSuppressed(true);

        currentRangeUser =
            user;


        if (ability == null ||
            user == null ||
            gridManager == null)
        {
            return;
        }


        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                user
            );


        if (rangeTiles == null ||
            rangeTiles.Count == 0)
        {
            return;
        }


        foreach (Vector2Int position
                 in rangeTiles)
        {
            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            abilityCells.Add(
                position
            );
        }


        RefreshAbilityCells();
    }


    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        ClearAbilityRange();

        SetMovementHighlightSuppressed(true);

        currentRangeUser =
            user;


        if (gridManager == null ||
            positions == null ||
            positions.Count == 0)
        {
            return;
        }


        foreach (Vector2Int position
                 in positions)
        {
            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            abilityCells.Add(
                position
            );
        }


        RefreshAbilityCells();
    }


    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets,
        GameObject user = null)
    {
        ClearAbilityRange();

        SetMovementHighlightSuppressed(true);

        currentRangeUser =
            user;


        if (gridManager == null ||
            offsets == null ||
            offsets.Count == 0)
        {
            return;
        }


        foreach (Vector2Int offset
                 in offsets)
        {
            Vector2Int position =
                origin + offset;


            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            abilityCells.Add(
                position
            );
        }


        RefreshAbilityCells();
    }


    public void ShowAbilityCell(
        Vector2Int position)
    {
        SetMovementHighlightSuppressed(true);


        if (gridManager == null ||
            !gridManager.IsInsideGrid(
                position))
        {
            return;
        }


        if (abilityCells.Add(position))
        {
            RefreshTile(position);

            RefreshEnemyHoverForTile(
                position
            );
        }
    }


    public void ClearAbilityRange()
    {
        ClearAllEnemyHovers();

        currentRangeUser = null;

        // Ability/attack hover is ending,
        // so restore the shared movement/attack tiles.
        SetMovementHighlightSuppressed(false);


        if (abilityCells.Count == 0)
        {
            return;
        }


        List<Vector2Int> cells =
            new List<Vector2Int>(
                abilityCells
            );


        abilityCells.Clear();


        foreach (Vector2Int position
                 in cells)
        {
            RefreshTile(position);
        }
    }


    private void RefreshAbilityCells()
    {
        foreach (Vector2Int position
                 in abilityCells)
        {
            RefreshTile(position);
        }


        RefreshAllEnemyHovers();
    }


    // =============================================================
    // TILE REFRESH
    // =============================================================

    private void RefreshTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }


        CacheTile(position);


        if (!tileVisuals.TryGetValue(
                position,
                out TileVisual visual))
        {
            return;
        }


        bool isPlacement =
            hasPlacementPosition &&
            placementPosition == position;


        GameObject unit =
            gridManager.GetUnitAt(position);


        bool isEnemy =
            IsEnemyUnit(unit);


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


        float currentAbilityAlpha =
            GetCurrentAbilityAlpha();


        float currentScale =
            GetCurrentScale();


        for (int i = 0;
             i < visual.renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                visual.renderers[i];


            if (renderer == null)
            {
                continue;
            }


            Color original =
                visual.originalColors[i];


            Vector3 originalScale =
                visual.originalScales[i];


            Color finalColor =
                original;


            Vector3 finalScale =
                originalScale;


            // =====================================================
            // EXPLOSION
            // =====================================================

            if (isExplosion)
            {
                finalColor =
                    explosionColor;

                finalScale =
                    originalScale *
                    (1f + explosionPulseAmount);
            }


            // =====================================================
            // PLACEMENT
            // =====================================================

            else if (isPlacement)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        placementColor,
                        placementAlpha
                    );
            }


            // =====================================================
            // ABILITY / ATTACK
            // =====================================================
            //
            // IMPORTANT:
            // Ability/attack takes priority over movement.
            //
            // Therefore, if a tile exists in both
            // abilityCells and movementCells, the ability
            // highlight is displayed.
            //
            // =====================================================

            else if (isAbility)
            {
                if (isEnemy)
                {
                    Color enemyColor =
                        enemyTileColor;

                    enemyColor.a =
                        enemyTileAlpha;

                    finalColor =
                        enemyColor;
                }
                else
                {
                    finalColor =
                        Color.Lerp(
                            original,
                            abilityRangeColor,
                            currentAbilityAlpha
                        );
                }


                if (animateTileScale)
                {
                    finalScale =
                        originalScale *
                        currentScale;
                }
            }


            // =====================================================
            // MOVEMENT / ATTACK
            // =====================================================
            //
            // Movement and attack share movementCells.
            //
            // When an ability/attack is being hovered,
            // suppressMovementHighlight is true and this
            // visualization is skipped.
            //
            // =====================================================

            else if (isMovement &&
                     !suppressMovementHighlight)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        movementRangeColor,
                        movementRangeAlpha
                    );


                if (animateTileScale)
                {
                    finalScale =
                        originalScale *
                        currentScale;
                }
            }


            renderer.color =
                finalColor;


            renderer.transform.localScale =
                finalScale;
        }
    }


    // =============================================================
    // ENEMY CHECK
    // =============================================================

    private bool IsEnemyUnit(
        GameObject unit)
    {
        if (unit == null)
        {
            return false;
        }


        AttackUnit attackUnit =
            unit.GetComponent<AttackUnit>();


        if (attackUnit != null &&
            !attackUnit.IsDead())
        {
            if (currentRangeUser != null)
            {
                AttackUnit sourceAttackUnit =
                    currentRangeUser.GetComponent<AttackUnit>();


                if (sourceAttackUnit != null)
                {
                    return
                        attackUnit.GetTeam() !=
                        sourceAttackUnit.GetTeam();
                }
            }


            return false;
        }


        HealthManager targetHealth =
            unit.GetComponent<HealthManager>();


        if (targetHealth != null &&
            !targetHealth.IsDead())
        {
            if (currentRangeUser != null)
            {
                HealthManager sourceHealth =
                    currentRangeUser.GetComponent<HealthManager>();


                if (sourceHealth != null)
                {
                    return
                        targetHealth.GetTeam() !=
                        sourceHealth.GetTeam();
                }
            }


            return false;
        }


        return false;
    }


    // =============================================================
    // ENEMY HOVER
    // =============================================================

    private void RefreshAllEnemyHovers()
    {
        ClearAllEnemyHovers();


        if (!enableEnemyHoverShader ||
            gridManager == null)
        {
            return;
        }


        foreach (Vector2Int position
                 in abilityCells)
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
            !abilityCells.Contains(position))
        {
            return;
        }


        GameObject unit =
            gridManager.GetUnitAt(position);


        if (unit == null)
        {
            return;
        }


        if (ShouldOutlineUnit(unit))
        {
            SetEnemyHover(
                unit,
                true
            );
        }
    }


    private bool ShouldOutlineUnit(
        GameObject unit)
    {
        if (unit == null)
        {
            return false;
        }


        return IsEnemyUnit(unit);
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


            renderer.GetPropertyBlock(block);


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


    // =============================================================
    // EXPLOSION
    // =============================================================

    public void FlashExplosionTile(
        Vector2Int position)
    {
        if (gridManager == null ||
            !gridManager.IsInsideGrid(
                position))
        {
            return;
        }


        CacheTile(position);


        StartCoroutine(
            ExplosionTileRoutine(position)
        );
    }


    private IEnumerator ExplosionTileRoutine(
        Vector2Int position)
    {
        if (!tileVisuals.TryGetValue(
                position,
                out TileVisual visual))
        {
            yield break;
        }


        explosionCells.Add(
            position
        );


        float elapsed =
            0f;


        while (elapsed <
               explosionPulseDuration)
        {
            if (!explosionCells.Contains(
                    position))
            {
                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    explosionPulseDuration
                );


            float curveValue =
                explosionPulseCurve.Evaluate(t);


            for (int i = 0;
                 i < visual.renderers.Length;
                 i++)
            {
                SpriteRenderer renderer =
                    visual.renderers[i];


                if (renderer == null)
                {
                    continue;
                }


                Vector3 originalScale =
                    visual.originalScales[i];


                renderer.color =
                    Color.Lerp(
                        visual.originalColors[i],
                        explosionColor,
                        curveValue
                    );


                renderer.transform.localScale =
                    Vector3.LerpUnclamped(
                        originalScale,
                        originalScale *
                        (1f + explosionPulseAmount),
                        curveValue
                    );
            }


            yield return null;
        }


        elapsed =
            0f;


        while (elapsed <
               explosionPulseDuration)
        {
            if (!explosionCells.Contains(
                    position))
            {
                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    explosionPulseDuration
                );


            float curveValue =
                explosionPulseCurve.Evaluate(t);


            for (int i = 0;
                 i < visual.renderers.Length;
                 i++)
            {
                SpriteRenderer renderer =
                    visual.renderers[i];


                if (renderer == null)
                {
                    continue;
                }


                Vector3 originalScale =
                    visual.originalScales[i];


                renderer.color =
                    Color.Lerp(
                        explosionColor,
                        visual.originalColors[i],
                        curveValue
                    );


                renderer.transform.localScale =
                    Vector3.LerpUnclamped(
                        originalScale *
                        (1f + explosionPulseAmount),
                        originalScale,
                        curveValue
                    );
            }


            yield return null;
        }


        explosionCells.Remove(
            position
        );


        RefreshTile(position);
    }


    // =============================================================
    // ANIMATION
    // =============================================================

    private float GetCurrentAbilityAlpha()
    {
        if (!pulseAbility)
        {
            return abilityRangeAlpha;
        }


        float pulse =
            (Mathf.Sin(
                Time.time *
                pulseSpeed
            ) + 1f) * 0.5f;


        float multiplier =
            Mathf.Lerp(
                1f - pulseAmount,
                1f + pulseAmount,
                pulse
            );


        return Mathf.Clamp01(
            abilityRangeAlpha *
            multiplier
        );
    }


    private float GetCurrentScale()
    {
        if (!animateTileScale ||
            scaleAmount <= 0f ||
            scaleSpeed <= 0f)
        {
            return 1f;
        }


        float pingPong =
            (Mathf.Sin(
                Time.time *
                scaleSpeed
            ) + 1f) * 0.5f;


        return Mathf.Lerp(
            1f - scaleAmount,
            1f + scaleAmount,
            pingPong
        );
    }


    // =============================================================
    // QUERIES
    // =============================================================

    public bool IsAbilityCell(
        Vector2Int position)
    {
        return abilityCells.Contains(
            position
        );
    }


    public bool IsPlacementCell(
        Vector2Int position)
    {
        return
            hasPlacementPosition &&
            placementPosition == position;
    }


    // =============================================================
    // CLEAR EVERYTHING
    // =============================================================

    public void ClearAllHighlights()
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightManager] " +
                "Clearing all highlights."
            );
        }


        ClearPlacementTile();

        ClearMovementRange();

        ClearAbilityRange();

        ClearAllEnemyHovers();

        // Make absolutely sure movement highlighting
        // is restored after everything is cleared.
        SetMovementHighlightSuppressed(false);
    }


    // =============================================================
    // GETTERS
    // =============================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}