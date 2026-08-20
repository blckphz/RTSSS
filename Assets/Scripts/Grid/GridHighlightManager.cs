using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
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
        explosionCells =
            new HashSet<Vector2Int>();

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
    }


    private void Update()
    {
        if (abilityCells.Count > 0)
        {
            RefreshAbilityCells();
        }
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
                Vector2Int position =
                    new Vector2Int(x, y);

                CacheTile(position);
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

                if (gridManager.GetDistance(
                        centerPosition,
                        position
                    ) <= range)
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
    // CUSTOM ABILITY CELLS
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

        foreach (Vector2Int position in cells)
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

        bool isExplosion =
            explosionCells.Contains(position);

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
            // EXPLOSION HAS HIGHEST PRIORITY
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
            // ABILITY RANGE
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
            // ORIGINAL
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
        // SCALE UP / RED
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
        // SCALE DOWN / RESTORE
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
        // FORCE RESTORE
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
    // TILE SCALE PING-PONG
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
    }


    // ==================================================
    // GET GRID MANAGER
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}