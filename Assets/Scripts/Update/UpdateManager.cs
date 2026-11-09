
using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; private set; }

    [Header("Current Player")]
    public UnitData currentUnit;
    public CharacterSO currentCharacter;

    [Header("Turret Upgrade Target")]
    [SerializeField] private UpgradeableCombatUnit turretTarget;

    private readonly List<UpgradeSO> purchasedUpgrades =
        new List<UpgradeSO>();

    private readonly HashSet<UpgradeSO> appliedToTurret =
        new HashSet<UpgradeSO>();

    private readonly Dictionary<UnitData, HashSet<RingUpgrade>>
        appliedPlayerRings =
        new Dictionary<UnitData, HashSet<RingUpgrade>>();

    private readonly Dictionary<UnitData, HashSet<RingUpgrade>>
        appliedChainBounce =
        new Dictionary<UnitData, HashSet<RingUpgrade>>();

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =====================================================
    // REGISTER PLAYER
    // =====================================================

    public void SetCurrentUnit(UnitData unit)
    {
        if (unit == null)
            return;

        CharacterSO character = unit.GetCharacter();

        if (character == null)
            return;

        if (!character.isPlayerCharacter &&
            character.team != Team.Player)
            return;

        currentUnit = unit;
        currentCharacter = character;

        ApplyPlayerRingUpgrades();
    }

    public void RefreshPlayerUpgradesAfterInitialization(UnitData unit)
    {
        if (unit == null)
            return;

        CharacterSO character = unit.GetCharacter();

        if (character == null)
            return;

        if (!character.isPlayerCharacter &&
            character.team != Team.Player)
            return;

        currentUnit = unit;
        currentCharacter = character;

        // HealthManager.Initialize() may reset health.
        // Allow health rings to be reapplied after initialization.
        if (appliedPlayerRings.TryGetValue(
            unit,
            out HashSet<RingUpgrade> applied))
        {
            foreach (UpgradeSO upgrade in purchasedUpgrades)
            {
                if (upgrade is RingUpgrade ring &&
                    ring.GetTarget() == UpgradeTarget.Player &&
                    ring.GetStat() == RingStat.Health)
                {
                    applied.Remove(ring);
                }
            }
        }

        ApplyPlayerRingUpgrades();
    }

    public CharacterSO GetCurrentCharacter()
    {
        return currentCharacter;
    }

    // =====================================================
    // AVAILABLE UPGRADES
    // =====================================================

    public List<UpgradeSO> GetCurrentCharacterUpgrades()
    {
        List<UpgradeSO> result = new List<UpgradeSO>();

        if (currentCharacter == null)
            return result;

        UpgradeSO[] upgrades = currentCharacter.upgrades;

        if (upgrades == null)
            return result;

        foreach (UpgradeSO upgrade in upgrades)
        {
            if (upgrade == null)
                continue;

            if (purchasedUpgrades.Contains(upgrade))
                continue;

            result.Add(upgrade);
        }

        return result;
    }

    public List<UpgradeSO> GetPurchasedUpgrades()
    {
        return new List<UpgradeSO>(purchasedUpgrades);
    }

    // =====================================================
    // PURCHASE UPGRADE
    // =====================================================

    public void ApplyUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null)
            return;

        if (purchasedUpgrades.Contains(upgrade))
        {
            Debug.LogWarning(
                "[UpgradeManager] Upgrade already purchased: " +
                upgrade.UpgradeName
            );
            return;
        }

        purchasedUpgrades.Add(upgrade);

        Debug.Log(
            "[UpgradeManager] Purchased: " +
            upgrade.UpgradeName
        );

        // RING UPGRADE
        if (upgrade is RingUpgrade ring)
        {
            if (ring.GetTarget() == UpgradeTarget.Player)
            {
                ApplyPlayerRingUpgrade(ring);
            }
            else if (ring.GetTarget() == UpgradeTarget.Turret)
            {
                ApplyTurretRingUpgrade(ring);
            }

            return;
        }

        // TURRET CHARGE UPGRADE
        if (upgrade is TurretChargeUpgrade chargeUpgrade)
        {
            ApplyTurretChargeUpgrade(chargeUpgrade);
            return;
        }

        // OTHER UPGRADES
        if (currentUnit != null)
            upgrade.ApplyToUnit(currentUnit);

        if (currentCharacter != null)
            upgrade.Apply(currentCharacter);
    }

    // =====================================================
    // PLAYER RINGS
    // =====================================================

    private void ApplyPlayerRingUpgrades()
    {
        if (currentUnit == null)
            return;

        foreach (UpgradeSO upgrade in purchasedUpgrades)
        {
            if (upgrade is RingUpgrade ring &&
                ring.GetTarget() == UpgradeTarget.Player)
            {
                ApplyPlayerRingUpgrade(ring);
            }
        }
    }

    private void ApplyPlayerRingUpgrade(RingUpgrade ring)
    {
        if (ring == null || currentUnit == null)
            return;

        if (!appliedPlayerRings.ContainsKey(currentUnit))
        {
            appliedPlayerRings[currentUnit] =
                new HashSet<RingUpgrade>();
        }

        if (appliedPlayerRings[currentUnit].Contains(ring))
            return;

        int amount = ring.GetBonusAmount();

        switch (ring.GetStat())
        {
            case RingStat.Health:
                {
                    HealthManager healthManager =
                        currentUnit.GetComponent<HealthManager>();

                    if (healthManager == null)
                    {
                        healthManager =
                            currentUnit.GetComponentInChildren<HealthManager>(
                                true
                            );
                    }

                    if (healthManager == null)
                    {
                        Debug.LogWarning(
                            "[UpgradeManager] Player Health ring could not " +
                            "be applied: no HealthManager found."
                        );
                        return;
                    }

                    healthManager.AddMaxHealth(amount);
                    appliedPlayerRings[currentUnit].Add(ring);

                    Debug.Log(
                        "[UpgradeManager] Player Health ring applied. " +
                        "Bonus: +" + amount
                    );

                    break;
                }

            case RingStat.ChainBounce:
                {
                    CharacterSO character = currentUnit.GetCharacter();

                    if (character == null)
                        return;

                    ChainLightning chainLightning =
                        character.GetAbility<ChainLightning>();

                    if (chainLightning == null)
                    {
                        Debug.LogWarning(
                            "[UpgradeManager] Chain Bounce ring could not " +
                            "be applied: ChainLightning ability not found."
                        );
                        return;
                    }

                    if (!appliedChainBounce.ContainsKey(currentUnit))
                    {
                        appliedChainBounce[currentUnit] =
                            new HashSet<RingUpgrade>();
                    }

                    if (appliedChainBounce[currentUnit].Contains(ring))
                        return;

                    currentUnit.AddBonusJumps(
                        chainLightning,
                        amount
                    );

                    appliedChainBounce[currentUnit].Add(ring);
                    appliedPlayerRings[currentUnit].Add(ring);

                    Debug.Log(
                        "[UpgradeManager] Player Chain Bounce applied. " +
                        "Additional jumps: +" + amount
                    );

                    break;
                }

            case RingStat.Damage:
                Debug.LogWarning(
                    "[UpgradeManager] Player Damage ring is not " +
                    "implemented yet."
                );
                break;

            case RingStat.Range:
                Debug.LogWarning(
                    "[UpgradeManager] Player Range ring is not " +
                    "implemented yet."
                );
                break;
        }
    }

    // =====================================================
    // TURRET REGISTRATION
    // =====================================================

    public void RegisterRingUpgradeTarget(
        UpgradeableCombatUnit target)
    {
        if (target == null)
            return;

        // Only clear the applied list when registering
        // a different turret. This prevents duplicate bonuses
        // if the same turret registers more than once.
        if (turretTarget != target)
        {
            turretTarget = target;
            appliedToTurret.Clear();
        }

        Debug.Log(
            "[UpgradeManager] Turret registered for upgrades: " +
            target.gameObject.name
        );

        // Apply previously purchased turret upgrades.
        ApplyStoredTurretUpgrades();
    }

    // =====================================================
    // APPLY STORED TURRET UPGRADES
    // =====================================================

    private void ApplyStoredTurretUpgrades()
    {
        if (turretTarget == null)
            return;

        foreach (UpgradeSO upgrade in purchasedUpgrades)
        {
            if (upgrade is RingUpgrade ring &&
                ring.GetTarget() == UpgradeTarget.Turret)
            {
                ApplyTurretRingUpgrade(ring);
            }
            else if (upgrade is TurretChargeUpgrade chargeUpgrade)
            {
                ApplyTurretChargeUpgrade(chargeUpgrade);
            }
        }
    }

    // =====================================================
    // TURRET RING UPGRADES
    // =====================================================

    private void ApplyTurretRingUpgrade(RingUpgrade ring)
    {
        if (ring == null ||
            ring.GetTarget() != UpgradeTarget.Turret)
            return;

        if (ring.GetStat() == RingStat.ChainBounce)
            return;

        if (turretTarget == null)
            return;

        if (appliedToTurret.Contains(ring))
            return;

        if (!turretTarget.CanApplyUpgrade(ring))
        {
            Debug.LogWarning(
                "[UpgradeManager] Turret rejected ring upgrade: " +
                ring.UpgradeName
            );
            return;
        }

        turretTarget.ApplyUpgrade(ring);
        appliedToTurret.Add(ring);

        Debug.Log(
            "[UpgradeManager] Turret ring applied: " +
            ring.UpgradeName +
            " | Stat: " + ring.GetStat() +
            " | Bonus: +" + ring.GetBonusAmount()
        );
    }

    // =====================================================
    // TURRET CHAIN LIGHTNING UPGRADE
    // =====================================================

    private void ApplyTurretChargeUpgrade(
        TurretChargeUpgrade chargeUpgrade)
    {
        if (chargeUpgrade == null)
            return;

        if (turretTarget == null)
        {
            Debug.Log(
                "[UpgradeManager] Turret Charge purchased. " +
                "Waiting for a turret to register."
            );
            return;
        }

        if (appliedToTurret.Contains(chargeUpgrade))
            return;

        turretbehav turret = FindTurretBehaviour(turretTarget);

        if (turret == null)
        {
            Debug.LogWarning(
                "[UpgradeManager] Could not apply Turret Charge: " +
                "no turretbehav component found on " +
                turretTarget.gameObject.name +
                " or its parents/children."
            );
            return;
        }

        chargeUpgrade.ApplyToTurret(turret);
        appliedToTurret.Add(chargeUpgrade);

        Debug.Log(
            "[UpgradeManager] Turret Charge applied successfully to: " +
            turret.gameObject.name +
            ". Chain Lightning unlocked."
        );
    }

    private turretbehav FindTurretBehaviour(
        UpgradeableCombatUnit target)
    {
        if (target == null)
            return null;

        turretbehav turret =
            target.GetComponent<turretbehav>();

        if (turret == null)
        {
            turret =
                target.GetComponentInChildren<turretbehav>(true);
        }

        if (turret == null)
        {
            turret =
                target.GetComponentInParent<turretbehav>();
        }

        return turret;
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetRuntimeUpgrades()
    {
        purchasedUpgrades.Clear();

        appliedToTurret.Clear();
        appliedPlayerRings.Clear();
        appliedChainBounce.Clear();

        currentUnit = null;
        currentCharacter = null;
        turretTarget = null;

        Debug.Log("[UpgradeManager] Runtime upgrades cleared.");
    }

    public void ResetAllUpgrades()
    {
        ResetRuntimeUpgrades();
    }

    public void BeginNewRun()
    {
        Debug.Log("[UpgradeManager] Starting a new run.");

        ResetRuntimeUpgrades();
    }
}
