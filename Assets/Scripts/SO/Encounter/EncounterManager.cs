using System;
using System.Collections;
using UnityEngine;

public class EncounterManager : MonoBehaviour
{
    // ============================================================
    // EVENTS
    // ============================================================

    public static event Action<EncounterDefinition>
        OnEncounterVictory;


    // ============================================================
    // ENCOUNTER STATE
    // ============================================================

    public enum EncounterState
    {
        None,
        Preparing,
        CreatingGrid,
        SpawningUnits,
        StartingCombat,
        Combat,
        Victory,
        Defeat
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private EncounterSpawner encounterSpawner;

    [SerializeField]
    private RoundManager roundManager;

    [SerializeField]
    private CombatManager combatManager;


    // ============================================================
    // CURRENT ENCOUNTER
    // ============================================================

    [Header("Current Encounter")]
    [SerializeField]
    private EncounterDefinition currentEncounter;


    // ============================================================
    // TIMING
    // ============================================================

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float gridSpawnDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float unitSpawnDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float combatStartDelay = 0.25f;


    // ============================================================
    // STATE
    // ============================================================

    private EncounterState currentState =
        EncounterState.None;

    private bool encounterRunning;

    private bool firstRoundStarted;


    // ============================================================
    // ACCESSORS
    // ============================================================

    public EncounterState CurrentState =>
        currentState;

    public EncounterDefinition CurrentEncounter =>
        currentEncounter;

    public VictoryCondition CurrentVictoryCondition
    {
        get
        {
            if (currentEncounter == null)
            {
                return VictoryCondition.DefeatAllEnemies;
            }

            return currentEncounter.victoryCondition;
        }
    }

    public string TargetEnemyId
    {
        get
        {
            if (currentEncounter == null)
            {
                return string.Empty;
            }

            return currentEncounter.targetEnemyId;
        }
    }

    public int RoundsToSurvive
    {
        get
        {
            if (currentEncounter == null)
            {
                return 1;
            }

            return Mathf.Max(
                1,
                currentEncounter.roundsToSurvive
            );
        }
    }


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindDependencies();
    }

    private void OnEnable()
    {
        HealthManager.OnHealthChanged +=
            HandleHealthChanged;
    }

    private void OnDisable()
    {
        HealthManager.OnHealthChanged -=
            HandleHealthChanged;
    }


    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private void FindDependencies()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (encounterSpawner == null)
        {
            encounterSpawner =
                FindFirstObjectByType<EncounterSpawner>();
        }

        if (roundManager == null)
        {
            roundManager =
                FindFirstObjectByType<RoundManager>();
        }

        if (combatManager == null)
        {
            combatManager =
                FindFirstObjectByType<CombatManager>();
        }
    }


    // ============================================================
    // START ENCOUNTER
    // ============================================================

    public void StartEncounter()
    {
        if (encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] StartEncounter ignored. " +
                "Encounter is already running.",
                this
            );

            return;
        }

        if (currentEncounter == null)
        {
            Debug.LogError(
                "[EncounterManager] Cannot start encounter. " +
                "No EncounterDefinition is assigned.",
                this
            );

            return;
        }

        if (!ValidateEncounterDefinition())
        {
            return;
        }

        StartCoroutine(
            StartEncounterRoutine()
        );
    }


    // ============================================================
    // VALIDATE ENCOUNTER DEFINITION
    // ============================================================

    private bool ValidateEncounterDefinition()
    {
        if (currentEncounter == null)
        {
            return false;
        }

        // --------------------------------------------------------
        // SURVIVAL
        // --------------------------------------------------------

        if (
            currentEncounter.victoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            if (currentEncounter.roundsToSurvive < 1)
            {
                Debug.LogError(
                    "[EncounterManager] Survival mission requires " +
                    "at least 1 round.",
                    this
                );

                return false;
            }
        }


        // --------------------------------------------------------
        // SPECIFIC TARGET
        // --------------------------------------------------------

        if (
            currentEncounter.victoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    currentEncounter.targetEnemyId
                )
            )
            {
                Debug.LogError(
                    "[EncounterManager] Specific-target mission " +
                    "has no Target Enemy ID.",
                    this
                );

                return false;
            }
        }

        return true;
    }


    // ============================================================
    // ENCOUNTER PREPARATION
    // ============================================================

    private IEnumerator StartEncounterRoutine()
    {
        encounterRunning = true;

        firstRoundStarted = false;

        SetEncounterState(
            EncounterState.Preparing
        );

        Debug.Log(
            "[EncounterManager] Encounter preparation started.\n" +
            "Encounter=" +
            currentEncounter.encounterName +
            "\nCondition=" +
            currentEncounter.victoryCondition +
            "\nSurvivalRounds=" +
            currentEncounter.roundsToSurvive +
            "\nTarget=" +
            currentEncounter.targetEnemyId,
            this
        );


        // --------------------------------------------------------
        // VALIDATE DEPENDENCIES
        // --------------------------------------------------------

        if (!ValidateDependencies())
        {
            encounterRunning = false;

            SetEncounterState(
                EncounterState.None
            );

            yield break;
        }


        // --------------------------------------------------------
        // CLEAR PREVIOUS ENCOUNTER
        // --------------------------------------------------------

        ClearPreviousEncounter();

        yield return null;


        // --------------------------------------------------------
        // CREATE GRID
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.CreatingGrid
        );

        SetupGrid();

        if (gridSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                gridSpawnDelay
            );
        }


        // --------------------------------------------------------
        // INITIAL UNIT SPAWN
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.SpawningUnits
        );

        if (encounterSpawner == null)
        {
            Debug.LogError(
                "[EncounterManager] EncounterSpawner is missing.",
                this
            );

            encounterRunning = false;

            SetEncounterState(
                EncounterState.None
            );

            yield break;
        }

        encounterSpawner.SpawnEncounter(
            currentEncounter
        );

        if (unitSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                unitSpawnDelay
            );
        }


        // --------------------------------------------------------
        // PREPARATION COMPLETE
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.Preparing
        );

        Debug.Log(
            "[EncounterManager] Encounter ready. " +
            "Waiting for first round.",
            this
        );
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool ValidateDependencies()
    {
        bool valid = true;

        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterManager] GridManager is missing.",
                this
            );

            valid = false;
        }

        if (encounterSpawner == null)
        {
            Debug.LogError(
                "[EncounterManager] EncounterSpawner is missing.",
                this
            );

            valid = false;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing.",
                this
            );

            valid = false;
        }

        if (gameStateManager == null)
        {
            Debug.LogWarning(
                "[EncounterManager] GameStateManager is missing.",
                this
            );
        }

        if (combatManager == null)
        {
            Debug.LogWarning(
                "[EncounterManager] CombatManager is missing.",
                this
            );
        }

        return valid;
    }


    // ============================================================
    // GRID
    // ============================================================

    private void SetupGrid()
    {
        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterManager] GridManager is missing.",
                this
            );

            return;
        }

        if (currentEncounter == null)
        {
            Debug.LogError(
                "[EncounterManager] Current encounter is NULL.",
                this
            );

            return;
        }

        gridManager.SetGridShape(
            currentEncounter.shape,
            currentEncounter.width,
            currentEncounter.height,
            currentEncounter.minRadius,
            currentEncounter.maxRadius,
            true
        );
    }


    // ============================================================
    // CLEAR PREVIOUS ENCOUNTER
    // ============================================================

    private void ClearPreviousEncounter()
    {
        ClearUnits();

        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }
    }

    private void ClearUnits()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (gridManager != null)
            {
                gridManager.RemoveUnit(
                    unit.gameObject
                );
            }

            Destroy(
                unit.gameObject
            );
        }
    }


    // ============================================================
    // SURVIVAL WAVE SPAWNING
    // ============================================================

    public bool ShouldSpawnNextRound()
    {
        if (!encounterRunning)
        {
            return false;
        }

        return UsesSurvival();
    }

    public void SpawnNextRoundEnemies()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (!UsesSurvival())
        {
            return;
        }

        if (currentEncounter == null)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot spawn next-round enemies. " +
                "Current EncounterDefinition is NULL.",
                this
            );

            return;
        }

        if (encounterSpawner == null)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot spawn next-round enemies. " +
                "EncounterSpawner is missing.",
                this
            );

            return;
        }

        SetEncounterState(
            EncounterState.SpawningUnits
        );

        encounterSpawner.SpawnEncounter(
            currentEncounter
        );

        SetEncounterState(
            EncounterState.Combat
        );

        Debug.Log(
            "[EncounterManager] SURVIVAL WAVE SPAWNED | " +
            "Round=" +
            (
                roundManager != null
                    ? roundManager.GetCurrentRound()
                    : -1
            ) +
            " | Required=" +
            RoundsToSurvive,
            this
        );
    }


    // ============================================================
    // NEXT ROUND
    // ============================================================

    public void NextRound()
    {
        if (!encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] NextRound ignored. " +
                "Encounter is not running.",
                this
            );

            return;
        }

        if (currentState == EncounterState.Preparing)
        {
            BeginFirstRound();

            return;
        }

        if (currentState == EncounterState.Combat)
        {
            StartNextRound();

            return;
        }

        Debug.LogWarning(
            "[EncounterManager] NextRound ignored. " +
            "State=" +
            currentState,
            this
        );
    }


    // ============================================================
    // FIRST ROUND
    // ============================================================

    private void BeginFirstRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (firstRoundStarted)
        {
            Debug.LogWarning(
                "[EncounterManager] First round already started.",
                this
            );

            return;
        }

        if (currentState != EncounterState.Preparing)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot start first round. " +
                "State=" +
                currentState,
                this
            );

            return;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // VERIFY PLAYER
        // --------------------------------------------------------

        AttackUnit[] players =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        bool playerFound = false;

        for (int i = 0; i < players.Length; i++)
        {
            AttackUnit unit =
                players[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() == Team.Player)
            {
                playerFound = true;
                break;
            }
        }

        if (!playerFound)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot start combat. " +
                "No Team.Player unit exists.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // START COMBAT
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.StartingCombat
        );

        if (gameStateManager != null)
        {
            gameStateManager.EncounterStarted();
        }

        if (combatStartDelay <= 0f)
        {
            StartCombatRound();
        }
        else
        {
            StartCoroutine(
                StartCombatAfterDelay()
            );
        }
    }


    // ============================================================
    // START COMBAT AFTER DELAY
    // ============================================================

    private IEnumerator StartCombatAfterDelay()
    {
        yield return new WaitForSeconds(
            combatStartDelay
        );

        if (!encounterRunning)
        {
            yield break;
        }

        if (currentState != EncounterState.StartingCombat)
        {
            yield break;
        }

        StartCombatRound();
    }


    // ============================================================
    // START COMBAT ROUND
    // ============================================================

    private void StartCombatRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing.",
                this
            );

            return;
        }

        firstRoundStarted = true;

        SetEncounterState(
            EncounterState.Combat
        );

        roundManager.StartRound();
    }


    // ============================================================
    // NEXT NORMAL ROUND
    // ============================================================

    private void StartNextRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing.",
                this
            );

            return;
        }

        if (roundManager.IsRoundRunning())
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot start next round. " +
                "Current round is still running.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // FINAL SAFETY CHECK
        // --------------------------------------------------------

        if (CheckVictoryConditions())
        {
            return;
        }

        roundManager.StartRound();
    }


    // ============================================================
    // UNIT KILLED
    // ============================================================

    public void HandleUnitKilled(
        HealthManager killedUnit,
        string encounterUnitId)
    {
        if (!encounterRunning)
        {
            return;
        }

        if (killedUnit == null)
        {
            return;
        }


        // --------------------------------------------------------
        // PLAYER DEATH
        // --------------------------------------------------------

        if (killedUnit.GetTeam() != Team.Enemy)
        {
            CheckPlayerDefeat();

            return;
        }


        // --------------------------------------------------------
        // SPECIFIC TARGET
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            if (
                !string.IsNullOrWhiteSpace(
                    encounterUnitId
                ) &&
                encounterUnitId ==
                TargetEnemyId
            )
            {
                EncounterVictory();

                return;
            }

            return;
        }


        // --------------------------------------------------------
        // DEFEAT ALL ENEMIES
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();

                return;
            }
        }
    }


    // ============================================================
    // HEALTH CHANGE
    // ============================================================

    private void HandleHealthChanged(
        HealthManager healthManager)
    {
        if (!encounterRunning)
        {
            return;
        }

        if (
            currentState != EncounterState.Combat &&
            currentState != EncounterState.StartingCombat
        )
        {
            return;
        }

        if (healthManager == null)
        {
            return;
        }


        // --------------------------------------------------------
        // PLAYER DEFEAT
        // --------------------------------------------------------

        if (healthManager.GetTeam() == Team.Player)
        {
            CheckPlayerDefeat();

            return;
        }


        // --------------------------------------------------------
        // ENEMY
        // --------------------------------------------------------

        if (healthManager.GetTeam() != Team.Enemy)
        {
            return;
        }

        if (!healthManager.IsAlive())
        {
            return;
        }


        // --------------------------------------------------------
        // SURVIVAL MISSIONS
        // --------------------------------------------------------

        if (UsesSurvival())
        {
            return;
        }


        // --------------------------------------------------------
        // NORMAL DEFEAT-ALL
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            CheckVictoryConditions();
        }
    }


    // ============================================================
    // PLAYER DEFEAT
    // ============================================================

    private void CheckPlayerDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() != Team.Player)
            {
                continue;
            }

            HealthManager health =
                unit.GetComponent<HealthManager>();

            if (
                health != null &&
                health.IsAlive()
            )
            {
                return;
            }
        }

        Debug.Log(
            "[EncounterManager] PLAYER DEFEATED.",
            this
        );

        EncounterDefeat();
    }


    // ============================================================
    // VICTORY CONDITION CHECK
    // ============================================================

    private bool CheckVictoryConditions()
    {
        if (!encounterRunning)
        {
            return false;
        }

        if (currentEncounter == null)
        {
            return false;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] Victory check failed. " +
                "RoundManager is NULL.",
                this
            );

            return false;
        }


        // --------------------------------------------------------
        // SURVIVE ROUNDS
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            int currentRound =
                roundManager.GetCurrentRound();

            bool survived =
                currentRound >= RoundsToSurvive;

            Debug.Log(
                "[EncounterManager] SURVIVAL CHECK | " +
                "Round=" +
                currentRound +
                " | Required=" +
                RoundsToSurvive +
                " | Result=" +
                survived,
                this
            );

            if (survived)
            {
                EncounterVictory();

                return true;
            }

            return false;
        }


        // --------------------------------------------------------
        // DEFEAT ALL ENEMIES
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();

                return true;
            }

            return false;
        }


        // --------------------------------------------------------
        // SPECIFIC TARGET
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            if (IsTargetEnemyDead())
            {
                EncounterVictory();

                return true;
            }

            return false;
        }

        return false;
    }


    // ============================================================
    // TARGET ENEMY DEAD
    // ============================================================

    private bool IsTargetEnemyDead()
    {
        if (!UsesSpecificEnemyTarget())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetEnemyId))
        {
            return false;
        }

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        bool targetWasFound = false;

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() != Team.Enemy)
            {
                continue;
            }

            EncounterUnit encounterUnit =
                unit.GetComponent<EncounterUnit>();

            if (encounterUnit == null)
            {
                continue;
            }

            if (
                !encounterUnit.HasEncounterUnitId(
                    TargetEnemyId
                )
            )
            {
                continue;
            }

            targetWasFound = true;

            HealthManager health =
                unit.GetComponent<HealthManager>();

            if (
                health != null &&
                health.IsAlive()
            )
            {
                return false;
            }
        }

        if (targetWasFound)
        {
            return true;
        }

        return false;
    }


    // ============================================================
    // LIVING ENEMIES
    // ============================================================

    private bool HasLivingEnemies()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() != Team.Enemy)
            {
                continue;
            }

            HealthManager health =
                unit.GetComponent<HealthManager>();

            if (health == null)
            {
                continue;
            }

            if (health.IsAlive())
            {
                return true;
            }
        }

        return false;
    }


    // ============================================================
    // SURVIVAL
    // ============================================================

    private bool HasSurvivedRequiredRounds()
    {
        if (roundManager == null)
        {
            return false;
        }

        int currentRound =
            roundManager.GetCurrentRound();

        bool survived =
            currentRound >= RoundsToSurvive;

        Debug.Log(
            "[EncounterManager] SURVIVAL STATUS | " +
            "Round=" +
            currentRound +
            " | Required=" +
            RoundsToSurvive +
            " | Survived=" +
            survived,
            this
        );

        return survived;
    }


    // ============================================================
    // CHECK AFTER ROUND
    // ============================================================

    public void CheckVictoryAfterRound()
    {
        if (!encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] Post-round victory check skipped. " +
                "Encounter is no longer running.",
                this
            );

            return;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] Post-round victory check failed. " +
                "RoundManager is NULL.",
                this
            );

            return;
        }

        int currentRound =
            roundManager.GetCurrentRound();


        // --------------------------------------------------------
        // SURVIVAL
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            if (currentRound >= RoundsToSurvive)
            {
                Debug.Log(
                    "[EncounterManager] POST-ROUND SURVIVAL SUCCESS | " +
                    "Round " +
                    currentRound +
                    " reached required " +
                    RoundsToSurvive +
                    " rounds.",
                    this
                );

                EncounterVictory();

                return;
            }
        }


        // --------------------------------------------------------
        // OTHER VICTORY CONDITIONS
        // --------------------------------------------------------

        CheckVictoryConditions();
    }


    // ============================================================
    // VICTORY
    // ============================================================

    public void EncounterVictory()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (currentEncounter == null)
        {
            return;
        }


        // --------------------------------------------------------
        // DEFEAT ALL ENEMIES
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            if (HasLivingEnemies())
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory rejected. " +
                    "Living enemies remain.",
                    this
                );

                return;
            }
        }


        // --------------------------------------------------------
        // SURVIVE ROUNDS
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            if (!HasSurvivedRequiredRounds())
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory rejected. " +
                    "Required survival rounds have not been reached.",
                    this
                );

                return;
            }
        }


        // --------------------------------------------------------
        // SPECIFIC TARGET
        // --------------------------------------------------------

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            if (!IsTargetEnemyDead())
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory rejected. " +
                    "Target enemy is still alive.",
                    this
                );

                return;
            }
        }


        // --------------------------------------------------------
        // FINISH ENCOUNTER
        // --------------------------------------------------------

        encounterRunning = false;

        StopAllCoroutines();

        SetEncounterState(
            EncounterState.Victory
        );

        int finalRound =
            roundManager != null
                ? roundManager.GetCurrentRound()
                : -1;

        Debug.Log(
            "[EncounterManager] ========================================\n" +
            "[EncounterManager] VICTORY\n" +
            "Encounter=" +
            currentEncounter.encounterName +
            "\nCondition=" +
            CurrentVictoryCondition +
            "\nFinalRound=" +
            finalRound +
            "\nRequiredRounds=" +
            RoundsToSurvive +
            "\nTarget=" +
            TargetEnemyId +
            "\n========================================",
            this
        );

        if (gameStateManager != null)
        {
            gameStateManager.EncounterVictory();
        }

        OnEncounterVictory?.Invoke(
            currentEncounter
        );
    }


    // ============================================================
    // DEFEAT
    // ============================================================

    public void EncounterDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        encounterRunning = false;

        StopAllCoroutines();

        SetEncounterState(
            EncounterState.Defeat
        );

        Debug.Log(
            "[EncounterManager] ENCOUNTER DEFEAT.",
            this
        );

        if (gameStateManager != null)
        {
            gameStateManager.EncounterDefeat();
        }
    }


    // ============================================================
    // CONDITION HELPERS
    // ============================================================

    private bool UsesSpecificEnemyTarget()
    {
        return
            CurrentVictoryCondition ==
            VictoryCondition.DefeatSpecificEnemy;
    }

    private bool UsesSurvival()
    {
        return
            CurrentVictoryCondition ==
            VictoryCondition.SurviveRounds;
    }


    // ============================================================
    // STATE
    // ============================================================

    private void SetEncounterState(
        EncounterState newState)
    {
        currentState =
            newState;
    }


    // ============================================================
    // SET CURRENT ENCOUNTER
    // ============================================================

    public void SetCurrentEncounter(
        EncounterDefinition encounter)
    {
        if (encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot change encounter " +
                "while encounter is running.",
                this
            );

            return;
        }

        if (encounter == null)
        {
            Debug.LogWarning(
                "[EncounterManager] Assigned encounter is NULL.",
                this
            );
        }

        currentEncounter =
            encounter;
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

    public bool IsEncounterRunning()
    {
        return encounterRunning;
    }

    public bool IsPreparing()
    {
        return
            currentState ==
            EncounterState.Preparing;
    }

    public bool IsInCombat()
    {
        return
            currentState ==
            EncounterState.Combat;
    }

    public bool IsFinished()
    {
        return
            currentState ==
                EncounterState.Victory
            ||
            currentState ==
                EncounterState.Defeat;
    }

    public bool CanPressNextRound()
    {
        if (!encounterRunning)
        {
            return false;
        }

        if (currentState == EncounterState.Preparing)
        {
            return true;
        }

        if (currentState == EncounterState.Combat)
        {
            if (roundManager == null)
            {
                return false;
            }

            return
                !roundManager.IsRoundRunning();
        }

        return false;
    }
}