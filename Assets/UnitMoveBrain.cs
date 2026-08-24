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
    [SerializeField]
    private AttackUnit attackUnit;

    [SerializeField]
    private UnitTilePin tilePin;


    // ============================================================
    // MOVEMENT
    // ============================================================

    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    private float moveDuration = 0.08f;


    // ============================================================
    // AI CONFIG
    // ============================================================

    [Header("AI Config")]
    [SerializeField, Min(1)]
    private int attackRange = 1;

    [SerializeField]
    private bool preferLowHealthEnemies = true;

    [SerializeField]
    private bool preferCloserEnemies = true;


    // ============================================================
    // TACTICAL CONFIG
    // ============================================================

    [Header("Tactical Config")]
    [SerializeField]
    private bool preferCloserAttackPosition = true;

    [SerializeField]
    private bool preferMoreOpenPositions = true;

    [SerializeField]
    private bool preferSidePositions = true;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Movement Debug")]
    [SerializeField]
    private bool enableMovementDebug = true;

    [SerializeField]
    private bool logMovementRequests = true;

    [SerializeField]
    private bool logMovementSteps = true;

    [SerializeField]
    private bool logPathResults = true;

    [SerializeField]
    private bool logGridSynchronization = true;

    [SerializeField]
    private bool logMovementWarnings = true;


    // ============================================================
    // STATE
    // ============================================================

    private bool isMoving;
    private bool movementConsumed;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit = GetComponent<AttackUnit>();
        }

        if (tilePin == null)
        {
            tilePin = GetComponent<UnitTilePin>();
        }

        movementConsumed = false;
        isMoving = false;

        DebugLog(
            "Awake | " +
            "MoveRange=" + GetMoveRange() +
            " | Diagonal=" + CanWalkDiagonally()
        );
    }


    private void OnEnable()
    {
        if (tilePin == null)
        {
            tilePin = GetComponent<UnitTilePin>();
        }
    }


    private void OnDisable()
    {
        isMoving = false;
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(string message)
    {
        if (!enableMovementDebug)
        {
            return;
        }

        Debug.Log(
            "[UnitMoveBrain] " +
            gameObject.name +
            " | " +
            message,
            this
        );
    }


    private void DebugWarning(string message)
    {
        if (!enableMovementDebug || !logMovementWarnings)
        {
            return;
        }

        Debug.LogWarning(
            "[UnitMoveBrain] " +
            gameObject.name +
            " | " +
            message,
            this
        );
    }


    // ============================================================
    // STATE GETTERS
    // ============================================================

    public bool CanUseAIMovement()
    {
        return
            attackUnit != null &&
            !attackUnit.IsDead() &&
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

        DebugLog("Movement consumed.");
    }


    public void ResetMovement()
    {
        movementConsumed = false;

        DebugLog("Movement reset.");
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


    // ============================================================
    // GRID MANAGER
    // ============================================================

    public GridManager GetGridManager()
    {
        if (UnitMoveBrainManager.Instance == null)
        {
            DebugWarning(
                "UnitMoveBrainManager.Instance is NULL."
            );

            return null;
        }

        return
            UnitMoveBrainManager.Instance
                .GetGridManager();
    }


    // ============================================================
    // IMPORTANT:
    // GET CURRENT LOGICAL TILE
    //
    // UnitTilePin is the authoritative source.
    // Do NOT use transform.position here.
    // ============================================================

    public bool TryGetCurrentTile(
        out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        if (tilePin == null)
        {
            tilePin =
                GetComponent<UnitTilePin>();
        }

        if (tilePin != null &&
            tilePin.HasTile())
        {
            tile =
                tilePin.GetTile();

            return true;
        }

        GridManager gridManager =
            GetGridManager();

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
        TryGetCurrentTile(
            out Vector2Int tile
        );

        return tile;
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


    // ============================================================
    // MOVE RANGE
    // ============================================================

    public int GetMoveRange()
    {
        CharacterSO data =
            GetCharacterData();

        if (data == null)
        {
            DebugWarning(
                "CharacterSO is NULL."
            );

            return 0;
        }

        return Mathf.Max(
            0,
            data.moveRange
        );
    }


    // ============================================================
    // DIAGONAL MOVEMENT
    // ============================================================

    public bool CanWalkDiagonally()
    {
        CharacterSO data =
            GetCharacterData();

        if (data == null)
        {
            return false;
        }

        return data.canwalkdiagonally;
    }


    // ============================================================
    // CAN MOVE
    // ============================================================

    private bool CanMove()
    {
        return
            attackUnit != null &&
            !attackUnit.IsDead();
    }


    // ============================================================
    // ABILITY AFTER MOVEMENT
    // ============================================================

    public bool CanAttackAfterMoving(
        AbilitySO ability)
    {
        if (
            attackUnit == null ||
            ability == null
        )
        {
            return false;
        }

        return
            !movementConsumed ||
            ability.CanAttackWithThisAfterMove();
    }


    // ============================================================
    // DIRECT MOVE
    // ============================================================

    public bool TryMoveTo(
        Vector2Int destination)
    {
        if (!CanMoveThisTurn())
        {
            DebugWarning(
                "TryMoveTo rejected | " +
                "Moving=" + isMoving +
                " | Consumed=" + movementConsumed
            );

            return false;
        }

        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            return false;
        }

        // IMPORTANT:
        // Use logical tile instead of transform.position.
        if (!TryGetCurrentTile(
                out Vector2Int start))
        {
            DebugWarning(
                "Move rejected | Could not determine current tile."
            );

            return false;
        }

        if (logMovementRequests)
        {
            DebugLog(
                "Move request | " +
                "Start=" + start +
                " | Destination=" + destination +
                " | World=" + transform.position
            );
        }

        if (!gridManager.IsInsideGrid(destination))
        {
            DebugWarning(
                "Move rejected | Destination outside grid: " +
                destination
            );

            return false;
        }

        if (start == destination)
        {
            DebugWarning(
                "Move rejected | Start equals destination."
            );

            return false;
        }

        GameObject occupant =
            gridManager.GetUnitAt(destination);

        if (
            occupant != null &&
            occupant != gameObject
        )
        {
            DebugWarning(
                "Move rejected | Destination occupied by " +
                occupant.name
            );

            return false;
        }

        int moveRange =
            GetMoveRange();

        if (moveRange <= 0)
        {
            DebugWarning(
                "Move rejected | Move range is zero."
            );

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
            DebugWarning(
                "Move rejected | No path."
            );

            return false;
        }

        int steps =
            path.Count - 1;

        if (logPathResults)
        {
            DebugLog(
                "Path found | " +
                "Steps=" + steps +
                " | Path=" + FormatPath(path)
            );
        }

        if (steps <= 0)
        {
            return false;
        }

        if (steps > moveRange)
        {
            DebugWarning(
                "Move rejected | " +
                "Steps=" + steps +
                " > MoveRange=" + moveRange
            );

            return false;
        }

        StartCoroutine(
            ExecuteMoveRoutine(
                path,
                steps
            )
        );

        return true;
    }


    // ============================================================
    // AI MOVEMENT
    // ============================================================

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
    // MOVE TOWARDS ENEMY
    // ============================================================

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
            DebugWarning(
                "AI movement | No target found."
            );

            yield break;
        }

        // IMPORTANT:
        // Get own position from UnitTilePin.
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

        DebugLog(
            "AI movement | " +
            "Current=" + currentPos +
            " | Target=" + target.name +
            " | TargetTile=" + targetPos +
            " | Distance=" + distance
        );

        if (distance <= attackRange)
        {
            DebugLog(
                "AI movement cancelled | Already in attack range."
            );

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

        DebugLog(
            "AI attack position | " +
            "Selected=" + attackPos
        );

        if (attackPos == currentPos)
        {
            DebugLog(
                "AI movement cancelled | Already at best position."
            );

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

        if (
            !foundPath ||
            path.Count < 2
        )
        {
            DebugWarning(
                "AI movement failed | No path to attack position."
            );

            yield break;
        }

        int moveRange =
            GetMoveRange();

        int availableSteps =
            Mathf.Min(
                moveRange,
                path.Count - 1
            );

        DebugLog(
            "AI movement path | " +
            "Steps=" + (path.Count - 1) +
            " | Taking=" + availableSteps +
            " | Path=" + FormatPath(path)
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
    // EXECUTE MOVEMENT
    // ============================================================

    private IEnumerator ExecuteMoveRoutine(
        List<Vector2Int> path,
        int stepsToTake)
    {
        GridManager gridManager =
            GetGridManager();

        if (gridManager == null)
        {
            yield break;
        }

        if (
            path == null ||
            path.Count < 2 ||
            stepsToTake <= 0
        )
        {
            yield break;
        }

        isMoving = true;
        ConsumeMovement();

        Vector2Int lastLogicalTile =
            path[0];

        DebugLog(
            "MOVEMENT START | " +
            "LogicalStart=" + lastLogicalTile +
            " | WorldStart=" + transform.position +
            " | Steps=" + stepsToTake
        );

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
                i++
            )
            {
                if (!CanMove())
                {
                    DebugWarning(
                        "Movement interrupted | Unit cannot move."
                    );

                    break;
                }

                Vector2Int currentPosition =
                    lastLogicalTile;

                Vector2Int nextPosition =
                    path[i];

                if (logMovementSteps)
                {
                    DebugLog(
                        "STEP START | " +
                        "Step=" + i +
                        "/" + actualSteps +
                        " | LogicalCurrent=" + currentPosition +
                        " | LogicalNext=" + nextPosition +
                        " | VisualWorld=" + transform.position
                    );
                }

                if (
                    !gridManager.IsInsideGrid(
                        nextPosition
                    )
                )
                {
                    DebugWarning(
                        "Step rejected | Outside grid: " +
                        nextPosition
                    );

                    break;
                }

                GameObject occupant =
                    gridManager.GetUnitAt(
                        nextPosition
                    );

                if (
                    occupant != null &&
                    occupant != gameObject
                )
                {
                    DebugWarning(
                        "Step rejected | " +
                        nextPosition +
                        " occupied by " +
                        occupant.name
                    );

                    break;
                }

                if (
                    !gridManager.StartMoveUnit(
                        gameObject,
                        currentPosition,
                        nextPosition
                    )
                )
                {
                    DebugWarning(
                        "StartMoveUnit FAILED | " +
                        currentPosition +
                        " -> " +
                        nextPosition
                    );

                    break;
                }

                if (logGridSynchronization)
                {
                    DebugLog(
                        "GRID RESERVED | " +
                        currentPosition +
                        " -> " +
                        nextPosition
                    );
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
                        DebugWarning(
                            "Movement interrupted during animation."
                        );

                        break;
                    }

                    elapsed += Time.deltaTime;

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

                lastLogicalTile =
                    nextPosition;

                if (tilePin != null)
                {
                    tilePin.UpdateTileAfterMovement(
                        nextPosition
                    );
                }

                transform.position =
                    gridManager.GridToWorldPosition(
                        nextPosition
                    );

                if (logGridSynchronization)
                {
                    DebugLog(
                        "STEP FINISHED | " +
                        "LogicalTile=" + nextPosition +
                        " | World=" + transform.position
                    );
                }

                yield return null;
            }
        }
        finally
        {
            isMoving = false;

            Vector2Int finalTile =
                lastLogicalTile;

            Vector3 finalWorld =
                gridManager.GridToWorldPosition(
                    finalTile
                );

            transform.position =
                finalWorld;

            if (tilePin != null)
            {
                tilePin.UpdateTileAfterMovement(
                    finalTile
                );
            }

            gridManager.FinishMoveUnit(
                gameObject,
                finalTile
            );

            DebugLog(
                "MOVEMENT END | " +
                "FinalLogicalTile=" + finalTile +
                " | FinalWorld=" + transform.position
            );
        }
    }


    // ============================================================
    // MOVEMENT PREVIEW PATH
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

        if (
            gridManager == null ||
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            return false;
        }

        // IMPORTANT:
        // Use UnitTilePin instead of visual position.
        if (!TryGetCurrentTile(
                out Vector2Int start))
        {
            return false;
        }

        if (!gridManager.IsInsideGrid(destination))
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

        if (
            occupant != null &&
            occupant != gameObject
        )
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

            if (logPathResults)
            {
                DebugLog(
                    "Preview path FAILED | " +
                    start +
                    " -> " +
                    destination
                );
            }

            return false;
        }

        int steps =
            result.Count - 1;

        if (
            steps <= 0 ||
            steps > GetMoveRange()
        )
        {
            result.Clear();
            return false;
        }

        return true;
    }


    // ============================================================
    // GET ALL REACHABLE CELLS
    // ============================================================

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

        if (
            gridManager == null ||
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            return;
        }

        // ========================================================
        // THIS IS THE IMPORTANT FIX
        //
        // Movement range is centred on UnitTilePin's logical tile.
        // This remains correct when the board rotates.
        // ========================================================

        if (!TryGetCurrentTile(
                out Vector2Int start))
        {
            DebugWarning(
                "Could not determine current logical tile."
            );

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

        // The unit's own tile is not a movement destination.
        result.Remove(start);

        DebugLog(
            "Reachable cells | " +
            "Start=" + start +
            " | Range=" + moveRange +
            " | Count=" + result.Count
        );
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

            builder.Append(path[i]);
        }

        return builder.ToString();
    }
}