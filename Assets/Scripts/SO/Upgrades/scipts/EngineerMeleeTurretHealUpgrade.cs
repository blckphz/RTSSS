using UnityEngine;

[CreateAssetMenu(
    fileName = "EngineerMeleeTurretHeal",
    menuName = "Upgrades/Engineer Melee Turret Heal"
)]
public class EngineerMeleeTurretHealUpgrade : UpgradeSO
{
    [Header("Turret Heal Bonus")]
    [Tooltip(
        "Extra healing added on top of the FrontAttack's base damage."
    )]
    [SerializeField]
    private int bonusHealAmount = 5;

    public int GetBonusHealAmount()
    {
        return Mathf.Max(
            0,
            bonusHealAmount
        );
    }

    public void ApplyToUnit(
        UnitData unit)
    {
        if (unit == null)
        {
            return;
        }

        unit.SetMeleeTurretHealAmount(
            GetBonusHealAmount()
        );
    }
}
