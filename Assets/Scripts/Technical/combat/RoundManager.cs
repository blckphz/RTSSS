using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Round State")]
    [SerializeField]
    private int currentRound = 1;

    [SerializeField]
    private RoundState currentState =
        RoundState.Setup;

    public event Action<int> OnRoundChanged;
    public event Action<RoundState> OnRoundStateChanged;
    public event Action OnEnemyTurnStarted;

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

    [Header("UI & Settings")]
    [SerializeField]
    private float delayBetweenUnits = 0.1f;

    private WaitForSeconds unitDelay;

    private readonly List<AbilityLogEntry> roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit> cachedUnits =
        new List<AttackUnit>();

    private readonly HashSet<AttackUnit> enemyTurnLockedUnits =
        new HashSet<AttackUnit>();

    private bool roundRunning;

    private void Awake()
    {
        if (combatManager == null)
        {
            combatManager =
                FindObjectOfType<CombatManager>();
        }

        if (gridManager == null)
        {
            gridManager =
                FindObjectOfType<GridManager>();
        }

        if (encounterManager == null)
        {
            encounterManager =
                FindObjectOfType<EncounterManager>();
        }

        if (canvasInfoManager == null)
        {
            canvasInfoManager =
                FindObjectOfType<CanvasInfoManager>();
        }

        if (canvasJuiceManager == null)
        {
            canvasJuiceManager =
                FindObjectOfType<CanvasJuiceManager>();
        }

        if (nextRoundTextManager == null)
        {
            nextRoundTextManager =
                FindObjectOfType<NextRoundTextManager>();
        }

        unitDelay =
            new WaitForSeconds(
                Mathf.Max(
                    0f,
                    delayBetweenUnits
                )
            );

        CombatUtility.SetPlayerInputLocked(false);
    }

    private void OnDestroy()
    {
        CombatUtility.SetPlayerInputLocked(false);
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetRounds()
    {
        StopAllCoroutines();

        currentRound = 1;
        currentState = RoundState.Setup;
        roundRunning = false;

        enemyTurnLockedUnits.Clear();
        roundAbilityLogs.Clear();
        cachedUnits.Clear();

        if (combatManager != null)
        {
            combatManager.ClearEnemyTurnLocks();
        }

        CombatUtility.SetPlayerInputLocked(false);

        OnRoundChanged?.Invoke(
            currentRound
        );

        OnRoundStateChanged?.Invoke(
            currentState
        );

        if (canvasJuiceManager != null)
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }

        Debug.Log(
            "[RoundManager] Reset -> Round 1 Prepare."
        );
    }

    // =========================================================
    // START ROUND
    // =========================================================

    public void StartRound()
    {
        if (roundRunning)
        {
            return;
        }

        if (currentState != RoundState.Setup)
        {
            return;
        }

        if (!IsPlayerOnField())
        {
            Debug.LogWarning(
                "[RoundManager] Cannot start Round " +
                currentRound +
                ". No player on field."
            );

            return;
        }

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            return;
        }

        roundRunning = true;

        CombatUtility.SetPlayerInputLocked(false);

        enemyTurnLockedUnits.Clear();

        if (combatManager != null)
        {
            combatManager.ClearEnemyTurnLocks();
        }

        RefreshCachedUnits();

        ResetAllUnitMovement();

        UpdateAllUnitCooldowns();

        Debug.Log(
            "[RoundManager] STARTING ROUND " +
            currentRound
        );

        StartCoroutine(
            RunRoundPipeline()
        );
    }

    // =========================================================
    // ROUND PIPELINE
    // =========================================================

    private IEnumerator RunRoundPipeline()
    {
        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }

        /*
         * EncounterManager owns encounter spawning.
         *
         * This method must NEVER spawn a survival wave.
         */
        EnsureEnemiesExist();

        SetRoundState(
            RoundState.PlayerAndAllyTurn
        );

        CombatUtility.SetPlayerInputLocked(false);

        if (canvasInfoManager != null)
        {
            canvasInfoManager.RefreshCurrentSelection();
        }

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            EndRound();
            yield break;
        }

        SetRoundState(
            RoundState.EnemyTurn
        );

        OnEnemyTurnStarted?.Invoke();

        CombatUtility.SetPlayerInputLocked(true);

        if (nextRoundTextManager != null)
        {
            while (nextRoundTextManager.IsAnimating)
            {
                yield return null;
            }
        }

        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );

        EndRound();
    }

    // =========================================================
    // PLAYER + ALLY TURN
    // =========================================================

    private IEnumerator ExecutePlayerAndAllyTurn()
    {
        RefreshCachedUnits();

        for (
            int i = 0;
            i < cachedUnits.Count;
            i++
        )
        {
            AttackUnit unit =
                cachedUnits[i];

            if (!IsValidUnit(unit))
            {
                continue;
            }

            Team team =
                unit.GetTeam();

            if (
                team != Team.Player &&
                team != Team.Ally
            )
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

            if (
                encounterManager != null &&
                encounterManager.IsFinished()
            )
            {
                yield break;
            }
        }
    }

    // =========================================================
    // ENEMY TURN
    // =========================================================

    private IEnumerator ExecuteEnemyTurn()
    {
        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (
            enemies == null ||
            enemies.Count == 0
        )
        {
            if (nextRoundTextManager != null)
            {
                yield return StartCoroutine(
                    nextRoundTextManager.HideCinematicBars()
                );

                nextRoundTextManager.ShowPlayerTurn();

                while (
                    nextRoundTextManager.IsAnimating
                )
                {
                    yield return null;
                }
            }

            yield break;
        }

        if (combatManager == null)
        {
            yield break;
        }

        for (
            int i = 0;
            i < enemies.Count;
            i++
        )
        {
            AttackUnit enemy =
                enemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (!CombatUtility.IsAlive(enemy))
            {
                continue;
            }

            if (IsEnemyTurnLocked(enemy))
            {
                continue;
            }

            if (ConditionManager.IsStunned(enemy))
            {
                ConditionManager.ConsumeStunTurn(
                    enemy
                );

                if (delayBetweenUnits > 0f)
                {
                    yield return unitDelay;
                }

                continue;
            }

            if (canvasJuiceManager != null)
            {
                canvasJuiceManager.MoveCameraToUnit(
                    enemy.transform
                );
            }

            yield return null;

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !combatManager.EnemiesMoveAfterRound,
                    combatManager.EnemiesAttackAfterMoving
                )
            );

            yield return null;

            if (
                encounterManager != null &&
                encounterManager.IsFinished()
            )
            {
                if (nextRoundTextManager != null)
                {
                    yield return StartCoroutine(
                        nextRoundTextManager.HideCinematicBars()
                    );
                }

                yield break;
            }

            if (delayBetweenUnits > 0f)
            {
                yield return unitDelay;
            }
        }

        if (nextRoundTextManager != null)
        {
            yield return StartCoroutine(
                nextRoundTextManager.HideCinematicBars()
            );

            nextRoundTextManager.ShowPlayerTurn();

            while (
                nextRoundTextManager.IsAnimating
            )
            {
                yield return null;
            }
        }
    }

    // =========================================================
    // END ROUND
    // =========================================================

    private void EndRound()
    {
        CombatUtility.SetPlayerInputLocked(false);

        if (canvasJuiceManager != null)
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }

        /*
         * IMPORTANT:
         *
         * currentRound is STILL the round that just finished.
         *
         * Example:
         *
         * Round 1 combat finishes.
         * currentRound == 1.
         *
         * EncounterManager checks:
         *
         * 1 / 6
         *
         * Then this method advances to Round 2.
         */
        if (encounterManager != null)
        {
            encounterManager.CheckVictoryAfterRound();

            if (encounterManager.IsFinished())
            {
                roundRunning = false;

                SetRoundState(
                    RoundState.Setup
                );

                return;
            }
        }

        /*
         * The combat round has finished.
         *
         * Advance to the NEXT round's Prepare phase.
         */
        currentRound++;

        roundRunning = false;

        SetRoundState(
            RoundState.Setup
        );

        OnRoundChanged?.Invoke(
            currentRound
        );

        Debug.Log(
            "[RoundManager] Round " +
            (currentRound - 1) +
            " complete. " +
            "Preparing Round " +
            currentRound +
            "."
        );
    }

    // =========================================================
    // ENEMY SAFETY CHECK
    // =========================================================

    private bool EnsureEnemiesExist()
    {
        /*
         * EncounterManager owns enemy spawning
         * during an active encounter.
         */
        if (
            encounterManager != null &&
            encounterManager.IsEncounterRunning()
        )
        {
            return false;
        }

        if (combatManager == null)
        {
            return false;
        }

        if (
            CombatUtility.GetUnitCount(
                Team.Enemy
            ) == 0
        )
        {
            combatManager.CheckForEnemies();

            return true;
        }

        return false;
    }

    // =========================================================
    // CACHE
    // =========================================================

    private void RefreshCachedUnits()
    {
        cachedUnits.Clear();

        List<AttackUnit> units =
            CombatUtility.GetAllAliveUnits();

        if (units == null)
        {
            return;
        }

        for (
            int i = 0;
            i < units.Count;
            i++
        )
        {
            if (units[i] != null)
            {
                cachedUnits.Add(
                    units[i]
                );
            }
        }
    }

    // =========================================================
    // ROUND RESET
    // =========================================================

    private void ResetAllUnitMovement()
    {
        for (
            int i = 0;
            i < cachedUnits.Count;
            i++
        )
        {
            AttackUnit unit =
                cachedUnits[i];

            if (unit == null)
            {
                continue;
            }

            UnitMoveBrain moveBrain =
                unit.GetComponent<UnitMoveBrain>();

            if (moveBrain != null)
            {
                moveBrain.ResetMovement();
            }
        }
    }

    private void UpdateAllUnitCooldowns()
    {
        for (
            int i = 0;
            i < cachedUnits.Count;
            i++
        )
        {
            AttackUnit unit =
                cachedUnits[i];

            if (unit == null)
            {
                continue;
            }

            unit.StartNewRound();
        }
    }

    // =========================================================
    // PLAYER
    // =========================================================

    private bool IsPlayerOnField()
    {
        List<AttackUnit> players =
            CombatUtility.GetUnitsByTeam(
                Team.Player
            );

        if (
            players == null ||
            players.Count == 0
        )
        {
            return false;
        }

        for (
            int i = 0;
            i < players.Count;
            i++
        )
        {
            AttackUnit player =
                players[i];

            if (player == null)
            {
                continue;
            }

            if (!CombatUtility.IsAlive(player))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // =========================================================
    // ENEMY LOCKS
    // =========================================================

    private bool IsEnemyTurnLocked(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return true;
        }

        return enemyTurnLockedUnits.Contains(
            enemy
        );
    }

    public void LockEnemyForCurrentRound(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        enemyTurnLockedUnits.Add(
            enemy
        );
    }

    public void LockEnemiesForCurrentRound(
        List<AttackUnit> enemies
    )
    {
        if (enemies == null)
        {
            return;
        }

        for (
            int i = 0;
            i < enemies.Count;
            i++
        )
        {
            LockEnemyForCurrentRound(
                enemies[i]
            );
        }
    }

    public void UnlockEnemy(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        enemyTurnLockedUnits.Remove(
            enemy
        );
    }

    public int GetLockedEnemyCount()
    {
        return enemyTurnLockedUnits.Count;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool IsValidUnit(
        AttackUnit unit
    )
    {
        return CombatUtility.IsAlive(
            unit
        );
    }

    // =========================================================
    // STATE
    // =========================================================

    private void SetRoundState(
        RoundState newState
    )
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        OnRoundStateChanged?.Invoke(
            currentState
        );
    }

    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsSetupPhase()
    {
        return currentState ==
               RoundState.Setup;
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

    public bool IsEnemyTurn()
    {
        return currentState ==
               RoundState.EnemyTurn;
    }

    // =========================================================
    // ABILITY LOGS
    // =========================================================

    public void LogAbilityUse(
        string unitName,
        string abilityName
    )
    {
        roundAbilityLogs.Add(
            new AbilityLogEntry
            {
                round = currentRound,
                unitName = unitName,
                abilityName = abilityName
            }
        );
    }

    public List<AbilityLogEntry> GetAbilityLogsForRound(
        int round
    )
    {
        return roundAbilityLogs.FindAll(
            log => log.round == round
        );
    }

    public List<AbilityLogEntry> GetAllAbilityLogs()
    {
        return new List<AbilityLogEntry>(
            roundAbilityLogs
        );
    }
}