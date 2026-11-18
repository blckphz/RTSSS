
using UnityEngine;

[CreateAssetMenu(
    fileName = "EngineerMeleeTurretHeal",
    menuName = "Upgrades/Engineer Melee Turret Heal"
)]
public class EngineerMeleeTurretHealUpgrade : UpgradeSO
{
    [Header("Turret Heal Bonus")]
    [SerializeField]
    private int bonusHealAmount = 5;

    public int GetBonusHealAmount()
    {
        return Mathf.Max(0, bonusHealAmount);
    }

    public override void ApplyToUnit(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning(
                "[EngineerMeleeTurretHealUpgrade] UnitData is missing."
            );
            return;
        }

        unit.SetMeleeTurretHealAmount(GetBonusHealAmount());

        Debug.Log(
            "[EngineerMeleeTurretHealUpgrade] Applied to " +
            unit.gameObject.name +
            ". Healing bonus: " +
            unit.GetMeleeTurretHealAmount()
        );
    }
}
