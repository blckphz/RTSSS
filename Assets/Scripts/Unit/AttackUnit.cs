using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private CharacterSO characterData;

    [Header("Abilities")]
    [SerializeField]
    private List<AbilitySO> abilities = new List<AbilitySO>();

    [Header("References")]
    [SerializeField] private HealthManager healthManager;

    [Header("Debug")]
    [SerializeField] private bool debugAttacks = true;

    private readonly Dictionary<AbilitySO, int> abilityCooldowns =
        new Dictionary<AbilitySO, int>();

    private readonly List<AbilitySO> cooldownKeysCache =
        new List<AbilitySO>();

    private GridManager cachedGridManager;


    // ==================================================
    // DEBUG
    // ==================================================

    private void AttackDebug(string message)
    {
        if (!debugAttacks)
        {
            return;
        }

        Debug.Log(
            $"[AttackUnit] {gameObject.name}: {message}",
            gameObject
        );
    }


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager = GetComponent<HealthManager>();
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
        else
        {
            InitializeCooldowns();
        }

        AttackDebug(
            $"Awake. Abilities={abilities.Count}, " +
            $"HealthManager={(healthManager != null ? "FOUND" : "NULL")}"
        );
    }


    // ==================================================
    // INITIALIZATION
    // ==================================================

    public void Initialize(CharacterSO data)
    {
        if (data == null)
        {
            AttackDebug(
                "Initialize FAILED: CharacterSO is NULL."
            );

            return;
        }

        characterData = data;

        abilities.Clear();

        List<AbilitySO> characterAbilities =
            data.GetAbilities();

        if (characterAbilities != null)
        {
            for (int i = 0; i < characterAbilities.Count; i++)
            {
                AbilitySO ability = characterAbilities[i];

                if (ability != null &&
                    !abilities.Contains(ability))
                {
                    abilities.Add(ability);
                }
            }
        }

        InitializeCooldowns();

        AttackDebug(
            $"Initialized character '{data.name}'. " +
            $"Abilities={abilities.Count}"
        );
    }


    private void InitializeCooldowns()
    {
        abilityCooldowns.Clear();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability != null)
            {
                abilityCooldowns[ability] = 0;
            }
        }
    }


    // ==================================================
    // COOLDOWNS
    // ==================================================

    public int GetAbilityCooldown(AbilitySO ability)
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


    public bool IsAbilityOnCooldown(AbilitySO ability)
    {
        return GetAbilityCooldown(ability) > 0;
    }


    public bool IsAbilityReady(AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        if (!abilities.Contains(ability))
        {
            return false;
        }

        return GetAbilityCooldown(ability) <= 0;
    }


    public void StartNewRound()
    {
        cooldownKeysCache.Clear();

        cooldownKeysCache.AddRange(
            abilityCooldowns.Keys
        );

        for (int i = 0; i < cooldownKeysCache.Count; i++)
        {
            AbilitySO ability = cooldownKeysCache[i];

            if (ability != null &&
                abilityCooldowns[ability] > 0)
            {
                int oldCooldown =
                    abilityCooldowns[ability];

                abilityCooldowns[ability] =
                    Mathf.Max(
                        0,
                        abilityCooldowns[ability] - 1
                    );

                AttackDebug(
                    $"Cooldown tick: " +
                    $"{ability.GetAbilityName()} " +
                    $"{oldCooldown} -> " +
                    $"{abilityCooldowns[ability]}"
                );
            }
        }
    }


    private void StartAbilityCooldown(AbilitySO ability)
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

        AttackDebug(
            $"Started cooldown for " +
            $"'{ability.GetAbilityName()}': " +
            $"{abilityCooldowns[ability]}"
        );
    }


    // ==================================================
    // ATTACK EXECUTION
    // ==================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility)
    {
        AttackDebug(
            $"Attack(target, ability) called. " +
            $"Target=" +
            $"{(target != null ? target.name : "NULL")}, " +
            $"Ability=" +
            $"{(selectedAbility != null ? selectedAbility.GetAbilityName() : "NULL")}"
        );

        if (!CanAttack())
        {
            AttackDebug(
                "Attack FAILED: Unit cannot attack."
            );

            return false;
        }

        if (target == null)
        {
            AttackDebug(
                "Attack FAILED: Target is NULL."
            );

            return false;
        }

        if (selectedAbility == null)
        {
            AttackDebug(
                "Attack FAILED: Ability is NULL."
            );

            return false;
        }

        if (!abilities.Contains(selectedAbility))
        {
            AttackDebug(
                $"Attack FAILED: Ability " +
                $"'{selectedAbility.GetAbilityName()}' " +
                $"is not owned by this unit."
            );

            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            AttackDebug(
                $"Attack FAILED: Ability " +
                $"'{selectedAbility.GetAbilityName()}' " +
                $"is on cooldown. " +
                $"Cooldown=" +
                $"{GetAbilityCooldown(selectedAbility)}"
            );

            return false;
        }

        if (!IsValidTarget(target))
        {
            AttackDebug(
                $"Attack FAILED: Target " +
                $"'{target.name}' is invalid."
            );

            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            AttackDebug(
                "Attack FAILED: GridManager is NULL."
            );

            return false;
        }

        bool canHit =
            selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            );

        AttackDebug(
            $"Ability.CanHit(" +
            $"'{selectedAbility.GetAbilityName()}', " +
            $"{target.name}) = {canHit}"
        );

        if (!canHit)
        {
            AttackDebug(
                $"Attack FAILED: Target " +
                $"'{target.name}' is outside ability hit area."
            );

            return false;
        }

        AttackDebug(
            $"USING ABILITY " +
            $"'{selectedAbility.GetAbilityName()}' " +
            $"on '{target.name}'."
        );

        bool success =
            selectedAbility.Use(
                gameObject,
                target
            );

        AttackDebug(
            $"Ability.Use result = {success}"
        );

        if (!success)
        {
            AttackDebug(
                "Attack FAILED: Ability.Use returned false."
            );

            return false;
        }

        StartAbilityCooldown(selectedAbility);

        AttackDebug(
            $"ATTACK SUCCESS. " +
            $"Ability='{selectedAbility.GetAbilityName()}', " +
            $"Target='{target.name}', " +
            $"Cooldown={GetAbilityCooldown(selectedAbility)}"
        );

        return true;
    }


    // ==================================================
    // TARGET VALIDATION
    // ==================================================

    public bool IsValidTarget(GameObject target)
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

        if (targetHealth.GetTeam() ==
            healthManager.GetTeam())
        {
            return false;
        }

        return true;
    }


    // ==================================================
    // CAN ATTACK
    // ==================================================

    public bool CanAttack()
    {
        if (healthManager == null ||
            healthManager.IsDead())
        {
            return false;
        }

        for (int i = 0; i < abilities.Count; i++)
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
    // GRID MANAGER
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

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

            if (ability != null &&
                IsAbilityReady(ability))
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

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilitySO ability = abilities[i];

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
    // ABILITY MANAGEMENT
    // ==================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }


    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    public void AddAbility(AbilitySO ability)
    {
        if (ability == null ||
            abilities.Contains(ability))
        {
            return;
        }

        abilities.Add(ability);

        abilityCooldowns[ability] = 0;

        AttackDebug(
            $"Added ability '{ability.GetAbilityName()}'."
        );
    }


    public void RemoveAbility(AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        abilities.Remove(ability);

        abilityCooldowns.Remove(ability);

        AttackDebug(
            $"Removed ability '{ability.GetAbilityName()}'."
        );
    }


    // ==================================================
    // CHARACTER / TEAM
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