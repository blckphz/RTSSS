using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    public enum RoundState
    {
        Setup,
        PlayerAndAllyTurn,
        EnemyTurn
    }

    [Serializable]
    public struct AbilityLogEntry
    {
        public int round;
        public string unitName;
        public string abilityName;
    }

    // ============================================================
    // ROUND STATE
    // ============================================================

    [Header("Round State")]

    [SerializeField]
    private int currentRound = 1;

    [SerializeField]
    private RoundState currentState = RoundState.Setup;

    [SerializeField]
    private bool autoBattle = false;

    // ============================================================
    // EVENTS
    // ============================================================

    public event Action<int> OnRoundChanged;

    public event Action<RoundState> OnRoundStateChanged;

    // Fires when the enemy turn begins.
    public event Action OnEnemyTurnStarted;

    // ============================================================
    // DEPENDENCIES
    // ============================================================

    [Header("Dependencies")]

    [SerializeField]
    private CombatManager combatManager;

    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private EncounterManager encounterManager;

    [SerializeField]
    private CanvasInfoManager canvasInfoManager;

    [SerializeField]
    private CanvasJuiceManager canvasJuiceManager;

    [SerializeField]
    private NextRoundTextManager nextRoundTextManager;

    // ============================================================
    // UI / SETTINGS
    // ============================================================

    [Header("UI & Settings")]

    [SerializeField]
    private Toggle autoBattleToggle;

    [SerializeField]
    private float delayBetweenUnits = 0.1f;

    // ============================================================
    // INTERNAL
    // ============================================================

    private readonly List<AbilityLogEntry> roundAbilityLogs = new List<AbilityLogEntry>();
    private readonly List<AttackUnit> cachedUnits = new List<AttackUnit>();
    private readonly HashSet<AttackUnit> enemyTurnLockedUnits = new HashSet<AttackUnit>();

    private WaitForSeconds unitDelay;
    private bool roundRunning;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (encounterManager == null)
        {
            encounterManager = FindFirstObjectByType<EncounterManager>();
        }

        if (canvasInfoManager == null)
        {
            canvasInfoManager = FindFirstObjectByType<CanvasInfoManager>();
        }

        if (canvasJuiceManager == null)
        {
            canvasJuiceManager = FindFirstObjectByType<CanvasJuiceManager>();
        }

        if (nextRoundTextManager == null)
        {
            nextRoundTextManager = FindFirstObjectByType<NextRoundTextManager>();
        }

        unitDelay = new WaitForSeconds(Mathf.Max(0f, delayBetweenUnits));

        if (autoBattleToggle != null)
        {
            autoBattleToggle.isOn = autoBattle;
            autoBattleToggle.onValueChanged.AddListener(SetAutoBattle);
        }

        CombatUtility.SetPlayerInputLocked(false);
    }

    private void OnDestroy()
    {
        if (autoBattleToggle != null)
        {
            autoBattleToggle.onValueChanged.RemoveListener(SetAutoBattle);
        }

        CombatUtility.SetPlayerInputLocked(false);
    }

    private void Update()
    {
        if (autoBattle && currentState == RoundState.Setup && !roundRunning)
        {
            StartRound();
        }
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetRounds()
    {
        StopAllCoroutines();

        currentRound = 1;
        SetRoundState(RoundState.Setup);
        roundRunning = false;

        enemyTurnLockedUnits.Clear();
        roundAbilityLogs.Clear();
        cachedUnits.Clear();

        CombatUtility.SetPlayerInputLocked(false);

        OnRoundChanged?.Invoke(currentRound);

        if (canvasJuiceManager != null)
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }
    }

    // ============================================================
    // START ROUND
    // ============================================================

    public void StartRound()
    {
        if (roundRunning) return;
        if (currentState != RoundState.Setup) return;
        if (!IsPlayerOnField()) return;
        if (encounterManager != null && encounterManager.IsFinished()) return;

        roundRunning = true;

        CombatUtility.SetPlayerInputLocked(false);
        enemyTurnLockedUnits.Clear();

        HashSet<AttackUnit> enemiesExistingBeforeSpawn = CaptureLivingEnemies();

        RefreshCachedUnits();
        ResetAllUnitMovement();
        UpdateAllUnitCooldowns();

        // ========================================================
        // SPAWN NEXT ROUND ENEMIES
        // ========================================================

        if (encounterManager != null &&
            currentRound > 1 &&
            encounterManager.IsEncounterRunning())
        {
            encounterManager.SpawnNextRoundEnemies();

            RefreshCachedUnits();

            LockNewlySpawnedEnemies(enemiesExistingBeforeSpawn);
        }

        StartCoroutine(RunRoundPipeline());
    }

    // ============================================================
    // PLAYER ON FIELD
    // ============================================================

    private bool IsPlayerOnField()
    {
        List<AttackUnit> players = CombatUtility.GetUnitsByTeam(Team.Player);

        if (players == null || players.Count == 0)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            AttackUnit player = players[i];

            if (player == null) continue;
            if (!CombatUtility.IsAlive(player)) continue;

            return true;
        }

        return false;
    }

    // ============================================================
    // CAPTURE LIVING ENEMIES
    // ============================================================

    private HashSet<AttackUnit> CaptureLivingEnemies()
    {
        HashSet<AttackUnit> enemies = new HashSet<AttackUnit>();

        List<AttackUnit> units =
            CombatUtility.GetUnitsByTeam(Team.Enemy);

        if (units == null)
            return enemies;

        for (int i = 0; i < units.Count; i++)
        {
            AttackUnit unit = units[i];

            if (unit == null) continue;
            if (!CombatUtility.IsAlive(unit)) continue;

            enemies.Add(unit);
        }

        return enemies;
    }

    // ============================================================
    // LOCK NEW ENEMIES
    // ============================================================

    private void LockNewlySpawnedEnemies(
        HashSet<AttackUnit> enemiesExistingBeforeSpawn)
    {
        if (enemiesExistingBeforeSpawn == null)
            return;

        List<AttackUnit> currentEnemies =
            CombatUtility.GetUnitsByTeam(Team.Enemy);

        if (currentEnemies == null)
            return;

        for (int i = 0; i < currentEnemies.Count; i++)
        {
            AttackUnit enemy = currentEnemies[i];

            if (enemy == null) continue;
            if (!CombatUtility.IsAlive(enemy)) continue;
            if (enemiesExistingBeforeSpawn.Contains(enemy)) continue;

            enemyTurnLockedUnits.Add(enemy);
        }
    }

    // ============================================================
    // ENEMY TURN LOCK
    // ============================================================

    private bool IsEnemyTurnLocked(AttackUnit enemy)
    {
        if (enemy == null)
            return true;

        return enemyTurnLockedUnits.Contains(enemy);
    }

    // ============================================================
    // RESET MOVEMENT
    // ============================================================

    private void ResetAllUnitMovement()
    {
        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit = cachedUnits[i];

            if (unit == null)
                continue;

            UnitMoveBrain moveBrain =
                unit.GetComponent<UnitMoveBrain>();

            if (moveBrain != null)
            {
                moveBrain.ResetMovement();
            }
        }
    }

    // ============================================================
    // ROUND PIPELINE
    // ============================================================

    private IEnumerator RunRoundPipeline()
    {
        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }

        EnsureEnemiesExist();

        // ========================================================
        // PLAYER / ALLY TURN
        // ========================================================

        SetRoundState(RoundState.PlayerAndAllyTurn);

        CombatUtility.SetPlayerInputLocked(false);

        if (canvasInfoManager != null)
        {
            canvasInfoManager.RefreshCurrentSelection();
        }

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );

        // ========================================================
        // ENCOUNTER FINISHED
        // ========================================================

        if (encounterManager != null &&
            encounterManager.IsFinished())
        {
            EndRound();
            yield break;
        }

        // ========================================================
        // ENEMY TURN
        // ========================================================

        SetRoundState(RoundState.EnemyTurn);

        // Tell anything listening that the enemy turn has officially started.
        // RoundUIManager receives this state change first, activates
        // the cinematic GameObject and starts
        // NextRoundTextManager.ShowEnemyTurn().
        OnEnemyTurnStarted?.Invoke();

        CombatUtility.SetPlayerInputLocked(true);

        // ========================================================
        // WAIT FOR ENEMY TURN UI
        // ========================================================

        if (nextRoundTextManager != null)
        {
            while (nextRoundTextManager.IsAnimating)
            {
                yield return null;
            }
        }

        // ========================================================
        // ENEMIES CAN NOW ACT
        // ========================================================

        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );

        EndRound();
    }

    // ============================================================
    // SET ROUND STATE
    // ============================================================

    private void SetRoundState(RoundState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        OnRoundStateChanged?.Invoke(currentState);
    }

    // ============================================================
    // PLAYER / ALLY TURN
    // ============================================================

    private IEnumerator ExecutePlayerAndAllyTurn()
    {
        RefreshCachedUnits();

        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit = cachedUnits[i];

            if (!IsValidUnit(unit))
                continue;

            Team team = unit.GetTeam();

            if (team != Team.Player &&
                team != Team.Ally)
            {
                continue;
            }

            yield return StartCoroutine(
                CombatUtility.ExecuteUnitTurnCoroutine(
                    unit,
                    gridManager
                )
            );

            if (delayBetweenUnits > 0f)
            {
                yield return unitDelay;
            }

            if (encounterManager != null &&
                encounterManager.IsFinished())
            {
                yield break;
            }
        }
    }

    // ============================================================
    // ENEMY TURN
    // ============================================================

    private IEnumerator ExecuteEnemyTurn()
    {
        if (combatManager == null)
            yield break;

        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(Team.Enemy);

        if (enemies == null || enemies.Count == 0)
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy = enemies[i];

            // ====================================================
            // VALIDATION
            // ====================================================

            if (enemy == null)
                continue;

            if (!CombatUtility.IsAlive(enemy))
                continue;

            // ====================================================
            // NEWLY SPAWNED ENEMY
            // ====================================================

            if (IsEnemyTurnLocked(enemy))
                continue;

            // ====================================================
            // STUN
            // ====================================================

            if (ConditionManager.IsStunned(enemy))
            {
                int remainingTurns =
                    ConditionManager.GetStunRemaining(enemy);

                Debug.Log(
                    $"[STUN] {enemy.name} skips its turn. " +
                    $"Remaining before skip: {remainingTurns}"
                );

                ConditionManager.ConsumeStunTurn(enemy);

                if (delayBetweenUnits > 0f)
                {
                    yield return unitDelay;
                }

                continue;
            }

            // ====================================================
            // CAMERA FOLLOW
            // ====================================================

            if (canvasJuiceManager != null)
            {
                canvasJuiceManager.MoveCameraToUnit(
                    enemy.transform
                );
            }

            // Let the camera begin moving.
            yield return null;

            // ====================================================
            // ENEMY ACTION
            // ====================================================

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !combatManager.EnemiesMoveAfterRound,
                    combatManager.EnemiesAttackAfterMoving
                )
            );

            yield return null;

            // ====================================================
            // ENCOUNTER FINISHED
            // ====================================================

            if (encounterManager != null &&
                encounterManager.IsFinished())
            {
                if (nextRoundTextManager != null)
                {
                    yield return StartCoroutine(
                        nextRoundTextManager.HideCinematicBars()
                    );
                }

                yield break;
            }

            // ====================================================
            // DELAY
            // ====================================================

            if (delayBetweenUnits > 0f)
            {
                yield return unitDelay;
            }
        }

        // ========================================================
        // ENEMY TURN FINISHED
        // ========================================================

        if (nextRoundTextManager != null)
        {
            // First hide/retract the cinematic bars.
            yield return StartCoroutine(
                nextRoundTextManager.HideCinematicBars()
            );

            // ====================================================
            // SHOW PLAYER TURN
            // ====================================================

            nextRoundTextManager.ShowPlayerTurn();

            // Wait until the PLAYER TURN banner animation finishes.
            while (nextRoundTextManager.IsAnimating)
            {
                yield return null;
            }
        }
    }

    // ============================================================
    // END ROUND
    // ============================================================

    private void EndRound()
    {
        CombatUtility.SetPlayerInputLocked(false);

        // Stop enemy camera follow and return to normal position.
        if (canvasJuiceManager != null)
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }

        // ========================================================
        // VICTORY
        // ========================================================

        if (encounterManager != null)
        {
            encounterManager.CheckVictoryAfterRound();

            if (encounterManager.IsFinished())
            {
                roundRunning = false;

                SetRoundState(RoundState.Setup);

                return;
            }
        }

        // ========================================================
        // NEXT ROUND
        // ========================================================

        currentRound++;

        OnRoundChanged?.Invoke(currentRound);

        SetRoundState(RoundState.Setup);

        roundRunning = false;
    }

    // ============================================================
    // ENSURE ENEMIES
    // ============================================================

    private bool EnsureEnemiesExist()
    {
        if (encounterManager != null &&
            encounterManager.IsEncounterRunning())
        {
            return false;
        }

        if (combatManager == null)
            return false;

        if (CombatUtility.GetUnitCount(Team.Enemy) == 0)
        {
            combatManager.CheckForEnemies();

            return true;
        }

        return false;
    }

    // ============================================================
    // REFRESH UNITS
    // ============================================================

    private void RefreshCachedUnits()
    {
        cachedUnits.Clear();

        List<AttackUnit> units =
            CombatUtility.GetAllAliveUnits();

        if (units == null)
            return;

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null)
            {
                cachedUnits.Add(units[i]);
            }
        }
    }

    // ============================================================
    // COOLDOWNS
    // ============================================================

    private void UpdateAllUnitCooldowns()
    {
        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit = cachedUnits[i];

            if (unit == null)
                continue;

            unit.StartNewRound();
        }
    }

    // ============================================================
    // VALID UNIT
    // ============================================================

    private bool IsValidUnit(AttackUnit unit)
    {
        return CombatUtility.IsAlive(unit);
    }

    // ============================================================
    // AUTO BATTLE
    // ============================================================

    public void SetAutoBattle(bool enabled)
    {
        autoBattle = enabled;
    }

    public void ToggleAutoBattle()
    {
        SetAutoBattle(!autoBattle);

        if (autoBattleToggle != null)
        {
            autoBattleToggle.SetIsOnWithoutNotify(autoBattle);
        }
    }

    // ============================================================
    // ABILITY LOGGING
    // ============================================================

    private void LogAbilityUse(
        string unitName,
        string abilityName)
    {
        AbilityLogEntry entry = new AbilityLogEntry
        {
            round = currentRound,
            unitName = unitName,
            abilityName = abilityName
        };

        roundAbilityLogs.Add(entry);
    }

    // ============================================================
    // ACCESSORS
    // ============================================================

    public List<AbilityLogEntry> GetAbilityLogsForRound(int round)
    {
        return roundAbilityLogs.FindAll(
            log => log.round == round
        );
    }

    public List<AbilityLogEntry> GetAllAbilityLogs()
    {
        return roundAbilityLogs;
    }

    public bool IsSetupPhase()
    {
        return currentState == RoundState.Setup;
    }

    public bool IsRoundRunning()
    {
        return roundRunning;
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    public RoundState GetCurrentState()
    {
        return currentState;
    }

    public bool HasPlayerOnField()
    {
        return IsPlayerOnField();
    }
}