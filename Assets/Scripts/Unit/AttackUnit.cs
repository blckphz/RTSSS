using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField]
    private CharacterSO characterData;

    [Header("Abilities")]
    [SerializeField]
    private List<AbilitySO> abilities = new List<AbilitySO>();

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    private Dictionary<AbilitySO, int> abilityCooldowns = new Dictionary<AbilitySO, int>();

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager = GetComponent<HealthManager>();
        }

        if (healthManager == null)
        {
            Debug.LogError($"[AttackUnit] {gameObject.name} is missing HealthManager.");
            return;
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

    public void Initialize(CharacterSO data)
    {
        if (data == null)
        {
            Debug.LogError($"[AttackUnit] {gameObject.name} cannot initialize with null CharacterSO.");
            return;
        }

        characterData = data;
        abilities.Clear();

        List<AbilitySO> characterAbilities = data.GetAbilities();

        if (characterAbilities != null)
        {
            foreach (AbilitySO ability in characterAbilities)
            {
                if (ability == null)
                    continue;

                if (!abilities.Contains(ability))
                {
                    abilities.Add(ability);
                }
            }
        }

        InitializeCooldowns();
    }

    // ==================================================
    // COOLDOWNS
    // ==================================================

    private void InitializeCooldowns()
    {
        abilityCooldowns.Clear();

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            abilityCooldowns[ability] = 0;
        }
    }

    public int GetAbilityCooldown(AbilitySO ability)
    {
        if (ability == null)
            return -1;

        if (!abilityCooldowns.ContainsKey(ability))
            return 0;

        return abilityCooldowns[ability];
    }

    public bool IsAbilityOnCooldown(AbilitySO ability)
    {
        return GetAbilityCooldown(ability) > 0;
    }

    public bool IsAbilityReady(AbilitySO ability)
    {
        if (ability == null)
            return false;

        if (!abilities.Contains(ability))
            return false;

        return GetAbilityCooldown(ability) <= 0;
    }

    public void StartNewRound()
    {
        if (abilityCooldowns == null)
            return;

        List<AbilitySO> keys = new List<AbilitySO>(abilityCooldowns.Keys);

        foreach (AbilitySO ability in keys)
        {
            if (ability == null)
                continue;

            if (abilityCooldowns[ability] > 0)
            {
                abilityCooldowns[ability]--;

                if (abilityCooldowns[ability] < 0)
                {
                    abilityCooldowns[ability] = 0;
                }
            }
        }
    }

    private void StartAbilityCooldown(AbilitySO ability)
    {
        if (ability == null)
            return;

        if (!abilityCooldowns.ContainsKey(ability))
        {
            abilityCooldowns[ability] = 0;
        }

        abilityCooldowns[ability] = Mathf.Max(0, ability.GetCooldown());
    }

    // ==================================================
    // PLAYER ACTION HELPERS
    // ==================================================

    /// <summary>
    /// Returns the range of the unit's primary (first available/ready) ability.
    /// Used by PlayerAction for movement checks.
    /// </summary>
    public int GetPrimaryAbilityRange()
    {
        foreach (AbilitySO ability in abilities)
        {
            if (ability != null && IsAbilityReady(ability))
            {
                return ability.GetRange();
            }
        }

        // Fallback: Check all abilities even if on cooldown, or return max range
        return GetMaximumAttackRange();
    }

    /// <summary>
    /// Triggers the unit's best or primary ability against a target.
    /// </summary>
    public bool UsePrimaryAbility(GameObject target)
    {
        return Attack(target);
    }

    /// <summary>
    /// Check if unit is dead according to HealthManager.
    /// </summary>
    public bool IsDead()
    {
        if (healthManager == null) return true;
        return healthManager.IsDead();
    }

    // ==================================================
    // ATTACK
    // ==================================================

    public bool Attack(GameObject target)
    {
        if (!CanAttack())
            return false;

        if (target == null)
            return false;

        HealthManager targetHealth = target.GetComponent<HealthManager>();

        if (targetHealth == null || targetHealth.IsDead())
        {
            return false;
        }

        if (targetHealth.GetTeam() == healthManager.GetTeam())
        {
            return false;
        }

        AbilitySO ability = GetBestAbilityForTarget(target);

        if (ability == null)
            return false;

        return Attack(target, ability);
    }

    // ==================================================
    // ATTACK WITH SPECIFIC ABILITY
    // ==================================================

    public bool Attack(GameObject target, AbilitySO selectedAbility)
    {
        if (!CanAttack())
            return false;

        if (target == null || selectedAbility == null)
        {
            return false;
        }

        if (!abilities.Contains(selectedAbility))
        {
            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            return false;
        }

        HealthManager targetHealth = target.GetComponent<HealthManager>();

        if (targetHealth == null || targetHealth.IsDead())
        {
            return false;
        }

        if (targetHealth.GetTeam() == healthManager.GetTeam())
        {
            return false;
        }

        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        // ==========================================
        // ACTUAL HITBOX CHECK
        // ==========================================

        if (!selectedAbility.CanHit(gridManager, gameObject, target))
        {
            Debug.Log($"[AttackUnit] {gameObject.name} cannot hit {target.name} with {selectedAbility.GetAbilityName()}.");
            return false;
        }

        bool success = selectedAbility.Use(gameObject, target);

        if (!success)
            return false;

        StartAbilityCooldown(selectedAbility);

        Debug.Log($"[AttackUnit] {gameObject.name} attacked {target.name} using {selectedAbility.GetAbilityName()}.");

        return true;
    }

    // ==================================================
    // ATTACK ANY TARGET IN ABILITY HITBOX
    // ==================================================

    public bool AttackAnyTargetInAbilityRange()
    {
        if (!CanAttack())
            return false;

        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);

        GameObject bestTarget = null;
        AbilitySO bestAbility = null;

        int bestDamage = int.MinValue;
        int bestRange = int.MinValue;
        int bestDistance = int.MaxValue;

        Vector2Int attackerPosition = gridManager.WorldToGridPosition(transform.position);

        foreach (AttackUnit otherUnit in allUnits)
        {
            if (otherUnit == null || otherUnit == this)
            {
                continue;
            }

            HealthManager targetHealth = otherUnit.GetHealthManager();

            if (targetHealth == null || !targetHealth.IsAlive())
            {
                continue;
            }

            if (targetHealth.GetTeam() == GetTeam())
            {
                continue;
            }

            Vector2Int targetPosition = gridManager.WorldToGridPosition(otherUnit.transform.position);
            int distance = gridManager.GetDistance(attackerPosition, targetPosition);

            foreach (AbilitySO ability in abilities)
            {
                if (ability == null)
                    continue;

                if (!IsAbilityReady(ability))
                    continue;

                // ==========================================
                // THIS IS THE IMPORTANT CHECK
                // ==========================================

                if (!ability.CanHit(gridManager, gameObject, otherUnit.gameObject))
                {
                    continue;
                }

                int damage = ability.GetDamage();
                int range = ability.GetRange();

                bool better = damage > bestDamage ||
                             (damage == bestDamage && range > bestRange) ||
                             (damage == bestDamage && range == bestRange && distance < bestDistance);

                if (!better)
                    continue;

                bestTarget = otherUnit.gameObject;
                bestAbility = ability;
                bestDamage = damage;
                bestRange = range;
                bestDistance = distance;
            }
        }

        if (bestTarget == null || bestAbility == null)
        {
            return false;
        }

        Debug.Log($"[AttackUnit] {gameObject.name} found {bestTarget.name} in {bestAbility.GetAbilityName()} hitbox.");

        return Attack(bestTarget, bestAbility);
    }

    // ==================================================
    // HAS TARGET IN ABILITY HITBOX
    // ==================================================

    public bool HasAnyTargetInAbilityRange()
    {
        if (!CanAttack())
            return false;

        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);

        foreach (AttackUnit otherUnit in allUnits)
        {
            if (otherUnit == null || otherUnit == this)
            {
                continue;
            }

            HealthManager targetHealth = otherUnit.GetHealthManager();

            if (targetHealth == null || !targetHealth.IsAlive())
            {
                continue;
            }

            if (targetHealth.GetTeam() == GetTeam())
            {
                continue;
            }

            foreach (AbilitySO ability in abilities)
            {
                if (ability == null)
                    continue;

                if (!IsAbilityReady(ability))
                    continue;

                if (ability.CanHit(gridManager, gameObject, otherUnit.gameObject))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ==================================================
    // GET BEST ABILITY
    // ==================================================

    public AbilitySO GetBestAbilityForTarget(GameObject target)
    {
        if (target == null)
            return null;

        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return null;

        AbilitySO bestAbility = null;
        int bestDamage = int.MinValue;
        int bestRange = int.MinValue;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            if (!IsAbilityReady(ability))
                continue;

            if (!ability.CanHit(gridManager, gameObject, target))
            {
                continue;
            }

            int damage = ability.GetDamage();
            int range = ability.GetRange();

            if (damage > bestDamage)
            {
                bestAbility = ability;
                bestDamage = damage;
                bestRange = range;
            }
            else if (damage == bestDamage && range > bestRange)
            {
                bestAbility = ability;
                bestRange = range;
            }
        }

        return bestAbility;
    }

    // ==================================================
    // ATTACK RANGE
    // ==================================================

    public int GetAttackRange()
    {
        int maxRange = 0;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            if (!IsAbilityReady(ability))
                continue;

            maxRange = Mathf.Max(maxRange, ability.GetRange());
        }

        return maxRange;
    }

    public int GetMaximumAttackRange()
    {
        int maxRange = 0;

        foreach (AbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            maxRange = Mathf.Max(maxRange, ability.GetRange());
        }

        return maxRange;
    }

    // ==================================================
    // CAN ATTACK
    // ==================================================

    public bool CanAttack()
    {
        if (healthManager == null)
            return false;

        if (healthManager.IsDead())
            return false;

        foreach (AbilitySO ability in abilities)
        {
            if (ability != null)
                return true;
        }

        return false;
    }

    // ==================================================
    // TEAM
    // ==================================================

    public Team GetTeam()
    {
        if (healthManager == null)
            return Team.Ally;

        return healthManager.GetTeam();
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

    public void AddAbility(AbilitySO ability)
    {
        if (ability == null || abilities.Contains(ability))
        {
            return;
        }

        abilities.Add(ability);
        abilityCooldowns[ability] = 0;
    }

    public void RemoveAbility(AbilitySO ability)
    {
        if (ability == null)
            return;

        abilities.Remove(ability);
        abilityCooldowns.Remove(ability);
    }

    public AbilitySO GetAbility()
    {
        foreach (AbilitySO ability in abilities)
        {
            if (ability != null)
                return ability;
        }

        return null;
    }

    // ==================================================
    // REFERENCES
    // ==================================================

    public HealthManager GetHealthManager()
    {
        return healthManager;
    }

    public CharacterSO GetCharacterData()
    {
        return characterData;
    }
}