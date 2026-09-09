using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Current Character")]

    [SerializeField]
    private CharacterSO currentCharacter;


    // ============================================================
    // GET CURRENT CHARACTER
    // ============================================================

    public CharacterSO GetCurrentCharacter()
    {
        return currentCharacter;
    }


    // ============================================================
    // SET CURRENT CHARACTER
    // ============================================================

    public void SetCurrentCharacter(
        CharacterSO character
    )
    {
        if (character == null)
        {
            Debug.LogError(
                "[UpdateManager] " +
                "Cannot set current character to null."
            );

            return;
        }

        currentCharacter = character;

        Debug.Log(
            "[UpdateManager] Current character is now: " +
            currentCharacter.characterName
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
        UpgradeSO upgrade
    )
    {
        if (upgrade == null)
        {
            Debug.LogError(
                "[UpdateManager] Upgrade is null."
            );

            return;
        }

        if (currentCharacter == null)
        {
            Debug.LogError(
                "[UpdateManager] Current character is null."
            );

            return;
        }

        if (upgrade is RustyUpgrades rustyUpgrade)
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
        }
        else
        {
            Debug.LogWarning(
                "[UpdateManager] Upgrade " +
                upgrade.name +
                " is not a Rusty upgrade."
            );
        }
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private void OnUpgradeRefresh()
    {
        Debug.Log(
            "[UpdateManager] Upgrade refresh completed."
        );
    }
}