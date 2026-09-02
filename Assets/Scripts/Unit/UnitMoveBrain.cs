using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttackUnit))]
public class UnitMoveBrain : MonoBehaviour
{
// ============================================================
// REFERENCES
// ============================================================

[Header("References")]
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private UnitTilePin tilePin;
    [SerializeField] private AnimationController animationController;


    // ============================================================
    // MOVEMENT CONFIG
    // ============================================================

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.08f;


    // ============================================================
    // AI CONFIG
    // ============================================================

    [Header("AI Config")]
    [SerializeField, Min(1)] private int attackRange = 1;
    [SerializeField] private bool preferLowHealthEnemies = true;
    [SerializeField] private bool preferCloserEnemies = true;


    // ============================================================
    // TACTICAL CONFIG
    // ============================================================

    [Header("Tactical Config")]
    [SerializeField] private bool preferCloserAttackPosition = true;
    [SerializeField] private bool preferMoreOpenPositions = true;
    [SerializeField] private bool preferSidePositions = true;


    // ============================================================
    // STATE & CACHE
    // ============================================================

    private bool isMoving;
    private bool movementConsumed;


    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        EnsureComponents();

        movementConsumed = false;
        isMoving = false;
    }


    private void OnEnable()
    {
        EnsureComponents();
    }


    private void OnDisable()
    {
        isMoving = false;

        if (animationController != null)
        {
            animationController.StopWalking();
        }
    }


    private void EnsureComponents()
    {
        if (attackUnit == null)
        {
            attackUnit = GetComponent<AttackUnit>();
        }

        if (tilePin == null)
        {
            tilePin = GetComponent<UnitTilePin>();
        }

        if (animationController == null)
        {
            animationController = GetComponent<AnimationController>();
        }
    }


    // ============================================================
    // STATE GETTERS & SETTERS
    // ============================================================

    public bool CanUseAIMovement()
    {
        return attackUnit != null &&
               !attackUnit.IsDead() &&
               attackUnit.GetTeam() != Team.Player;
    }


    public bool CanMoveThisTurn()
    {
        return !isMoving &&
               !movementConsumed &&
               CanMove();
    }


    public void ConsumeMovement()
    {
        movementConsumed = true;
    }


    public void ResetMovement()
    {
        movementConsumed = false;
    }


    public bool HasConsumedMovement()
    {
        return movementConsumed;
    }


    public bool IsMoving()
    {
        return isMoving;
    }


    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }


    private bool CanMove()
    {
        return attackUnit != null &&
               !attackUnit.IsDead();
    }


    // ============================================================
    // DATA & MANAGER ACCESSORS
    // ============================================================

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance == null)
        {
            return null;
        }

        return UnitMoveBrainManager.Instance.GetGridManager();
    }


    public CharacterSO GetCharacterData()
    {
        return attackUnit != null
            ? attackUnit.GetCharacterData()
            : null;
    }


    public int GetMoveRange()
    {
        CharacterSO data = GetCharacterData();

        if (data == null)
        {
            return 0;
        }

        return Mathf.Max(0, data.moveRange);
    }


    public bool CanWalkDiagonally()
    {
        CharacterSO data = GetCharacterData();

        return data != null &&
               data.canwalkdiagonally;
    }


    public bool CanAttackAfterMoving(AbilitySO ability)
    {
        if (attackUnit == null || ability == null)
        {
            return false;
        }

        return !movementConsumed ||
               ability.CanAttackWithThisAfterMove();
    }


    // ============================================================
    // TILE CALCULATIONS
    // ============================================================

    public bool TryGetCurrentTile(out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        EnsureComponents();

        if (tilePin != null && tilePin.HasTile())
        {
            tile = tilePin.GetTile();
            return true;
        }

        GridManager gridManager = GetGridManager();

        if (gridManager == null)
        {
            return false;
        }

        tile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        return true;
    }


    public Vector2Int GetCurrentTile()
    {
        TryGetCurrentTile(out Vector2Int tile);

        return tile;
    }


    // ============================================================
    // DIRECT MOVEMENT
    // ============================================================

    public bool TryMoveTo(Vector2Int destination)
    {
        if (!CanMoveThisTurn())
        {
            return false;
        }


        GridManager gridManager = GetGridManager();

        if (gridManager == null)
        {
            return false;
        }


        if (!TryGetCurrentTile(out Vector2Int start))
        {
            return false;
        }


        if (!gridManager.IsInsideGrid(destination))
        {
            return false;
        }


        if (start == destination)
        {
            return false;
        }


        GameObject occupant =
            gridManager.GetUnitAt(destination);

        if (occupant != null &&
            occupant != gameObject)
        {
            return false;
        }


        int moveRange = GetMoveRange();

        if (moveRange <= 0)
        {
            return false;
        }


        List<Vector2Int> path =
            new List<Vector2Int>();

        bool foundPath =
            UnitMoveBrainManager.Instance.FindPath(
                start,
                destination,
                CanWalkDiagonally(),
                path,
                gameObject
            );


        if (!foundPath)
        {
            return false;
        }


        int steps = path.Count - 1;

        if (steps <= 0)
        {
            return false;
        }


        if (steps > moveRange)
        {
            return false;
        }


        HideMovementRangeImmediately();


        StartCoroutine(
            ExecuteMoveRoutine(path, steps)
        );

        return true;
    }


    // ============================================================
    // HIDE MOVEMENT RANGE
    // ============================================================

    private void HideMovementRangeImmediately()
    {
        GridHighlightBrain highlightBrain =
            FindFirstObjectByType<GridHighlightBrain>();

        if (highlightBrain == null)
        {
            return;
        }

        highlightBrain.HideMovementRange();
    }


    // ============================================================
    // AI MOVEMENT LOGIC
    // ============================================================

    public bool TryMoveTowardsEnemy()
    {
        if (!CanUseAIMovement() ||
            isMoving ||
            !CanMoveThisTurn())
        {
            return false;
        }

        StartCoroutine(
            MoveTowardsEnemy()
        );

        return true;
    }


    public IEnumerator MoveTowardsEnemy()
    {
        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            yield break;
        }


        AttackUnit target =
            UnitMoveBrainManager.Instance.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally()
            );


        if (target == null)
        {
            yield break;
        }


        if (!TryGetCurrentTile(
            out Vector2Int currentPos))
        {
            yield break;
        }


        Vector2Int targetPos =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        int distance =
            UnitMoveBrainManager.Instance.GetMovementDistance(
                currentPos,
                targetPos,
                CanWalkDiagonally()
            );


        if (distance <= attackRange)
        {
            yield break;
        }


        Vector2Int attackPos =
            UnitMoveBrainManager.Instance.FindBestAttackPosition(
                currentPos,
                targetPos,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions
            );


        if (attackPos == currentPos)
        {
            yield break;
        }


        List<Vector2Int> path =
            new List<Vector2Int>();


        bool foundPath =
            UnitMoveBrainManager.Instance.FindPath(
                currentPos,
                attackPos,
                CanWalkDiagonally(),
                path,
                gameObject
            );


        if (!foundPath ||
            path.Count < 2)
        {
            yield break;
        }


        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );


        if (availableSteps <= 0)
        {
            yield break;
        }


        yield return StartCoroutine(
            ExecuteMoveRoutine(
                path,
                availableSteps
            )
        );
    }


    // ============================================================
    // EXECUTE MOVEMENT ROUTINE
    // ============================================================

    private IEnumerator ExecuteMoveRoutine(
        List<Vector2Int> path,
        int stepsToTake)
    {
        GridManager gridManager =
            GetGridManager();


        if (gridManager == null ||
            path == null ||
            path.Count < 2 ||
            stepsToTake <= 0)
        {
            yield break;
        }


        isMoving = true;

        ConsumeMovement();


        Vector2Int lastLogicalTile =
            path[0];


        Vector2Int finalRequestedTile =
            path[
                Mathf.Min(
                    stepsToTake,
                    path.Count - 1
                )
            ];


        bool completedAllSteps = false;


        try
        {
            int actualSteps =
                Mathf.Min(
                    stepsToTake,
                    path.Count - 1
                );


            for (
                int i = 1;
                i <= actualSteps;
                i++)
            {
                if (!CanMove())
                {
                    break;
                }


                Vector2Int currentPosition =
                    lastLogicalTile;


                Vector2Int nextPosition =
                    path[i];


                if (!gridManager.IsInsideGrid(
                    nextPosition))
                {
                    break;
                }


                GameObject occupant =
                    gridManager.GetUnitAt(
                        nextPosition
                    );


                if (occupant != null &&
                    occupant != gameObject)
                {
                    break;
                }


                if (!gridManager.StartMoveUnit(
                    gameObject,
                    currentPosition,
                    nextPosition))
                {
                    break;
                }


                Vector2Int movementDirection =
                    nextPosition - currentPosition;


                if (animationController != null)
                {
                    animationController.SetMovementDirection(
                        movementDirection
                    );
                }


                Vector3 startWorldPosition =
                    transform.position;


                Vector3 targetWorldPosition =
                    gridManager.GridToWorldPosition(
                        nextPosition
                    );


                float elapsed = 0f;


                while (elapsed < moveDuration)
                {
                    if (!CanMove())
                    {
                        break;
                    }


                    elapsed += Time.deltaTime;


                    float progress =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                elapsed / moveDuration
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


                lastLogicalTile =
                    nextPosition;


                if (tilePin != null)
                {
                    tilePin.UpdateTileAfterMovement(
                        nextPosition
                    );
                }


                yield return null;
            }


            completedAllSteps =
                lastLogicalTile ==
                finalRequestedTile;
        }
        finally
        {
            isMoving = false;


            Vector3 finalWorld =
                gridManager.GridToWorldPosition(
                    lastLogicalTile
                );


            transform.position =
                finalWorld;


            if (tilePin != null)
            {
                tilePin.UpdateTileAfterMovement(
                    lastLogicalTile
                );
            }


            if (!completedAllSteps)
            {
                gridManager.FinishMoveUnit(
                    gameObject,
                    lastLogicalTile
                );
            }


            if (animationController != null)
            {
                animationController.PlayIdle();
            }
        }
    }


    // ============================================================
    // PATH PREVIEW & QUERY METHODS
    // ============================================================

    public bool GetPreviewPath(
        Vector2Int destination,
        List<Vector2Int> result)
    {
        if (result == null)
        {
            return false;
        }


        result.Clear();


        GridManager gridManager =
            GetGridManager();


        if (gridManager == null ||
            attackUnit == null ||
            attackUnit.IsDead())
        {
            return false;
        }


        if (!TryGetCurrentTile(
            out Vector2Int start))
        {
            return false;
        }


        if (!gridManager.IsInsideGrid(
            destination))
        {
            return false;
        }


        if (start == destination)
        {
            result.Add(start);

            return true;
        }


        GameObject occupant =
            gridManager.GetUnitAt(destination);


        if (occupant != null &&
            occupant != gameObject)
        {
            return false;
        }


        bool foundPath =
            UnitMoveBrainManager.Instance.FindPath(
                start,
                destination,
                CanWalkDiagonally(),
                result,
                gameObject
            );


        if (!foundPath)
        {
            result.Clear();

            return false;
        }


        int steps =
            result.Count - 1;


        if (steps <= 0 ||
            steps > GetMoveRange())
        {
            result.Clear();

            return false;
        }


        return true;
    }


    public void GetReachableCells(
        List<Vector2Int> result)
    {
        if (result == null)
        {
            return;
        }


        result.Clear();


        GridManager gridManager =
            GetGridManager();


        if (gridManager == null ||
            attackUnit == null ||
            attackUnit.IsDead())
        {
            return;
        }


        if (!TryGetCurrentTile(
            out Vector2Int start))
        {
            return;
        }


        int moveRange =
            GetMoveRange();


        if (moveRange <= 0)
        {
            return;
        }


        UnitMoveBrainManager.Instance.GetReachableCells(
            start,
            moveRange,
            CanWalkDiagonally(),
            result,
            gameObject
        );


        result.Remove(start);
    }

}
