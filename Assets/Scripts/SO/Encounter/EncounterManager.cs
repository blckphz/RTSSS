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
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private EncounterSpawner encounterSpawner;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private CombatManager combatManager;

    [Header("Current Encounter")]
    [SerializeField] private EncounterDefinition currentEncounter;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float gridSpawnDelay = 0.1f;
    [SerializeField, Min(0f)] private float unitSpawnDelay = 0.1f;
    [SerializeField, Min(0f)] private float combatStartDelay = 0.25f;

    private EncounterState currentState = EncounterState.None;
    private bool encounterRunning;
    private bool firstRoundStarted;

    public EncounterState CurrentState => currentState;

    public EncounterDefinition CurrentEncounter => currentEncounter;

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

            return Mathf.Max(1, currentEncounter.roundsToSurvive);
        }
    }

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

    private void FindDependencies()
    {
        if (gameStateManager == null)
        {
            gameStateManager = FindFirstObjectByType<GameStateManager>();
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (encounterSpawner == null)
        {
            encounterSpawner = FindFirstObjectByType<EncounterSpawner>();
        }

        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>();
        }

        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
    }

    public void StartEncounter()
    {
        if (encounterRunning)
        {
            return;
        }

        if (currentEncounter == null)
        {
            return;
        }

        if (!ValidateEncounterDefinition())
        {
            return;
        }

        StartCoroutine(StartEncounterRoutine());
    }

    private bool ValidateEncounterDefinition()
    {
        if (currentEncounter == null)
        {
            return false;
        }

        if (currentEncounter.victoryCondition == VictoryCondition.SurviveRounds)
        {
            if (currentEncounter.roundsToSurvive < 1)
            {
                return false;
            }
        }

        if (currentEncounter.victoryCondition == VictoryCondition.DefeatSpecificEnemy)
        {
            if (string.IsNullOrWhiteSpace(currentEncounter.targetEnemyId))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator StartEncounterRoutine()
    {
        encounterRunning = true;
        firstRoundStarted = false;

        SetEncounterState(EncounterState.Preparing);

        if (!ValidateDependencies())
        {
            encounterRunning = false;
            SetEncounterState(EncounterState.None);
            yield break;
        }

        ClearPreviousEncounter();

        yield return null;

        SetEncounterState(EncounterState.CreatingGrid);

        SetupGrid();

        if (gridSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(gridSpawnDelay);
        }

        SetEncounterState(EncounterState.SpawningUnits);

        if (encounterSpawner == null)
        {
            encounterRunning = false;
            SetEncounterState(EncounterState.None);
            yield break;
        }

        encounterSpawner.SpawnEncounter(currentEncounter);

        if (unitSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(unitSpawnDelay);
        }

        SetEncounterState(EncounterState.Preparing);
    }

    private bool ValidateDependencies()
    {
        bool valid = true;

        if (gridManager == null)
        {
            valid = false;
        }

        if (encounterSpawner == null)
        {
            valid = false;
        }

        if (roundManager == null)
        {
            valid = false;
        }

        return valid;
    }

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
        AttackUnit[] units = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (unit == null)
            {
                continue;
            }

            if (gridManager != null)
            {
                gridManager.RemoveUnit(unit.gameObject);
            }

            Destroy(unit.gameObject);
        }
    }

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
            return;
        }

        if (encounterSpawner == null)
        {
            return;
        }

        SetEncounterState(EncounterState.SpawningUnits);

        encounterSpawner.SpawnEncounter(currentEncounter);

        SetEncounterState(EncounterState.Combat);
    }

    public void NextRound()
    {
        if (!encounterRunning)
        {
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
    }

    private void BeginFirstRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (firstRoundStarted)
        {
            return;
        }

        if (currentState != EncounterState.Preparing)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        AttackUnit[] players = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        bool playerFound = false;

        for (int i = 0; i < players.Length; i++)
        {
            AttackUnit unit = players[i];

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
            return;
        }

        SetEncounterState(EncounterState.StartingCombat);

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
            StartCoroutine(StartCombatAfterDelay());
        }
    }

    private IEnumerator StartCombatAfterDelay()
    {
        yield return new WaitForSeconds(combatStartDelay);

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

        firstRoundStarted = true;

        SetEncounterState(EncounterState.Combat);

        roundManager.StartRound();
    }

    private void StartNextRound()
    {
        if (!encounterRunning)
        {
            return;
        }

        if (roundManager == null)
        {
            return;
        }

        if (roundManager.IsRoundRunning())
        {
            return;
        }

        if (CheckVictoryConditions())
        {
            return;
        }

        roundManager.StartRound();
    }

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

        if (killedUnit.GetTeam() != Team.Enemy)
        {
            CheckPlayerDefeat();
            return;
        }

        if (CurrentVictoryCondition == VictoryCondition.DefeatSpecificEnemy)
        {
            if (
                !string.IsNullOrWhiteSpace(encounterUnitId) &&
                encounterUnitId == TargetEnemyId
            )
            {
                EncounterVictory();
                return;
            }

            return;
        }

        if (CurrentVictoryCondition == VictoryCondition.DefeatAllEnemies)
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();
                return;
            }
        }
    }

    private void HandleHealthChanged(HealthManager healthManager)
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

        if (healthManager.GetTeam() == Team.Player)
        {
            CheckPlayerDefeat();
            return;
        }

        if (healthManager.GetTeam() != Team.Enemy)
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

        if (CurrentVictoryCondition == VictoryCondition.DefeatAllEnemies)
        {
            CheckVictoryConditions();
        }
    }

    private void CheckPlayerDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        AttackUnit[] units = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() != Team.Player)
            {
                continue;
            }

            HealthManager health = unit.GetComponent<HealthManager>();

            if (health != null && health.IsAlive())
            {
                return;
            }
        }

        EncounterDefeat();
    }

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
            return false;
        }

        if (CurrentVictoryCondition == VictoryCondition.SurviveRounds)
        {
            int currentRound = roundManager.GetCurrentRound();
            bool survived = currentRound >= RoundsToSurvive;

            if (survived)
            {
                EncounterVictory();
                return true;
            }

            return false;
        }

        if (CurrentVictoryCondition == VictoryCondition.DefeatAllEnemies)
        {
            if (!HasLivingEnemies())
            {
                EncounterVictory();
                return true;
            }

            return false;
        }

        if (CurrentVictoryCondition == VictoryCondition.DefeatSpecificEnemy)
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

        AttackUnit[] units = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        bool targetWasFound = false;

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

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

            if (!encounterUnit.HasEncounterUnitId(TargetEnemyId))
            {
                continue;
            }

            targetWasFound = true;

            HealthManager health =
                unit.GetComponent<HealthManager>();

            if (health != null && health.IsAlive())
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

    private bool HasLivingEnemies()
    {
        AttackUnit[] units = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        for (int i = 0; i < units.Length; i++)
        {
            AttackUnit unit = units[i];

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

    private bool HasSurvivedRequiredRounds()
    {
        if (roundManager == null)
        {
            return false;
        }

        int currentRound = roundManager.GetCurrentRound();

        bool survived = currentRound >= RoundsToSurvive;

        return survived;
    }

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

        int currentRound = roundManager.GetCurrentRound();

        if (CurrentVictoryCondition == VictoryCondition.SurviveRounds)
        {
            if (currentRound >= RoundsToSurvive)
            {
                EncounterVictory();
                return;
            }
        }

        CheckVictoryConditions();
    }

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

        if (CurrentVictoryCondition == VictoryCondition.DefeatAllEnemies)
        {
            if (HasLivingEnemies())
            {
                return;
            }
        }

        if (CurrentVictoryCondition == VictoryCondition.SurviveRounds)
        {
            if (!HasSurvivedRequiredRounds())
            {
                return;
            }
        }

        if (CurrentVictoryCondition == VictoryCondition.DefeatSpecificEnemy)
        {
            if (!IsTargetEnemyDead())
            {
                return;
            }
        }

        encounterRunning = false;

        StopAllCoroutines();

        SetEncounterState(EncounterState.Victory);

        if (gameStateManager != null)
        {
            gameStateManager.EncounterVictory();
        }

        OnEncounterVictory?.Invoke(currentEncounter);
    }

    public void EncounterDefeat()
    {
        if (!encounterRunning)
        {
            return;
        }

        encounterRunning = false;

        StopAllCoroutines();

        SetEncounterState(EncounterState.Defeat);

        if (gameStateManager != null)
        {
            gameStateManager.EncounterDefeat();
        }
    }

    private bool UsesSpecificEnemyTarget()
    {
        return CurrentVictoryCondition ==
               VictoryCondition.DefeatSpecificEnemy;
    }

    private bool UsesSurvival()
    {
        return CurrentVictoryCondition ==
               VictoryCondition.SurviveRounds;
    }

    private void SetEncounterState(EncounterState newState)
    {
        currentState = newState;
    }

    public void SetCurrentEncounter(EncounterDefinition encounter)
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

    public bool IsPreparing()
    {
        return currentState == EncounterState.Preparing;
    }

    public bool IsInCombat()
    {
        return currentState == EncounterState.Combat;
    }

    public bool IsFinished()
    {
        return currentState == EncounterState.Victory ||
               currentState == EncounterState.Defeat;
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

            return !roundManager.IsRoundRunning();
        }

        return false;
    }
}
