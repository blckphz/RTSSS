using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; private set; }


    // =========================================================
    // CURRENT PLAYER
    // =========================================================

    [Header("Current Player")]

    [SerializeField]
    private UnitData currentUnit;

    [SerializeField]
    private CharacterSO currentCharacter;


    // =========================================================
    // FORTRESS / TURRET
    // =========================================================

    [Header("Fortress / Turret")]

    [Tooltip(
        "The current turret/pet that receives FortressUpgrade."
    )]
    [SerializeField]
    private UpgradeableCombatUnit fortressTarget;


    // =========================================================
    // PURCHASED UPGRADES
    // =========================================================

    private readonly List<UpgradeSO> purchasedUpgrades =
        new List<UpgradeSO>();


    // =========================================================
    // FORTRESS TRACKING
    // =========================================================

    private UpgradeableCombatUnit lastFortressTarget;

    private int lastAppliedFortressUpgradeCount;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // CURRENT CHARACTER
    // =========================================================

    public void SetCurrentCharacter(
        CharacterSO character
    )
    {
        currentCharacter = character;

        if (character == null)
        {
            return;
        }
    }


    public CharacterSO GetCurrentCharacter()
    {
        return currentCharacter;
    }


    // =========================================================
    // CURRENT UNIT
    // =========================================================

    public void SetCurrentUnit(
        UnitData unit
    )
    {
        if (unit == null)
        {
            return;
        }

        CharacterSO unitCharacter =
            unit.GetCharacter();

        if (unitCharacter == null)
        {
            return;
        }

        // Only player characters can become
        // the current unit.

        if (!unitCharacter.isPlayerCharacter)
        {
            return;
        }

        // Prevent duplicate registration.

        if (currentUnit == unit)
        {
            return;
        }

        currentUnit = unit;
        currentCharacter = unitCharacter;

        // Reapply normal player upgrades.

        ApplyRuntimeUpgradesToUnit(unit);

        // Fortress upgrades are NOT applied here.
        // The actual turret/pet registers itself when spawned.
    }


    public UnitData GetCurrentUnit()
    {
        return currentUnit;
    }


    public void ClearCurrentUnit()
    {
        currentUnit = null;
    }


    // =========================================================
    // FORTRESS TARGET REGISTRATION
    // =========================================================

    public void RegisterFortressTarget(
        UpgradeableCombatUnit target
    )
    {
        if (target == null)
        {
            return;
        }

        Debug.Log(
            $"[UpdateManager] RegisterFortressTarget | " +
            $"Target={target.gameObject.name}",
            target
        );


        // -----------------------------------------------------
        // SAME TARGET
        // -----------------------------------------------------

        if (fortressTarget == target)
        {
            Debug.Log(
                $"[UpdateManager] {target.gameObject.name} " +
                $"is already the Fortress target.",
                target
            );

            ApplyStoredFortressUpgrades(target);

            return;
        }


        // -----------------------------------------------------
        // NEW TARGET
        // -----------------------------------------------------

        fortressTarget = target;

        lastFortressTarget = target;

        // New turret has received none of the stored upgrades.

        lastAppliedFortressUpgradeCount = 0;


        Debug.Log(
            $"[UpdateManager] New Fortress target registered: " +
            $"{target.gameObject.name}",
            target
        );


        // -----------------------------------------------------
        // APPLY STORED UPGRADES
        // -----------------------------------------------------

        ApplyStoredFortressUpgrades(target);
    }


    // =========================================================
    // APPLY UPGRADE
    // =========================================================

    public void ApplyUpgrade(
        UpgradeSO upgrade
    )
    {
        if (upgrade == null)
        {
            return;
        }

        Debug.Log(
            $"[UpdateManager] ApplyUpgrade | " +
            $"Upgrade={upgrade.name} | " +
            $"Type={upgrade.GetType().Name}",
            this
        );


        // =====================================================
        // FORTRESS UPGRADE
        // =====================================================

        FortressUpgrade fortressUpgrade =
            upgrade as FortressUpgrade;

        if (fortressUpgrade != null)
        {
            // Store purchase.

            purchasedUpgrades.Add(upgrade);

            Debug.Log(
                $"[UpdateManager] FortressUpgrade purchased: " +
                $"{upgrade.name}",
                this
            );


            // -------------------------------------------------
            // APPLY TO CURRENT TURRET
            // -------------------------------------------------

            if (fortressTarget != null)
            {
                ApplyFortressUpgrade(fortressUpgrade);

                // IMPORTANT:
                // This upgrade has now been applied to the
                // current target, so advance the counter.
                lastAppliedFortressUpgradeCount =
                    purchasedUpgrades.Count;

                lastFortressTarget =
                    fortressTarget;
            }
            else
            {
                Debug.Log(
                    "[UpdateManager] No Fortress target currently " +
                    "exists. Upgrade stored for the next turret/pet.",
                    this
                );
            }

            return;
        }


        // =====================================================
        // STORE NORMAL PLAYER UPGRADE
        // =====================================================

        purchasedUpgrades.Add(upgrade);


        // =====================================================
        // CHAIN BOUNCE
        // =====================================================

        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            if (currentUnit != null)
            {
                ApplyUpgradeToUnit(
                    upgrade,
                    currentUnit
                );
            }

            return;
        }


        // =====================================================
        // ENGINEER MELEE TURRET HEAL
        // =====================================================

        EngineerMeleeTurretHealUpgrade
            turretHealUpgrade =
            upgrade as EngineerMeleeTurretHealUpgrade;

        if (turretHealUpgrade != null)
        {
            if (currentUnit != null)
            {
                ApplyUpgradeToUnit(
                    upgrade,
                    currentUnit
                );
            }

            return;
        }


        // =====================================================
        // RUSTY UPGRADES
        // =====================================================

        RustyUpgrades rustyUpgrade =
            upgrade as RustyUpgrades;

        if (rustyUpgrade != null)
        {
            if (currentCharacter != null)
            {
                rustyUpgrade.Apply(
                    currentCharacter
                );
            }

            return;
        }
    }


    // =========================================================
    // APPLY FORTRESS UPGRADE TO CURRENT TARGET
    // =========================================================

    private void ApplyFortressUpgrade(
        FortressUpgrade upgrade
    )
    {
        if (upgrade == null)
        {
            return;
        }

        if (fortressTarget == null)
        {
            Debug.Log(
                "[UpdateManager] Fortress target is currently null. " +
                "Upgrade will remain stored.",
                this
            );

            return;
        }

        Debug.Log(
            $"[UpdateManager] Applying FortressUpgrade to " +
            $"{fortressTarget.gameObject.name}: " +
            $"{upgrade.name}",
            fortressTarget
        );

        fortressTarget.ApplyUpgrade(upgrade);

        lastFortressTarget =
            fortressTarget;
    }


    // =========================================================
    // APPLY STORED FORTRESS UPGRADES
    // =========================================================

    private void ApplyStoredFortressUpgrades(
        UpgradeableCombatUnit target
    )
    {
        if (target == null)
        {
            return;
        }


        if (purchasedUpgrades.Count == 0)
        {
            Debug.Log(
                $"[UpdateManager] No purchased upgrades to apply " +
                $"to {target.gameObject.name}.",
                target
            );

            return;
        }


        // -----------------------------------------------------
        // NEW TARGET
        // -----------------------------------------------------

        if (lastFortressTarget != target)
        {
            lastAppliedFortressUpgradeCount = 0;

            lastFortressTarget = target;
        }


        // -----------------------------------------------------
        // SAME TARGET - NOTHING NEW
        // -----------------------------------------------------

        if (
            lastAppliedFortressUpgradeCount >=
            purchasedUpgrades.Count
        )
        {
            Debug.Log(
                $"[UpdateManager] All stored Fortress upgrades " +
                $"already applied to {target.gameObject.name}.",
                target
            );

            return;
        }


        // -----------------------------------------------------
        // APPLY ONLY NEW FORTRESS UPGRADES
        // -----------------------------------------------------

        for (
            int i = lastAppliedFortressUpgradeCount;
            i < purchasedUpgrades.Count;
            i++
        )
        {
            UpgradeSO upgrade =
                purchasedUpgrades[i];

            if (upgrade == null)
            {
                continue;
            }

            FortressUpgrade fortressUpgrade =
                upgrade as FortressUpgrade;

            // Ignore normal player upgrades.

            if (fortressUpgrade == null)
            {
                continue;
            }

            Debug.Log(
                $"[UpdateManager] Reapplying stored FortressUpgrade " +
                $"to {target.gameObject.name}: " +
                $"{fortressUpgrade.name}",
                target
            );

            target.ApplyUpgrade(
                fortressUpgrade
            );
        }


        // Everything currently stored has now been processed.

        lastAppliedFortressUpgradeCount =
            purchasedUpgrades.Count;

        lastFortressTarget =
            target;
    }


    // =========================================================
    // APPLY UPGRADE TO SPECIFIC PLAYER UNIT
    // =========================================================

    public void ApplyUpgradeToUnit(
        UpgradeSO upgrade,
        UnitData unit
    )
    {
        if (upgrade == null)
        {
            return;
        }

        if (unit == null)
        {
            return;
        }

        CharacterSO character =
            unit.GetCharacter();

        if (character == null)
        {
            return;
        }

        // Never apply player upgrades to enemies.

        if (!character.isPlayerCharacter)
        {
            return;
        }


        // -----------------------------------------------------
        // CHAIN BOUNCE
        // -----------------------------------------------------

        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            chainBounceUpgrade.ApplyToUnit(unit);

            return;
        }


        // -----------------------------------------------------
        // ENGINEER MELEE TURRET HEAL
        // -----------------------------------------------------

        EngineerMeleeTurretHealUpgrade
            turretHealUpgrade =
            upgrade as
            EngineerMeleeTurretHealUpgrade;

        if (turretHealUpgrade != null)
        {
            turretHealUpgrade.ApplyToUnit(unit);

            return;
        }
    }


    // =========================================================
    // APPLY STORED PLAYER UPGRADES
    // =========================================================

    private void ApplyRuntimeUpgradesToUnit(
        UnitData unit
    )
    {
        if (unit == null)
        {
            return;
        }

        CharacterSO character =
            unit.GetCharacter();

        if (character == null)
        {
            return;
        }

        if (!character.isPlayerCharacter)
        {
            return;
        }

        if (purchasedUpgrades.Count == 0)
        {
            return;
        }


        for (
            int i = 0;
            i < purchasedUpgrades.Count;
            i++
        )
        {
            UpgradeSO upgrade =
                purchasedUpgrades[i];

            if (upgrade == null)
            {
                continue;
            }


            // Fortress upgrades NEVER go to
            // the player character.

            if (upgrade is FortressUpgrade)
            {
                continue;
            }


            // -------------------------------------------------
            // CHAIN BOUNCE
            // -------------------------------------------------

            if (
                upgrade is
                ChainBounceUpgrade
            )
            {
                ApplyUpgradeToUnit(
                    upgrade,
                    unit
                );

                continue;
            }


            // -------------------------------------------------
            // ENGINEER TURRET HEAL
            // -------------------------------------------------

            if (
                upgrade is
                EngineerMeleeTurretHealUpgrade
            )
            {
                ApplyUpgradeToUnit(
                    upgrade,
                    unit
                );

                continue;
            }
        }
    }


    // =========================================================
    // NEW GAME
    // =========================================================

    public void StartNewGame()
    {
        purchasedUpgrades.Clear();


        if (currentUnit != null)
        {
            currentUnit.ResetRuntimeUpgrades();
        }


        currentUnit = null;
        currentCharacter = null;

        fortressTarget = null;
        lastFortressTarget = null;

        lastAppliedFortressUpgradeCount = 0;
    }


    // =========================================================
    // RESET ALL UPGRADES
    // =========================================================

    public void ResetAllUpgrades()
    {
        purchasedUpgrades.Clear();


        if (currentUnit != null)
        {
            currentUnit.ResetRuntimeUpgrades();
        }


        fortressTarget = null;
        lastFortressTarget = null;

        lastAppliedFortressUpgradeCount = 0;
    }


    // =========================================================
    // UPGRADE INFORMATION
    // =========================================================

    public int GetPurchasedUpgradeCount()
    {
        return purchasedUpgrades.Count;
    }


    public List<UpgradeSO> GetPurchasedUpgrades()
    {
        return purchasedUpgrades;
    }


    // =========================================================
    // CHARACTER UPGRADE POOL
    // =========================================================

    public UpgradeSO[] GetCurrentCharacterUpgrades()
    {
        if (currentCharacter == null)
        {
            return null;
        }

        int count =
            currentCharacter.GetUpgradeCount();

        if (count <= 0)
        {
            return null;
        }

        UpgradeSO[] result =
            new UpgradeSO[count];

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            result[i] =
                currentCharacter.GetUpgrade(i);
        }

        return result;
    }
}