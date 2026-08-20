using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;

    [SerializeField]
    private GridManager gridManager;


    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    private float moveDuration = 0.25f;


    [Header("AI")]
    [SerializeField, Min(1)]
    private int attackRange = 1;


    [SerializeField]
    private bool preferLowHealthEnemies = true;


    [SerializeField]
    private bool preferCloserEnemies = true;


    [SerializeField]
    private bool avoidMovingThroughEnemies = true;


    [SerializeField]
    private bool avoidMovingThroughAllies = true;


    [Header("Tactical")]
    [SerializeField]
    private bool preferCloserAttackPosition = true;


    [SerializeField]
    private bool preferMoreOpenPositions = true;


    [SerializeField]
    private bool preferSidePositions = true;


    [SerializeField]
    private bool allowMovingAwayToGoAroundObstacles = true;


    [Header("Debug")]
    [SerializeField]
    private bool debugMovement = true;


    // ==================================================
    // DATA
    // ==================================================

    private bool isMoving;


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

    private void MoveDebug(
        string message)
    {
        if (!debugMovement)
        {
            return;
        }


        Debug.Log(
            $"[UnitMoveBrain] " +
            $"{gameObject.name}: {message}",
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
    // PLAYER MOVEMENT
    // ==================================================

    public bool TryMoveTo(
        Vector2Int destination)
    {
        if (isMoving)
        {
            MoveDebug(
                "FAILED: Unit is already moving."
            );

            return false;
        }


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


        Vector2Int start =
            gridManager.WorldToGridPosition(
                transform.position
            );


        if (start == destination)
        {
            return false;
        }


        if (!gridManager.IsInsideGrid(
                destination))
        {
            MoveDebug(
                $"FAILED: {destination} " +
                "outside grid."
            );

            return false;
        }


        if (gridManager.IsCellOccupied(
                destination))
        {
            MoveDebug(
                $"FAILED: {destination} " +
                "is occupied."
            );

            return false;
        }


        int moveRange =
            GetMoveRange();


        if (moveRange <= 0)
        {
            MoveDebug(
                "FAILED: Move range is 0."
            );

            return false;
        }


        // ==================================================
        // FIND ACTUAL PATH
        // ==================================================

        List<Vector2Int> path =
            FindPath(
                start,
                destination
            );


        if (path == null ||
            path.Count < 2)
        {
            MoveDebug(
                $"FAILED: No path from " +
                $"{start} to {destination}."
            );

            return false;
        }


        int movementCost =
            path.Count - 1;


        if (movementCost > moveRange)
        {
            MoveDebug(
                $"FAILED: Destination requires " +
                $"{movementCost} movement, " +
                $"but range is {moveRange}."
            );

            return false;
        }


        MoveDebug(
            $"PLAYER MOVE: " +
            $"{start} -> {destination}. " +
            $"Cost={movementCost}, " +
            $"Range={moveRange}"
        );


        StartCoroutine(
            MoveAlongPath(path)
        );


        return true;
    }


    // ==================================================
    // PLAYER PATH MOVEMENT
    // ==================================================

    private IEnumerator MoveAlongPath(
        List<Vector2Int> path)
    {
        if (path == null ||
            path.Count < 2)
        {
            yield break;
        }


        isMoving = true;


        for (int i = 1;
             i < path.Count;
             i++)
        {
            Vector2Int currentPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );


            Vector2Int nextPosition =
                path[i];


            // ==================================================
            // SAFETY
            // ==================================================

            if (!gridManager.IsInsideGrid(
                    nextPosition))
            {
                MoveDebug(
                    $"Movement stopped. " +
                    $"{nextPosition} outside grid."
                );

                break;
            }


            // ==================================================
            // RESERVE NEXT CELL
            // ==================================================

            bool movementStarted =
                gridManager.StartMoveUnit(
                    gameObject,
                    currentPosition,
                    nextPosition
                );


            if (!movementStarted)
            {
                MoveDebug(
                    $"Movement stopped. " +
                    $"Could not reserve {nextPosition}."
                );

                break;
            }


            // ==================================================
            // WORLD POSITIONS
            // ==================================================

            Vector3 startWorldPosition =
                transform.position;


            Vector3 targetWorldPosition =
                gridManager.GridToWorldPosition(
                    nextPosition
                );


            float elapsed =
                0f;


            // ==================================================
            // SMOOTH MOVEMENT
            // ==================================================

            while (elapsed < moveDuration)
            {
                if (!CanMove())
                {
                    transform.position =
                        targetWorldPosition;

                    break;
                }


                elapsed +=
                    Time.deltaTime;


                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        moveDuration
                    );


                progress =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        progress
                    );


                transform.position =
                    Vector3.Lerp(
                        startWorldPosition,
                        targetWorldPosition,
                        progress
                    );


                yield return null;
            }


            // ==================================================
            // FINAL POSITION
            // ==================================================

            transform.position =
                targetWorldPosition;


            gridManager.FinishMoveUnit(
                gameObject,
                nextPosition
            );


            yield return null;
        }


        isMoving = false;


        MoveDebug(
            "PLAYER MOVEMENT COMPLETE."
        );
    }


    // ==================================================
    // AI MOVEMENT
    // ==================================================

    public IEnumerator MoveTowardsEnemy()
    {
        if (isMoving)
        {
            MoveDebug(
                "FAILED: Unit is already moving."
            );

            yield break;
        }


        if (!CanMove())
        {
            yield break;
        }


        EnsureGridManager();


        if (gridManager == null)
        {
            MoveDebug(
                "FAILED: GridManager is NULL."
            );

            yield break;
        }


        // ==================================================
        // FIND BEST TARGET
        // ==================================================

        AttackUnit target =
            FindBestTarget();


        if (target == null)
        {
            MoveDebug(
                "FAILED: No valid enemy found."
            );

            yield break;
        }


        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );


        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        MoveDebug(
            $"Target selected: {target.name}. " +
            $"Current={currentPosition}, " +
            $"Target={targetPosition}"
        );


        // ==================================================
        // CHECK CURRENT RANGE
        // ==================================================

        int currentDistance =
            gridManager.GetDistance(
                currentPosition,
                targetPosition
            );


        if (currentDistance <= attackRange)
        {
            MoveDebug(
                $"Already in attack range. " +
                $"Distance={currentDistance}, " +
                $"Range={attackRange}"
            );

            yield break;
        }


        // ==================================================
        // FIND BEST ATTACK POSITION
        // ==================================================

        Vector2Int attackPosition =
            FindBestAttackPosition(
                currentPosition,
                targetPosition
            );


        if (attackPosition ==
            currentPosition)
        {
            MoveDebug(
                "No better attack position found."
            );

            yield break;
        }


        MoveDebug(
            $"Best attack position: " +
            $"{attackPosition}"
        );


        // ==================================================
        // FIND PATH
        // ==================================================

        List<Vector2Int> path =
            FindPath(
                currentPosition,
                attackPosition
            );


        if (path == null ||
            path.Count < 2)
        {
            MoveDebug(
                "FAILED: No valid path to " +
                "attack position."
            );

            yield break;
        }


        // ==================================================
        // FIRST STEP
        // ==================================================

        Vector2Int nextPosition =
            path[1];


        MoveDebug(
            $"Path found. " +
            $"PathLength={path.Count - 1}, " +
            $"NextStep={nextPosition}"
        );


        // ==================================================
        // SAFETY
        // ==================================================

        if (!gridManager.IsInsideGrid(
                nextPosition))
        {
            MoveDebug(
                $"FAILED: {nextPosition} " +
                "outside grid."
            );

            yield break;
        }


        if (gridManager.IsCellOccupied(
                nextPosition))
        {
            MoveDebug(
                $"FAILED: {nextPosition} " +
                "became occupied."
            );

            yield break;
        }


        // ==================================================
        // RESERVE CELL
        // ==================================================

        bool movementStarted =
            gridManager.StartMoveUnit(
                gameObject,
                currentPosition,
                nextPosition
            );


        if (!movementStarted)
        {
            MoveDebug(
                $"FAILED: Could not reserve " +
                $"{nextPosition}."
            );

            yield break;
        }


        isMoving = true;


        // ==================================================
        // WORLD POSITIONS
        // ==================================================

        Vector3 startWorldPosition =
            transform.position;


        Vector3 targetWorldPosition =
            gridManager.GridToWorldPosition(
                nextPosition
            );


        // ==================================================
        // MOVE
        // ==================================================

        MoveDebug(
            $"MOVING: {currentPosition} -> " +
            $"{nextPosition}. " +
            $"Duration={moveDuration}s"
        );


        float elapsed =
            0f;


        while (elapsed < moveDuration)
        {
            if (!CanMove())
            {
                transform.position =
                    targetWorldPosition;

                break;
            }


            elapsed +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    moveDuration
                );


            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );


            transform.position =
                Vector3.Lerp(
                    startWorldPosition,
                    targetWorldPosition,
                    progress
                );


            yield return null;
        }


        // ==================================================
        // FINAL POSITION
        // ==================================================

        transform.position =
            targetWorldPosition;


        // ==================================================
        // FINISH
        // ==================================================

        gridManager.FinishMoveUnit(
            gameObject,
            nextPosition
        );


        isMoving = false;


        MoveDebug(
            $"MOVE COMPLETE: " +
            $"{currentPosition} -> " +
            $"{nextPosition}"
        );
    }


    // ==================================================
    // LEGACY MOVEMENT
    // ==================================================

    public bool TryMoveTowardsEnemy()
    {
        if (isMoving)
        {
            MoveDebug(
                "FAILED: Unit is already moving."
            );

            return false;
        }


        StartCoroutine(
            MoveTowardsEnemy()
        );


        return true;
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
    // MOVE RANGE
    // ==================================================

    public int GetMoveRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }


        CharacterSO characterData =
            attackUnit.GetCharacterData();


        if (characterData == null)
        {
            return 0;
        }


        return Mathf.Max(
            0,
            characterData.moveRange
        );
    }


    // ==================================================
    // TARGET SELECTION
    // ==================================================

    private AttackUnit FindBestTarget()
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


        AttackUnit bestTarget =
            null;


        float bestScore =
            float.MaxValue;


        Vector2Int myPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );


        for (int i = 0;
             i < allUnits.Length;
             i++)
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


            HealthManager enemyHealth =
                other.GetHealthManager();


            float score =
                0f;


            // ==================================================
            // DISTANCE
            // ==================================================

            if (preferCloserEnemies)
            {
                score +=
                    distance * 10f;
            }


            // ==================================================
            // LOW HEALTH
            // ==================================================

            if (preferLowHealthEnemies &&
                enemyHealth != null)
            {
                int health =
                    enemyHealth.GetHealth();


                int maxHealth =
                    Mathf.Max(
                        1,
                        enemyHealth.GetMaxHealth()
                    );


                float healthPercent =
                    (float)health /
                    maxHealth;


                score +=
                    healthPercent * 20f;
            }


            // ==================================================
            // TARGET ALREADY IN RANGE
            // ==================================================

            if (distance <= attackRange)
            {
                score -=
                    100f;
            }


            // ==================================================
            // DEBUG
            // ==================================================

            MoveDebug(
                $"Target candidate: " +
                $"{other.name}, " +
                $"distance={distance}, " +
                $"score={score}"
            );


            if (score < bestScore)
            {
                bestScore =
                    score;


                bestTarget =
                    other;
            }
        }


        if (bestTarget != null)
        {
            MoveDebug(
                $"BEST TARGET: " +
                $"{bestTarget.name}, " +
                $"score={bestScore}"
            );
        }


        return bestTarget;
    }


    // ==================================================
    // FIND BEST ATTACK POSITION
    // ==================================================

    private Vector2Int FindBestAttackPosition(
        Vector2Int start,
        Vector2Int target)
    {
        List<Vector2Int> candidates =
            GetAttackPositions(
                target
            );


        if (candidates.Count == 0)
        {
            MoveDebug(
                "No attack positions found."
            );


            return FindBestReachableCell(
                start,
                target
            );
        }


        Vector2Int bestPosition =
            start;


        float bestScore =
            float.MaxValue;


        bool foundReachablePosition =
            false;


        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            Vector2Int candidate =
                candidates[i];


            if (!gridManager.IsInsideGrid(
                    candidate))
            {
                continue;
            }


            // Our own cell is always allowed.
            if (candidate != start &&
                gridManager.IsCellOccupied(
                    candidate))
            {
                continue;
            }


            List<Vector2Int> path =
                FindPath(
                    start,
                    candidate
                );


            if (path == null ||
                path.Count < 2)
            {
                continue;
            }


            foundReachablePosition =
                true;


            int movementCost =
                path.Count - 1;


            int distanceToEnemy =
                gridManager.GetDistance(
                    candidate,
                    target
                );


            float score =
                movementCost * 10f;


            // ==================================================
            // PREFER SHORTER MOVEMENT
            // ==================================================

            if (preferCloserAttackPosition)
            {
                score +=
                    distanceToEnemy * 5f;
            }


            // ==================================================
            // OPEN SPACE
            // ==================================================

            if (preferMoreOpenPositions)
            {
                int openNeighbours =
                    CountOpenNeighbours(
                        candidate
                    );


                score -=
                    openNeighbours * 2f;
            }


            // ==================================================
            // SIDE POSITIONS
            // ==================================================

            if (preferSidePositions)
            {
                if (candidate.x != target.x &&
                    candidate.y != target.y)
                {
                    score +=
                        3f;
                }
            }


            MoveDebug(
                $"Attack position candidate: " +
                $"{candidate}, " +
                $"movementCost={movementCost}, " +
                $"distance={distanceToEnemy}, " +
                $"score={score}"
            );


            if (score < bestScore)
            {
                bestScore =
                    score;


                bestPosition =
                    candidate;
            }
        }


        if (foundReachablePosition)
        {
            MoveDebug(
                $"BEST ATTACK POSITION: " +
                $"{bestPosition}, " +
                $"score={bestScore}"
            );


            return bestPosition;
        }


        // ==================================================
        // NO ATTACK POSITION REACHABLE
        // ==================================================

        MoveDebug(
            "No attack position reachable. " +
            "Searching for best reachable cell."
        );


        return FindBestReachableCell(
            start,
            target
        );
    }


    // ==================================================
    // GET ATTACK POSITIONS
    // ==================================================

    private List<Vector2Int> GetAttackPositions(
        Vector2Int target)
    {
        List<Vector2Int> positions =
            new List<Vector2Int>();


        // ==================================================
        // RANGE 1
        // ==================================================

        if (attackRange == 1)
        {
            for (int i = 0;
                 i < Directions.Length;
                 i++)
            {
                positions.Add(
                    target +
                    Directions[i]
                );
            }


            return positions;
        }


        // ==================================================
        // LARGER RANGE
        // ==================================================

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


                if (position == target)
                {
                    continue;
                }


                int distance =
                    gridManager.GetDistance(
                        position,
                        target
                    );


                if (distance <= attackRange)
                {
                    positions.Add(
                        position
                    );
                }
            }
        }


        return positions;
    }


    // ==================================================
    // PATHFINDING
    // ==================================================

    private List<Vector2Int> FindPath(
        Vector2Int start,
        Vector2Int destination)
    {
        if (start == destination)
        {
            return new List<Vector2Int>
            {
                start
            };
        }


        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();


        Dictionary<Vector2Int, Vector2Int>
            cameFrom =
                new Dictionary<
                    Vector2Int,
                    Vector2Int
                >();


        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();


        queue.Enqueue(start);

        visited.Add(start);


        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();


            for (int i = 0;
                 i < Directions.Length;
                 i++)
            {
                Vector2Int next =
                    current +
                    Directions[i];


                if (visited.Contains(next))
                {
                    continue;
                }


                if (!gridManager.IsInsideGrid(
                        next))
                {
                    continue;
                }


                // ==================================================
                // DESTINATION
                // ==================================================

                if (next == destination)
                {
                    cameFrom[next] =
                        current;


                    return ReconstructPath(
                        start,
                        destination,
                        cameFrom
                    );
                }


                // ==================================================
                // OCCUPIED
                // ==================================================

                if (gridManager.IsCellOccupied(
                        next))
                {
                    continue;
                }


                visited.Add(next);


                cameFrom[next] =
                    current;


                queue.Enqueue(next);
            }
        }


        return null;
    }


    // ==================================================
    // RECONSTRUCT PATH
    // ==================================================

    private List<Vector2Int> ReconstructPath(
        Vector2Int start,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int>
            cameFrom)
    {
        List<Vector2Int> path =
            new List<Vector2Int>();


        Vector2Int current =
            destination;


        path.Add(current);


        while (current != start)
        {
            if (!cameFrom.TryGetValue(
                    current,
                    out Vector2Int previous))
            {
                return null;
            }


            current =
                previous;


            path.Add(current);
        }


        path.Reverse();


        return path;
    }


    // ==================================================
    // BEST REACHABLE CELL
    // ==================================================

    private Vector2Int FindBestReachableCell(
        Vector2Int start,
        Vector2Int target)
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();


        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();


        Dictionary<Vector2Int, Vector2Int>
            cameFrom =
                new Dictionary<
                    Vector2Int,
                    Vector2Int
                >();


        queue.Enqueue(start);

        visited.Add(start);


        Vector2Int bestCell =
            start;


        float bestScore =
            CalculateReachableCellScore(
                start,
                target,
                0
            );


        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();


            int pathDistance =
                GetPathDistance(
                    start,
                    current,
                    cameFrom
                );


            float score =
                CalculateReachableCellScore(
                    current,
                    target,
                    pathDistance
                );


            if (score < bestScore)
            {
                bestScore =
                    score;


                bestCell =
                    current;
            }


            for (int i = 0;
                 i < Directions.Length;
                 i++)
            {
                Vector2Int next =
                    current +
                    Directions[i];


                if (visited.Contains(next))
                {
                    continue;
                }


                if (!gridManager.IsInsideGrid(
                        next))
                {
                    continue;
                }


                if (gridManager.IsCellOccupied(
                        next))
                {
                    continue;
                }


                visited.Add(next);


                cameFrom[next] =
                    current;


                queue.Enqueue(next);
            }
        }


        if (bestCell == start)
        {
            MoveDebug(
                "Best reachable cell " +
                "is current cell."
            );


            return start;
        }


        List<Vector2Int> path =
            ReconstructPath(
                start,
                bestCell,
                cameFrom
            );


        if (path == null ||
            path.Count < 2)
        {
            return start;
        }


        Vector2Int firstStep =
            path[1];


        MoveDebug(
            $"Best reachable cell=" +
            $"{bestCell}. " +
            $"FirstStep={firstStep}"
        );


        return firstStep;
    }


    // ==================================================
    // REACHABLE CELL SCORE
    // ==================================================

    private float CalculateReachableCellScore(
        Vector2Int position,
        Vector2Int target,
        int movementCost)
    {
        int distance =
            gridManager.GetDistance(
                position,
                target
            );


        float score =
            distance * 10f;


        score +=
            movementCost * 1f;


        if (preferMoreOpenPositions)
        {
            int openNeighbours =
                CountOpenNeighbours(
                    position
                );


            score -=
                openNeighbours * 2f;
        }


        return score;
    }


    // ==================================================
    // PATH DISTANCE
    // ==================================================

    private int GetPathDistance(
        Vector2Int start,
        Vector2Int current,
        Dictionary<Vector2Int, Vector2Int>
            cameFrom)
    {
        if (current == start)
        {
            return 0;
        }


        int distance =
            0;


        Vector2Int position =
            current;


        while (position != start)
        {
            if (!cameFrom.TryGetValue(
                    position,
                    out Vector2Int previous))
            {
                break;
            }


            position =
                previous;


            distance++;
        }


        return distance;
    }


    // ==================================================
    // OPEN NEIGHBOURS
    // ==================================================

    private int CountOpenNeighbours(
        Vector2Int position)
    {
        int count =
            0;


        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Vector2Int neighbour =
                position +
                Directions[i];


            if (!gridManager.IsInsideGrid(
                    neighbour))
            {
                continue;
            }


            if (!gridManager.IsCellOccupied(
                    neighbour))
            {
                count++;
            }
        }


        return count;
    }


    // ==================================================
    // GRID
    // ==================================================

    private void EnsureGridManager()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<
                    GridManager>();
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


    public bool IsMoving()
    {
        return isMoving;
    }
}