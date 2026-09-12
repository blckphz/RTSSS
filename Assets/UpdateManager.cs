using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; private set; }

    [Header("Current Player")]
    [SerializeField] private UnitData currentUnit;
    [SerializeField] private CharacterSO currentCharacter;

    [Header("Purchased Upgrades")]
    private readonly List<UpgradeSO> purchasedUpgrades = new List<UpgradeSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // CURRENT CHARACTER
    // =========================================================

    public void SetCurrentCharacter(CharacterSO character)
    {
        currentCharacter = character;

        if (character == null)
        {
            Debug.LogWarning(
                "[UpdateManager] SetCurrentCharacter received NULL."
            );

            return;
        }

        Debug.Log(
            $"[UpdateManager] Current Character set to: " +
            $"{character.characterName}"
        );
    }

    public CharacterSO GetCurrentCharacter()
    {
        return currentCharacter;
    }

    // =========================================================
    // CURRENT UNIT
    // =========================================================

    public void SetCurrentUnit(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning(
                "[UpdateManager] SetCurrentUnit received NULL."
            );

            return;
        }

        // Make sure this is actually a player character.
        CharacterSO unitCharacter = unit.GetCharacter();

        if (unitCharacter == null)
        {
            Debug.LogWarning(
                $"[UpdateManager] Cannot register UnitData " +
                $"{unit.name}. CharacterSO is NULL."
            );

            return;
        }

        if (!unitCharacter.isPlayerCharacter)
        {
            Debug.Log(
                $"[UpdateManager] Ignoring non-player UnitData: " +
                $"{unit.name} | Character={unitCharacter.characterName}"
            );

            return;
        }

        // Prevent the same UnitData from being registered repeatedly.
        if (currentUnit == unit)
        {
            Debug.Log(
                $"[UpdateManager] This UnitData is already the current " +
                $"unit. Skipping duplicate upgrade application. | " +
                $"Unit={unit.name} | " +
                $"UnitData ID={unit.GetInstanceID()}"
            );

            return;
        }

        currentUnit = unit;

        // Keep CharacterSO synchronized with the current player unit.
        currentCharacter = unitCharacter;

        Debug.Log(
            $"[UpdateManager] Current UnitData registered. | " +
            $"Unit={unit.name} | " +
            $"Character={unitCharacter.characterName} | " +
            $"UnitData ID={unit.GetInstanceID()} | " +
            $"Stored Upgrades={purchasedUpgrades.Count}"
        );

        // Apply upgrades that were purchased before this unit existed.
        ApplyRuntimeUpgradesToUnit(unit);
    }

    public UnitData GetCurrentUnit()
    {
        return currentUnit;
    }

    public void ClearCurrentUnit()
    {
        currentUnit = null;

        Debug.Log(
            "[UpdateManager] Current UnitData cleared."
        );
    }

    // =========================================================
    // APPLY UPGRADE
    // =========================================================

    public void ApplyUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogWarning(
                "[UpdateManager] ApplyUpgrade received NULL."
            );

            return;
        }

        Debug.Log(
            $"[UpdateManager] Upgrade received: " +
            $"{upgrade.name}"
        );

        // Always store the upgrade.
        //
        // This is important because the player's AttackUnit/UnitData
        // may not exist yet when the upgrade is selected.
        purchasedUpgrades.Add(upgrade);

        Debug.Log(
            $"[UpdateManager] Upgrade stored. | " +
            $"Total Stored Upgrades={purchasedUpgrades.Count}"
        );

        // -----------------------------------------------------
        // Chain Bounce
        // -----------------------------------------------------
        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            if (currentUnit != null)
            {
                Debug.Log(
                    "[UpdateManager] Current UnitData exists. " +
                    "Applying ChainBounceUpgrade immediately."
                );

                ApplyUpgradeToUnit(upgrade, currentUnit);
            }
            else
            {
                Debug.Log(
                    "[UpdateManager] Current UnitData is NULL. " +
                    "ChainBounceUpgrade will be applied when the " +
                    "player UnitData is registered."
                );
            }

            return;
        }

        // -----------------------------------------------------
        // Other RustyUpgrades
        // -----------------------------------------------------
        RustyUpgrades rustyUpgrade = upgrade as RustyUpgrades;

        if (rustyUpgrade != null)
        {
            if (currentCharacter != null)
            {
                rustyUpgrade.Apply(currentCharacter);

                Debug.Log(
                    $"[UpdateManager] Applied {upgrade.name} " +
                    $"to CharacterSO {currentCharacter.characterName}."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[UpdateManager] Cannot apply {upgrade.name}. " +
                    "Current CharacterSO is NULL."
                );
            }

            return;
        }

        Debug.LogWarning(
            $"[UpdateManager] Upgrade {upgrade.name} is not a " +
            $"recognized RustyUpgrades type."
        );
    }

    // =========================================================
    // APPLY UPGRADE TO SPECIFIC UNIT
    // =========================================================

    public void ApplyUpgradeToUnit(
        UpgradeSO upgrade,
        UnitData unit
    )
    {
        if (upgrade == null)
        {
            Debug.LogWarning(
                "[UpdateManager] ApplyUpgradeToUnit: upgrade is NULL."
            );

            return;
        }

        if (unit == null)
        {
            Debug.LogWarning(
                "[UpdateManager] ApplyUpgradeToUnit: UnitData is NULL."
            );

            return;
        }

        CharacterSO character = unit.GetCharacter();

        if (character == null)
        {
            Debug.LogWarning(
                $"[UpdateManager] Cannot apply {upgrade.name} to " +
                $"{unit.name}. CharacterSO is NULL."
            );

            return;
        }

        // Never apply player runtime upgrades to enemies.
        if (!character.isPlayerCharacter)
        {
            Debug.Log(
                $"[UpdateManager] Skipping upgrade {upgrade.name} " +
                $"for non-player UnitData: {unit.name}"
            );

            return;
        }

        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            chainBounceUpgrade.ApplyToUnit(unit);

            Debug.Log(
                $"[UpdateManager] ChainBounceUpgrade applied to " +
                $"UnitData. | Unit={unit.name} | " +
                $"UnitData ID={unit.GetInstanceID()}"
            );

            return;
        }

        Debug.Log(
            $"[UpdateManager] Upgrade {upgrade.name} does not have " +
            $"a UnitData-specific implementation."
        );
    }

    // =========================================================
    // APPLY STORED RUNTIME UPGRADES
    // =========================================================

    private void ApplyRuntimeUpgradesToUnit(UnitData unit)
    {
        if (unit == null)
        {
            return;
        }

        CharacterSO character = unit.GetCharacter();

        if (character == null)
        {
            Debug.LogWarning(
                $"[UpdateManager] Cannot apply stored upgrades to " +
                $"{unit.name}. CharacterSO is NULL."
            );

            return;
        }

        // Safety check: only player characters receive the
        // upgrades stored by this manager.
        if (!character.isPlayerCharacter)
        {
            Debug.Log(
                $"[UpdateManager] Skipping stored upgrades for " +
                $"non-player UnitData: {unit.name}"
            );

            return;
        }

        if (purchasedUpgrades.Count == 0)
        {
            Debug.Log(
                $"[UpdateManager] No stored upgrades to apply to " +
                $"{unit.name}."
            );

            return;
        }

        Debug.Log(
            $"[UpdateManager] Applying stored upgrades to new " +
            $"Player UnitData. | " +
            $"Unit={unit.name} | " +
            $"Stored Upgrades={purchasedUpgrades.Count}"
        );

        for (int i = 0; i < purchasedUpgrades.Count; i++)
        {
            UpgradeSO upgrade = purchasedUpgrades[i];

            if (upgrade == null)
            {
                continue;
            }

            ChainBounceUpgrade chainBounceUpgrade =
                upgrade as ChainBounceUpgrade;

            if (chainBounceUpgrade != null)
            {
                ApplyUpgradeToUnit(upgrade, unit);
            }
        }

        Debug.Log(
            $"[UpdateManager] Finished applying stored upgrades " +
            $"to {unit.name}."
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetAllUpgrades()
    {
        purchasedUpgrades.Clear();

        if (currentUnit != null)
        {
            currentUnit.ResetRuntimeUpgrades();
        }

        Debug.Log(
            "[UpdateManager] All purchased upgrades cleared."
        );
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

        int count = currentCharacter.GetUpgradeCount();

        if (count <= 0)
        {
            return null;
        }

        UpgradeSO[] result = new UpgradeSO[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = currentCharacter.GetUpgrade(i);
        }

        return result;
    }
}