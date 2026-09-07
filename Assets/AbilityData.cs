using UnityEngine;

[System.Serializable]
public class AbilityData
{
// ============================================================
// SOURCE DATA
// ============================================================

private AbilitySO abilitySO;


    // ============================================================
    // RUNTIME DATA
    // ============================================================

    private int cooldownRemaining;

    private int usesRemaining;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public AbilityData(
        AbilitySO source
    )
    {
        abilitySO =
            source;

        cooldownRemaining =
            0;

        usesRemaining =
            source != null
                ? source.GetUsesPerTurn()
                : 0;
    }


    // ============================================================
    // SOURCE
    // ============================================================

    public AbilitySO GetAbilitySO()
    {
        return abilitySO;
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    public int GetCooldownRemaining()
    {
        return cooldownRemaining;
    }


    public void SetCooldown(
        int value
    )
    {
        cooldownRemaining =
            Mathf.Max(
                0,
                value
            );
    }


    public void ReduceCooldown()
    {
        cooldownRemaining =
            Mathf.Max(
                0,
                cooldownRemaining - 1
            );
    }


    // ============================================================
    // USES
    // ============================================================

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


    public bool ConsumeUse()
    {
        if (abilitySO == null)
        {
            return false;
        }


        if (
            abilitySO
                .GetUsesPerTurn() <= 0
        )
        {
            return false;
        }


        usesRemaining =
            Mathf.Max(
                0,
                usesRemaining - 1
            );


        return
            usesRemaining <= 0;
    }

}
