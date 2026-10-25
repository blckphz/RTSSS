
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrainManager : MonoBehaviour
{
    public static UnitMoveBrainManager Instance { get; private set; }

    public static bool IsUnitMoving => movingUnitCount > 0;
    private static int movingUnitCount;

    public static void BeginUnitMovement()
    {
        movingUnitCount++;
    }

    public static void EndUnitMovement()
    {
        movingUnitCount = Mathf.Max(0, movingUnitCount - 1);
    }

    public static void ClearUnitMovementState()
    {
        movingUnitCount = 0;
    }

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    private const int CardinalDirectionCount = 4;

    private readonly List<Vector2Int> pathCache = new List<Vector2Int>(32);
    private readonly List<Vector2Int> candidatesCache = new List<Vector2Int>(64);

    private readonly Dictionary<Vector2Int, Vector2Int> cameFromCache =
        new Dictionary<Vector2Int, Vector2Int>(256);

    private readonly Dictionary<Vector2Int, int> distanceCache =
        new Dictionary<Vector2Int, int>(256);

    private readonly HashSet<Vector2Int> visitedCache =
        new HashSet<Vector2Int>(256);

    private readonly Queue<Vector2Int> bfsQueueCache =
        new Queue<Vector2Int>(256);

    private readonly List<AttackUnit> targetUnitBuffer =
        new List<AttackUnit>(64);

    // Caches footprint-movement checks during one BFS traversal.
    // Cleared before each traversal to prevent stale occupancy results.
    private readonly Dictionary<Vector2Int, bool> movingUnitBlockedCache =
        new Dictionary<Vector2Int, bool>(256);

    private GameObject cachedMovingUnit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        movingUnitCount = 0;
        EnsureGridManager();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            movingUnitCount = 0;
        }
    }

    public GridManager GetGridManager()
    {
        EnsureGridManager();
        return gridManager;
    }

    private void EnsureGridManager()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    public int GetDirectionCount(bool canWalkDiagonally)
    {
        return canWalkDiagonally
            ? Directions.Length
            : CardinalDirectionCount;
    }

    public bool IsDiagonalDirection(Vector2Int direction)
    {
        return direction.x != 0 && direction.y != 0;
    }

    public int GetMovementDistance(
        Vector2Int a,
        Vector2Int b,
        bool canWalkDiagonally)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        return canWalkDiagonally
            ? Mathf.Max(dx, dy)
            : dx + dy;
    }

    // ============================================================
    // BLOCKED-CELL CACHE
    // ============================================================

    private void BeginBlockedCellCache(GameObject movingUnit)
    {
        movingUnitBlockedCache.Clear();
        cachedMovingUnit = movingUnit;
    }

    private bool IsBlocked(Vector2Int position, GameObject movingUnit)
    {
        if (gridManager == null)
            return true;

        if (movingUnit == null)
            return gridManager.GetUnitAt(position) != null;

        // If the moving unit changes, discard results from the old unit.
        if (cachedMovingUnit != movingUnit)
            BeginBlockedCellCache(movingUnit);

        if (movingUnitBlockedCache.TryGetValue(position, out bool blocked))
            return blocked;

        blocked = !gridManager.CanMoveToCell(movingUnit, position);
        movingUnitBlockedCache[position] = blocked;

        return blocked;
    }

    // ============================================================
    // REACHABLE CELLS
    // ============================================================

    public void GetReachableCells(
        Vector2Int start,
        int moveRange,
        bool canWalkDiagonally,
        List<Vector2Int> results,
        GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (results == null)
            return;

        results.Clear();

        if (gridManager == null ||
            moveRange <= 0 ||
            !gridManager.IsInsideGrid(start))
        {
            return;
        }

        bfsQueueCache.Clear();
        visitedCache.Clear();
        distanceCache.Clear();
        cameFromCache.Clear();

        BeginBlockedCellCache(movingUnit);

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);
        distanceCache[start] = 0;

        int directionCount = GetDirectionCount(canWalkDiagonally);

        while (bfsQueueCache.Count > 0)
        {
            Vector2Int current = bfsQueueCache.Dequeue();
            int currentDistance = distanceCache[current];

            if (currentDistance >= moveRange)
                continue;

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next))
                    continue;

                if (!gridManager.IsInsideGrid(next))
                    continue;

                if (!CanEnterCell(current, next, direction, movingUnit))
                    continue;

                visitedCache.Add(next);
                distanceCache[next] = currentDistance + 1;
                bfsQueueCache.Enqueue(next);
                results.Add(next);
            }
        }
    }

    private bool CanEnterCell(
        Vector2Int current,
        Vector2Int next,
        Vector2Int direction,
        GameObject movingUnit)
    {
        if (!gridManager.IsInsideGrid(next))
            return false;

        if (IsDiagonalDirection(direction))
        {
            Vector2Int horizontal =
                new Vector2Int(current.x + direction.x, current.y);

            Vector2Int vertical =
                new Vector2Int(current.x, current.y + direction.y);

            if (IsBlocked(horizontal, movingUnit) ||
                IsBlocked(vertical, movingUnit))
            {
                return false;
            }
        }

        return !IsBlocked(next, movingUnit);
    }

    // ============================================================
    // OPEN NEIGHBOURS
    // ============================================================

    public int CountOpenNeighbours(
        Vector2Int position,
        bool canWalkDiagonally)
    {
        EnsureGridManager();

        if (gridManager == null)
            return 0;

        int count = 0;
        int directionCount = GetDirectionCount(canWalkDiagonally);

        for (int i = 0; i < directionCount; i++)
        {
            Vector2Int direction = Directions[i];
            Vector2Int neighbour = position + direction;

            if (!gridManager.IsInsideGrid(neighbour))
                continue;

            if (IsBlocked(neighbour, null))
                continue;

            if (IsDiagonalDirection(direction))
            {
                Vector2Int horizontal =
                    new Vector2Int(position.x + direction.x, position.y);

                Vector2Int vertical =
                    new Vector2Int(position.x, position.y + direction.y);

                if (IsBlocked(horizontal, null) ||
                    IsBlocked(vertical, null))
                {
                    continue;
                }
            }

            count++;
        }

        return count;
    }

    // ============================================================
    // FOOTPRINT DISTANCE
    // ============================================================

    private int GetDistanceBetweenFootprints(
        Vector2Int a,
        GameObject unitA,
        Vector2Int b,
        GameObject unitB,
        bool diagonal)
    {
        int sizeA = gridManager.GetUnitFootprintSize(unitA);
        int sizeB = gridManager.GetUnitFootprintSize(unitB);

        int minAx = a.x - sizeA / 2;
        int minAy = a.y - sizeA / 2;
        int maxAx = minAx + sizeA - 1;
        int maxAy = minAy + sizeA - 1;

        int minBx = b.x - sizeB / 2;
        int minBy = b.y - sizeB / 2;
        int maxBx = minBx + sizeB - 1;
        int maxBy = minBy + sizeB - 1;

        int dx = Mathf.Max(
            0,
            Mathf.Max(minAx - maxBx, minBx - maxAx));

        int dy = Mathf.Max(
            0,
            Mathf.Max(minAy - maxBy, minBy - maxAy));

        return diagonal ? Mathf.Max(dx, dy) : dx + dy;
    }

    // ============================================================
    // TARGET SELECTION
    // ============================================================

    public AttackUnit FindBestTarget(
        AttackUnit currentUnit,
        bool preferCloserEnemies,
        bool preferLowHealthEnemies,
        int attackRange,
        bool canWalkDiagonally)
    {
        EnsureGridManager();

        if (gridManager == null || currentUnit == null)
            return null;

        targetUnitBuffer.Clear();

        targetUnitBuffer.AddRange(
            FindObjectsByType<AttackUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));

        AttackUnit bestTarget = null;
        float bestScore = float.MaxValue;

        Vector2Int myPosition =
            gridManager.WorldToGridPosition(currentUnit.transform.position);

        for (int i = 0; i < targetUnitBuffer.Count; i++)
        {
            AttackUnit other = targetUnitBuffer[i];

            if (other == null ||
                other == currentUnit ||
                other.IsDead() ||
                other.GetTeam() == currentUnit.GetTeam())
            {
                continue;
            }

            if (currentUnit.GetTeam() != Team.Player &&
                other.GetTeam() == Team.Player &&
                BushBehav.IsHidden(other.gameObject))
            {
                continue;
            }

            Vector2Int enemyPosition =
                gridManager.WorldToGridPosition(other.transform.position);

            int distance = GetDistanceBetweenFootprints(
                myPosition,
                currentUnit.gameObject,
                enemyPosition,
                other.gameObject,
                canWalkDiagonally);

            float score = 0f;

            if (preferCloserEnemies)
                score += distance * 10f;

            if (preferLowHealthEnemies)
            {
                HealthManager health = other.GetHealthManager();

                if (health != null)
                {
                    int maxHp = health.GetMaxHealth();

                    float healthPercent =
                        (float)health.GetHealth() /
                        (maxHp > 0 ? maxHp : 1);

                    score += healthPercent * 20f;
                }
            }

            if (distance <= attackRange)
                score -= 100f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = other;
            }
        }

        targetUnitBuffer.Clear();
        return bestTarget;
    }

    // ============================================================
    // BEST ATTACK POSITION
    // ============================================================

    public Vector2Int FindBestAttackPosition(
        Vector2Int start,
        Vector2Int target,
        int attackRange,
        bool canWalkDiagonally,
        bool preferCloserAttackPosition,
        bool preferMoreOpenPositions,
        bool preferSidePositions,
        GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (gridManager == null)
            return start;

        GetAttackPositions(
            target,
            attackRange,
            canWalkDiagonally,
            candidatesCache);

        CalculateReachableDistances(start, canWalkDiagonally, movingUnit);

        Vector2Int bestPosition = start;
        float bestScore = float.MaxValue;
        bool found = false;

        GameObject targetObject = gridManager.GetUnitAt(target);

        int candidateCount = candidatesCache.Count;

        for (int i = 0; i < candidateCount; i++)
        {
            Vector2Int candidate = candidatesCache[i];

            if (candidate != start &&
                gridManager.IsCellOccupied(candidate))
            {
                continue;
            }

            if (!distanceCache.TryGetValue(candidate, out int movementCost))
                continue;

            if (movementCost == 0)
                continue;

            int distanceToEnemy = GetDistanceBetweenFootprints(
                candidate,
                movingUnit,
                target,
                targetObject,
                canWalkDiagonally);

            float score = movementCost * 10f;

            if (preferCloserAttackPosition)
                score += distanceToEnemy * 5f;

            if (preferMoreOpenPositions)
            {
                score -= CountOpenNeighbours(
                    candidate,
                    canWalkDiagonally) * 2f;
            }

            if (preferSidePositions &&
                candidate.x != target.x &&
                candidate.y != target.y)
            {
                score += 3f;
            }

            if (!found || score < bestScore)
            {
                found = true;
                bestScore = score;
                bestPosition = candidate;
            }
        }

        if (found)
            return bestPosition;

        return FindBestReachableCell(
            start,
            target,
            canWalkDiagonally,
            preferMoreOpenPositions,
            movingUnit);
    }

    // ============================================================
    // SINGLE BFS FOR REACHABLE MOVEMENT COSTS
    // ============================================================

    private void CalculateReachableDistances(
        Vector2Int start,
        bool canWalkDiagonally,
        GameObject movingUnit)
    {
        distanceCache.Clear();
        cameFromCache.Clear();
        visitedCache.Clear();
        bfsQueueCache.Clear();

        BeginBlockedCellCache(movingUnit);

        if (!gridManager.IsInsideGrid(start))
            return;

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);
        distanceCache[start] = 0;

        int directionCount = GetDirectionCount(canWalkDiagonally);

        while (bfsQueueCache.Count > 0)
        {
            Vector2Int current = bfsQueueCache.Dequeue();
            int currentDistance = distanceCache[current];

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next))
                    continue;

                if (!gridManager.IsInsideGrid(next))
                    continue;

                if (!CanEnterPathCell(
                    current,
                    next,
                    direction,
                    next,
                    movingUnit))
                {
                    continue;
                }

                visitedCache.Add(next);
                distanceCache[next] = currentDistance + 1;
                cameFromCache[next] = current;
                bfsQueueCache.Enqueue(next);
            }
        }
    }

    // ============================================================
    // ATTACK POSITIONS
    // ============================================================

    private void GetAttackPositions(
        Vector2Int target,
        int attackRange,
        bool canWalkDiagonally,
        List<Vector2Int> results)
    {
        results.Clear();

        if (gridManager == null || attackRange <= 0)
            return;

        GameObject targetObject = gridManager.GetUnitAt(target);
        int targetSize = gridManager.GetUnitFootprintSize(targetObject);

        int targetMinX = target.x - targetSize / 2;
        int targetMinY = target.y - targetSize / 2;
        int targetMaxX = targetMinX + targetSize - 1;
        int targetMaxY = targetMinY + targetSize - 1;

        int minX = Mathf.Max(
            gridManager.GetMinX(),
            targetMinX - attackRange);

        int maxX = Mathf.Min(
            gridManager.GetMaxX(),
            targetMaxX + attackRange);

        int minY = Mathf.Max(
            gridManager.GetMinY(),
            targetMinY - attackRange);

        int maxY = Mathf.Min(
            gridManager.GetMaxY(),
            targetMaxY + attackRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                int dx = Mathf.Max(
                    0,
                    Mathf.Max(targetMinX - x, x - targetMaxX));

                int dy = Mathf.Max(
                    0,
                    Mathf.Max(targetMinY - y, y - targetMaxY));

                int distance = canWalkDiagonally
                    ? Mathf.Max(dx, dy)
                    : dx + dy;

                if (distance > 0 && distance <= attackRange)
                    results.Add(position);
            }
        }
    }

    // ============================================================
    // PATHFINDING
    // ============================================================

    public bool FindPath(
        Vector2Int start,
        Vector2Int destination,
        bool canWalkDiagonally,
        List<Vector2Int> resultPath,
        GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (resultPath == null)
            return false;

        resultPath.Clear();

        if (gridManager == null ||
            !gridManager.IsInsideGrid(start) ||
            !gridManager.IsInsideGrid(destination))
        {
            return false;
        }

        if (start == destination)
        {
            resultPath.Add(start);
            return true;
        }

        CalculateReachableDistances(
            start,
            canWalkDiagonally,
            movingUnit);

        if (!distanceCache.ContainsKey(destination))
            return false;

        ReconstructPath(
            start,
            destination,
            cameFromCache,
            resultPath);

        return resultPath.Count > 0;
    }

    // ============================================================
    // PATH CELL VALIDATION
    // ============================================================

    private bool CanEnterPathCell(
        Vector2Int current,
        Vector2Int next,
        Vector2Int direction,
        Vector2Int destination,
        GameObject movingUnit)
    {
        if (IsDiagonalDirection(direction))
        {
            Vector2Int horizontal =
                new Vector2Int(current.x + direction.x, current.y);

            Vector2Int vertical =
                new Vector2Int(current.x, current.y + direction.y);

            if (IsBlocked(horizontal, movingUnit) ||
                IsBlocked(vertical, movingUnit))
            {
                return false;
            }
        }

        if (movingUnit != null)
            return !IsBlocked(next, movingUnit);

        GameObject occupant = gridManager.GetUnitAt(next);

        return occupant == null || next == destination;
    }

    // ============================================================
    // FALLBACK
    // ============================================================

    private Vector2Int FindBestReachableCell(
        Vector2Int start,
        Vector2Int target,
        bool canWalkDiagonally,
        bool preferMoreOpenPositions,
        GameObject movingUnit)
    {
        CalculateReachableDistances(
            start,
            canWalkDiagonally,
            movingUnit);

        Vector2Int bestCell = start;

        float bestScore = CalculateReachableCellScore(
            start,
            target,
            0,
            canWalkDiagonally,
            preferMoreOpenPositions);

        foreach (KeyValuePair<Vector2Int, int> entry in distanceCache)
        {
            Vector2Int position = entry.Key;
            int movementCost = entry.Value;

            float score = CalculateReachableCellScore(
                position,
                target,
                movementCost,
                canWalkDiagonally,
                preferMoreOpenPositions);

            if (score < bestScore)
            {
                bestScore = score;
                bestCell = position;
            }
        }

        if (bestCell == start)
            return start;

        ReconstructPath(
            start,
            bestCell,
            cameFromCache,
            pathCache);

        return pathCache.Count >= 2
            ? pathCache[1]
            : start;
    }

    private float CalculateReachableCellScore(
        Vector2Int position,
        Vector2Int target,
        int movementCost,
        bool canWalkDiagonally,
        bool preferMoreOpenPositions)
    {
        int distance = GetMovementDistance(
            position,
            target,
            canWalkDiagonally);

        float score = distance * 10f + movementCost;

        if (preferMoreOpenPositions)
        {
            score -= CountOpenNeighbours(
                position,
                canWalkDiagonally) * 2f;
        }

        return score;
    }

    // ============================================================
    // PATH RECONSTRUCTION
    // ============================================================

    private void ReconstructPath(
        Vector2Int start,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        List<Vector2Int> path)
    {
        path.Clear();

        Vector2Int current = destination;
        path.Add(current);

        while (current != start)
        {
            if (!cameFrom.TryGetValue(current, out Vector2Int previous))
            {
                path.Clear();
                return;
            }

            current = previous;
            path.Add(current);
        }

        path.Reverse();
    }
}
