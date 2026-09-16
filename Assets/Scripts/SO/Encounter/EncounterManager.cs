using System;
using System.Collections;
using UnityEngine;

public class EncounterManager : MonoBehaviour
{
    public static event Action<EncounterDefinition> OnEncounterVictory;

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

    [SerializeField]
    private CardManager cardManager;

    [SerializeField]
    private biomesManager biomesManager;

    [SerializeField]
    private musicManager musicManager;

    [Header("Current Encounter")]
    [SerializeField]
    private EncounterDefinition currentEncounter;

    [Header("Timing")]
    [SerializeField]
    private float gridSpawnDelay = 0.1f;

    [SerializeField]
    private float unitSpawnDelay = 0.1f;

    [SerializeField]
    private float combatStartDelay = 0.25f;

    private EncounterState currentState =
        EncounterState.None;

    private bool encounterRunning;
    private bool startingRound;

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

        if (cardManager == null)
        {
            cardManager =
                FindFirstObjectByType<CardManager>();
        }

        if (biomesManager == null)
        {
            biomesManager =
                FindFirstObjectByType<biomesManager>();
        }

        if (musicManager == null)
        {
            musicManager =
                FindFirstObjectByType<musicManager>();
        }
    }

    // =========================================================
    // ENCOUNTER START
    // =========================================================

    public void StartEncounter()
    {
        if (encounterRunning)
        {
            return;
        }

        if (currentEncounter == null)
        {
            Debug.LogWarning(
                "[EncounterManager] No encounter assigned.",
                this
            );

            return;
        }

        if (!ValidateEncounterDefinition())
        {
            Debug.LogWarning(
                "[EncounterManager] Encounter definition is invalid.",
                this
            );

            return;
        }

        StartCoroutine(
            StartEncounterRoutine()
        );
    }

    private IEnumerator StartEncounterRoutine()
    {
        encounterRunning = true;
        startingRound = false;

        /*
         * Round 1 begins in Prepare.
         *
         * The initial Wave 1 is spawned below.
         * It is deliberately NOT locked, so it can act
         * during Round 1.
         */
        SetEncounterState(
            EncounterState.Preparing
        );

        if (!ValidateDependencies())
        {
            encounterRunning = false;

            SetEncounterState(
                EncounterState.None
            );

            yield break;
        }

        // -----------------------------------------------------
        // RESET ROUND SYSTEM
        // -----------------------------------------------------

        if (roundManager != null)
        {
            roundManager.ResetRounds();
        }

        // -----------------------------------------------------
        // CLEAR PREVIOUS ENCOUNTER
        // -----------------------------------------------------

        ClearPreviousEncounter();

        // -----------------------------------------------------
        // BIOME / MUSIC
        // -----------------------------------------------------

        SetupBiomeAndMusic();

        yield return null;

        // -----------------------------------------------------
        // CARDS
        // -----------------------------------------------------

        if (cardManager != null)
        {
            cardManager.StartNewEncounterHand();
        }

        yield return null;

        // -----------------------------------------------------
        // CREATE GRID
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // INITIAL ENCOUNTER SPAWN
        // -----------------------------------------------------

        SetEncounterState(
            EncounterState.SpawningUnits
        );

        /*
         * SpawnEncounter() creates Wave 1.
         *
         * IMPORTANT:
         * Wave 1 is spawned UNLOCKED.
         *
         * Therefore initial enemies can act in Round 1.
         */
        encounterSpawner.SpawnEncounter(
            currentEncounter
        );

        if (unitSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                unitSpawnDelay
            );
        }

        // -----------------------------------------------------
        // ROUND 1 PREPARE
        // -----------------------------------------------------

        SetEncounterState(
            EncounterState.Preparing
        );

        Debug.Log(
            "[EncounterManager] Round 1 Prepare phase. " +
            "Waiting for Next Round button.",
            this
        );
    }

    // =========================================================
    // SURVIVAL WAVE SPAWNING
    // =========================================================

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
            return;
        }

        if (encounterSpawner == null)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        int currentRound =
            roundManager.GetCurrentRound();

        Debug.Log(
            "[EncounterManager] Spawning Wave " +
            currentRound +
            " for Round " +
            currentRound +
            ".",
            this
        );

        /*
         * IMPORTANT:
         *
         * Every survival wave after the initial encounter
         * is locked for the round in which it spawns.
         *
         * Wave 2 -> locked during Round 2
         * Wave 3 -> locked during Round 3
         * etc.
         */
        encounterSpawner.SpawnWave(
            currentEncounter,
            true
        );
    }

    // =========================================================
    // NEXT ROUND BUTTON
    // =========================================================

    public void NextRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (
            currentState !=
            EncounterState.Preparing
        )
        {
            Debug.Log(
                "[EncounterManager] NextRound ignored. " +
                "Encounter state is " +
                currentState,
                this
            );

            return;
        }

        if (roundManager == null)
        {
            return;
        }

        if (startingRound)
        {
            Debug.Log(
                "[EncounterManager] NextRound ignored. " +
                "A round is already starting.",
                this
            );

            return;
        }

        if (roundManager.IsRoundRunning())
        {
            Debug.Log(
                "[EncounterManager] NextRound ignored. " +
                "Round is still running.",
                this
            );

            return;
        }

        if (!roundManager.IsSetupPhase())
        {
            Debug.Log(
                "[EncounterManager] NextRound ignored. " +
                "RoundManager is not in Setup.",
                this
            );

            return;
        }

        int round =
            roundManager.GetCurrentRound();

        Debug.Log(
            "[EncounterManager] Next Round pressed. " +
            "Leaving Prepare Phase for Round " +
            round +
            ".",
            this
        );

        StartNextRound();
    }

    // =========================================================
    // START CURRENT PREPARED ROUND
    // =========================================================

    private void StartNextRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (startingRound)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        if (roundManager.IsRoundRunning())
        {
            Debug.Log(
                "[EncounterManager] StartNextRound blocked. " +
                "Round is still running.",
                this
            );

            return;
        }

        if (!roundManager.IsSetupPhase())
        {
            Debug.Log(
                "[EncounterManager] StartNextRound blocked. " +
                "RoundManager is not in Setup.",
                this
            );

            return;
        }

        if (!HasLivingPlayer())
        {
            Debug.LogWarning(
                "[EncounterManager] Cannot start round. " +
                "No living player found.",
                this
            );

            return;
        }

        int round =
            roundManager.GetCurrentRound();

        /*
         * For survival encounters:
         *
         * Round 1 -> Spawn Wave 1
         * Round 2 -> Spawn Wave 2
         * Round 3 -> Spawn Wave 3
         *
         * Wave 1 is handled by SpawnEncounter() and is
         * unlocked.
         *
         * Waves 2+ are handled here and are locked.
         */
        if (UsesSurvival())
        {
            Debug.Log(
                "[EncounterManager] Spawning Wave " +
                round +
                " before starting Round " +
                round +
                ".",
                this
            );

            SpawnNextRoundEnemies();
        }

        if (!encounterRunning)
        {
            return;
        }

        startingRound = true;

        SetEncounterState(
            EncounterState.StartingCombat
        );

        if (round == 1)
        {
            if (gameStateManager != null)
            {
                gameStateManager.EncounterStarted();
            }
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

    private IEnumerator StartCombatAfterDelay()
    {
        yield return new WaitForSeconds(
            combatStartDelay
        );

        if (!encounterRunning)
        {
            yield break;
        }

        if (
            currentState !=
            EncounterState.StartingCombat
        )
        {
            yield break;
        }

        StartCombatRound();
    }

    private void StartCombatRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        int round =
            roundManager.GetCurrentRound();

        SetEncounterState(
            EncounterState.Combat
        );

        startingRound = false;

        Debug.Log(
            "[EncounterManager] Starting combat Round " +
            round,
            this
        );

        /*
         * IMPORTANT:
         *
         * Wave locks have already been registered by
         * EncounterSpawner.
         *
         * RoundManager.StartRound() deliberately DOES NOT
         * clear them.
         */
        roundManager.StartRound();
    }

    // =========================================================
    // UNIT DEATH
    // =========================================================

    public void HandleUnitKilled(
        HealthManager killedUnit,
        string encounterUnitId
    )
    {
        if (!encounterRunning)
        {
            return;
        }

        if (killedUnit == null)
        {
            return;
        }

        if (
            killedUnit.GetTeam() !=
            Team.Enemy
        )
        {
            CheckPlayerDefeat();
            return;
        }

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
            }

            return;
        }

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();
            }
        }
    }

    // =========================================================
    // HEALTH CHANGES
    // =========================================================

    private void HandleHealthChanged(
        HealthManager healthManager
    )
    {
        if (!encounterRunning)
        {
            return;
        }

        if (healthManager == null)
        {
            return;
        }

        if (
            healthManager.GetTeam() ==
            Team.Player
        )
        {
            CheckPlayerDefeat();
            return;
        }

        if (
            healthManager.GetTeam() !=
            Team.Enemy
        )
        {
            return;
        }

        if (!healthManager.IsAlive())
        {
            return;
        }

        if (UsesSurvival())
        {
            return;
        }

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies
        )
        {
            CheckVictoryConditions();
        }
    }

    // =========================================================
    // PLAYER DEFEAT
    // =========================================================

    private void CheckPlayerDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (!HasLivingPlayer())
        {
            EncounterDefeat();
        }
    }

    // =========================================================
    // VICTORY CHECKING
    // =========================================================

    public bool CheckVictoryConditions()
    {
        if (!encounterRunning)
        {
            return false;
        }

        if (currentEncounter == null)
        {
            return false;
        }

        if (UsesSurvival())
        {
            return false;
        }

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
        }

        return false;
    }

    // =========================================================
    // CHECK VICTORY AFTER ROUND
    // =========================================================

    public void CheckVictoryAfterRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        if (UsesSurvival())
        {
            int completedRound =
                roundManager.GetCurrentRound();

            Debug.Log(
                "[EncounterManager] Completed survival round: " +
                completedRound +
                " / " +
                RoundsToSurvive,
                this
            );

            if (
                completedRound >=
                RoundsToSurvive
            )
            {
                EncounterVictory();
                return;
            }

            SetEncounterState(
                EncounterState.Preparing
            );

            Debug.Log(
                "[EncounterManager] Round " +
                completedRound +
                " complete. " +
                "Waiting for Next Round button " +
                "for Round " +
                (completedRound + 1) +
                ".",
                this
            );

            return;
        }

        CheckVictoryConditions();

        if (!encounterRunning)
        {
            return;
        }

        SetEncounterState(
            EncounterState.Preparing
        );
    }

    // =========================================================
    // LIVING UNITS
    // =========================================================

    private bool HasLivingPlayer()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (
            int i = 0;
            i < units.Length;
            i++
        )
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (
                unit.GetTeam() !=
                Team.Player
            )
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
                return true;
            }
        }

        return false;
    }

    private bool HasLivingEnemies()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (
            int i = 0;
            i < units.Length;
            i++
        )
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (
                unit.GetTeam() !=
                Team.Enemy
            )
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
                return true;
            }
        }

        return false;
    }

    private bool IsTargetEnemyDead()
    {
        if (
            CurrentVictoryCondition !=
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            return false;
        }

        if (
            string.IsNullOrWhiteSpace(
                TargetEnemyId
            )
        )
        {
            return false;
        }

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        bool targetFound = false;

        for (
            int i = 0;
            i < units.Length;
            i++
        )
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (
                unit.GetTeam() !=
                Team.Enemy
            )
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

            targetFound = true;

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

        return targetFound;
    }

    // =========================================================
    // ENCOUNTER VICTORY
    // =========================================================

    public void EncounterVictory()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatAllEnemies &&
            HasLivingEnemies()
        )
        {
            return;
        }

        if (
            CurrentVictoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            if (
                roundManager == null ||
                roundManager.GetCurrentRound() <
                RoundsToSurvive
            )
            {
                return;
            }
        }

        if (
            CurrentVictoryCondition ==
            VictoryCondition.DefeatSpecificEnemy &&
            !IsTargetEnemyDead()
        )
        {
            return;
        }

        encounterRunning = false;
        startingRound = false;

        StopAllCoroutines();

        /*
         * Make sure no wave locks survive into a later encounter.
         */
        if (combatManager != null)
        {
            combatManager.ClearEnemyTurnLocks();
        }

        SetEncounterState(
            EncounterState.Victory
        );

        if (musicManager != null)
        {
            musicManager.StopMusic();
        }

        if (gameStateManager != null)
        {
            gameStateManager.EncounterVictory();
        }

        OnEncounterVictory?.Invoke(
            currentEncounter
        );
    }

    // =========================================================
    // ENCOUNTER DEFEAT
    // =========================================================

    public void EncounterDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        encounterRunning = false;
        startingRound = false;

        StopAllCoroutines();

        /*
         * Clean up any wave locks.
         */
        if (combatManager != null)
        {
            combatManager.ClearEnemyTurnLocks();
        }

        SetEncounterState(
            EncounterState.Defeat
        );

        if (musicManager != null)
        {
            musicManager.StopMusic();
        }

        if (gameStateManager != null)
        {
            gameStateManager.EncounterDefeat();
        }
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateEncounterDefinition()
    {
        if (currentEncounter == null)
        {
            return false;
        }

        if (
            currentEncounter.victoryCondition ==
            VictoryCondition.SurviveRounds
        )
        {
            return currentEncounter.roundsToSurvive > 0;
        }

        if (
            currentEncounter.victoryCondition ==
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            return !string.IsNullOrWhiteSpace(
                currentEncounter.targetEnemyId
            );
        }

        return true;
    }

    private bool ValidateDependencies()
    {
        return gridManager != null &&
               encounterSpawner != null &&
               roundManager != null;
    }

    // =========================================================
    // GRID
    // =========================================================

    private void SetupGrid()
    {
        if (gridManager == null)
        {
            return;
        }

        if (currentEncounter == null)
        {
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

    // =========================================================
    // BIOME / MUSIC
    // =========================================================

    private void SetupBiomeAndMusic()
    {
        if (currentEncounter == null)
        {
            return;
        }

        if (biomesManager != null)
        {
            if (
                !string.IsNullOrWhiteSpace(
                    currentEncounter.biomeGameObjectName
                )
            )
            {
                biomesManager.SetBiome(
                    currentEncounter.biomeGameObjectName
                );
            }
        }

        if (musicManager != null)
        {
            if (currentEncounter.music != null)
            {
                musicManager.PlayMusic(
                    currentEncounter.music
                );
            }
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void ClearPreviousEncounter()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (
            int i = 0;
            i < units.Length;
            i++
        )
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

        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }

        if (combatManager != null)
        {
            combatManager.ClearEnemyTurnLocks();
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private bool UsesSurvival()
    {
        return CurrentVictoryCondition ==
               VictoryCondition.SurviveRounds;
    }

    private void SetEncounterState(
        EncounterState state
    )
    {
        if (currentState == state)
        {
            return;
        }

        currentState = state;

        Debug.Log(
            "[EncounterManager] State changed to " +
            currentState,
            this
        );
    }

    public void SetCurrentEncounter(
        EncounterDefinition encounter
    )
    {
        if (encounterRunning)
        {
            return;
        }

        currentEncounter = encounter;
    }

    public bool IsEncounterRunning()
    {
        return encounterRunning;
    }

    public bool IsFinished()
    {
        return currentState ==
                   EncounterState.Victory ||
               currentState ==
                   EncounterState.Defeat;
    }

    public bool IsPreparing()
    {
        return currentState ==
               EncounterState.Preparing;
    }

    public bool IsInCombat()
    {
        return currentState ==
               EncounterState.Combat;
    }

    public bool CanPressNextRound()
    {
        if (!encounterRunning)
        {
            return false;
        }

        if (
            currentState !=
            EncounterState.Preparing
        )
        {
            return false;
        }

        if (startingRound)
        {
            return false;
        }

        if (roundManager == null)
        {
            return false;
        }

        if (roundManager.IsRoundRunning())
        {
            return false;
        }

        if (!roundManager.IsSetupPhase())
        {
            return false;
        }

        return true;
    }
}