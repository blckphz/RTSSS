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

    private Dictionary<AbilitySO, AbilityData> abilityData =
        new Dictionary<AbilitySO, AbilityData>();


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
        CharacterSO characterData)
    {
        character = characterData;

        // IMPORTANT:
        //
        // Do NOT call ResetRuntimeData() here.
        //
        // Initialize() may be called again when a unit is placed
        // or recreated.
        //
        // Runtime upgrades must survive initialization.
        //
        // We only rebuild the ability data here.

        abilityData.Clear();

        if (character == null)
        {
            return;
        }


        List<AbilitySO> abilities =
            character.GetAbilities();


        if (abilities != null)
        {
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
                    abilityData.ContainsKey(
                        ability
                    )
                )
                {
                    continue;
                }


                abilityData[ability] =
                    new AbilityData(
                        ability
                    );
            }
        }
    }


    // ============================================================
    // CHARACTER
    // ============================================================

    public CharacterSO GetCharacter()
    {
        return character;
    }


    // ============================================================
    // ABILITY DATA
    // ============================================================

    public AbilityData GetAbilityData(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return null;
        }


        if (
            abilityData.TryGetValue(
                ability,
                out AbilityData data
            )
        )
        {
            return data;
        }


        return null;
    }


    public bool HasAbilityData(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }


        return abilityData.ContainsKey(
            ability
        );
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    public int GetAbilityCooldown(
        AbilitySO ability)
    {
        AbilityData data =
            GetAbilityData(
                ability
            );


        if (data == null)
        {
            return 0;
        }


        return data.GetCooldownRemaining();
    }


    public void SetAbilityCooldown(
        AbilitySO ability,
        int cooldown)
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
        foreach (
            AbilityData data
            in abilityData.Values
        )
        {
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
        AbilitySO ability)
    {
        AbilityData data =
            GetAbilityData(
                ability
            );


        if (data == null)
        {
            return 0;
        }


        return data.GetUsesRemaining();
    }


    public bool CanUseAbility(
        AbilitySO ability)
    {
        AbilityData data =
            GetAbilityData(
                ability
            );


        if (data == null)
        {
            return false;
        }


        if (data.IsOnCooldown())
        {
            return false;
        }


        return data.CanUse();
    }


    public bool ConsumeAbilityUse(
        AbilitySO ability)
    {
        AbilityData data =
            GetAbilityData(
                ability
            );


        if (data == null)
        {
            return false;
        }


        if (data.IsOnCooldown())
        {
            return false;
        }


        return data.ConsumeUse();
    }


    public void ResetAbilityUses()
    {
        foreach (
            AbilityData data
            in abilityData.Values
        )
        {
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
        int amount)
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
        AbilitySO ability)
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
        int amount)
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
        return meleeTurretHealAmount > 0;
    }


    // ============================================================
    // DEBUG
    // ============================================================

    public void DebugBonusJumps(
        AbilitySO ability)
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
        abilityData.Clear();

        abilityBonusJumps.Clear();

        meleeTurretHealAmount = 0;
    }
}
