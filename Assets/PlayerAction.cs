using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
    }

    // ==================================================
    // TURN EXECUTION
    // ==================================================

    /// <summary>
    /// Executes a unit's turn purely based on its active ability range.
    /// </summary>
    public void TryExecuteUnitTurn(AttackUnit unit, GameObject target)
    {
        if (unit == null || unit.IsDead()) return;

        if (target == null)
        {
            Debug.Log($"[PlayerAction] {unit.name}: No valid target found. Skipping turn.");
            return;
        }

        ProcessMovementAndAttack(unit, target);
    }

    // ==================================================
    // MOVEMENT & ATTACK LOGIC (ABILITY RANGE ONLY)
    // ==================================================

    public void ProcessMovementAndAttack(AttackUnit unit, GameObject target)
    {
        if (unit == null || target == null) return;

        Vector2Int currentPos = gridManager.WorldToGridPosition(unit.transform.position);
        Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);

        int distance = gridManager.GetDistance(currentPos, targetPos);
        int abilityRange = unit.GetPrimaryAbilityRange();

        Debug.Log($"[PlayerAction] {unit.name}: distance={distance}, abilityRange={abilityRange}.");

        // 1. ALREADY IN ABILITY RANGE -> ATTACK IMMEDIATELY
        if (distance <= abilityRange)
        {
            Debug.Log($"[PlayerAction] {unit.name}: Inside ability range. No movement needed.");
            ExecuteAttack(unit, target);
            return;
        }

        // 2. OUT OF ABILITY RANGE -> MOVE TOWARDS TARGET
        Vector2Int nextPos = GetNextStepTowards(currentPos, targetPos);

        // Attempt movement on the grid
        if (nextPos != currentPos && gridManager.MoveUnit(unit.gameObject, currentPos, nextPos))
        {
            int newDistance = gridManager.GetDistance(nextPos, targetPos);
            Debug.Log($"[PlayerAction] {unit.name}: Moved {currentPos} -> {nextPos}. Distance {distance} -> {newDistance}. AbilityRange={abilityRange}.");

            // 3. CHECK IF NOW IN ABILITY RANGE AFTER MOVING
            if (newDistance <= abilityRange)
            {
                Debug.Log($"[PlayerAction] {unit.name}: Target is now inside ability range after step. Executing attack!");
                ExecuteAttack(unit, target);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerAction] {unit.name}: Path blocked or movement failed.");
        }
    }

    // ==================================================
    // PATHFINDING / STEPPING
    // ==================================================

    /// <summary>
    /// Calculates the next adjacent tile towards the target (Manhattan step).
    /// </summary>
    private Vector2Int GetNextStepTowards(Vector2Int start, Vector2Int target)
    {
        Vector2Int bestStep = start;
        int currentDistance = gridManager.GetDistance(start, target);
        int bestDistance = currentDistance;

        // Check 4 adjacent directions (Up, Down, Left, Right)
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = start + dir;

            // Tile must be on grid and unblocked
            if (gridManager.IsInsideGrid(neighbor) && !gridManager.IsCellOccupied(neighbor))
            {
                int dist = gridManager.GetDistance(neighbor, target);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestStep = neighbor;
                }
            }
        }

        return bestStep;
    }

    // ==================================================
    // ATTACK EXECUTION
    // ==================================================

    private void ExecuteAttack(AttackUnit attacker, GameObject target)
    {
        if (attacker == null || target == null) return;

        HealthManager targetHealth = target.GetComponent<HealthManager>();
        if (targetHealth != null && !targetHealth.IsDead())
        {
            attacker.UsePrimaryAbility(target);
            Debug.Log($"[PlayerAction] {attacker.name} attacked {target.name} using primary ability.");
        }
    }
}