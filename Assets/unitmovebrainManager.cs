using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrainManager : MonoBehaviour
{
    public static UnitMoveBrainManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Directions Settings")]
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

    // Static Memory Allocations & Caches (Shared across units)
    private readonly List<Vector2Int> pathCache = new List<Vector2Int>(32);
    private readonly List<Vector2Int> candidatesCache = new List<Vector2Int>(16);
    private readonly Dictionary<Vector2Int, Vector2Int> cameFromCache = new Dictionary<Vector2Int, Vector2Int>(128);
    private readonly Dictionary<Vector2Int, int> gScoreCache = new Dictionary<Vector2Int, int>(128);
    private readonly HashSet<Vector2Int> visitedCache = new HashSet<Vector2Int>(128);
    private readonly BinaryMinHeap<Vector2Int> openSetCache = new BinaryMinHeap<Vector2Int>(128);
    private readonly Queue<Vector2Int> bfsQueueCache = new Queue<Vector2Int>(64);

    private static AttackUnit[] unitsBuffer = new AttackUnit[64];

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

    // ========================================================================
    // HELPER COMPUTATIONS
    // ========================================================================

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

        return canWalkDiagonally ? Mathf.Max(dx, dy) : dx + dy;
    }

    public int CountOpenNeighbours(Vector2Int position, bool canWalkDiagonally)
    {
        EnsureGridManager();
        if (gridManager == null) return 0;

        int count = 0;
        int directionCount = GetDirectionCount(canWalkDiagonally);

        for (int i = 0; i < directionCount; i++)
        {
            Vector2Int direction = Directions[i];
            Vector2Int neighbour = position + direction;

            if (!gridManager.IsInsideGrid(neighbour) || gridManager.IsCellOccupied(neighbour))
            {
                continue;
            }

            if (IsDiagonalDirection(direction))
            {
                Vector2Int horizontal = new Vector2Int(direction.x, 0);
                Vector2Int vertical = new Vector2Int(0, direction.y);

                if (gridManager.IsCellOccupied(position + horizontal) || gridManager.IsCellOccupied(position + vertical))
                {
                    continue;
                }
            }

            count++;
        }

        return count;
    }

    // ========================================================================
    // TARGET & ATTACK POSITION SELECTION
    // ========================================================================

    public AttackUnit FindBestTarget(
        AttackUnit currentUnit,
        bool preferCloserEnemies,
        bool preferLowHealthEnemies,
        int attackRange,
        bool canWalkDiagonally)
    {
        EnsureGridManager();
        if (gridManager == null || currentUnit == null) return null;

        int unitCount = FindObjectsByType<AttackUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        if (unitsBuffer.Length < unitCount)
        {
            unitsBuffer = new AttackUnit[Mathf.NextPowerOfTwo(unitCount)];
        }

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        AttackUnit bestTarget = null;
        float bestScore = float.MaxValue;

        Vector2Int myPosition = gridManager.WorldToGridPosition(currentUnit.transform.position);

        for (int i = 0; i < allUnits.Length; i++)
        {
            AttackUnit other = allUnits[i];

            if (other == null || other == currentUnit || other.IsDead() || other.GetTeam() == currentUnit.GetTeam())
            {
                continue;
            }

            Vector2Int enemyPosition = gridManager.WorldToGridPosition(other.transform.position);
            int distance = GetMovementDistance(myPosition, enemyPosition, canWalkDiagonally);

            float score = 0f;
            if (preferCloserEnemies) score += distance * 10f;

            HealthManager enemyHealth = other.GetHealthManager();
            if (preferLowHealthEnemies && enemyHealth != null)
            {
                float healthPercent = (float)enemyHealth.GetHealth() / Mathf.Max(1, enemyHealth.GetMaxHealth());
                score += healthPercent * 20f;
            }

            if (distance <= attackRange) score -= 100f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = other;
            }
        }

        return bestTarget;
    }

    public Vector2Int FindBestAttackPosition(
        Vector2Int start,
        Vector2Int target,
        int attackRange,
        bool canWalkDiagonally,
        bool preferCloserAttackPosition,
        bool preferMoreOpenPositions,
        bool preferSidePositions)
    {
        EnsureGridManager();
        GetAttackPositions(target, attackRange, canWalkDiagonally, candidatesCache);

        if (candidatesCache.Count == 0)
        {
            return FindBestReachableCell(start, target, canWalkDiagonally, preferMoreOpenPositions);
        }

        Vector2Int bestPosition = start;
        float bestScore = float.MaxValue;
        bool foundReachablePosition = false;

        for (int i = 0; i < candidatesCache.Count; i++)
        {
            Vector2Int candidate = candidatesCache[i];

            if (!gridManager.IsInsideGrid(candidate)) continue;
            if (candidate != start && gridManager.IsCellOccupied(candidate)) continue;

            if (!FindPath(start, candidate, canWalkDiagonally, pathCache) || pathCache.Count < 2)
            {
                continue;
            }

            foundReachablePosition = true;
            int movementCost = pathCache.Count - 1;
            int distanceToEnemy = GetMovementDistance(candidate, target, canWalkDiagonally);

            float score = movementCost * 10f;
            if (preferCloserAttackPosition) score += distanceToEnemy * 5f;
            if (preferMoreOpenPositions) score -= CountOpenNeighbours(candidate, canWalkDiagonally) * 2f;
            if (preferSidePositions && candidate.x != target.x && candidate.y != target.y) score += 3f;

            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        return foundReachablePosition
            ? bestPosition
            : FindBestReachableCell(start, target, canWalkDiagonally, preferMoreOpenPositions);
    }

    private void GetAttackPositions(Vector2Int target, int attackRange, bool canWalkDiagonally, List<Vector2Int> results)
    {
        results.Clear();

        if (attackRange == 1)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                if (IsDiagonalDirection(Directions[i]) && !canWalkDiagonally) continue;
                results.Add(target + Directions[i]);
            }
            return;
        }

        int minX = Mathf.Max(0, target.x - attackRange);
        int maxX = Mathf.Min(gridManager.GetWidth() - 1, target.x + attackRange);
        int minY = Mathf.Max(0, target.y - attackRange);
        int maxY = Mathf.Min(gridManager.GetHeight() - 1, target.y + attackRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (position == target) continue;

                if (GetMovementDistance(position, target, canWalkDiagonally) <= attackRange)
                {
                    results.Add(position);
                }
            }
        }
    }

    // ========================================================================
    // PATHFINDING (A* & BFS)
    // ========================================================================

    public bool FindPath(Vector2Int start, Vector2Int destination, bool canWalkDiagonally, List<Vector2Int> resultPath)
    {
        EnsureGridManager();
        if (gridManager == null) return false;

        resultPath.Clear();
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

        int directionCount = GetDirectionCount(canWalkDiagonally);

        while (openSetCache.Count > 0)
        {
            Vector2Int current = openSetCache.Dequeue();

            if (current == destination)
            {
                ReconstructPath(start, destination, cameFromCache, resultPath);
                return true;
            }

            visitedCache.Add(current);
            int currentG = gScoreCache[current];

            for (int i = 0; i < directionCount; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (visitedCache.Contains(next) || !gridManager.IsInsideGrid(next)) continue;

                if (IsDiagonalDirection(direction))
                {
                    Vector2Int horizontal = new Vector2Int(direction.x, 0);
                    Vector2Int vertical = new Vector2Int(0, direction.y);

                    if (gridManager.IsCellOccupied(current + horizontal) || gridManager.IsCellOccupied(current + vertical))
                    {
                        continue;
                    }
                }

                if (next != destination && gridManager.IsCellOccupied(next)) continue;

                int tentativeG = currentG + 1;

                if (!gScoreCache.TryGetValue(next, out int nextG) || tentativeG < nextG)
                {
                    cameFromCache[next] = current;
                    gScoreCache[next] = tentativeG;

                    int fScore = tentativeG + GetMovementDistance(next, destination, canWalkDiagonally);
                    openSetCache.EnqueueOrUpdate(next, fScore);
                }
            }
        }

        return false;
    }

    private Vector2Int FindBestReachableCell(Vector2Int start, Vector2Int target, bool canWalkDiagonally, bool preferMoreOpenPositions)
    {
        bfsQueueCache.Clear();
        visitedCache.Clear();
        cameFromCache.Clear();

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);

        Vector2Int bestCell = start;
        float bestScore = CalculateReachableCellScore(start, target, 0, canWalkDiagonally, preferMoreOpenPositions);
        int directionCount = GetDirectionCount(canWalkDiagonally);

        while (bfsQueueCache.Count > 0)
        {
            Vector2Int current = bfsQueueCache.Dequeue();
            int pathDistance = GetPathDistance(start, current, cameFromCache);

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

                if (visitedCache.Contains(next) || !gridManager.IsInsideGrid(next) || gridManager.IsCellOccupied(next))
                {
                    continue;
                }

                if (IsDiagonalDirection(direction))
                {
                    Vector2Int horizontal = new Vector2Int(direction.x, 0);
                    Vector2Int vertical = new Vector2Int(0, direction.y);

                    if (gridManager.IsCellOccupied(current + horizontal) || gridManager.IsCellOccupied(current + vertical))
                    {
                        continue;
                    }
                }

                visitedCache.Add(next);
                cameFromCache[next] = current;
                bfsQueueCache.Enqueue(next);
            }
        }

        if (bestCell == start) return start;

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

    private void ReconstructPath(Vector2Int start, Vector2Int destination, Dictionary<Vector2Int, Vector2Int> cameFrom, List<Vector2Int> path)
    {
        Vector2Int current = destination;
        path.Add(current);

        while (current != start)
        {
            if (!cameFrom.TryGetValue(current, out Vector2Int previous)) break;
            current = previous;
            path.Add(current);
        }

        path.Reverse();
    }

    private int GetPathDistance(Vector2Int start, Vector2Int current, Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        if (current == start) return 0;

        int distance = 0;
        Vector2Int position = current;

        while (position != start)
        {
            if (!cameFrom.TryGetValue(position, out Vector2Int previous)) break;
            position = previous;
            distance++;
        }

        return distance;
    }

    // ========================================================================
    // DATA STRUCTURES
    // ========================================================================

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
            Count = 0;
        }

        public void EnqueueOrUpdate(T item, int priority)
        {
            if (itemIndices.TryGetValue(item, out int existingIndex))
            {
                if (priority < nodes[existingIndex].Priority)
                {
                    nodes[existingIndex].Priority = priority;
                    BubbleUp(existingIndex);
                }
            }
            else
            {
                Enqueue(item, priority);
            }
        }

        public void Enqueue(T item, int priority)
        {
            if (Count >= nodes.Length) Array.Resize(ref nodes, nodes.Length * 2);

            HeapNode node = new HeapNode(item, priority);
            nodes[Count] = node;
            itemIndices[item] = Count;
            BubbleUp(Count);
            Count++;
        }

        public T Dequeue()
        {
            T result = nodes[0].Item;
            itemIndices.Remove(result);

            Count--;
            if (Count > 0)
            {
                nodes[0] = nodes[Count];
                itemIndices[nodes[0].Item] = 0;
                BubbleDown(0);
            }

            return result;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (nodes[index].Priority >= nodes[parent].Priority) break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int smallest = index;
                int left = 2 * index + 1;
                int right = 2 * index + 2;

                if (left < Count && nodes[left].Priority < nodes[smallest].Priority) smallest = left;
                if (right < Count && nodes[right].Priority < nodes[smallest].Priority) smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            HeapNode temp = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = temp;

            itemIndices[nodes[i].Item] = i;
            itemIndices[nodes[j].Item] = j;
        }

        public void Clear()
        {
            Count = 0;
            itemIndices.Clear();
        }
    }
}