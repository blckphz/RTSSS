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
    // PURCHASED UPGRADES
    // =========================================================

    private readonly List<UpgradeSO> purchasedUpgrades =
        new List<UpgradeSO>();


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
        CharacterSO character)
    {
        currentCharacter =
            character;


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
        UnitData unit)
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


        currentUnit =
            unit;


        currentCharacter =
            unitCharacter;


        // Reapply every previously purchased runtime upgrade.

        ApplyRuntimeUpgradesToUnit(
            unit
        );
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
    // APPLY UPGRADE
    // =========================================================

    public void ApplyUpgrade(
        UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            return;
        }


        // Remember the upgrade so that it can be reapplied
        // if the player unit is recreated.

        purchasedUpgrades.Add(
            upgrade
        );


        // -----------------------------------------------------
        // CHAIN BOUNCE
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // ENGINEER MELEE TURRET HEAL
        // -----------------------------------------------------

        EngineerMeleeTurretHealUpgrade
            turretHealUpgrade =
            upgrade as
            EngineerMeleeTurretHealUpgrade;


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


        // -----------------------------------------------------
        // RUSTY UPGRADES
        // -----------------------------------------------------

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
    // APPLY UPGRADE TO SPECIFIC UNIT
    // =========================================================

    public void ApplyUpgradeToUnit(
        UpgradeSO upgrade,
        UnitData unit)
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
            chainBounceUpgrade.ApplyToUnit(
                unit
            );


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
            turretHealUpgrade.ApplyToUnit(
                unit
            );


            return;
        }
    }


    // =========================================================
    // APPLY STORED RUNTIME UPGRADES
    // =========================================================

    private void ApplyRuntimeUpgradesToUnit(
        UnitData unit)
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


        // Only players receive stored upgrades.

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


            // -------------------------------------------------
            // CHAIN BOUNCE
            // -------------------------------------------------

            ChainBounceUpgrade chainBounceUpgrade =
                upgrade as ChainBounceUpgrade;


            if (chainBounceUpgrade != null)
            {
                ApplyUpgradeToUnit(
                    upgrade,
                    unit
                );


                continue;
            }


            // -------------------------------------------------
            // ENGINEER MELEE TURRET HEAL
            // -------------------------------------------------

            EngineerMeleeTurretHealUpgrade
                turretHealUpgrade =
                upgrade as
                EngineerMeleeTurretHealUpgrade;


            if (turretHealUpgrade != null)
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
