using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrainManager : MonoBehaviour
{
    public static UnitMoveBrainManager Instance { get; private set; }

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

    // ============================================================
    // REUSABLE CACHES & BUFFERS
    // ============================================================

    private readonly List<Vector2Int> pathCache = new List<Vector2Int>(32);
    private readonly List<Vector2Int> candidatesCache = new List<Vector2Int>(64);
    private readonly Dictionary<Vector2Int, Vector2Int> cameFromCache = new Dictionary<Vector2Int, Vector2Int>(256);
    private readonly Dictionary<Vector2Int, int> gScoreCache = new Dictionary<Vector2Int, int>(256);
    private readonly Dictionary<Vector2Int, int> distanceCache = new Dictionary<Vector2Int, int>(256);
    private readonly HashSet<Vector2Int> visitedCache = new HashSet<Vector2Int>(256);
    private readonly Queue<Vector2Int> bfsQueueCache = new Queue<Vector2Int>(256);
    private readonly BinaryMinHeap<Vector2Int> openSetCache = new BinaryMinHeap<Vector2Int>(256);
    private readonly List<AttackUnit> targetUnitBuffer = new List<AttackUnit>(64);

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureGridManager();
    }

    // ============================================================
    // GRID API
    // ============================================================

    public GridManager GetGridManager()
    {
        EnsureGridManager();
        return gridManager;
    }

    private void EnsureGridManager()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }

    // ============================================================
    // DIRECTIONS & DISTANCE
    // ============================================================

    public int GetDirectionCount(bool canWalkDiagonally)
    {
        return canWalkDiagonally ? Directions.Length : CardinalDirectionCount;
    }

    public bool IsDiagonalDirection(Vector2Int direction)
    {
        return direction.x != 0 && direction.y != 0;
    }

    public int GetMovementDistance(Vector2Int a, Vector2Int b, bool canWalkDiagonally)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        return canWalkDiagonally ? (dx > dy ? dx : dy) : dx + dy;
    }

    // ============================================================
    // REACHABLE CELLS
    // ============================================================

    public void GetReachableCells(Vector2Int start, int moveRange, bool canWalkDiagonally, List<Vector2Int> results, GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (gridManager == null || results == null)
            return;

        results.Clear();

        if (moveRange <= 0 || !gridManager.IsInsideGrid(start))
            return;

        bfsQueueCache.Clear();
        visitedCache.Clear();
        distanceCache.Clear();

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);
        distanceCache[start] = 0;

        int directionCount = canWalkDiagonally ? Directions.Length : CardinalDirectionCount;

        while (bfsQueueCache.Count > 0)
        {
            Vector2Int current = bfsQueueCache.Dequeue();
            int currentDistance = distanceCache[current];

            if (currentDistance >= moveRange)
                continue;

            int nextDistance = currentDistance + 1;

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next) || !gridManager.IsInsideGrid(next))
                    continue;

                if (!CanEnterCell(current, next, direction, movingUnit))
                    continue;

                visitedCache.Add(next);
                distanceCache[next] = nextDistance;
                bfsQueueCache.Enqueue(next);

                results.Add(next);
            }
        }
    }

    // ============================================================
    // CELL ENTRY & OCCUPANCY
    // ============================================================

    private bool CanEnterCell(Vector2Int current, Vector2Int next, Vector2Int direction, GameObject movingUnit)
    {
        if (!gridManager.IsInsideGrid(next))
            return false;

        if (IsDiagonalDirection(direction))
        {
            Vector2Int horizontal = new Vector2Int(current.x + direction.x, current.y);
            Vector2Int vertical = new Vector2Int(current.x, current.y + direction.y);

            if (IsBlocked(horizontal, movingUnit) || IsBlocked(vertical, movingUnit))
                return false;
        }

        return !IsBlocked(next, movingUnit);
    }

    private bool IsBlocked(Vector2Int position, GameObject movingUnit)
    {
        GameObject occupant = gridManager.GetUnitAt(position);
        return occupant != null && occupant != movingUnit;
    }

    // ============================================================
    // OPEN NEIGHBOURS
    // ============================================================

    public int CountOpenNeighbours(Vector2Int position, bool canWalkDiagonally)
    {
        EnsureGridManager();

        if (gridManager == null)
            return 0;

        int count = 0;
        int directionCount = canWalkDiagonally ? Directions.Length : CardinalDirectionCount;

        for (int i = 0; i < directionCount; i++)
        {
            Vector2Int direction = Directions[i];
            Vector2Int neighbour = position + direction;

            if (!gridManager.IsInsideGrid(neighbour) || IsBlocked(neighbour, null))
                continue;

            if (IsDiagonalDirection(direction))
            {
                Vector2Int horizontal = new Vector2Int(position.x + direction.x, position.y);
                Vector2Int vertical = new Vector2Int(position.x, position.y + direction.y);

                if (IsBlocked(horizontal, null) || IsBlocked(vertical, null))
                    continue;
            }

            count++;
        }

        return count;
    }

    // ============================================================
    // TARGET SELECTION
    // ============================================================

    public AttackUnit FindBestTarget(AttackUnit currentUnit, bool preferCloserEnemies, bool preferLowHealthEnemies, int attackRange, bool canWalkDiagonally)
    {
        EnsureGridManager();

        if (gridManager == null || currentUnit == null)
            return null;

        targetUnitBuffer.Clear();
        targetUnitBuffer.AddRange(FindObjectsByType<AttackUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

        AttackUnit bestTarget = null;
        float bestScore = float.MaxValue;

        Vector2Int myPosition = gridManager.WorldToGridPosition(currentUnit.transform.position);
        int targetCount = targetUnitBuffer.Count;

        for (int i = 0; i < targetCount; i++)
        {
            AttackUnit other = targetUnitBuffer[i];

            if (other == null || other == currentUnit || other.IsDead() || other.GetTeam() == currentUnit.GetTeam())
                continue;

            Vector2Int enemyPosition = gridManager.WorldToGridPosition(other.transform.position);
            int distance = GetMovementDistance(myPosition, enemyPosition, canWalkDiagonally);

            float score = 0f;

            if (preferCloserEnemies)
            {
                score += distance * 10f;
            }

            if (preferLowHealthEnemies)
            {
                HealthManager health = other.GetHealthManager();
                if (health != null)
                {
                    int maxHp = health.GetMaxHealth();
                    float healthPercent = (float)health.GetHealth() / (maxHp > 0 ? maxHp : 1);
                    score += healthPercent * 20f;
                }
            }

            if (distance <= attackRange)
            {
                score -= 100f;
            }

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

    public Vector2Int FindBestAttackPosition(Vector2Int start, Vector2Int target, int attackRange, bool canWalkDiagonally, bool preferCloserAttackPosition, bool preferMoreOpenPositions, bool preferSidePositions)
    {
        EnsureGridManager();

        if (gridManager == null)
            return start;

        GetAttackPositions(target, attackRange, canWalkDiagonally, candidatesCache);

        Vector2Int bestPosition = start;
        float bestScore = float.MaxValue;
        bool found = false;

        int candidateCount = candidatesCache.Count;
        for (int i = 0; i < candidateCount; i++)
        {
            Vector2Int candidate = candidatesCache[i];

            if (candidate != start && gridManager.IsCellOccupied(candidate))
                continue;

            if (!FindPath(start, candidate, canWalkDiagonally, pathCache))
                continue;

            if (pathCache.Count < 2)
                continue;

            int movementCost = pathCache.Count - 1;
            int distanceToEnemy = GetMovementDistance(candidate, target, canWalkDiagonally);

            float score = movementCost * 10f;

            if (preferCloserAttackPosition)
            {
                score += distanceToEnemy * 5f;
            }

            if (preferMoreOpenPositions)
            {
                score -= CountOpenNeighbours(candidate, canWalkDiagonally) * 2f;
            }

            if (preferSidePositions && candidate.x != target.x && candidate.y != target.y)
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

        return FindBestReachableCell(start, target, canWalkDiagonally, preferMoreOpenPositions);
    }

    // ============================================================
    // ATTACK POSITIONS CALCULATOR
    // ============================================================

    private void GetAttackPositions(Vector2Int target, int attackRange, bool canWalkDiagonally, List<Vector2Int> results)
    {
        results.Clear();

        if (gridManager == null || attackRange <= 0)
            return;

        int minX = Mathf.Max(gridManager.GetMinX(), target.x - attackRange);
        int maxX = Mathf.Min(gridManager.GetMaxX(), target.x + attackRange);
        int minY = Mathf.Max(gridManager.GetMinY(), target.y - attackRange);
        int maxY = Mathf.Min(gridManager.GetMaxY(), target.y + attackRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x == target.x && y == target.y)
                    continue;

                Vector2Int position = new Vector2Int(x, y);

                if (GetMovementDistance(position, target, canWalkDiagonally) <= attackRange)
                {
                    results.Add(position);
                }
            }
        }
    }

    // ============================================================
    // PATHFINDING (A*)
    // ============================================================

    public bool FindPath(Vector2Int start, Vector2Int destination, bool canWalkDiagonally, List<Vector2Int> resultPath, GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (gridManager == null || resultPath == null)
            return false;

        resultPath.Clear();

        if (!gridManager.IsInsideGrid(start) || !gridManager.IsInsideGrid(destination))
            return false;

        if (start == destination)
        {
            resultPath.Add(start);
            return true;
        }

        openSetCache.Clear();
        visitedCache.Clear();
        cameFromCache.Clear();
        gScoreCache.Clear();

        gScoreCache[start] = 0;
        openSetCache.Enqueue(start, GetMovementDistance(start, destination, canWalkDiagonally));

        int directionCount = canWalkDiagonally ? Directions.Length : CardinalDirectionCount;

        while (openSetCache.Count > 0)
        {
            Vector2Int current = openSetCache.Dequeue();

            if (visitedCache.Contains(current))
                continue;

            if (current == destination)
            {
                ReconstructPath(start, destination, cameFromCache, resultPath);
                return resultPath.Count > 0;
            }

            visitedCache.Add(current);
            int currentG = gScoreCache[current];

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next) || !gridManager.IsInsideGrid(next))
                    continue;

                if (!CanEnterPathCell(current, next, direction, destination, movingUnit))
                    continue;

                int tentativeG = currentG + 1;

                if (gScoreCache.TryGetValue(next, out int oldG) && tentativeG >= oldG)
                    continue;

                cameFromCache[next] = current;
                gScoreCache[next] = tentativeG;

                int heuristic = GetMovementDistance(next, destination, canWalkDiagonally);
                openSetCache.EnqueueOrUpdate(next, tentativeG + heuristic);
            }
        }

        return false;
    }

    private bool CanEnterPathCell(Vector2Int current, Vector2Int next, Vector2Int direction, Vector2Int destination, GameObject movingUnit)
    {
        if (IsDiagonalDirection(direction))
        {
            Vector2Int horizontal = new Vector2Int(current.x + direction.x, current.y);
            Vector2Int vertical = new Vector2Int(current.x, current.y + direction.y);

            if (IsBlocked(horizontal, movingUnit) || IsBlocked(vertical, movingUnit))
                return false;
        }

        GameObject occupant = gridManager.GetUnitAt(next);
        if (occupant != null && occupant != movingUnit && next != destination)
        {
            return false;
        }

        return true;
    }

    // ============================================================
    // FALLBACK PATHFINDING (BFS)
    // ============================================================

    private Vector2Int FindBestReachableCell(Vector2Int start, Vector2Int target, bool canWalkDiagonally, bool preferMoreOpenPositions)
    {
        bfsQueueCache.Clear();
        visitedCache.Clear();
        cameFromCache.Clear();
        distanceCache.Clear();

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);
        distanceCache[start] = 0;

        Vector2Int bestCell = start;
        float bestScore = CalculateReachableCellScore(start, target, 0, canWalkDiagonally, preferMoreOpenPositions);

        int directionCount = canWalkDiagonally ? Directions.Length : CardinalDirectionCount;

        while (bfsQueueCache.Count > 0)
        {
            Vector2Int current = bfsQueueCache.Dequeue();
            int pathDistance = distanceCache[current];

            float score = CalculateReachableCellScore(current, target, pathDistance, canWalkDiagonally, preferMoreOpenPositions);

            if (score < bestScore)
            {
                bestScore = score;
                bestCell = current;
            }

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next) || !gridManager.IsInsideGrid(next))
                    continue;

                if (!CanEnterCell(current, next, direction, null))
                    continue;

                visitedCache.Add(next);
                cameFromCache[next] = current;
                distanceCache[next] = pathDistance + 1;

                bfsQueueCache.Enqueue(next);
            }
        }

        if (bestCell == start)
            return start;

        ReconstructPath(start, bestCell, cameFromCache, pathCache);

        return pathCache.Count >= 2 ? pathCache[1] : start;
    }

    private float CalculateReachableCellScore(Vector2Int position, Vector2Int target, int movementCost, bool canWalkDiagonally, bool preferMoreOpenPositions)
    {
        int distance = GetMovementDistance(position, target, canWalkDiagonally);
        float score = distance * 10f + movementCost;

        if (preferMoreOpenPositions)
        {
            score -= CountOpenNeighbours(position, canWalkDiagonally) * 2f;
        }

        return score;
    }

    // ============================================================
    // PATH RECONSTRUCTION
    // ============================================================

    private void ReconstructPath(Vector2Int start, Vector2Int destination, Dictionary<Vector2Int, Vector2Int> cameFrom, List<Vector2Int> path)
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

    // ============================================================
    // BINARY MIN HEAP DATA STRUCTURE
    // ============================================================

    private class BinaryMinHeap<T>
    {
        private struct HeapNode
        {
            public T Item;
            public int Priority;

            public HeapNode(T item, int priority)
            {
                Item = item;
                Priority = priority;
            }
        }

        private HeapNode[] nodes;
        private readonly Dictionary<T, int> itemIndices;

        public int Count { get; private set; }

        public BinaryMinHeap(int capacity = 64)
        {
            nodes = new HeapNode[capacity];
            itemIndices = new Dictionary<T, int>(capacity);
        }

        public void Clear()
        {
            Count = 0;
            itemIndices.Clear();
        }

        public void Enqueue(T item, int priority)
        {
            if (itemIndices.ContainsKey(item))
            {
                EnqueueOrUpdate(item, priority);
                return;
            }

            EnsureCapacity();

            int index = Count++;
            nodes[index] = new HeapNode(item, priority);
            itemIndices[item] = index;

            BubbleUp(index);
        }

        public void EnqueueOrUpdate(T item, int priority)
        {
            if (itemIndices.TryGetValue(item, out int index))
            {
                if (priority >= nodes[index].Priority)
                    return;

                nodes[index].Priority = priority;
                BubbleUp(index);
                return;
            }

            Enqueue(item, priority);
        }

        public T Dequeue()
        {
            if (Count == 0)
                return default;

            T result = nodes[0].Item;
            itemIndices.Remove(result);

            Count--;
            if (Count == 0)
                return result;

            nodes[0] = nodes[Count];
            itemIndices[nodes[0].Item] = 0;

            BubbleDown(0);

            return result;
        }

        private void EnsureCapacity()
        {
            if (Count < nodes.Length)
                return;

            int newCapacity = Mathf.Max(nodes.Length * 2, 4);
            Array.Resize(ref nodes, newCapacity);
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;

                if (nodes[parent].Priority <= nodes[index].Priority)
                    break;

                Swap(parent, index);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = (index << 1) + 1;
                if (left >= Count)
                    break;

                int right = left + 1;
                int smallest = (right < Count && nodes[right].Priority < nodes[left].Priority) ? right : left;

                if (nodes[index].Priority <= nodes[smallest].Priority)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            HeapNode temp = nodes[a];
            nodes[a] = nodes[b];
            nodes[b] = temp;

            itemIndices[nodes[a].Item] = a;
            itemIndices[nodes[b].Item] = b;
        }
    }
}