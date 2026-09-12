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
    //
    // The AbilitySO is shared.
    //
    // The AbilityData is NOT shared.
    //
    // Every UnitData creates its own AbilityData objects.
    // ============================================================

    private Dictionary<AbilitySO, AbilityData> abilityData =
        new Dictionary<AbilitySO, AbilityData>();


    // ============================================================
    // RUNTIME UPGRADES
    // ============================================================

    private Dictionary<AbilitySO, int> abilityBonusJumps =
        new Dictionary<AbilitySO, int>();


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
        // Initialize() may be called again when a card/unit is
        // placed or recreated.
        //
        // ResetRuntimeData() clears purchased runtime upgrades,
        // which would cause Chain Lightning bonuses to disappear.
        //
        // We only rebuild the AbilityData here.
        abilityData.Clear();


        if (character == null)
        {
            Debug.LogWarning(
                "[UnitData] Initialize called with NULL character.",
                this
            );

            return;
        }


        // ========================================================
        // CREATE THIS UNIT'S ABILITY DATA
        // ========================================================

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


        Debug.Log(
            "[UnitData] Initialized " +
            gameObject.name +
            " | Character=" +
            character.name +
            " | Runtime abilities=" +
            abilityData.Count +
            " | Bonus jump entries=" +
            abilityBonusJumps.Count,
            this
        );
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
            Debug.LogWarning(
                "[UnitData] AddBonusJumps failed: ability is NULL.",
                this
            );

            return;
        }


        if (amount <= 0)
        {
            Debug.LogWarning(
                "[UnitData] AddBonusJumps failed: amount <= 0.",
                this
            );

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


        Debug.Log(
            "[UnitData] " +
            gameObject.name +
            " received +" +
            amount +
            " bonus jumps for " +
            ability.name +
            " | Ability ID=" +
            ability.GetInstanceID() +
            " | Total Bonus=" +
            abilityBonusJumps[ability] +
            " | Unit ID=" +
            GetInstanceID(),
            this
        );
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
    // DEBUG
    // ============================================================

    public void DebugBonusJumps(
        AbilitySO ability)
    {
        if (ability == null)
        {
            Debug.Log(
                "[UnitData] " +
                gameObject.name +
                " | No ability supplied.",
                this
            );

            return;
        }


        int bonus =
            GetBonusJumps(
                ability
            );


        Debug.Log(
            "[UnitData] DEBUG | " +
            "Unit=" +
            gameObject.name +
            " | Unit ID=" +
            GetInstanceID() +
            " | Ability=" +
            ability.name +
            " | Ability ID=" +
            ability.GetInstanceID() +
            " | Bonus Jumps=" +
            bonus,
            this
        );
    }


    // ============================================================
    // RESET RUNTIME UPGRADES
    // ============================================================

    public void ResetRuntimeUpgrades()
    {
        Debug.Log(
            "[UnitData] ResetRuntimeUpgrades | " +
            gameObject.name +
            " | Unit ID=" +
            GetInstanceID(),
            this
        );


        abilityBonusJumps.Clear();
    }


    // ============================================================
    // RESET EVERYTHING
    // ============================================================
    //
    // Use this ONLY when you intentionally want to completely
    // wipe the unit's runtime state.
    //
    // DO NOT call this from Initialize().
    // ============================================================

    public void ResetRuntimeData()
    {
        Debug.Log(
            "[UnitData] ResetRuntimeData | " +
            gameObject.name +
            " | Unit ID=" +
            GetInstanceID(),
            this
        );


        abilityData.Clear();
        abilityBonusJumps.Clear();
    }
}