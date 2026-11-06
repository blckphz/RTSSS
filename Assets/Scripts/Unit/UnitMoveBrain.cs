
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrain : MonoBehaviour
{
    public static bool IsAnyUnitMoving { get; private set; }

    private static int movingUnitCount;

    public static event Action<Vector3> OnWalkParticleTile;
    public static event Action<UnitMoveBrain> OnMovementActionsChanged;
    public static event Action<UnitMoveBrain> OnMovementStarted;
    public static event Action<UnitMoveBrain> OnMovementFinished;

    [Header("References")]
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private UnitTilePin tilePin;
    [SerializeField] private AnimationController animationController;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.02f;

    private int stepsRemaining;

    [Header("Facing")]
    [SerializeField] private Vector2Int facingDirection = Vector2Int.up;

    [Header("Explosion Knockback")]
    [SerializeField, Min(0)] private int defaultKnockbackDistance = 1;
    [SerializeField, Min(0f)] private float knockbackStepDuration = 0.08f;
    [SerializeField, Min(0f)] private float knockbackArcHeight = 0.5f;

    private bool isBeingKnockedBack;

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
    private Coroutine movementCoroutine;

    private readonly List<Vector2Int> reachableCells = new List<Vector2Int>(64);
    private readonly List<Vector2Int> candidatePath = new List<Vector2Int>(32);
    private readonly List<Vector2Int> bestPath = new List<Vector2Int>(32);

    private readonly Dictionary<Vector2Int, int> reachableDistances =
        new Dictionary<Vector2Int, int>(128);

    private void Awake()
    {
        EnsureComponents();
        ResetMovement();
        NormalizeFacingDirection();
    }

    private void OnEnable()
    {
        EnsureComponents();
        ResetMovement();
        NormalizeFacingDirection();
    }

    private void OnDisable()
    {
        movementSequence++;

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isBeingKnockedBack = false;

        if (isMoving)
            SetMovingState(false);

        StopWalkAnimation();
    }

    private void EnsureComponents()
    {
        if (attackUnit == null)
            attackUnit = GetComponent<AttackUnit>();

        if (tilePin == null)
            tilePin = GetComponent<UnitTilePin>();

        if (animationController == null)
            animationController = GetComponent<AnimationController>();
    }

    private void NormalizeFacingDirection()
    {
        if (facingDirection == Vector2Int.zero)
        {
            facingDirection = Vector2Int.up;
            return;
        }

        if (Mathf.Abs(facingDirection.x) >= Mathf.Abs(facingDirection.y))
        {
            facingDirection = facingDirection.x >= 0
                ? Vector2Int.right
                : Vector2Int.left;
        }
        else
        {
            facingDirection = facingDirection.y >= 0
                ? Vector2Int.up
                : Vector2Int.down;
        }
    }

    public Vector2Int GetFacingDirection()
    {
        return facingDirection;
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            facingDirection = direction.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }
        else
        {
            facingDirection = direction.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }
    }

    private void SetMovingState(bool moving)
    {
        if (moving)
        {
            if (isMoving)
                return;

            isMoving = true;
            movingUnitCount++;
            IsAnyUnitMoving = movingUnitCount > 0;

            OnMovementStarted?.Invoke(this);
        }
        else
        {
            if (!isMoving)
                return;

            isMoving = false;
            movingUnitCount = Mathf.Max(0, movingUnitCount - 1);
            IsAnyUnitMoving = movingUnitCount > 0;

            OnMovementFinished?.Invoke(this);
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

    public void AnimationEvent_Footstep()
    {
        if (AudioFXManager.Instance != null)
            AudioFXManager.Instance.PlayUnitFootstep();
    }

    public bool CanMoveThisTurn()
    {
        return !isMoving
            && !isBeingKnockedBack
            && stepsRemaining > 0
            && CanMove();
    }

    public int GetStepsRemaining()
    {
        return stepsRemaining;
    }

    public int GetMoveRange()
    {
        if (attackUnit == null)
            return 0;

        return Mathf.Max(0, attackUnit.GetEffectiveMoveRange());
    }

    private void ConsumeSteps(int steps)
    {
        if (steps <= 0)
            return;

        stepsRemaining = Mathf.Max(0, stepsRemaining - steps);
        OnMovementActionsChanged?.Invoke(this);
    }

    public void ResetMovement()
    {
        stepsRemaining = GetMoveRange();
        OnMovementActionsChanged?.Invoke(this);
    }

    public bool HasConsumedMovement()
    {
        return stepsRemaining < GetMoveRange();
    }

    public bool HasUsedAllMovement()
    {
        return stepsRemaining <= 0;
    }

    public int GetMoveActionsRemaining()
    {
        return stepsRemaining;
    }

    public int GetMoveActionsPerTurn()
    {
        return GetMoveRange();
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
        return attackUnit != null && !attackUnit.IsDead();
    }

    public void StopMovement()
    {
        movementSequence++;

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        StopWalkAnimation();

        if (isMoving)
            SetMovingState(false);
    }

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance != null)
        {
            GridManager managerGrid =
                UnitMoveBrainManager.Instance.GetGridManager();

            if (managerGrid != null)
                return managerGrid;
        }

        return FindFirstObjectByType<GridManager>();
    }

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null)
            return null;

        return attackUnit.GetCharacterData();
    }

    public bool CanWalkDiagonally()
    {
        CharacterSO characterData = GetCharacterData();
        return characterData != null && characterData.canwalkdiagonally;
    }

    public bool CanAttackAfterMoving(AbilitySO ability)
    {
        if (ability == null)
            return false;

        return GetMoveRange() > 0;
    }

    public bool TryGetCurrentTile(out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        GridManager gridManager = GetGridManager();

        if (gridManager == null)
            return false;

        if (tilePin != null && tilePin.HasTile())
        {
            tile = tilePin.GetTile();
            return gridManager.IsInsideGrid(tile);
        }

        tile = gridManager.WorldToGridPosition(transform.position);
        return gridManager.IsInsideGrid(tile);
    }

    public Vector2Int GetCurrentTile()
    {
        TryGetCurrentTile(out Vector2Int tile);
        return tile;
    }

    public bool TryGetPredictedMoveTile(out Vector2Int predictedTile)
    {
        return TryGetPredictedMoveTile(out predictedTile, false);
    }

    public bool TryGetPredictedMoveTile(
        out Vector2Int predictedTile,
        bool ignoreMovementSteps)
    {
        predictedTile = GetCurrentTile();

        if (!CanUseAIMovement() || isMoving || isBeingKnockedBack)
            return false;

        UnitMoveBrainManager manager = UnitMoveBrainManager.Instance;
        GridManager gridManager = GetGridManager();

        if (manager == null || gridManager == null)
            return false;

        AttackUnit target = manager.FindBestTarget(
            attackUnit,
            preferCloserEnemies,
            preferLowHealthEnemies,
            attackRange,
            CanWalkDiagonally()
        );

        if (target == null)
            return false;

        Vector2Int currentPosition = GetCurrentTile();
        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(target.transform.position);

        int currentDistance = manager.GetMovementDistance(
            currentPosition,
            targetPosition,
            CanWalkDiagonally()
        );

        if (currentDistance <= attackRange)
            return false;

        int movementBudget = ignoreMovementSteps
            ? GetMoveRange()
            : stepsRemaining;

        if (movementBudget <= 0)
            return false;

        reachableDistances.Clear();

        manager.GetReachableCellDistances(
            currentPosition,
            movementBudget,
            CanWalkDiagonally(),
            reachableDistances,
            gameObject
        );

        Vector2Int bestPosition = currentPosition;
        float bestScore = float.MaxValue;

        foreach (KeyValuePair<Vector2Int, int> entry in reachableDistances)
        {
            Vector2Int candidate = entry.Key;
            int stepsToCandidate = entry.Value;

            int distanceToTarget = manager.GetMovementDistance(
                candidate,
                targetPosition,
                CanWalkDiagonally()
            );

            float score;

            if (distanceToTarget <= attackRange)
            {
                score = -100000f
                    + (attackRange - distanceToTarget) * 100f
                    - stepsToCandidate;
            }
            else
            {
                score = distanceToTarget * 1000f
                    - stepsToCandidate;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        if (bestPosition == currentPosition)
            return false;

        predictedTile = bestPosition;
        return true;
    }

    public bool TryMoveTo(Vector2Int destination)
    {
        if (!CanMoveThisTurn())
            return false;

        UnitMoveBrainManager manager = UnitMoveBrainManager.Instance;
        GridManager gridManager = GetGridManager();

        if (manager == null || gridManager == null)
            return false;

        Vector2Int start = GetCurrentTile();

        if (start == destination)
            return false;

        if (!gridManager.IsInsideGrid(destination))
            return false;

        if (!gridManager.CanMoveToCell(gameObject, destination))
            return false;

        candidatePath.Clear();

        bool foundPath = manager.FindPath(
            start,
            destination,
            CanWalkDiagonally(),
            candidatePath,
            gameObject
        );

        if (!foundPath || candidatePath.Count < 2)
            return false;

        int steps = Mathf.Min(stepsRemaining, candidatePath.Count - 1);

        if (steps <= 0)
            return false;

        movementCoroutine = StartCoroutine(
            ExecuteMoveRoutine(new List<Vector2Int>(candidatePath), steps)
        );

        return true;
    }

    public void TryMoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
            return;

        StartCoroutine(MoveTowardsEnemy());
    }

    public IEnumerator MoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
            yield break;

        UnitMoveBrainManager manager = UnitMoveBrainManager.Instance;
        GridManager gridManager = GetGridManager();

        if (manager == null || gridManager == null)
            yield break;

        AttackUnit target = manager.FindBestTarget(
            attackUnit,
            preferCloserEnemies,
            preferLowHealthEnemies,
            attackRange,
            CanWalkDiagonally()
        );

        if (target == null)
            yield break;

        Vector2Int currentPosition = GetCurrentTile();
        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(target.transform.position);

        int currentDistance = manager.GetMovementDistance(
            currentPosition,
            targetPosition,
            CanWalkDiagonally()
        );

        if (currentDistance <= attackRange)
            yield break;

        int movementBudget = stepsRemaining;

        if (movementBudget <= 0)
            yield break;

        reachableCells.Clear();

        manager.GetReachableCells(
            currentPosition,
            movementBudget,
            CanWalkDiagonally(),
            reachableCells,
            gameObject
        );

        reachableDistances.Clear();

        manager.GetReachableCellDistances(
            currentPosition,
            movementBudget,
            CanWalkDiagonally(),
            reachableDistances,
            gameObject
        );

        Vector2Int bestPosition = currentPosition;
        float bestScore = float.MaxValue;

        foreach (KeyValuePair<Vector2Int, int> entry in reachableDistances)
        {
            Vector2Int candidate = entry.Key;
            int stepsToCandidate = entry.Value;

            int distanceToTarget = manager.GetMovementDistance(
                candidate,
                targetPosition,
                CanWalkDiagonally()
            );

            float score;

            if (distanceToTarget <= attackRange)
            {
                score = -100000f
                    + (attackRange - distanceToTarget) * 100f
                    - stepsToCandidate;
            }
            else
            {
                score = distanceToTarget * 1000f
                    - stepsToCandidate;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        if (bestPosition == currentPosition)
            yield break;

        bestPath.Clear();

        bool foundBestPath = manager.FindPath(
            currentPosition,
            bestPosition,
            CanWalkDiagonally(),
            bestPath,
            gameObject
        );

        if (!foundBestPath || bestPath.Count < 2)
            yield break;

        int availableSteps = Mathf.Min(
            stepsRemaining,
            bestPath.Count - 1
        );

        if (availableSteps <= 0)
            yield break;

        List<Vector2Int> pathToExecute = new List<Vector2Int>(bestPath);

        movementCoroutine = StartCoroutine(
            ExecuteMoveRoutine(pathToExecute, availableSteps)
        );

        while (isMoving || movementCoroutine != null)
            yield return null;
    }

    private IEnumerator ExecuteMoveRoutine(
        List<Vector2Int> path,
        int steps)
    {
        if (path == null || path.Count < 2 || steps <= 0)
        {
            movementCoroutine = null;
            yield break;
        }

        GridManager gridManager = GetGridManager();

        if (gridManager == null)
        {
            movementCoroutine = null;
            yield break;
        }

        SetMovingState(true);
        movementSequence++;

        int currentMovementSequence = movementSequence;
        int actualSteps = Mathf.Min(steps, path.Count - 1);

        for (int i = 1; i <= actualSteps; i++)
        {
            if (currentMovementSequence != movementSequence)
                break;

            Vector2Int fromTile = path[i - 1];
            Vector2Int toTile = path[i];
            Vector2Int direction = toTile - fromTile;

            SetFacingDirection(direction);

            if (animationController != null)
                animationController.SetMovementDirection(direction);

            bool started = gridManager.StartMoveUnit(
                gameObject,
                fromTile,
                toTile
            );

            if (!started)
                break;

            Vector3 startPosition = gridManager.GridToWorldPosition(fromTile);
            Vector3 endPosition = gridManager.GridToWorldPosition(toTile);

            float elapsed = 0f;

            while (
                elapsed < moveDuration &&
                currentMovementSequence == movementSequence
            )
            {
                elapsed += Time.deltaTime;

                float t = moveDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / moveDuration);

                transform.position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

                yield return null;
            }

            if (currentMovementSequence != movementSequence)
                break;

            transform.position = endPosition;
            gridManager.FinishMoveUnit(gameObject, toTile);

            if (tilePin != null)
                tilePin.SetTile(toTile);

            if (attackUnit != null)
                attackUnit.SetLogicalGridPosition(toTile);

            ConsumeSteps(1);

            OnWalkParticleTile?.Invoke(endPosition);

            yield return null;
        }

        StopWalkAnimation();
        SetMovingState(false);
        movementCoroutine = null;
    }

    // ============================================================
    // EXPLOSION KNOCKBACK
    // ============================================================

    public void ApplyKnockback(
        Vector2Int explosionCenter,
        int distance = -1,
        float stepDuration = -1f)
    {
        // Do not require AttackUnit on the player.
        if (attackUnit != null && attackUnit.IsDead())
            return;

        if (isBeingKnockedBack)
            return;

        distance = distance < 0
            ? defaultKnockbackDistance
            : distance;

        stepDuration = stepDuration < 0f
            ? knockbackStepDuration
            : stepDuration;

        if (distance <= 0)
            return;

        GridManager gridManager = GetGridManager();

        if (gridManager == null)
        {
            Debug.LogError(
                $"[UnitMoveBrain] No GridManager found for {name}.",
                this
            );
            return;
        }

        Vector2Int currentTile = GetCurrentTile();
        Vector2Int direction = currentTile - explosionCenter;

        if (direction == Vector2Int.zero)
        {
            Debug.LogWarning(
                $"[UnitMoveBrain] Cannot calculate knockback direction for {name}.",
                this
            );
            return;
        }

        if (!CanWalkDiagonally())
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                direction = new Vector2Int(
                    direction.x > 0 ? 1 : -1,
                    0
                );
            }
            else
            {
                direction = new Vector2Int(
                    0,
                    direction.y > 0 ? 1 : -1
                );
            }
        }
        else
        {
            direction = new Vector2Int(
                direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1),
                direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1)
            );
        }

        StopMovement();

        isBeingKnockedBack = true;

        movementCoroutine = StartCoroutine(
            KnockbackRoutine(direction, distance, stepDuration)
        );

        Debug.Log(
            $"[UnitMoveBrain] Knockback started: {name}, " +
            $"direction {direction}, distance {distance}.",
            this
        );
    }

    private IEnumerator KnockbackRoutine(
        Vector2Int direction,
        int distance,
        float stepDuration)
    {
        GridManager gridManager = GetGridManager();

        if (gridManager == null)
        {
            isBeingKnockedBack = false;
            movementCoroutine = null;
            yield break;
        }

        SetMovingState(true);

        float duration = Mathf.Max(0.01f, stepDuration);

        for (int i = 0; i < distance; i++)
        {
            if (attackUnit != null && attackUnit.IsDead())
                break;

            Vector2Int fromTile = GetCurrentTile();
            Vector2Int toTile = fromTile + direction;

            if (!gridManager.IsInsideGrid(toTile))
                break;

            if (!gridManager.CanMoveToCell(gameObject, toTile))
                break;

            if (!gridManager.StartMoveUnit(gameObject, fromTile, toTile))
                break;

            Vector3 startPosition =
                gridManager.GridToWorldPosition(fromTile);

            Vector3 endPosition =
                gridManager.GridToWorldPosition(toTile);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (attackUnit != null && attackUnit.IsDead())
                    break;

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

                // Add a smooth arc while moving between tiles.
                position.y +=
                    4f * knockbackArcHeight * t * (1f - t);

                transform.position = position;

                yield return null;
            }

            if (attackUnit != null && attackUnit.IsDead())
            {
                transform.position = startPosition;
                gridManager.FinishMoveUnit(gameObject, fromTile);
                break;
            }

            transform.position = endPosition;

            gridManager.FinishMoveUnit(gameObject, toTile);

            if (tilePin != null)
                tilePin.SetTile(toTile);

            if (attackUnit != null)
                attackUnit.SetLogicalGridPosition(toTile);

            SetFacingDirection(direction);

            if (animationController != null)
                animationController.SetMovementDirection(direction);

            yield return null;
        }

        StopWalkAnimation();
        SetMovingState(false);

        isBeingKnockedBack = false;
        movementCoroutine = null;
    }

    private void UpdateWalkAnimation(Vector2Int direction)
    {
        if (animationController != null)
            animationController.SetMovementDirection(direction);
    }

    private void StopWalkAnimation()
    {
        if (animationController != null)
            animationController.PlayIdle();
    }

    public bool GetPreviewPath(
        Vector2Int destination,
        List<Vector2Int> result)
    {
        if (result == null)
            return false;

        result.Clear();

        UnitMoveBrainManager manager = UnitMoveBrainManager.Instance;

        if (manager == null)
            return false;

        Vector2Int start = GetCurrentTile();

        return manager.FindPath(
            start,
            destination,
            CanWalkDiagonally(),
            result,
            gameObject
        );
    }

    public void GetReachableCells(List<Vector2Int> result)
    {
        if (result == null)
            return;

        result.Clear();

        UnitMoveBrainManager manager = UnitMoveBrainManager.Instance;

        if (manager == null || GetGridManager() == null)
            return;

        manager.GetReachableCells(
            GetCurrentTile(),
            stepsRemaining,
            CanWalkDiagonally(),
            result,
            gameObject
        );
    }

    public bool CanUseAIMovement()
    {
        return attackUnit != null
            && !attackUnit.IsDead()
            && attackUnit.GetTeam() != Team.Player;
    }
}
