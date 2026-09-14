using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    public static event Action<AttackUnit, AbilitySO> OnAbilityUsed;

    [Header("Character Data")]
    [SerializeField]
    private CharacterSO characterData;

    [Header("Unit Data")]
    [SerializeField]
    private UnitData unitData;

    [Header("Abilities")]
    [SerializeField]
    private List<AbilityData> abilities = new();

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    [SerializeField]
    private AnimationController animationController;

    [Header("Attack Animation")]
    [SerializeField]
    private bool useDirectionalEnemyAttackAnimation;

    [SerializeField]
    private string attackAnimatorLayerName = "attackLayer";

    private UpdateManager updateManager;
    private UnitMoveBrain moveBrain;
    private IAttackAnimation attackAnimation;
    private GridManager cachedGridManager;
    private UnitTilePin unitTilePin;

    private int attackAnimatorLayerIndex = -1;

    [SerializeField]
    private Vector2Int logicalGridPosition;

    private bool hasLogicalGridPosition;

    private AbilitySO animationEventAbility;
    private GameObject animationEventTarget;
    private Vector2Int animationEventTargetTile;
    private bool animationEventFired;

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager = GetComponent<HealthManager>();
        }

        moveBrain = GetComponent<UnitMoveBrain>();
        attackAnimation = GetComponent<IAttackAnimation>();
        unitTilePin = GetComponent<UnitTilePin>();

        if (animationController == null)
        {
            animationController = GetComponent<AnimationController>();
        }

        FindUnitData();

        updateManager = FindFirstObjectByType<UpdateManager>();

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
                initialTile = cachedGridManager.WorldToGridPosition(transform.position);
            }

            logicalGridPosition = initialTile;
            hasLogicalGridPosition = true;
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
    }

    private void FindAttackAnimatorLayer()
    {
        attackAnimatorLayerIndex = -1;

        if (animationController == null)
        {
            return;
        }

        Animator animator = animationController.GetComponent<Animator>();

        if (animator == null)
        {
            return;
        }

        attackAnimatorLayerIndex = animator.GetLayerIndex(attackAnimatorLayerName);
    }

    private void FindUnitData()
    {
        if (unitData == null)
        {
            unitData = GetComponent<UnitData>();
        }

        if (unitData == null)
        {
            unitData = GetComponentInChildren<UnitData>();
        }

        if (unitData == null)
        {
            unitData = GetComponentInParent<UnitData>();
        }
    }

    public void Initialize(CharacterSO data)
    {
        if (data == null)
        {
            return;
        }

        characterData = data;

        abilities.Clear();

        List<AbilitySO> characterAbilities = data.GetAbilities();

        if (characterAbilities != null)
        {
            for (int i = 0; i < characterAbilities.Count; i++)
            {
                AbilitySO abilitySO = characterAbilities[i];

                if (abilitySO == null)
                {
                    continue;
                }

                AbilityData runtimeAbility = new AbilityData(abilitySO);
                abilities.Add(runtimeAbility);
            }
        }

        if (unitData != null)
        {
            unitData.Initialize(data);
        }

        RegisterWithUpdateManager();
    }

    private void RegisterWithUpdateManager()
    {
        if (unitData == null)
        {
            return;
        }

        if (updateManager == null)
        {
            updateManager = FindFirstObjectByType<UpdateManager>();
        }

        if (updateManager == null)
        {
            return;
        }

        updateManager.SetCurrentUnit(unitData);
    }

    private AbilityData GetAbilityData(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return null;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            if (abilityData.GetAbilitySO() == abilitySO)
            {
                return abilityData;
            }
        }

        return null;
    }

    public bool HasAbility(AbilitySO abilitySO)
    {
        return GetAbilityData(abilitySO) != null;
    }

    public void SetLogicalGridPosition(Vector2Int position)
    {
        logicalGridPosition = position;
        hasLogicalGridPosition = true;

        if (unitTilePin == null)
        {
            unitTilePin = GetComponent<UnitTilePin>();
        }

        if (unitTilePin != null)
        {
            unitTilePin.SetTile(position);
        }

        if (cachedGridManager == null)
        {
            EnsureGridManager();
        }
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

    public int GetAbilityCooldown(AbilitySO abilitySO)
    {
        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
        {
            return -1;
        }

        return abilityData.GetCooldownRemaining();
    }

    public bool IsAbilityOnCooldown(AbilitySO abilitySO)
    {
        return GetAbilityCooldown(abilitySO) > 0;
    }

    public int GetAbilityUsesRemaining(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return -1;
        }

        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return 0;
        }

        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
        {
            return -1;
        }

        return abilityData.GetUsesRemaining();
    }

    public bool HasAbilityUsesRemaining(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return false;
        }

        if (abilitySO.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        return GetAbilityUsesRemaining(abilitySO) > 0;
    }

    private bool ConsumeAbilityUse(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return false;
        }

        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
        {
            return false;
        }

        return abilityData.ConsumeUse();
    }

    private bool CanUseAbilityAfterMovement(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return false;
        }

        if (moveBrain == null)
        {
            return true;
        }

        if (!moveBrain.HasConsumedMovement())
        {
            return true;
        }

        return abilitySO.CanAttackWithThisAfterMove();
    }

    public bool IsAbilityReady(AbilitySO abilitySO)
    {
        if (!HasAbility(abilitySO))
        {
            return false;
        }

        if (GetAbilityCooldown(abilitySO) > 0)
        {
            return false;
        }

        if (!HasAbilityUsesRemaining(abilitySO))
        {
            return false;
        }

        return CanUseAbilityAfterMovement(abilitySO);
    }

    public bool CanUseAbility(AbilitySO abilitySO)
    {
        return IsAbilityReady(abilitySO);
    }

    public void StartNewRound()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            abilityData.ReduceCooldown();
            abilityData.ResetUses();
        }
    }

    private void StartAbilityCooldown(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return;
        }

        AbilityData abilityData = GetAbilityData(abilitySO);

        if (abilityData == null)
        {
            return;
        }

        abilityData.SetCooldown(abilitySO.GetCooldown());
    }

    public bool Attack(GameObject target, AbilitySO selectedAbility)
    {
        if (!CanAttack())
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        if (selectedAbility == null)
        {
            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        if (!selectedAbility.CanHit(cachedGridManager, gameObject, target))
        {
            return false;
        }

        if (!selectedAbility.Use(gameObject, target))
        {
            return false;
        }

        animationEventAbility = selectedAbility;
        animationEventTarget = target;
        animationEventTargetTile = ResolveUnitTile(target);
        animationEventFired = false;

        CompleteAbilityUse(selectedAbility);

        if (useDirectionalEnemyAttackAnimation)
        {
            if (animationController != null)
            {
                PlayEnemyAttackAnimation(target);
            }
        }

        return true;
    }

    public bool PlayerAttack(GameObject target, AbilitySO selectedAbility)
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
        {
            return false;
        }

        return Attack(target, selectedAbility);
    }

    public bool AttackAtTile(
        Vector2Int targetTile,
        AbilitySO selectedAbility
    )
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
        {
            return false;
        }

        if (!CanAttack())
        {
            return false;
        }

        if (selectedAbility == null)
        {
            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        if (!cachedGridManager.IsInsideGrid(targetTile))
        {
            return false;
        }

        if (!selectedAbility.CanHitTile(
                cachedGridManager,
                gameObject,
                targetTile))
        {
            return false;
        }

        if (!selectedAbility.UseAtTile(
                gameObject,
                cachedGridManager,
                targetTile))
        {
            return false;
        }

        animationEventAbility = selectedAbility;
        animationEventTarget = null;
        animationEventTargetTile = targetTile;
        animationEventFired = false;

        CompleteAbilityUse(selectedAbility);

        return true;
    }

    private void CompleteAbilityUse(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return;
        }

        bool usesExhausted = false;

        if (abilitySO.GetUsesPerTurn() > 0)
        {
            usesExhausted = ConsumeAbilityUse(abilitySO);
        }

        if (usesExhausted)
        {
            StartAbilityCooldown(abilitySO);
        }

        OnAbilityUsed?.Invoke(this, abilitySO);
    }

    public void OnAttackAnimationEvent()
    {
        if (animationEventFired)
        {
            return;
        }

        animationEventFired = true;

        TriggerAttackVisuals(
            animationEventAbility,
            animationEventTarget,
            animationEventTargetTile
        );
    }

    private void TriggerAttackVisuals(
        AbilitySO abilitySO,
        GameObject target,
        Vector2Int targetTile
    )
    {
    }

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility
    )
    {
        if (!CanAttack())
        {
            yield break;
        }

        if (target == null)
        {
            yield break;
        }

        if (selectedAbility == null)
        {
            yield break;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            yield break;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            yield break;
        }

        if (!selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target))
        {
            yield break;
        }

        if (!selectedAbility.Use(gameObject, target))
        {
            yield break;
        }

        animationEventAbility = selectedAbility;
        animationEventTarget = target;
        animationEventTargetTile = ResolveUnitTile(target);
        animationEventFired = false;

        CompleteAbilityUse(selectedAbility);

        if (
            useDirectionalEnemyAttackAnimation &&
            animationController != null
        )
        {
            PlayEnemyAttackAnimation(target);

            yield return StartCoroutine(
                WaitForEnemyAttackAnimation()
            );

            yield break;
        }

        if (attackAnimation == null)
        {
            yield break;
        }

        attackAnimation.PlayAttackAnimation();

        yield return StartCoroutine(
            attackAnimation.WaitForAttackFinished()
        );
    }

    private Vector2Int ResolveUnitTile(GameObject unit)
    {
        if (unit == null)
        {
            return Vector2Int.zero;
        }

        UnitTilePin pin = unit.GetComponent<UnitTilePin>();

        if (pin != null && pin.HasTile())
        {
            return pin.GetTile();
        }

        EnsureGridManager();

        if (cachedGridManager != null)
        {
            return cachedGridManager.WorldToGridPosition(
                unit.transform.position
            );
        }

        return Vector2Int.zero;
    }

    private void PlayEnemyAttackAnimation(GameObject target)
    {
        if (animationController == null)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return;
        }

        Vector2Int attackerTile = ResolveUnitTile(gameObject);
        Vector2Int targetTile = ResolveUnitTile(target);

        animationController.PlayEnemyAttackDown(
            attackerTile,
            targetTile
        );
    }

    private IEnumerator WaitForEnemyAttackAnimation()
    {
        if (animationController == null)
        {
            yield break;
        }

        Animator animator =
            animationController.GetComponent<Animator>();

        if (animator == null)
        {
            yield break;
        }

        if (attackAnimatorLayerIndex < 0)
        {
            FindAttackAnimatorLayer();
        }

        if (attackAnimatorLayerIndex < 0)
        {
            yield break;
        }

        yield return null;

        float timeout = 2f;
        float timer = 0f;

        while (timer < timeout)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
                    attackAnimatorLayerIndex
                );

            if (IsEnemyAttackState(stateInfo))
            {
                break;
            }

            timer += Time.deltaTime;

            yield return null;
        }

        if (timer >= timeout)
        {
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
                    attackAnimatorLayerIndex
                );

            bool isAttackState =
                IsEnemyAttackState(stateInfo);

            if (!isAttackState)
            {
                yield break;
            }

            if (
                stateInfo.normalizedTime >= 1f &&
                !animator.IsInTransition(
                    attackAnimatorLayerIndex
                )
            )
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool IsEnemyAttackState(
        AnimatorStateInfo stateInfo
    )
    {
        return
            stateInfo.IsName(
                attackAnimatorLayerName +
                ".enemyattackdownup"
            ) ||
            stateInfo.IsName(
                attackAnimatorLayerName +
                ".enemyattackdowndown"
            ) ||
            stateInfo.IsName(
                attackAnimatorLayerName +
                ".enemyattackdownleft"
            ) ||
            stateInfo.IsName(
                attackAnimatorLayerName +
                ".enemyattackdownright"
            ) ||
            stateInfo.IsName(
                "enemyattackdownup"
            ) ||
            stateInfo.IsName(
                "enemyattackdowndown"
            ) ||
            stateInfo.IsName(
                "enemyattackdownleft"
            ) ||
            stateInfo.IsName(
                "enemyattackdownright"
            );
    }

    public IEnumerator WaitForAttackAnimation()
    {
        if (
            !useDirectionalEnemyAttackAnimation ||
            animationController == null
        )
        {
            yield break;
        }

        yield return StartCoroutine(
            WaitForEnemyAttackAnimation()
        );
    }

    public bool UsesDirectionalEnemyAttackAnimation()
    {
        return
            useDirectionalEnemyAttackAnimation &&
            animationController != null;
    }

    public bool IsValidTarget(GameObject target)
    {
        if (
            target == null ||
            target == gameObject
        )
        {
            return false;
        }

        if (healthManager == null)
        {
            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (
            targetHealth == null ||
            targetHealth.IsDead()
        )
        {
            return false;
        }

        return
            targetHealth.GetTeam() !=
            healthManager.GetTeam();
    }

    public bool CanAttack()
    {
        if (
            healthManager == null ||
            healthManager.IsDead()
        )
        {
            return false;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (
                abilityData != null &&
                abilityData.GetAbilitySO() != null
            )
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDead()
    {
        return
            healthManager == null ||
            healthManager.IsDead();
    }

    public GridManager GetGridManager()
    {
        EnsureGridManager();

        return cachedGridManager;
    }

    private void EnsureGridManager()
    {
        if (cachedGridManager != null)
        {
            return;
        }

        cachedGridManager =
            FindFirstObjectByType<GridManager>();
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return;
        }

        Vector2Int gridPosition =
            GetLogicalGridPosition();

        if (!cachedGridManager.IsInsideGrid(gridPosition))
        {
            return;
        }

        Vector3 targetPosition =
            cachedGridManager.GridToWorldPosition(
                gridPosition
            );

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
        {
            return Vector2Int.zero;
        }

        return cachedGridManager.WorldToGridPosition(
            transform.position
        );
    }

    public int GetAttackRange()
    {
        int maxRange = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            AbilitySO abilitySO =
                abilityData.GetAbilitySO();

            if (
                abilitySO != null &&
                IsAbilityReady(abilitySO)
            )
            {
                maxRange = Mathf.Max(
                    maxRange,
                    abilitySO.GetRange()
                );
            }
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
            {
                continue;
            }

            AbilitySO abilitySO =
                abilityData.GetAbilitySO();

            if (abilitySO != null)
            {
                maxRange = Mathf.Max(
                    maxRange,
                    abilitySO.GetRange()
                );
            }
        }

        return maxRange;
    }

    public List<AbilityData> GetRuntimeAbilities()
    {
        return abilities;
    }

    public List<AbilitySO> GetAbilities()
    {
        List<AbilitySO> result =
            new List<AbilitySO>();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData abilityData = abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            AbilitySO abilitySO =
                abilityData.GetAbilitySO();

            if (abilitySO != null)
            {
                result.Add(abilitySO);
            }
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
        {
            return;
        }

        if (HasAbility(abilitySO))
        {
            return;
        }

        AbilityData runtimeAbility =
            new AbilityData(abilitySO);

        abilities.Add(runtimeAbility);
    }

    public void RemoveAbility(AbilitySO abilitySO)
    {
        if (abilitySO == null)
        {
            return;
        }

        AbilityData abilityData =
            GetAbilityData(abilitySO);

        if (abilityData == null)
        {
            return;
        }

        abilities.Remove(abilityData);
    }

    public Team GetTeam()
    {
        return
            healthManager == null
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
}
