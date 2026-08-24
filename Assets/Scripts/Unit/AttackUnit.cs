using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    // ==================================================
    // STATIC EVENTS
    // ==================================================

    public static event Action<AttackUnit, AbilitySO> OnAbilityUsed;


    // ==================================================
    // CHARACTER DATA
    // ==================================================

    [Header("Character Data")]
    [SerializeField]
    private CharacterSO characterData;


    // ==================================================
    // ABILITIES
    // ==================================================

    [Header("Abilities")]
    [SerializeField]
    private List<AbilitySO> abilities = new();


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;

    private UnitMoveBrain moveBrain;
    private IAttackAnimation attackAnimation;


    // ==================================================
    // ABILITY STATE
    // ==================================================

    private readonly Dictionary<AbilitySO, int> abilityCooldowns =
        new();

    private readonly Dictionary<AbilitySO, int> abilityUsesRemaining =
        new();


    // ==================================================
    // GRID
    // ==================================================

    private GridManager cachedGridManager;

    /*
     * IMPORTANT:
     *
     * This is the UNIT'S LOGICAL GRID POSITION.
     *
     * It must NOT be recalculated from transform.position
     * after the board visually rotates.
     *
     * Example:
     *
     * Logical position:
     * (-2, 1)
     *
     * Board rotates 90 degrees.
     *
     * The unit may move in WORLD SPACE to another position,
     * but its logical grid position is STILL:
     *
     * (-2, 1)
     */
    [SerializeField]
    private Vector2Int logicalGridPosition;

    private bool hasLogicalGridPosition;


    // ==================================================
    // UNITY
    // ==================================================

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

        /*
         * Do not immediately overwrite an already assigned
         * logical position.
         */
        if (!hasLogicalGridPosition &&
            cachedGridManager != null)
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


    // ==================================================
    // INITIALIZE
    // ==================================================

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


    // ==================================================
    // INITIALIZE ABILITY STATE
    // ==================================================

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


    // ==================================================
    // ENSURE ABILITY STATE
    // ==================================================

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


    // ==================================================
    // LOGICAL GRID POSITION
    // ==================================================

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

        if (cachedGridManager != null)
        {
            Debug.Log(
                "[AttackUnit] LOGICAL GRID POSITION SET\n" +
                "Unit: " +
                gameObject.name +
                "\n" +
                "Logical Position: " +
                logicalGridPosition,
                this
            );
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


    // ==================================================
    // MOVEMENT STATE
    // ==================================================

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


    // ==================================================
    // COOLDOWN
    // ==================================================

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


    // ==================================================
    // USES PER TURN
    // ==================================================

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


    // ==================================================
    // CONSUME ABILITY USE
    // ==================================================

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


    // ==================================================
    // MOVEMENT / ABILITY RESTRICTION
    // ==================================================

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


    // ==================================================
    // READY
    // ==================================================

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


    // ==================================================
    // PUBLIC ABILITY CHECK
    // ==================================================

    public bool CanUseAbility(
        AbilitySO ability)
    {
        return
            IsAbilityReady(ability);
    }


    // ==================================================
    // ROUND
    // ==================================================

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


    // ==================================================
    // COOLDOWN START
    // ==================================================

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


    // ==================================================
    // ATTACK
    // ==================================================

    public bool Attack(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (
            !CanAttack() ||
            target == null ||
            selectedAbility == null
        )
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

        if (!selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target))
        {
            return false;
        }

        if (!selectedAbility.Use(
                gameObject,
                target))
        {
            return false;
        }

        CompleteAbilityUse(
            selectedAbility
        );

        return true;
    }


    // ==================================================
    // ATTACK AT TILE
    // ==================================================

    public bool AttackAtTile(
        Vector2Int targetTile,
        AbilitySO selectedAbility)
    {
        if (
            !CanAttack() ||
            selectedAbility == null
        )
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


    // ==================================================
    // COMPLETE ABILITY USE
    // ==================================================

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


    // ==================================================
    // ATTACK ROUTINE
    // ==================================================

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility)
    {
        if (
            !CanAttack() ||
            target == null ||
            selectedAbility == null
        )
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

        if (!selectedAbility.Use(
                gameObject,
                target))
        {
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


    // ==================================================
    // GENERIC TARGET CHECK
    // ==================================================

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


    // ==================================================
    // CAN ATTACK
    // ==================================================

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


    // ==================================================
    // DEAD
    // ==================================================

    public bool IsDead()
    {
        return
            healthManager == null ||
            healthManager.IsDead();
    }


    // ==================================================
    // GRID MANAGER
    // ==================================================

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

        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit] GridManager NOT FOUND!\n" +
                "Unit: " +
                gameObject.name,
                this
            );

            return;
        }

        Debug.Log(
            "[AttackUnit] GridManager FOUND.\n" +
            "Unit: " +
            gameObject.name +
            "\n" +
            "GridManager: " +
            cachedGridManager.gameObject.name +
            "\n" +
            "Grid Size: " +
            cachedGridManager.GetWidth() +
            " x " +
            cachedGridManager.GetHeight(),
            this
        );
    }


    // ==================================================
    // GRID POSITION DEBUG
    // ==================================================

    [ContextMenu("Debug Grid Position")]
    public void DebugGridPosition()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            Debug.LogError(
                "[AttackUnit] Cannot debug grid position.\n" +
                "GridManager is missing.\n" +
                "Unit: " +
                gameObject.name,
                this
            );

            return;
        }

        Vector3 worldPosition =
            transform.position;

        Vector2Int logicalPosition =
            GetLogicalGridPosition();

        Vector2Int worldDetectedPosition =
            cachedGridManager.WorldToGridPosition(
                worldPosition
            );

        bool insideGrid =
            cachedGridManager.IsInsideGrid(
                logicalPosition
            );

        Vector3 expectedWorldPosition =
            cachedGridManager.GridToWorldPosition(
                logicalPosition
            );

        float distanceFromLogicalTile =
            Vector3.Distance(
                worldPosition,
                expectedWorldPosition
            );

        GameObject registeredUnit =
            cachedGridManager.GetUnitAt(
                logicalPosition
            );

        bool isRegisteredHere =
            registeredUnit == gameObject;

        Debug.Log(
            "========================================\n" +
            "[AttackUnit] GRID POSITION DEBUG\n" +
            "========================================\n\n" +

            "UNIT\n" +
            "Name: " +
            gameObject.name +
            "\n\n" +

            "WORLD POSITION\n" +
            worldPosition +
            "\n\n" +

            "CACHED LOGICAL GRID POSITION\n" +
            logicalPosition +
            "\n\n" +

            "WORLD -> GRID DETECTED POSITION\n" +
            worldDetectedPosition +
            "\n\n" +

            "POSITION MATCHES\n" +
            (
                logicalPosition ==
                worldDetectedPosition
            ) +
            "\n\n" +

            "INSIDE GRID\n" +
            insideGrid +
            "\n\n" +

            "EXPECTED LOGICAL TILE CENTER\n" +
            expectedWorldPosition +
            "\n\n" +

            "DISTANCE FROM LOGICAL TILE CENTER\n" +
            distanceFromLogicalTile +
            "\n\n" +

            "REGISTERED UNIT AT LOGICAL TILE\n" +
            (
                registeredUnit == null
                    ? "NONE"
                    : registeredUnit.name
            ) +
            "\n\n" +

            "IS THIS UNIT REGISTERED HERE\n" +
            isRegisteredHere +
            "\n\n" +

            "GRID SIZE\n" +
            cachedGridManager.GetWidth() +
            " x " +
            cachedGridManager.GetHeight() +
            "\n\n" +

            "GRID X RANGE\n" +
            cachedGridManager.GetMinX() +
            " -> " +
            cachedGridManager.GetMaxX() +
            "\n\n" +

            "GRID Y RANGE\n" +
            cachedGridManager.GetMinY() +
            " -> " +
            cachedGridManager.GetMaxY() +
            "\n\n" +

            "========================================",
            this
        );
    }


    // ==================================================
    // SNAP TO GRID
    // ==================================================

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return;
        }

        Vector3 beforePosition =
            transform.position;

        /*
         * IMPORTANT:
         *
         * Snap uses the logical grid position.
         *
         * It does NOT redefine the logical position from the
         * rotated world position.
         */
        Vector2Int gridPosition =
            GetLogicalGridPosition();

        Debug.Log(
            "[AttackUnit] SNAP START\n" +
            "Unit: " +
            gameObject.name +
            "\n" +
            "World Position Before: " +
            beforePosition +
            "\n" +
            "Logical Grid Position: " +
            gridPosition,
            this
        );

        if (!cachedGridManager.IsInsideGrid(
                gridPosition))
        {
            Debug.LogWarning(
                "[AttackUnit] SNAP CANCELLED.\n" +
                "Logical position is OUTSIDE grid.\n" +
                "Unit: " +
                gameObject.name +
                "\n" +
                "Grid Position: " +
                gridPosition +
                "\n" +
                "Valid X: " +
                cachedGridManager.GetMinX() +
                " -> " +
                cachedGridManager.GetMaxX() +
                "\n" +
                "Valid Y: " +
                cachedGridManager.GetMinY() +
                " -> " +
                cachedGridManager.GetMaxY(),
                this
            );

            return;
        }

        Vector3 targetPosition =
            cachedGridManager.GridToWorldPosition(
                gridPosition
            );

        transform.position =
            targetPosition;

        Vector3 afterPosition =
            transform.position;

        Debug.Log(
            "[AttackUnit] SNAP COMPLETE\n" +
            "========================================\n" +
            "Unit: " +
            gameObject.name +
            "\n\n" +

            "Logical Grid Position:\n" +
            gridPosition +
            "\n\n" +

            "Before:\n" +
            beforePosition +
            "\n\n" +

            "After:\n" +
            afterPosition +
            "\n\n" +

            "Expected Tile Center:\n" +
            targetPosition +
            "\n\n" +

            "Distance From Expected Center:\n" +
            Vector3.Distance(
                afterPosition,
                targetPosition
            ) +
            "\n\n" +

            "========================================",
            this
        );
    }


    // ==================================================
    // CURRENT GRID POSITION
    // ==================================================

    public Vector2Int GetCurrentGridPosition()
    {
        /*
         * DO NOT DO THIS:
         *
         * WorldToGridPosition(transform.position)
         *
         * after board rotation.
         *
         * The transform has been visually rotated/moved while
         * the logical board coordinate remains unchanged.
         */
        Vector2Int position =
            GetLogicalGridPosition();

        return position;
    }


    // ==================================================
    // CURRENT WORLD-DETECTED POSITION
    // ==================================================

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


    // ==================================================
    // RANGE
    // ==================================================

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


    // ==================================================
    // ABILITIES
    // ==================================================

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


    // ==================================================
    // ACCESSORS
    // ==================================================

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