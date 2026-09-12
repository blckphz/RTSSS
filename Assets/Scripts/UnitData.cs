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
    // IMPORTANT:
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
        character =
            characterData;

        ResetRuntimeData();


        if (character == null)
        {
            Debug.LogWarning(
                "[UnitData] " +
                name +
                " initialized with NULL CharacterSO.",
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
            name +
            " with character " +
            character.characterName +
            " | Runtime abilities = " +
            abilityData.Count,
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
                "[UnitData] Cannot add bonus jumps. " +
                "Ability is null.",
                this
            );

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


        Debug.Log(
            "[UnitData] " +
            name +
            " received +" +
            amount +
            " bonus jumps for " +
            ability.name +
            ". Total bonus = " +
            abilityBonusJumps[ability],
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
    // RESET
    // ============================================================

    public void ResetRuntimeUpgrades()
    {
        abilityBonusJumps.Clear();


        Debug.Log(
            "[UnitData] Runtime upgrades reset for " +
            name,
            this
        );
    }


    public void ResetRuntimeData()
    {
        abilityData.Clear();

        abilityBonusJumps.Clear();


        Debug.Log(
            "[UnitData] Runtime data reset for " +
            name,
            this
        );
    }
}