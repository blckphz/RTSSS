using UnityEngine;

[System.Serializable]
public class AbilityData
{
    private AbilitySO abilitySO;

    private int cooldownRemaining;
    private int usesRemaining;

    public AbilityData(AbilitySO source)
    {
        abilitySO = source;

        cooldownRemaining = 0;

        usesRemaining =
            source != null
                ? source.GetUsesPerTurn()
                : 0;
    }

    public AbilitySO GetAbilitySO()
    {
        return abilitySO;
    }

    public int GetCooldownRemaining()
    {
        return cooldownRemaining;
    }

    public void SetCooldown(int value)
    {
        cooldownRemaining =
            Mathf.Max(0, value);
    }

    public void ReduceCooldown()
    {
        cooldownRemaining =
            Mathf.Max(
                0,
                cooldownRemaining - 1
            );
    }

    public int GetUsesRemaining()
    {
        return usesRemaining;
    }

    public void ResetUses()
    {
        if (abilitySO == null)
        {
            return;
        }

        usesRemaining =
            abilitySO.GetUsesPerTurn();
    }

    public bool CanUse()
    {
        if (abilitySO == null)
        {
            return false;
        }

        // 0 = unlimited uses.
        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        bool result =
            usesRemaining > 0;

        return result;
    }

    public bool IsOnCooldown()
    {
        return cooldownRemaining > 0;
    }

    public bool ConsumeUse()
    {
        if (abilitySO == null)
        {
            return false;
        }

        int maximumUses =
            abilitySO.GetUsesPerTurn();

        // 0 = unlimited uses.
        if (maximumUses <= 0)
        {
            return false;
        }

        if (usesRemaining <= 0)
        {
            return true;
        }

        usesRemaining =
            Mathf.Max(
                0,
                usesRemaining - 1
            );

        bool exhausted =
            usesRemaining == 0;

        return exhausted;
    }

    public override string ToString()
    {
        return
            abilitySO != null
                ? abilitySO.GetAbilityName() +
                  " | Uses=" +
                  usesRemaining +
                  " | Cooldown=" +
                  cooldownRemaining
                : "NULL Ability";
    }
}