using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }


    // ==================================================
    // UNIT TURN
    // ==================================================

    public string TryExecuteUnitTurn(
        AttackUnit unit,
        GameObject target)
    {
        if (unit == null ||
            unit.IsDead())
        {
            return string.Empty;
        }

        return ProcessMovementAndAttack(
            unit,
            target
        );
    }


    // ==================================================
    // PROCESS TURN
    // ==================================================

    public string ProcessMovementAndAttack(
        AttackUnit unit,
        GameObject target)
    {
        if (unit == null ||
            unit.IsDead())
        {
            return string.Empty;
        }

        // ==================================================
        // GET ATTACK BRAIN
        // ==================================================

        UnitAttackBrain attackBrain =
            unit.GetComponent<UnitAttackBrain>();

        if (attackBrain == null)
        {
            Debug.LogWarning(
                $"[PlayerAction] {unit.name} " +
                $"has no UnitAttackBrain.",
                unit
            );
        }


        // ==================================================
        // 1. ATTACK TARGET IF POSSIBLE
        // ==================================================

        if (attackBrain != null &&
            target != null &&
            attackBrain.CanAttackTarget(target))
        {
            int attacks =
                ExecuteAllAttacks(attackBrain);

            if (attacks > 0)
            {
                return attackBrain.GetPrimaryAbilityName();
            }
        }


        // ==================================================
        // GET MOVEMENT BRAIN
        // ==================================================

        UnitMoveBrain moveBrain =
            unit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            Debug.LogWarning(
                $"[PlayerAction] {unit.name} " +
                $"has no UnitMoveBrain.",
                unit
            );

            // Attack anything already in range.

            if (attackBrain != null)
            {
                int attacks =
                    ExecuteAllAttacks(attackBrain);

                return attacks > 0
                    ? attackBrain.GetPrimaryAbilityName()
                    : string.Empty;
            }

            return string.Empty;
        }


        // ==================================================
        // MOVE
        // ==================================================

        bool moved =
            moveBrain.TryMoveTowardsEnemy();


        // ==================================================
        // ATTACK AFTER MOVEMENT
        // ==================================================

        int totalAttacks = 0;

        if (attackBrain != null)
        {
            totalAttacks =
                ExecuteAllAttacks(attackBrain);
        }


        // ==================================================
        // RESULT
        // ==================================================

        if (totalAttacks > 0)
        {
            return attackBrain.GetPrimaryAbilityName();
        }

        if (moved)
        {
            return "Move";
        }

        return string.Empty;
    }


    // ==================================================
    // EXECUTE ALL ATTACKS
    // ==================================================

    private int ExecuteAllAttacks(
        UnitAttackBrain attackBrain)
    {
        if (attackBrain == null)
        {
            return 0;
        }

        return attackBrain.UseAllAvailableAbilities();
    }


    // ==================================================
    // SINGLE ATTACK
    // ==================================================

    private string ExecuteAttack(
        UnitAttackBrain attackBrain,
        GameObject target)
    {
        if (attackBrain == null ||
            target == null)
        {
            return string.Empty;
        }

        AbilitySO ability =
            attackBrain.GetBestAbilityForTarget(
                target
            );

        if (ability == null)
        {
            return string.Empty;
        }

        string abilityName =
            ability.GetAbilityName();

        AttackUnit attackUnit =
            attackBrain.GetAttackUnit();

        if (attackUnit == null)
        {
            return string.Empty;
        }

        bool success =
            attackUnit.Attack(
                target,
                ability
            );

        if (!success)
        {
            return string.Empty;
        }

        return abilityName;
    }
}