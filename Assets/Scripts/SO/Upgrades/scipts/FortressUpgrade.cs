using UnityEngine;

[CreateAssetMenu(
    fileName = "SentryUpgrade",
    menuName = "Upgrades/Sentry Upgrade"
)]
public class FortressUpgrade : UpgradeSO
{
    [Header("Health")]
    [SerializeField]
    private int bonusHealth = 20;


    [Header("Damage")]
    [SerializeField]
    private int bonusDamage = 5;


    [Header("Range")]
    [SerializeField]
    private int bonusRange = 1;


    // ============================================================
    // ENABLE DEBUG
    // ============================================================

    private void OnEnable()
    {
        Debug.Log(
            $"[FortressUpgrade] Loaded '{name}' | " +
            $"Health +{bonusHealth} | " +
            $"Damage +{bonusDamage} | " +
            $"Range +{bonusRange}"
        );
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public int GetBonusHealth()
    {
        Debug.Log(
            $"[FortressUpgrade] GetBonusHealth('{name}') = {bonusHealth}"
        );

        return bonusHealth;
    }


    public int GetBonusDamage()
    {
        Debug.Log(
            $"[FortressUpgrade] GetBonusDamage('{name}') = {bonusDamage}"
        );

        return bonusDamage;
    }


    public int GetBonusRange()
    {
        Debug.Log(
            $"[FortressUpgrade] GetBonusRange('{name}') = {bonusRange}"
        );

        return bonusRange;
    }
}
