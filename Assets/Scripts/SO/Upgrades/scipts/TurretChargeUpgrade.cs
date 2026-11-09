
using UnityEngine;

[CreateAssetMenu(
    fileName = "TurretChargeUpgrade",
    menuName = "Upgrades/Turret Charge Upgrade"
)]
public class TurretChargeUpgrade : UpgradeSO
{
    public void ApplyToTurret(turretbehav turret)
    {
        if (turret == null)
        {
            Debug.LogWarning(
                "[TurretChargeUpgrade] Cannot unlock Chain Lightning: " +
                "turret reference is missing."
            );
            return;
        }

        turret.UnlockChainLightning();
    }
}
