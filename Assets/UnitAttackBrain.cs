using UnityEngine;

public class UnitAttackBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;

    [Header("Debug")]
    [SerializeField]
    private bool debugBrain = true;

    // ==================================================
    // DEBUG
    // ==================================================

    private void BrainDebug(string message)
    {
        if (!debugBrain)
        {
            return;
        }

        Debug.Log(
            $"[UnitAttackBrain] {gameObject.name}: {message}",
            gameObject
        );
    }

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit =
                GetComponent<AttackUnit>();
        }

        BrainDebug(
            $"Awake. AttackUnit=" +
            $"{(attackUnit != null ? "FOUND" : "NULL")}"
        );
    }

    // ==================================================
    // PRIMARY ABILITY
    // ==================================================

    public string GetPrimaryAbilityName()
    {
        if (attackUnit == null)
        {
            return "Basic Attack";
        }

        var abilities =
            attackUnit.GetAbilities();

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability != null &&
                attackUnit.IsAbilityReady(
                    ability))
            {
                return ability.GetAbilityName();
            }
        }

        if (abilities.Count > 0 &&
            abilities[0] != null)
        {
            return abilities[0].GetAbilityName();
        }

        return "Basic Attack";
    }

    public int GetPrimaryAbilityRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }

        var abilities =
            attackUnit.GetAbilities();

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability != null &&
                attackUnit.IsAbilityReady(
                    ability))
            {
                return ability.GetRange();
            }
        }

        return attackUnit.GetMaximumAttackRange();
    }

    // ==================================================
    // CAN ATTACK
    // ==================================================

    public bool CanAttackTarget(
        GameObject target)
    {
        if (attackUnit == null)
        {
            return false;
        }

        if (!attackUnit.CanAttack())
        {
            return false;
        }

        if (!attackUnit.IsValidTarget(
                target))
        {
            return false;
        }

        return GetBestAbilityForTarget(
            target
        ) != null;
    }

    // ==================================================
    // ATTACK
    // ==================================================

    public bool Attack(
        GameObject target)
    {
        if (attackUnit == null)
        {
            return false;
        }

        BrainDebug(
            $"Attack requested against " +
            $"{(target != null ? target.name : "NULL")}"
        );

        if (!CanAttackTarget(target))
        {
            BrainDebug(
                "Attack rejected: no usable ability."
            );

            return false;
        }

        AbilitySO ability =
            GetBestAbilityForTarget(
                target
            );

        if (ability == null)
        {
            return false;
        }

        BrainDebug(
            $"Selected ability " +
            $"'{ability.GetAbilityName()}' " +
            $"against '{target.name}'."
        );

        return attackUnit.Attack(
            target,
            ability
        );
    }

    public bool UsePrimaryAbility(
        GameObject target)
    {
        return Attack(target);
    }

    // ==================================================
    // BEST ABILITY
    // ==================================================

    public AbilitySO GetBestAbilityForTarget(
        GameObject target)
    {
        if (attackUnit == null)
        {
            return null;
        }

        if (!attackUnit.IsValidTarget(
                target))
        {
            return null;
        }

        GridManager gridManager =
            attackUnit.GetGridManager();

        if (gridManager == null)
        {
            return null;
        }

        var abilities =
            attackUnit.GetAbilities();

        AbilitySO bestAbility = null;

        int bestDamage =
            int.MinValue;

        int bestRange =
            int.MinValue;

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability == null)
            {
                continue;
            }

            if (!attackUnit.IsAbilityReady(
                    ability))
            {
                continue;
            }

            if (!ability.CanHit(
                    gridManager,
                    gameObject,
                    target))
            {
                continue;
            }

            int damage =
                ability.GetDamage();

            int range =
                ability.GetRange();

            if (damage > bestDamage ||
                (damage == bestDamage &&
                 range > bestRange))
            {
                bestAbility = ability;

                bestDamage = damage;
                bestRange = range;
            }
        }

        return bestAbility;
    }

    // ==================================================
    // FIND TARGET
    // ==================================================

    public GameObject FindTargetForAbility(
        AbilitySO ability)
    {
        if (attackUnit == null ||
            ability == null)
        {
            return null;
        }

        if (!attackUnit.IsAbilityReady(
                ability))
        {
            return null;
        }

        GridManager gridManager =
            attackUnit.GetGridManager();

        if (gridManager == null)
        {
            return null;
        }

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        GameObject bestTarget = null;

        int bestDistance =
            int.MaxValue;

        Vector2Int myPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );

        for (
            int i = 0;
            i < allUnits.Length;
            i++
        )
        {
            AttackUnit otherUnit =
                allUnits[i];

            if (otherUnit == null ||
                otherUnit == attackUnit ||
                otherUnit.IsDead())
            {
                continue;
            }

            GameObject target =
                otherUnit.gameObject;

            if (!attackUnit.IsValidTarget(
                    target))
            {
                continue;
            }

            if (!ability.CanHit(
                    gridManager,
                    gameObject,
                    target))
            {
                continue;
            }

            Vector2Int targetPosition =
                gridManager.WorldToGridPosition(
                    target.transform.position
                );

            int distance =
                gridManager.GetDistance(
                    myPosition,
                    targetPosition
                );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    // ==================================================
    // USE ALL AVAILABLE ABILITIES
    // ==================================================

    public int UseAllAvailableAbilities()
    {
        if (attackUnit == null ||
            !attackUnit.CanAttack())
        {
            return 0;
        }

        int attacksPerformed = 0;

        BrainDebug(
            "========================================"
        );

        BrainDebug(
            "STARTING MULTI-ABILITY ATTACK PHASE"
        );

        bool performedAttack;

        do
        {
            performedAttack = false;

            if (attackUnit.IsDead())
            {
                break;
            }

            var abilities =
                attackUnit.GetAbilities();

            AbilitySO bestAbility = null;

            GameObject bestTarget = null;

            int bestDamage =
                int.MinValue;

            int bestRange =
                int.MinValue;

            for (
                int i = 0;
                i < abilities.Count;
                i++
            )
            {
                AbilitySO ability =
                    abilities[i];

                if (ability == null)
                {
                    continue;
                }

                if (!attackUnit.IsAbilityReady(
                        ability))
                {
                    continue;
                }

                GameObject target =
                    FindTargetForAbility(
                        ability
                    );

                if (target == null)
                {
                    continue;
                }

                int damage =
                    ability.GetDamage();

                int range =
                    ability.GetRange();

                if (bestAbility == null ||
                    damage > bestDamage ||
                    (damage == bestDamage &&
                     range > bestRange))
                {
                    bestAbility = ability;
                    bestTarget = target;

                    bestDamage = damage;
                    bestRange = range;
                }
            }

            if (bestAbility == null ||
                bestTarget == null)
            {
                break;
            }

            BrainDebug(
                $"Using ability " +
                $"'{bestAbility.GetAbilityName()}' " +
                $"against '{bestTarget.name}'. " +
                $"Uses remaining before attack: " +
                $"{GetUsesRemainingText(bestAbility)}"
            );

            if (attackUnit.Attack(
                    bestTarget,
                    bestAbility))
            {
                attacksPerformed++;

                performedAttack = true;

                BrainDebug(
                    $"Attack successful. " +
                    $"Uses remaining: " +
                    $"{GetUsesRemainingText(bestAbility)}"
                );
            }

        }
        while (performedAttack);

        BrainDebug(
            $"MULTI-ABILITY ATTACK PHASE COMPLETE. " +
            $"Attacks={attacksPerformed}"
        );

        BrainDebug(
            "========================================"
        );

        return attacksPerformed;
    }

    private string GetUsesRemainingText(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return "NULL";
        }

        if (ability.GetUsesPerTurn() <= 0)
        {
            return "Unlimited";
        }

        return attackUnit
            .GetAbilityUsesRemaining(ability)
            .ToString();
    }

    // ==================================================
    // TARGET SEARCH HELPERS
    // ==================================================

    public bool TryAttackAnyTargetInAbilityRange()
    {
        return UseAllAvailableAbilities() > 0;
    }

    public bool AttackAnyTargetInAbilityRange()
    {
        return TryAttackAnyTargetInAbilityRange();
    }

    public bool HasAnyTargetInAbilityRange()
    {
        if (attackUnit == null ||
            !attackUnit.CanAttack())
        {
            return false;
        }

        var abilities =
            attackUnit.GetAbilities();

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability == null ||
                !attackUnit.IsAbilityReady(
                    ability))
            {
                continue;
            }

            if (FindTargetForAbility(
                    ability) != null)
            {
                return true;
            }
        }

        return false;
    }

    // ==================================================
    // ACCESSORS
    // ==================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }
}