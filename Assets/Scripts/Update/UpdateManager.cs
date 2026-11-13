
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

    // Tracks upgrades already applied to the player UnitData.
    private readonly Dictionary<UnitData, HashSet<UpgradeSO>>
        appliedToUnits =
        new Dictionary<UnitData, HashSet<UpgradeSO>>();

    // Tracks upgrades already applied to CharacterSO.
    private readonly HashSet<UpgradeSO> appliedToCharacters =
        new HashSet<UpgradeSO>();

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        Debug.Log(
            "[UpgradeManager] Player registered: " +
            unit.gameObject.name
        );

        // Apply upgrades purchased before the player spawned.
        ApplyStoredUnitUpgrades();

        // Apply player ring upgrades.
        ApplyPlayerRingUpgrades();
    }

    // =====================================================
    // REFRESH PLAYER AFTER INITIALIZATION
    // =====================================================

    public void RefreshPlayerUpgradesAfterInitialization(
        UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogError(
                "[UpgradeManager] Cannot refresh upgrades: UnitData is NULL."
            );
            return;
        }

        CharacterSO character = unit.GetCharacter();

        if (character == null)
        {
            Debug.LogError(
                "[UpgradeManager] Cannot refresh upgrades: " +
                "CharacterSO is NULL."
            );
            return;
        }

        if (!character.isPlayerCharacter &&
            character.team != Team.Player)
            return;

        currentUnit = unit;
        currentCharacter = character;

        Debug.Log(
            "[UpgradeManager] Refreshing upgrades for player: " +
            unit.gameObject.name
        );

        // Apply upgrades that were purchased before spawning.
        ApplyStoredUnitUpgrades();

        // Apply CharacterSO upgrades that were purchased
        // before currentCharacter was registered.
        ApplyStoredCharacterUpgrades();

        // HealthManager.Initialize() may reset health.
        // Remove previously applied health rings so they
        // can be applied again after initialization.
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

        Debug.Log(
            "[UpgradeManager] Player upgrade refresh complete: " +
            unit.gameObject.name
        );
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
        List<UpgradeSO> result =
            new List<UpgradeSO>();

        if (currentCharacter == null)
            return result;

        UpgradeSO[] upgrades =
            currentCharacter.upgrades;

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

        Debug.Log(
            "[UpgradeManager] Upgrade runtime type: " +
            upgrade.GetType().Name
        );

        Debug.Log(
            "[UpgradeManager] Current UnitData: " +
            (currentUnit != null
                ? currentUnit.name
                : "NULL")
        );

        Debug.Log(
            "[UpgradeManager] Current Character: " +
            (currentCharacter != null
                ? currentCharacter.name
                : "NULL")
        );

        // =============================================
        // RING UPGRADE
        // =============================================

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

        // =============================================
        // TURRET CHARGE UPGRADE
        // =============================================

        if (upgrade is TurretChargeUpgrade chargeUpgrade)
        {
            ApplyTurretChargeUpgrade(chargeUpgrade);
            return;
        }

        // =============================================
        // OTHER UNIT UPGRADES
        // =============================================

        if (currentUnit != null)
        {
            ApplyUnitUpgrade(upgrade, currentUnit);
        }
        else
        {
            Debug.Log(
                "[UpgradeManager] UnitData not registered yet. " +
                upgrade.UpgradeName +
                " will be applied when the player spawns."
            );
        }

        // =============================================
        // CHARACTER DATA UPGRADE
        // =============================================

        if (currentCharacter != null)
        {
            ApplyCharacterUpgrade(upgrade, currentCharacter);
        }
        else
        {
            Debug.Log(
                "[UpgradeManager] Character not registered yet. " +
                upgrade.UpgradeName +
                " will be applied when the player spawns."
            );
        }
    }

    // =====================================================
    // APPLY UPGRADE TO UNITDATA
    // =====================================================

    private void ApplyUnitUpgrade(
        UpgradeSO upgrade,
        UnitData unit)
    {
        if (upgrade == null || unit == null)
            return;

        if (!appliedToUnits.ContainsKey(unit))
        {
            appliedToUnits[unit] =
                new HashSet<UpgradeSO>();
        }

        if (appliedToUnits[unit].Contains(upgrade))
        {
            Debug.Log(
                "[UpgradeManager] Upgrade already applied to unit: " +
                upgrade.UpgradeName
            );

            return;
        }

        Debug.Log(
            "[UpgradeManager] Applying upgrade to UnitData: " +
            unit.gameObject.name
        );

        upgrade.ApplyToUnit(unit);

        appliedToUnits[unit].Add(upgrade);

        Debug.Log(
            "[UpgradeManager] Applied " +
            upgrade.UpgradeName +
            " to " +
            unit.gameObject.name
        );
    }

    // =====================================================
    // APPLY STORED UNIT UPGRADES
    // =====================================================

    private void ApplyStoredUnitUpgrades()
    {
        if (currentUnit == null)
            return;

        Debug.Log(
            "[UpgradeManager] Checking stored upgrades for: " +
            currentUnit.gameObject.name
        );

        foreach (UpgradeSO upgrade in purchasedUpgrades)
        {
            if (upgrade == null)
                continue;

            // Ring upgrades use their own application system.
            if (upgrade is RingUpgrade)
                continue;

            // Turret upgrades use their own application system.
            if (upgrade is TurretChargeUpgrade)
                continue;

            ApplyUnitUpgrade(upgrade, currentUnit);
        }
    }

    // =====================================================
    // APPLY UPGRADE TO CHARACTER
    // =====================================================

    private void ApplyCharacterUpgrade(
        UpgradeSO upgrade,
        CharacterSO character)
    {
        if (upgrade == null || character == null)
            return;

        if (appliedToCharacters.Contains(upgrade))
            return;

        upgrade.Apply(character);

        appliedToCharacters.Add(upgrade);

        Debug.Log(
            "[UpgradeManager] Applied " +
            upgrade.UpgradeName +
            " to CharacterSO: " +
            character.name
        );
    }

    // =====================================================
    // APPLY STORED CHARACTER UPGRADES
    // =====================================================

    private void ApplyStoredCharacterUpgrades()
    {
        if (currentCharacter == null)
            return;

        foreach (UpgradeSO upgrade in purchasedUpgrades)
        {
            if (upgrade == null)
                continue;

            if (upgrade is RingUpgrade)
                continue;

            if (upgrade is TurretChargeUpgrade)
                continue;

            ApplyCharacterUpgrade(
                upgrade,
                currentCharacter
            );
        }
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
            // =============================================
            // HEALTH
            // =============================================

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

            // =============================================
            // CHAIN BOUNCE
            // =============================================

            case RingStat.ChainBounce:
                {
                    CharacterSO character =
                        currentUnit.GetCharacter();

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

            // =============================================
            // EXPLOSION SIZE
            // =============================================

            case RingStat.ExplosionSize:
                {
                    appliedPlayerRings[currentUnit].Add(ring);

                    Debug.Log(
                        "[UpgradeManager] Player Explosion Size ring applied. " +
                        "Bonus: +" + amount
                    );

                    break;
                }

            // =============================================
            // ABILITY DAMAGE RINGS
            // =============================================

            case RingStat.Ability1Damage:
            case RingStat.Ability2Damage:
            case RingStat.Ability3Damage:
            case RingStat.Ability4Damage:
                {
                    appliedPlayerRings[currentUnit].Add(ring);

                    Debug.Log(
                        "[UpgradeManager] Player Ability Damage ring applied. " +
                        "Stat: " + ring.GetStat() +
                        " | Bonus: +" + amount
                    );

                    break;
                }

            // =============================================
            // GENERAL DAMAGE
            // =============================================

            case RingStat.Damage:
                {
                    Debug.LogWarning(
                        "[UpgradeManager] Player Damage ring is not " +
                        "implemented yet."
                    );

                    break;
                }

            // =============================================
            // RANGE
            // =============================================

            case RingStat.Range:
                {
                    Debug.LogWarning(
                        "[UpgradeManager] Player Range ring is not " +
                        "implemented yet."
                    );

                    break;
                }
        }
    }

    // =====================================================
    // GET PLAYER RING BONUS
    // =====================================================

    public int GetPlayerRingBonus(
        GameObject user,
        RingStat stat)
    {
        if (user == null || currentUnit == null)
            return 0;

        UnitData userUnit =
            user.GetComponent<UnitData>();

        if (userUnit == null)
        {
            userUnit =
                user.GetComponentInParent<UnitData>();
        }

        if (userUnit == null)
        {
            userUnit =
                user.GetComponentInChildren<UnitData>(true);
        }

        if (userUnit != currentUnit)
            return 0;

        int totalBonus = 0;

        foreach (UpgradeSO upgrade in purchasedUpgrades)
        {
            if (!(upgrade is RingUpgrade ring))
                continue;

            if (ring.GetTarget() != UpgradeTarget.Player)
                continue;

            if (ring.GetStat() != stat)
                continue;

            totalBonus += ring.GetBonusAmount();
        }

        return totalBonus;
    }

    // =====================================================
    // TURRET REGISTRATION
    // =====================================================

    public void RegisterRingUpgradeTarget(
        UpgradeableCombatUnit target)
    {
        if (target == null)
            return;

        if (turretTarget != target)
        {
            turretTarget = target;
            appliedToTurret.Clear();
        }

        Debug.Log(
            "[UpgradeManager] Turret registered for upgrades: " +
            target.gameObject.name
        );

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

        turretbehav turret =
            FindTurretBehaviour(turretTarget);

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

        appliedToUnits.Clear();
        appliedToCharacters.Clear();

        appliedToTurret.Clear();

        appliedPlayerRings.Clear();
        appliedChainBounce.Clear();

        currentUnit = null;
        currentCharacter = null;
        turretTarget = null;

        Debug.Log(
            "[UpgradeManager] Runtime upgrades cleared."
        );
    }

    public void ResetAllUpgrades()
    {
        ResetRuntimeUpgrades();
    }

    public void BeginNewRun()
    {
        Debug.Log(
            "[UpgradeManager] Starting a new run."
        );

        ResetRuntimeUpgrades();
    }
}
