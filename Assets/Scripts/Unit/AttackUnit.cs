using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField]
    private CharacterSO characterData;

    [Header("Abilities")]
    [SerializeField]
    private List<AbilitySO> abilities =
        new List<AbilitySO>();

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    private readonly Dictionary<AbilitySO, int>
        abilityCooldowns =
        new Dictionary<AbilitySO, int>();

    private readonly Dictionary<AbilitySO, int>
        abilityUsesRemaining =
        new Dictionary<AbilitySO, int>();

    private readonly List<AbilitySO>
        cooldownKeysCache =
        new List<AbilitySO>();

    private GridManager cachedGridManager;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager =
                GetComponent<HealthManager>();
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
        else
        {
            InitializeCooldowns();
        }
    }

    // ==================================================
    // INITIALIZE
    // ==================================================

    public void Initialize(
        CharacterSO data)
    {
        if (data == null)
        {
            return;
        }

        characterData = data;

        abilities.Clear();

        List<AbilitySO> characterAbilities =
            data.GetAbilities();

        if (characterAbilities != null)
        {
            for (
                int i = 0;
                i < characterAbilities.Count;
                i++
            )
            {
                AbilitySO ability =
                    characterAbilities[i];

                if (
                    ability != null &&
                    !abilities.Contains(ability)
                )
                {
                    abilities.Add(ability);
                }
            }
        }

        InitializeCooldowns();
    }

    private void InitializeCooldowns()
    {
        abilityCooldowns.Clear();
        abilityUsesRemaining.Clear();

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

            abilityCooldowns[ability] = 0;

            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }
    }

    // ==================================================
    // COOLDOWN
    // ==================================================

    public int GetAbilityCooldown(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return -1;
        }

        return abilityCooldowns.TryGetValue(
            ability,
            out int cooldown
        )
            ? cooldown
            : 0;
    }

    public bool IsAbilityOnCooldown(
        AbilitySO ability)
    {
        return GetAbilityCooldown(
            ability
        ) > 0;
    }

    // ==================================================
    // USES PER TURN
    // ==================================================

    public int GetAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return -1;
        }

        // 0 means unlimited.
        if (ability.GetUsesPerTurn() <= 0)
        {
            return 0;
        }

        return abilityUsesRemaining.TryGetValue(
            ability,
            out int uses
        )
            ? uses
            : 0;
    }

    public bool HasAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        // 0 = unlimited.
        if (ability.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        return GetAbilityUsesRemaining(
            ability
        ) > 0;
    }

    private void ConsumeAbilityUse(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        // Unlimited.
        if (ability.GetUsesPerTurn() <= 0)
        {
            return;
        }

        if (!abilityUsesRemaining.ContainsKey(
                ability))
        {
            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }

        abilityUsesRemaining[ability] =
            Mathf.Max(
                0,
                abilityUsesRemaining[ability] - 1
            );
    }

    // ==================================================
    // READY
    // ==================================================

    public bool IsAbilityReady(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        if (!abilities.Contains(
                ability))
        {
            return false;
        }

        if (GetAbilityCooldown(
                ability) > 0)
        {
            return false;
        }

        return HasAbilityUsesRemaining(
            ability
        );
    }

    // ==================================================
    // ROUND
    // ==================================================

    public void StartNewRound()
    {
        cooldownKeysCache.Clear();

        cooldownKeysCache.AddRange(
            abilityCooldowns.Keys
        );

        for (
            int i = 0;
            i < cooldownKeysCache.Count;
            i++
        )
        {
            AbilitySO ability =
                cooldownKeysCache[i];

            if (ability == null)
            {
                continue;
            }

            if (abilityCooldowns[ability] > 0)
            {
                abilityCooldowns[ability] =
                    Mathf.Max(
                        0,
                        abilityCooldowns[ability] - 1
                    );
            }

            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }
    }

    // ==================================================
    // COOLDOWN START
    // ==================================================

    private void StartAbilityCooldown(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        abilityCooldowns[ability] =
            Mathf.Max(
                0,
                ability.GetCooldown()
            );
    }

    // ==================================================
    // ATTACK
    // ==================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (!CanAttack())
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        if (selectedAbility == null)
        {
            return false;
        }

        if (!abilities.Contains(
                selectedAbility))
        {
            return false;
        }

        if (!IsAbilityReady(
                selectedAbility))
        {
            return false;
        }

        if (!IsValidTarget(target))
        {
            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        bool canHit =
            selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            );

        if (!canHit)
        {
            return false;
        }

        bool success =
            selectedAbility.Use(
                gameObject,
                target
            );

        if (!success)
        {
            return false;
        }

        ConsumeAbilityUse(
            selectedAbility
        );

        StartAbilityCooldown(
            selectedAbility
        );

        return true;
    }

    // ==================================================
    // TARGET
    // ==================================================

    public bool IsValidTarget(
        GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (target == gameObject)
        {
            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            return false;
        }

        if (targetHealth.IsDead())
        {
            return false;
        }

        if (healthManager == null)
        {
            return false;
        }

        if (
            targetHealth.GetTeam() ==
            healthManager.GetTeam()
        )
        {
            return false;
        }

        return true;
    }

    public bool CanAttack()
    {
        if (
            healthManager == null ||
            healthManager.IsDead()
        )
        {
            return false;
        }

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            if (abilities[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDead()
    {
        return healthManager == null ||
               healthManager.IsDead();
    }

    // ==================================================
    // GRID
    // ==================================================

    public GridManager GetGridManager()
    {
        EnsureGridManager();

        return cachedGridManager;
    }

    private void EnsureGridManager()
    {
        if (cachedGridManager == null)
        {
            cachedGridManager =
                FindFirstObjectByType<GridManager>();
        }
    }

    // ==================================================
    // RANGE
    // ==================================================

    public int GetAttackRange()
    {
        int maxRange = 0;

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
                IsAbilityReady(ability)
            )
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        ability.GetRange()
                    );
            }
        }

        return maxRange;
    }

    public int GetMaximumAttackRange()
    {
        int maxRange = 0;

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability != null)
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        ability.GetRange()
                    );
            }
        }

        return maxRange;
    }

    // ==================================================
    // ABILITIES
    // ==================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }

    public int GetAbilityCount()
    {
        return abilities.Count;
    }

    public void AddAbility(
        AbilitySO ability)
    {
        if (
            ability == null ||
            abilities.Contains(ability)
        )
        {
            return;
        }

        abilities.Add(ability);

        abilityCooldowns[ability] = 0;

        abilityUsesRemaining[ability] =
            ability.GetUsesPerTurn();
    }

    public void RemoveAbility(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        abilities.Remove(ability);

        abilityCooldowns.Remove(
            ability
        );

        abilityUsesRemaining.Remove(
            ability
        );
    }

    // ==================================================
    // ACCESSORS
    // ==================================================

    public Team GetTeam()
    {
        return healthManager == null
            ? Team.Ally
            : healthManager.GetTeam();
    }

    public HealthManager GetHealthManager()
    {
        return healthManager;
    }

    public CharacterSO GetCharacterData()
    {
        return characterData;
    }
}