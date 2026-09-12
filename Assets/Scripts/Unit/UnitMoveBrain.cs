using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private UnitTilePin tilePin;
    [SerializeField] private AnimationController animationController;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.08f;

    [Header("AI Targeting")]
    [SerializeField, Min(1)] private int attackRange = 1;
    [SerializeField] private bool preferLowHealthEnemies = true;
    [SerializeField] private bool preferCloserEnemies = true;

    [Header("AI Attack Position")]
    [SerializeField] private bool preferCloserAttackPosition = true;
    [SerializeField] private bool preferMoreOpenPositions = true;
    [SerializeField] private bool preferSidePositions = true;

    private bool isMoving;
    private bool movementConsumed;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        EnsureComponents();
    }

    private void OnEnable()
    {
        EnsureComponents();
        ResetMovement();
    }

    private void OnDisable()
    {
        isMoving = false;
    }

    // ============================================================
    // COMPONENTS
    // ============================================================

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
    // MOVEMENT STATE
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
    // GRID
    // ============================================================

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance == null)
            return null;

        return UnitMoveBrainManager.Instance.GetGridManager();
    }

    // ============================================================
    // CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null)
            return null;

        return attackUnit.GetCharacterData();
    }

    public int GetMoveRange()
    {
        CharacterSO characterData = GetCharacterData();

        if (characterData == null)
            return 0;

        return Mathf.Max(0, characterData.moveRange);
    }

    public bool CanWalkDiagonally()
    {
        CharacterSO characterData = GetCharacterData();

        if (characterData == null)
            return false;

        return characterData.canwalkdiagonally;
    }

    public bool CanAttackAfterMoving(AbilitySO ability)
    {
        if (ability == null)
            return false;

        return GetMoveRange() > 0;
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

        tile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        return gridManager.IsInsideGrid(tile);
    }

    public Vector2Int GetCurrentTile()
    {
        TryGetCurrentTile(out Vector2Int tile);

        return tile;
    }

    // ============================================================
    // DYNAMIC ENEMY MOVEMENT PREDICTION
    // ============================================================
    //
    // This uses the exact same AI calculations used by
    // MoveTowardsEnemy().
    //
    // It predicts the tile this enemy would actually reach
    // during its next movement.
    //
    // The hologram manager can call this every frame.
    // ============================================================

    public bool TryGetPredictedMoveTile(
        out Vector2Int predictedTile)
    {
        predictedTile = GetCurrentTile();

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

        // --------------------------------------------------------
        // FIND THE SAME TARGET THE AI WOULD USE
        // --------------------------------------------------------

        AttackUnit target =
            manager.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally()
            );

        if (target == null)
            return false;

        // --------------------------------------------------------
        // CURRENT POSITION
        // --------------------------------------------------------

        Vector2Int currentPosition =
            GetCurrentTile();

        // --------------------------------------------------------
        // TARGET POSITION
        // --------------------------------------------------------

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        // --------------------------------------------------------
        // ALREADY IN ATTACK RANGE
        // --------------------------------------------------------

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally()
            );

        if (distance <= attackRange)
            return false;

        // --------------------------------------------------------
        // FIND THE SAME ATTACK POSITION THE AI USES
        // --------------------------------------------------------

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions
            );

        if (attackPosition == currentPosition)
            return false;

        // --------------------------------------------------------
        // FIND ACTUAL PATH
        // --------------------------------------------------------

        List<Vector2Int> path =
            new List<Vector2Int>(32);

        bool foundPath =
            manager.FindPath(
                currentPosition,
                attackPosition,
                CanWalkDiagonally(),
                path,
                gameObject
            );

        if (!foundPath)
            return false;

        if (path.Count < 2)
            return false;

        // --------------------------------------------------------
        // DETERMINE HOW MANY TILES THE ENEMY CAN ACTUALLY MOVE
        // --------------------------------------------------------

        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );

        if (availableSteps <= 0)
            return false;

        // --------------------------------------------------------
        // THIS IS THE PREDICTED DESTINATION
        // --------------------------------------------------------

        predictedTile =
            path[availableSteps];

        return predictedTile != currentPosition;
    }

    // ============================================================
    // DIRECT MOVE
    // ============================================================

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
                CanWalkDiagonally()
            );

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
                gameObject
            );

        if (!foundPath)
            return false;

        if (path.Count < 2)
            return false;

        int steps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );

        if (steps <= 0)
            return false;

        ConsumeMovement();

        StartCoroutine(
            ExecuteMoveRoutine(
                path,
                steps
            )
        );

        return true;
    }

    // ============================================================
    // ENEMY AI MOVEMENT
    // ============================================================

    public void TryMoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
            return;

        StartCoroutine(
            MoveTowardsEnemy()
        );
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

        // --------------------------------------------------------
        // FIND TARGET
        // --------------------------------------------------------

        AttackUnit target =
            manager.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally()
            );

        if (target == null)
            yield break;

        // --------------------------------------------------------
        // POSITIONS
        // --------------------------------------------------------

        Vector2Int currentPosition =
            GetCurrentTile();

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        // --------------------------------------------------------
        // CHECK RANGE
        // --------------------------------------------------------

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally()
            );

        if (distance <= attackRange)
            yield break;

        // --------------------------------------------------------
        // FIND ATTACK POSITION
        // --------------------------------------------------------

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions
            );

        if (attackPosition == currentPosition)
            yield break;

        // --------------------------------------------------------
        // FIND PATH
        // --------------------------------------------------------

        List<Vector2Int> path =
            new List<Vector2Int>(32);

        bool foundPath =
            manager.FindPath(
                currentPosition,
                attackPosition,
                CanWalkDiagonally(),
                path,
                gameObject
            );

        if (!foundPath)
            yield break;

        if (path.Count < 2)
            yield break;

        // --------------------------------------------------------
        // AVAILABLE MOVEMENT
        // --------------------------------------------------------

        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );

        if (availableSteps <= 0)
            yield break;

        // --------------------------------------------------------
        // CONSUME MOVEMENT
        // --------------------------------------------------------

        ConsumeMovement();

        // --------------------------------------------------------
        // MOVE
        // --------------------------------------------------------

        yield return ExecuteMoveRoutine(
            path,
            availableSteps
        );
    }

    // ============================================================
    // MOVE ROUTINE
    // ============================================================

    private IEnumerator ExecuteMoveRoutine(
        List<Vector2Int> path,
        int steps)
    {
        if (path == null)
            yield break;

        if (path.Count < 2)
            yield break;

        if (steps <= 0)
            yield break;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
            yield break;

        isMoving = true;

        int actualSteps =
            Mathf.Min(
                steps,
                path.Count - 1
            );

        for (int i = 1;
             i <= actualSteps;
             i++)
        {
            Vector2Int fromTile =
                path[i - 1];

            Vector2Int toTile =
                path[i];

            // ----------------------------------------------------
            // UPDATE GRID OCCUPANCY FIRST
            // ----------------------------------------------------

            if (!gridManager.StartMoveUnit(
                    gameObject,
                    fromTile,
                    toTile))
            {
                break;
            }

            Vector3 startPosition =
                gridManager.GridToWorldPosition(
                    fromTile
                );

            Vector3 endPosition =
                gridManager.GridToWorldPosition(
                    toTile
                );

            // ----------------------------------------------------
            // ANIMATION
            // ----------------------------------------------------

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / moveDuration
                    );

                transform.position =
                    Vector3.Lerp(
                        startPosition,
                        endPosition,
                        t
                    );

                yield return null;
            }

            transform.position =
                endPosition;

            // ----------------------------------------------------
            // FINISH GRID MOVE
            // ----------------------------------------------------

            gridManager.FinishMoveUnit(
                gameObject,
                toTile
            );

            // ----------------------------------------------------
            // UNIT TILE PIN
            // ----------------------------------------------------
            //
            // Your UnitTilePin does not have Refresh(),
            // so we intentionally do not call it here.
            //
            // If UnitTilePin updates automatically, nothing
            // else is needed.
            // ----------------------------------------------------

            yield return null;
        }

        isMoving = false;
    }

    // ============================================================
    // PREVIEW PATH
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

        Vector2Int start =
            GetCurrentTile();

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
            gameObject
        );
    }
}