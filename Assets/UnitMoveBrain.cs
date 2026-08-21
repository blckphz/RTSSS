using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrain : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;

    [SerializeField]
    private GridManager gridManager;


    // ============================================================
    // MOVEMENT
    // ============================================================

    [Header("Movement")]

    // Faster movement.
    // Old value: 0.25f
    [SerializeField, Min(0.01f)]
    private float moveDuration = 0.08f;


    // ============================================================
    // AI
    // ============================================================

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


    // ============================================================
    // TACTICAL
    // ============================================================

    [Header("Tactical")]
    [SerializeField]
    private bool preferCloserAttackPosition = true;

    [SerializeField]
    private bool preferMoreOpenPositions = true;

    [SerializeField]
    private bool preferSidePositions = true;

    [SerializeField]
    private bool allowMovingAwayToGoAroundObstacles = true;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Enemy Movement Debug")]
    [SerializeField]
    private bool debugEnemyMovement = true;


    // ============================================================
    // STATE
    // ============================================================

    private bool isMoving;

    private bool movementConsumed;


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
    // CACHE
    // ============================================================

    private readonly List<Vector2Int>
        pathCache =
        new List<Vector2Int>(32);

    private readonly List<Vector2Int>
        candidatesCache =
        new List<Vector2Int>(16);

    private readonly Dictionary<Vector2Int, Vector2Int>
        cameFromCache =
        new Dictionary<Vector2Int, Vector2Int>(128);

    private readonly Dictionary<Vector2Int, int>
        gScoreCache =
        new Dictionary<Vector2Int, int>(128);

    private readonly HashSet<Vector2Int>
        visitedCache =
        new HashSet<Vector2Int>(128);

    private readonly PriorityQueue<Vector2Int>
        openSetCache =
        new PriorityQueue<Vector2Int>();


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit =
                GetComponent<AttackUnit>();
        }

        EnsureGridManager();

        movementConsumed = false;
        isMoving = false;
    }


    // ============================================================
    // ENEMY MOVEMENT DEBUG
    // ============================================================

    private bool ShouldDebugEnemyMovement()
    {
        return
            debugEnemyMovement &&
            attackUnit != null &&
            !attackUnit.IsDead() &&
            attackUnit.GetTeam() != Team.Player;
    }


    // ============================================================
    // MOVEMENT STATE
    // ============================================================

    public bool CanUseAIMovement()
    {
        if (
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            return false;
        }

        return
            attackUnit.GetTeam() != Team.Player;
    }


    public bool CanMoveThisTurn()
    {
        return
            !isMoving &&
            !movementConsumed &&
            CanMove();
    }


    public void ConsumeMovement()
    {
        movementConsumed = true;

        if (ShouldDebugEnemyMovement())
        {
            Debug.Log(
                $"[MoveBrain] ENEMY {gameObject.name} movement CONSUMED.",
                gameObject
            );
        }
    }


    public void ResetMovement()
    {
        movementConsumed = false;

        if (ShouldDebugEnemyMovement())
        {
            Debug.Log(
                $"[MoveBrain] ENEMY {gameObject.name} movement RESET.",
                gameObject
            );
        }
    }


    public bool HasConsumedMovement()
    {
        return movementConsumed;
    }


    // ============================================================
    // ATTACK AFTER MOVEMENT
    // ============================================================

    public bool CanAttackAfterMoving(
        AbilitySO ability
    )
    {
        if (attackUnit == null)
        {
            return false;
        }

        if (ability == null)
        {
            return false;
        }

        if (!movementConsumed)
        {
            return true;
        }

        return
            ability.CanAttackWithThisAfterMove();
    }


    // ============================================================
    // MANUAL MOVEMENT
    // ============================================================

    public bool TryMoveTo(
        Vector2Int destination
    )
    {
        if (
            isMoving ||
            !CanMoveThisTurn()
        )
        {
            return false;
        }

        EnsureGridManager();

        if (gridManager == null)
        {
            return false;
        }

        Vector2Int start =
            gridManager.WorldToGridPosition(
                transform.position
            );

        if (
            start == destination ||
            !gridManager.IsInsideGrid(destination) ||
            gridManager.IsCellOccupied(destination)
        )
        {
            return false;
        }

        int moveRange =
            GetMoveRange();

        if (moveRange <= 0)
        {
            return false;
        }

        if (!FindPath(
                start,
                destination,
                pathCache
            ))
        {
            return false;
        }

        int movementCost =
            pathCache.Count - 1;

        if (movementCost > moveRange)
        {
            return false;
        }

        ConsumeMovement();

        StartCoroutine(
            MoveAlongPath(pathCache)
        );

        return true;
    }


    // ============================================================
    // MOVE ALONG PATH
    // ============================================================

    private IEnumerator MoveAlongPath(
        List<Vector2Int> path
    )
    {
        if (
            path == null ||
            path.Count < 2
        )
        {
            yield break;
        }

        isMoving = true;

        for (
            int i = 1;
            i < path.Count;
            i++
        )
        {
            Vector2Int currentPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );

            Vector2Int nextPosition =
                path[i];

            if (!gridManager.IsInsideGrid(
                    nextPosition
                ))
            {
                break;
            }

            bool movementStarted =
                gridManager.StartMoveUnit(
                    gameObject,
                    currentPosition,
                    nextPosition
                );

            if (!movementStarted)
            {
                break;
            }

            Vector3 startWorldPosition =
                transform.position;

            Vector3 targetWorldPosition =
                gridManager.GridToWorldPosition(
                    nextPosition
                );

            float elapsed = 0f;

            while (
                elapsed < moveDuration
            )
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
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            elapsed /
                            moveDuration
                        )
                    );

                transform.position =
                    Vector3.Lerp(
                        startWorldPosition,
                        targetWorldPosition,
                        progress
                    );

                yield return null;
            }

            transform.position =
                targetWorldPosition;

            gridManager.FinishMoveUnit(
                gameObject,
                nextPosition
            );
        }

        isMoving = false;
    }


    // ============================================================
    // AI MOVEMENT
    // ============================================================

    public IEnumerator MoveTowardsEnemy()
    {
        if (
            !CanUseAIMovement() ||
            isMoving ||
            !CanMoveThisTurn()
        )
        {
            yield break;
        }

        EnsureGridManager();

        if (gridManager == null)
        {
            yield break;
        }

        AttackUnit target =
            FindBestTarget();

        if (target == null)
        {
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

        int currentDistance =
            GetMovementDistance(
                currentPosition,
                targetPosition
            );

        if (
            currentDistance <= attackRange
        )
        {
            yield break;
        }

        Vector2Int attackPosition =
            FindBestAttackPosition(
                currentPosition,
                targetPosition
            );

        if (
            attackPosition ==
            currentPosition
        )
        {
            yield break;
        }

        if (
            !FindPath(
                currentPosition,
                attackPosition,
                pathCache
            ) ||
            pathCache.Count < 2
        )
        {
            yield break;
        }

        int moveRange =
            GetMoveRange();

        if (moveRange <= 0)
        {
            yield break;
        }

        int stepsToTake =
            Mathf.Min(
                moveRange,
                pathCache.Count - 1
            );

        if (stepsToTake <= 0)
        {
            yield break;
        }

        ConsumeMovement();

        isMoving = true;

        for (
            int i = 1;
            i <= stepsToTake;
            i++
        )
        {
            Vector2Int currentStepPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );

            Vector2Int nextPosition =
                pathCache[i];

            if (!gridManager.IsInsideGrid(
                    nextPosition
                ))
            {
                break;
            }

            if (gridManager.IsCellOccupied(
                    nextPosition
                ))
            {
                break;
            }

            if (!gridManager.StartMoveUnit(
                    gameObject,
                    currentStepPosition,
                    nextPosition
                ))
            {
                break;
            }

            Vector3 startWorldPosition =
                transform.position;

            Vector3 targetWorldPosition =
                gridManager.GridToWorldPosition(
                    nextPosition
                );

            float elapsed = 0f;

            while (
                elapsed < moveDuration
            )
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
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            elapsed /
                            moveDuration
                        )
                    );

                transform.position =
                    Vector3.Lerp(
                        startWorldPosition,
                        targetWorldPosition,
                        progress
                    );

                yield return null;
            }

            transform.position =
                targetWorldPosition;

            gridManager.FinishMoveUnit(
                gameObject,
                nextPosition
            );
        }

        isMoving = false;

        if (ShouldDebugEnemyMovement())
        {
            Vector2Int finalPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );

            Debug.Log(
                $"[MoveBrain] ENEMY {gameObject.name} moved " +
                $"{stepsToTake} tile(s). " +
                $"Move range: {moveRange}. " +
                $"Start: {currentPosition}. " +
                $"End: {finalPosition}.",
                gameObject
            );
        }
    }


    public bool TryMoveTowardsEnemy()
    {
        if (
            !CanUseAIMovement() ||
            isMoving ||
            !CanMoveThisTurn()
        )
        {
            return false;
        }

        StartCoroutine(
            MoveTowardsEnemy()
        );

        return true;
    }


    // ============================================================
    // PATH PREVIEW
    // ============================================================

    public bool GetPreviewPath(
        Vector2Int destination,
        List<Vector2Int> result
    )
    {
        if (result == null)
        {
            return false;
        }

        result.Clear();

        EnsureGridManager();

        if (gridManager == null)
        {
            return false;
        }

        if (attackUnit == null)
        {
            return false;
        }

        if (attackUnit.IsDead())
        {
            return false;
        }

        Vector2Int start =
            gridManager.WorldToGridPosition(
                transform.position
            );

        if (!gridManager.IsInsideGrid(
                destination
            ))
        {
            return false;
        }

        if (start == destination)
        {
            result.Add(start);

            return true;
        }

        if (!FindPath(
                start,
                destination,
                result
            ))
        {
            return false;
        }

        if (result.Count < 2)
        {
            result.Clear();

            return false;
        }

        int movementCost =
            result.Count - 1;

        int moveRange =
            GetMoveRange();

        if (movementCost > moveRange)
        {
            result.Clear();

            return false;
        }

        return true;
    }


    // ============================================================
    // BASIC MOVEMENT
    // ============================================================

    private bool CanMove()
    {
        return
            attackUnit != null &&
            !attackUnit.IsDead();
    }


    public int GetMoveRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }

        CharacterSO characterData =
            attackUnit.GetCharacterData();

        return characterData == null
            ? 0
            : Mathf.Max(
                0,
                characterData.moveRange
            );
    }


    // ============================================================
    // DIAGONAL MOVEMENT
    // ============================================================

    private bool CanWalkDiagonally()
    {
        if (attackUnit == null)
        {
            return false;
        }

        CharacterSO characterData =
            attackUnit.GetCharacterData();

        return
            characterData != null &&
            characterData.canwalkdiagonally;
    }


    private int GetDirectionCount()
    {
        return CanWalkDiagonally()
            ? Directions.Length
            : CardinalDirectionCount;
    }


    private bool IsDiagonalDirection(
        Vector2Int direction
    )
    {
        return
            direction.x != 0 &&
            direction.y != 0;
    }


    private int GetMovementDistance(
        Vector2Int a,
        Vector2Int b
    )
    {
        int dx =
            Mathf.Abs(
                a.x - b.x
            );

        int dy =
            Mathf.Abs(
                a.y - b.y
            );

        if (CanWalkDiagonally())
        {
            return Mathf.Max(
                dx,
                dy
            );
        }

        return dx + dy;
    }


    // ============================================================
    // TARGET
    // ============================================================

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
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        AttackUnit bestTarget = null;

        float bestScore =
            float.MaxValue;

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

            if (
                other == null ||
                other == attackUnit ||
                other.IsDead() ||
                other.GetTeam() ==
                attackUnit.GetTeam()
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
                    enemyPosition
                );

            float score = 0f;

            if (preferCloserEnemies)
            {
                score +=
                    distance * 10f;
            }

            HealthManager enemyHealth =
                other.GetHealthManager();

            if (
                preferLowHealthEnemies &&
                enemyHealth != null
            )
            {
                float healthPercent =
                    (float)enemyHealth.GetHealth() /
                    Mathf.Max(
                        1,
                        enemyHealth.GetMaxHealth()
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

            if (score < bestScore)
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
    // ATTACK POSITION
    // ============================================================

    private Vector2Int FindBestAttackPosition(
        Vector2Int start,
        Vector2Int target
    )
    {
        GetAttackPositions(
            target,
            candidatesCache
        );

        if (candidatesCache.Count == 0)
        {
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

        for (
            int i = 0;
            i < candidatesCache.Count;
            i++
        )
        {
            Vector2Int candidate =
                candidatesCache[i];

            if (!gridManager.IsInsideGrid(
                    candidate
                ))
            {
                continue;
            }

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
                    pathCache
                ) ||
                pathCache.Count < 2
            )
            {
                continue;
            }

            foundReachablePosition =
                true;

            int movementCost =
                pathCache.Count - 1;

            int distanceToEnemy =
                GetMovementDistance(
                    candidate,
                    target
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
                        candidate
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

            if (score < bestScore)
            {
                bestScore =
                    score;

                bestPosition =
                    candidate;
            }
        }

        return foundReachablePosition
            ? bestPosition
            : FindBestReachableCell(
                start,
                target
            );
    }


    // ============================================================
    // ATTACK POSITIONS
    // ============================================================

    private void GetAttackPositions(
        Vector2Int target,
        List<Vector2Int> results
    )
    {
        results.Clear();

        if (attackRange == 1)
        {
            for (
                int i = 0;
                i < Directions.Length;
                i++
            )
            {
                if (
                    IsDiagonalDirection(
                        Directions[i]
                    ) &&
                    !CanWalkDiagonally()
                )
                {
                    continue;
                }

                results.Add(
                    target +
                    Directions[i]
                );
            }

            return;
        }

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (
            int x = 0;
            x < width;
            x++
        )
        {
            for (
                int y = 0;
                y < height;
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

                if (
                    GetMovementDistance(
                        position,
                        target
                    ) <= attackRange
                )
                {
                    results.Add(
                        position
                    );
                }
            }
        }
    }


    // ============================================================
    // A*
    // ============================================================

    private bool FindPath(
        Vector2Int start,
        Vector2Int destination,
        List<Vector2Int> resultPath
    )
    {
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

        openSetCache.Enqueue(
            start,
            GetMovementDistance(
                start,
                destination
            )
        );

        int directionCount =
            GetDirectionCount();

        while (
            openSetCache.Count > 0
        )
        {
            Vector2Int current =
                openSetCache.Dequeue();

            if (
                current ==
                destination
            )
            {
                ReconstructPath(
                    start,
                    destination,
                    cameFromCache,
                    resultPath
                );

                return true;
            }

            visitedCache.Add(
                current
            );

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
                    current +
                    direction;

                if (
                    visitedCache.Contains(next) ||
                    !gridManager.IsInsideGrid(next)
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
                        new Vector2Int(
                            direction.x,
                            0
                        );

                    Vector2Int vertical =
                        new Vector2Int(
                            0,
                            direction.y
                        );

                    if (
                        gridManager.IsCellOccupied(
                            current + horizontal
                        ) ||
                        gridManager.IsCellOccupied(
                            current + vertical
                        )
                    )
                    {
                        continue;
                    }
                }

                if (
                    next != destination &&
                    gridManager.IsCellOccupied(
                        next
                    )
                )
                {
                    continue;
                }

                int tentativeG =
                    currentG + 1;

                if (
                    !gScoreCache.TryGetValue(
                        next,
                        out int nextG
                    ) ||
                    tentativeG < nextG
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
                            destination
                        );

                    if (!openSetCache.Contains(
                            next
                        ))
                    {
                        openSetCache.Enqueue(
                            next,
                            fScore
                        );
                    }
                }
            }
        }

        return false;
    }


    // ============================================================
    // RECONSTRUCT PATH
    // ============================================================

    private void ReconstructPath(
        Vector2Int start,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        List<Vector2Int> path
    )
    {
        Vector2Int current =
            destination;

        path.Add(
            current
        );

        while (
            current != start
        )
        {
            if (!cameFrom.TryGetValue(
                    current,
                    out Vector2Int previous
                ))
            {
                break;
            }

            current =
                previous;

            path.Add(
                current
            );
        }

        path.Reverse();
    }


    // ============================================================
    // BEST REACHABLE CELL
    // ============================================================

    private Vector2Int FindBestReachableCell(
        Vector2Int start,
        Vector2Int target
    )
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        visitedCache.Clear();
        cameFromCache.Clear();

        queue.Enqueue(start);
        visitedCache.Add(start);

        Vector2Int bestCell =
            start;

        float bestScore =
            CalculateReachableCellScore(
                start,
                target,
                0
            );

        int directionCount =
            GetDirectionCount();

        while (
            queue.Count > 0
        )
        {
            Vector2Int current =
                queue.Dequeue();

            int pathDistance =
                GetPathDistance(
                    start,
                    current,
                    cameFromCache
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

            for (
                int i = 0;
                i < directionCount;
                i++
            )
            {
                Vector2Int direction =
                    Directions[i];

                Vector2Int next =
                    current +
                    direction;

                if (
                    visitedCache.Contains(next) ||
                    !gridManager.IsInsideGrid(next) ||
                    gridManager.IsCellOccupied(next)
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
                        new Vector2Int(
                            direction.x,
                            0
                        );

                    Vector2Int vertical =
                        new Vector2Int(
                            0,
                            direction.y
                        );

                    if (
                        gridManager.IsCellOccupied(
                            current + horizontal
                        ) ||
                        gridManager.IsCellOccupied(
                            current + vertical
                        )
                    )
                    {
                        continue;
                    }
                }

                visitedCache.Add(next);

                cameFromCache[next] =
                    current;

                queue.Enqueue(next);
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

        return
            pathCache.Count >= 2
            ? pathCache[1]
            : start;
    }


    // ============================================================
    // SCORE
    // ============================================================

    private float CalculateReachableCellScore(
        Vector2Int position,
        Vector2Int target,
        int movementCost
    )
    {
        int distance =
            GetMovementDistance(
                position,
                target
            );

        float score =
            distance * 10f +
            movementCost;

        if (preferMoreOpenPositions)
        {
            score -=
                CountOpenNeighbours(
                    position
                ) * 2f;
        }

        return score;
    }


    // ============================================================
    // PATH DISTANCE
    // ============================================================

    private int GetPathDistance(
        Vector2Int start,
        Vector2Int current,
        Dictionary<Vector2Int, Vector2Int> cameFrom
    )
    {
        if (current == start)
        {
            return 0;
        }

        int distance = 0;

        Vector2Int position =
            current;

        while (
            position != start
        )
        {
            if (!cameFrom.TryGetValue(
                    position,
                    out Vector2Int previous
                ))
            {
                break;
            }

            position =
                previous;

            distance++;
        }

        return distance;
    }


    // ============================================================
    // OPEN NEIGHBOURS
    // ============================================================

    private int CountOpenNeighbours(
        Vector2Int position
    )
    {
        int count = 0;

        int directionCount =
            GetDirectionCount();

        for (
            int i = 0;
            i < directionCount;
            i++
        )
        {
            Vector2Int direction =
                Directions[i];

            Vector2Int neighbour =
                position +
                direction;

            if (
                !gridManager.IsInsideGrid(
                    neighbour
                ) ||
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
                    new Vector2Int(
                        direction.x,
                        0
                    );

                Vector2Int vertical =
                    new Vector2Int(
                        0,
                        direction.y
                    );

                if (
                    gridManager.IsCellOccupied(
                        position + horizontal
                    ) ||
                    gridManager.IsCellOccupied(
                        position + vertical
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
    // GRID
    // ============================================================

    private void EnsureGridManager()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

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


    // ============================================================
    // PRIORITY QUEUE
    // ============================================================

    private class PriorityQueue<T>
    {
        private readonly List<KeyValuePair<T, int>>
            elements =
            new List<KeyValuePair<T, int>>();


        public int Count =>
            elements.Count;


        public void Enqueue(
            T item,
            int priority
        )
        {
            elements.Add(
                new KeyValuePair<T, int>(
                    item,
                    priority
                )
            );
        }


        public T Dequeue()
        {
            int bestIndex = 0;

            for (
                int i = 1;
                i < elements.Count;
                i++
            )
            {
                if (
                    elements[i].Value <
                    elements[bestIndex].Value
                )
                {
                    bestIndex = i;
                }
            }

            T bestItem =
                elements[bestIndex].Key;

            elements.RemoveAt(
                bestIndex
            );

            return bestItem;
        }


        public bool Contains(T item)
        {
            for (
                int i = 0;
                i < elements.Count;
                i++
            )
            {
                if (
                    EqualityComparer<T>.Default.Equals(
                        elements[i].Key,
                        item
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }


        public void Clear()
        {
            elements.Clear();
        }
    }
}