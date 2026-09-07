using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    // ============================================================
    // STATIC EVENTS
    // ============================================================

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

    private UnitMoveBrain moveBrain;

    private IAttackAnimation attackAnimation;


    // ============================================================
    // GRID
    // ============================================================

    private GridManager cachedGridManager;

    [SerializeField]
    private Vector2Int logicalGridPosition;

    private bool hasLogicalGridPosition;


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

        EnsureGridManager();


        if (
            !hasLogicalGridPosition &&
            cachedGridManager != null
        )
        {
            logicalGridPosition =
                cachedGridManager
                    .WorldToGridPosition(
                        transform.position
                    );

            hasLogicalGridPosition =
                true;
        }


        if (characterData != null)
        {
            Initialize(characterData);
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
            return;
        }


        // Reference only.
        // We do NOT modify the CharacterSO.
        characterData =
            data;


        // Remove references from this encounter's
        // local list.
        //
        // The actual AbilityData objects for player
        // characters are stored by PlayerDataManager.
        abilities.Clear();


        List<AbilitySO> characterAbilities =
            data.GetAbilities();


        if (characterAbilities == null)
        {
            return;
        }


        // ========================================================
        // PLAYER DATA MANAGER
        // ========================================================

        PlayerDataManager playerDataManager =
            PlayerDataManager.Instance;


        // ========================================================
        // CREATE / RESTORE ABILITIES
        // ========================================================

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


            AbilityData runtimeAbility;


            // ====================================================
            // PERSISTENT ABILITY DATA
            // ====================================================
            //
            // PlayerDataManager returns the existing AbilityData
            // if this character/ability already exists.
            //
            // Therefore:
            //
            // Encounter 1:
            //     new AbilityData
            //
            // Encounter 2:
            //     same AbilityData
            //
            // Encounter 3:
            //     same AbilityData
            //
            // The cooldown is NOT reset.
            // ====================================================

            if (playerDataManager != null)
            {
                runtimeAbility =
                    playerDataManager
                        .GetOrCreateAbilityData(
                            data,
                            abilitySO
                        );
            }
            else
            {
                // Fallback for situations where there is
                // no PlayerDataManager.
                //
                // This keeps the unit functional.
                runtimeAbility =
                    new AbilityData(
                        abilitySO
                    );
            }


            if (runtimeAbility != null)
            {
                abilities.Add(
                    runtimeAbility
                );
            }
        }
    }


    // ============================================================
    // GET RUNTIME ABILITY
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


            if (
                abilityData == null
            )
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


        if (
            cachedGridManager == null
        )
        {
            EnsureGridManager();
        }
    }


    public Vector2Int
        GetLogicalGridPosition()
    {
        if (
            !hasLogicalGridPosition
        )
        {
            EnsureGridManager();


            if (
                cachedGridManager != null
            )
            {
                logicalGridPosition =
                    cachedGridManager
                        .WorldToGridPosition(
                            transform.position
                        );


                hasLogicalGridPosition =
                    true;
            }
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
    // MOVEMENT STATE
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
    // USES PER TURN
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
            abilitySO
                .GetUsesPerTurn() <= 0
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


        // 0 = unlimited
        if (
            abilitySO
                .GetUsesPerTurn() <= 0
        )
        {
            return true;
        }


        return
            GetAbilityUsesRemaining(
                abilitySO
            ) > 0;
    }


    // ============================================================
    // CONSUME ABILITY USE
    // ============================================================

    private bool ConsumeAbilityUse(
        AbilitySO abilitySO
    )
    {
        if (abilitySO == null)
        {
            return false;
        }


        // Unlimited uses.
        if (
            abilitySO
                .GetUsesPerTurn() <= 0
        )
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
    // READY
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


    // ============================================================
    // PUBLIC ABILITY CHECK
    // ============================================================

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


            // Cooldown continues to count down
            // normally between turns.
            abilityData
                .ReduceCooldown();


            // Uses per turn reset normally.
            abilityData
                .ResetUses();
        }
    }


    // ============================================================
    // COOLDOWN START
    // ============================================================

    private void StartAbilityCooldown(
        AbilitySO abilitySO
    )
    {
        AbilityData abilityData =
            GetAbilityData(
                abilitySO
            );


        if (abilityData == null)
        {
            return;
        }


        abilityData.SetCooldown(
            abilitySO
                .GetCooldown()
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


        if (
            !IsAbilityReady(
                selectedAbility
            )
        )
        {
            return false;
        }


        EnsureGridManager();


        if (
            cachedGridManager == null
        )
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


        if (
            !selectedAbility.Use(
                gameObject,
                target
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


        if (
            selectedAbility == null
        )
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


        if (
            cachedGridManager == null
        )
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
        bool usesExhausted =
            ConsumeAbilityUse(
                abilitySO
            );


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


        if (
            selectedAbility == null
        )
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


        if (
            cachedGridManager == null
        )
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


        if (
            attackAnimation == null
        )
        {
            yield break;
        }


        attackAnimation
            .PlayAttackAnimation();


        yield return StartCoroutine(
            attackAnimation
                .WaitForAttackFinished()
        );
    }


    // ============================================================
    // GENERIC TARGET CHECK
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


        if (
            healthManager == null
        )
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
        if (
            cachedGridManager != null
        )
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


        if (
            cachedGridManager == null
        )
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


    // ============================================================
    // CURRENT WORLD-DETECTED POSITION
    // ============================================================

    public Vector2Int
        GetWorldDetectedGridPosition()
    {
        EnsureGridManager();


        if (
            cachedGridManager == null
        )
        {
            return
                Vector2Int.zero;
        }


        return
            cachedGridManager
                .WorldToGridPosition(
                    transform.position
                );
    }


    // ============================================================
    // RANGE
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


            if (
                abilityData == null
            )
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


            if (
                abilityData == null
            )
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


            if (
                abilityData == null
            )
            {
                continue;
            }


            AbilitySO abilitySO =
                abilityData
                    .GetAbilitySO();


            if (
                abilitySO != null
            )
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


        if (
            HasAbility(
                abilitySO
            )
        )
        {
            return;
        }


        AbilityData runtimeAbility;


        // Use the persistent runtime data if
        // PlayerDataManager exists.
        PlayerDataManager playerDataManager =
            PlayerDataManager.Instance;


        if (playerDataManager != null &&
            characterData != null)
        {
            runtimeAbility =
                playerDataManager
                    .GetOrCreateAbilityData(
                        characterData,
                        abilitySO
                    );
        }
        else
        {
            runtimeAbility =
                new AbilityData(
                    abilitySO
                );
        }


        if (runtimeAbility != null)
        {
            abilities.Add(
                runtimeAbility
            );
        }
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


        if (
            abilityData == null
        )
        {
            return;
        }


        abilities.Remove(
            abilityData
        );
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

    public Team GetTeam()
    {
        return
            healthManager == null
                ? Team.Ally
                : healthManager.GetTeam();
    }


    public HealthManager
        GetHealthManager()
    {
        return healthManager;
    }


    public CharacterSO
        GetCharacterData()
    {
        return characterData;
    }
}