using System.Collections.Generic;
using UnityEngine;

public class UnitData : MonoBehaviour
{
    // ============================================================
    // CHARACTER
    // ============================================================

    [SerializeField]
    private CharacterSO character;


    // ============================================================
    // RUNTIME ABILITY DATA
    // ============================================================

    /*
     * IMPORTANT:
     *
     * AttackUnit is the owner of the actual runtime AbilityData.
     *
     * Do NOT create a second AbilityData dictionary here.
     *
     * Otherwise a turret/player/enemy can have two different
     * cooldown/charge states for the same AbilitySO.
     */


    // ============================================================
    // RUNTIME UPGRADES
    // ============================================================

    private Dictionary<AbilitySO, int> abilityBonusJumps =
        new Dictionary<AbilitySO, int>();


    // ============================================================
    // ENGINEER TURRET HEAL UPGRADE
    // ============================================================

    // 0 = no turret-heal upgrade.
    //
    // Greater than 0 = amount of HP restored when this unit's
    // FrontAttack hits a friendly turret.

    private int meleeTurretHealAmount;


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        CharacterSO characterData
    )
    {
        character =
            characterData;

        /*
         * IMPORTANT:
         *
         * We intentionally do NOT create AbilityData here.
         *
         * AttackUnit.Initialize() creates the runtime ability
         * data and owns the cooldown/charge state.
         *
         * This prevents players, enemies and turrets from having
         * duplicate ability states.
         */
    }


    // ============================================================
    // CHARACTER
    // ============================================================

    public CharacterSO GetCharacter()
    {
        return character;
    }


    // ============================================================
    // ATTACK UNIT
    // ============================================================

    private AttackUnit GetAttackUnit()
    {
        AttackUnit attackUnit =
            GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            attackUnit =
                GetComponentInParent<AttackUnit>();
        }

        if (attackUnit == null)
        {
            attackUnit =
                GetComponentInChildren<AttackUnit>();
        }

        return attackUnit;
    }


    // ============================================================
    // ABILITY DATA
    // ============================================================

    public AbilityData GetAbilityData(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return null;
        }

        AttackUnit attackUnit =
            GetAttackUnit();

        if (attackUnit == null)
        {
            return null;
        }

        List<AbilityData> runtimeAbilities =
            attackUnit.GetRuntimeAbilities();

        if (runtimeAbilities == null)
        {
            return null;
        }

        for (
            int i = 0;
            i < runtimeAbilities.Count;
            i++
        )
        {
            AbilityData data =
                runtimeAbilities[i];

            if (data == null)
            {
                continue;
            }

            if (
                data.GetAbilitySO() ==
                ability
            )
            {
                return data;
            }
        }

        return null;
    }


    public bool HasAbilityData(
        AbilitySO ability
    )
    {
        return
            GetAbilityData(
                ability
            ) != null;
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    public int GetAbilityCooldown(
        AbilitySO ability
    )
    {
        AbilityData data =
            GetAbilityData(
                ability
            );

        if (data == null)
        {
            return 0;
        }

        return
            data.GetCooldownRemaining();
    }


    public void SetAbilityCooldown(
        AbilitySO ability,
        int cooldown
    )
    {
        AbilityData data =
            GetAbilityData(
                ability
            );

        if (data == null)
        {
            return;
        }

        data.SetCooldown(
            cooldown
        );
    }


    public void ReduceAbilityCooldowns()
    {
        AttackUnit attackUnit =
            GetAttackUnit();

        if (attackUnit == null)
        {
            return;
        }

        List<AbilityData> runtimeAbilities =
            attackUnit.GetRuntimeAbilities();

        if (runtimeAbilities == null)
        {
            return;
        }

        for (
            int i = 0;
            i < runtimeAbilities.Count;
            i++
        )
        {
            AbilityData data =
                runtimeAbilities[i];

            if (data == null)
            {
                continue;
            }

            data.ReduceCooldown();
        }
    }


    // ============================================================
    // USES
    // ============================================================

    public int GetAbilityUsesRemaining(
        AbilitySO ability
    )
    {
        AbilityData data =
            GetAbilityData(
                ability
            );

        if (data == null)
        {
            return 0;
        }

        /*
         * For unlimited abilities, AttackUnit/AbilityData handles
         * the special case.
         */
        if (
            ability != null &&
            ability.GetUsesPerTurn() <= 0
        )
        {
            return 0;
        }

        return
            data.GetUsesRemaining();
    }


    public bool CanUseAbility(
        AbilitySO ability
    )
    {
        AbilityData data =
            GetAbilityData(
                ability
            );

        if (data == null)
        {
            return false;
        }

        /*
         * IMPORTANT:
         *
         * Do NOT do this:
         *
         * if (data.IsOnCooldown())
         * {
         *     return false;
         * }
         *
         * With per-charge cooldowns:
         *
         * [3, 0]
         *
         * means one charge is cooling down and one charge
         * is ready.
         *
         * CanUse() checks the actual available charges.
         */

        return
            data.CanUse();
    }


    public bool ConsumeAbilityUse(
        AbilitySO ability
    )
    {
        AbilityData data =
            GetAbilityData(
                ability
            );

        if (data == null)
        {
            return false;
        }

        /*
         * ConsumeUse() itself finds a ready charge and puts
         * that specific charge on cooldown.
         */

        return
            data.ConsumeUse();
    }


    public void ResetAbilityUses()
    {
        AttackUnit attackUnit =
            GetAttackUnit();

        if (attackUnit == null)
        {
            return;
        }

        List<AbilityData> runtimeAbilities =
            attackUnit.GetRuntimeAbilities();

        if (runtimeAbilities == null)
        {
            return;
        }

        for (
            int i = 0;
            i < runtimeAbilities.Count;
            i++
        )
        {
            AbilityData data =
                runtimeAbilities[i];

            if (data == null)
            {
                continue;
            }

            data.ResetUses();
        }
    }


    // ============================================================
    // RUNTIME UPGRADES
    // ============================================================

    public void AddBonusJumps(
        AbilitySO ability,
        int amount
    )
    {
        if (ability == null)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        if (
            !abilityBonusJumps.ContainsKey(
                ability
            )
        )
        {
            abilityBonusJumps[ability] =
                0;
        }

        abilityBonusJumps[ability] +=
            amount;
    }


    public int GetBonusJumps(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return 0;
        }

        if (
            abilityBonusJumps.TryGetValue(
                ability,
                out int bonus
            )
        )
        {
            return bonus;
        }

        return 0;
    }


    // ============================================================
    // ENGINEER TURRET HEAL UPGRADE
    // ============================================================

    public void SetMeleeTurretHealAmount(
        int amount
    )
    {
        meleeTurretHealAmount =
            Mathf.Max(
                0,
                amount
            );
    }


    public int GetMeleeTurretHealAmount()
    {
        return meleeTurretHealAmount;
    }


    public bool HasMeleeTurretHealUpgrade()
    {
        return
            meleeTurretHealAmount > 0;
    }


    // ============================================================
    // DEBUG
    // ============================================================

    public void DebugBonusJumps(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return;
        }

        int bonus =
            GetBonusJumps(
                ability
            );
    }


    // ============================================================
    // RESET RUNTIME UPGRADES
    // ============================================================

    public void ResetRuntimeUpgrades()
    {
        abilityBonusJumps.Clear();

        meleeTurretHealAmount = 0;
    }


    // ============================================================
    // RESET EVERYTHING
    // ============================================================

    public void ResetRuntimeData()
    {
        /*
         * AbilityData is owned by AttackUnit.
         *
         * Do not clear or recreate it here.
         */

        abilityBonusJumps.Clear();

        meleeTurretHealAmount = 0;
    }
}