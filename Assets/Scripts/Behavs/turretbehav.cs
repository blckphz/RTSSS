using System.Collections.Generic;
using UnityEngine;

public class turretbehav : MonoBehaviour, IconditionsPuller
{
    [Header("Chain Lightning")]
    [SerializeField, Min(0)]
    private int chainLightningCharges = 0;

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
    // ENABLE CHAIN LIGHTNING
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

        chainLightningCharges += amount;

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

        if (
            chainLightningUnlocked &&
            chainLightningCharges > 0
        )
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
            chainLightningUnlocked &&
            chainLightningCharges > 0;

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