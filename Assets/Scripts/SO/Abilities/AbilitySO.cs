using UnityEngine;

public abstract class AbilitySO : ScriptableObject
{
    [Header("Ability")]
    [SerializeField] private string abilityName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Combat")]
    [SerializeField] private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [SerializeField, Min(1)]
    private int range = 1;

    // ==================================================
    // GETTERS
    // ==================================================

    public string GetAbilityName()
    {
        return abilityName;
    }

    public string GetDescription()
    {
        return description;
    }

    public int GetDamage()
    {
        return damage;
    }

    public int GetCooldown()
    {
        return cooldown;
    }

    public int GetRange()
    {
        return range;
    }

    // ==================================================
    // USE ABILITY
    // ==================================================

    public virtual bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null)
        {
            Debug.LogWarning(
                $"[AbilitySO] {abilityName}: " +
                "User is null."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[AbilitySO] {abilityName}: " +
                "Target is null."
            );

            return false;
        }

        Debug.Log(
            $"[AbilitySO] {user.name} uses " +
            $"{abilityName} on {target.name}."
        );

        return true;
    }
}