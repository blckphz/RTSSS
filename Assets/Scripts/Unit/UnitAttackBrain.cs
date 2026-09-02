using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AttackUnit attackUnit;


    [Header("Debug")]
    [SerializeField]
    private bool debugAttack = true;


    // ============================================================
    // DEBUG
    // ============================================================

    private const string DEBUG_PREFIX =
        "[UnitAttackBrain] ";


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


        DebugLog(
            "Awake. AttackUnit = " +
            (
                attackUnit != null
                    ? attackUnit.name
                    : "NULL"
            )
        );
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
            DebugLog(
                "Attack failed. Invalid target or AttackUnit."
            );


            return false;
        }


        AbilitySO ability =
            GetBestAbilityForTarget(target);


        if (ability == null)
        {
            DebugLog(
                "Attack failed. No ability available."
            );


            return false;
        }


        DebugLog(
            "Single Attack: " +
            ability.name +
            " -> " +
            target.name
        );


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
    //
    // SYNCHRONOUS VERSION.
    //
    // This performs all available uses immediately.
    //
    // DO NOT use this for the enemy turn if you want visible
    // delays between attacks.
    // ============================================================

    public int UseAllAvailableAbilities()
    {
        DebugLog(
            "========================================"
        );


        DebugLog(
            "UseAllAvailableAbilities START"
        );


        if (
            attackUnit == null ||
            !attackUnit.CanAttack()
        )
        {
            DebugLog(
                "Cannot attack."
            );


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
                DebugLog(
                    "No more abilities/targets."
                );


                break;
            }


            int attackNumber =
                attacksPerformed + 1;


            DebugLog(
                "ATTACK #" +
                attackNumber +
                " | Ability = " +
                ability.GetAbilityName() +
                " | Target = " +
                target.name +
                " | Damage = " +
                ability.GetDamage()
            );


            bool success =
                attackUnit.Attack(
                    target,
                    ability
                );


            DebugLog(
                "ATTACK #" +
                attackNumber +
                " result = " +
                success
            );


            if (!success)
            {
                Debug.LogWarning(
                    DEBUG_PREFIX +
                    name +
                    ": Attack failed. Stopping."
                );


                break;
            }


            attacksPerformed++;
        }


        DebugLog(
            "UseAllAvailableAbilities END | " +
            "Attacks performed = " +
            attacksPerformed
        );


        DebugLog(
            "========================================"
        );


        return attacksPerformed;
    }


    // ============================================================
    // USE ALL AVAILABLE ABILITIES COROUTINE
    // ============================================================
    //
    // THIS is the version used by the enemy turn.
    //
    // Every ability use is a SEPARATE attack.
    //
    // Example:
    //
    // usesPerTurn = 3
    // damage = 10
    // useDuration = 0.25
    //
    // RESULT:
    //
    // ATTACK #1 -> 10
    // wait 0.25
    // ATTACK #2 -> 10
    // wait 0.25
    // ATTACK #3 -> 10
    //
    // There is NO combined damage system here.
    // ============================================================

    public IEnumerator UseAllAvailableAbilitiesCoroutine()
    {
        DebugLog(
            "========================================"
        );


        DebugLog(
            "UseAllAvailableAbilitiesCoroutine START"
        );


        if (attackUnit == null)
        {
            Debug.LogError(
                DEBUG_PREFIX +
                name +
                ": attackUnit is NULL!"
            );


            yield break;
        }


        if (attackUnit.IsDead())
        {
            DebugLog(
                "Unit is already dead."
            );


            yield break;
        }


        if (!attackUnit.CanAttack())
        {
            DebugLog(
                "attackUnit.CanAttack() = FALSE"
            );


            yield break;
        }


        int attacksPerformed =
            0;


        while (!attackUnit.IsDead())
        {
            // ----------------------------------------------------
            // FIND NEXT ABILITY
            // ----------------------------------------------------

            GameObject target;


            AbilitySO ability =
                FindBestAvailableAbility(
                    out target
                );


            DebugLog(
                "Searching for attack #" +
                (attacksPerformed + 1)
            );


            // ----------------------------------------------------
            // NO MORE ATTACKS
            // ----------------------------------------------------

            if (
                ability == null ||
                target == null
            )
            {
                DebugLog(
                    "No more abilities/targets."
                );


                break;
            }


            int attackNumber =
                attacksPerformed + 1;


            // ----------------------------------------------------
            // DEBUG INFO
            // ----------------------------------------------------

            DebugLog(
                "----------------------------------------"
            );


            DebugLog(
                "ATTACK #" +
                attackNumber +
                " START"
            );


            DebugLog(
                "Ability = " +
                ability.GetAbilityName()
            );


            DebugLog(
                "Target = " +
                target.name
            );


            DebugLog(
                "Damage = " +
                ability.GetDamage()
            );


            DebugLog(
                "UseDuration = " +
                ability.GetUseDuration()
            );


            // ----------------------------------------------------
            // PERFORM ONE ATTACK
            // ----------------------------------------------------

            bool success =
                attackUnit.Attack(
                    target,
                    ability
                );


            DebugLog(
                "ATTACK #" +
                attackNumber +
                " result = " +
                success
            );


            if (!success)
            {
                Debug.LogWarning(
                    DEBUG_PREFIX +
                    name +
                    ": Attack #" +
                    attackNumber +
                    " FAILED."
                );


                break;
            }


            attacksPerformed++;


            DebugLog(
                "ATTACK #" +
                attacksPerformed +
                " COMPLETE"
            );


            // ----------------------------------------------------
            // WAIT BETWEEN USES
            // ----------------------------------------------------

            float useDuration =
                ability.GetUseDuration();


            if (useDuration > 0f)
            {
                DebugLog(
                    "WAITING " +
                    useDuration +
                    " SECONDS BEFORE NEXT ATTACK"
                );


                yield return new WaitForSeconds(
                    useDuration
                );


                DebugLog(
                    "WAIT COMPLETE"
                );
            }
            else
            {
                DebugLog(
                    "UseDuration = 0. " +
                    "Waiting one frame."
                );


                yield return null;
            }
        }


        DebugLog(
            "UseAllAvailableAbilitiesCoroutine END | " +
            "Attacks performed = " +
            attacksPerformed
        );


        DebugLog(
            "========================================"
        );
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


    // ============================================================
    // DEBUG HELPER
    // ============================================================

    private void DebugLog(
        string message)
    {
        if (!debugAttack)
        {
            return;
        }


        Debug.Log(
            DEBUG_PREFIX +
            name +
            ": " +
            message
        );
    }
}