using System.Collections.Generic;
using UnityEngine;

public class turretbehav : MonoBehaviour, IconditionsPuller
{
    [Header("Chain Lightning")]
    [SerializeField, Min(0)]
    private int chainLightningCharges = 0;

    [Header("Chain Lightning Buffs")]
    [SerializeField]
    private int chargedDamageBonus = 10;

    [SerializeField]
    private int chargedMaxTargetsBonus = 1;

    [Header("Charge Visual")]
    [SerializeField]
    private ParticleSystem chargeParticles;

    [Header("Charge System")]
    [SerializeField]
    private bool chainLightningUnlocked = false;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        UpdateChargeParticles();
    }


    // ============================================================
    // UNLOCK CHAIN LIGHTNING
    // ============================================================

    public void UnlockChainLightning()
    {
        chainLightningUnlocked = true;

        Debug.Log(
            gameObject.name +
            " unlocked Chain Lightning."
        );

        UpdateChargeParticles();
    }


    // ============================================================
    // CHECK IF UNLOCKED
    // ============================================================

    public bool IsChainLightningUnlocked()
    {
        return chainLightningUnlocked;
    }


    // ============================================================
    // CHECK IF CHARGED
    // ============================================================

    public bool IsChainLightningCharged()
    {
        return chainLightningUnlocked &&
               chainLightningCharges > 0;
    }


    // ============================================================
    // DAMAGE BONUS
    // ============================================================

    public int GetChainLightningDamageBonus()
    {
        if (!IsChainLightningCharged())
            return 0;

        return chargedDamageBonus;
    }


    // ============================================================
    // MAX TARGETS BONUS
    // ============================================================

    public int GetChainLightningMaxTargetsBonus()
    {
        if (!IsChainLightningCharged())
            return 0;

        return chargedMaxTargetsBonus;
    }


    // ============================================================
    // ADD ONE CHARGE
    // ============================================================

    public void AddChainLightningCharge()
    {
        if (!chainLightningUnlocked)
        {
            return;
        }

        AddChainLightningCharges(1);
    }


    // ============================================================
    // ADD MULTIPLE CHARGES
    // ============================================================

    public void AddChainLightningCharges(int amount)
    {
        if (!chainLightningUnlocked)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        // Was completely uncharged before adding?
        bool wasUncharged =
            chainLightningCharges <= 0;

        chainLightningCharges += amount;


        // ========================================================
        // CHARGE BEGAN
        // ========================================================

        if (wasUncharged)
        {
            AddUseToFirstAbility();
        }


        Debug.Log(
            gameObject.name +
            " gained " +
            amount +
            " Chain Lightning charge(s). " +
            "Total charges: " +
            chainLightningCharges
        );

        UpdateChargeParticles();
    }


    // ============================================================
    // ADD USE TO FIRST ABILITY
    // ============================================================

    private void AddUseToFirstAbility()
    {
        AttackUnit attackUnit =
            GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            attackUnit =
                GetComponentInParent<AttackUnit>();
        }

        if (attackUnit == null)
        {
            attackUnit =
                GetComponentInChildren<AttackUnit>();
        }

        if (attackUnit == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " could not add Chain Lightning ability use " +
                "because no AttackUnit was found."
            );

            return;
        }

        List<AbilityData> runtimeAbilities =
            attackUnit.GetRuntimeAbilities();

        if (runtimeAbilities == null ||
            runtimeAbilities.Count == 0)
        {
            Debug.LogWarning(
                gameObject.name +
                " could not add Chain Lightning ability use " +
                "because the AttackUnit has no abilities."
            );

            return;
        }

        AbilityData firstAbility =
            runtimeAbilities[0];

        if (firstAbility == null)
        {
            return;
        }

        firstAbility.AddUse();

        Debug.Log(
            gameObject.name +
            " started a Chain Lightning charge. " +
            "Added 1 use to first ability. " +
            "First ability uses now: " +
            firstAbility.GetUsesRemaining()
        );
    }


    // ============================================================
    // GET CHARGES
    // ============================================================

    public int GetChainLightningCharges()
    {
        return chainLightningCharges;
    }


    // ============================================================
    // CONSUME ONE CHARGE
    // ============================================================

    public void ConsumeChainLightningCharge()
    {
        if (!chainLightningUnlocked)
        {
            return;
        }

        if (chainLightningCharges <= 0)
        {
            return;
        }

        chainLightningCharges--;

        Debug.Log(
            gameObject.name +
            " lost 1 Chain Lightning charge. " +
            "Remaining charges: " +
            chainLightningCharges
        );

        UpdateChargeParticles();
    }


    // ============================================================
    // CLEAR CHARGES
    // ============================================================

    public void ClearChainLightningCharges()
    {
        chainLightningCharges = 0;

        UpdateChargeParticles();
    }


    // ============================================================
    // CONDITIONS
    // ============================================================

    public List<string> GetConditions()
    {
        List<string> conditions =
            new List<string>();

        if (IsChainLightningCharged())
        {
            conditions.Add(
                $"Chain Lightning Charged ({chainLightningCharges})"
            );
        }

        return conditions;
    }


    // ============================================================
    // UPDATE PARTICLES
    // ============================================================

    private void UpdateChargeParticles()
    {
        if (chargeParticles == null)
        {
            return;
        }

        bool charged =
            IsChainLightningCharged();

        if (charged)
        {
            if (!chargeParticles.isPlaying)
            {
                chargeParticles.Play();
            }
        }
        else
        {
            if (chargeParticles.isPlaying)
            {
                chargeParticles.Stop();
            }
        }
    }
}