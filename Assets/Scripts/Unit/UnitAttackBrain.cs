using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (attackUnit == null)
        {
            attackUnit =
                GetComponent<AttackUnit>();
        }
    }


    // ============================================================
    // PRIMARY ABILITY
    // ============================================================

    public string GetPrimaryAbilityName()
    {
        if (attackUnit == null)
        {
            return "Basic Attack";
        }


        List<AbilitySO> abilities =
            attackUnit.GetAbilities();


        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];


            if (
                ability != null &&
                attackUnit.IsAbilityReady(
                    ability
                )
            )
            {
                return ability.GetAbilityName();
            }
        }


        if (
            abilities.Count > 0 &&
            abilities[0] != null
        )
        {
            return abilities[0].GetAbilityName();
        }


        return "Basic Attack";
    }


    // ============================================================
    // PRIMARY RANGE
    // ============================================================

    public int GetPrimaryAbilityRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }


        List<AbilitySO> abilities =
            attackUnit.GetAbilities();


        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];


            if (
                ability != null &&
                attackUnit.IsAbilityReady(
                    ability
                )
            )
            {
                return ability.GetRange();
            }
        }


        return attackUnit.GetMaximumAttackRange();
    }


    // ============================================================
    // CAN ATTACK TARGET
    // ============================================================

    public bool CanAttackTarget(
        GameObject target)
    {
        if (
            attackUnit == null ||
            attackUnit.IsDead() ||
            !attackUnit.CanAttack() ||
            !attackUnit.IsValidTarget(target)
        )
        {
            return false;
        }


        return
            GetBestAbilityForTarget(target) != null;
    }


    // ============================================================
    // SINGLE ATTACK
    // ============================================================

    public bool Attack(
        GameObject target)
    {
        if (
            attackUnit == null ||
            !CanAttackTarget(target)
        )
        {
            return false;
        }


        AbilitySO ability =
            GetBestAbilityForTarget(target);


        if (ability == null)
        {
            return false;
        }


        return attackUnit.Attack(
            target,
            ability
        );
    }


    // ============================================================
    // PRIMARY ABILITY
    // ============================================================

    public bool UsePrimaryAbility(
        GameObject target)
    {
        return Attack(target);
    }


    // ============================================================
    // ATTACK TILE
    // ============================================================

    public bool UseAbilityAtTile(
        AbilitySO ability,
        Vector2Int targetTile)
    {
        if (
            attackUnit == null ||
            ability == null ||
            attackUnit.IsDead()
        )
        {
            return false;
        }


        GridManager gridManager =
            attackUnit.GetGridManager();


        if (gridManager == null)
        {
            return false;
        }


        if (!gridManager.IsInsideGrid(
                targetTile))
        {
            return false;
        }


        if (!attackUnit.IsAbilityReady(
                ability))
        {
            return false;
        }


        if (!ability.CanHitTile(
                gridManager,
                gameObject,
                targetTile))
        {
            return false;
        }


        // ========================================================
        // FIND UNIT ON TILE
        // ========================================================

        List<AttackUnit> units =
            CombatUtility.GetAllAliveUnits();


        GameObject target =
            null;


        for (
            int i = 0;
            i < units.Count;
            i++
        )
        {
            AttackUnit unit =
                units[i];


            if (
                unit == null ||
                unit == attackUnit
            )
            {
                continue;
            }


            Vector2Int unitPosition =
                gridManager.WorldToGridPosition(
                    unit.transform.position
                );


            if (unitPosition == targetTile)
            {
                target =
                    unit.gameObject;


                break;
            }
        }


        // ========================================================
        // TARGET FOUND
        // ========================================================

        if (target != null)
        {
            if (!attackUnit.IsValidTarget(
                    target))
            {
                return false;
            }


            return attackUnit.Attack(
                target,
                ability
            );
        }


        // ========================================================
        // EMPTY TILE
        // ========================================================

        return attackUnit.AttackAtTile(
            targetTile,
            ability
        );
    }


    // ============================================================
    // BEST ABILITY FOR TARGET
    // ============================================================

    public AbilitySO GetBestAbilityForTarget(
        GameObject target)
    {
        if (
            attackUnit == null ||
            !attackUnit.IsValidTarget(target)
        )
        {
            return null;
        }


        GridManager gridManager =
            attackUnit.GetGridManager();


        if (gridManager == null)
        {
            return null;
        }


        List<AbilitySO> abilities =
            attackUnit.GetAbilities();


        AbilitySO bestAbility =
            null;


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


            if (
                ability == null ||
                !attackUnit.IsAbilityReady(
                    ability
                )
            )
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


            if (
                bestAbility == null ||
                damage > bestDamage ||
                (
                    damage == bestDamage &&
                    range > bestRange
                )
            )
            {
                bestAbility =
                    ability;


                bestDamage =
                    damage;


                bestRange =
                    range;
            }
        }


        return bestAbility;
    }


    // ============================================================
    // FIND TARGET FOR ABILITY
    // ============================================================

    public GameObject FindTargetForAbility(
        AbilitySO ability)
    {
        if (
            attackUnit == null ||
            ability == null ||
            !attackUnit.IsAbilityReady(
                ability
            )
        )
        {
            return null;
        }


        GridManager gridManager =
            attackUnit.GetGridManager();


        if (gridManager == null)
        {
            return null;
        }


        List<AttackUnit> allUnits =
            CombatUtility.GetAllAliveUnits();


        GameObject bestTarget =
            null;


        int bestDistance =
            int.MaxValue;


        Vector2Int myPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );


        for (
            int i = 0;
            i < allUnits.Count;
            i++
        )
        {
            AttackUnit otherUnit =
                allUnits[i];


            if (
                otherUnit == null ||
                otherUnit == attackUnit
            )
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
                bestDistance =
                    distance;


                bestTarget =
                    target;
            }
        }


        return bestTarget;
    }


    // ============================================================
    // USE ALL AVAILABLE ABILITIES
    // ============================================================

    public int UseAllAvailableAbilities()
    {
        if (
            attackUnit == null ||
            !attackUnit.CanAttack()
        )
        {
            return 0;
        }


        int attacksPerformed =
            0;


        while (!attackUnit.IsDead())
        {
            GameObject target;


            AbilitySO ability =
                FindBestAvailableAbility(
                    out target
                );


            if (
                ability == null ||
                target == null
            )
            {
                break;
            }


            bool success =
                attackUnit.Attack(
                    target,
                    ability
                );


            if (!success)
            {
                break;
            }


            attacksPerformed++;
        }


        return attacksPerformed;
    }


    // ============================================================
    // USE ALL AVAILABLE ABILITIES COROUTINE
    // ============================================================

    public IEnumerator UseAllAvailableAbilitiesCoroutine()
    {
        if (attackUnit == null)
        {
            yield break;
        }


        if (attackUnit.IsDead())
        {
            yield break;
        }


        if (!attackUnit.CanAttack())
        {
            yield break;
        }


        int attacksPerformed =
            0;


        while (!attackUnit.IsDead())
        {
            GameObject target;


            AbilitySO ability =
                FindBestAvailableAbility(
                    out target
                );


            if (
                ability == null ||
                target == null
            )
            {
                break;
            }


            int attackNumber =
                attacksPerformed + 1;


            bool success =
                attackUnit.Attack(
                    target,
                    ability
                );


            if (!success)
            {
                break;
            }


            attacksPerformed++;


            float useDuration =
                ability.GetUseDuration();


            if (useDuration > 0f)
            {
                yield return new WaitForSeconds(
                    useDuration
                );
            }
            else
            {
                yield return null;
            }
        }
    }


    // ============================================================
    // FIND BEST AVAILABLE ABILITY
    // ============================================================

    private AbilitySO FindBestAvailableAbility(
        out GameObject bestTarget)
    {
        bestTarget =
            null;


        if (attackUnit == null)
        {
            return null;
        }


        List<AbilitySO> abilities =
            attackUnit.GetAbilities();


        AbilitySO bestAbility =
            null;


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


            if (
                ability == null ||
                !attackUnit.IsAbilityReady(
                    ability
                )
            )
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


            if (
                bestAbility == null ||
                damage > bestDamage ||
                (
                    damage == bestDamage &&
                    range > bestRange
                )
            )
            {
                bestAbility =
                    ability;


                bestTarget =
                    target;


                bestDamage =
                    damage;


                bestRange =
                    range;
            }
        }


        return bestAbility;
    }


    // ============================================================
    // TARGET SEARCH
    // ============================================================

    public bool TryAttackAnyTargetInAbilityRange()
    {
        return
            UseAllAvailableAbilities() > 0;
    }


    public bool AttackAnyTargetInAbilityRange()
    {
        return
            TryAttackAnyTargetInAbilityRange();
    }


    // ============================================================
    // HAS TARGET
    // ============================================================

    public bool HasAnyTargetInAbilityRange()
    {
        if (
            attackUnit == null ||
            !attackUnit.CanAttack()
        )
        {
            return false;
        }


        List<AbilitySO> abilities =
            attackUnit.GetAbilities();


        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];


            if (
                ability == null ||
                !attackUnit.IsAbilityReady(
                    ability
                )
            )
            {
                continue;
            }


            if (
                FindTargetForAbility(
                    ability
                ) != null
            )
            {
                return true;
            }
        }


        return false;
    }


    // ============================================================
    // MAX RANGE
    // ============================================================

    public int GetMaximumAttackRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }


        return
            attackUnit.GetMaximumAttackRange();
    }


    // ============================================================
    // ACCESSOR
    // ============================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }
}