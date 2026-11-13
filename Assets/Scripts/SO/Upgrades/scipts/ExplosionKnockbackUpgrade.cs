using UnityEngine;

[CreateAssetMenu(
    fileName = "ExplosionKnockbackUpgrade",
    menuName = "Upgrades/Explosion Knockback"
)]
public class ExplosionKnockbackUpgrade : UpgradeSO
{
    public override void ApplyToUnit(UnitData unitData)
    {
        base.ApplyToUnit(unitData);

        if (unitData == null)
            return;

        unitData.UnlockExplosionKnockback();

        Debug.Log(
            "[ExplosionKnockbackUpgrade] Explosion knockback unlocked!"
        );
    }
}