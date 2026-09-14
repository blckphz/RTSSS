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

    // ============================================================
    // PENDING ATTACK
    // ============================================================

    private AbilitySO animationEventAbility;
    private GameObject animationEventTarget;
    private Vector2Int animationEventTargetTile;

    private bool animationEventFired;

    // True from Attack() until AttackFinished animation event.
    private bool attackInProgress;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager =
                GetComponent<HealthManager>();
        }

        moveBrain =
            GetComponent<UnitMoveBrain>();

        attackAnimation =
            GetComponent<IAttackAnimation>();

        unitTilePin =
            GetComponent<UnitTilePin>();

        if (animationController == null)
        {
            animationController =
                GetComponent<AnimationController>();
        }

        FindUnitData();

        updateManager =
            FindFirstObjectByType<UpdateManager>();

        EnsureGridManager();

        FindAttackAnimatorLayer();

        if (cachedGridManager != null)
        {
            Vector2Int initialTile;

            if (
                unitTilePin != null &&
                unitTilePin.HasTile()
            )
            {
                initialTile =
                    unitTilePin.GetTile();
            }
            else
            {
                initialTile =
                    cachedGridManager.WorldToGridPosition(
                        transform.position
                    );
            }

            logicalGridPosition =
                initialTile;

            hasLogicalGridPosition =
                true;
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
    }


    // ============================================================
    // FIND ATTACK ANIMATOR LAYER
    // ============================================================

    private void FindAttackAnimatorLayer()
    {
        attackAnimatorLayerIndex = -1;

        if (animationController == null)
        {
            return;
        }

        Animator animator =
            animationController.GetAnimator();

        if (animator == null)
        {
            return;
        }

        attackAnimatorLayerIndex =
            animator.GetLayerIndex(
                attackAnimatorLayerName
            );
    }


    // ============================================================
    // FIND UNIT DATA
    // ============================================================

    private void FindUnitData()
    {
        if (unitData == null)
        {
            unitData =
                GetComponent<UnitData>();
        }

        if (unitData == null)
        {
            unitData =
                GetComponentInChildren<UnitData>();
        }

        if (unitData == null)
        {
            unitData =
                GetComponentInParent<UnitData>();
        }
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(CharacterSO data)
    {
        if (data == null)
        {
            return;
        }

        characterData =
            data;

        abilities.Clear();

        List<AbilitySO> characterAbilities =
            data.GetAbilities();

        if (characterAbilities != null)
        {
            for (
                int i = 0;
                i < characterAbilities.Count;
                i++
            )
            {
                AbilitySO abilitySO =
                    characterAbilities[i];

                if (abilitySO == null)
                {
                    continue;
                }

                AbilityData runtimeAbility =
                    new AbilityData(abilitySO);

                abilities.Add(
                    runtimeAbility
                );
            }
        }

        if (unitData != null)
        {
            unitData.Initialize(data);
        }

        RegisterWithUpdateManager();
    }


    // ============================================================
    // REGISTER UPDATE MANAGER
    // ============================================================

    private void RegisterWithUpdateManager()
    {
        if (unitData == null)
        {
            return;
        }

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }

        if (updateManager == null)
        {
            return;
        }

        updateManager.SetCurrentUnit(
            unitData
        );
    }


    // ============================================================
    // GET ABILITY DATA
    // ============================================================

    private AbilityData GetAbilityData(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return null;
        }

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            if (
                abilityData.GetAbilitySO() ==
                abilitySO
            )
            {
                return abilityData;
            }
        }

        return null;
    }


    // ============================================================
    // HAS ABILITY
    // ============================================================

    public bool HasAbility(
        AbilitySO abilitySO
    )
    {
        return
            GetAbilityData(abilitySO) != null;
    }


    // ============================================================
    // LOGICAL GRID POSITION
    // ============================================================

    public void SetLogicalGridPosition(
        Vector2Int position
    )
    {
        logicalGridPosition =
            position;

        hasLogicalGridPosition =
            true;

        if (unitTilePin == null)
        {
            unitTilePin =
                GetComponent<UnitTilePin>();
        }

        if (unitTilePin != null)
        {
            unitTilePin.SetTile(
                position
            );
        }

        if (cachedGridManager == null)
        {
            EnsureGridManager();
        }
    }


    public Vector2Int GetLogicalGridPosition()
    {
        EnsureGridManager();

        if (
            unitTilePin != null &&
            unitTilePin.HasTile()
        )
        {
            logicalGridPosition =
                unitTilePin.GetTile();

            hasLogicalGridPosition =
                true;

            return logicalGridPosition;
        }

        if (cachedGridManager != null)
        {
            logicalGridPosition =
                cachedGridManager.WorldToGridPosition(
                    transform.position
                );

            hasLogicalGridPosition =
                true;
        }

        return logicalGridPosition;
    }


    public bool HasLogicalGridPosition()
    {
        return hasLogicalGridPosition;
    }


    // ============================================================
    // MOVEMENT
    // ============================================================

    public bool HasMovedThisTurn()
    {
        return
            moveBrain != null &&
            moveBrain.HasConsumedMovement();
    }


    public void SetHasMovedThisTurn(
        bool value
    )
    {
        // Intentionally empty.
        // Movement state is controlled by UnitMoveBrain.
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    public int GetAbilityCooldown(
        AbilitySO abilitySO
    )
    {
        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );

        if (abilityData == null)
        {
            return -1;
        }

        return
            abilityData.GetCooldownRemaining();
    }


    public bool IsAbilityOnCooldown(
        AbilitySO abilitySO
    )
    {
        return
            GetAbilityCooldown(
                abilitySO
            ) > 0;
    }


    // ============================================================
    // USES
    // ============================================================

    public int GetAbilityUsesRemaining(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return -1;
        }

        if (
            abilitySO.GetUsesPerTurn() <= 0
        )
        {
            return 0;
        }

        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );

        if (abilityData == null)
        {
            return -1;
        }

        return
            abilityData.GetUsesRemaining();
    }


    public bool HasAbilityUsesRemaining(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return false;
        }

        if (
            abilitySO.GetUsesPerTurn() <= 0
        )
        {
            return true;
        }

        return
            GetAbilityUsesRemaining(
                abilitySO
            ) > 0;
    }


    private bool ConsumeAbilityUse(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return false;
        }

        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );

        if (abilityData == null)
        {
            return false;
        }

        return
            abilityData.ConsumeUse();
    }


    // ============================================================
    // MOVEMENT / ATTACK VALIDATION
    // ============================================================

    private bool CanUseAbilityAfterMovement(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return false;
        }

        if (moveBrain == null)
        {
            return true;
        }

        if (
            !moveBrain.HasConsumedMovement()
        )
        {
            return true;
        }

        return
            abilitySO.CanAttackWithThisAfterMove();
    }


    public bool IsAbilityReady(
        AbilitySO abilitySO
    )
    {
        if (!HasAbility(abilitySO))
        {
            return false;
        }

        if (
            GetAbilityCooldown(
                abilitySO
            ) > 0
        )
        {
            return false;
        }

        if (
            !HasAbilityUsesRemaining(
                abilitySO
            )
        )
        {
            return false;
        }

        return
            CanUseAbilityAfterMovement(
                abilitySO
            );
    }


    public bool CanUseAbility(
        AbilitySO abilitySO
    )
    {
        return
            IsAbilityReady(
                abilitySO
            );
    }


    // ============================================================
    // ROUND
    // ============================================================

    public void StartNewRound()
    {
        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            abilityData.ReduceCooldown();
            abilityData.ResetUses();
        }
    }


    // ============================================================
    // START COOLDOWN
    // ============================================================

    private void StartAbilityCooldown(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return;
        }

        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );

        if (abilityData == null)
        {
            return;
        }

        abilityData.SetCooldown(
            abilitySO.GetCooldown()
        );
    }


    // ============================================================
    // ATTACK
    //
    // IMPORTANT:
    //
    // NO DAMAGE IS APPLIED HERE.
    //
    // The ability is queued and waits for AttackHit.
    // ============================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility
    )
    {
        if (!CanAttack())
        {
            return false;
        }

        if (attackInProgress)
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

        if (
            !IsAbilityReady(
                selectedAbility
            )
        )
        {
            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        if (
            !selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            )
        )
        {
            return false;
        }

        // --------------------------------------------------------
        // QUEUE ATTACK
        // --------------------------------------------------------

        animationEventAbility =
            selectedAbility;

        animationEventTarget =
            target;

        animationEventTargetTile =
            ResolveUnitTile(target);

        animationEventFired =
            false;

        attackInProgress =
            true;

        // --------------------------------------------------------
        // START ATTACK ANIMATION
        // --------------------------------------------------------

        if (
            useDirectionalEnemyAttackAnimation
        )
        {
            if (animationController != null)
            {
                PlayEnemyAttackAnimation(
                    target
                );
            }
            else
            {
                ClearPendingAttack();
            }
        }
        else
        {
            if (attackAnimation != null)
            {
                attackAnimation.PlayAttackAnimation();
            }
            else
            {
                ClearPendingAttack();
            }
        }

        return attackInProgress;
    }


    // ============================================================
    // PLAYER ATTACK
    // ============================================================

    public bool PlayerAttack(
        GameObject target,
        AbilitySO selectedAbility
    )
    {
        if (
            !CombatUtility.IsPlayerTurnInputAllowed(
                this
            )
        )
        {
            return false;
        }

        return Attack(
            target,
            selectedAbility
        );
    }


    // ============================================================
    // ATTACK AT TILE
    // ============================================================

    public bool AttackAtTile(
        Vector2Int targetTile,
        AbilitySO selectedAbility
    )
    {
        if (
            !CombatUtility.IsPlayerTurnInputAllowed(
                this
            )
        )
        {
            return false;
        }

        if (!CanAttack())
        {
            return false;
        }

        if (attackInProgress)
        {
            return false;
        }

        if (selectedAbility == null)
        {
            return false;
        }

        if (
            !IsAbilityReady(
                selectedAbility
            )
        )
        {
            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        if (
            !cachedGridManager.IsInsideGrid(
                targetTile
            )
        )
        {
            return false;
        }

        if (
            !selectedAbility.CanHitTile(
                cachedGridManager,
                gameObject,
                targetTile
            )
        )
        {
            return false;
        }

        // --------------------------------------------------------
        // QUEUE ATTACK
        // --------------------------------------------------------

        animationEventAbility =
            selectedAbility;

        animationEventTarget =
            null;

        animationEventTargetTile =
            targetTile;

        animationEventFired =
            false;

        attackInProgress =
            true;

        // --------------------------------------------------------
        // START ATTACK ANIMATION
        // --------------------------------------------------------

        if (
            useDirectionalEnemyAttackAnimation &&
            animationController != null
        )
        {
            Vector2Int attackerTile =
                ResolveUnitTile(gameObject);

            animationController.PlayEnemyAttack(
                attackerTile,
                targetTile
            );
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
    // COMPLETE ABILITY USE
    //
    // Called AFTER AttackHit has applied the ability.
    // ============================================================

    private void CompleteAbilityUse(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return;
        }

        bool usesExhausted =
            false;

        if (
            abilitySO.GetUsesPerTurn() > 0
        )
        {
            usesExhausted =
                ConsumeAbilityUse(
                    abilitySO
                );
        }

        if (usesExhausted)
        {
            StartAbilityCooldown(
                abilitySO
            );
        }

        OnAbilityUsed?.Invoke(
            this,
            abilitySO
        );
    }


    // ============================================================
    // ATTACK HIT ANIMATION EVENT
    //
    // AnimationController.AttackHit()
    // calls this method.
    //
    // THIS IS THE ONLY PLACE WHERE THE ABILITY IS EXECUTED.
    // ============================================================

    public void OnAttackAnimationEvent()
    {
        if (animationEventFired)
        {
            return;
        }

        if (!attackInProgress)
        {
            return;
        }

        animationEventFired =
            true;

        AbilitySO ability =
            animationEventAbility;

        GameObject target =
            animationEventTarget;

        Vector2Int targetTile =
            animationEventTargetTile;

        if (ability == null)
        {
            ClearPendingAttack();
            return;
        }

        bool usedSuccessfully =
            false;

        // --------------------------------------------------------
        // UNIT TARGET
        // --------------------------------------------------------

        if (target != null)
        {
            if (!IsValidTarget(target))
            {
                ClearPendingAttack();
                return;
            }

            // ====================================================
            // DAMAGE / EFFECT HAPPENS EXACTLY HERE
            // ====================================================

            usedSuccessfully =
                ability.Use(
                    gameObject,
                    target
                );
        }

        // --------------------------------------------------------
        // TILE TARGET
        // --------------------------------------------------------

        else
        {
            EnsureGridManager();

            if (cachedGridManager != null)
            {
                // =================================================
                // TILE EFFECT HAPPENS EXACTLY HERE
                // =================================================

                usedSuccessfully =
                    ability.UseAtTile(
                        gameObject,
                        cachedGridManager,
                        targetTile
                    );
            }
        }

        // --------------------------------------------------------
        // ABILITY FAILED
        // --------------------------------------------------------

        if (!usedSuccessfully)
        {
            ClearPendingAttack();
            return;
        }

        // --------------------------------------------------------
        // CONSUME USE / COOLDOWN
        // --------------------------------------------------------

        CompleteAbilityUse(
            ability
        );

        // --------------------------------------------------------
        // HIT VISUALS
        // --------------------------------------------------------

        TriggerAttackVisuals(
            ability,
            target,
            targetTile
        );

        // --------------------------------------------------------
        // DO NOT CLEAR attackInProgress.
        //
        // The animation is still playing.
        //
        // AttackFinished() will call:
        //
        // OnAttackAnimationFinished()
        //
        // which clears the attack.
        // --------------------------------------------------------

        animationEventAbility =
            null;

        animationEventTarget =
            null;

        animationEventTargetTile =
            Vector2Int.zero;
    }


    // ============================================================
    // ATTACK ANIMATION FINISHED
    //
    // AnimationController.AttackFinished()
    // calls this method.
    //
    // This allows the NEXT attack to start.
    // ============================================================

    public void OnAttackAnimationFinished()
    {
        if (!attackInProgress)
        {
            return;
        }

        Debug.Log(
            "[AttackUnit] Attack animation finished."
            + " | Unit: "
            + gameObject.name
        );

        ClearPendingAttack();
    }


    // ============================================================
    // WAIT FOR CURRENT ATTACK FINISHED
    //
    // UnitAttackBrain should yield this before starting
    // another attack.
    // ============================================================

    public IEnumerator WaitForCurrentAttackFinished()
    {
        while (attackInProgress)
        {
            yield return null;
        }
    }


    // ============================================================
    // ATTACK VISUALS
    // ============================================================

    private void TriggerAttackVisuals(
        AbilitySO abilitySO,
        GameObject target,
        Vector2Int targetTile
    )
    {
        // Put hit VFX / impact effects here if needed.
        //
        // This method is called at the exact AttackHit event.
    }


    // ============================================================
    // ATTACK ROUTINE
    //
    // This version also waits for AttackFinished.
    // ============================================================

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility
    )
    {
        if (!Attack(
                target,
                selectedAbility
            ))
        {
            yield break;
        }

        // --------------------------------------------------------
        // Attack() has already started the animation.
        //
        // Wait until AnimationController.AttackFinished()
        // releases attackInProgress.
        // --------------------------------------------------------

        yield return StartCoroutine(
            WaitForCurrentAttackFinished()
        );
    }


    // ============================================================
    // RESOLVE UNIT TILE
    // ============================================================

    private Vector2Int ResolveUnitTile(
        GameObject unit
    )
    {
        if (unit == null)
        {
            return Vector2Int.zero;
        }

        UnitTilePin pin =
            unit.GetComponent<UnitTilePin>();

        if (
            pin != null &&
            pin.HasTile()
        )
        {
            return pin.GetTile();
        }

        EnsureGridManager();

        if (cachedGridManager != null)
        {
            return
                cachedGridManager.WorldToGridPosition(
                    unit.transform.position
                );
        }

        return Vector2Int.zero;
    }


    // ============================================================
    // PLAY ENEMY ATTACK ANIMATION
    // ============================================================

    private void PlayEnemyAttackAnimation(
        GameObject target
    )
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

        Vector2Int attackerTile =
            ResolveUnitTile(gameObject);

        Vector2Int targetTile =
            ResolveUnitTile(target);

        animationController.PlayEnemyAttackDown(
            attackerTile,
            targetTile
        );
    }


    // ============================================================
    // WAIT FOR ENEMY ATTACK ANIMATION
    //
    // Kept for compatibility with existing code.
    //
    // The preferred completion mechanism is now AttackFinished.
    // ============================================================

    private IEnumerator WaitForEnemyAttackAnimation()
    {
        if (animationController == null)
        {
            yield break;
        }

        Animator animator =
            animationController.GetAnimator();

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
                IsEnemyAttackState(
                    stateInfo
                );

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


    // ============================================================
    // IS ENEMY ATTACK STATE
    // ============================================================

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


    // ============================================================
    // WAIT FOR ATTACK ANIMATION
    // ============================================================

    public IEnumerator WaitForAttackAnimation()
    {
        if (
            !useDirectionalEnemyAttackAnimation ||
            animationController == null
        )
        {
            if (attackAnimation != null)
            {
                yield return StartCoroutine(
                    attackAnimation.WaitForAttackFinished()
                );
            }

            yield break;
        }

        yield return StartCoroutine(
            WaitForCurrentAttackFinished()
        );
    }


    // ============================================================
    // USES DIRECTIONAL ATTACK ANIMATION
    // ============================================================

    public bool UsesDirectionalEnemyAttackAnimation()
    {
        return
            useDirectionalEnemyAttackAnimation &&
            animationController != null;
    }


    // ============================================================
    // VALID TARGET
    // ============================================================

    public bool IsValidTarget(
        GameObject target
    )
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


    // ============================================================
    // CAN ATTACK
    // ============================================================

    public bool CanAttack()
    {
        if (
            healthManager == null ||
            healthManager.IsDead()
        )
        {
            return false;
        }

        // --------------------------------------------------------
        // IMPORTANT:
        //
        // Don't allow another attack while the current animation
        // is still playing.
        // --------------------------------------------------------

        if (attackInProgress)
        {
            return false;
        }

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

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


    // ============================================================
    // DEAD
    // ============================================================

    public bool IsDead()
    {
        return
            healthManager == null ||
            healthManager.IsDead();
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
        if (cachedGridManager != null)
        {
            return;
        }

        cachedGridManager =
            FindFirstObjectByType<GridManager>();
    }


    // ============================================================
    // SNAP TO GRID
    // ============================================================

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

        if (
            !cachedGridManager.IsInsideGrid(
                gridPosition
            )
        )
        {
            return;
        }

        Vector3 targetPosition =
            cachedGridManager.GridToWorldPosition(
                gridPosition
            );

        transform.position =
            targetPosition;
    }


    // ============================================================
    // GRID POSITIONS
    // ============================================================

    public Vector2Int GetCurrentGridPosition()
    {
        return
            GetLogicalGridPosition();
    }


    public Vector2Int GetWorldDetectedGridPosition()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return Vector2Int.zero;
        }

        return
            cachedGridManager.WorldToGridPosition(
                transform.position
            );
    }


    // ============================================================
    // ATTACK RANGE
    // ============================================================

    public int GetAttackRange()
    {
        int maxRange = 0;

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

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
                maxRange =
                    Mathf.Max(
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

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            AbilitySO abilitySO =
                abilityData.GetAbilitySO();

            if (abilitySO != null)
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        abilitySO.GetRange()
                    );
            }
        }

        return maxRange;
    }


    // ============================================================
    // ABILITIES
    // ============================================================

    public List<AbilityData> GetRuntimeAbilities()
    {
        return abilities;
    }


    public List<AbilitySO> GetAbilities()
    {
        List<AbilitySO> result =
            new List<AbilitySO>();

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilityData abilityData =
                abilities[i];

            if (abilityData == null)
            {
                continue;
            }

            AbilitySO abilitySO =
                abilityData.GetAbilitySO();

            if (abilitySO != null)
            {
                result.Add(
                    abilitySO
                );
            }
        }

        return result;
    }


    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    public void AddAbility(
        AbilitySO abilitySO
    )
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
            new AbilityData(
                abilitySO
            );

        abilities.Add(
            runtimeAbility
        );
    }


    public void RemoveAbility(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return;
        }

        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );

        if (abilityData == null)
        {
            return;
        }

        abilities.Remove(
            abilityData
        );
    }


    // ============================================================
    // TEAM / DATA
    // ============================================================

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


    // ============================================================
    // ATTACK STATE
    // ============================================================

    public bool IsAttackInProgress()
    {
        return attackInProgress;
    }


    // ============================================================
    // CLEAR PENDING ATTACK
    // ============================================================

    private void ClearPendingAttack()
    {
        animationEventAbility = null;

        animationEventTarget = null;

        animationEventTargetTile =
            Vector2Int.zero;

        animationEventFired = false;

        attackInProgress = false;
    }
}