using UnityEngine;

public interface IUpgradeableCombatUnit
{
    void ApplyUpgrade(
        UpgradeSO upgrade
    );

    bool CanApplyUpgrade(
        UpgradeSO upgrade
    );
}
