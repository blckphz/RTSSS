using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMoveBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AttackUnit attackUnit;

    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    private float moveDuration = 0.08f;

    [Header("AI Config")]
    [SerializeField, Min(1)]
    private int attackRange = 1;

    [SerializeField]
    private bool preferLowHealthEnemies = true;

    [SerializeField]
    private bool preferCloserEnemies = true;

    [Header("Tactical Config")]
    [SerializeField]
    private bool preferCloserAttackPosition = true;

    [SerializeField]
    private bool preferMoreOpenPositions = true;

    [SerializeField]
    private bool preferSidePositions = true;

    private bool isMoving;
    private bool movementConsumed;

    private readonly List<Vector2Int> localPathCache =
        new List<Vector2Int>(32);

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit = GetComponent<AttackUnit>();
        }

        movementConsumed = false;
        isMoving = false;
    }

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

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance == null)
        {
            return null;
        }

        return UnitMoveBrainManager.Instance.GetGridManager();
    }

    public bool CanAttackAfterMoving(AbilitySO ability)
    {
        if (attackUnit == null || ability == null)
        {
            return false;
        }

        if (!movementConsumed)
        {
            return true;
        }

        return ability.CanAttackWithThisAfterMove();
    }

    private bool CanMove()
    {
        return attackUnit != null &&
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

        if (characterData == null)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            characterData.moveRange
        );
    }

    public bool CanWalkDiagonally()
    {
        if (attackUnit == null)
        {
            return false;
        }

        CharacterSO characterData =
            attackUnit.GetCharacterData();

        return characterData != null &&
               characterData.canwalkdiagonally;
    }

    public bool TryMoveTo(Vector2Int destination)
    {
        if (isMoving || !CanMoveThisTurn())
        {
            return false;
        }

        GridManager gridManager =
            GetGridManager();

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

        if (!UnitMoveBrainManager.Instance.FindPath(
                start,
                destination,
                CanWalkDiagonally(),
                localPathCache))
        {
            return false;
        }

        int movementCost =
            localPathCache.Count - 1;

        if (movementCost > moveRange)
        {
            return false;
        }

        ConsumeMovement();

        StartCoroutine(
            MoveAlongPath(localPathCache)
        );

        return true;
    }

    private IEnumerator MoveAlongPath(
        List<Vector2Int> path
    )
    {
        if (path == null || path.Count < 2)
        {
            yield break;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            movementConsumed = false;
            yield break;
        }

        isMoving = true;

        for (
            int i = 1;
            i < path.Count;
            i++
        )
        {
            if (!CanMove())
            {
                break;
            }

            Vector2Int currentPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );

            Vector2Int nextPosition =
                path[i];

            if (!gridManager.IsInsideGrid(nextPosition))
            {
                break;
            }

            if (gridManager.IsCellOccupied(nextPosition))
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
                    transform.position =
                        targetWorldPosition;

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
        }

        isMoving = false;
    }

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
        if (!CanUseAIMovement() ||
            isMoving ||
            !CanMoveThisTurn())
        {
            yield break;
        }

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

        Vector2Int currentPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        int currentDistance =
            UnitMoveBrainManager.Instance.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally()
            );

        if (currentDistance <= attackRange)
        {
            yield break;
        }

        Vector2Int attackPosition =
            UnitMoveBrainManager.Instance.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions
            );

        if (attackPosition == currentPosition)
        {
            yield break;
        }

        bool pathFound =
            UnitMoveBrainManager.Instance.FindPath(
                currentPosition,
                attackPosition,
                CanWalkDiagonally(),
                localPathCache
            );

        if (!pathFound ||
            localPathCache.Count < 2)
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
                localPathCache.Count - 1
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
                localPathCache[i];

            if (!gridManager.IsInsideGrid(nextPosition))
            {
                break;
            }

            if (gridManager.IsCellOccupied(nextPosition))
            {
                break;
            }

            if (!gridManager.StartMoveUnit(
                    gameObject,
                    currentStepPosition,
                    nextPosition))
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

            while (elapsed < moveDuration)
            {
                if (!CanMove())
                {
                    transform.position =
                        targetWorldPosition;

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
        }

        isMoving = false;
    }

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

        GridManager gridManager =
            GetGridManager();

        if (
            gridManager == null ||
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            return false;
        }

        Vector2Int start =
            gridManager.WorldToGridPosition(
                transform.position
            );

        if (!gridManager.IsInsideGrid(destination))
        {
            return false;
        }

        if (start == destination)
        {
            result.Add(start);
            return true;
        }

        if (!UnitMoveBrainManager.Instance.FindPath(
                start,
                destination,
                CanWalkDiagonally(),
                result))
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

        if (movementCost > GetMoveRange())
        {
            result.Clear();
            return false;
        }

        return true;
    }
}