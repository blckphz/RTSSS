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

        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"UnitAttackBrain Awake | " +
            $"AttackUnit={(attackUnit != null ? "FOUND" : "NULL")}"
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

        return
            attackUnit.GetMaximumAttackRange();
    }


    // ============================================================
    // TARGET ATTACK
    // ============================================================

    public bool CanAttackTarget(
        GameObject target
    )
    {
        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"CanAttackTarget | " +
            $"Target={(target != null ? target.name : "NULL")}"
        );

        if (
            attackUnit == null ||
            attackUnit.IsDead() ||
            !attackUnit.CanAttack() ||
            !attackUnit.IsValidTarget(target)
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"CanAttackTarget FAILED."
            );

            return false;
        }

        AbilitySO best =
            GetBestAbilityForTarget(
                target
            );

        bool result =
            best != null;

        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"CanAttackTarget result={result} | " +
            $"Ability={(best != null ? best.GetAbilityName() : "NONE")}"
        );

        return result;
    }


    public bool Attack(
        GameObject target
    )
    {
        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"UnitAttackBrain.Attack() | " +
            $"Target={(target != null ? target.name : "NULL")}"
        );

        if (
            attackUnit == null ||
            !CanAttackTarget(target)
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"UnitAttackBrain.Attack FAILED."
            );

            return false;
        }

        AbilitySO ability =
            GetBestAbilityForTarget(
                target
            );

        if (ability == null)
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"No ability found for target."
            );

            return false;
        }

        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"Selected ability '{ability.GetAbilityName()}'"
        );

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


    // ============================================================
    // USE ABILITY AT TILE
    // ============================================================

    public bool UseAbilityAtTile(
        AbilitySO ability,
        Vector2Int targetTile
    )
    {
        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"UseAbilityAtTile | " +
            $"Ability={(ability != null ? ability.GetAbilityName() : "NULL")} | " +
            $"Tile={targetTile}"
        );

        if (
            attackUnit == null ||
            ability == null ||
            attackUnit.IsDead()
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"UseAbilityAtTile FAILED - initial validation."
            );

            return false;
        }

        GridManager gridManager =
            attackUnit.GetGridManager();

        if (gridManager == null)
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"UseAbilityAtTile FAILED - GridManager NULL."
            );

            return false;
        }

        if (
            !gridManager.IsInsideGrid(
                targetTile
            )
        )
        {
            return false;
        }

        if (
            !attackUnit.IsAbilityReady(
                ability
            )
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"UseAbilityAtTile FAILED - ability not ready."
            );

            return false;
        }

        if (
            !ability.CanHitTile(
                gridManager,
                gameObject,
                targetTile
            )
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"UseAbilityAtTile FAILED - tile cannot be hit."
            );

            return false;
        }

        List<AttackUnit> units =
            CombatUtility.GetAllAliveUnits();

        GameObject target = null;

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
                GetUnitTile(
                    gridManager,
                    unit.gameObject
                );

            if (
                unitPosition ==
                targetTile
            )
            {
                target =
                    unit.gameObject;

                break;
            }
        }

        if (target != null)
        {
            if (
                !attackUnit.IsValidTarget(
                    target
                )
            )
            {
                Debug.LogWarning(
                    $"[AbilityDebug] {gameObject.name}: " +
                    $"Target on tile is not valid."
                );

                return false;
            }

            return attackUnit.Attack(
                target,
                ability
            );
        }

        return attackUnit.AttackAtTile(
            targetTile,
            ability
        );
    }


    // ============================================================
    // GET UNIT TILE
    // ============================================================

    private Vector2Int GetUnitTile(
        GridManager gridManager,
        GameObject unit
    )
    {
        if (unit == null)
        {
            return Vector2Int.zero;
        }

        UnitTilePin pin =
            unit.GetComponent<UnitTilePin>();

        if (
            pin != null &&
            pin.HasTile()
        )
        {
            return pin.GetTile();
        }

        return
            gridManager.WorldToGridPosition(
                unit.transform.position
            );
    }


    // ============================================================
    // BEST ABILITY
    // ============================================================

    public AbilitySO GetBestAbilityForTarget(
        GameObject target
    )
    {
        if (
            attackUnit == null ||
            !attackUnit.IsValidTarget(target)
        )
        {
            Debug.LogWarning(
                $"[AbilityDebug] {gameObject.name}: " +
                $"GetBestAbilityForTarget FAILED - invalid target."
            );

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

            Debug.Log(
                $"[AbilityDebug] {gameObject.name}: " +
                $"Testing ability '{ability.GetAbilityName()}'"
            );

            if (
                !attackUnit.IsAbilityReady(
                    ability
                )
            )
            {
                Debug.Log(
                    $"[AbilityDebug] {gameObject.name}: " +
                    $"Skipping '{ability.GetAbilityName()}' - not ready."
                );

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
                Debug.Log(
                    $"[AbilityDebug] {gameObject.name}: " +
                    $"Skipping '{ability.GetAbilityName()}' - cannot hit target."
                );

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

        Debug.Log(
            $"[AbilityDebug] {gameObject.name}: " +
            $"Best ability = " +
            $"{(bestAbility != null ? bestAbility.GetAbilityName() : "NONE")}"
        );

        return bestAbility;
    }


    // ============================================================
    // FIND TARGET
    // ============================================================

    public GameObject FindTargetForAbility(
        AbilitySO ability
    )
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

        GameObject bestTarget = null;

        int bestDistance =
            int.MaxValue;

        Vector2Int myPosition =
            GetUnitTile(
                gridManager,
                gameObject
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
                !attackUnit.IsValidTarget(
                    target
                )
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
                GetUnitTile(
                    gridManager,
                    target
                );

            int distance =
                gridManager.GetDistance(
                    myPosition,
                    targetPosition
                );

            if (
                distance <
                bestDistance
            )
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
    // USE ALL AVAILABLE
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
            return 0;
        }

        bool success =
            attackUnit.Attack(
                target,
                ability
            );

        return success ? 1 : 0;
    }


    // ============================================================
    // COROUTINE
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

            yield return StartCoroutine(
                attackUnit.WaitForAttackAnimation()
            );

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
    // FIND BEST AVAILABLE
    // ============================================================

    private AbilitySO FindBestAvailableAbility(
        out GameObject bestTarget
    )
    {
        bestTarget = null;

        if (attackUnit == null)
        {
            return null;
        }

        List<AbilitySO> abilities =
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

            if (
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
    // AFTER MOVEMENT
    // ============================================================

    public bool HasAbilityUsableAfterMovement()
    {
        if (
            attackUnit == null ||
            attackUnit.IsDead() ||
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
                !ability.CanAttackWithThisAfterMove()
            )
            {
                continue;
            }

            GameObject target =
                FindTargetForAbility(
                    ability
                );

            if (target != null)
            {
                return true;
            }
        }

        return false;
    }


    // ============================================================
    // ANY TARGET
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
    // TARGET RANGE
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

            GameObject target =
                FindTargetForAbility(
                    ability
                );

            if (target != null)
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