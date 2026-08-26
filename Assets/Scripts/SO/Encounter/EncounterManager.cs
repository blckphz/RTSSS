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
    // MISSION TYPE
    // ============================================================

    public enum VictoryCondition
    {
        DefeatAllEnemies,
        SurviveRounds,
        DefeatAllEnemiesOrSurviveRounds,
        DefeatSpecificEnemy,
        DefeatSpecificEnemyOrSurviveRounds
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
    // VICTORY
    // ============================================================

    [Header("Victory Condition")]
    [SerializeField]
    private VictoryCondition victoryCondition =
        VictoryCondition.DefeatAllEnemies;


    [Header("Specific Enemy Target")]
    [Tooltip(
        "EncounterUnit ID that must be killed when using " +
        "DefeatSpecificEnemy or DefeatSpecificEnemyOrSurviveRounds."
    )]
    [SerializeField]
    private string targetEnemyId;


    [Header("Survival")]
    [SerializeField, Min(1)]
    private int roundsToSurvive = 5;


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


    public VictoryCondition CurrentVictoryCondition =>
        victoryCondition;


    public string TargetEnemyId =>
        targetEnemyId;


    public int RoundsToSurvive =>
        roundsToSurvive;


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
                "[EncounterManager] Encounter is already running.",
                this
            );

            return;
        }


        if (currentEncounter == null)
        {
            Debug.LogError(
                "[EncounterManager] No EncounterDefinition assigned!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // LOAD MISSION SETTINGS FROM DEFINITION
        // --------------------------------------------------------

        LoadMissionFromDefinition();


        StartCoroutine(
            StartEncounterRoutine()
        );
    }


    // ============================================================
    // LOAD MISSION FROM DEFINITION
    // ============================================================

    private void LoadMissionFromDefinition()
    {
        if (currentEncounter == null)
        {
            return;
        }


        victoryCondition =
            currentEncounter.victoryCondition;


        targetEnemyId =
            currentEncounter.targetEnemyId;


        roundsToSurvive =
            Mathf.Max(
                1,
                currentEncounter.roundsToSurvive
            );


        Debug.Log(
            "[EncounterManager] Mission loaded from definition.\n" +
            "Condition=" +
            victoryCondition +
            "\nTargetEnemyId=" +
            targetEnemyId +
            "\nRoundsToSurvive=" +
            roundsToSurvive,
            this
        );
    }


    // ============================================================
    // ENCOUNTER PREPARATION
    // ============================================================

    private IEnumerator StartEncounterRoutine()
    {
        encounterRunning = true;

        firstRoundStarted = false;


        // --------------------------------------------------------
        // PREPARING
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.Preparing
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] STARTING ENCOUNTER",
            this
        );


        Debug.Log(
            "[EncounterManager] Encounter: " +
            currentEncounter.encounterName,
            this
        );


        Debug.Log(
            "[EncounterManager] Victory Condition: " +
            victoryCondition,
            this
        );


        if (UsesSpecificEnemyTarget())
        {
            Debug.Log(
                "[EncounterManager] Target Enemy ID: " +
                targetEnemyId,
                this
            );
        }


        if (UsesSurvival())
        {
            Debug.Log(
                "[EncounterManager] Rounds to survive: " +
                roundsToSurvive,
                this
            );
        }


        // --------------------------------------------------------
        // VALIDATE
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
        // CLEAR OLD ENCOUNTER
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
        // SPAWN UNITS
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.SpawningUnits
        );


        if (encounterSpawner == null)
        {
            Debug.LogError(
                "[EncounterManager] EncounterSpawner is missing!",
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
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] PREPARATION COMPLETE",
            this
        );


        Debug.Log(
            "[EncounterManager] Player may now be placed manually.",
            this
        );


        Debug.Log(
            "[EncounterManager] Waiting for NEXT ROUND button.",
            this
        );


        Debug.Log(
            "[EncounterManager] ========================================",
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
                "[EncounterManager] GridManager is missing!",
                this
            );

            valid = false;
        }


        if (encounterSpawner == null)
        {
            Debug.LogError(
                "[EncounterManager] EncounterSpawner is missing!",
                this
            );

            valid = false;
        }


        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing!",
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


        roundsToSurvive =
            Mathf.Max(
                1,
                roundsToSurvive
            );


        // --------------------------------------------------------
        // SPECIFIC TARGET VALIDATION
        // --------------------------------------------------------

        if (UsesSpecificEnemyTarget())
        {
            if (string.IsNullOrWhiteSpace(targetEnemyId))
            {
                Debug.LogError(
                    "[EncounterManager] This mission requires a " +
                    "specific enemy target, but Target Enemy ID is empty!",
                    this
                );

                valid = false;
            }
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
                "[EncounterManager] GridManager is missing!",
                this
            );

            return;
        }


        if (currentEncounter == null)
        {
            Debug.LogError(
                "[EncounterManager] Current encounter is NULL!",
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


        Debug.Log(
            "[EncounterManager] Grid created: " +
            "Shape=" +
            currentEncounter.shape +
            ", Width=" +
            currentEncounter.width +
            ", Height=" +
            currentEncounter.height +
            ", MinRadius=" +
            currentEncounter.minRadius +
            ", MaxRadius=" +
            currentEncounter.maxRadius,
            this
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
    // NEXT ROUND
    // ============================================================

    public void NextRound()
    {
        if (!encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] NextRound ignored. " +
                "No encounter is running.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // PREPARATION -> FIRST ROUND
        // --------------------------------------------------------

        if (currentState == EncounterState.Preparing)
        {
            BeginFirstRound();

            return;
        }


        // --------------------------------------------------------
        // COMBAT -> NEXT ROUND
        // --------------------------------------------------------

        if (currentState == EncounterState.Combat)
        {
            StartNextRound();

            return;
        }


        Debug.LogWarning(
            "[EncounterManager] NextRound ignored. " +
            "Current state = " +
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
                "[EncounterManager] First round has already started.",
                this
            );

            return;
        }


        if (currentState != EncounterState.Preparing)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot start Round 1 from state: " +
                currentState,
                this
            );

            return;
        }


        if (roundManager == null)
        {
            Debug.LogError(
                "[EncounterManager] RoundManager is missing!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // VERIFY PLAYER EXISTS
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
                "No Team.Player unit exists.\n" +
                "Place the player on the grid first.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // STARTING COMBAT
        // --------------------------------------------------------

        SetEncounterState(
            EncounterState.StartingCombat
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] STARTING COMBAT",
            this
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
                "[EncounterManager] RoundManager is missing!",
                this
            );

            return;
        }


        firstRoundStarted = true;


        SetEncounterState(
            EncounterState.Combat
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] COMBAT STARTED",
            this
        );


        Debug.Log(
            "[EncounterManager] ROUND 1 START",
            this
        );


        Debug.Log(
            "[EncounterManager] PLAYER TURN SHOULD START FIRST",
            this
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
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
                "[EncounterManager] RoundManager is missing!",
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
        // CHECK VICTORY BEFORE STARTING ANOTHER ROUND
        // --------------------------------------------------------

        if (CheckVictoryConditions())
        {
            return;
        }


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] NEXT ROUND",
            this
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        roundManager.StartRound();
    }


    // ============================================================
    // UNIT KILLED
    // ============================================================

    /// <summary>
    /// Called by HealthManager BEFORE the unit is disabled.
    /// This is important for specific-target missions.
    /// </summary>
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


        Debug.Log(
            "[EncounterManager] UNIT KILLED\n" +
            "Unit=" +
            killedUnit.name +
            "\nTeam=" +
            killedUnit.GetTeam() +
            "\nEncounterID=" +
            encounterUnitId,
            this
        );


        // --------------------------------------------------------
        // ONLY ENEMY DEATHS CAN COMPLETE ENEMY MISSIONS
        // --------------------------------------------------------

        if (killedUnit.GetTeam() != Team.Enemy)
        {
            CheckPlayerDefeat();

            return;
        }


        // --------------------------------------------------------
        // SPECIFIC TARGET
        // --------------------------------------------------------

        if (UsesSpecificEnemyTarget())
        {
            if (
                !string.IsNullOrWhiteSpace(encounterUnitId) &&
                encounterUnitId == targetEnemyId
            )
            {
                Debug.Log(
                    "[EncounterManager] ========================================",
                    this
                );


                Debug.Log(
                    "[EncounterManager] TARGET ENEMY DEFEATED!",
                    this
                );


                Debug.Log(
                    "[EncounterManager] Target ID: " +
                    targetEnemyId,
                    this
                );


                Debug.Log(
                    "[EncounterManager] Killed ID: " +
                    encounterUnitId,
                    this
                );


                Debug.Log(
                    "[EncounterManager] ========================================",
                    this
                );


                EncounterVictory();

                return;
            }


            Debug.Log(
                "[EncounterManager] Enemy killed, " +
                "but it was NOT the mission target.\n" +
                "Target=" +
                targetEnemyId +
                "\nKilled=" +
                encounterUnitId,
                this
            );


            // ----------------------------------------------------
            // OR SURVIVE ROUNDS
            // ----------------------------------------------------

            if (
                victoryCondition ==
                VictoryCondition.DefeatSpecificEnemyOrSurviveRounds
            )
            {
                CheckVictoryConditions();
            }


            return;
        }


        // --------------------------------------------------------
        // DEFEAT ALL ENEMIES
        // --------------------------------------------------------

        if (
            victoryCondition ==
            VictoryCondition.DefeatAllEnemies ||
            victoryCondition ==
            VictoryCondition.DefeatAllEnemiesOrSurviveRounds
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
        // ENEMY HEALTH
        // --------------------------------------------------------

        if (healthManager.GetTeam() != Team.Enemy)
        {
            return;
        }


        // --------------------------------------------------------
        // DO NOT DETERMINE SPECIFIC TARGET HERE.
        //
        // Die() calls HandleUnitKilled() with the actual ID.
        // --------------------------------------------------------

        if (!healthManager.IsAlive())
        {
            return;
        }


        // --------------------------------------------------------
        // SURVIVAL CONDITIONS DO NOT CARE ABOUT HEALTH.
        // --------------------------------------------------------

        if (UsesSurvival())
        {
            return;
        }


        // --------------------------------------------------------
        // NORMAL DEFEAT-ALL CHECK
        // --------------------------------------------------------

        if (
            victoryCondition ==
            VictoryCondition.DefeatAllEnemies ||
            victoryCondition ==
            VictoryCondition.DefeatAllEnemiesOrSurviveRounds
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
            "[EncounterManager] PLAYER HAS BEEN DEFEATED.",
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


        if (
            currentState != EncounterState.Combat &&
            currentState != EncounterState.StartingCombat
        )
        {
            return false;
        }


        Debug.Log(
            "[EncounterManager] Checking victory after round " +
            roundManager.GetCurrentRound(),
            this
        );


        // ========================================================
        // DEFEAT ALL ENEMIES
        // ========================================================

        if (
            victoryCondition ==
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


        // ========================================================
        // SURVIVE ROUNDS
        // ========================================================

        if (
            victoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            if (HasSurvivedRequiredRounds())
            {
                EncounterVictory();

                return true;
            }


            return false;
        }


        // ========================================================
        // DEFEAT ALL OR SURVIVE
        // ========================================================

        if (
            victoryCondition ==
            VictoryCondition.DefeatAllEnemiesOrSurviveRounds
        )
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();

                return true;
            }


            if (HasSurvivedRequiredRounds())
            {
                EncounterVictory();

                return true;
            }


            return false;
        }


        // ========================================================
        // DEFEAT SPECIFIC ENEMY
        // ========================================================

        if (
            victoryCondition ==
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


        // ========================================================
        // SPECIFIC ENEMY OR SURVIVE
        // ========================================================

        if (
            victoryCondition ==
            VictoryCondition.DefeatSpecificEnemyOrSurviveRounds
        )
        {
            if (IsTargetEnemyDead())
            {
                EncounterVictory();

                return true;
            }


            if (HasSurvivedRequiredRounds())
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
                    targetEnemyId
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


        // --------------------------------------------------------
        // If we found the target and it isn't alive, it is dead.
        // --------------------------------------------------------

        if (targetWasFound)
        {
            return true;
        }


        // --------------------------------------------------------
        // Target may already have been disabled by HealthManager
        // after HandleUnitKilled() was called.
        // --------------------------------------------------------

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


        Debug.Log(
            "[EncounterManager] Survival check: " +
            "Round " +
            currentRound +
            " / " +
            roundsToSurvive,
            this
        );


        return currentRound >= roundsToSurvive;
    }


    // ============================================================
    // CHECK AFTER ROUND
    // ============================================================

    public void CheckVictoryAfterRound()
    {
        if (!encounterRunning)
        {
            return;
        }


        if (currentState != EncounterState.Combat)
        {
            return;
        }


        Debug.Log(
            "[EncounterManager] Checking victory after round " +
            roundManager.GetCurrentRound(),
            this
        );


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


        // --------------------------------------------------------
        // SAFETY CHECK
        // --------------------------------------------------------

        if (
            victoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            if (HasLivingEnemies())
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory requested, " +
                    "but living enemies still remain.",
                    this
                );

                return;
            }
        }


        if (
            victoryCondition ==
            VictoryCondition.DefeatAllEnemiesOrSurviveRounds
        )
        {
            if (
                HasLivingEnemies() &&
                !HasSurvivedRequiredRounds()
            )
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory requested, " +
                    "but no victory condition has been satisfied.",
                    this
                );

                return;
            }
        }


        if (
            victoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            if (!IsTargetEnemyDead())
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory requested, " +
                    "but target enemy has not been defeated.",
                    this
                );

                return;
            }
        }


        if (
            victoryCondition ==
            VictoryCondition.DefeatSpecificEnemyOrSurviveRounds
        )
        {
            if (
                !IsTargetEnemyDead() &&
                !HasSurvivedRequiredRounds()
            )
            {
                Debug.LogWarning(
                    "[EncounterManager] Victory requested, " +
                    "but no victory condition has been satisfied.",
                    this
                );

                return;
            }
        }


        // --------------------------------------------------------
        // FINISH
        // --------------------------------------------------------

        encounterRunning = false;


        StopAllCoroutines();


        SetEncounterState(
            EncounterState.Victory
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] ENCOUNTER FINISHED — VICTORY",
            this
        );


        Debug.Log(
            "[EncounterManager] Victory condition: " +
            victoryCondition,
            this
        );


        if (UsesSpecificEnemyTarget())
        {
            Debug.Log(
                "[EncounterManager] Target enemy defeated: " +
                targetEnemyId,
                this
            );
        }


        if (UsesSurvival())
        {
            Debug.Log(
                "[EncounterManager] Survived " +
                roundsToSurvive +
                " rounds.",
                this
            );
        }


        if (roundManager != null)
        {
            Debug.Log(
                "[EncounterManager] Final round: " +
                roundManager.GetCurrentRound(),
                this
            );
        }


        if (gameStateManager != null)
        {
            gameStateManager.EncounterVictory();
        }


        string encounterName =
            currentEncounter != null
                ? currentEncounter.encounterName
                : "Unknown";


        Debug.Log(
            "[EncounterManager] VICTORY: " +
            encounterName,
            this
        );


        // --------------------------------------------------------
        // NOTIFY LEVEL MAP
        // --------------------------------------------------------

        OnEncounterVictory?.Invoke(
            currentEncounter
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
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
            "[EncounterManager] ========================================",
            this
        );


        Debug.Log(
            "[EncounterManager] ENCOUNTER FINISHED — DEFEAT",
            this
        );


        if (gameStateManager != null)
        {
            gameStateManager.EncounterDefeat();
        }


        string encounterName =
            currentEncounter != null
                ? currentEncounter.encounterName
                : "Unknown";


        Debug.Log(
            "[EncounterManager] DEFEAT: " +
            encounterName,
            this
        );


        Debug.Log(
            "[EncounterManager] ========================================",
            this
        );
    }


    // ============================================================
    // CONDITION HELPERS
    // ============================================================

    private bool UsesSpecificEnemyTarget()
    {
        return
            victoryCondition ==
                VictoryCondition.DefeatSpecificEnemy
            ||
            victoryCondition ==
                VictoryCondition.DefeatSpecificEnemyOrSurviveRounds;
    }


    private bool UsesSurvival()
    {
        return
            victoryCondition ==
                VictoryCondition.SurviveRounds
            ||
            victoryCondition ==
                VictoryCondition.DefeatAllEnemiesOrSurviveRounds
            ||
            victoryCondition ==
                VictoryCondition.DefeatSpecificEnemyOrSurviveRounds;
    }


    // ============================================================
    // STATE
    // ============================================================

    private void SetEncounterState(
        EncounterState newState)
    {
        currentState =
            newState;


        Debug.Log(
            "[EncounterManager] State → " +
            newState,
            this
        );
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
                "while one is running.",
                this
            );

            return;
        }


        if (encounter == null)
        {
            Debug.LogWarning(
                "[EncounterManager] Trying to assign NULL encounter.",
                this
            );
        }


        currentEncounter =
            encounter;
    }


    // ============================================================
    // SET VICTORY CONDITION
    // ============================================================

    public void SetVictoryCondition(
        VictoryCondition condition)
    {
        if (encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot change victory " +
                "condition while an encounter is running.",
                this
            );

            return;
        }


        victoryCondition =
            condition;
    }


    public void SetTargetEnemyId(
        string id)
    {
        if (encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot change target enemy " +
                "while an encounter is running.",
                this
            );

            return;
        }


        targetEnemyId =
            id;
    }


    public void SetRoundsToSurvive(
        int rounds)
    {
        if (encounterRunning)
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot change survival " +
                "rounds while an encounter is running.",
                this
            );

            return;
        }


        roundsToSurvive =
            Mathf.Max(
                1,
                rounds
            );
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