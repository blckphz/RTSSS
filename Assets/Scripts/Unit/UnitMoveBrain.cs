using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitMoveBrain : MonoBehaviour
{
    // ============================================================
    // PARTICLE EVENTS
    // ============================================================

    public static event System.Action<Vector3> OnWalkParticleTile;


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;

    [SerializeField]
    private UnitTilePin tilePin;

    [SerializeField]
    private AnimationController animationController;


    // ============================================================
    // MOVEMENT
    // ============================================================

    [Header("Movement")]
    [SerializeField]
    private float moveDuration = 0.08f;


    // ============================================================
    // AI TARGETING
    // ============================================================

    [Header("AI Targeting")]
    [SerializeField]
    private int attackRange = 1;

    [SerializeField]
    private bool preferLowHealthEnemies = true;

    [SerializeField]
    private bool preferCloserEnemies = true;


    // ============================================================
    // AI ATTACK POSITION
    // ============================================================

    [Header("AI Attack Position")]
    [SerializeField]
    private bool preferCloserAttackPosition = true;

    [SerializeField]
    private bool preferMoreOpenPositions = true;

    [SerializeField]
    private bool preferSidePositions = true;


    // ============================================================
    // STATE
    // ============================================================

    private bool isMoving;

    private bool movementConsumed;

    private int movementSequence;


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

        StopWalkAnimation();
    }


    // ============================================================
    // COMPONENTS
    // ============================================================

    private void EnsureComponents()
    {
        if (attackUnit == null)
        {
            attackUnit =
                GetComponent<AttackUnit>();
        }

        if (tilePin == null)
        {
            tilePin =
                GetComponent<UnitTilePin>();
        }

        if (animationController == null)
        {
            animationController =
                GetComponent<AnimationController>();
        }
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
        {
            return null;
        }

        return UnitMoveBrainManager.Instance.GetGridManager();
    }


    // ============================================================
    // CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null)
        {
            return null;
        }

        return attackUnit.GetCharacterData();
    }


    public int GetMoveRange()
    {
        CharacterSO characterData =
            GetCharacterData();

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
        CharacterSO characterData =
            GetCharacterData();

        if (characterData == null)
        {
            return false;
        }

        return characterData.canwalkdiagonally;
    }


    public bool CanAttackAfterMoving(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return false;
        }

        return GetMoveRange() > 0;
    }


    // ============================================================
    // CURRENT TILE
    // ============================================================

    public bool TryGetCurrentTile(
        out Vector2Int tile
    )
    {
        tile = Vector2Int.zero;

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            return false;
        }

        // UnitTilePin is the authoritative logical tile
        // whenever it has one.
        if (
            tilePin != null &&
            tilePin.HasTile()
        )
        {
            tile =
                tilePin.GetTile();

            return gridManager.IsInsideGrid(
                tile
            );
        }

        tile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        return gridManager.IsInsideGrid(
            tile
        );
    }


    public Vector2Int GetCurrentTile()
    {
        TryGetCurrentTile(
            out Vector2Int tile
        );

        return tile;
    }


    // ============================================================
    // PREDICTED MOVE TILE
    // ============================================================

    public bool TryGetPredictedMoveTile(
        out Vector2Int predictedTile
    )
    {
        predictedTile =
            GetCurrentTile();

        if (!CanUseAIMovement())
        {
            return false;
        }

        if (!CanMoveThisTurn())
        {
            return false;
        }

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
        {
            return false;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            return false;
        }

        AttackUnit target =
            manager.FindBestTarget(
                attackUnit,
                preferCloserEnemies,
                preferLowHealthEnemies,
                attackRange,
                CanWalkDiagonally()
            );

        if (target == null)
        {
            return false;
        }

        Vector2Int currentPosition =
            GetCurrentTile();

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally()
            );

        if (distance <= attackRange)
        {
            return false;
        }

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions,
                gameObject
            );

        if (attackPosition == currentPosition)
        {
            return false;
        }

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
        {
            return false;
        }

        if (path.Count < 2)
        {
            return false;
        }

        int availableSteps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );

        if (availableSteps <= 0)
        {
            return false;
        }

        predictedTile =
            path[availableSteps];

        return predictedTile != currentPosition;
    }


    // ============================================================
    // DIRECT MOVEMENT
    // ============================================================

    public bool TryMoveTo(
        Vector2Int destination
    )
    {
        if (!CanMoveThisTurn())
        {
            return false;
        }

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
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
            GetCurrentTile();

        if (start == destination)
        {
            return false;
        }

        if (!gridManager.IsInsideGrid(
                destination))
        {
            return false;
        }

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
        {
            return false;
        }

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
        {
            return false;
        }

        if (path.Count < 2)
        {
            return false;
        }

        int steps =
            Mathf.Min(
                GetMoveRange(),
                path.Count - 1
            );

        if (steps <= 0)
        {
            return false;
        }

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
    // ENEMY MOVEMENT
    // ============================================================

    public void TryMoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
        {
            return;
        }

        StartCoroutine(
            MoveTowardsEnemy()
        );
    }


    public IEnumerator MoveTowardsEnemy()
    {
        if (!CanMoveThisTurn())
        {
            yield break;
        }

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
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
            manager.FindBestTarget(
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
            GetCurrentTile();

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        int distance =
            manager.GetMovementDistance(
                currentPosition,
                targetPosition,
                CanWalkDiagonally()
            );

        if (distance <= attackRange)
        {
            yield break;
        }

        Vector2Int attackPosition =
            manager.FindBestAttackPosition(
                currentPosition,
                targetPosition,
                attackRange,
                CanWalkDiagonally(),
                preferCloserAttackPosition,
                preferMoreOpenPositions,
                preferSidePositions,
                gameObject
            );

        if (attackPosition == currentPosition)
        {
            yield break;
        }

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
        {
            yield break;
        }

        if (path.Count < 2)
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

        ConsumeMovement();

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
        int steps
    )
    {
        if (path == null)
        {
            yield break;
        }

        if (path.Count < 2)
        {
            yield break;
        }

        if (steps <= 0)
        {
            yield break;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            yield break;
        }

        isMoving = true;

        movementSequence++;

        int currentMovementSequence =
            movementSequence;

        Debug.Log(
            $"[WalkParticles][UnitMoveBrain] START -> {name} | Movement #{currentMovementSequence}",
            this
        );

        int actualSteps =
            Mathf.Min(
                steps,
                path.Count - 1
            );

        for (
            int i = 1;
            i <= actualSteps;
            i++
        )
        {
            Vector2Int fromTile =
                path[i - 1];

            Vector2Int toTile =
                path[i];


            // ====================================================
            // SET WALK DIRECTION
            // ====================================================

            Vector2Int direction =
                toTile - fromTile;

            if (animationController != null)
            {
                animationController.SetMovementDirection(
                    direction
                );
            }


            // ====================================================
            // START GRID MOVEMENT
            // ====================================================

            bool started =
                gridManager.StartMoveUnit(
                    gameObject,
                    fromTile,
                    toTile
                );

            if (!started)
            {
                Debug.LogWarning(
                    $"[WalkParticles][UnitMoveBrain] Grid movement failed -> {name} | Movement #{currentMovementSequence}",
                    this
                );

                break;
            }


            // ====================================================
            // WORLD POSITIONS
            // ====================================================

            Vector3 startPosition =
                gridManager.GridToWorldPosition(
                    fromTile
                );

            Vector3 endPosition =
                gridManager.GridToWorldPosition(
                    toTile
                );


            // ====================================================
            // ACTUAL MOVEMENT
            // ====================================================

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t;

                if (moveDuration <= 0f)
                {
                    t = 1f;
                }
                else
                {
                    t =
                        Mathf.Clamp01(
                            elapsed /
                            moveDuration
                        );
                }

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


            // ====================================================
            // FINISH GRID MOVEMENT
            // ====================================================

            gridManager.FinishMoveUnit(
                gameObject,
                toTile
            );


            // ====================================================
            // UPDATE TILE PIN
            // ====================================================

            if (tilePin != null)
            {
                tilePin.SetTile(
                    toTile
                );
            }


            // ====================================================
            // UPDATE LOGICAL UNIT POSITION
            // ====================================================

            if (attackUnit != null)
            {
                attackUnit.SetLogicalGridPosition(
                    toTile
                );
            }


            // ====================================================
            // SPAWN PARTICLE AT THIS TILE
            // ====================================================

            Vector3 particlePosition =
                gridManager.GridToWorldPosition(
                    toTile
                );

            OnWalkParticleTile?.Invoke(
                particlePosition
            );


            // ====================================================
            // NEXT TILE
            // ====================================================

            yield return null;
        }


        // ========================================================
        // MOVEMENT FINISHED
        // ========================================================

        Debug.Log(
            $"[WalkParticles][UnitMoveBrain] STOP -> {name} | Movement #{currentMovementSequence}",
            this
        );

        StopWalkAnimation();

        isMoving = false;
    }


    // ============================================================
    // WALK ANIMATION
    // ============================================================

    private void UpdateWalkAnimation(
        Vector2Int direction
    )
    {
        if (animationController == null)
        {
            return;
        }

        animationController.SetMovementDirection(
            direction
        );
    }


    // ============================================================
    // STOP WALK ANIMATION
    // ============================================================

    private void StopWalkAnimation()
    {
        if (animationController == null)
        {
            return;
        }

        animationController.PlayIdle();
    }


    // ============================================================
    // PREVIEW PATH
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

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
        {
            return false;
        }

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
        List<Vector2Int> result
    )
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        UnitMoveBrainManager manager =
            UnitMoveBrainManager.Instance;

        if (manager == null)
        {
            return;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            return;
        }

        manager.GetReachableCells(
            GetCurrentTile(),
            GetMoveRange(),
            CanWalkDiagonally(),
            result,
            gameObject
        );
    }
}