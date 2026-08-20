using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour, IGridHighlight
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    // ==================================================
    // PLACEMENT HIGHLIGHT
    // ==================================================

    [Header("Placement Highlight")]
    [SerializeField] private Color placementColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float placementAlpha = 0.5f;

    // ==================================================
    // ABILITY RANGE
    // ==================================================

    [Header("Ability Range")]
    [SerializeField] private Color abilityRangeColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float abilityRangeAlpha = 0.35f;

    // ==================================================
    // ENEMY IN RANGE
    // ==================================================

    [Header("Enemy In Range")]
    [SerializeField] private Color enemyRangeColor = Color.red;

    [SerializeField, Range(0f, 1f)]
    private float enemyRangeAlpha = 0.9f;

    // ==================================================
    // ENEMY HOVER SHADER
    // ==================================================

    [Header("Enemy Hover Shader")]
    [SerializeField]
    private string hoverShaderObjectName = "HoverShaderSprite";

    // ==================================================
    // ABILITY FLASH
    // ==================================================

    [Header("Ability Flash")]
    [SerializeField]
    private bool pulseAbility = true;

    [SerializeField, Min(0f)]
    private float pulseSpeed = 5f;

    [SerializeField, Range(0f, 1f)]
    private float pulseAmount = 0.2f;

    // ==================================================
    // TILE SCALE JUICE
    // ==================================================

    [Header("Tile Scale Ping-Pong")]
    [SerializeField]
    private bool animateTileScale = true;

    [SerializeField, Min(0f)]
    private float scaleAmount = 0.06f;

    [SerializeField, Min(0f)]
    private float scaleSpeed = 5f;

    // ==================================================
    // EXPLOSION TILE
    // ==================================================

    [Header("Explosion Tile")]
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

    // ==================================================
    // TILE VISUAL
    // ==================================================

    private class TileVisual
    {
        public SpriteRenderer[] renderers;
        public Color[] originalColors;
        public Vector3[] originalScales;
    }

    private readonly Dictionary<Vector2Int, TileVisual>
        tileVisuals =
            new Dictionary<Vector2Int, TileVisual>();

    // ==================================================
    // HIGHLIGHT STATES
    // ==================================================

    private readonly HashSet<Vector2Int>
        abilityCells =
            new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int>
        attackCells =
            new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int>
        explosionCells =
            new HashSet<Vector2Int>();

    private readonly HashSet<GameObject>
        hoveredEnemies =
            new HashSet<GameObject>();

    private Vector2Int placementPosition;

    private bool hasPlacementPosition;

    // ==================================================
    // UNITY
    // ==================================================

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
    }

    private void Start()
    {
        CacheTiles();

        ClearAllHoverShaderSprites();
    }

    private void Update()
    {
        if (abilityCells.Count > 0)
        {
            RefreshAbilityCells();
        }

        if (attackCells.Count > 0)
        {
            RefreshAttackCells();
        }

        RefreshEnemyHoverShaders();
    }

    // ==================================================
    // CACHE ALL TILES
    // ==================================================

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
    }

    // ==================================================
    // CACHE SINGLE TILE
    // ==================================================

    private void CacheTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }

        if (tileVisuals.ContainsKey(position))
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

    // ==================================================
    // REBUILD CACHE
    // ==================================================

    public void RebuildTileCache()
    {
        CacheTiles();
    }

    // ==================================================
    // PLACEMENT HIGHLIGHT
    // ==================================================

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

    // ==================================================

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

    // ==================================================

    public bool HasPlacementTile()
    {
        return hasPlacementPosition;
    }

    // ==================================================

    public Vector2Int GetPlacementTile()
    {
        return placementPosition;
    }

    // ==================================================
    // ABILITY RANGE
    // ==================================================

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        ClearAbilityRange();

        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(
                centerPosition))
        {
            return;
        }

        range =
            Mathf.Max(0, range);

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (position == centerPosition)
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

    // ==================================================
    // SHOW ABILITY RANGE FROM ABILITY
    // ==================================================

    public void ShowAbilityRange(
        AbilitySO ability,
        GameObject user)
    {
        ClearAbilityRange();

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

    // ==================================================
    // CUSTOM ABILITY TILES
    // ==================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions)
    {
        ClearAbilityRange();

        if (gridManager == null ||
            positions == null ||
            positions.Count == 0)
        {
            return;
        }

        foreach (Vector2Int position in positions)
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

    // ==================================================
    // CUSTOM ABILITY CELLS
    // LEGACY OFFSET VERSION
    // ==================================================

    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets)
    {
        ClearAbilityRange();

        if (gridManager == null)
        {
            return;
        }

        if (offsets == null ||
            offsets.Count == 0)
        {
            return;
        }

        foreach (Vector2Int offset in offsets)
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

    // ==================================================
    // SINGLE ABILITY CELL
    // ==================================================

    public void ShowAbilityCell(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(
                position))
        {
            return;
        }

        if (abilityCells.Add(position))
        {
            RefreshTile(position);
        }
    }

    // ==================================================
    // CLEAR ABILITY
    // ==================================================

    public void ClearAbilityRange()
    {
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

    // ==================================================
    // REFRESH ABILITY CELLS
    // ==================================================

    private void RefreshAbilityCells()
    {
        foreach (Vector2Int position
                 in abilityCells)
        {
            RefreshTile(position);
        }
    }

    // ==================================================
    // ATTACK HIGHLIGHT
    // ==================================================

    public void ShowAttackCell(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        if (attackCells.Add(position))
        {
            RefreshTile(position);
        }
    }

    // ==================================================

    public void ShowAttackCells(
        List<Vector2Int> positions)
    {
        ClearAttackCells();

        if (gridManager == null ||
            positions == null ||
            positions.Count == 0)
        {
            return;
        }

        foreach (Vector2Int position in positions)
        {
            if (!gridManager.IsInsideGrid(position))
            {
                continue;
            }

            attackCells.Add(position);
        }

        RefreshAttackCells();
    }

    // ==================================================

    public void ShowAttackRange(
        Vector2Int centerPosition,
        int range)
    {
        ClearAttackCells();

        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(
                centerPosition))
        {
            return;
        }

        range =
            Mathf.Max(0, range);

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (position == centerPosition)
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
                    attackCells.Add(position);
                }
            }
        }

        RefreshAttackCells();
    }

    // ==================================================

    public void ClearAttackCells()
    {
        if (attackCells.Count == 0)
        {
            return;
        }

        List<Vector2Int> cells =
            new List<Vector2Int>(
                attackCells
            );

        attackCells.Clear();

        foreach (Vector2Int position
                 in cells)
        {
            RefreshTile(position);
        }
    }

    // ==================================================

    private void RefreshAttackCells()
    {
        foreach (Vector2Int position
                 in attackCells)
        {
            RefreshTile(position);
        }
    }

    // ==================================================
    // ENEMY DETECTION
    // ==================================================

    private bool IsEnemyOnTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return false;
        }

        GameObject unit =
            gridManager.GetUnitAt(position);

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

        return
            attackUnit.GetTeam() ==
            Team.Enemy;
    }

    // ==================================================
    // ENEMY HOVER SHADER
    // ==================================================

    private void RefreshEnemyHoverShaders()
    {
        HashSet<GameObject> enemiesInRange =
            new HashSet<GameObject>();

        // ----------------------------------------------
        // ABILITY CELLS
        // ----------------------------------------------

        foreach (Vector2Int position in abilityCells)
        {
            AddEnemyFromTile(
                position,
                enemiesInRange
            );
        }

        // ----------------------------------------------
        // ATTACK CELLS
        // ----------------------------------------------

        foreach (Vector2Int position in attackCells)
        {
            AddEnemyFromTile(
                position,
                enemiesInRange
            );
        }

        // ----------------------------------------------
        // ENABLE NEW ENEMIES
        // ----------------------------------------------

        foreach (GameObject enemy in enemiesInRange)
        {
            if (enemy == null)
            {
                continue;
            }

            SetHoverShader(
                enemy,
                true
            );
        }

        // ----------------------------------------------
        // DISABLE ENEMIES THAT LEFT RANGE
        // ----------------------------------------------

        List<GameObject> previousEnemies =
            new List<GameObject>(
                hoveredEnemies
            );

        foreach (GameObject enemy in previousEnemies)
        {
            if (enemy == null)
            {
                hoveredEnemies.Remove(enemy);
                continue;
            }

            if (!enemiesInRange.Contains(enemy))
            {
                SetHoverShader(
                    enemy,
                    false
                );

                hoveredEnemies.Remove(enemy);
            }
        }

        // ----------------------------------------------
        // STORE CURRENT ENEMIES
        // ----------------------------------------------

        foreach (GameObject enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                hoveredEnemies.Add(enemy);
            }
        }
    }

    // ==================================================

    private void AddEnemyFromTile(
        Vector2Int position,
        HashSet<GameObject> enemies)
    {
        if (gridManager == null)
        {
            return;
        }

        GameObject unit =
            gridManager.GetUnitAt(position);

        if (unit == null)
        {
            return;
        }

        AttackUnit attackUnit =
            unit.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            return;
        }

        if (attackUnit.GetTeam() != Team.Enemy)
        {
            return;
        }

        enemies.Add(unit);
    }

    // ==================================================

    private void SetHoverShader(
        GameObject unit,
        bool enabled)
    {
        if (unit == null)
        {
            return;
        }

        Transform hoverTransform =
            FindChildRecursive(
                unit.transform,
                hoverShaderObjectName
            );

        if (hoverTransform == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer =
            hoverTransform.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled =
            enabled;
    }

    // ==================================================

    private Transform FindChildRecursive(
        Transform parent,
        string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform result =
                FindChildRecursive(
                    parent.GetChild(i),
                    childName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // ==================================================

    private void ClearAllHoverShaderSprites()
    {
        hoveredEnemies.Clear();

        if (gridManager == null)
        {
            return;
        }

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject unit =
                    gridManager.GetUnitAt(
                        new Vector2Int(x, y)
                    );

                if (unit == null)
                {
                    continue;
                }

                SetHoverShader(
                    unit,
                    false
                );
            }
        }
    }

    // ==================================================
    // REFRESH TILE
    // ==================================================

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

        bool isAbility =
            abilityCells.Contains(position);

        bool isAttack =
            attackCells.Contains(position);

        bool isExplosion =
            explosionCells.Contains(position);

        bool isEnemy =
            IsEnemyOnTile(position);

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

            // ==================================================
            // EXPLOSION
            // ==================================================

            if (isExplosion)
            {
                finalColor =
                    explosionColor;

                finalScale =
                    originalScale *
                    (1f + explosionPulseAmount);
            }

            // ==================================================
            // ENEMY
            // ==================================================

            else if ((isAbility || isAttack) &&
                     isEnemy)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        enemyRangeColor,
                        enemyRangeAlpha
                    );

                if (animateTileScale)
                {
                    finalScale =
                        originalScale *
                        currentScale;
                }
            }

            // ==================================================
            // PLACEMENT
            // ==================================================

            else if (isPlacement)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        placementColor,
                        placementAlpha
                    );
            }

            // ==================================================
            // ABILITY
            // ==================================================

            else if (isAbility)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        abilityRangeColor,
                        currentAbilityAlpha
                    );

                if (animateTileScale)
                {
                    finalScale =
                        originalScale *
                        currentScale;
                }
            }

            // ==================================================
            // ATTACK
            // ==================================================

            else if (isAttack)
            {
                finalColor =
                    Color.Lerp(
                        original,
                        abilityRangeColor,
                        currentAbilityAlpha
                    );

                if (animateTileScale)
                {
                    finalScale =
                        originalScale *
                        currentScale;
                }
            }

            // ==================================================
            // NORMAL
            // ==================================================

            else
            {
                finalColor =
                    original;

                finalScale =
                    originalScale;
            }

            renderer.color =
                finalColor;

            renderer.transform.localScale =
                finalScale;
        }
    }

    // ==================================================
    // EXPLOSION TILE FLASH
    // ==================================================

    public void FlashExplosionTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        CacheTile(position);

        StartCoroutine(
            ExplosionTileRoutine(position)
        );
    }

    // ==================================================

    private IEnumerator ExplosionTileRoutine(
        Vector2Int position)
    {
        if (!tileVisuals.TryGetValue(
                position,
                out TileVisual visual))
        {
            yield break;
        }

        explosionCells.Add(position);

        float elapsed = 0f;

        // ==================================================
        // SCALE UP
        // ==================================================

        while (elapsed < explosionPulseDuration)
        {
            if (!explosionCells.Contains(position))
            {
                yield break;
            }

            elapsed += Time.deltaTime;

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

        // ==================================================
        // SCALE DOWN
        // ==================================================

        elapsed = 0f;

        while (elapsed < explosionPulseDuration)
        {
            if (!explosionCells.Contains(position))
            {
                yield break;
            }

            elapsed += Time.deltaTime;

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

        // ==================================================
        // RESTORE
        // ==================================================

        explosionCells.Remove(position);

        RefreshTile(position);
    }

    // ==================================================
    // ABILITY FLASH
    // ==================================================

    private float GetCurrentAbilityAlpha()
    {
        if (!pulseAbility)
        {
            return abilityRangeAlpha;
        }

        float pulse =
            (Mathf.Sin(
                Time.time * pulseSpeed
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

    // ==================================================
    // TILE SCALE
    // ==================================================

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
                Time.time * scaleSpeed
            ) + 1f) * 0.5f;

        return Mathf.Lerp(
            1f - scaleAmount,
            1f + scaleAmount,
            pingPong
        );
    }

    // ==================================================
    // STATE
    // ==================================================

    public bool IsAbilityCell(
        Vector2Int position)
    {
        return abilityCells.Contains(
            position
        );
    }

    // ==================================================

    public bool IsPlacementCell(
        Vector2Int position)
    {
        return hasPlacementPosition &&
               placementPosition == position;
    }

    // ==================================================
    // CLEAR EVERYTHING
    // ==================================================

    public void ClearAllHighlights()
    {
        ClearPlacementTile();
        ClearAbilityRange();
        ClearAttackCells();

        ClearAllHoverShaderSprites();
    }

    // ==================================================
    // GET GRID MANAGER
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}