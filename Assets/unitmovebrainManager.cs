using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrainManager : MonoBehaviour
{
    public static UnitMoveBrainManager Instance { get; private set; }


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private GridManager gridManager;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugLogs = true;

    [SerializeField]
    private bool logGridBounds = true;

    [SerializeField]
    private bool logWorldToGrid = true;

    [SerializeField]
    private bool logTargetSelection = true;

    [SerializeField]
    private bool logAttackPositions = true;

    [SerializeField]
    private bool logPathfinding = true;

    [SerializeField]
    private bool logOccupiedCells = false;

    [SerializeField]
    private bool logDiagonalChecks = false;


    // ============================================================
    // DIRECTIONS
    // ============================================================

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
    // CACHES
    // ============================================================

    private readonly List<Vector2Int> pathCache =
        new List<Vector2Int>(32);

    private readonly List<Vector2Int> candidatesCache =
        new List<Vector2Int>(64);

    private readonly Dictionary<Vector2Int, Vector2Int> cameFromCache =
        new Dictionary<Vector2Int, Vector2Int>(128);

    private readonly Dictionary<Vector2Int, int> gScoreCache =
        new Dictionary<Vector2Int, int>(128);

    private readonly HashSet<Vector2Int> visitedCache =
        new HashSet<Vector2Int>(128);

    private readonly BinaryMinHeap<Vector2Int> openSetCache =
        new BinaryMinHeap<Vector2Int>(128);

    private readonly Queue<Vector2Int> bfsQueueCache =
        new Queue<Vector2Int>(128);


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureGridManager();

        if (enableDebugLogs)
        {
            Debug.Log(
                "[UnitMoveBrainManager] Awake completed.",
                this
            );
        }

        DebugGridInformation();
    }


    // ============================================================
    // GRID
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
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogError(
                "[UnitMoveBrainManager] GridManager is NULL.",
                this
            );
        }
    }


    // ============================================================
    // DEBUG GRID
    // ============================================================

    [ContextMenu("Debug Grid Information")]
    public void DebugGridInformation()
    {
        if (
            !enableDebugLogs &&
            !logGridBounds
        )
        {
            return;
        }

        EnsureGridManager();

        if (gridManager == null)
        {
            return;
        }

        Debug.Log(
            "[UnitMoveBrainManager] GRID INFORMATION\n" +
            "Min X: " +
            gridManager.GetMinX() +
            "\n" +
            "Max X: " +
            gridManager.GetMaxX() +
            "\n" +
            "Min Y: " +
            gridManager.GetMinY() +
            "\n" +
            "Max Y: " +
            gridManager.GetMaxY() +
            "\n" +
            "Width: " +
            gridManager.GetWidth() +
            "\n" +
            "Height: " +
            gridManager.GetHeight(),
            this
        );
    }


    // ============================================================
    // WORLD -> GRID DEBUG
    // ============================================================

    public Vector2Int DebugWorldToGrid(
        GameObject unit)
    {
        EnsureGridManager();

        if (
            gridManager == null ||
            unit == null
        )
        {
            return Vector2Int.zero;
        }

        Vector2Int result =
            gridManager.WorldToGridPosition(
                unit.transform.position
            );

        Debug.Log(
            "[UnitMoveBrainManager] WORLD -> GRID\n" +
            "Unit: " +
            unit.name +
            "\nWorld: " +
            unit.transform.position +
            "\nGrid: " +
            result +
            "\nInside: " +
            gridManager.IsInsideGrid(result),
            unit
        );

        return result;
    }


    // ============================================================
    // DIRECTIONS
    // ============================================================

    public int GetDirectionCount(
        bool canWalkDiagonally)
    {
        return canWalkDiagonally
            ? Directions.Length
            : CardinalDirectionCount;
    }


    public bool IsDiagonalDirection(
        Vector2Int direction)
    {
        return
            direction.x != 0 &&
            direction.y != 0;
    }


    // ============================================================
    // MOVEMENT DISTANCE
    // ============================================================

    public int GetMovementDistance(
        Vector2Int a,
        Vector2Int b,
        bool canWalkDiagonally)
    {
        int dx =
            Mathf.Abs(
                a.x - b.x
            );

        int dy =
            Mathf.Abs(
                a.y - b.y
            );

        if (canWalkDiagonally)
        {
            return Mathf.Max(
                dx,
                dy
            );
        }

        return dx + dy;
    }


    // ============================================================
    // GET ALL REACHABLE CELLS
    // ============================================================

    public void GetReachableCells(
        Vector2Int start,
        int moveRange,
        bool canWalkDiagonally,
        List<Vector2Int> results,
        GameObject movingUnit = null)
    {
        EnsureGridManager();

        if (
            gridManager == null ||
            results == null
        )
        {
            return;
        }

        results.Clear();

        if (
            moveRange <= 0 ||
            !gridManager.IsInsideGrid(start)
        )
        {
            return;
        }

        bfsQueueCache.Clear();
        visitedCache.Clear();

        Dictionary<Vector2Int, int> distances =
            new Dictionary<Vector2Int, int>();

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);
        distances[start] = 0;

        int directionCount =
            GetDirectionCount(
                canWalkDiagonally
            );

        while (
            bfsQueueCache.Count > 0
        )
        {
            Vector2Int current =
                bfsQueueCache.Dequeue();

            int currentDistance =
                distances[current];

            if (
                currentDistance >=
                moveRange
            )
            {
                continue;
            }

            for (
                int i = 0;
                i < directionCount;
                i++
            )
            {
                Vector2Int direction =
                    Directions[i];

                Vector2Int next =
                    current + direction;

                if (
                    visitedCache.Contains(next)
                )
                {
                    continue;
                }

                if (
                    !gridManager.IsInsideGrid(next)
                )
                {
                    continue;
                }

                if (
                    !CanEnterCell(
                        current,
                        next,
                        direction,
                        canWalkDiagonally,
                        movingUnit
                    )
                )
                {
                    continue;
                }

                int nextDistance =
                    currentDistance + 1;

                if (
                    nextDistance >
                    moveRange
                )
                {
                    continue;
                }

                visitedCache.Add(next);

                distances[next] =
                    nextDistance;

                bfsQueueCache.Enqueue(next);

                // IMPORTANT:
                // Only actual movement destinations are returned.
                // The start tile is NOT added.
                results.Add(next);
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log(
                "[UnitMoveBrainManager] REACHABLE CELLS\n" +
                "Start: " +
                start +
                "\n" +
                "Move Range: " +
                moveRange +
                "\n" +
                "Diagonal: " +
                canWalkDiagonally +
                "\n" +
                "Reachable Count: " +
                results.Count,
                this
            );
        }
    }


    // ============================================================
    // CELL ENTRY VALIDATION
    // ============================================================

    private bool CanEnterCell(
        Vector2Int current,
        Vector2Int next,
        Vector2Int direction,
        bool canWalkDiagonally,
        GameObject movingUnit)
    {
        if (
            !gridManager.IsInsideGrid(next)
        )
        {
            return false;
        }

        if (
            IsDiagonalDirection(direction)
        )
        {
            Vector2Int horizontal =
                current +
                new Vector2Int(
                    direction.x,
                    0
                );

            Vector2Int vertical =
                current +
                new Vector2Int(
                    0,
                    direction.y
                );

            GameObject horizontalUnit =
                gridManager.GetUnitAt(
                    horizontal
                );

            GameObject verticalUnit =
                gridManager.GetUnitAt(
                    vertical
                );

            if (
                horizontalUnit != null &&
                horizontalUnit != movingUnit
            )
            {
                return false;
            }

            if (
                verticalUnit != null &&
                verticalUnit != movingUnit
            )
            {
                return false;
            }
        }

        GameObject occupant =
            gridManager.GetUnitAt(next);

        if (
            occupant != null &&
            occupant != movingUnit
        )
        {
            return false;
        }

        return true;
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
        {
            return 0;
        }

        int count = 0;

        int directionCount =
            GetDirectionCount(
                canWalkDiagonally
            );

        for (
            int i = 0;
            i < directionCount;
            i++
        )
        {
            Vector2Int direction =
                Directions[i];

            Vector2Int neighbour =
                position + direction;

            if (
                !gridManager.IsInsideGrid(
                    neighbour
                )
            )
            {
                continue;
            }

            if (
                gridManager.IsCellOccupied(
                    neighbour
                )
            )
            {
                continue;
            }

            if (
                IsDiagonalDirection(
                    direction
                )
            )
            {
                Vector2Int horizontal =
                    position +
                    new Vector2Int(
                        direction.x,
                        0
                    );

                Vector2Int vertical =
                    position +
                    new Vector2Int(
                        0,
                        direction.y
                    );

                if (
                    gridManager.IsCellOccupied(
                        horizontal
                    ) ||
                    gridManager.IsCellOccupied(
                        vertical
                    )
                )
                {
                    continue;
                }
            }

            count++;
        }

        return count;
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

        if (
            gridManager == null ||
            currentUnit == null
        )
        {
            return null;
        }

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        AttackUnit bestTarget = null;

        float bestScore =
            float.MaxValue;

        Vector2Int myPosition =
            gridManager.WorldToGridPosition(
                currentUnit.transform.position
            );

        for (
            int i = 0;
            i < allUnits.Length;
            i++
        )
        {
            AttackUnit other =
                allUnits[i];

            if (
                other == null ||
                other == currentUnit ||
                other.IsDead() ||
                other.GetTeam() ==
                currentUnit.GetTeam()
            )
            {
                continue;
            }

            Vector2Int enemyPosition =
                gridManager.WorldToGridPosition(
                    other.transform.position
                );

            int distance =
                GetMovementDistance(
                    myPosition,
                    enemyPosition,
                    canWalkDiagonally
                );

            float score = 0f;

            if (preferCloserEnemies)
            {
                score +=
                    distance * 10f;
            }

            HealthManager health =
                other.GetHealthManager();

            if (
                preferLowHealthEnemies &&
                health != null
            )
            {
                float healthPercent =
                    (float)
                    health.GetHealth() /
                    Mathf.Max(
                        1,
                        health.GetMaxHealth()
                    );

                score +=
                    healthPercent * 20f;
            }

            if (
                distance <= attackRange
            )
            {
                score -= 100f;
            }

            if (
                score < bestScore
            )
            {
                bestScore =
                    score;

                bestTarget =
                    other;
            }
        }

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
        bool preferSidePositions)
    {
        EnsureGridManager();

        if (gridManager == null)
        {
            return start;
        }

        GetAttackPositions(
            target,
            attackRange,
            canWalkDiagonally,
            candidatesCache
        );

        Vector2Int bestPosition =
            start;

        float bestScore =
            float.MaxValue;

        bool found =
            false;

        for (
            int i = 0;
            i < candidatesCache.Count;
            i++
        )
        {
            Vector2Int candidate =
                candidatesCache[i];

            if (
                candidate != start &&
                gridManager.IsCellOccupied(
                    candidate
                )
            )
            {
                continue;
            }

            if (
                !FindPath(
                    start,
                    candidate,
                    canWalkDiagonally,
                    pathCache
                )
            )
            {
                continue;
            }

            if (
                pathCache.Count < 2
            )
            {
                continue;
            }

            int movementCost =
                pathCache.Count - 1;

            int distanceToEnemy =
                GetMovementDistance(
                    candidate,
                    target,
                    canWalkDiagonally
                );

            float score =
                movementCost * 10f;

            if (preferCloserAttackPosition)
            {
                score +=
                    distanceToEnemy * 5f;
            }

            if (preferMoreOpenPositions)
            {
                score -=
                    CountOpenNeighbours(
                        candidate,
                        canWalkDiagonally
                    ) * 2f;
            }

            if (
                preferSidePositions &&
                candidate.x != target.x &&
                candidate.y != target.y
            )
            {
                score += 3f;
            }

            if (
                !found ||
                score < bestScore
            )
            {
                found = true;

                bestScore =
                    score;

                bestPosition =
                    candidate;
            }
        }

        if (found)
        {
            return bestPosition;
        }

        return
            FindBestReachableCell(
                start,
                target,
                canWalkDiagonally,
                preferMoreOpenPositions
            );
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

        if (
            gridManager == null ||
            attackRange <= 0
        )
        {
            return;
        }

        int minX =
            Mathf.Max(
                gridManager.GetMinX(),
                target.x - attackRange
            );

        int maxX =
            Mathf.Min(
                gridManager.GetMaxX(),
                target.x + attackRange
            );

        int minY =
            Mathf.Max(
                gridManager.GetMinY(),
                target.y - attackRange
            );

        int maxY =
            Mathf.Min(
                gridManager.GetMaxY(),
                target.y + attackRange
            );

        for (
            int x = minX;
            x <= maxX;
            x++
        )
        {
            for (
                int y = minY;
                y <= maxY;
                y++
            )
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
                    GetMovementDistance(
                        position,
                        target,
                        canWalkDiagonally
                    );

                if (
                    distance >
                    attackRange
                )
                {
                    continue;
                }

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

        if (
            gridManager == null ||
            resultPath == null
        )
        {
            return false;
        }

        resultPath.Clear();

        if (
            !gridManager.IsInsideGrid(start) ||
            !gridManager.IsInsideGrid(destination)
        )
        {
            return false;
        }

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

        openSetCache.Enqueue(
            start,
            GetMovementDistance(
                start,
                destination,
                canWalkDiagonally
            )
        );

        int directionCount =
            GetDirectionCount(
                canWalkDiagonally
            );

        while (
            openSetCache.Count > 0
        )
        {
            Vector2Int current =
                openSetCache.Dequeue();

            if (
                visitedCache.Contains(
                    current
                )
            )
            {
                continue;
            }

            if (
                current == destination
            )
            {
                ReconstructPath(
                    start,
                    destination,
                    cameFromCache,
                    resultPath
                );

                return
                    resultPath.Count > 0;
            }

            visitedCache.Add(current);

            int currentG =
                gScoreCache[current];

            for (
                int i = 0;
                i < directionCount;
                i++
            )
            {
                Vector2Int direction =
                    Directions[i];

                Vector2Int next =
                    current + direction;

                if (
                    visitedCache.Contains(
                        next
                    )
                )
                {
                    continue;
                }

                if (
                    !gridManager.IsInsideGrid(
                        next
                    )
                )
                {
                    continue;
                }

                if (
                    IsDiagonalDirection(
                        direction
                    )
                )
                {
                    Vector2Int horizontal =
                        current +
                        new Vector2Int(
                            direction.x,
                            0
                        );

                    Vector2Int vertical =
                        current +
                        new Vector2Int(
                            0,
                            direction.y
                        );

                    GameObject horizontalUnit =
                        gridManager.GetUnitAt(
                            horizontal
                        );

                    GameObject verticalUnit =
                        gridManager.GetUnitAt(
                            vertical
                        );

                    if (
                        horizontalUnit != null &&
                        horizontalUnit != movingUnit
                    )
                    {
                        continue;
                    }

                    if (
                        verticalUnit != null &&
                        verticalUnit != movingUnit
                    )
                    {
                        continue;
                    }
                }

                GameObject occupant =
                    gridManager.GetUnitAt(
                        next
                    );

                if (
                    occupant != null &&
                    occupant != movingUnit
                )
                {
                    if (
                        next != destination
                    )
                    {
                        continue;
                    }
                }

                int tentativeG =
                    currentG + 1;

                if (
                    !gScoreCache.TryGetValue(
                        next,
                        out int oldG
                    ) ||
                    tentativeG < oldG
                )
                {
                    cameFromCache[next] =
                        current;

                    gScoreCache[next] =
                        tentativeG;

                    int fScore =
                        tentativeG +
                        GetMovementDistance(
                            next,
                            destination,
                            canWalkDiagonally
                        );

                    openSetCache.EnqueueOrUpdate(
                        next,
                        fScore
                    );
                }
            }
        }

        return false;
    }


    // ============================================================
    // FALLBACK
    // ============================================================

    private Vector2Int FindBestReachableCell(
        Vector2Int start,
        Vector2Int target,
        bool canWalkDiagonally,
        bool preferMoreOpenPositions)
    {
        bfsQueueCache.Clear();
        visitedCache.Clear();
        cameFromCache.Clear();

        bfsQueueCache.Enqueue(start);
        visitedCache.Add(start);

        Vector2Int bestCell =
            start;

        float bestScore =
            CalculateReachableCellScore(
                start,
                target,
                0,
                canWalkDiagonally,
                preferMoreOpenPositions
            );

        int directionCount =
            GetDirectionCount(
                canWalkDiagonally
            );

        Dictionary<Vector2Int, int> distances =
            new Dictionary<Vector2Int, int>();

        distances[start] = 0;

        while (
            bfsQueueCache.Count > 0
        )
        {
            Vector2Int current =
                bfsQueueCache.Dequeue();

            int pathDistance =
                distances[current];

            float score =
                CalculateReachableCellScore(
                    current,
                    target,
                    pathDistance,
                    canWalkDiagonally,
                    preferMoreOpenPositions
                );

            if (
                score < bestScore
            )
            {
                bestScore =
                    score;

                bestCell =
                    current;
            }

            for (
                int i = 0;
                i < directionCount;
                i++
            )
            {
                Vector2Int direction =
                    Directions[i];

                Vector2Int next =
                    current + direction;

                if (
                    visitedCache.Contains(next)
                )
                {
                    continue;
                }

                if (
                    !gridManager.IsInsideGrid(next)
                )
                {
                    continue;
                }

                if (
                    gridManager.IsCellOccupied(
                        next
                    )
                )
                {
                    continue;
                }

                if (
                    IsDiagonalDirection(
                        direction
                    )
                )
                {
                    Vector2Int horizontal =
                        current +
                        new Vector2Int(
                            direction.x,
                            0
                        );

                    Vector2Int vertical =
                        current +
                        new Vector2Int(
                            0,
                            direction.y
                        );

                    if (
                        gridManager.IsCellOccupied(
                            horizontal
                        ) ||
                        gridManager.IsCellOccupied(
                            vertical
                        )
                    )
                    {
                        continue;
                    }
                }

                visitedCache.Add(next);

                cameFromCache[next] =
                    current;

                distances[next] =
                    pathDistance + 1;

                bfsQueueCache.Enqueue(next);
            }
        }

        if (bestCell == start)
        {
            return start;
        }

        ReconstructPath(
            start,
            bestCell,
            cameFromCache,
            pathCache
        );

        if (pathCache.Count >= 2)
        {
            return pathCache[1];
        }

        return start;
    }


    // ============================================================
    // REACHABLE CELL SCORE
    // ============================================================

    private float CalculateReachableCellScore(
        Vector2Int position,
        Vector2Int target,
        int movementCost,
        bool canWalkDiagonally,
        bool preferMoreOpenPositions)
    {
        int distance =
            GetMovementDistance(
                position,
                target,
                canWalkDiagonally
            );

        float score =
            distance * 10f +
            movementCost;

        if (preferMoreOpenPositions)
        {
            score -=
                CountOpenNeighbours(
                    position,
                    canWalkDiagonally
                ) * 2f;
        }

        return score;
    }


    // ============================================================
    // RECONSTRUCT PATH
    // ============================================================

    private void ReconstructPath(
        Vector2Int start,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        List<Vector2Int> path)
    {
        path.Clear();

        Vector2Int current =
            destination;

        path.Add(current);

        while (
            current != start
        )
        {
            if (
                !cameFrom.TryGetValue(
                    current,
                    out Vector2Int previous
                )
            )
            {
                path.Clear();
                return;
            }

            current =
                previous;

            path.Add(current);
        }

        path.Reverse();
    }


    // ============================================================
    // PATH FORMAT
    // ============================================================

    private string FormatPath(
        List<Vector2Int> path)
    {
        if (
            path == null ||
            path.Count == 0
        )
        {
            return "EMPTY";
        }

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        for (
            int i = 0;
            i < path.Count;
            i++
        )
        {
            if (i > 0)
            {
                builder.Append(" -> ");
            }

            builder.Append(
                path[i]
            );
        }

        return builder.ToString();
    }


    // ============================================================
    // BINARY MIN HEAP
    // ============================================================

    private class BinaryMinHeap<T>
    {
        private struct HeapNode
        {
            public T Item;
            public int Priority;

            public HeapNode(
                T item,
                int priority)
            {
                Item = item;
                Priority = priority;
            }
        }

        private HeapNode[] nodes;

        private readonly Dictionary<T, int>
            itemIndices;

        public int Count
        {
            get;
            private set;
        }


        public BinaryMinHeap(
            int capacity = 64)
        {
            nodes =
                new HeapNode[
                    Mathf.Max(
                        1,
                        capacity
                    )
                ];

            itemIndices =
                new Dictionary<T, int>(
                    capacity
                );

            Count = 0;
        }


        public void Clear()
        {
            Count = 0;
            itemIndices.Clear();
        }


        public void EnqueueOrUpdate(
            T item,
            int priority)
        {
            if (
                itemIndices.TryGetValue(
                    item,
                    out int existingIndex
                )
            )
            {
                if (
                    priority <
                    nodes[
                        existingIndex
                    ].Priority
                )
                {
                    nodes[
                        existingIndex
                    ].Priority =
                        priority;

                    BubbleUp(
                        existingIndex
                    );
                }

                return;
            }

            Enqueue(
                item,
                priority
            );
        }


        public void Enqueue(
            T item,
            int priority)
        {
            if (
                Count >=
                nodes.Length
            )
            {
                Array.Resize(
                    ref nodes,
                    nodes.Length * 2
                );
            }

            nodes[Count] =
                new HeapNode(
                    item,
                    priority
                );

            itemIndices[item] =
                Count;

            BubbleUp(Count);

            Count++;
        }


        public T Dequeue()
        {
            T result =
                nodes[0].Item;

            itemIndices.Remove(
                result
            );

            Count--;

            if (Count > 0)
            {
                nodes[0] =
                    nodes[Count];

                itemIndices[
                    nodes[0].Item
                ] = 0;

                BubbleDown(0);
            }

            return result;
        }


        private void BubbleUp(
            int index)
        {
            while (
                index > 0
            )
            {
                int parent =
                    (index - 1) / 2;

                if (
                    nodes[index].Priority >=
                    nodes[parent].Priority
                )
                {
                    break;
                }

                Swap(
                    index,
                    parent
                );

                index =
                    parent;
            }
        }


        private void BubbleDown(
            int index)
        {
            while (true)
            {
                int smallest =
                    index;

                int left =
                    index * 2 + 1;

                int right =
                    index * 2 + 2;

                if (
                    left < Count &&
                    nodes[left].Priority <
                    nodes[smallest].Priority
                )
                {
                    smallest =
                        left;
                }

                if (
                    right < Count &&
                    nodes[right].Priority <
                    nodes[smallest].Priority
                )
                {
                    smallest =
                        right;
                }

                if (
                    smallest == index
                )
                {
                    break;
                }

                Swap(
                    index,
                    smallest
                );

                index =
                    smallest;
            }
        }


        private void Swap(
            int a,
            int b)
        {
            HeapNode temp =
                nodes[a];

            nodes[a] =
                nodes[b];

            nodes[b] =
                temp;

            itemIndices[
                nodes[a].Item
            ] = a;

            itemIndices[
                nodes[b].Item
            ] = b;
        }
    }
}