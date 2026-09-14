using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; private set; }

    [Header("Current Player")]
    [SerializeField] private UnitData currentUnit;
    [SerializeField] private CharacterSO currentCharacter;

    [Header("Purchased Upgrades")]
    private readonly List<UpgradeSO> purchasedUpgrades =
        new List<UpgradeSO>();

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

    public void SetCurrentUnit(UnitData unit)
    {
        if (unit == null)
        {
            return;
        }

        CharacterSO unitCharacter = unit.GetCharacter();

        if (unitCharacter == null)
        {
            return;
        }

        // Only player characters can become the current unit.
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

        // Apply upgrades that were purchased before this unit spawned.
        ApplyRuntimeUpgradesToUnit(unit);
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

    public void ApplyUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            return;
        }

        // Always remember the upgrade.
        //
        // This allows upgrades to be selected before the player
        // AttackUnit/UnitData has spawned.
        purchasedUpgrades.Add(upgrade);

        // -----------------------------------------------------
        // Chain Bounce
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
        // Other RustyUpgrades
        // -----------------------------------------------------

        RustyUpgrades rustyUpgrade =
            upgrade as RustyUpgrades;

        if (rustyUpgrade != null)
        {
            if (currentCharacter != null)
            {
                rustyUpgrade.Apply(currentCharacter);
            }

            return;
        }
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
            return;
        }

        if (unit == null)
        {
            return;
        }

        CharacterSO character = unit.GetCharacter();

        if (character == null)
        {
            return;
        }

        // Never apply player upgrades to enemies.
        if (!character.isPlayerCharacter)
        {
            return;
        }

        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            chainBounceUpgrade.ApplyToUnit(unit);
            return;
        }
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
                ApplyUpgradeToUnit(
                    upgrade,
                    unit
                );
            }
        }
    }

    // =========================================================
    // NEW GAME
    // =========================================================

    public void StartNewGame()
    {
        // Clear all upgrades stored by UpdateManager.
        purchasedUpgrades.Clear();

        // Clear runtime upgrade bonuses from the current player.
        if (currentUnit != null)
        {
            currentUnit.ResetRuntimeUpgrades();
        }

        // The old player should no longer be considered
        // the current unit.
        currentUnit = null;

        // Clear the current character reference too.
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