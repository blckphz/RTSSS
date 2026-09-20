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

        if (!unitCharacter.isPlayerCharacter)
        {
            return;
        }

        if (currentUnit == unit)
        {
            return;
        }

        currentUnit =
            unit;

        currentCharacter =
            unitCharacter;

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

        if (fortressTarget == target)
        {
            ApplyStoredFortressUpgrades(
                target
            );

            return;
        }

        fortressTarget =
            target;

        lastFortressTarget =
            target;

        lastAppliedFortressUpgradeCount =
            0;

        ApplyStoredFortressUpgrades(
            target
        );
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

        FortressUpgrade fortressUpgrade =
            upgrade as FortressUpgrade;

        if (fortressUpgrade != null)
        {
            purchasedUpgrades.Add(
                upgrade
            );

            if (fortressTarget != null)
            {
                ApplyFortressUpgrade(
                    fortressUpgrade
                );
            }

            return;
        }

        purchasedUpgrades.Add(
            upgrade
        );

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
            return;
        }

        fortressTarget.ApplyUpgrade(
            upgrade
        );

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
            return;
        }

        if (
            lastFortressTarget == target &&
            lastAppliedFortressUpgradeCount >=
            purchasedUpgrades.Count
        )
        {
            return;
        }

        if (lastFortressTarget != target)
        {
            lastAppliedFortressUpgradeCount =
                0;

            lastFortressTarget =
                target;
        }

        for (
            int i =
                lastAppliedFortressUpgradeCount;

            i <
            purchasedUpgrades.Count;

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

            if (fortressUpgrade == null)
            {
                continue;
            }

            target.ApplyUpgrade(
                fortressUpgrade
            );
        }

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

        if (!character.isPlayerCharacter)
        {
            return;
        }

        ChainBounceUpgrade chainBounceUpgrade =
            upgrade as ChainBounceUpgrade;

        if (chainBounceUpgrade != null)
        {
            chainBounceUpgrade.ApplyToUnit(
                unit
            );

            return;
        }

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

            if (upgrade is FortressUpgrade)
            {
                continue;
            }

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

        currentUnit =
            null;

        currentCharacter =
            null;

        fortressTarget =
            null;

        lastFortressTarget =
            null;

        lastAppliedFortressUpgradeCount =
            0;
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

        fortressTarget =
            null;

        lastFortressTarget =
            null;

        lastAppliedFortressUpgradeCount =
            0;
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