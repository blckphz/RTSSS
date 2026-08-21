using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackUnit : MonoBehaviour
{
    // ==================================================
    // STATIC EVENTS
    // ==================================================

    public static event Action<AttackUnit, AbilitySO>
        OnAbilityUsed;


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
    private List<AbilitySO> abilities =
        new List<AbilitySO>();


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField]
    private HealthManager healthManager;


    // ==================================================
    // ABILITY STATE
    // ==================================================

    private readonly Dictionary<AbilitySO, int>
        abilityCooldowns =
        new Dictionary<AbilitySO, int>();

    private readonly Dictionary<AbilitySO, int>
        abilityUsesRemaining =
        new Dictionary<AbilitySO, int>();


    // ==================================================
    // GRID
    // ==================================================

    private GridManager cachedGridManager;


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

    public void Initialize(
        CharacterSO data)
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

            abilityCooldowns[ability] = 0;

            /*
             * 0 = unlimited uses.
             *
             * Otherwise this is the number
             * of uses available this turn.
             */
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
        if (ability == null)
        {
            return false;
        }

        /*
         * Do NOT automatically add the ability.
         *
         * Ability registration is controlled by:
         *
         * - Initialize()
         * - AddAbility()
         *
         * This method only initializes state for
         * an already registered ability.
         */
        if (!abilities.Contains(ability))
        {
            return false;
        }

        if (!abilityCooldowns.ContainsKey(ability))
        {
            abilityCooldowns[ability] = 0;
        }

        if (!abilityUsesRemaining.ContainsKey(ability))
        {
            abilityUsesRemaining[ability] =
                ability.GetUsesPerTurn();
        }

        return true;
    }


    // ==================================================
    // MOVEMENT STATE
    // ==================================================

    public bool HasMovedThisTurn()
    {
        UnitMoveBrain moveBrain =
            GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            return false;
        }

        return moveBrain.HasConsumedMovement();
    }


    public void SetHasMovedThisTurn(
        bool value)
    {
        /*
         * Intentionally unused.
         *
         * UnitMoveBrain owns movement state.
         */
    }


    // ==================================================
    // COOLDOWN
    // ==================================================

    public int GetAbilityCooldown(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return -1;
        }

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

        /*
         * 0 = unlimited.
         */
        if (ability.GetUsesPerTurn() <= 0)
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
            : ability.GetUsesPerTurn();
    }


    public bool HasAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        /*
         * 0 = unlimited.
         */
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

        /*
         * 0 = unlimited.
         *
         * Unlimited-use abilities do not have a
         * use limit, so the cooldown starts after
         * every successful attack.
         */
        if (ability.GetUsesPerTurn() <= 0)
        {
            return true;
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

        /*
         * TRUE means the ability has now consumed
         * its final allowed use for this turn.
         */
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

        UnitMoveBrain moveBrain =
            GetComponent<UnitMoveBrain>();

        /*
         * No movement brain means there is
         * nothing to restrict.
         */
        if (moveBrain == null)
        {
            return true;
        }

        /*
         * Unit has not moved.
         */
        if (!moveBrain.HasConsumedMovement())
        {
            return true;
        }

        /*
         * Unit moved, so the ability itself decides
         * whether it can be used after movement.
         */
        return
            ability.CanAttackWithThisAfterMove();
    }


    // ==================================================
    // READY
    // ==================================================

    public bool IsAbilityReady(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }

        if (!abilities.Contains(ability))
        {
            return false;
        }

        if (!EnsureAbilityState(ability))
        {
            return false;
        }

        /*
         * Cooldown blocks the ability.
         */
        if (GetAbilityCooldown(ability) > 0)
        {
            return false;
        }

        /*
         * Uses per turn block the ability only
         * after all allowed uses have been consumed.
         */
        if (!HasAbilityUsesRemaining(ability))
        {
            return false;
        }

        /*
         * Movement restriction.
         */
        if (!CanUseAbilityAfterMovement(ability))
        {
            return false;
        }

        return true;
    }


    // ==================================================
    // DEBUG READY STATE
    // ==================================================

    public string GetAbilityReadyFailureReason(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return "Ability is null.";
        }

        if (!abilities.Contains(ability))
        {
            return
                "Ability is not registered on this AttackUnit.";
        }

        if (!EnsureAbilityState(ability))
        {
            return
                "Ability state could not be initialized.";
        }

        int cooldown =
            GetAbilityCooldown(ability);

        if (cooldown > 0)
        {
            return
                "Ability is on cooldown: " +
                cooldown;
        }

        int usesRemaining =
            GetAbilityUsesRemaining(ability);

        /*
         * 0 uses per turn means unlimited.
         */
        if (
            ability.GetUsesPerTurn() > 0 &&
            usesRemaining <= 0
        )
        {
            return
                "No uses remaining. Uses remaining: " +
                usesRemaining;
        }

        UnitMoveBrain moveBrain =
            GetComponent<UnitMoveBrain>();

        if (moveBrain != null)
        {
            bool moved =
                moveBrain.HasConsumedMovement();

            if (
                moved &&
                !ability.CanAttackWithThisAfterMove()
            )
            {
                return
                    "Unit has moved and this ability " +
                    "cannot be used after movement.";
            }
        }

        return "Ability is ready.";
    }


    // ==================================================
    // PUBLIC ABILITY CHECK
    // ==================================================

    public bool CanUseAbility(
        AbilitySO ability)
    {
        return IsAbilityReady(ability);
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

            /*
             * Reduce cooldown by one round.
             */
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

            /*
             * Reset uses per turn.
             *
             * 0 = unlimited.
             */
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
        if (ability == null)
        {
            return;
        }

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

        if (!abilities.Contains(selectedAbility))
        {
            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.Log(
                "[AttackUnit] Ability '" +
                selectedAbility.GetAbilityName() +
                "' is not ready.\n" +
                "Reason: " +
                GetAbilityReadyFailureReason(
                    selectedAbility
                ) +
                "\nUses remaining: " +
                GetAbilityUsesRemaining(
                    selectedAbility
                ) +
                "\nCooldown: " +
                GetAbilityCooldown(
                    selectedAbility
                ),
                this
            );

            return false;
        }

        /*
         * IMPORTANT:
         *
         * Do NOT call IsValidTarget() here.
         *
         * IsValidTarget() assumes a traditional
         * hostile attack and rejects same-team targets.
         *
         * That would prevent:
         *
         * - Healing allies
         * - Buffing allies
         * - Supporting allies
         * - Other friendly-target abilities
         *
         * AbilitySO is responsible for deciding
         * whether this specific target is valid.
         */

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            return false;
        }

        /*
         * Ability-specific target validation.
         */
        bool canHit =
            selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            );

        if (!canHit)
        {
            return false;
        }

        /*
         * Execute ability.
         */
        bool success =
            selectedAbility.Use(
                gameObject,
                target
            );

        if (!success)
        {
            return false;
        }

        // ==================================================
        // CONSUME ONE USE
        // ==================================================

        bool usesExhausted =
            ConsumeAbilityUse(
                selectedAbility
            );


        // ==================================================
        // START COOLDOWN
        // ==================================================

        if (usesExhausted)
        {
            StartAbilityCooldown(
                selectedAbility
            );
        }


        // ==================================================
        // EVENT
        // ==================================================

        OnAbilityUsed?.Invoke(
            this,
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
        if (!CanAttack())
        {
            return false;
        }

        if (selectedAbility == null)
        {
            return false;
        }

        if (!abilities.Contains(selectedAbility))
        {
            return false;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.Log(
                "[AttackUnit] Ability '" +
                selectedAbility.GetAbilityName() +
                "' is not ready.\n" +
                "Reason: " +
                GetAbilityReadyFailureReason(
                    selectedAbility
                ) +
                "\nUses remaining: " +
                GetAbilityUsesRemaining(
                    selectedAbility
                ) +
                "\nCooldown: " +
                GetAbilityCooldown(
                    selectedAbility
                ),
                this
            );

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

        /*
         * Tile abilities are also controlled by AbilitySO.
         */
        bool canHit =
            selectedAbility.CanHitTile(
                cachedGridManager,
                gameObject,
                targetTile
            );

        if (!canHit)
        {
            return false;
        }

        /*
         * Execute tile ability.
         */
        bool success =
            selectedAbility.UseAtTile(
                gameObject,
                cachedGridManager,
                targetTile
            );

        if (!success)
        {
            return false;
        }

        // ==================================================
        // CONSUME ONE USE
        // ==================================================

        bool usesExhausted =
            ConsumeAbilityUse(
                selectedAbility
            );


        // ==================================================
        // START COOLDOWN
        // ==================================================

        if (usesExhausted)
        {
            StartAbilityCooldown(
                selectedAbility
            );
        }


        // ==================================================
        // EVENT
        // ==================================================

        OnAbilityUsed?.Invoke(
            this,
            selectedAbility
        );

        return true;
    }


    // ==================================================
    // ATTACK ROUTINE
    // ==================================================

    public IEnumerator AttackRoutine(
        GameObject target,
        AbilitySO selectedAbility)
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

        if (!abilities.Contains(selectedAbility))
        {
            yield break;
        }

        if (!IsAbilityReady(selectedAbility))
        {
            Debug.Log(
                "[AttackUnit] Ability '" +
                selectedAbility.GetAbilityName() +
                "' is not ready. Reason: " +
                GetAbilityReadyFailureReason(
                    selectedAbility
                ),
                this
            );

            yield break;
        }

        /*
         * IMPORTANT:
         *
         * Do NOT use IsValidTarget() here.
         *
         * AbilitySO.CanHit() determines whether
         * this particular ability can affect the target.
         *
         * This allows healing abilities to target
         * allies while offensive abilities can still
         * reject allies.
         */

        EnsureGridManager();

        if (cachedGridManager == null)
        {
            yield break;
        }

        bool canHit =
            selectedAbility.CanHit(
                cachedGridManager,
                gameObject,
                target
            );

        if (!canHit)
        {
            yield break;
        }

        /*
         * Execute ability.
         */
        bool success =
            selectedAbility.Use(
                gameObject,
                target
            );

        if (!success)
        {
            yield break;
        }

        // ==================================================
        // CONSUME ONE USE
        // ==================================================

        bool usesExhausted =
            ConsumeAbilityUse(
                selectedAbility
            );


        // ==================================================
        // START COOLDOWN
        // ==================================================

        if (usesExhausted)
        {
            StartAbilityCooldown(
                selectedAbility
            );
        }


        // ==================================================
        // EVENT
        // ==================================================

        OnAbilityUsed?.Invoke(
            this,
            selectedAbility
        );


        // ==================================================
        // ATTACK ANIMATION
        // ==================================================

        IAttackAnimation attackAnimation =
            GetComponent<IAttackAnimation>();

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

    /*
     * This method is intentionally kept for other systems
     * that need to ask:
     *
     * "Is this a normal hostile attack target?"
     *
     * It is NOT used by Attack() or AttackRoutine().
     *
     * Therefore healing/support abilities can still target
     * friendly units through AbilitySO.CanHit().
     */
    public bool IsValidTarget(
        GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (target == gameObject)
        {
            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            return false;
        }

        if (targetHealth.IsDead())
        {
            return false;
        }

        if (healthManager == null)
        {
            return false;
        }

        if (
            targetHealth.GetTeam() ==
            healthManager.GetTeam()
        )
        {
            return false;
        }

        return true;
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
    // GRID
    // ==================================================

    public GridManager GetGridManager()
    {
        EnsureGridManager();

        return cachedGridManager;
    }


    private void EnsureGridManager()
    {
        if (cachedGridManager == null)
        {
            cachedGridManager =
                FindFirstObjectByType<GridManager>();
        }
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

        abilityCooldowns[ability] = 0;

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

        abilityCooldowns.Remove(ability);

        abilityUsesRemaining.Remove(ability);
    }


    // ==================================================
    // ACCESSORS
    // ==================================================

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
}