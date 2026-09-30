using UnityEngine;

public class UpgradeableCombatUnit :
    MonoBehaviour,
    IUpgradeableCombatUnit
{
    // ==================================================
    // RUNTIME UPGRADE BONUSES
    // ==================================================

    [Header("Runtime Upgrade Bonuses")]

    [SerializeField]
    private int bonusHealth = 0;

    [SerializeField]
    private int bonusDamage = 0;

    [SerializeField]
    private int bonusRange = 0;


    // ==================================================
    // REFERENCES
    // ==================================================

    private HealthManager healthManager;
    private AttackUnit attackUnit;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        Debug.Log(
            $"[UpgradeableCombatUnit] Awake on {gameObject.name}",
            this
        );

        FindReferences();

        Debug.Log(
            $"[UpgradeableCombatUnit] References on " +
            $"{gameObject.name} | " +
            $"HealthManager = " +
            $"{(healthManager != null ? healthManager.gameObject.name : "NULL")} | " +
            $"AttackUnit = " +
            $"{(attackUnit != null ? attackUnit.gameObject.name : "NULL")}",
            this
        );
    }


    // ==================================================
    // FIND REFERENCES
    // ==================================================

    private void FindReferences()
    {
        // ----------------------------------------------
        // HEALTH MANAGER
        // ----------------------------------------------

        healthManager =
            GetComponent<HealthManager>();

        if (healthManager == null)
        {
            healthManager =
                GetComponentInParent<HealthManager>();
        }

        if (healthManager == null)
        {
            healthManager =
                GetComponentInChildren<HealthManager>();
        }


        // ----------------------------------------------
        // ATTACK UNIT
        // ----------------------------------------------

        attackUnit =
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


        Debug.Log(
            $"[UpgradeableCombatUnit] FindReferences() on " +
            $"{gameObject.name} | " +
            $"HealthManager=" +
            $"{(healthManager != null ? healthManager.gameObject.name : "NULL")} | " +
            $"AttackUnit=" +
            $"{(attackUnit != null ? attackUnit.gameObject.name : "NULL")}",
            this
        );
    }


    // ==================================================
    // FORTRESS TARGET REGISTRATION
    // ==================================================

    public void RegisterAsFortressTarget()
    {
        UpdateManager updateManager =
            FindFirstObjectByType<UpdateManager>();

        if (updateManager == null)
        {
            Debug.LogWarning(
                $"[UpgradeableCombatUnit] Could not find " +
                $"UpdateManager when registering " +
                $"{gameObject.name} as Fortress target.",
                this
            );

            return;
        }

        Debug.Log(
            $"[UpgradeableCombatUnit] Registering " +
            $"{gameObject.name} as Fortress target.",
            this
        );

        updateManager.RegisterFortressTarget(this);
    }


    // ==================================================
    // INTERFACE
    // ==================================================

    public bool CanApplyUpgrade(
        UpgradeSO upgrade
    )
    {
        if (upgrade == null)
        {
            return false;
        }

        return upgrade is FortressUpgrade;
    }


    // ==================================================
    // APPLY UPGRADE
    // ==================================================

    public void ApplyUpgrade(
        UpgradeSO upgrade
    )
    {
        if (upgrade == null)
        {
            Debug.LogWarning(
                $"[UpgradeableCombatUnit] ApplyUpgrade received " +
                $"NULL on {gameObject.name}",
                this
            );

            return;
        }

        Debug.Log(
            $"[UpgradeableCombatUnit] ApplyUpgrade on " +
            $"{gameObject.name} | " +
            $"Upgrade={upgrade.name} | " +
            $"Type={upgrade.GetType().Name}",
            this
        );

        if (!CanApplyUpgrade(upgrade))
        {
            Debug.LogWarning(
                $"[UpgradeableCombatUnit] Cannot apply upgrade " +
                $"{upgrade.name} to {gameObject.name}",
                this
            );

            return;
        }

        FortressUpgrade fortressUpgrade =
            upgrade as FortressUpgrade;

        if (fortressUpgrade == null)
        {
            Debug.LogWarning(
                $"[UpgradeableCombatUnit] Upgrade is not a " +
                $"FortressUpgrade on {gameObject.name}",
                this
            );

            return;
        }

        Debug.Log(
            $"[UpgradeableCombatUnit] Fortress upgrade values | " +
            $"HP +{fortressUpgrade.GetBonusHealth()} | " +
            $"Damage +{fortressUpgrade.GetBonusDamage()} | " +
            $"Range +{fortressUpgrade.GetBonusRange()}",
            this
        );

        ApplyFortressUpgrade(
            fortressUpgrade
        );
    }


    // ==================================================
    // APPLY FORTRESS UPGRADE
    // ==================================================

    private void ApplyFortressUpgrade(
        FortressUpgrade upgrade
    )
    {
        if (upgrade == null)
        {
            return;
        }


        // ----------------------------------------------
        // MAKE SURE REFERENCES EXIST
        // ----------------------------------------------

        if (
            healthManager == null ||
            attackUnit == null
        )
        {
            FindReferences();
        }


        // ----------------------------------------------
        // GET UPGRADE VALUES
        // ----------------------------------------------

        int healthBonus =
            upgrade.GetBonusHealth();

        int damageBonus =
            upgrade.GetBonusDamage();

        int rangeBonus =
            upgrade.GetBonusRange();


        // ----------------------------------------------
        // BEFORE DEBUG
        // ----------------------------------------------

        Debug.Log(
            $"[UpgradeableCombatUnit] BEFORE upgrade on " +
            $"{gameObject.name} | " +
            $"MaxHP=" +
            $"{(healthManager != null ? healthManager.GetMaxHealth().ToString() : "NULL")} | " +
            $"HP=" +
            $"{(healthManager != null ? healthManager.GetHealth().ToString() : "NULL")} | " +
            $"BonusHP={bonusHealth} | " +
            $"BonusDamage={bonusDamage} | " +
            $"BonusRange={bonusRange}",
            this
        );


        // ----------------------------------------------
        // HEALTH
        // ----------------------------------------------

        if (healthBonus > 0)
        {
            bonusHealth +=
                healthBonus;

            if (healthManager != null)
            {
                healthManager.AddMaxHealth(
                    healthBonus
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[UpgradeableCombatUnit] No HealthManager " +
                    $"found for HP upgrade on {gameObject.name}",
                    this
                );
            }
        }


        // ----------------------------------------------
        // DAMAGE
        // ----------------------------------------------

        if (damageBonus > 0)
        {
            bonusDamage +=
                damageBonus;
        }


        // ----------------------------------------------
        // RANGE
        // ----------------------------------------------

        if (rangeBonus > 0)
        {
            bonusRange +=
                rangeBonus;
        }


        // ----------------------------------------------
        // AFTER DEBUG
        // ----------------------------------------------

        Debug.Log(
            $"[UpgradeableCombatUnit] AFTER upgrade on " +
            $"{gameObject.name} | " +
            $"MaxHP=" +
            $"{(healthManager != null ? healthManager.GetMaxHealth().ToString() : "NULL")} | " +
            $"HP=" +
            $"{(healthManager != null ? healthManager.GetHealth().ToString() : "NULL")} | " +
            $"BonusHP={bonusHealth} | " +
            $"BonusDamage={bonusDamage} | " +
            $"BonusRange={bonusRange}",
            this
        );
    }


    // ==================================================
    // EFFECTIVE HEALTH
    // ==================================================

    public int GetEffectiveHealth()
    {
        if (healthManager == null)
        {
            FindReferences();
        }

        if (healthManager != null)
        {
            return healthManager.GetMaxHealth();
        }

        return bonusHealth;
    }


    // ==================================================
    // EFFECTIVE DAMAGE
    // ==================================================

    public int GetEffectiveDamage(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return 0;
        }

        int baseDamage =
            abilitySO.GetDamage();

        int totalDamage =
            baseDamage +
            bonusDamage;

        Debug.Log(
            $"[UpgradeableCombatUnit] Effective Damage on " +
            $"{gameObject.name} | " +
            $"Base={baseDamage} | " +
            $"Bonus={bonusDamage} | " +
            $"Total={totalDamage}",
            this
        );

        return totalDamage;
    }


    // ==================================================
    // EFFECTIVE RANGE
    // ==================================================

    public int GetEffectiveRange(
        AbilitySO ability
    )
    {
        if (ability == null)
        {
            return Mathf.Max(
                1,
                bonusRange
            );
        }

        int baseRange =
            ability.GetRange();

        int totalRange =
            Mathf.Max(
                1,
                baseRange + bonusRange
            );

        Debug.Log(
            $"[UpgradeableCombatUnit] Effective Range on " +
            $"{gameObject.name} | " +
            $"Base={baseRange} | " +
            $"Bonus={bonusRange} | " +
            $"Total={totalRange}",
            this
        );

        return totalRange;
    }


    // ==================================================
    // BONUS GETTERS
    // ==================================================

    public int GetBonusHealth()
    {
        return bonusHealth;
    }


    public int GetBonusDamage()
    {
        return bonusDamage;
    }


    public int GetBonusRange()
    {
        return bonusRange;
    }


    // ==================================================
    // CURRENT HEALTH
    // ==================================================

    public int GetCurrentHealth()
    {
        if (healthManager == null)
        {
            FindReferences();
        }

        if (healthManager == null)
        {
            return 0;
        }

        return healthManager.GetHealth();
    }


    // ==================================================
    // MAXIMUM HEALTH
    // ==================================================

    public int GetMaximumHealth()
    {
        if (healthManager == null)
        {
            FindReferences();
        }

        if (healthManager == null)
        {
            return bonusHealth;
        }

        return healthManager.GetMaxHealth();
    }


    // ==================================================
    // RESET UPGRADES
    // ==================================================

    public void ResetUpgrades()
    {
        bonusHealth = 0;
        bonusDamage = 0;
        bonusRange = 0;

        Debug.Log(
            $"[UpgradeableCombatUnit] Reset upgrades on " +
            $"{gameObject.name}",
            this
        );
    }
}