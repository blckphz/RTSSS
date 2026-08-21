using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Brain Reference")]
    [SerializeField] private GridHighlightBrain brain;


    // ============================================================
    // ENEMY TARGET HOVER
    // ============================================================

    [Header("Enemy Target Hover")]
    [SerializeField] private Color enemyHoverColor = Color.red;

    [SerializeField]
    private bool enableEnemyHoverShader = true;

    [SerializeField]
    private string enemyHoverObjectName =
        "HoverShaderSprite";


    // ============================================================
    // FRIENDLY HEAL TARGET HOVER
    // ============================================================

    [Header("Friendly Heal Target Hover")]
    [SerializeField] private Color healHoverColor = Color.green;

    [SerializeField]
    private bool enableHealHoverShader = true;

    [SerializeField]
    private string healHoverObjectName =
        "HoverShaderSprite";


    // ============================================================
    // TARGET PULSE
    // ============================================================

    [Header("Target Hover Pulse")]

    [SerializeField]
    private bool enableTargetPulse = true;

    [SerializeField, Min(0f)]
    private float targetPulseAmount = 0.06f;

    [SerializeField, Min(0.01f)]
    private float targetPulseSpeed = 5f;

    [SerializeField, Min(0.01f)]
    private float targetPulseSmoothSpeed = 12f;


    // ============================================================
    // EXPLOSION
    // ============================================================

    [Header("Animations")]

    [SerializeField, Min(0.01f)]
    private float explosionPulseDuration = 0.12f;


    // ============================================================
    // DATA CACHING
    // ============================================================

    private readonly Dictionary<Vector2Int, GridHighlightVisuals>
        tileVisuals =
        new Dictionary<Vector2Int, GridHighlightVisuals>(256);


    private readonly HashSet<Vector2Int>
        abilityCells =
        new HashSet<Vector2Int>();


    private readonly HashSet<Vector2Int>
        movementCells =
        new HashSet<Vector2Int>();


    private readonly HashSet<Vector2Int>
        explosionCells =
        new HashSet<Vector2Int>();


    private readonly HashSet<SpriteRenderer>
        activeTargetHoverRenderers =
        new HashSet<SpriteRenderer>();


    private readonly Dictionary<GameObject, AttackUnit>
        unitComponentCache =
        new Dictionary<GameObject, AttackUnit>();


    private readonly List<Vector2Int>
        tempCellList =
        new List<Vector2Int>(64);


    private readonly List<SpriteRenderer>
        tempRendererList =
        new List<SpriteRenderer>(16);


    // ============================================================
    // TARGET PULSE CACHE
    // ============================================================

    private readonly Dictionary<Transform, Vector3>
        targetOriginalScales =
        new Dictionary<Transform, Vector3>(16);


    private readonly HashSet<Transform>
        activeTargetPulseTransforms =
        new HashSet<Transform>();


    private readonly List<Transform>
        tempTargetTransformList =
        new List<Transform>(16);


    // ============================================================
    // SHADER PROPERTY BLOCK
    // ============================================================

    private MaterialPropertyBlock hoverPropertyBlock;


    private static readonly int OutlineColorID =
        Shader.PropertyToID(
            "_OutlineColor"
        );


    // ============================================================
    // STATE
    // ============================================================

    private Vector2Int placementPosition;

    private bool hasPlacementPosition;


    private GameObject currentRangeUser;

    private AttackUnit currentRangeUserUnit;


    private bool suppressMovementHighlight;

    private bool currentAbilityIsHeal;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        hoverPropertyBlock =
            new MaterialPropertyBlock();

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


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (
            gridManager == null &&
            !TryGetComponent(out gridManager)
        )
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (
            brain == null &&
            !TryGetComponent(out brain)
        )
        {
            brain =
                FindFirstObjectByType<GridHighlightBrain>();
        }
    }


    // ============================================================
    // CACHE MANAGEMENT
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
                out GridHighlightVisuals existingVisual
            )
        )
        {
            return existingVisual;
        }


        GameObject tile =
            gridManager.GetFloorTile(
                position
            );


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


        tileVisuals[position] =
            visuals;


        return visuals;
    }


    public void RebuildTileCache()
    {
        unitComponentCache.Clear();

        CacheTiles();
    }


    public void UncacheUnit(
        GameObject unit)
    {
        if (unit != null)
        {
            unitComponentCache.Remove(unit);
        }
    }


    // ============================================================
    // PLACEMENT
    // ============================================================

    public void SetPlacementTile(
        Vector2Int position)
    {
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


        hasPlacementPosition =
            true;


        if (hadOldPosition)
        {
            RefreshTile(
                oldPosition
            );
        }


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
            FindReferences();
        }


        if (brain == null)
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
        ClearMovementRange();


        SetCurrentRangeUser(
            user
        );


        if (cells == null)
        {
            return;
        }


        int count =
            cells.Count;


        for (int i = 0; i < count; i++)
        {
            Vector2Int pos =
                cells[i];


            if (
                gridManager != null &&
                gridManager.IsInsideGrid(pos)
            )
            {
                if (movementCells.Add(pos))
                {
                    RefreshTile(pos);
                }
            }
        }
    }


    public void ClearMovementRange()
    {
        if (movementCells.Count == 0)
        {
            SetCurrentRangeUser(null);

            return;
        }


        tempCellList.Clear();


        tempCellList.AddRange(
            movementCells
        );


        movementCells.Clear();


        int count =
            tempCellList.Count;


        for (int i = 0; i < count; i++)
        {
            RefreshTile(
                tempCellList[i]
            );
        }


        SetCurrentRangeUser(
            null
        );
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
        if (
            suppressMovementHighlight ==
            suppressed
        )
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
        ClearAbilityRange();


        SetMovementHighlightSuppressed(
            true
        );


        SetCurrentRangeUser(
            user
        );


        currentAbilityIsHeal =
            isHealAbility;


        if (positions != null)
        {
            int count =
                positions.Count;


            for (int i = 0; i < count; i++)
            {
                Vector2Int pos =
                    positions[i];


                if (
                    gridManager != null &&
                    gridManager.IsInsideGrid(pos)
                )
                {
                    abilityCells.Add(pos);
                }
            }
        }


        RefreshCells(
            abilityCells
        );


        RefreshAllTargetHovers();
    }


    public void ShowAbilityCell(
        Vector2Int position)
    {
        SetMovementHighlightSuppressed(
            true
        );


        if (
            gridManager == null ||
            !gridManager.IsInsideGrid(position)
        )
        {
            return;
        }


        if (abilityCells.Add(position))
        {
            RefreshTile(
                position
            );


            RefreshTargetHoverForTile(
                position
            );
        }
    }


    public void ClearAbilityRange()
    {
        ClearAllTargetHovers();


        ClearAllTargetPulse();


        SetCurrentRangeUser(
            null
        );


        currentAbilityIsHeal =
            false;


        SetMovementHighlightSuppressed(
            false
        );


        if (abilityCells.Count == 0)
        {
            return;
        }


        tempCellList.Clear();


        tempCellList.AddRange(
            abilityCells
        );


        abilityCells.Clear();


        int count =
            tempCellList.Count;


        for (int i = 0; i < count; i++)
        {
            RefreshTile(
                tempCellList[i]
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


    public bool IsCurrentAbilityHeal()
    {
        return currentAbilityIsHeal;
    }


    // ============================================================
    // TILE REFRESH
    // ============================================================

    private void RefreshCells(
        HashSet<Vector2Int> cells)
    {
        foreach (
            Vector2Int position in cells
        )
        {
            RefreshTile(
                position
            );
        }
    }


    private void RefreshTile(
        Vector2Int position)
    {
        GridHighlightVisuals visual =
            CacheTile(
                position
            );


        if (visual == null)
        {
            return;
        }


        if (explosionCells.Contains(position))
        {
            return;
        }


        // ========================================================
        // PLACEMENT
        // ========================================================

        if (
            hasPlacementPosition &&
            placementPosition == position
        )
        {
            visual.ShowPlacement();

            return;
        }


        // ========================================================
        // ABILITY
        // ========================================================

        if (abilityCells.Contains(position))
        {
            GameObject unit =
                gridManager.GetUnitAt(
                    position
                );


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


        // ========================================================
        // MOVEMENT
        // ========================================================

        if (
            !suppressMovementHighlight &&
            movementCells.Contains(position)
        )
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
    // UNIT CACHE
    // ============================================================

    private void SetCurrentRangeUser(
        GameObject user)
    {
        currentRangeUser =
            user;


        currentRangeUserUnit =
            user != null
                ? GetCachedAttackUnit(user)
                : null;
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
                out AttackUnit unit
            )
        )
        {
            if (unit == null)
            {
                unitComponentCache.Remove(
                    target
                );


                return GetCachedAttackUnit(
                    target
                );
            }


            return unit;
        }


        if (
            target.TryGetComponent(
                out unit
            )
        )
        {
            unitComponentCache[target] =
                unit;
        }


        return unit;
    }


    // ============================================================
    // TARGET VALIDATION
    // ============================================================

    private bool IsValidAbilityTarget(
        GameObject target)
    {
        if (
            currentRangeUserUnit == null ||
            target == null
        )
        {
            return false;
        }


        AttackUnit targetUnit =
            GetCachedAttackUnit(
                target
            );


        if (
            targetUnit == null ||
            targetUnit.IsDead()
        )
        {
            return false;
        }


        Team userTeam =
            currentRangeUserUnit.GetTeam();


        Team targetTeam =
            targetUnit.GetTeam();


        // ========================================================
        // HEAL
        // ========================================================

        if (currentAbilityIsHeal)
        {
            if (
                userTeam == Team.Player ||
                userTeam == Team.Ally
            )
            {
                return
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;
            }


            if (userTeam == Team.Enemy)
            {
                return
                    targetTeam == Team.Enemy;
            }


            return false;
        }


        // ========================================================
        // ATTACK
        // ========================================================

        if (
            userTeam == Team.Player ||
            userTeam == Team.Ally
        )
        {
            return
                targetTeam == Team.Enemy;
        }


        if (userTeam == Team.Enemy)
        {
            return
                targetTeam == Team.Player ||
                targetTeam == Team.Ally;
        }


        return false;
    }


    // ============================================================
    // PUBLIC TARGET VALIDATION
    // ============================================================

    public bool IsValidCurrentAbilityTarget(
        GameObject target)
    {
        if (target == null)
        {
            return false;
        }


        if (currentRangeUserUnit == null)
        {
            return false;
        }


        if (abilityCells.Count == 0)
        {
            return false;
        }


        return IsValidAbilityTarget(
            target
        );
    }


    // ============================================================
    // TARGET HOVER
    // ============================================================

    private void RefreshAllTargetHovers()
    {
        ClearAllTargetHovers();

        ClearAllTargetPulse();


        if (gridManager == null)
        {
            return;
        }


        foreach (
            Vector2Int position in abilityCells
        )
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
            gridManager.GetUnitAt(
                position
            );


        if (
            unit == null ||
            !IsValidAbilityTarget(unit)
        )
        {
            return;
        }


        // ========================================================
        // OUTLINE
        // ========================================================

        if (currentAbilityIsHeal)
        {
            if (enableHealHoverShader)
            {
                SetTargetHover(
                    unit,
                    true,
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
                    true,
                    enemyHoverColor,
                    enemyHoverObjectName
                );
            }
        }


        // ========================================================
        // PULSE
        // ========================================================

        if (enableTargetPulse)
        {
            AddTargetPulse(
                unit.transform
            );
        }
    }


    private void SetTargetHover(
        GameObject targetUnit,
        bool enabled,
        Color outlineColor,
        string hoverObjectName)
    {
        if (targetUnit == null)
        {
            return;
        }


        Transform hoverShader =
            targetUnit.transform.Find(
                hoverObjectName
            );


        if (hoverShader == null)
        {
            SpriteRenderer sr =
                targetUnit.GetComponentInChildren<SpriteRenderer>();


            if (
                sr != null &&
                sr.gameObject != targetUnit
            )
            {
                hoverShader =
                    sr.transform;
            }
        }


        if (
            hoverShader == null ||
            !hoverShader.TryGetComponent(
                out SpriteRenderer renderer
            )
        )
        {
            return;
        }


        // ========================================================
        // ENABLE
        // ========================================================

        if (enabled)
        {
            renderer.GetPropertyBlock(
                hoverPropertyBlock
            );


            hoverPropertyBlock.SetColor(
                OutlineColorID,
                outlineColor
            );


            renderer.SetPropertyBlock(
                hoverPropertyBlock
            );


            renderer.enabled =
                true;


            activeTargetHoverRenderers.Add(
                renderer
            );
        }


        // ========================================================
        // DISABLE
        // ========================================================

        else
        {
            renderer.enabled =
                false;


            renderer.SetPropertyBlock(
                null
            );


            activeTargetHoverRenderers.Remove(
                renderer
            );
        }
    }


    private void ClearAllTargetHovers()
    {
        if (
            activeTargetHoverRenderers.Count == 0
        )
        {
            return;
        }


        tempRendererList.Clear();


        tempRendererList.AddRange(
            activeTargetHoverRenderers
        );


        activeTargetHoverRenderers.Clear();


        int count =
            tempRendererList.Count;


        for (int i = 0; i < count; i++)
        {
            SpriteRenderer renderer =
                tempRendererList[i];


            if (renderer != null)
            {
                renderer.enabled =
                    false;


                renderer.SetPropertyBlock(
                    null
                );
            }
        }
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
            targetOriginalScales.Add(
                target,
                target.localScale
            );
        }


        activeTargetPulseTransforms.Add(
            target
        );
    }


    private void UpdateTargetPulse()
    {
        if (!enableTargetPulse)
        {
            ResetTargetPulseScales();

            return;
        }


        if (
            activeTargetPulseTransforms.Count == 0
        )
        {
            return;
        }


        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    targetPulseSpeed
                ) + 1f
            ) * 0.5f;


        float targetMultiplier =
            1f +
            pulse *
            targetPulseAmount;


        tempTargetTransformList.Clear();


        tempTargetTransformList.AddRange(
            activeTargetPulseTransforms
        );


        int count =
            tempTargetTransformList.Count;


        for (int i = 0; i < count; i++)
        {
            Transform target =
                tempTargetTransformList[i];


            if (target == null)
            {
                activeTargetPulseTransforms.Remove(
                    target
                );

                continue;
            }


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
                originalScale *
                targetMultiplier;


            target.localScale =
                Vector3.Lerp(
                    target.localScale,
                    desiredScale,
                    Time.deltaTime *
                    targetPulseSmoothSpeed
                );
        }
    }


    private void ClearAllTargetPulse()
    {
        if (
            activeTargetPulseTransforms.Count == 0 &&
            targetOriginalScales.Count == 0
        )
        {
            return;
        }


        ResetTargetPulseScales();


        activeTargetPulseTransforms.Clear();
    }


    private void ResetTargetPulseScales()
    {
        if (targetOriginalScales.Count == 0)
        {
            return;
        }


        tempTargetTransformList.Clear();


        foreach (
            KeyValuePair<Transform, Vector3> pair
            in targetOriginalScales
        )
        {
            tempTargetTransformList.Add(
                pair.Key
            );
        }


        int count =
            tempTargetTransformList.Count;


        for (int i = 0; i < count; i++)
        {
            Transform target =
                tempTargetTransformList[i];


            if (target == null)
            {
                continue;
            }


            if (
                targetOriginalScales.TryGetValue(
                    target,
                    out Vector3 originalScale
                )
            )
            {
                target.localScale =
                    Vector3.Lerp(
                        target.localScale,
                        originalScale,
                        Time.deltaTime *
                        targetPulseSmoothSpeed
                    );


                if (
                    (
                        target.localScale -
                        originalScale
                    ).sqrMagnitude <
                    0.000001f
                )
                {
                    target.localScale =
                        originalScale;
                }
            }
        }


        // --------------------------------------------------------
        // Remove transforms that have returned to normal
        // --------------------------------------------------------

        tempTargetTransformList.Clear();


        foreach (
            KeyValuePair<Transform, Vector3> pair
            in targetOriginalScales
        )
        {
            Transform target =
                pair.Key;


            if (target == null)
            {
                tempTargetTransformList.Add(
                    target
                );

                continue;
            }


            if (
                (
                    target.localScale -
                    pair.Value
                ).sqrMagnitude <
                0.000001f
            )
            {
                tempTargetTransformList.Add(
                    target
                );
            }
        }


        for (
            int i = 0;
            i < tempTargetTransformList.Count;
            i++
        )
        {
            targetOriginalScales.Remove(
                tempTargetTransformList[i]
            );
        }
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
            CacheTile(
                position
            );


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


    private IEnumerator RemoveExplosionCellAfterDelay(
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
        ClearPlacementTile();


        ClearMovementRange();


        ClearAbilityRange();


        ClearAllTargetHovers();


        ClearAllTargetPulse();


        explosionCells.Clear();


        suppressMovementHighlight =
            false;


        currentAbilityIsHeal =
            false;


        foreach (
            var pair in tileVisuals
        )
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

    public GridManager GetGridManager()
    {
        return gridManager;
    }


    public GridHighlightBrain GetBrain()
    {
        return brain;
    }
}