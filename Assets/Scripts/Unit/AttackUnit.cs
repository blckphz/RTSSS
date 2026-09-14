using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    public static event Action<
        AttackUnit,
        AbilitySO
    > OnAbilityUsed;


    // ============================================================
    // CHARACTER DATA
    // ============================================================

    [Header("Character Data")]
    [SerializeField]
    private CharacterSO characterData;


    // ============================================================
    // UNIT DATA
    // ============================================================

    [Header("Unit Data")]
    [SerializeField]
    private UnitData unitData;


    // ============================================================
    // ABILITIES
    // ============================================================

    [Header("Abilities")]
    [SerializeField]
    private List<AbilityData> abilities =
        new();


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    [SerializeField]
    private AnimationController animationController;


    // ============================================================
    // ATTACK ANIMATION
    // ============================================================

    [Header("Attack Animation")]

    [SerializeField]
    private bool useDirectionalEnemyAttackAnimation;

    [SerializeField]
    private string attackAnimatorLayerName =
        "attackLayer";


    // ============================================================
    // INTERNAL REFERENCES
    // ============================================================

    private UpdateManager updateManager;

    private UnitMoveBrain moveBrain;

    private IAttackAnimation attackAnimation;

    private GridManager cachedGridManager;

    private UnitTilePin unitTilePin;


    // ============================================================
    // ATTACK LAYER
    // ============================================================

    private int attackAnimatorLayerIndex = -1;


    // ============================================================
    // LOGICAL GRID POSITION
    // ============================================================

    [SerializeField]
    private Vector2Int logicalGridPosition;

    private bool hasLogicalGridPosition;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        Debug.Log(
            "[AttackUnit] Awake. Unit=" +
            name,
            this
        );


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


        // ========================================================
        // INITIAL TILE
        // ========================================================

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
                    cachedGridManager
                        .WorldToGridPosition(
                            transform.position
                        );
            }

            logicalGridPosition =
                initialTile;

            hasLogicalGridPosition =
                true;


            Debug.Log(
                "[AttackUnit] Initial logical tile = " +
                logicalGridPosition +
                " | Unit=" +
                name,
                this
            );
        }


        if (characterData != null)
        {
            Initialize(
                characterData
            );
        }


        Debug.Log(
            "[AttackUnit] Awake complete. " +
            "DirectionalEnemyAttack=" +
            useDirectionalEnemyAttackAnimation +
            " | AnimationController=" +
            (
                animationController != null
                    ? animationController.name
                    : "NULL"
            ) +
            " | AttackLayerIndex=" +
            attackAnimatorLayerIndex +
            " | Unit=" +
            name,
            this
        );
    }


    // ============================================================
    // FIND ATTACK ANIMATOR LAYER
    // ============================================================

    private void FindAttackAnimatorLayer()
    {
        attackAnimatorLayerIndex = -1;


        if (animationController == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Cannot find attack layer because " +
                "AnimationController is NULL. | Unit=" +
                name,
                this
            );

            return;
        }


        Animator animator =
            animationController
                .GetComponent<Animator>();


        if (animator == null)
        {
            Debug.LogWarning(
                "[AttackUnit] AnimationController has no Animator. " +
                "| Unit=" +
                name,
                this
            );

            return;
        }


        attackAnimatorLayerIndex =
            animator.GetLayerIndex(
                attackAnimatorLayerName
            );


        if (
            attackAnimatorLayerIndex < 0
        )
        {
            Debug.LogError(
                "[AttackUnit] Animator layer NOT FOUND. " +
                "Layer=" +
                attackAnimatorLayerName +
                " | Unit=" +
                name,
                this
            );

            return;
        }


        Debug.Log(
            "[AttackUnit] Found attack Animator layer. " +
            "Layer=" +
            attackAnimatorLayerName +
            " | Index=" +
            attackAnimatorLayerIndex +
            " | Unit=" +
            name,
            this
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

        if (unitData == null)
        {
            Debug.LogWarning(
                "[AttackUnit] No UnitData found. | Unit=" +
                name,
                this
            );
        }
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        CharacterSO data
    )
    {
        if (data == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Initialize called with NULL data. " +
                "| Unit=" +
                name,
                this
            );

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
                    new AbilityData(
                        abilitySO
                    );

                abilities.Add(
                    runtimeAbility
                );
            }
        }

        if (unitData != null)
        {
            unitData.Initialize(
                data
            );
        }

        RegisterWithUpdateManager();
    }


    // ============================================================
    // REGISTER WITH UPDATE MANAGER
    // ============================================================

    private void RegisterWithUpdateManager()
    {
        if (unitData == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Cannot register with " +
                "UpdateManager because UnitData is NULL. " +
                "| Unit=" +
                name,
                this
            );

            return;
        }

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }

        if (updateManager == null)
        {
            Debug.LogWarning(
                "[AttackUnit] UpdateManager not found. " +
                "| Unit=" +
                name,
                this
            );

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
            GetAbilityData(
                abilitySO
            ) != null;
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


        // ========================================================
        // KEEP UNIT TILE PIN IN SYNC
        // ========================================================

        if (unitTilePin == null)
        {
            unitTilePin =
                GetComponent<UnitTilePin>();
        }

        if (
            unitTilePin != null
        )
        {
            unitTilePin.SetTile(
                position
            );
        }


        if (cachedGridManager == null)
        {
            EnsureGridManager();
        }


        Debug.Log(
            "[AttackUnit] Logical tile updated. " +
            "Unit=" +
            name +
            " | Tile=" +
            position,
            this
        );
    }


    public Vector2Int
        GetLogicalGridPosition()
    {
        EnsureGridManager();


        // ========================================================
        // UNIT TILE PIN IS AUTHORITATIVE
        // ========================================================

        if (
            unitTilePin != null &&
            unitTilePin.HasTile()
        )
        {
            logicalGridPosition =
                unitTilePin.GetTile();

            hasLogicalGridPosition =
                true;

            return
                logicalGridPosition;
        }


        // ========================================================
        // FALL BACK TO WORLD POSITION
        // ========================================================

        if (cachedGridManager != null)
        {
            logicalGridPosition =
                cachedGridManager
                    .WorldToGridPosition(
                        transform.position
                    );

            hasLogicalGridPosition =
                true;
        }


        return
            logicalGridPosition;
    }


    public bool
        HasLogicalGridPosition()
    {
        return
            hasLogicalGridPosition;
    }


    // ============================================================
    // MOVEMENT
    // ============================================================

    public bool
        HasMovedThisTurn()
    {
        return
            moveBrain != null &&
            moveBrain
                .HasConsumedMovement();
    }


    public void SetHasMovedThisTurn(
        bool value
    )
    {
        // Movement state is handled
        // by UnitMoveBrain.
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
            abilityData
                .GetCooldownRemaining();
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
    // ABILITY USES
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
            abilityData
                .GetUsesRemaining();
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
            abilityData
                .ConsumeUse();
    }


    // ============================================================
    // MOVEMENT / ABILITY RESTRICTION
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
            !moveBrain
                .HasConsumedMovement()
        )
        {
            return true;
        }

        return
            abilitySO
                .CanAttackWithThisAfterMove();
    }


    // ============================================================
    // ABILITY READY
    // ============================================================

    public bool IsAbilityReady(
        AbilitySO abilitySO
    )
    {
        if (
            !HasAbility(
                abilitySO
            )
        )
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
    // NEW ROUND
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

            abilityData
                .ReduceCooldown();

            abilityData
                .ResetUses();
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
    // ============================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility
    )
    {
        Debug.Log(
            "[AttackUnit] Attack requested. " +
            "Unit=" +
            name +
            " | Target=" +
            (
                target != null
                    ? target.name
                    : "NULL"
            ) +
            " | Ability=" +
            (
                selectedAbility != null
                    ? selectedAbility.GetAbilityName()
                    : "NULL"
            ),
            this
        );


        if (!CanAttack())
        {
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "CanAttack() = false. | Unit=" +
                name,
                this
            );

            return false;
        }


        if (target == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "Target is NULL. | Unit=" +
                name,
                this
            );

            return false;
        }


        if (selectedAbility == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "Ability is NULL. | Unit=" +
                name,
                this
            );

            return false;
        }


        if (
            !IsAbilityReady(
                selectedAbility
            )
        )
        {
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "Ability is not ready. | Unit=" +
                name +
                " | Ability=" +
                selectedAbility.GetAbilityName(),
                this
            );

            return false;
        }


        EnsureGridManager();


        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit] Attack rejected: " +
                "GridManager is NULL. | Unit=" +
                name,
                this
            );

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
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "Ability cannot hit target. | Unit=" +
                name +
                " | Target=" +
                target.name,
                this
            );

            return false;
        }


        if (
            !selectedAbility.Use(
                gameObject,
                target
            )
        )
        {
            Debug.LogWarning(
                "[AttackUnit] Attack rejected: " +
                "Ability.Use() returned false. | Unit=" +
                name,
                this
            );

            return false;
        }


        CompleteAbilityUse(
            selectedAbility
        );


        // ========================================================
        // ENEMY DIRECTIONAL ATTACK
        // ========================================================

        if (
            useDirectionalEnemyAttackAnimation
        )
        {
            Debug.Log(
                "[AttackUnit] Directional enemy attack " +
                "animation requested. | Unit=" +
                name +
                " | Target=" +
                target.name,
                this
            );


            if (animationController == null)
            {
                Debug.LogError(
                    "[AttackUnit] Directional animation " +
                    "enabled but AnimationController is NULL. " +
                    "| Unit=" +
                    name,
                    this
                );
            }
            else
            {
                PlayEnemyAttackAnimation(
                    target
                );
            }
        }


        return true;
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
            !CombatUtility
                .IsPlayerTurnInputAllowed(
                    this
                )
        )
        {
            return false;
        }

        return
            Attack(
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
            !CombatUtility
                .IsPlayerTurnInputAllowed(
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
            !cachedGridManager
                .IsInsideGrid(
                    targetTile
                )
        )
        {
            return false;
        }

        if (
            !selectedAbility
                .CanHitTile(
                    cachedGridManager,
                    gameObject,
                    targetTile
                )
        )
        {
            return false;
        }

        if (
            !selectedAbility
                .UseAtTile(
                    gameObject,
                    cachedGridManager,
                    targetTile
                )
        )
        {
            return false;
        }

        CompleteAbilityUse(
            selectedAbility
        );

        return true;
    }


    // ============================================================
    // COMPLETE ABILITY USE
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
    // ATTACK ROUTINE
    // ============================================================

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

        if (
            !IsAbilityReady(
                selectedAbility
            )
        )
        {
            yield break;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            yield break;
        }

        if (
            !selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            )
        )
        {
            yield break;
        }

        if (
            !selectedAbility.Use(
                gameObject,
                target
            )
        )
        {
            yield break;
        }

        CompleteAbilityUse(
            selectedAbility
        );


        // --------------------------------------------------------
        // ENEMY
        // --------------------------------------------------------

        if (
            useDirectionalEnemyAttackAnimation &&
            animationController != null
        )
        {
            PlayEnemyAttackAnimation(
                target
            );

            yield return
                StartCoroutine(
                    WaitForEnemyAttackAnimation()
                );

            yield break;
        }


        // --------------------------------------------------------
        // PLAYER
        // --------------------------------------------------------

        if (attackAnimation == null)
        {
            yield break;
        }

        attackAnimation
            .PlayAttackAnimation();

        yield return
            StartCoroutine(
                attackAnimation
                    .WaitForAttackFinished()
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


        // ========================================================
        // UNIT TILE PIN
        // ========================================================

        UnitTilePin pin =
            unit.GetComponent<UnitTilePin>();

        if (
            pin != null &&
            pin.HasTile()
        )
        {
            return
                pin.GetTile();
        }


        // ========================================================
        // GRID MANAGER FALLBACK
        // ========================================================

        EnsureGridManager();

        if (cachedGridManager != null)
        {
            return
                cachedGridManager
                    .WorldToGridPosition(
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
        if (
            animationController == null
        )
        {
            Debug.LogError(
                "[AttackUnit] Cannot play enemy attack: " +
                "AnimationController is NULL. | Unit=" +
                name,
                this
            );

            return;
        }


        if (target == null)
        {
            Debug.LogError(
                "[AttackUnit] Cannot play enemy attack: " +
                "Target is NULL. | Unit=" +
                name,
                this
            );

            return;
        }


        EnsureGridManager();


        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit] Cannot play enemy attack: " +
                "GridManager is NULL. | Unit=" +
                name,
                this
            );

            return;
        }


        // ========================================================
        // IMPORTANT
        //
        // DO NOT USE GetLogicalGridPosition() HERE AS THE
        // ATTACKER SOURCE.
        //
        // Resolve both attacker and target using exactly the
        // same system.
        // ========================================================

        Vector2Int attackerTile =
            ResolveUnitTile(
                gameObject
            );


        Vector2Int targetTile =
            ResolveUnitTile(
                target
            );


        Vector2Int rawDirection =
            targetTile -
            attackerTile;


        Vector2Int direction =
            new Vector2Int(
                Mathf.Clamp(
                    rawDirection.x,
                    -1,
                    1
                ),
                Mathf.Clamp(
                    rawDirection.y,
                    -1,
                    1
                )
            );


        // ========================================================
        // DETAILED POSITION DEBUG
        // ========================================================

        Vector2Int attackerWorldTile =
            cachedGridManager
                .WorldToGridPosition(
                    transform.position
                );


        Vector2Int targetWorldTile =
            cachedGridManager
                .WorldToGridPosition(
                    target.transform.position
                );


        UnitTilePin attackerPin =
            GetComponent<UnitTilePin>();


        UnitTilePin targetPin =
            target.GetComponent<UnitTilePin>();


        string attackerPinText =
            attackerPin != null &&
            attackerPin.HasTile()
                ? attackerPin.GetTile().ToString()
                : "NO PIN";


        string targetPinText =
            targetPin != null &&
            targetPin.HasTile()
                ? targetPin.GetTile().ToString()
                : "NO PIN";


        Debug.Log(
            "[AttackUnit] ENEMY ATTACK POSITION DEBUG\n" +
            "Unit=" +
            name +
            "\n" +
            "Target=" +
            target.name +
            "\n\n" +
            "Attacker World=" +
            transform.position +
            "\n" +
            "Attacker Pin=" +
            attackerPinText +
            "\n" +
            "Attacker WorldToGrid=" +
            attackerWorldTile +
            "\n" +
            "Attacker Resolved=" +
            attackerTile +
            "\n\n" +
            "Target World=" +
            target.transform.position +
            "\n" +
            "Target Pin=" +
            targetPinText +
            "\n" +
            "Target WorldToGrid=" +
            targetWorldTile +
            "\n" +
            "Target Resolved=" +
            targetTile +
            "\n\n" +
            "Raw Direction=" +
            rawDirection +
            "\n" +
            "Clamped Direction=" +
            direction,
            this
        );


        // ========================================================
        // PLAY ANIMATION
        // ========================================================

        animationController
            .PlayEnemyAttackDown(
                attackerTile,
                targetTile
            );


        Debug.Log(
            "[AttackUnit] PlayEnemyAttackDown() called. " +
            " | Unit=" +
            name +
            " | Target=" +
            target.name +
            " | AttackerTile=" +
            attackerTile +
            " | TargetTile=" +
            targetTile +
            " | Direction=" +
            direction,
            this
        );
    }


    // ============================================================
    // WAIT FOR ENEMY ATTACK ANIMATION
    // ============================================================

    private IEnumerator
        WaitForEnemyAttackAnimation()
    {
        if (animationController == null)
        {
            Debug.LogWarning(
                "[AttackUnit] Cannot wait for enemy attack: " +
                "AnimationController is NULL. | Unit=" +
                name,
                this
            );

            yield break;
        }


        Animator animator =
            animationController
                .GetComponent<Animator>();


        if (animator == null)
        {
            Debug.LogError(
                "[AttackUnit] Cannot wait for enemy attack: " +
                "Animator is NULL. | Unit=" +
                name,
                this
            );

            yield break;
        }


        if (
            attackAnimatorLayerIndex < 0
        )
        {
            FindAttackAnimatorLayer();
        }


        if (
            attackAnimatorLayerIndex < 0
        )
        {
            Debug.LogError(
                "[AttackUnit] Cannot wait for enemy attack: " +
                "attackLayer was not found. | Unit=" +
                name,
                this
            );

            yield break;
        }


        Debug.Log(
            "[AttackUnit] Waiting for enemy attack animation " +
            "to enter attackLayer. | Unit=" +
            name +
            " | Layer=" +
            attackAnimatorLayerName +
            " | Index=" +
            attackAnimatorLayerIndex,
            this
        );


        yield return null;


        float timeout =
            2f;

        float timer =
            0f;


        while (timer < timeout)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
                    attackAnimatorLayerIndex
                );


            bool isAttackState =
                IsEnemyAttackState(
                    stateInfo
                );


            if (isAttackState)
            {
                Debug.Log(
                    "[AttackUnit] Enemy attack animation " +
                    "entered. " +
                    " | Unit=" +
                    name +
                    " | NormalizedTime=" +
                    stateInfo.normalizedTime,
                    this
                );

                break;
            }


            timer +=
                Time.deltaTime;


            yield return null;
        }


        if (timer >= timeout)
        {
            Debug.LogError(
                "[AttackUnit] Timed out waiting for enemy " +
                "attack animation. " +
                " | Unit=" +
                name +
                " | Layer=" +
                attackAnimatorLayerName +
                " | Index=" +
                attackAnimatorLayerIndex,
                this
            );

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
                Debug.Log(
                    "[AttackUnit] Enemy attack animation " +
                    "state ended. " +
                    " | Unit=" +
                    name,
                    this
                );

                yield break;
            }


            if (
                stateInfo.normalizedTime >= 1f &&
                !animator.IsInTransition(
                    attackAnimatorLayerIndex
                )
            )
            {
                Debug.Log(
                    "[AttackUnit] Enemy attack animation " +
                    "finished. " +
                    " | Unit=" +
                    name +
                    " | NormalizedTime=" +
                    stateInfo.normalizedTime,
                    this
                );

                yield break;
            }


            yield return null;
        }
    }


    // ============================================================
    // CHECK ENEMY ATTACK STATE
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
            yield break;
        }


        yield return
            StartCoroutine(
                WaitForEnemyAttackAnimation()
            );
    }


    // ============================================================
    // USES DIRECTIONAL ENEMY ATTACK
    // ============================================================

    public bool
        UsesDirectionalEnemyAttackAnimation()
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
            target.GetComponent<
                HealthManager
            >();

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

        return
            cachedGridManager;
    }


    private void EnsureGridManager()
    {
        if (cachedGridManager != null)
        {
            return;
        }

        cachedGridManager =
            FindFirstObjectByType<
                GridManager
            >();
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
            !cachedGridManager
                .IsInsideGrid(
                    gridPosition
                )
        )
        {
            return;
        }

        Vector3 targetPosition =
            cachedGridManager
                .GridToWorldPosition(
                    gridPosition
                );

        transform.position =
            targetPosition;
    }


    // ============================================================
    // CURRENT GRID POSITION
    // ============================================================

    public Vector2Int
        GetCurrentGridPosition()
    {
        return
            GetLogicalGridPosition();
    }


    public Vector2Int
        GetWorldDetectedGridPosition()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return Vector2Int.zero;
        }

        return
            cachedGridManager
                .WorldToGridPosition(
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
                abilityData
                    .GetAbilitySO();

            if (
                abilitySO != null &&
                IsAbilityReady(
                    abilitySO
                )
            )
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        abilitySO
                            .GetRange()
                    );
            }
        }

        return maxRange;
    }


    public int
        GetMaximumAttackRange()
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
                abilityData
                    .GetAbilitySO();

            if (abilitySO != null)
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        abilitySO
                            .GetRange()
                    );
            }
        }

        return maxRange;
    }


    // ============================================================
    // ABILITIES
    // ============================================================

    public List<AbilityData>
        GetRuntimeAbilities()
    {
        return abilities;
    }


    public List<AbilitySO>
        GetAbilities()
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
                abilityData
                    .GetAbilitySO();

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


    // ============================================================
    // ADD ABILITY
    // ============================================================

    public void AddAbility(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return;
        }

        if (
            HasAbility(
                abilitySO
            )
        )
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


    // ============================================================
    // REMOVE ABILITY
    // ============================================================

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
    // TEAM
    // ============================================================

    public Team GetTeam()
    {
        return
            healthManager == null
                ? Team.Ally
                : healthManager.GetTeam();
    }


    // ============================================================
    // HEALTH
    // ============================================================

    public HealthManager
        GetHealthManager()
    {
        return healthManager;
    }


    // ============================================================
    // CHARACTER DATA
    // ============================================================

    public CharacterSO
        GetCharacterData()
    {
        return characterData;
    }


    // ============================================================
    // UNIT DATA
    // ============================================================

    public UnitData
        GetUnitData()
    {
        return unitData;
    }
}