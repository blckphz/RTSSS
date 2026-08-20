using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit = GetComponent<AttackUnit>();
        }
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

        List<AbilitySO> abilities = attackUnit.GetAbilities();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability != null && attackUnit.IsAbilityReady(ability))
            {
                return ability.GetAbilityName();
            }
        }

        if (abilities.Count > 0 && abilities[0] != null)
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

        List<AbilitySO> abilities = attackUnit.GetAbilities();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability != null && attackUnit.IsAbilityReady(ability))
            {
                return ability.GetRange();
            }
        }

        return attackUnit.GetMaximumAttackRange();
    }

    // ==================================================
    // CAN ATTACK
    // ==================================================

    public bool CanAttackTarget(GameObject target)
    {
        if (attackUnit == null || !attackUnit.CanAttack() || !attackUnit.IsValidTarget(target))
        {
            return false;
        }

        return GetBestAbilityForTarget(target) != null;
    }

    // ==================================================
    // SINGLE ATTACK
    // ==================================================

    public bool Attack(GameObject target)
    {
        if (attackUnit == null || !CanAttackTarget(target))
        {
            return false;
        }

        AbilitySO ability = GetBestAbilityForTarget(target);

        if (ability == null)
        {
            return false;
        }

        return attackUnit.Attack(target, ability);
    }

    public bool UsePrimaryAbility(GameObject target)
    {
        return Attack(target);
    }

    // ==================================================
    // BEST ABILITY
    // ==================================================

    public AbilitySO GetBestAbilityForTarget(GameObject target)
    {
        if (attackUnit == null || !attackUnit.IsValidTarget(target))
        {
            return null;
        }

        GridManager gridManager = attackUnit.GetGridManager();

        if (gridManager == null)
        {
            return null;
        }

        List<AbilitySO> abilities = attackUnit.GetAbilities();

        AbilitySO bestAbility = null;
        int bestDamage = int.MinValue;
        int bestRange = int.MinValue;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability == null || !attackUnit.IsAbilityReady(ability))
            {
                continue;
            }

            if (!ability.CanHit(gridManager, gameObject, target))
            {
                continue;
            }

            int damage = ability.GetDamage();
            int range = ability.GetRange();

            if (bestAbility == null || damage > bestDamage || (damage == bestDamage && range > bestRange))
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

    public GameObject FindTargetForAbility(AbilitySO ability)
    {
        if (attackUnit == null || ability == null || !attackUnit.IsAbilityReady(ability))
        {
            return null;
        }

        GridManager gridManager = attackUnit.GetGridManager();

        if (gridManager == null)
        {
            return null;
        }

        List<AttackUnit> allUnits = CombatUtility.GetAllAliveUnits();

        GameObject bestTarget = null;
        int bestDistance = int.MaxValue;

        Vector2Int myPosition = gridManager.WorldToGridPosition(transform.position);

        for (int i = 0; i < allUnits.Count; i++)
        {
            AttackUnit otherUnit = allUnits[i];

            if (otherUnit == null || otherUnit == attackUnit)
            {
                continue;
            }

            GameObject target = otherUnit.gameObject;

            if (!attackUnit.IsValidTarget(target) || !ability.CanHit(gridManager, gameObject, target))
            {
                continue;
            }

            Vector2Int targetPosition = gridManager.WorldToGridPosition(target.transform.position);
            int distance = gridManager.GetDistance(myPosition, targetPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    // ==================================================
    // MULTI ATTACK
    // ==================================================

    public int UseAllAvailableAbilities()
    {
        if (attackUnit == null || !attackUnit.CanAttack())
        {
            return 0;
        }

        int attacksPerformed = 0;

        while (!attackUnit.IsDead())
        {
            AbilitySO bestAbility = FindBestAvailableAbility(out GameObject target);

            if (bestAbility == null || target == null)
            {
                break;
            }

            bool success = attackUnit.Attack(target, bestAbility);

            if (!success)
            {
                break;
            }

            attacksPerformed++;
        }

        return attacksPerformed;
    }

    // ==================================================
    // MULTI ATTACK COROUTINE
    // ==================================================

    public IEnumerator UseAllAvailableAbilitiesCoroutine()
    {
        if (attackUnit == null || !attackUnit.CanAttack())
        {
            yield break;
        }

        while (!attackUnit.IsDead())
        {
            AbilitySO bestAbility = FindBestAvailableAbility(out GameObject target);

            if (bestAbility == null || target == null)
            {
                break;
            }

            bool success = attackUnit.Attack(target, bestAbility);

            if (!success)
            {
                break;
            }

            float duration = bestAbility.GetUseDuration();

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }
        }
    }

    // ==================================================
    // FIND BEST AVAILABLE
    // ==================================================

    private AbilitySO FindBestAvailableAbility(out GameObject bestTarget)
    {
        bestTarget = null;

        if (attackUnit == null)
        {
            return null;
        }

        List<AbilitySO> abilities = attackUnit.GetAbilities();

        AbilitySO bestAbility = null;
        int bestDamage = int.MinValue;
        int bestRange = int.MinValue;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability == null || !attackUnit.IsAbilityReady(ability))
            {
                continue;
            }

            GameObject target = FindTargetForAbility(ability);

            if (target == null)
            {
                continue;
            }

            int damage = ability.GetDamage();
            int range = ability.GetRange();

            if (bestAbility == null || damage > bestDamage || (damage == bestDamage && range > bestRange))
            {
                bestAbility = ability;
                bestTarget = target;

                bestDamage = damage;
                bestRange = range;
            }
        }

        return bestAbility;
    }

    // ==================================================
    // TARGET SEARCH
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
        if (attackUnit == null || !attackUnit.CanAttack())
        {
            return false;
        }

        List<AbilitySO> abilities = attackUnit.GetAbilities();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability == null || !attackUnit.IsAbilityReady(ability))
            {
                continue;
            }

            if (FindTargetForAbility(ability) != null)
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