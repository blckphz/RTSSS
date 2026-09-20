using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AbilityData
{
    private AbilitySO abilitySO;

    // One cooldown per charge.
    // 0 = ready.
    private List<int> chargeCooldowns =
        new List<int>();


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AbilityData(AbilitySO ability)
    {
        abilitySO = ability;

        InitializeCharges();
    }


    // =========================================================
    // INITIALIZE CHARGES
    // =========================================================

    public void InitializeCharges()
    {
        chargeCooldowns.Clear();

        if (abilitySO == null)
        {
            return;
        }

        int maxUses =
            abilitySO.GetUsesPerTurn();

        // 0 or less = unlimited.
        if (maxUses <= 0)
        {
            return;
        }

        for (int i = 0; i < maxUses; i++)
        {
            chargeCooldowns.Add(0);
        }
    }


    // =========================================================
    // GET ABILITY
    // =========================================================

    public AbilitySO GetAbilitySO()
    {
        return abilitySO;
    }


    // =========================================================
    // COOLDOWN
    // =========================================================

    public int GetCooldownRemaining()
    {
        if (abilitySO == null)
        {
            return 0;
        }

        // Unlimited abilities don't have a cooldown
        // between charges.
        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return 0;
        }

        if (chargeCooldowns.Count == 0)
        {
            return 0;
        }

        int lowestCooldown =
            int.MaxValue;

        foreach (int cooldown
                 in chargeCooldowns)
        {
            if (cooldown < lowestCooldown)
            {
                lowestCooldown = cooldown;
            }
        }

        if (lowestCooldown ==
            int.MaxValue)
        {
            return 0;
        }

        return lowestCooldown;
    }


    public void SetCooldown(int value)
    {
        value = Mathf.Max(0, value);

        for (int i = 0;
             i < chargeCooldowns.Count;
             i++)
        {
            chargeCooldowns[i] = value;
        }
    }


    public void ReduceCooldown()
    {
        for (int i = 0;
             i < chargeCooldowns.Count;
             i++)
        {
            chargeCooldowns[i] =
                Mathf.Max(
                    0,
                    chargeCooldowns[i] - 1
                );
        }
    }


    public bool IsOnCooldown()
    {
        if (abilitySO == null)
        {
            return false;
        }

        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return false;
        }

        foreach (int cooldown
                 in chargeCooldowns)
        {
            if (cooldown > 0)
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // USES
    // =========================================================

    public int GetUsesRemaining()
    {
        if (abilitySO == null)
        {
            return 0;
        }

        // 0 = unlimited.
        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return 0;
        }

        int readyCharges = 0;

        foreach (int cooldown
                 in chargeCooldowns)
        {
            if (cooldown <= 0)
            {
                readyCharges++;
            }
        }

        return readyCharges;
    }


    public void ResetUses()
    {
        if (abilitySO == null)
        {
            return;
        }

        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return;
        }

        for (int i = 0;
             i < chargeCooldowns.Count;
             i++)
        {
            chargeCooldowns[i] = 0;
        }
    }


    // =========================================================
    // CAN USE
    // =========================================================

    public bool CanUse()
    {
        if (abilitySO == null)
        {
            return false;
        }

        // Unlimited.
        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        return GetUsesRemaining() > 0;
    }


    // =========================================================
    // CONSUME USE
    // =========================================================

    public bool ConsumeUse()
    {
        if (abilitySO == null)
        {
            return false;
        }

        // Unlimited ability.
        // This is a successful use.
        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        int abilityCooldown =
            Mathf.Max(
                0,
                abilitySO.GetCooldown()
            );

        for (int i = 0;
             i < chargeCooldowns.Count;
             i++)
        {
            if (chargeCooldowns[i] <= 0)
            {
                chargeCooldowns[i] =
                    abilityCooldown;

                return true;
            }
        }

        return false;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    public override string ToString()
    {
        if (abilitySO == null)
        {
            return "AbilityData: NULL";
        }

        return
            abilitySO.GetAbilityName()
            + " | Uses: "
            + GetUsesRemaining()
            + " | Cooldown: "
            + GetCooldownRemaining();
    }
}