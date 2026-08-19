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


    // ============================================================
    // RUNTIME COOLDOWNS
    // ============================================================

    private Dictionary<AbilitySO, int> abilityCooldowns =
        new Dictionary<AbilitySO, int>();


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager =
                GetComponent<HealthManager>();
        }

        if (healthManager == null)
        {
            Debug.LogError(
                $"[AttackUnit] {gameObject.name} " +
                "is missing a HealthManager!"
            );

            return;
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
        else
        {
            InitializeCooldowns();

            if (abilities.Count == 0)
            {
                Debug.LogWarning(
                    $"[AttackUnit] {gameObject.name} " +
                    "has no CharacterSO or abilities."
                );
            }
        }
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(CharacterSO data)
    {
        if (data == null)
        {
            Debug.LogError(
                $"[AttackUnit] Cannot initialize " +
                $"{gameObject.name}. CharacterSO is null."
            );

            return;
        }

        characterData = data;

        // --------------------------------------------------------
        // CLEAR OLD ABILITIES
        // --------------------------------------------------------

        abilities.Clear();

        // --------------------------------------------------------
        // COPY ALL CHARACTER ABILITIES
        // --------------------------------------------------------

        List<AbilitySO> characterAbilities =
            data.GetAbilities();

        if (characterAbilities != null)
        {
            foreach (AbilitySO ability in characterAbilities)
            {
                if (ability == null)
                {
                    continue;
                }

                if (!abilities.Contains(ability))
                {
                    abilities.Add(ability);
                }
            }
        }

        // --------------------------------------------------------
        // RESET COOLDOWNS
        // --------------------------------------------------------

        InitializeCooldowns();

    }


    // ============================================================
    // INITIALIZE COOLDOWNS
    // ============================================================

    private void InitializeCooldowns()
    {
        abilityCooldowns.Clear();

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
            {
                continue;
            }

            abilityCooldowns[ability] = 0;
        }
    }


    // ============================================================
    // ADD ABILITY
    // ============================================================

    public void AddAbility(AbilitySO ability)
    {
        if (ability == null)
        {
            Debug.LogWarning(
                $"[AttackUnit] {gameObject.name} " +
                "tried to add a null ability."
            );

            return;
        }

        if (abilities.Contains(ability))
        {
            Debug.LogWarning(
                $"[AttackUnit] {gameObject.name} already has " +
                $"{ability.GetAbilityName()}."
            );

            return;
        }

        abilities.Add(ability);

        abilityCooldowns[ability] = 0;

        Debug.Log(
            $"[AttackUnit] {gameObject.name} added " +
            $"{ability.GetAbilityName()}."
        );
    }


    // ============================================================
    // REMOVE ABILITY
    // ============================================================

    public void RemoveAbility(AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        abilities.Remove(ability);

        abilityCooldowns.Remove(ability);
    }


    // ============================================================
    // GET ABILITIES
    // ============================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }


    // ============================================================
    // GET ABILITY COUNT
    // ============================================================

    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    // ============================================================
    // GET COOLDOWN
    // ============================================================

    public int GetAbilityCooldown(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return -1;
        }

        if (!abilityCooldowns.ContainsKey(ability))
        {
            return 0;
        }

        return abilityCooldowns[ability];
    }


    // ============================================================
    // IS ON COOLDOWN
    // ============================================================

    public bool IsAbilityOnCooldown(
        AbilitySO ability
    )
    {
        return GetAbilityCooldown(ability) > 0;
    }


    // ============================================================
    // IS READY
    // ============================================================

    public bool IsAbilityReady(
        AbilitySO ability
    )
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


    // ============================================================
    // START NEW ROUND
    // ============================================================
    //
    // THIS MUST BE CALLED ONCE PER ROUND.
    //
    // Example:
    //
    // Ability cooldown = 2
    //
    // Used in Round 7:
    //
    // Round 7 -> 2
    // Round 8 -> 1
    // Round 9 -> 0
    // Round 10 -> usable
    //
    // ============================================================

    public void StartNewRound()
    {
        if (abilityCooldowns == null)
        {
            return;
        }

        List<AbilitySO> abilitiesToUpdate =
            new List<AbilitySO>(
                abilityCooldowns.Keys
            );

        foreach (AbilitySO ability in abilitiesToUpdate)
        {
            if (ability == null)
            {
                continue;
            }

            int oldCooldown =
                abilityCooldowns[ability];

            if (oldCooldown <= 0)
            {
                continue;
            }

            int newCooldown =
                oldCooldown - 1;

            if (newCooldown < 0)
            {
                newCooldown = 0;
            }

            abilityCooldowns[ability] =
                newCooldown;

            Debug.Log(
                $"[AttackUnit] {gameObject.name} -> " +
                $"{ability.GetAbilityName()} cooldown: " +
                $"{oldCooldown} -> {newCooldown}"
            );
        }
    }


    // ============================================================
    // SET COOLDOWN
    // ============================================================

    private void StartAbilityCooldown(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return;
        }

        if (!abilityCooldowns.ContainsKey(ability))
        {
            abilityCooldowns[ability] = 0;
        }

        int cooldown =
            ability.GetCooldown();

        if (cooldown < 0)
        {
            cooldown = 0;
        }

        abilityCooldowns[ability] =
            cooldown;

        Debug.Log(
            $"[AttackUnit] {gameObject.name} -> " +
            $"{ability.GetAbilityName()} cooldown " +
            $"started at {cooldown}."
        );
    }


    // ============================================================
    // ATTACK
    // ============================================================

    public bool Attack(GameObject target)
    {
        if (!CanAttack())
        {
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[AttackUnit] {gameObject.name} " +
                "has a null target."
            );

            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            Debug.LogWarning(
                $"[AttackUnit] {target.name} does not have " +
                "a HealthManager."
            );

            return false;
        }

        if (targetHealth.IsDead())
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

        // --------------------------------------------------------
        // FIND A USABLE ABILITY
        // --------------------------------------------------------

        AbilitySO ability =
            GetBestAbilityForTarget(target);

        if (ability == null)
        {
            Debug.Log(
                $"[AttackUnit] {gameObject.name} has no " +
                $"usable ability against {target.name}."
            );

            return false;
        }

        // --------------------------------------------------------
        // RANGE
        // --------------------------------------------------------

        if (!IsTargetInRange(target, ability))
        {
            Debug.Log(
                $"[AttackUnit] {gameObject.name} cannot attack " +
                $"{target.name}. Target is out of range."
            );

            return false;
        }

        // --------------------------------------------------------
        // USE ABILITY
        // --------------------------------------------------------

        bool success =
            ability.Use(
                gameObject,
                target
            );

        // --------------------------------------------------------
        // START COOLDOWN
        // --------------------------------------------------------

        if (success)
        {
            StartAbilityCooldown(ability);

            Debug.Log(
                $"[AttackUnit] {gameObject.name} used " +
                $"{ability.GetAbilityName()}."
            );
        }

        return success;
    }


    // ============================================================
    // ATTACK WITH SPECIFIC ABILITY
    // ============================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility
    )
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

        if (!abilities.Contains(selectedAbility))
        {
            Debug.LogWarning(
                $"[AttackUnit] {gameObject.name} does not " +
                $"have ability {selectedAbility.name}."
            );

            return false;
        }

        // --------------------------------------------------------
        // COOLDOWN CHECK
        // --------------------------------------------------------

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.Log(
                $"[AttackUnit] {gameObject.name} -> " +
                $"{selectedAbility.GetAbilityName()} " +
                $"is on cooldown for " +
                $"{GetAbilityCooldown(selectedAbility)} " +
                $"round(s)."
            );

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

        if (
            targetHealth.GetTeam() ==
            healthManager.GetTeam()
        )
        {
            return false;
        }

        if (
            !IsTargetInRange(
                target,
                selectedAbility
            )
        )
        {
            return false;
        }

        // --------------------------------------------------------
        // USE
        // --------------------------------------------------------

        bool success =
            selectedAbility.Use(
                gameObject,
                target
            );

        // --------------------------------------------------------
        // COOLDOWN
        // --------------------------------------------------------

        if (success)
        {
            StartAbilityCooldown(
                selectedAbility
            );
        }

        return success;
    }


    // ============================================================
    // GET BEST ABILITY
    // ============================================================

    public AbilitySO GetBestAbilityForTarget(
        GameObject target
    )
    {
        if (target == null)
        {
            return null;
        }

        AbilitySO bestAbility = null;

        int bestDamage =
            int.MinValue;

        int bestRange =
            int.MinValue;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
            {
                continue;
            }

            // ----------------------------------------------------
            // COOLDOWN
            // ----------------------------------------------------

            if (!IsAbilityReady(ability))
            {
                continue;
            }

            // ----------------------------------------------------
            // RANGE
            // ----------------------------------------------------

            if (!IsTargetInRange(target, ability))
            {
                continue;
            }

            int damage =
                ability.GetDamage();

            int range =
                ability.GetRange();

            // ----------------------------------------------------
            // BEST DAMAGE
            // ----------------------------------------------------

            if (damage > bestDamage)
            {
                bestAbility = ability;

                bestDamage =
                    damage;

                bestRange =
                    range;
            }
            else if (
                damage == bestDamage &&
                range > bestRange
            )
            {
                bestAbility = ability;

                bestRange =
                    range;
            }
        }

        return bestAbility;
    }


    // ============================================================
    // CHECK RANGE
    // ============================================================

    private bool IsTargetInRange(
        GameObject target,
        AbilitySO ability
    )
    {
        if (
            target == null ||
            ability == null
        )
        {
            return false;
        }

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[AttackUnit] GridManager not found."
            );

            return false;
        }

        Vector2Int attackerPosition =
            gridManager.WorldToGridPosition(
                transform.position
            );

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        int distance =
            gridManager.GetDistance(
                attackerPosition,
                targetPosition
            );

        return distance <=
               ability.GetRange();
    }


    // ============================================================
    // GET ATTACK RANGE
    // ============================================================

    public int GetAttackRange()
    {
        int maxRange = 0;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
            {
                continue;
            }

            if (!IsAbilityReady(ability))
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


    // ============================================================
    // GET MAXIMUM RANGE
    // ============================================================

    public int GetMaximumAttackRange()
    {
        int maxRange = 0;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
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


    // ============================================================
    // CAN ATTACK
    // ============================================================

    public bool CanAttack()
    {
        if (healthManager == null)
        {
            return false;
        }

        if (healthManager.IsDead())
        {
            return false;
        }

        foreach (AbilitySO ability in abilities)
        {
            if (ability != null)
            {
                return true;
            }
        }

        return false;
    }


    // ============================================================
    // GET TEAM
    // ============================================================

    public Team GetTeam()
    {
        if (healthManager == null)
        {
            return Team.Ally;
        }

        return healthManager.GetTeam();
    }


    // ============================================================
    // GET FIRST ABILITY
    // ============================================================

    public AbilitySO GetAbility()
    {
        foreach (AbilitySO ability in abilities)
        {
            if (ability != null)
            {
                return ability;
            }
        }

        return null;
    }


    // ============================================================
    // GET HEALTH
    // ============================================================

    public HealthManager GetHealthManager()
    {
        return healthManager;
    }


    // ============================================================
    // GET CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        return characterData;
    }
}