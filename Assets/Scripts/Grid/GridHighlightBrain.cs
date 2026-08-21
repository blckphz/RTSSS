using System.Collections.Generic;
using UnityEngine;

public class GridHighlightBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GridHighlightManager highlightManager;


    [Header("Debug Settings")]
    [SerializeField]
    private bool enableDebugLogs = true;


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        FindReferences();
    }


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


        if (highlightManager == null)
        {
            highlightManager =
                GetComponent<GridHighlightManager>();
        }


        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }


        if (
            gridManager == null &&
            enableDebugLogs
        )
        {
            Debug.LogError(
                "[GridHighlightBrain] " +
                "GridManager reference is missing!",
                this
            );
        }


        if (
            highlightManager == null &&
            enableDebugLogs
        )
        {
            Debug.LogError(
                "[GridHighlightBrain] " +
                "GridHighlightManager reference is missing!",
                this
            );
        }
    }


    // =============================================================
    // MOVEMENT RANGE
    // =============================================================

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        if (user != null)
        {
            UnitMoveBrain moveBrain =
                user.GetComponent<UnitMoveBrain>();


            if (
                moveBrain != null &&
                !moveBrain.CanMoveThisTurn()
            )
            {
                highlightManager.ClearMovementRange();


                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[GridHighlightBrain] " +
                        $"Movement range blocked for " +
                        $"{user.name}. Movement already consumed."
                    );
                }


                return;
            }
        }


        if (!gridManager.IsInsideGrid(
                centerPosition))
        {
            highlightManager.ClearMovementRange();
            return;
        }


        range =
            Mathf.Max(
                0,
                range
            );


        List<Vector2Int> movementCells =
            CalculateMovementRange(
                centerPosition,
                range
            );


        highlightManager.ShowMovementTiles(
            movementCells,
            user
        );


        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GridHighlightBrain] " +
                $"Movement range calculated. " +
                $"Center: {centerPosition}. " +
                $"Range: {range}. " +
                $"Cells: {movementCells.Count}."
            );
        }
    }


    private List<Vector2Int> CalculateMovementRange(
        Vector2Int centerPosition,
        int range)
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();


        int width =
            gridManager.GetWidth();


        int height =
            gridManager.GetHeight();


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


                if (position == centerPosition)
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


                if (gridManager.IsCellOccupied(
                        position))
                {
                    continue;
                }


                cells.Add(position);
            }
        }


        return cells;
    }


    // =============================================================
    // BASIC ABILITY RANGE
    // =============================================================

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        if (!gridManager.IsInsideGrid(
                centerPosition))
        {
            highlightManager.ClearAbilityRange();
            return;
        }


        range =
            Mathf.Max(
                0,
                range
            );


        List<Vector2Int> cells =
            CalculateAbilityRange(
                centerPosition,
                range
            );


        highlightManager.ShowAbilityTiles(
            cells
        );
    }


    private List<Vector2Int> CalculateAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();


        int width =
            gridManager.GetWidth();


        int height =
            gridManager.GetHeight();


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


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
                    cells.Add(position);
                }
            }
        }


        return cells;
    }


    // =============================================================
    // ABILITY SO
    // =============================================================

    public void ShowAbilityRange(
        AbilitySO ability,
        GameObject user)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        if (
            ability == null ||
            user == null
        )
        {
            highlightManager.ClearAbilityRange();
            return;
        }


        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                user
            );


        if (rangeTiles == null)
        {
            highlightManager.ClearAbilityRange();
            return;
        }


        List<Vector2Int> validTiles =
            FilterValidAbilityTiles(
                ability,
                user,
                rangeTiles
            );


        bool isHealAbility =
            ability is HealAbilitySO;


        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GridHighlightBrain] " +
                $"Showing ability '{ability.name}'. " +
                $"Heal: {isHealAbility}. " +
                $"Valid tiles: {validTiles.Count}."
            );
        }


        highlightManager.ShowAbilityTiles(
            validTiles,
            user,
            isHealAbility
        );
    }


    private List<Vector2Int> FilterValidAbilityTiles(
        AbilitySO ability,
        GameObject user,
        List<Vector2Int> positions)
    {
        List<Vector2Int> validTiles =
            new List<Vector2Int>();


        if (
            ability == null ||
            user == null ||
            positions == null
        )
        {
            return validTiles;
        }


        foreach (Vector2Int position in positions)
        {
            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            if (validTiles.Contains(
                    position))
            {
                continue;
            }


            bool canHit =
                ability.CanHitTile(
                    gridManager,
                    user,
                    position
                );


            if (canHit)
            {
                validTiles.Add(position);
            }
        }


        return validTiles;
    }


    // =============================================================
    // ABILITY TILES
    // =============================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        List<Vector2Int> validTiles =
            FilterValidTiles(
                positions
            );


        highlightManager.ShowAbilityTiles(
            validTiles,
            user
        );
    }


    // =============================================================
    // ABILITY CELLS FROM OFFSETS
    // =============================================================

    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets,
        GameObject user = null)
    {
        if (
            gridManager == null ||
            highlightManager == null ||
            offsets == null
        )
        {
            return;
        }


        List<Vector2Int> cells =
            new List<Vector2Int>();


        foreach (
            Vector2Int offset
            in offsets
        )
        {
            Vector2Int position =
                origin + offset;


            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            if (!cells.Contains(position))
            {
                cells.Add(position);
            }
        }


        highlightManager.ShowAbilityTiles(
            cells,
            user
        );
    }


    // =============================================================
    // SINGLE ABILITY CELL
    // =============================================================

    public void ShowAbilityCell(
        Vector2Int position)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        if (!gridManager.IsInsideGrid(
                position))
        {
            return;
        }


        highlightManager.ShowAbilityCell(
            position
        );
    }


    // =============================================================
    // TILE FILTERING
    // =============================================================

    private List<Vector2Int> FilterValidTiles(
        List<Vector2Int> positions)
    {
        List<Vector2Int> validTiles =
            new List<Vector2Int>();


        if (positions == null)
        {
            return validTiles;
        }


        foreach (
            Vector2Int position
            in positions
        )
        {
            if (!gridManager.IsInsideGrid(
                    position))
            {
                continue;
            }


            if (!validTiles.Contains(
                    position))
            {
                validTiles.Add(position);
            }
        }


        return validTiles;
    }


    // =============================================================
    // ENEMY CHECK
    // =============================================================

    public bool IsEnemyUnit(
        GameObject unit,
        GameObject sourceUser)
    {
        if (unit == null)
        {
            return false;
        }


        AttackUnit attackUnit =
            unit.GetComponent<AttackUnit>();


        if (
            attackUnit != null &&
            !attackUnit.IsDead()
        )
        {
            if (sourceUser != null)
            {
                AttackUnit sourceAttackUnit =
                    sourceUser.GetComponent<AttackUnit>();


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


        if (
            targetHealth != null &&
            !targetHealth.IsDead()
        )
        {
            if (sourceUser != null)
            {
                HealthManager sourceHealth =
                    sourceUser.GetComponent<HealthManager>();


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
    // QUERIES
    // =============================================================

    public bool IsCellInsideGrid(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return false;
        }


        return gridManager.IsInsideGrid(
            position
        );
    }


    public GridManager GetGridManager()
    {
        return gridManager;
    }


    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }
}