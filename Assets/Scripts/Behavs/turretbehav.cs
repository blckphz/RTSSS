using UnityEngine;

public class turretbehav : MonoBehaviour
{
    [Header("Chain Lightning")]
    [SerializeField, Min(0)]
    private int chainLightningCharges = 0;

    [Header("Charge Visual")]
    [SerializeField]
    private ParticleSystem chargeParticles;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        UpdateChargeParticles();
    }


    // ============================================================
    // ADD ONE CHARGE
    // ============================================================

    public void AddChainLightningCharge()
    {
        AddChainLightningCharges(1);
    }


    // ============================================================
    // ADD MULTIPLE CHARGES
    // ============================================================

    public void AddChainLightningCharges(
        int amount)
    {
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
    // CLEAR CHARGES
    // ============================================================

    public void ClearChainLightningCharges()
    {
        chainLightningCharges = 0;

        UpdateChargeParticles();
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
            chainLightningCharges > 0;

        if (charged)
        {
            chargeParticles.Play();
        }
        else
        {
            chargeParticles.Stop();
        }
    }
}