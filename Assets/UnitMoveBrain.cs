using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private GridManager gridManager;

    [Header("Debug")]
    [SerializeField] private bool debugMovement = true;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };


    // ==================================================
    // DEBUG
    // ==================================================

    private void MoveDebug(string message)
    {
        if (!debugMovement)
        {
            return;
        }

        Debug.Log(
            $"[UnitMoveBrain] {gameObject.name}: {message}",
            gameObject
        );
    }


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit =
                GetComponent<AttackUnit>();
        }

        EnsureGridManager();

        MoveDebug(
            $"Awake. AttackUnit=" +
            $"{(attackUnit != null ? "FOUND" : "NULL")}, " +
            $"GridManager=" +
            $"{(gridManager != null ? "FOUND" : "NULL")}"
        );
    }


    // ==================================================
    // MOVE TOWARDS ENEMY
    // ==================================================

    public bool TryMoveTowardsEnemy()
    {
        if (!CanMove())
        {
            return false;
        }

        EnsureGridManager();

        if (gridManager == null)
        {
            MoveDebug(
                "FAILED: GridManager is NULL."
            );

            return false;
        }

        AttackUnit enemy =
            FindNearestEnemy();

        if (enemy == null)
        {
            MoveDebug(
                "FAILED: No enemy found."
            );

            return false;
        }

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );

        Vector2Int enemyPosition =
            gridManager.WorldToGridPosition(
                enemy.transform.position
            );

        MoveDebug(
            $"Current={currentPosition}, " +
            $"Enemy={enemy.name}, " +
            $"EnemyPosition={enemyPosition}"
        );


        // ==================================================
        // FIND BEST MOVE
        // ==================================================

        Vector2Int nextPosition =
            FindBestMove(
                currentPosition,
                enemyPosition
            );

        if (nextPosition == currentPosition)
        {
            MoveDebug(
                "No valid movement found."
            );

            return false;
        }


        // ==================================================
        // SAFETY CHECK
        // ==================================================

        if (!gridManager.IsInsideGrid(nextPosition))
        {
            MoveDebug(
                $"FAILED: Next position " +
                $"{nextPosition} is outside grid."
            );

            return false;
        }

        if (gridManager.IsCellOccupied(nextPosition))
        {
            MoveDebug(
                $"FAILED: Next position " +
                $"{nextPosition} is occupied."
            );

            return false;
        }


        // ==================================================
        // MOVE
        // ==================================================

        bool moved =
            gridManager.MoveUnit(
                gameObject,
                currentPosition,
                nextPosition
            );

        MoveDebug(
            $"MOVE: {currentPosition} -> " +
            $"{nextPosition}. Success={moved}"
        );

        return moved;
    }


    // ==================================================
    // CAN MOVE
    // ==================================================

    private bool CanMove()
    {
        if (attackUnit == null)
        {
            MoveDebug(
                "FAILED: AttackUnit is NULL."
            );

            return false;
        }

        if (attackUnit.IsDead())
        {
            MoveDebug(
                "FAILED: Unit is dead."
            );

            return false;
        }

        return true;
    }


    // ==================================================
    // FIND BEST MOVE
    // ==================================================

    private Vector2Int FindBestMove(
        Vector2Int start,
        Vector2Int target)
    {
        if (start == target)
        {
            return start;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);


        // ==================================================
        // BFS
        // ==================================================

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            for (
                int i = 0;
                i < Directions.Length;
                i++
            )
            {
                Vector2Int next =
                    current + Directions[i];

                if (visited.Contains(next))
                {
                    continue;
                }

                if (!gridManager.IsInsideGrid(next))
                {
                    continue;
                }


                // ==================================================
                // TARGET CELL
                // ==================================================

                if (next == target)
                {
                    /*
                     * The target occupies this cell.
                     *
                     * Record the target in the path,
                     * but return the first step so that
                     * we never move onto the enemy.
                     */

                    cameFrom[next] = current;

                    Vector2Int firstStep =
                        ReconstructFirstStep(
                            start,
                            next,
                            cameFrom
                        );

                    MoveDebug(
                        $"Path found to enemy. " +
                        $"First step={firstStep}"
                    );

                    return firstStep;
                }


                // ==================================================
                // OCCUPIED CELL
                // ==================================================

                if (gridManager.IsCellOccupied(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }


        // ==================================================
        // NO DIRECT PATH
        // ==================================================

        MoveDebug(
            "No complete path to enemy. " +
            "Searching for closest reachable cell."
        );

        return FindClosestReachableStep(
            start,
            target
        );
    }


    // ==================================================
    // RECONSTRUCT FIRST STEP
    // ==================================================

    private Vector2Int ReconstructFirstStep(
        Vector2Int start,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        Vector2Int current =
            destination;

        Vector2Int previous;

        while (
            cameFrom.TryGetValue(
                current,
                out previous
            )
        )
        {
            if (previous == start)
            {
                return current;
            }

            current = previous;
        }

        return start;
    }


    // ==================================================
    // CLOSEST REACHABLE CELL
    // ==================================================

    private Vector2Int FindClosestReachableStep(
        Vector2Int start,
        Vector2Int target)
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int bestCell =
            start;

        int bestDistance =
            gridManager.GetDistance(
                start,
                target
            );


        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            int currentDistance =
                gridManager.GetDistance(
                    current,
                    target
                );

            if (currentDistance < bestDistance)
            {
                bestDistance =
                    currentDistance;

                bestCell =
                    current;
            }

            for (
                int i = 0;
                i < Directions.Length;
                i++
            )
            {
                Vector2Int next =
                    current + Directions[i];

                if (visited.Contains(next))
                {
                    continue;
                }

                if (!gridManager.IsInsideGrid(next))
                {
                    continue;
                }

                if (gridManager.IsCellOccupied(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }


        if (bestCell == start)
        {
            return start;
        }

        Vector2Int firstStep =
            ReconstructFirstStep(
                start,
                bestCell,
                cameFrom
            );

        MoveDebug(
            $"Closest reachable cell={bestCell}. " +
            $"FirstStep={firstStep}"
        );

        return firstStep;
    }


    // ==================================================
    // FIND NEAREST ENEMY
    // ==================================================

    private AttackUnit FindNearestEnemy()
    {
        if (attackUnit == null)
        {
            return null;
        }

        EnsureGridManager();

        if (gridManager == null)
        {
            return null;
        }

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        AttackUnit nearestEnemy =
            null;

        int nearestDistance =
            int.MaxValue;

        Vector2Int myPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );


        for (
            int i = 0;
            i < allUnits.Length;
            i++
        )
        {
            AttackUnit other =
                allUnits[i];

            if (other == null ||
                other == attackUnit)
            {
                continue;
            }

            if (other.IsDead())
            {
                continue;
            }

            if (other.GetTeam() ==
                attackUnit.GetTeam())
            {
                continue;
            }

            Vector2Int enemyPosition =
                gridManager.WorldToGridPosition(
                    other.transform.position
                );

            int distance =
                gridManager.GetDistance(
                    myPosition,
                    enemyPosition
                );

            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestEnemy =
                    other;
            }
        }

        if (nearestEnemy != null)
        {
            MoveDebug(
                $"Nearest enemy: " +
                $"{nearestEnemy.name}, " +
                $"distance={nearestDistance}"
            );
        }

        return nearestEnemy;
    }


    // ==================================================
    // GRID
    // ==================================================

    private void EnsureGridManager()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }


    // ==================================================
    // ACCESSORS
    // ==================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }


    public GridManager GetGridManager()
    {
        EnsureGridManager();

        return gridManager;
    }
}