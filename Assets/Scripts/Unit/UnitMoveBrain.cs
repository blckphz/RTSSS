
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
    [SerializeField, Min(0f)] private float moveDuration = 0.02f;

    private int stepsRemaining;

    [Header("Facing")]
    [SerializeField] private Vector2Int facingDirection = Vector2Int.up;

    [Header("Explosion Knockback")]
    [SerializeField, Min(0)] private int defaultKnockbackDistance = 1;
    [SerializeField, Min(0f)] private float knockbackStepDuration = 0.08f;
    [SerializeField, Min(0f)] private float knockbackArcHeight = 0.5f;

    [SerializeField, Range(1, 64)]
    private int knockbackDirectionCount = 16;

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

    private bool hasActiveGridMove;
    private Vector2Int activeMoveFrom;
    private GridManager activeMoveGrid;

    private readonly List<Vector2Int> reachableCells =
        new List<Vector2Int>(64);

    private readonly List<Vector2Int> candidatePath =
        new List<Vector2Int>(32);

    private readonly List<Vector2Int> bestPath =
        new List<Vector2Int>(32);

    private readonly Dictionary<Vector2Int, int> reachableDistances =
        new Dictionary<Vector2Int, int>(128);

    private readonly List<Vector2Int> knockbackPath =
        new List<Vector2Int>(64);

    // ============================================================
    // INITIALIZATION
    // ============================================================

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

        StopActiveCoroutine();
        CancelActiveGridMove();

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

    // ============================================================
    // FACING
    // ============================================================

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

    // ============================================================
    // MOVEMENT STATE
    // ============================================================

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

    public bool IsMoving()
    {
        return isMoving;
    }

    // ============================================================
    // MOVEMENT ACTIONS
    // ============================================================

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

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }

    private bool CanMove()
    {
        return attackUnit != null && !attackUnit.IsDead();
    }

    // ============================================================
    // COROUTINE AND GRID CLEANUP
    // ============================================================

    private void StopActiveCoroutine()
    {
        if (movementCoroutine == null)
            return;

        StopCoroutine(movementCoroutine);
        movementCoroutine = null;
    }

    private void BeginGridMove(
        GridManager gridManager,
        Vector2Int fromTile)
    {
        hasActiveGridMove = true;
        activeMoveGrid = gridManager;
        activeMoveFrom = fromTile;
    }

    private void FinishGridMove(
        GridManager gridManager,
        Vector2Int destination)
    {
        gridManager.FinishMoveUnit(gameObject, destination);

        hasActiveGridMove = false;
        activeMoveGrid = null;
    }

    private void CancelActiveGridMove()
    {
        if (!hasActiveGridMove)
            return;

        GridManager gridManager = activeMoveGrid;

        if (gridManager != null)
        {
            transform.position =
                gridManager.GridToWorldPosition(activeMoveFrom);

            gridManager.FinishMoveUnit(gameObject, activeMoveFrom);

            if (tilePin != null)
                tilePin.SetTile(activeMoveFrom);

            if (attackUnit != null)
                attackUnit.SetLogicalGridPosition(activeMoveFrom);
        }

        hasActiveGridMove = false;
        activeMoveGrid = null;
    }

    public void StopMovement()
    {
        movementSequence++;

        StopActiveCoroutine();
        CancelActiveGridMove();

        isBeingKnockedBack = false;

        StopWalkAnimation();

        if (isMoving)
            SetMovingState(false);
    }

    // ============================================================
    // REFERENCES
    // ============================================================

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
        return attackUnit != null
            ? attackUnit.GetCharacterData()
            : null;
    }

    public bool CanWalkDiagonally()
    {
        CharacterSO characterData = GetCharacterData();

        return characterData != null &&
               characterData.canwalkdiagonally;
    }

    public bool CanAttackAfterMoving(AbilitySO ability)
    {
        return ability != null && GetMoveRange() > 0;
    }

    // ============================================================
    // CURRENT TILE
    // ============================================================

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

    // ============================================================
    // AI MOVEMENT PREDICTION
    // ============================================================

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
            float score = CalculateAIMovementScore(
                manager,
                entry.Key,
                targetPosition,
                entry.Value
            );

            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = entry.Key;
            }
        }

        if (bestPosition == currentPosition)
            return false;

        predictedTile = bestPosition;
        return true;
    }

    private float CalculateAIMovementScore(
        UnitMoveBrainManager manager,
        Vector2Int candidate,
        Vector2Int targetPosition,
        int stepsToCandidate)
    {
        int distanceToTarget = manager.GetMovementDistance(
            candidate,
            targetPosition,
            CanWalkDiagonally()
        );

        if (distanceToTarget <= attackRange)
        {
            return -100000f
                + (attackRange - distanceToTarget) * 100f
                - stepsToCandidate;
        }

        return distanceToTarget * 1000f - stepsToCandidate;
    }

    // ============================================================
    // MANUAL MOVEMENT
    // ============================================================

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

        int steps = Mathf.Min(
            stepsRemaining,
            candidatePath.Count - 1
        );

        if (steps <= 0)
            return false;

        List<Vector2Int> path =
            new List<Vector2Int>(candidatePath);

        movementCoroutine = StartCoroutine(
            ExecuteMoveRoutine(path, steps)
        );

        return true;
    }

    // ============================================================
    // AI MOVEMENT TOWARDS ENEMY
    // ============================================================

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
            float score = CalculateAIMovementScore(
                manager,
                entry.Key,
                targetPosition,
                entry.Value
            );

            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = entry.Key;
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

        List<Vector2Int> pathToExecute =
            new List<Vector2Int>(bestPath);

        movementCoroutine = StartCoroutine(
            ExecuteMoveRoutine(pathToExecute, availableSteps)
        );

        while (isMoving || movementCoroutine != null)
            yield return null;
    }

    // ============================================================
    // MOVEMENT EXECUTION
    // ============================================================

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

        int actualSteps = Mathf.Min(
            steps,
            path.Count - 1
        );

        float duration = Mathf.Max(0f, moveDuration);

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

            BeginGridMove(gridManager, fromTile);

            Vector3 startPosition =
                gridManager.GridToWorldPosition(fromTile);

            Vector3 endPosition =
                gridManager.GridToWorldPosition(toTile);

            float elapsed = 0f;

            while (
                elapsed < duration &&
                currentMovementSequence == movementSequence
            )
            {
                elapsed += Time.deltaTime;

                float t = duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / duration);

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

            FinishGridMove(gridManager, toTile);

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
    // KNOCKBACK DIRECTION
    // ============================================================

    private Vector2 GetQuantizedKnockbackDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        int directionCount = Mathf.Max(1, knockbackDirectionCount);

        float angleStep = 360f / directionCount;

        float snappedAngle =
            Mathf.Round(angle / angleStep) * angleStep;

        float radians = snappedAngle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        ).normalized;
    }

    // ============================================================
    // KNOCKBACK PATH GENERATION
    // ============================================================

    private void BuildKnockbackPath(
        Vector2Int startTile,
        Vector2 direction,
        int distance,
        List<Vector2Int> result)
    {
        result.Clear();

        if (distance <= 0 || direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // The destination is calculated using actual geometric
        // distance. A diagonal direction cannot gain extra range.
        Vector2 exactDestination =
            (Vector2)startTile + direction * distance;

        Vector2Int destination = new Vector2Int(
            Mathf.RoundToInt(exactDestination.x),
            Mathf.RoundToInt(exactDestination.y)
        );

        int x = startTile.x;
        int y = startTile.y;

        int dx = Mathf.Abs(destination.x - startTile.x);
        int dy = Mathf.Abs(destination.y - startTile.y);

        int sx = startTile.x < destination.x ? 1 : -1;
        int sy = startTile.y < destination.y ? 1 : -1;

        int error = dx - dy;

        while (x != destination.x || y != destination.y)
        {
            int doubledError = 2 * error;

            int nextX = x;
            int nextY = y;

            if (doubledError > -dy)
            {
                error -= dy;
                nextX += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                nextY += sy;
            }

            // Check the actual distance from the starting tile.
            Vector2 offset = new Vector2(
                nextX - startTile.x,
                nextY - startTile.y
            );

            if (offset.magnitude > distance + 0.0001f)
                break;

            x = nextX;
            y = nextY;

            result.Add(new Vector2Int(x, y));
        }
    }

    // ============================================================
    // EXPLOSION KNOCKBACK
    // ============================================================

    public void ApplyKnockback(
        Vector2Int explosionCenter,
        int distance = -1,
        float stepDuration = -1f)
    {
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

        Vector2 rawDirection = new Vector2(
            currentTile.x - explosionCenter.x,
            currentTile.y - explosionCenter.y
        );

        if (rawDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning(
                $"[UnitMoveBrain] Cannot calculate knockback direction for {name}.",
                this
            );

            return;
        }

        Vector2 direction =
            GetQuantizedKnockbackDirection(rawDirection);

        knockbackPath.Clear();

        BuildKnockbackPath(
            currentTile,
            direction,
            distance,
            knockbackPath
        );

        if (knockbackPath.Count == 0)
            return;

        StopMovement();

        isBeingKnockedBack = true;

        List<Vector2Int> path =
            new List<Vector2Int>(knockbackPath);

        movementCoroutine = StartCoroutine(
            KnockbackRoutine(path, stepDuration)
        );

        Debug.Log(
            $"[UnitMoveBrain] Knockback started: {name}, " +
            $"direction {direction}, distance {distance}, " +
            $"tiles {path.Count}.",
            this
        );
    }

    private IEnumerator KnockbackRoutine(
        List<Vector2Int> path,
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

        for (int i = 0; i < path.Count; i++)
        {
            if (attackUnit != null && attackUnit.IsDead())
                break;

            Vector2Int fromTile = GetCurrentTile();
            Vector2Int toTile = path[i];

            if (!gridManager.IsInsideGrid(toTile))
                break;

            if (!gridManager.CanMoveToCell(gameObject, toTile))
                break;

            if (!gridManager.StartMoveUnit(gameObject, fromTile, toTile))
                break;

            BeginGridMove(gridManager, fromTile);

            Vector3 startPosition =
                gridManager.GridToWorldPosition(fromTile);

            Vector3 endPosition =
                gridManager.GridToWorldPosition(toTile);

            Vector2Int tileDirection = toTile - fromTile;

            float elapsed = 0f;
            bool interruptedByDeath = false;

            while (elapsed < duration)
            {
                if (attackUnit != null && attackUnit.IsDead())
                {
                    interruptedByDeath = true;
                    break;
                }

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

                position.y +=
                    4f * knockbackArcHeight * t * (1f - t);

                transform.position = position;

                yield return null;
            }

            if (interruptedByDeath)
            {
                transform.position = startPosition;

                FinishGridMove(gridManager, fromTile);

                break;
            }

            transform.position = endPosition;

            FinishGridMove(gridManager, toTile);

            if (tilePin != null)
                tilePin.SetTile(toTile);

            if (attackUnit != null)
                attackUnit.SetLogicalGridPosition(toTile);

            SetFacingDirection(tileDirection);

            if (animationController != null)
                animationController.SetMovementDirection(tileDirection);

            yield return null;
        }

        StopWalkAnimation();

        SetMovingState(false);

        isBeingKnockedBack = false;

        movementCoroutine = null;
    }

    // ============================================================
    // ANIMATION
    // ============================================================

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

    // ============================================================
    // PATH PREVIEW
    // ============================================================

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

        Vector2Int start = GetCurrentTile();

        return manager.FindPath(
            start,
            destination,
            CanWalkDiagonally(),
            result,
            gameObject
        );
    }

    // ============================================================
    // REACHABLE CELLS
    // ============================================================

    public void GetReachableCells(List<Vector2Int> result)
    {
        if (result == null)
            return;

        result.Clear();

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

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

    // ============================================================
    // AI VALIDATION
    // ============================================================

    public bool CanUseAIMovement()
    {
        return attackUnit != null
            && !attackUnit.IsDead()
            && attackUnit.GetTeam() != Team.Player;
    }

    // ============================================================
    // KNOCKBACK LANDING TILE PREDICTION
    // ============================================================

    public bool TryGetKnockbackLandingTile(
        Vector2Int explosionCenter,
        int distance,
        out Vector2Int landingTile)
    {
        landingTile = GetCurrentTile();

        if (distance <= 0)
            return false;

        GridManager gridManager = GetGridManager();

        if (gridManager == null)
            return false;

        Vector2Int currentTile = GetCurrentTile();

        Vector2 rawDirection = new Vector2(
            currentTile.x - explosionCenter.x,
            currentTile.y - explosionCenter.y
        );

        if (rawDirection.sqrMagnitude < 0.0001f)
            return false;

        Vector2 direction =
            GetQuantizedKnockbackDirection(rawDirection);

        List<Vector2Int> predictionPath =
            new List<Vector2Int>(64);

        BuildKnockbackPath(
            currentTile,
            direction,
            distance,
            predictionPath
        );

        for (int i = 0; i < predictionPath.Count; i++)
        {
            Vector2Int nextTile = predictionPath[i];

            if (!gridManager.IsInsideGrid(nextTile))
                break;

            if (!gridManager.CanMoveToCell(gameObject, nextTile))
                break;

            landingTile = nextTile;
        }

        return landingTile != currentTile;
    }
}
