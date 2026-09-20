using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrain : MonoBehaviour
{
    public static bool IsAnyUnitMoving { get; private set; }

    private static int movingUnitCount;

    public static event System.Action<Vector3>
        OnWalkParticleTile;

    public static event System.Action<UnitMoveBrain>
        OnMovementActionsChanged;

    [Header("References")]
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private UnitTilePin tilePin;
    [SerializeField] private AnimationController animationController;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.02f;

    [Tooltip("How many separate movement actions this unit gets per turn.")]
    [SerializeField] private int moveActionsPerTurn;

    private int moveActionsRemaining;

    [Header("AI Targeting")]
    [SerializeField] private int attackRange = 1;
    [SerializeField] private bool preferLowHealthEnemies = true;
    [SerializeField] private bool preferCloserEnemies = true;

    [Header("AI Attack Position")]
    [SerializeField] private bool preferCloserAttackPosition = true;
    [SerializeField] private bool preferMoreOpenPositions = true;
    [SerializeField] private bool preferSidePositions = true;

    private bool isMoving;
    private int movementSequence;

    private void Awake()
    {
        EnsureComponents();
        ResetMovement();
    }

    private void OnEnable()
    {
        EnsureComponents();
        ResetMovement();
    }

    private void OnDisable()
    {
        if (isMoving)
            SetMovingState(false);

        isMoving = false;

        StopWalkAnimation();
    }

    private void EnsureComponents()
    {
        if (attackUnit == null)
            attackUnit = GetComponent<AttackUnit>();

        if (tilePin == null)
            tilePin = GetComponent<UnitTilePin>();

        if (animationController == null)
            animationController =
                GetComponent<AnimationController>();
    }

    private void SetMovingState(bool moving)
    {
        if (moving)
        {
            if (isMoving)
                return;

            isMoving = true;
            movingUnitCount++;

            IsAnyUnitMoving =
                movingUnitCount > 0;
        }
        else
        {
            if (!isMoving)
                return;

            isMoving = false;

            movingUnitCount =
                Mathf.Max(
                    0,
                    movingUnitCount - 1);

            IsAnyUnitMoving =
                movingUnitCount > 0;
        }
    }

    public static bool AreAnyUnitsMoving()
    {
        return IsAnyUnitMoving;
    }

    public static int GetMovingUnitCount()
    {
        return movingUnitCount;
    }

    public bool CanMoveThisTurn()
    {
        return
            !isMoving &&
            moveActionsRemaining > 0 &&
            CanMove();
    }

    public void ConsumeMovement()
    {
        if (moveActionsRemaining <= 0)
            return;

        moveActionsRemaining--;

        OnMovementActionsChanged?.Invoke(this);
    }

    public void ResetMovement()
    {
        moveActionsRemaining =
            Mathf.Max(
                0,
                moveActionsPerTurn);

        OnMovementActionsChanged?.Invoke(this);
    }

    public bool HasConsumedMovement()
    {
        return moveActionsRemaining <
               moveActionsPerTurn;
    }

    public int GetMoveActionsRemaining()
    {
        return moveActionsRemaining;
    }

    public int GetMoveActionsPerTurn()
    {
        return moveActionsPerTurn;
    }

    public bool HasUsedAllMovement()
    {
        return moveActionsRemaining <= 0;
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

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance == null)
            return null;

        return UnitMoveBrainManager.Instance
            .GetGridManager();
    }

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null)
            return null;

        return attackUnit.GetCharacterData();
    }

    // ============================================================
    // EFFECTIVE MOVE RANGE
    // ============================================================

    public int GetMoveRange()
    {
        if (attackUnit == null)
            return 0;

        int range =
            attackUnit.GetEffectiveMoveRange();

        return Mathf.Max(0, range);
    }

    public bool CanWalkDiagonally()
    {
        CharacterSO characterData =
            GetCharacterData();

        if (characterData == null)
            return false;

        return characterData.canwalkdiagonally;
    }

    public bool CanAttackAfterMoving(
        AbilitySO ability)
    {
        if (ability == null)
            return false;

        return GetMoveRange() > 0;
    }

    public bool TryGetCurrentTile(
        out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            return false;

        if (tilePin != null &&
            tilePin.HasTile())
        {
            tile = tilePin.GetTile();

            return gridManager.IsInsideGrid(tile);
        }

        tile =
            gridManager.WorldToGridPosition(
                transform.position);

        return gridManager.IsInsideGrid(tile);
    }

    public Vector2Int GetCurrentTile()
    {
        TryGetCurrentTile(
            out Vector2Int tile);

        return tile;
    }

    public bool TryGetPredictedMoveTile(
        out Vector2Int predictedTile)
    {
        predictedTile =
            GetCurrentTile();

        if (!CanUseAIMovement())
            return false;

        if (!CanMoveThisTurn())
            return false;

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
            return false;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            return false;

        AttackUnit target =
            manager.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally());

        if (target == null)
            return false;

        Vector2Int currentPosition =
            GetCurrentTile();

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position);

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally());

        if (distance <= attackRange)
            return false;

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions,
                gameObject);

        if (attackPosition == currentPosition)
            return false;

        List<Vector2Int> path =
            new List<Vector2Int>(32);

        bool foundPath =
            manager.FindPath(
                currentPosition,
                attackPosition,
                CanWalkDiagonally(),
                path,
                gameObject);

        if (!foundPath)
            return false;

        if (path.Count < 2)
            return false;

        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1);

        if (availableSteps <= 0)
            return false;

        predictedTile =
            path[availableSteps];

        return predictedTile != currentPosition;
    }

    public bool TryMoveTo(
        Vector2Int destination)
    {
        if (!CanMoveThisTurn())
            return false;

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
            return false;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            return false;

        Vector2Int start =
            GetCurrentTile();

        if (start == destination)
            return false;

        if (!gridManager.IsInsideGrid(destination))
            return false;

        if (!gridManager.CanMoveToCell(
                gameObject,
                destination))
        {
            return false;
        }

        int distance =
            manager.GetMovementDistance(
                start,
                destination,
                CanWalkDiagonally());

        if (distance > GetMoveRange())
            return false;

        List<Vector2Int> path =
            new List<Vector2Int>(32);

        bool foundPath =
            manager.FindPath(
                start,
                destination,
                CanWalkDiagonally(),
                path,
                gameObject);

        if (!foundPath)
            return false;

        if (path.Count < 2)
            return false;

        int steps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1);

        if (steps <= 0)
            return false;

        ConsumeMovement();

        StartCoroutine(
            ExecuteMoveRoutine(
                path,
                steps));

        return true;
    }

    public void TryMoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
            return;

        StartCoroutine(
            MoveTowardsEnemy());
    }

    public IEnumerator MoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
            yield break;

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
            yield break;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            yield break;

        AttackUnit target =
            manager.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally());

        if (target == null)
            yield break;

        Vector2Int currentPosition =
            GetCurrentTile();

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position);

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally());

        if (distance <= attackRange)
            yield break;

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions,
                gameObject);

        if (attackPosition == currentPosition)
            yield break;

        List<Vector2Int> path =
            new List<Vector2Int>(32);

        bool foundPath =
            manager.FindPath(
                currentPosition,
                attackPosition,
                CanWalkDiagonally(),
                path,
                gameObject);

        if (!foundPath)
            yield break;

        if (path.Count < 2)
            yield break;

        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1);

        if (availableSteps <= 0)
            yield break;

        ConsumeMovement();

        yield return ExecuteMoveRoutine(
            path,
            availableSteps);
    }

    private IEnumerator ExecuteMoveRoutine(
        List<Vector2Int> path,
        int steps)
    {
        if (path == null ||
            path.Count < 2 ||
            steps <= 0)
        {
            yield break;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            yield break;

        SetMovingState(true);

        movementSequence++;

        int currentMovementSequence =
            movementSequence;

        int actualSteps =
            Mathf.Min(
                steps,
                path.Count - 1);

        for (int i = 1;
             i <= actualSteps;
             i++)
        {
            Vector2Int fromTile =
                path[i - 1];

            Vector2Int toTile =
                path[i];

            if (animationController != null)
            {
                Vector2Int direction =
                    toTile - fromTile;

                animationController.SetMovementDirection(
                    direction);
            }

            bool started =
                gridManager.StartMoveUnit(
                    gameObject,
                    fromTile,
                    toTile);

            if (!started)
            {
                Debug.LogWarning(
                    $"[Movement] {name} failed to move from {fromTile} to {toTile}.",
                    this);

                break;
            }

            Vector3 startPosition =
                gridManager.GridToWorldPosition(
                    fromTile);

            Vector3 endPosition =
                gridManager.GridToWorldPosition(
                    toTile);

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t =
                    moveDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed / moveDuration);

                transform.position =
                    Vector3.Lerp(
                        startPosition,
                        endPosition,
                        t);

                yield return null;
            }

            transform.position =
                endPosition;

            gridManager.FinishMoveUnit(
                gameObject,
                toTile);

            if (tilePin != null)
                tilePin.SetTile(toTile);

            if (attackUnit != null)
            {
                attackUnit.SetLogicalGridPosition(
                    toTile);
            }

            Vector3 particlePosition =
                gridManager.GridToWorldPosition(
                    toTile);

            OnWalkParticleTile?.Invoke(
                particlePosition);

            yield return null;
        }

        StopWalkAnimation();

        SetMovingState(false);
    }

    private void UpdateWalkAnimation(
        Vector2Int direction)
    {
        if (animationController == null)
            return;

        animationController.SetMovementDirection(
            direction);
    }

    private void StopWalkAnimation()
    {
        if (animationController == null)
            return;

        animationController.PlayIdle();
    }

    public bool GetPreviewPath(
        Vector2Int destination,
        List<Vector2Int> result)
    {
        if (result == null)
            return false;

        result.Clear();

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
            return false;

        Vector2Int start =
            GetCurrentTile();

        return manager.FindPath(
            start,
            destination,
            CanWalkDiagonally(),
            result,
            gameObject);
    }

    public void GetReachableCells(
        List<Vector2Int> result)
    {
        if (result == null)
            return;

        result.Clear();

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
            return;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            return;

        manager.GetReachableCells(
            GetCurrentTile(),
            GetMoveRange(),
            CanWalkDiagonally(),
            result,
            gameObject);
    }

    public bool CanUseAIMovement()
    {
        return attackUnit != null &&
               !attackUnit.IsDead() &&
               attackUnit.GetTeam() != Team.Player;
    }
}