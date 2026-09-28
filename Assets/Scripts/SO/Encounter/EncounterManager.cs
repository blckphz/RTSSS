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

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private EncounterSpawner encounterSpawner;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private biomesManager biomesManager;
    [SerializeField] private musicManager musicManager;
    [SerializeField] private DayNightManager dayNightManager;

    // =========================================================
    // CURRENT ENCOUNTER
    // =========================================================

    [Header("Current Encounter")]
    [SerializeField] private EncounterDefinition currentEncounter;

    // =========================================================
    // TIMING
    // =========================================================

    [Header("Timing")]
    [SerializeField] private float gridSpawnDelay = 0.1f;
    [SerializeField] private float unitSpawnDelay = 0.1f;
    [SerializeField] private float combatStartDelay = 0.25f;

    private EncounterState currentState = EncounterState.None;

    private bool encounterRunning;
    private bool startingRound;

    public EncounterState CurrentState => currentState;
    public EncounterDefinition CurrentEncounter => currentEncounter;

    public VictoryCondition CurrentVictoryCondition =>
        currentEncounter != null
            ? currentEncounter.victoryCondition
            : VictoryCondition.DefeatAllEnemies;

    public string TargetEnemyId =>
        currentEncounter != null
            ? currentEncounter.targetEnemyId
            : string.Empty;

    public int RoundsToSurvive =>
        currentEncounter != null
            ? Mathf.Max(1, currentEncounter.roundsToSurvive)
            : 1;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindDependencies();
    }

    private void OnEnable()
    {
        HealthManager.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        HealthManager.OnHealthChanged -= HandleHealthChanged;
    }

    // =========================================================
    // FIND DEPENDENCIES
    // =========================================================

    private void FindDependencies()
    {
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (encounterSpawner == null)
            encounterSpawner = FindFirstObjectByType<EncounterSpawner>();

        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>();

        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        if (biomesManager == null)
            biomesManager = FindFirstObjectByType<biomesManager>();

        if (musicManager == null)
            musicManager = FindFirstObjectByType<musicManager>();

        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightManager>();
    }

    // =========================================================
    // ENCOUNTER START
    // =========================================================

    public void StartEncounter()
    {
        if (encounterRunning)
            return;

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

        StartCoroutine(StartEncounterRoutine());
    }

    private IEnumerator StartEncounterRoutine()
    {
        encounterRunning = true;
        startingRound = false;

        SetEncounterState(EncounterState.Preparing);

        if (!ValidateDependencies())
        {
            encounterRunning = false;
            SetEncounterState(EncounterState.None);
            yield break;
        }

        roundManager.ResetRounds();

        ClearPreviousEncounter();

        SetupEncounterEnvironment();

        yield return null;

        if (cardManager != null)
            cardManager.StartNewEncounterHand();

        yield return null;

        SetEncounterState(EncounterState.CreatingGrid);

        SetupGrid();

        if (gridSpawnDelay > 0f)
            yield return new WaitForSeconds(gridSpawnDelay);

        SetEncounterState(EncounterState.SpawningUnits);

        encounterSpawner.SpawnEncounter(currentEncounter);

        if (unitSpawnDelay > 0f)
            yield return new WaitForSeconds(unitSpawnDelay);

        SetEncounterState(EncounterState.Preparing);

        Debug.Log(
            "[EncounterManager] Round 1 Prepare phase. " +
            "Waiting for Next Round button.",
            this
        );
    }

    // =========================================================
    // ENVIRONMENT
    // =========================================================

    private void SetupEncounterEnvironment()
    {
        if (currentEncounter == null)
            return;

        if (dayNightManager != null)
        {
            dayNightManager.SetDayNight(
                currentEncounter.dayNight
            );
        }

        if (
            biomesManager != null &&
            !string.IsNullOrWhiteSpace(
                currentEncounter.biomeGameObjectName
            )
        )
        {
            biomesManager.SetBiome(
                currentEncounter.biomeGameObjectName
            );
        }

        if (
            musicManager != null &&
            currentEncounter.music != null
        )
        {
            musicManager.PlayMusic(
                currentEncounter.music
            );
        }
    }

    // =========================================================
    // SURVIVAL WAVE SPAWNING
    // =========================================================

    public void SpawnNextRoundEnemies()
    {
        if (
            !encounterRunning ||
            !UsesSurvival() ||
            currentEncounter == null ||
            encounterSpawner == null ||
            roundManager == null
        )
        {
            return;
        }

        int currentRound = roundManager.GetCurrentRound();

        Debug.Log(
            "[EncounterManager] Spawning Wave " +
            currentRound +
            " for Round " +
            currentRound +
            ".",
            this
        );

        encounterSpawner.SpawnWave(
            currentEncounter,
            true
        );
    }

    // =========================================================
    // NEXT ROUND
    // =========================================================

    public void NextRound()
    {
        if (!CanPressNextRound())
            return;

        StartNextRound();
    }

    private void StartNextRound()
    {
        if (!CanStartRound())
            return;

        if (!HasLivingPlayer())
        {
            EncounterDefeat();
            return;
        }

        if (UsesSurvival())
            SpawnNextRoundEnemies();

        if (!encounterRunning)
            return;

        startingRound = true;

        SetEncounterState(EncounterState.StartingCombat);

        int round = roundManager.GetCurrentRound();

        if (round == 1 && gameStateManager != null)
            gameStateManager.EncounterStarted();

        if (combatStartDelay <= 0f)
        {
            StartCombatRound();
        }
        else
        {
            StartCoroutine(StartCombatAfterDelay());
        }
    }

    private bool CanStartRound()
    {
        return encounterRunning &&
               !startingRound &&
               roundManager != null &&
               !roundManager.IsRoundRunning() &&
               roundManager.IsSetupPhase();
    }

    private IEnumerator StartCombatAfterDelay()
    {
        yield return new WaitForSeconds(combatStartDelay);

        if (
            !encounterRunning ||
            currentState != EncounterState.StartingCombat
        )
        {
            yield break;
        }

        StartCombatRound();
    }

    private void StartCombatRound()
    {
        if (
            !encounterRunning ||
            roundManager == null
        )
        {
            return;
        }

        int round = roundManager.GetCurrentRound();

        SetEncounterState(EncounterState.Combat);

        startingRound = false;

        Debug.Log(
            "[EncounterManager] Starting combat Round " + round,
            this
        );

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
        if (
            !encounterRunning ||
            killedUnit == null
        )
        {
            return;
        }

        if (killedUnit.GetTeam() != Team.Enemy)
        {
            CheckPlayerDefeat();
            return;
        }

        VictoryCondition condition = CurrentVictoryCondition;

        if (condition == VictoryCondition.DefeatSpecificEnemy)
        {
            if (
                !string.IsNullOrWhiteSpace(encounterUnitId) &&
                encounterUnitId == TargetEnemyId
            )
            {
                EncounterVictory();
            }

            return;
        }

        if (
            condition == VictoryCondition.DefeatAllEnemies &&
            !HasLivingEnemies()
        )
        {
            EncounterVictory();
        }
    }

    // =========================================================
    // HEALTH
    // =========================================================

    private void HandleHealthChanged(
        HealthManager healthManager
    )
    {
        if (
            !encounterRunning ||
            healthManager == null
        )
        {
            return;
        }

        Team team = healthManager.GetTeam();

        if (team == Team.Player)
        {
            CheckPlayerDefeat();
            return;
        }

        if (
            team != Team.Enemy ||
            !healthManager.IsAlive() ||
            UsesSurvival()
        )
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
        if (
            encounterRunning &&
            !HasLivingPlayer()
        )
        {
            EncounterDefeat();
        }
    }

    // =========================================================
    // VICTORY
    // =========================================================

    public bool CheckVictoryConditions()
    {
        if (
            !encounterRunning ||
            currentEncounter == null ||
            UsesSurvival()
        )
        {
            return false;
        }

        switch (CurrentVictoryCondition)
        {
            case VictoryCondition.DefeatAllEnemies:

                if (!HasLivingEnemies())
                {
                    EncounterVictory();
                    return true;
                }

                break;

            case VictoryCondition.DefeatSpecificEnemy:

                if (IsTargetEnemyDead())
                {
                    EncounterVictory();
                    return true;
                }

                break;
        }

        return false;
    }

    // =========================================================
    // VICTORY AFTER ROUND
    // =========================================================

    public void CheckVictoryAfterRound()
    {
        if (
            !encounterRunning ||
            roundManager == null
        )
        {
            return;
        }

        if (UsesSurvival())
        {
            int completedRound =
                roundManager.GetCurrentRound();

            if (completedRound >= RoundsToSurvive)
            {
                EncounterVictory();
                return;
            }

            SetEncounterState(EncounterState.Preparing);
            return;
        }

        CheckVictoryConditions();

        if (!encounterRunning)
            return;

        SetEncounterState(EncounterState.Preparing);
    }

    // =========================================================
    // LIVING PLAYER
    // =========================================================

    private bool HasLivingPlayer()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (
                unit == null ||
                unit.GetTeam() != Team.Player
            )
            {
                continue;
            }

            if (
                unit.TryGetComponent(
                    out HealthManager health
                ) &&
                health.IsAlive()
            )
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // LIVING ENEMIES
    // =========================================================

    private bool HasLivingEnemies()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (
                unit == null ||
                unit.GetTeam() != Team.Enemy
            )
            {
                continue;
            }

            if (
                unit.TryGetComponent(
                    out HealthManager health
                ) &&
                health.IsAlive()
            )
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // TARGET ENEMY
    // =========================================================

    private bool IsTargetEnemyDead()
    {
        if (
            CurrentVictoryCondition !=
            VictoryCondition.DefeatSpecificEnemy
        )
        {
            return false;
        }

        string targetId = TargetEnemyId;

        if (string.IsNullOrWhiteSpace(targetId))
            return false;

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        bool targetFound = false;

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (
                unit == null ||
                unit.GetTeam() != Team.Enemy
            )
            {
                continue;
            }

            if (
                !unit.TryGetComponent(
                    out EncounterUnit encounterUnit
                )
            )
            {
                continue;
            }

            if (
                !encounterUnit.HasEncounterUnitId(
                    targetId
                )
            )
            {
                continue;
            }

            targetFound = true;

            if (
                unit.TryGetComponent(
                    out HealthManager health
                ) &&
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
            return;

        VictoryCondition condition =
            CurrentVictoryCondition;

        if (
            condition == VictoryCondition.DefeatAllEnemies &&
            HasLivingEnemies()
        )
        {
            return;
        }

        if (
            condition == VictoryCondition.SurviveRounds &&
            (
                roundManager == null ||
                roundManager.GetCurrentRound() < RoundsToSurvive
            )
        )
        {
            return;
        }

        if (
            condition == VictoryCondition.DefeatSpecificEnemy &&
            !IsTargetEnemyDead()
        )
        {
            return;
        }

        encounterRunning = false;
        startingRound = false;

        StopAllCoroutines();

        if (combatManager != null)
            combatManager.ClearEnemyTurnLocks();

        SetEncounterState(EncounterState.Victory);

        if (musicManager != null)
            musicManager.StopMusic();

        if (gameStateManager != null)
            gameStateManager.EncounterVictory();

        OnEncounterVictory?.Invoke(currentEncounter);
    }

    // =========================================================
    // ENCOUNTER DEFEAT
    // =========================================================

    public void EncounterDefeat()
    {
        if (!encounterRunning)
            return;

        encounterRunning = false;
        startingRound = false;

        StopAllCoroutines();

        if (combatManager != null)
            combatManager.ClearEnemyTurnLocks();

        SetEncounterState(EncounterState.Defeat);

        if (musicManager != null)
            musicManager.StopMusic();

        if (gameStateManager != null)
            gameStateManager.EncounterDefeat();
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateEncounterDefinition()
    {
        if (currentEncounter == null)
            return false;

        switch (currentEncounter.victoryCondition)
        {
            case VictoryCondition.SurviveRounds:
                return currentEncounter.roundsToSurvive > 0;

            case VictoryCondition.DefeatSpecificEnemy:
                return !string.IsNullOrWhiteSpace(
                    currentEncounter.targetEnemyId
                );

            default:
                return true;
        }
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
        if (
            gridManager == null ||
            currentEncounter == null
        )
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
    // CLEANUP
    // =========================================================

    private void ClearPreviousEncounter()
    {
        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (unit == null)
                continue;

            if (gridManager != null)
                gridManager.RemoveUnit(unit.gameObject);

            Destroy(unit.gameObject);
        }

        if (gridManager != null)
            gridManager.CleanupDeadUnits();

        if (combatManager != null)
            combatManager.ClearEnemyTurnLocks();
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
            return;

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
            return;

        currentEncounter = encounter;
    }

    public bool IsEncounterRunning()
    {
        return encounterRunning;
    }

    public bool IsFinished()
    {
        return currentState == EncounterState.Victory ||
               currentState == EncounterState.Defeat;
    }

    public bool IsPreparing()
    {
        return currentState == EncounterState.Preparing;
    }

    public bool IsInCombat()
    {
        return currentState == EncounterState.Combat;
    }

    public bool CanPressNextRound()
    {
        return encounterRunning &&
               currentState == EncounterState.Preparing &&
               !startingRound &&
               roundManager != null &&
               !roundManager.IsRoundRunning() &&
               roundManager.IsSetupPhase();
    }
}
