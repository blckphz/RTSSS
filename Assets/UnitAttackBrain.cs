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
            attackUnit =
                GetComponent<AttackUnit>();
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
                attackUnit.IsAbilityReady(ability)
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
                attackUnit.IsAbilityReady(ability)
            )
            {
                return ability.GetRange();
            }
        }

        return attackUnit.GetMaximumAttackRange();
    }


    // ==================================================
    // FULL TURN
    // ==================================================

    /// <summary>
    /// Performs the complete attack/move/attack sequence.
    ///
    /// ORDER:
    ///
    /// 1. Attack with all abilities available BEFORE movement.
    /// 2. Execute the supplied movement coroutine.
    /// 3. Attack again AFTER movement.
    ///
    /// After movement, ONLY abilities with
    /// canattackwiththisaftermove == true are allowed.
    /// </summary>
    public IEnumerator ExecuteAttackMoveAttackCoroutine(
        IEnumerator movementRoutine
    )
    {
        if (
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            yield break;
        }


        // ==================================================
        // PHASE 1 - ATTACK BEFORE MOVEMENT
        // ==================================================

        yield return StartCoroutine(
            UseAllAvailableAbilitiesCoroutine(
                false
            )
        );


        // ==================================================
        // STOP IF DEAD
        // ==================================================

        if (
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            yield break;
        }


        // ==================================================
        // PHASE 2 - MOVE
        // ==================================================

        if (movementRoutine != null)
        {
            yield return StartCoroutine(
                movementRoutine
            );
        }


        // ==================================================
        // STOP IF DEAD
        // ==================================================

        if (
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            yield break;
        }


        // ==================================================
        // PHASE 3 - ATTACK AFTER MOVEMENT
        // ==================================================

        yield return StartCoroutine(
            UseAllAvailableAbilitiesCoroutine(
                true
            )
        );
    }


    // ==================================================
    // CAN ATTACK
    // ==================================================

    public bool CanAttackTarget(
        GameObject target
    )
    {
        if (
            attackUnit == null ||
            !attackUnit.CanAttack() ||
            !attackUnit.IsValidTarget(target)
        )
        {
            return false;
        }

        return GetBestAbilityForTarget(target) != null;
    }


    // ==================================================
    // SINGLE ATTACK
    // ==================================================

    public bool Attack(
        GameObject target
    )
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


    public bool UsePrimaryAbility(
        GameObject target
    )
    {
        return Attack(target);
    }


    // ==================================================
    // BEST ABILITY FOR TARGET
    // ==================================================

    public AbilitySO GetBestAbilityForTarget(
        GameObject target
    )
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
                !attackUnit.IsAbilityReady(ability)
            )
            {
                continue;
            }

            if (
                !ability.CanHit(
                    gridManager,
                    gameObject,
                    target
                )
            )
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


    // ==================================================
    // FIND TARGET FOR ABILITY
    // ==================================================

    public GameObject FindTargetForAbility(
        AbilitySO ability
    )
    {
        if (
            attackUnit == null ||
            ability == null ||
            !attackUnit.IsAbilityReady(ability)
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

            if (
                !attackUnit.IsValidTarget(target)
            )
            {
                continue;
            }

            if (
                !ability.CanHit(
                    gridManager,
                    gameObject,
                    target
                )
            )
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


    // ==================================================
    // MULTI ATTACK
    // ==================================================

    /// <summary>
    /// Attacks with every currently available ability.
    ///
    /// This is the BEFORE-MOVEMENT attack phase.
    ///
    /// All abilities are allowed.
    /// </summary>
    public int UseAllAvailableAbilities()
    {
        return UseAllAvailableAbilities(
            false
        );
    }


    /// <summary>
    /// Attacks with every currently available ability.
    ///
    /// afterMoving == false:
    ///     All abilities are allowed.
    ///
    /// afterMoving == true:
    ///     ONLY abilities with
    ///     canattackwiththisaftermove == true
    ///     are allowed.
    /// </summary>
    public int UseAllAvailableAbilities(
        bool afterMoving
    )
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
            AbilitySO bestAbility =
                FindBestAvailableAbility(
                    afterMoving,
                    out GameObject target
                );

            if (
                bestAbility == null ||
                target == null
            )
            {
                break;
            }

            bool success =
                attackUnit.Attack(
                    target,
                    bestAbility
                );

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
        yield return StartCoroutine(
            UseAllAvailableAbilitiesCoroutine(
                false
            )
        );
    }


    public IEnumerator UseAllAvailableAbilitiesCoroutine(
        bool afterMoving
    )
    {
        if (
            attackUnit == null ||
            !attackUnit.CanAttack()
        )
        {
            yield break;
        }

        while (!attackUnit.IsDead())
        {
            AbilitySO bestAbility =
                FindBestAvailableAbility(
                    afterMoving,
                    out GameObject target
                );

            if (
                bestAbility == null ||
                target == null
            )
            {
                break;
            }

            bool success =
                attackUnit.Attack(
                    target,
                    bestAbility
                );

            if (!success)
            {
                break;
            }

            float duration =
                bestAbility.GetUseDuration();

            if (duration > 0f)
            {
                yield return new WaitForSeconds(
                    duration
                );
            }
            else
            {
                yield return null;
            }
        }
    }


    // ==================================================
    // FIND BEST AVAILABLE ABILITY
    // ==================================================

    private AbilitySO FindBestAvailableAbility(
        bool afterMoving,
        out GameObject bestTarget
    )
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
                !attackUnit.IsAbilityReady(ability)
            )
            {
                continue;
            }


            // ==================================================
            // AFTER-MOVE RESTRICTION
            // ==================================================

            if (
                afterMoving &&
                !ability.canAttackWithThisAfterMove
            )
            {
                continue;
            }


            // ==================================================
            // FIND TARGET
            // ==================================================

            GameObject target =
                FindTargetForAbility(
                    ability
                );

            if (target == null)
            {
                continue;
            }


            // ==================================================
            // PRIORITY
            // ==================================================

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


    // ==================================================
    // HAS TARGET IN RANGE
    // ==================================================

    public bool HasAnyTargetInAbilityRange()
    {
        return HasAnyTargetInAbilityRange(
            false
        );
    }


    public bool HasAnyTargetInAbilityRange(
        bool afterMoving
    )
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
                !attackUnit.IsAbilityReady(ability)
            )
            {
                continue;
            }


            // ==================================================
            // AFTER-MOVE RESTRICTION
            // ==================================================

            if (
                afterMoving &&
                !ability.canAttackWithThisAfterMove
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


    // ==================================================
    // MAXIMUM AFTER-MOVE RANGE
    // ==================================================

    /// <summary>
    /// Gets the maximum range of an ability that is
    /// allowed to be used after movement.
    ///
    /// This is useful for your movement AI when deciding
    /// where the unit should move.
    /// </summary>
    public int GetMaximumAfterMoveAttackRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }

        List<AbilitySO> abilities =
            attackUnit.GetAbilities();

        int maxRange =
            0;

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
                !attackUnit.IsAbilityReady(ability)
            )
            {
                continue;
            }

            if (
                !ability.canAttackWithThisAfterMove
            )
            {
                continue;
            }

            maxRange =
                Mathf.Max(
                    maxRange,
                    ability.GetRange()
                );
        }

        return maxRange;
    }


    // ==================================================
    // MAXIMUM ATTACK RANGE
    // ==================================================

    /// <summary>
    /// Gets the maximum range of ANY ready ability.
    /// </summary>
    public int GetMaximumAttackRange()
    {
        if (attackUnit == null)
        {
            return 0;
        }

        return attackUnit.GetMaximumAttackRange();
    }


    // ==================================================
    // ACCESSORS
    // ==================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }
}