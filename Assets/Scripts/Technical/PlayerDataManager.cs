using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // ==================================================
    // HEALTH
    // ==================================================

    private int currentHealth;
    private int maxHealth;
    private bool initialized;


    // ==================================================
    // PERSISTENT ABILITY STATE
    // ==================================================

    [System.Serializable]
    private class PersistentAbilityState
    {
        public CharacterSO character;
        public AbilitySO ability;
        public AbilityData runtimeData;

        public PersistentAbilityState(
            CharacterSO character,
            AbilitySO ability)
        {
            this.character = character;
            this.ability = ability;
            this.runtimeData = new AbilityData(ability);
        }
    }

    private readonly List<PersistentAbilityState> abilityStates =
        new List<PersistentAbilityState>();


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // ==================================================
    // HEALTH INITIALIZE
    // ==================================================

    public void Initialize(CharacterSO character)
    {
        if (character == null)
            return;

        maxHealth =
            Mathf.Max(1, character.maxHealth);

        if (!initialized)
        {
            currentHealth =
                maxHealth;

            initialized =
                true;
        }
        else
        {
            currentHealth =
                Mathf.Clamp(
                    currentHealth,
                    0,
                    maxHealth);
        }
    }


    // ==================================================
    // HEALTH
    // ==================================================

    public void SetHealth(int health)
    {
        currentHealth =
            Mathf.Clamp(
                health,
                0,
                maxHealth);
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth =
            Mathf.Clamp(
                currentHealth + amount,
                0,
                maxHealth);
    }

    public void FullHeal()
    {
        currentHealth =
            maxHealth;
    }


    // ==================================================
    // ABILITY DATA
    // ==================================================

    public AbilityData GetOrCreateAbilityData(
        CharacterSO character,
        AbilitySO ability)
    {
        if (character == null || ability == null)
            return null;

        // Look for an existing runtime ability.
        for (int i = 0; i < abilityStates.Count; i++)
        {
            PersistentAbilityState state =
                abilityStates[i];

            if (state == null)
                continue;

            if (state.character == character &&
                state.ability == ability)
            {
                return state.runtimeData;
            }
        }

        // No existing state.
        // Create it ONCE.
        PersistentAbilityState newState =
            new PersistentAbilityState(
                character,
                ability);

        abilityStates.Add(newState);

        return newState.runtimeData;
    }


    // ==================================================
    // PREPARE NEW ENCOUNTER
    // ==================================================

    public void PrepareAbilitiesForNewEncounter()
    {
        for (int i = 0; i < abilityStates.Count; i++)
        {
            PersistentAbilityState state =
                abilityStates[i];

            if (state == null ||
                state.runtimeData == null)
            {
                continue;
            }

            // Reset uses for the new encounter.
            //
            // IMPORTANT:
            // This does NOT reset cooldown.
            state.runtimeData.ResetUses();
        }
    }


    // ==================================================
    // CLEAR ABILITY DATA
    // ==================================================

    // Call this ONLY when starting a completely
    // new game/run.
    //
    // DO NOT call this when starting a new encounter.
    public void ClearAbilityData()
    {
        abilityStates.Clear();
    }
}