using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    // ============================================================
    // STATIC EVENTS
    // ============================================================

    public static event Action<AttackUnit, AbilitySO> OnAbilityUsed;


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
    private List<AbilitySO> abilities = new();


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    private UnitMoveBrain moveBrain;
    private IAttackAnimation attackAnimation;


    // ============================================================
    // ABILITY STATE
    // ============================================================

    private readonly Dictionary<AbilitySO, int> abilityCooldowns =
        new();

    private readonly Dictionary<AbilitySO, int> abilityUsesRemaining =
        new();


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
                cachedGridManager.WorldToGridPosition(
                    transform.position
                );

            hasLogicalGridPosition = true;
        }

        if (characterData != null)
        {
            Initialize(characterData);
        }
        else
        {
            InitializeCooldowns();
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

        if (characterAbilities == null)
        {
            InitializeCooldowns();
            return;
        }

        for (
            int i = 0;
            i < characterAbilities.Count;
            i++
        )
        {
            AbilitySO ability =
                characterAbilities[i];

            if (
                ability != null &&
                !abilities.Contains(ability)
            )
            {
                abilities.Add(ability);
            }
        }

        InitializeCooldowns();
    }


    // ============================================================
    // INITIALIZE ABILITY STATE
    // ============================================================

    private void InitializeCooldowns()
    {
        abilityCooldowns.Clear();
        abilityUsesRemaining.Clear();

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            AbilitySO ability =
                abilities[i];

            if (ability == null)
            {
                continue;
            }

            abilityCooldowns[ability] =
                0;

            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }
    }


    // ============================================================
    // ENSURE ABILITY STATE
    // ============================================================

    private bool EnsureAbilityState(
        AbilitySO ability)
    {
        if (
            ability == null ||
            !abilities.Contains(ability)
        )
        {
            return false;
        }

        if (!abilityCooldowns.ContainsKey(ability))
        {
            abilityCooldowns[ability] =
                0;
        }

        if (!abilityUsesRemaining.ContainsKey(ability))
        {
            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }

        return true;
    }


    // ============================================================
    // LOGICAL GRID POSITION
    // ============================================================

    public void SetLogicalGridPosition(
        Vector2Int position)
    {
        logicalGridPosition =
            position;

        hasLogicalGridPosition =
            true;

        if (cachedGridManager == null)
        {
            EnsureGridManager();
        }
    }


    public Vector2Int GetLogicalGridPosition()
    {
        if (!hasLogicalGridPosition)
        {
            EnsureGridManager();

            if (cachedGridManager != null)
            {
                logicalGridPosition =
                    cachedGridManager.WorldToGridPosition(
                        transform.position
                    );

                hasLogicalGridPosition =
                    true;
            }
        }

        return logicalGridPosition;
    }


    public bool HasLogicalGridPosition()
    {
        return hasLogicalGridPosition;
    }


    // ============================================================
    // MOVEMENT STATE
    // ============================================================

    public bool HasMovedThisTurn()
    {
        return
            moveBrain != null &&
            moveBrain.HasConsumedMovement();
    }


    public void SetHasMovedThisTurn(
        bool value)
    {
        // Movement state is owned by UnitMoveBrain.
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    public int GetAbilityCooldown(
        AbilitySO ability)
    {
        if (!EnsureAbilityState(ability))
        {
            return -1;
        }

        return abilityCooldowns.TryGetValue(
            ability,
            out int cooldown
        )
            ? cooldown
            : 0;
    }


    public bool IsAbilityOnCooldown(
        AbilitySO ability)
    {
        return
            GetAbilityCooldown(ability) > 0;
    }


    // ============================================================
    // USES PER TURN
    // ============================================================

    public int GetAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return -1;
        }

        int usesPerTurn =
            ability.GetUsesPerTurn();

        // 0 = unlimited.
        if (usesPerTurn <= 0)
        {
            return 0;
        }

        if (!EnsureAbilityState(ability))
        {
            return -1;
        }

        return abilityUsesRemaining.TryGetValue(
            ability,
            out int uses
        )
            ? uses
            : usesPerTurn;
    }


    public bool HasAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        // 0 = unlimited.
        if (ability.GetUsesPerTurn() <= 0)
        {
            return true;
        }

        return
            GetAbilityUsesRemaining(ability) > 0;
    }


    // ============================================================
    // CONSUME ABILITY USE
    // ============================================================

    private bool ConsumeAbilityUse(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        int usesPerTurn =
            ability.GetUsesPerTurn();

        // Unlimited-use ability.
        if (usesPerTurn <= 0)
        {
            return false;
        }

        if (!EnsureAbilityState(ability))
        {
            return false;
        }

        abilityUsesRemaining[ability] =
            Mathf.Max(
                0,
                abilityUsesRemaining[ability] - 1
            );

        return
            abilityUsesRemaining[ability] <= 0;
    }


    // ============================================================
    // MOVEMENT / ABILITY RESTRICTION
    // ============================================================

    private bool CanUseAbilityAfterMovement(
        AbilitySO ability)
    {
        if (ability == null)
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

        return
            ability.CanAttackWithThisAfterMove();
    }


    // ============================================================
    // READY
    // ============================================================

    public bool IsAbilityReady(
        AbilitySO ability)
    {
        if (!EnsureAbilityState(ability))
        {
            return false;
        }

        if (GetAbilityCooldown(ability) > 0)
        {
            return false;
        }

        if (!HasAbilityUsesRemaining(ability))
        {
            return false;
        }

        return
            CanUseAbilityAfterMovement(
                ability
            );
    }


    // ============================================================
    // PUBLIC ABILITY CHECK
    // ============================================================

    public bool CanUseAbility(
        AbilitySO ability)
    {
        return
            IsAbilityReady(ability);
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
            AbilitySO ability =
                abilities[i];

            if (ability == null)
            {
                continue;
            }

            EnsureAbilityState(ability);

            int currentCooldown =
                abilityCooldowns[ability];

            if (currentCooldown > 0)
            {
                abilityCooldowns[ability] =
                    Mathf.Max(
                        0,
                        currentCooldown - 1
                    );
            }

            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }
    }


    // ============================================================
    // COOLDOWN START
    // ============================================================

    private void StartAbilityCooldown(
        AbilitySO ability)
    {
        if (!EnsureAbilityState(ability))
        {
            return;
        }

        abilityCooldowns[ability] =
            Mathf.Max(
                0,
                ability.GetCooldown()
            );
    }


    // ============================================================
    // ATTACK
    // ============================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (!CanAttack())
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED CanAttack=false | " +
                "Unit=" + name +
                " | Team=" + GetTeam() +
                " | PlayerInputLocked=" +
                CombatUtility.IsPlayerInputLocked(),
                this
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED Target=NULL | " +
                "Unit=" + name +
                " | Team=" + GetTeam(),
                this
            );

            return false;
        }

        if (selectedAbility == null)
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED Ability=NULL | " +
                "Unit=" + name +
                " | Team=" + GetTeam(),
                this
            );

            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED AbilityNotReady | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName() +
                " | Cooldown=" +
                GetAbilityCooldown(selectedAbility) +
                " | UsesRemaining=" +
                GetAbilityUsesRemaining(selectedAbility),
                this
            );

            return false;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit][Attack] FAILED GridManager=NULL | " +
                "Unit=" + name,
                this
            );

            return false;
        }

        if (!selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target))
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED CanHit=false | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName() +
                " | Target=" + target.name,
                this
            );

            return false;
        }

        Debug.Log(
            "[AttackUnit][Attack] EXECUTE | " +
            "Unit=" + name +
            " | Team=" + GetTeam() +
            " | Ability=" +
            selectedAbility.GetAbilityName() +
            " | Target=" + target.name +
            " | PlayerInputLocked=" +
            CombatUtility.IsPlayerInputLocked(),
            this
        );

        if (!selectedAbility.Use(
                gameObject,
                target))
        {
            Debug.LogWarning(
                "[AttackUnit][Attack] FAILED Ability.Use | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName() +
                " | Target=" + target.name,
                this
            );

            return false;
        }

        CompleteAbilityUse(
            selectedAbility
        );

        Debug.Log(
            "[AttackUnit][Attack] SUCCESS | " +
            "Unit=" + name +
            " | Team=" + GetTeam() +
            " | Ability=" +
            selectedAbility.GetAbilityName() +
            " | Target=" + target.name,
            this
        );

        return true;
    }


    // ============================================================
    // PLAYER ATTACK
    // ============================================================

    public bool PlayerAttack(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
        {
            Debug.Log(
                "[AttackUnit][PlayerAttack] BLOCKED | " +
                "Unit=" + name +
                " | Team=" + GetTeam() +
                " | PlayerInputLocked=" +
                CombatUtility.IsPlayerInputLocked(),
                this
            );

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
        AbilitySO selectedAbility)
    {
        if (!CombatUtility.IsPlayerTurnInputAllowed(this))
        {
            Debug.Log(
                "[AttackUnit][AttackAtTile] BLOCKED | " +
                "Unit=" + name +
                " | Team=" + GetTeam() +
                " | PlayerInputLocked=" +
                CombatUtility.IsPlayerInputLocked(),
                this
            );

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

        if (!cachedGridManager.IsInsideGrid(
                targetTile))
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

        CompleteAbilityUse(
            selectedAbility
        );

        return true;
    }


    // ============================================================
    // COMPLETE ABILITY USE
    // ============================================================

    private void CompleteAbilityUse(
        AbilitySO ability)
    {
        bool usesExhausted =
            ConsumeAbilityUse(ability);

        if (usesExhausted)
        {
            StartAbilityCooldown(ability);
        }

        OnAbilityUsed?.Invoke(
            this,
            ability
        );
    }


    // ============================================================
    // ATTACK ROUTINE
    // ============================================================

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (!CanAttack())
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED CanAttack=false | " +
                "Unit=" + name +
                " | Team=" + GetTeam(),
                this
            );

            yield break;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED Target=NULL | " +
                "Unit=" + name,
                this
            );

            yield break;
        }

        if (selectedAbility == null)
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED Ability=NULL | " +
                "Unit=" + name,
                this
            );

            yield break;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED AbilityNotReady | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName(),
                this
            );

            yield break;
        }

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit][AttackRoutine] FAILED GridManager=NULL | " +
                "Unit=" + name,
                this
            );

            yield break;
        }

        if (!selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target))
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED CanHit=false | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName() +
                " | Target=" + target.name,
                this
            );

            yield break;
        }

        if (!selectedAbility.Use(
                gameObject,
                target))
        {
            Debug.LogWarning(
                "[AttackUnit][AttackRoutine] FAILED Ability.Use | " +
                "Unit=" + name +
                " | Ability=" +
                selectedAbility.GetAbilityName() +
                " | Target=" + target.name,
                this
            );

            yield break;
        }

        CompleteAbilityUse(
            selectedAbility
        );

        if (attackAnimation == null)
        {
            yield break;
        }

        attackAnimation.PlayAttackAnimation();

        yield return StartCoroutine(
            attackAnimation.WaitForAttackFinished()
        );
    }


    // ============================================================
    // GENERIC TARGET CHECK
    // ============================================================

    public bool IsValidTarget(
        GameObject target)
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

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            if (abilities[i] != null)
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
    // GRID POSITION DEBUG
    // ============================================================

    [ContextMenu("Debug Grid Position")]
    public void DebugGridPosition()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return;
        }
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

        if (!cachedGridManager.IsInsideGrid(
                gridPosition))
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
    // CURRENT GRID POSITION
    // ============================================================

    public Vector2Int GetCurrentGridPosition()
    {
        Vector2Int position =
            GetLogicalGridPosition();

        return position;
    }


    // ============================================================
    // CURRENT WORLD-DETECTED POSITION
    // ============================================================

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
            AbilitySO ability =
                abilities[i];

            if (
                ability != null &&
                IsAbilityReady(ability)
            )
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        ability.GetRange()
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
            AbilitySO ability =
                abilities[i];

            if (ability != null)
            {
                maxRange =
                    Mathf.Max(
                        maxRange,
                        ability.GetRange()
                    );
            }
        }

        return maxRange;
    }


    // ============================================================
    // ABILITIES
    // ============================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }


    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    public void AddAbility(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        if (!abilities.Contains(ability))
        {
            abilities.Add(ability);
        }

        abilityCooldowns[ability] =
            0;

        abilityUsesRemaining[ability] =
            ability.GetUsesPerTurn();
    }


    public void RemoveAbility(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return;
        }

        abilities.Remove(ability);

        abilityCooldowns.Remove(
            ability
        );

        abilityUsesRemaining.Remove(
            ability
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


    public HealthManager GetHealthManager()
    {
        return healthManager;
    }


    public CharacterSO GetCharacterData()
    {
        return characterData;
    }
}