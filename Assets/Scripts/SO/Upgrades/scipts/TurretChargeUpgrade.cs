using UnityEngine;

[CreateAssetMenu(
    fileName = "TurretChargeUpgrade",
    menuName = "Upgrades/Turret Charge Upgrade"
)]
public class TurretChargeUpgrade : UpgradeSO
{
    public void ApplyToTurret(
        turretbehav turret
    )
    {
        if (turret == null)
        {
            return;
        }

        turret.UnlockChainLightning();
    }
}
