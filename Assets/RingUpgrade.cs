
using UnityEngine;

public enum UpgradeTarget
{
    Player,
    Turret
}

public enum RingStat
{
    Health,
    Damage,
    Range,
    ChainBounce
}

[CreateAssetMenu(
    fileName = "NewRingUpgrade",
    menuName = "Upgrades/Ring Upgrade"
)]
public class RingUpgrade : UpgradeSO
{
    [Header("Upgrade Target")]
    [SerializeField] private UpgradeTarget target;

    [Header("Ring Bonus")]
    [SerializeField] private RingStat stat;

    [SerializeField, Min(1)]
    private int bonusAmount = 1;

    public UpgradeTarget GetTarget()
    {
        return target;
    }

    public RingStat GetStat()
    {
        return stat;
    }

    public int GetBonusAmount()
    {
        return bonusAmount;
    }
}
