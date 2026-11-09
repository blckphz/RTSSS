
using UnityEngine;

public class UpgradeableCombatUnit : MonoBehaviour
{
    [Header("Runtime Ring Bonuses")]
    [SerializeField] private int bonusHealth;
    [SerializeField] private int bonusDamage;
    [SerializeField] private int bonusRange;

    private HealthManager healthManager;

    private void Awake()
    {
        FindHealthManager();
    }

    private void FindHealthManager()
    {
        healthManager = GetComponent<HealthManager>();

        if (healthManager == null)
        {
            healthManager =
                GetComponentInChildren<HealthManager>(true);
        }

        if (healthManager == null)
        {
            healthManager =
                GetComponentInParent<HealthManager>();
        }
    }

    // =====================================================
    // CHECK UPGRADE
    // =====================================================

    public bool CanApplyUpgrade(UpgradeSO upgrade)
    {
        if (!(upgrade is RingUpgrade ring))
            return false;

        if (ring.GetTarget() != UpgradeTarget.Turret)
            return false;

        return ring.GetStat() != RingStat.ChainBounce;
    }

    // =====================================================
    // APPLY RING
    // =====================================================

    public void ApplyUpgrade(UpgradeSO upgrade)
    {
        if (!CanApplyUpgrade(upgrade))
            return;

        RingUpgrade ring = (RingUpgrade)upgrade;
        int amount = ring.GetBonusAmount();

        switch (ring.GetStat())
        {
            case RingStat.Health:
                bonusHealth += amount;

                if (healthManager == null)
                    FindHealthManager();

                if (healthManager != null)
                    healthManager.AddMaxHealth(amount);
                else
                    Debug.LogWarning(
                        "[UpgradeableCombatUnit] No HealthManager found."
                    );

                break;

            case RingStat.Damage:
                bonusDamage += amount;
                break;

            case RingStat.Range:
                bonusRange += amount;
                break;
        }

        Debug.Log(
            "[UpgradeableCombatUnit] " +
            gameObject.name +
            " received " + ring.GetStat() +
            " ring bonus: +" + amount
        );
    }

    // =====================================================
    // EFFECTIVE DAMAGE
    // =====================================================

    public int GetEffectiveDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + bonusDamage);
    }

    public int GetEffectiveDamage(AbilitySO ability)
    {
        if (ability == null)
            return Mathf.Max(0, bonusDamage);

        return GetEffectiveDamage(ability.GetDamage());
    }

    // =====================================================
    // EFFECTIVE RANGE
    // =====================================================

    public int GetEffectiveRange(int baseRange)
    {
        return Mathf.Max(1, baseRange + bonusRange);
    }

    public int GetEffectiveRange(AbilitySO ability)
    {
        if (ability == null)
            return GetEffectiveRange(0);

        return GetEffectiveRange(ability.GetRange());
    }

    // =====================================================
    // GET BONUSES
    // =====================================================

    public int GetBonusHealth() => bonusHealth;
    public int GetBonusDamage() => bonusDamage;
    public int GetBonusRange() => bonusRange;

    // =====================================================
    // REGISTER TURRET
    // =====================================================

    public void RegisterAsRingUpgradeTarget()
    {
        if (UpdateManager.Instance == null)
        {
            Debug.LogWarning(
                "[UpgradeableCombatUnit] UpdateManager not found."
            );
            return;
        }

        UpdateManager.Instance.RegisterRingUpgradeTarget(this);

        Debug.Log(
            "[UpgradeableCombatUnit] Registered turret: " +
            gameObject.name
        );
    }

    // Compatibility with the previous registration method name.
    public void RegisterAsFortressTarget()
    {
        RegisterAsRingUpgradeTarget();
    }
}
