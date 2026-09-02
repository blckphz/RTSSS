using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Character")]

    [SerializeField]
    private CharacterSO CurrentCChar;


    // ============================================================
    // APPLY UPGRADE
    // ============================================================

    public void ApplyUpgrade(
        UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogError(
                "[UpdateManager] Upgrade is null."
            );

            return;
        }

        if (CurrentCChar == null)
        {
            Debug.LogError(
                "[UpdateManager] Rusty is null."
            );

            return;
        }

        if (
            upgrade is RustyUpgrades rustyUpgrade
        )
        {
            rustyUpgrade.Apply(
                CurrentCChar
            );

            Debug.Log(
                "[UpdateManager] Applied " +
                upgrade.name +
                " to " +
                CurrentCChar.characterName
            );

            OnUpgradeRefresh();
        }
        else
        {
            Debug.LogWarning(
                "[UpdateManager] Upgrade is not " +
                "a Rusty upgrade."
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