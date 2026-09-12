using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    [Header("Current Unit")]
    [SerializeField]
    private UnitData currentUnit;

    [Header("Current Character")]
    [SerializeField]
    private CharacterSO currentCharacter;


    // ============================================================
    // GETTERS
    // ============================================================

    public CharacterSO GetCurrentCharacter()
    {
        return currentCharacter;
    }


    public UnitData GetCurrentUnit()
    {
        return currentUnit;
    }


    // ============================================================
    // SET CURRENT CHARACTER
    // ============================================================

    public void SetCurrentCharacter(
        CharacterSO character)
    {
        if (character == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Cannot set current character to null."
            );

            return;
        }


        currentCharacter =
            character;


        Debug.Log(
            "[UpdateManager] " +
            "Current character is now: " +
            currentCharacter.characterName
        );
    }


    // ============================================================
    // SET CURRENT UNIT
    // ============================================================

    public void SetCurrentUnit(
        UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Cannot set current unit to null."
            );

            return;
        }


        currentUnit =
            unit;


        currentCharacter =
            unit.GetCharacter();


        if (currentCharacter == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Current unit has no CharacterSO.",
                unit
            );

            return;
        }


        Debug.Log(
            "[UpdateManager] " +
            "Current unit is now: " +
            unit.name +
            " (" +
            currentCharacter.characterName +
            ")"
        );
    }


    // ============================================================
    // GET CHARACTER UPGRADES
    // ============================================================

    public UpgradeSO[] GetCurrentCharacterUpgrades()
    {
        if (currentCharacter == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Current character is null."
            );

            return null;
        }


        if (currentCharacter.upgrades == null)
        {
            Debug.LogWarning(
                "[UpdateManager] " +
                currentCharacter.characterName +
                " has no upgrades."
            );

            return null;
        }


        return currentCharacter.upgrades;
    }


    // ============================================================
    // APPLY UPGRADE
    // ============================================================

    public void ApplyUpgrade(
        UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Upgrade is null."
            );

            return;
        }


        if (currentCharacter == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Current character is null."
            );

            return;
        }


        // --------------------------------------------------------
        // CHAIN BOUNCE
        // --------------------------------------------------------

        if (
            upgrade is
            ChainBounceUpgrade chainBounceUpgrade
        )
        {
            if (currentUnit == null)
            {
                Debug.LogError(
                    "[UpdateManager] " +
                    "Current UnitData is null. " +
                    "Cannot apply per-unit upgrade."
                );

                return;
            }


            chainBounceUpgrade.ApplyToUnit(
                currentUnit
            );


            Debug.Log(
                "[UpdateManager] Applied " +
                upgrade.name +
                " to unit " +
                currentUnit.name
            );


            OnUpgradeRefresh();

            return;
        }


        // --------------------------------------------------------
        // OTHER RUSTY UPGRADES
        // --------------------------------------------------------

        if (
            upgrade is
            RustyUpgrades rustyUpgrade
        )
        {
            rustyUpgrade.Apply(
                currentCharacter
            );


            Debug.Log(
                "[UpdateManager] Applied " +
                upgrade.name +
                " to " +
                currentCharacter.characterName
            );


            OnUpgradeRefresh();

            return;
        }


        // --------------------------------------------------------
        // UNKNOWN UPGRADE
        // --------------------------------------------------------

        Debug.LogWarning(
            "[UpdateManager] Upgrade " +
            upgrade.name +
            " is not a Rusty upgrade."
        );
    }


    // ============================================================
    // RESET ALL UPGRADES
    // ============================================================

    public void ResetAllUpgrades()
    {
        if (currentUnit == null)
        {
            Debug.LogWarning(
                "[UpdateManager] " +
                "Current unit is null. " +
                "Nothing to reset."
            );

            return;
        }


        currentUnit.ResetRuntimeUpgrades();


        Debug.Log(
            "[UpdateManager] " +
            "All runtime upgrades reset for " +
            currentUnit.name
        );
    }


    // ============================================================
    // RESET CURRENT UNIT UPGRADES
    // ============================================================

    public void ResetCurrentUnitUpgrades()
    {
        ResetAllUpgrades();
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private void OnUpgradeRefresh()
    {
        Debug.Log(
            "[UpdateManager] " +
            "Upgrade refresh completed."
        );
    }
}