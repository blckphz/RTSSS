
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    public static event Action<AttackUnit, AbilitySO> OnAbilityUsed;

    [Header("Character Data")]
    [SerializeField] private CharacterSO characterData;

    [Header("Unit Data")]
    [SerializeField] private UnitData unitData;

    [Header("Abilities")]
    [SerializeField] private List<AbilityData> abilities = new();

    [Header("References")]
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private UpgradeableCombatUnit upgradeableCombatUnit;
    [SerializeField] private DayNightManager dayNightManager;

    [Header("Attack Animation")]
    [SerializeField] private bool useDirectionalEnemyAttackAnimation;
    [SerializeField] private string attackAnimatorLayerName = "attackLayer";

    [Header("AI Targeting")]
    [SerializeField] private bool playerLocked = false;

    [Header("Day / Night Bonuses")]
    [SerializeField] private DayNightType dayNightType = DayNightType.NeutralType;

    [SerializeField] private int dayDamageBonus = 0;
    [SerializeField] private int nightDamageBonus = 0;

    [SerializeField] private int dayMoveRangeBonus = 0;
    [SerializeField] private int nightMoveRangeBonus = 0;

    private UpdateManager updateManager;
    private UnitMoveBrain moveBrain;
    private IAttackAnimation attackAnimation;
    private GridManager cachedGridManager;
    private UnitTilePin unitTilePin;

    private int attackAnimatorLayerIndex = -1;

    [SerializeField] private Vector2Int logicalGridPosition;
    private bool hasLogicalGridPosition;

    private AbilitySO animationEventAbility;
    private GameObject animationEventTarget;
    private Vector2Int animationEventTargetTile;

    private bool animationEventFired;
    private bool attackInProgress;

    private bool waitingForEggLanding;

    public enum DayNightType
    {
        DayType,
        NightType,
        NeutralType
    }

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();

        moveBrain = GetComponent<UnitMoveBrain>();
        attackAnimation = GetComponent<IAttackAnimation>();
        unitTilePin = GetComponent<UnitTilePin>();

        if (animationController == null)
            animationController = GetComponent<AnimationController>();

        FindUpgradeableCombatUnit();
        FindUnitData();

        updateManager = FindFirstObjectByType<UpdateManager>();
        dayNightManager = FindFirstObjectByType<DayNightManager>();

        EnsureGridManager();
        FindAttackAnimatorLayer();

        if (cachedGridManager != null)
        {
            Vector2Int initialTile;

            if (unitTilePin != null && unitTilePin.HasTile())
            {
                initialTile = unitTilePin.GetTile();
            }
            else
            {
                initialTile =
                    cachedGridManager.WorldToGridPosition(transform.position);
            }

            logicalGridPosition = initialTile;
            hasLogicalGridPosition = true;
        }

        if (characterData != null)
            Initialize(characterData);
    }

    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindUpgradeableCombatUnit()
    {
        if (upgradeableCombatUnit != null)
            return;

        upgradeableCombatUnit = GetComponent<UpgradeableCombatUnit>();

        if (upgradeableCombatUnit == null)
            upgradeableCombatUnit = GetComponentInParent<UpgradeableCombatUnit>();

        if (upgradeableCombatUnit == null)
            upgradeableCombatUnit = GetComponentInChildren<UpgradeableCombatUnit>();
    }

    private UpgradeableCombatUnit GetUpgradeableCombatUnit()
    {
        if (upgradeableCombatUnit != null)
            return upgradeableCombatUnit;

        upgradeableCombatUnit = GetComponent<UpgradeableCombatUnit>();

        if (upgradeableCombatUnit == null)
            upgradeableCombatUnit = GetComponentInParent<UpgradeableCombatUnit>();

        if (upgradeableCombatUnit == null)
            upgradeableCombatUnit = GetComponentInChildren<UpgradeableCombatUnit>();

        return upgradeableCombatUnit;
    }

    private void FindAttackAnimatorLayer()
    {
        attackAnimatorLayerIndex = -1;

        if (animationController == null)
            return;

        Animator animator = animationController.GetAnimator();

        if (animator == null)
            return;

        attackAnimatorLayerIndex =
            animator.GetLayerIndex(attackAnimatorLayerName);
    }

    private void FindUnitData()
    {
        if (unitData == null)
            unitData = GetComponent<UnitData>();

        if (unitData == null)
            unitData = GetComponentInChildren<UnitData>();

        if (unitData == null)
            unitData = GetComponentInParent<UnitData>();
    }

    private DayNightManager GetDayNightManager()
    {
        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightManager>();

        return dayNightManager;
    }

    // ============================================================
    // PLAYER LOCKED
    // ============================================================

    public bool IsPlayerLocked()
    {
        return playerLocked;
    }

    public void SetPlayerLocked(bool value)
    {
        playerLocked = value;
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(CharacterSO data)
    {
        if (data == null)
            return;

        characterData = data;
        abilities.Clear();

        List<AbilitySO> characterAbilities = data.GetAbilities();

        if (characterAbilities != null)
        {
            for (int i = 0; i < characterAbilities.Count; i++)
            {
                AbilitySO abilitySO = characterAbilities[i];

                if (abilitySO == null)
                    continue;

                abilities.Add(new AbilityData(abilitySO));
            }
        }

        if (unitData != null)
            unitData.Initialize(data);

        RegisterWithUpdateManager();
    }

    private void RegisterWithUpdateManager()
    {
        if (unitData == null)
            return;

        if (updateManager == null)
            updateManager = FindFirstObjectByType<UpdateManager>();

        if (updateManager == null)
            return;

        updateManager.SetCurrentUnit(unitData);
    }

    // ============================================================
    // ABILITY DATA
    // ============================================================

    private AbilityData GetAbilityData(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return null;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
                continue;

            if (abilityData.GetAbilitySO() == abilitySO)
                return abilityData;
        }

        return null;
    }

    public bool HasAbility(AbilitySO abilitySO)
    {
        return GetAbilityData(abilitySO) != null;
    }

    public int GetAbilityCooldown(AbilitySO abilitySO)
    {
        AbilityData abilityData = GetAbilityData(abilitySO);

        return abilityData == null
            ? -1
            : abilityData.GetCooldownRemaining();
    }

    public bool IsAbilityOnCooldown(AbilitySO abilitySO)
    {
        AbilityData abilityData = GetAbilityData(abilitySO);

        return abilityData != null && abilityData.IsOnCooldown();
    }

    public int GetAbilityUsesRemaining(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return -1;

        if (abilitySO.GetUsesPerTurn() <= 0)
            return 0;

        AbilityData abilityData = GetAbilityData(abilitySO);

        return abilityData == null
            ? -1
            : abilityData.GetUsesRemaining();
    }

    public bool HasAbilityUsesRemaining(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return false;

        if (abilitySO.GetUsesPerTurn() <= 0)
            return true;

        return GetAbilityUsesRemaining(abilitySO) > 0;
    }

    private bool ConsumeAbilityUse(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return false;

        AbilityData abilityData = GetAbilityData(abilitySO);

        return abilityData != null && abilityData.ConsumeUse();
    }

    private bool CanUseAbilityAfterMovement(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return false;

        if (moveBrain == null)
            return true;

        if (!moveBrain.HasConsumedMovement())
            return true;

        return abilitySO.CanAttackWithThisAfterMove();
    }

    public bool IsAbilityReady(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return false;

        if (!HasAbility(abilitySO))
            return false;

        int uses = GetAbilityUsesRemaining(abilitySO);

        if (abilitySO.GetUsesPerTurn() > 0 && uses <= 0)
            return false;

        return CanUseAbilityAfterMovement(abilitySO);
    }

    public void StartNewRound()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
                continue;

            abilityData.ReduceCooldown();
        }
    }

    // ============================================================
    // GRID POSITION
    // ============================================================

    public void SetLogicalGridPosition(Vector2Int position)
    {
        logicalGridPosition = position;
        hasLogicalGridPosition = true;

        if (unitTilePin == null)
            unitTilePin = GetComponent<UnitTilePin>();

        if (unitTilePin != null)
            unitTilePin.SetTile(position);

        if (cachedGridManager == null)
            EnsureGridManager();
    }

    public Vector2Int GetLogicalGridPosition()
    {
        EnsureGridManager();

        if (unitTilePin != null && unitTilePin.HasTile())
        {
            logicalGridPosition = unitTilePin.GetTile();
            hasLogicalGridPosition = true;

            return logicalGridPosition;
        }

        if (cachedGridManager != null)
        {
            logicalGridPosition =
                cachedGridManager.WorldToGridPosition(transform.position);

            hasLogicalGridPosition = true;
        }

        return logicalGridPosition;
    }

    public bool HasLogicalGridPosition()
    {
        return hasLogicalGridPosition;
    }

    public bool HasMovedThisTurn()
    {
        return moveBrain != null && moveBrain.HasConsumedMovement();
    }

    public void SetHasMovedThisTurn(bool value)
    {
    }

    // ============================================================
    // ATTACK
    // ============================================================

    public bool Attack(GameObject target, AbilitySO selectedAbility)
    {
        if (!CanAttack())
            return false;

        if (attackInProgress)
            return false;

        if (target == null || selectedAbility == null)
            return false;

        if (!HasAbility(selectedAbility))
            return false;

        if (!IsAbilityReady(selectedAbility))
            return false;

        if (!IsValidTarget(target))
            return false;

        EnsureGridManager();

        if (cachedGridManager == null)
            return false;

        if (!selectedAbility.CanHit(cachedGridManager, gameObject, target))
            return false;

        Team team = GetTeam();

        if (team == Team.Player || team == Team.Ally)
        {
            attackInProgress = true;

            bool usedSuccessfully = selectedAbility.Use(gameObject, target);

            if (!usedSuccessfully)
            {
                ClearPendingAttack();
                return false;
            }

            CompleteAbilityUse(selectedAbility);

            TriggerAttackVisuals(
                selectedAbility,
                target,
                ResolveUnitTile(target));

            ClearPendingAttack();

            return true;
        }

        animationEventAbility = selectedAbility;
        animationEventTarget = target;
        animationEventTargetTile = ResolveUnitTile(target);
        animationEventFired = false;
        attackInProgress = true;
        waitingForEggLanding = false;

        if (useDirectionalEnemyAttackAnimation)
        {
            if (animationController != null)
                PlayEnemyAttackAnimation(target);
            else
                ClearPendingAttack();
        }
        else
        {
            if (attackAnimation != null)
                attackAnimation.PlayAttackAnimation();
            else
                ClearPendingAttack();
        }

        return attackInProgress;
    }

    public bool PlayerAttack(GameObject target, AbilitySO selectedAbility)
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
            return false;

        return Attack(target, selectedAbility);
    }

    // ============================================================
    // ATTACK AT TILE
    // ============================================================

    public bool AttackAtTile(Vector2Int targetTile, AbilitySO selectedAbility)
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
            return false;

        if (!CanAttack())
            return false;

        if (attackInProgress)
            return false;

        if (selectedAbility == null)
            return false;

        if (!IsAbilityReady(selectedAbility))
            return false;

        EnsureGridManager();

        if (cachedGridManager == null)
            return false;

        if (!cachedGridManager.IsInsideGrid(targetTile))
            return false;

        if (!selectedAbility.CanHitTile(
                cachedGridManager,
                gameObject,
                targetTile))
        {
            return false;
        }

        // Player-locked units cannot attack empty tiles.
        if (playerLocked)
        {
            bool containsPlayer = false;

            List<AttackUnit> allUnits =
                CombatUtility.GetAllAliveUnits();

            for (int i = 0; i < allUnits.Count; i++)
            {
                AttackUnit unit = allUnits[i];

                if (unit == null || unit == this)
                    continue;

                if (unit.GetTeam() != Team.Player)
                    continue;

                if (ResolveUnitTile(unit.gameObject) == targetTile)
                {
                    containsPlayer = true;
                    break;
                }
            }

            if (!containsPlayer)
                return false;
        }

        Team team = GetTeam();

        if (team == Team.Player || team == Team.Ally)
        {
            attackInProgress = true;

            bool usedSuccessfully = selectedAbility.UseAtTile(
                gameObject,
                cachedGridManager,
                targetTile);

            if (!usedSuccessfully)
            {
                ClearPendingAttack();
                return false;
            }

            CompleteAbilityUse(selectedAbility);

            TriggerAttackVisuals(
                selectedAbility,
                null,
                targetTile);

            ClearPendingAttack();

            return true;
        }

        animationEventAbility = selectedAbility;
        animationEventTarget = null;
        animationEventTargetTile = targetTile;
        animationEventFired = false;
        attackInProgress = true;
        waitingForEggLanding = false;

        if (useDirectionalEnemyAttackAnimation &&
            animationController != null)
        {
            Vector2Int attackerTile = ResolveUnitTile(gameObject);

            animationController.PlayEnemyAttack(
                attackerTile,
                targetTile);
        }
        else if (attackAnimation != null)
        {
            attackAnimation.PlayAttackAnimation();
        }
        else
        {
            ClearPendingAttack();
        }

        return attackInProgress;
    }

    // ============================================================
    // COMPLETE ABILITY
    // ============================================================

    private void CompleteAbilityUse(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return;

        if (abilitySO.GetUsesPerTurn() > 0)
            ConsumeAbilityUse(abilitySO);

        OnAbilityUsed?.Invoke(this, abilitySO);
    }

    // ============================================================
    // ANIMATION EVENTS
    // ============================================================

    public void OnAttackAnimationEvent()
    {
        if (animationEventFired)
            return;

        if (!attackInProgress)
            return;

        animationEventFired = true;

        AbilitySO ability = animationEventAbility;
        GameObject target = animationEventTarget;
        Vector2Int targetTile = animationEventTargetTile;

        if (ability == null)
        {
            ClearPendingAttack();
            return;
        }

        bool usedSuccessfully = false;

        if (target != null)
        {
            if (!IsValidTarget(target))
            {
                ClearPendingAttack();
                return;
            }

            usedSuccessfully = ability.Use(gameObject, target);
        }
        else
        {
            EnsureGridManager();

            if (cachedGridManager != null)
            {
                // Revalidate player lock when the animation event fires.
                if (playerLocked)
                {
                    bool containsPlayer = false;

                    List<AttackUnit> allUnits =
                        CombatUtility.GetAllAliveUnits();

                    for (int i = 0; i < allUnits.Count; i++)
                    {
                        AttackUnit unit = allUnits[i];

                        if (unit == null || unit == this)
                            continue;

                        if (unit.GetTeam() != Team.Player)
                            continue;

                        if (ResolveUnitTile(unit.gameObject) == targetTile)
                        {
                            containsPlayer = true;
                            break;
                        }
                    }

                    if (!containsPlayer)
                    {
                        ClearPendingAttack();
                        return;
                    }
                }

                usedSuccessfully = ability.UseAtTile(
                    gameObject,
                    cachedGridManager,
                    targetTile);
            }
        }

        if (!usedSuccessfully)
        {
            ClearPendingAttack();
            return;
        }

        waitingForEggLanding = ability is EggAbil;

        CompleteAbilityUse(ability);

        TriggerAttackVisuals(
            ability,
            target,
            targetTile);

        animationEventAbility = null;
        animationEventTarget = null;
        animationEventTargetTile = Vector2Int.zero;
    }

    public void OnAttackAnimationFinished()
    {
        if (!attackInProgress)
            return;

        if (waitingForEggLanding)
            return;

        ClearPendingAttack();
    }

    public void OnEggLanded()
    {
        if (!attackInProgress)
            return;

        if (!waitingForEggLanding)
            return;

        waitingForEggLanding = false;

        OnAttackAnimationFinished();
    }

    public IEnumerator WaitForCurrentAttackFinished()
    {
        while (attackInProgress)
            yield return null;
    }

    private void TriggerAttackVisuals(
        AbilitySO abilitySO,
        GameObject target,
        Vector2Int targetTile)
    {
    }

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (!Attack(target, selectedAbility))
            yield break;

        yield return StartCoroutine(WaitForCurrentAttackFinished());
    }

    // ============================================================
    // RESOLVE UNIT TILE
    // ============================================================

    private Vector2Int ResolveUnitTile(GameObject unit)
    {
        if (unit == null)
            return Vector2Int.zero;

        UnitTilePin pin = unit.GetComponent<UnitTilePin>();

        if (pin != null && pin.HasTile())
            return pin.GetTile();

        EnsureGridManager();

        if (cachedGridManager != null)
        {
            return cachedGridManager.WorldToGridPosition(
                unit.transform.position);
        }

        return Vector2Int.zero;
    }

    // ============================================================
    // ENEMY ATTACK ANIMATION
    // ============================================================

    private void PlayEnemyAttackAnimation(GameObject target)
    {
        if (animationController == null || target == null)
            return;

        EnsureGridManager();

        if (cachedGridManager == null)
            return;

        Vector2Int attackerTile = ResolveUnitTile(gameObject);
        Vector2Int targetTile = ResolveUnitTile(target);

        animationController.PlayEnemyAttackDown(
            attackerTile,
            targetTile);
    }

    private IEnumerator WaitForEnemyAttackAnimation()
    {
        if (animationController == null)
            yield break;

        Animator animator = animationController.GetAnimator();

        if (animator == null)
            yield break;

        if (attackAnimatorLayerIndex < 0)
            FindAttackAnimatorLayer();

        if (attackAnimatorLayerIndex < 0)
            yield break;

        yield return null;

        float timeout = 2f;
        float timer = 0f;

        while (timer < timeout)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
                    attackAnimatorLayerIndex);

            if (IsEnemyAttackState(stateInfo))
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= timeout)
            yield break;

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
                    attackAnimatorLayerIndex);

            bool isAttackState = IsEnemyAttackState(stateInfo);

            if (!isAttackState)
                yield break;

            if (stateInfo.normalizedTime >= 1f &&
                !animator.IsInTransition(attackAnimatorLayerIndex))
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool IsEnemyAttackState(AnimatorStateInfo stateInfo)
    {
        return
            stateInfo.IsName(attackAnimatorLayerName + ".enemyattackdownup") ||
            stateInfo.IsName(attackAnimatorLayerName + ".enemyattackdowndown") ||
            stateInfo.IsName(attackAnimatorLayerName + ".enemyattackdownleft") ||
            stateInfo.IsName(attackAnimatorLayerName + ".enemyattackdownright") ||

            stateInfo.IsName("enemyattackdownup") ||
            stateInfo.IsName("enemyattackdowndown") ||
            stateInfo.IsName("enemyattackdownleft") ||
            stateInfo.IsName("enemyattackdownright");
    }

    public IEnumerator WaitForAttackAnimation()
    {
        Team currentTeam = GetTeam();

        if (currentTeam == Team.Player || currentTeam == Team.Ally)
            yield break;

        if (!useDirectionalEnemyAttackAnimation ||
            animationController == null)
        {
            if (attackAnimation != null)
            {
                yield return StartCoroutine(
                    attackAnimation.WaitForAttackFinished());
            }

            yield break;
        }

        yield return StartCoroutine(WaitForCurrentAttackFinished());
    }

    public bool UsesDirectionalEnemyAttackAnimation()
    {
        return useDirectionalEnemyAttackAnimation &&
               animationController != null;
    }

    // ============================================================
    // VALID TARGET - PLAYER LOCKED
    // ============================================================

    public bool IsValidTarget(GameObject target)
    {
        if (target == null || target == gameObject)
            return false;

        if (healthManager == null)
            return false;

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null || targetHealth.IsDead())
            return false;

        Team targetTeam = targetHealth.GetTeam();

        // Player-locked units can ONLY target players.
        if (playerLocked && targetTeam != Team.Player)
            return false;

        // Enemy units can target explosive barrels,
        // even if the barrel has the same team.
        ExplosiveBarrel barrel =
            target.GetComponent<ExplosiveBarrel>();


        // Normal targeting rules.
        return targetTeam != healthManager.GetTeam();
    }

    // ============================================================
    // CAN ATTACK
    // ============================================================

    public bool CanAttack()
    {
        if (healthManager == null)
            return false;

        if (healthManager.IsDead())
            return false;

        if (attackInProgress)
            return false;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData != null &&
                abilityData.GetAbilitySO() != null)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDead()
    {
        return healthManager == null || healthManager.IsDead();
    }

    // ============================================================
    // GRID MANAGER
    // ============================================================

    public GridManager GetGridManager()
    {
        EnsureGridManager();
        return cachedGridManager;
    }

    private void EnsureGridManager()
    {
        if (cachedGridManager == null)
            cachedGridManager = FindFirstObjectByType<GridManager>();
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
            return;

        Vector2Int gridPosition = GetLogicalGridPosition();

        if (!cachedGridManager.IsInsideGrid(gridPosition))
            return;

        Vector3 targetPosition =
            cachedGridManager.GridToWorldPosition(gridPosition);

        transform.position = targetPosition;
    }

    public Vector2Int GetCurrentGridPosition()
    {
        return GetLogicalGridPosition();
    }

    public Vector2Int GetWorldDetectedGridPosition()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
            return Vector2Int.zero;

        return cachedGridManager.WorldToGridPosition(transform.position);
    }

    // ============================================================
    // ATTACK RANGE
    // ============================================================

    public int GetAttackRange()
    {
        int maxRange = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
                continue;

            AbilitySO abilitySO = abilityData.GetAbilitySO();

            if (abilitySO == null)
                continue;

            if (!IsAbilityReady(abilitySO))
                continue;

            int range = GetEffectiveRange(abilitySO);

            maxRange = Mathf.Max(maxRange, range);
        }

        return maxRange;
    }

    public int GetMaximumAttackRange()
    {
        int maxRange = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
                continue;

            AbilitySO abilitySO = abilityData.GetAbilitySO();

            if (abilitySO == null)
                continue;

            int range = GetEffectiveRange(abilitySO);

            maxRange = Mathf.Max(maxRange, range);
        }

        return maxRange;
    }

    // ============================================================
    // EFFECTIVE DAMAGE
    // ============================================================

    public int GetEffectiveDamage(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return 0;

        int damage = abilitySO.GetDamage();

        UpgradeableCombatUnit upgradeable =
            GetUpgradeableCombatUnit();

        if (upgradeable != null)
            damage += upgradeable.GetBonusDamage();

        damage += GetDayNightDamageBonus();

        damage += abilitySO.GetAbilityDamageRingBonus(gameObject);

        return Mathf.Max(0, damage);
    }

    public int GetEffectiveRange(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return 0;

        int range = abilitySO.GetRange();

        UpgradeableCombatUnit upgradeable =
            GetUpgradeableCombatUnit();

        if (upgradeable != null)
            range += upgradeable.GetBonusRange();

        return range;
    }

    // ============================================================
    // DAY / NIGHT BONUSES
    // ============================================================

    private int GetDayNightDamageBonus()
    {
        if (dayNightType == DayNightType.NeutralType)
            return 0;

        DayNightManager manager = GetDayNightManager();

        if (manager == null)
            return 0;

        if (manager.IsDay())
        {
            if (dayNightType == DayNightType.DayType)
                return dayDamageBonus;

            return 0;
        }

        if (dayNightType == DayNightType.NightType)
            return nightDamageBonus;

        return 0;
    }

    private int GetDayNightMoveRangeBonus()
    {
        if (dayNightType == DayNightType.NeutralType)
            return 0;

        DayNightManager manager = GetDayNightManager();

        if (manager == null)
            return 0;

        if (manager.IsDay())
        {
            if (dayNightType == DayNightType.DayType)
                return dayMoveRangeBonus;

            return 0;
        }

        if (dayNightType == DayNightType.NightType)
            return nightMoveRangeBonus;

        return 0;
    }

    public int GetEffectiveMoveRange()
    {
        if (characterData == null)
            return 0;

        int baseRange = Mathf.Max(0, characterData.moveRange);
        int bonus = GetDayNightMoveRangeBonus();

        return Mathf.Max(0, baseRange + bonus);
    }

    public DayNightType GetDayNightType()
    {
        return dayNightType;
    }

    public int GetCurrentDayNightDamageBonus()
    {
        return GetDayNightDamageBonus();
    }

    public int GetCurrentDayNightMoveRangeBonus()
    {
        return GetDayNightMoveRangeBonus();
    }

    // ============================================================
    // ABILITY ACCESSORS
    // ============================================================

    public List<AbilityData> GetRuntimeAbilities()
    {
        return abilities;
    }

    public List<AbilitySO> GetAbilities()
    {
        List<AbilitySO> result = new List<AbilitySO>();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
                continue;

            AbilitySO abilitySO = abilityData.GetAbilitySO();

            if (abilitySO != null)
                result.Add(abilitySO);
        }

        return result;
    }

    public int GetAbilityCount()
    {
        return abilities.Count;
    }

    public void AddAbility(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return;

        if (HasAbility(abilitySO))
            return;

        abilities.Add(new AbilityData(abilitySO));
    }

    public void RemoveAbility(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return;

        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
            return;

        abilities.Remove(abilityData);
    }

    // ============================================================
    // TEAM AND REFERENCES
    // ============================================================

    public Team GetTeam()
    {
        return healthManager == null
            ? Team.Ally
            : healthManager.GetTeam();
    }

    public HealthManager GetHealthManager()
    {
        return healthManager;
    }

    public CharacterSO GetCharacterData()
    {
        return characterData;
    }

    public UnitData GetUnitData()
    {
        return unitData;
    }

    public UpgradeableCombatUnit GetUpgradeableCombatUnitReference()
    {
        return GetUpgradeableCombatUnit();
    }

    public bool IsAttackInProgress()
    {
        return attackInProgress;
    }

    // ============================================================
    // CLEAR ATTACK
    // ============================================================

    private void ClearPendingAttack()
    {
        animationEventAbility = null;
        animationEventTarget = null;
        animationEventTargetTile = Vector2Int.zero;

        animationEventFired = false;
        waitingForEggLanding = false;
        attackInProgress = false;
    }

    // ============================================================
    // ADD ABILITY USE
    // ============================================================

    public bool AddAbilityUse(AbilitySO abilitySO)
    {
        if (abilitySO == null)
            return false;

        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
            return false;

        abilityData.AddUse();

        return true;
    }
}
