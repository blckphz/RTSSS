
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Round State")]
    [SerializeField] private int currentRound = 1;
    [SerializeField] private RoundState currentState = RoundState.Setup;

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

    public event Action<int> OnRoundChanged;
    public event Action<RoundState> OnRoundStateChanged;
    public event Action OnEnemyTurnStarted;
    public event Action<bool> OnBossWaterStatusChecked;

    [Header("References")]
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private CanvasInfoManager canvasInfoManager;
    [SerializeField] private CanvasJuiceManager canvasJuiceManager;
    [SerializeField] private NextRoundTextManager nextRoundTextManager;
    [SerializeField] private GridHighlightManager gridHighlightManager;
    [SerializeField] private LineWalkPreview lineWalkPreview;

    [Header("Water Check")]
    [SerializeField] private BossLobsterBrain bossLobster;

    [Header("Settings")]
    [SerializeField] private float delayBetweenUnits = 0.1f;

    private WaitForSeconds unitDelay;

    private readonly List<AbilityLogEntry> roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit> cachedUnits =
        new List<AttackUnit>();

    private bool roundRunning;

    private void Awake()
    {
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (encounterManager == null)
            encounterManager = FindFirstObjectByType<EncounterManager>();

        if (canvasInfoManager == null)
            canvasInfoManager = FindFirstObjectByType<CanvasInfoManager>();

        if (canvasJuiceManager == null)
            canvasJuiceManager = FindFirstObjectByType<CanvasJuiceManager>();

        if (nextRoundTextManager == null)
            nextRoundTextManager = FindFirstObjectByType<NextRoundTextManager>();

        if (gridHighlightManager == null)
            gridHighlightManager = FindFirstObjectByType<GridHighlightManager>();

        if (lineWalkPreview == null)
            lineWalkPreview = FindFirstObjectByType<LineWalkPreview>();

        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();

        unitDelay = new WaitForSeconds(
            Mathf.Max(0f, delayBetweenUnits)
        );

        EnablePlayerTurnInteraction();
    }

    private void OnDestroy()
    {
        EnablePlayerTurnInteraction();
    }

    // ============================================================
    // WATER CHECK
    // ============================================================

    private void CheckBossOnWater()
    {
        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();

        if (bossLobster == null)
        {
            Debug.LogWarning(
                "[RoundManager] BossLobsterBrain was not found."
            );
            return;
        }

        bool isOnWater = bossLobster.IsOnWater();

        Debug.Log("[RoundManager] Boss on water: " + isOnWater);

        OnBossWaterStatusChecked?.Invoke(isOnWater);
    }

    // ============================================================
    // START ROUND
    // ============================================================

    public void StartRound()
    {
        if (roundRunning)
            return;

        if (currentState != RoundState.Setup)
            return;

        if (!IsPlayerOnField())
        {
            Debug.LogWarning(
                "[RoundManager] Cannot start round. " +
                "No living player unit is on the field."
            );
            return;
        }

        if (encounterManager != null && encounterManager.IsFinished())
            return;

        roundRunning = true;

        CheckBossOnWater();
        EnablePlayerTurnInteraction();
        RefreshCachedUnits();
        ResetAllUnitMovement();
        UpdateAllUnitCooldowns();

        Debug.Log("[RoundManager] Starting Round " + currentRound);

        StartCoroutine(RunRoundPipeline());
    }

    // ============================================================
    // ROUND PIPELINE
    // ============================================================

    private IEnumerator RunRoundPipeline()
    {
        if (gridManager != null)
            gridManager.CleanupDeadUnits();

        EnsureEnemiesExist();

        SetRoundState(RoundState.PlayerAndAllyTurn);
        EnablePlayerTurnInteraction();

        if (canvasInfoManager != null)
            canvasInfoManager.RefreshCurrentSelection();

        yield return StartCoroutine(ExecutePlayerAndAllyTurn());

        if (encounterManager != null && encounterManager.IsFinished())
        {
            EndRound();
            yield break;
        }

        SetRoundState(RoundState.EnemyTurn);
        DisablePlayerTurnInteraction();

        OnEnemyTurnStarted?.Invoke();

        if (nextRoundTextManager != null)
        {
            while (nextRoundTextManager.IsAnimating)
                yield return null;
        }

        yield return StartCoroutine(ExecuteEnemyTurn());

        EnablePlayerTurnInteraction();
        EndRound();
    }

    // ============================================================
    // PLAYER + ALLY TURN
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

            if (team != Team.Player && team != Team.Ally)
                continue;

            if (!HasAnyAbility(unit))
            {
                Debug.Log(
                    "[RoundManager] Skipping unit with no abilities: " +
                    unit.name,
                    unit
                );
                continue;
            }

            yield return StartCoroutine(
                CombatUtility.ExecuteUnitTurnCoroutine(unit, gridManager)
            );

            if (delayBetweenUnits > 0f)
                yield return unitDelay;

            if (encounterManager != null && encounterManager.IsFinished())
                yield break;
        }
    }

    // ============================================================
    // ENEMY TURN
    // ============================================================

    private IEnumerator ExecuteEnemyTurn()
    {
        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(Team.Enemy);

        if (enemies == null || enemies.Count == 0)
        {
            if (nextRoundTextManager != null)
            {
                yield return StartCoroutine(
                    nextRoundTextManager.HideCinematicBars()
                );

                nextRoundTextManager.ShowPlayerTurn();

                while (nextRoundTextManager.IsAnimating)
                    yield return null;
            }

            if (combatManager != null)
                combatManager.ClearEnemyTurnLocks();

            yield break;
        }

        if (combatManager == null)
        {
            Debug.LogWarning(
                "[RoundManager] CombatManager is missing. " +
                "Cannot execute enemy turn."
            );
            yield break;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy = enemies[i];

            if (enemy == null || !CombatUtility.IsAlive(enemy))
                continue;

            if (combatManager.IsEnemyLocked(enemy))
                continue;

            if (ConditionManager.IsStunned(enemy))
            {
                ConditionManager.ConsumeStunTurn(enemy);

                if (delayBetweenUnits > 0f)
                    yield return unitDelay;

                continue;
            }

            if (canvasJuiceManager != null)
                canvasJuiceManager.MoveCameraToUnit(enemy.transform);

            yield return null;

            BossLobsterBrain bossBrain =
                enemy.GetComponent<BossLobsterBrain>();

            // A retreating boss must not execute its normal AI.
            if (bossBrain != null && bossBrain.IsRetreating())
            {
                yield return StartCoroutine(
                    ExecuteBossRetreat(bossBrain)
                );
            }
            else
            {
                if (!HasAnyAbility(enemy))
                {
                    Debug.Log(
                        "[RoundManager] Skipping enemy with no abilities: " +
                        enemy.name,
                        enemy
                    );
                    continue;
                }

                yield return StartCoroutine(
                    CombatUtility.ExecuteEnemyTurn(
                        enemy,
                        !combatManager.EnemiesMoveAfterRound,
                        combatManager.EnemiesAttackAfterMoving
                    )
                );
            }

            yield return null;

            if (encounterManager != null && encounterManager.IsFinished())
            {
                combatManager.ClearEnemyTurnLocks();

                if (nextRoundTextManager != null)
                {
                    yield return StartCoroutine(
                        nextRoundTextManager.HideCinematicBars()
                    );
                }

                yield break;
            }

            if (delayBetweenUnits > 0f)
                yield return unitDelay;
        }

        if (nextRoundTextManager != null)
        {
            yield return StartCoroutine(
                nextRoundTextManager.HideCinematicBars()
            );

            nextRoundTextManager.ShowPlayerTurn();

            while (nextRoundTextManager.IsAnimating)
                yield return null;
        }

        combatManager.ClearEnemyTurnLocks();
    }

    // ============================================================
    // BOSS RETREAT
    // ============================================================

    private IEnumerator ExecuteBossRetreat(BossLobsterBrain boss)
    {
        if (boss == null || !boss.IsRetreating())
            yield break;

        UnitMoveBrain moveBrain = boss.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            Debug.LogWarning(
                "[RoundManager] Retreating boss has no UnitMoveBrain.",
                boss
            );
            yield break;
        }

        Vector2Int spawnTile = boss.GetSpawnTile();

        Debug.Log(
            $"[RoundManager] Boss '{boss.name}' retreat destination: {spawnTile}.",
            boss
        );

        yield return StartCoroutine(boss.TryRetreat());

        // Don't advance to another enemy while the boss is still moving.
        while (moveBrain != null && moveBrain.IsMoving())
            yield return null;

        if (boss != null && boss.IsRetreating())
        {
            Debug.Log(
                "[RoundManager] Boss remains in retreat mode and can continue " +
                "on a later turn if it ran out of movement or its path was blocked.",
                boss
            );
        }
    }

    // ============================================================
    // PLAYER INTERACTION CONTROL
    // ============================================================

    private void DisablePlayerTurnInteraction()
    {
        CombatUtility.SetPlayerInputLocked(true);

        if (gridHighlightManager != null)
            gridHighlightManager.SetPlayerHighlightsDisabled(true);

        if (lineWalkPreview != null)
            lineWalkPreview.SetPlayerWalkPreviewDisabled(true);
    }

    private void EnablePlayerTurnInteraction()
    {
        CombatUtility.SetPlayerInputLocked(false);

        if (gridHighlightManager != null)
            gridHighlightManager.SetPlayerHighlightsDisabled(false);

        if (lineWalkPreview != null)
            lineWalkPreview.SetPlayerWalkPreviewDisabled(false);
    }

    // ============================================================
    // END ROUND
    // ============================================================

    private void EndRound()
    {
        EnablePlayerTurnInteraction();

        if (canvasJuiceManager != null)
            canvasJuiceManager.MoveCameraToNormalPosition();

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

        currentRound++;
        roundRunning = false;

        SetRoundState(RoundState.Setup);
        OnRoundChanged?.Invoke(currentRound);

        Debug.Log(
            "[RoundManager] Round " + (currentRound - 1) +
            " complete. Preparing Round " + currentRound + "."
        );
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetRounds()
    {
        StopAllCoroutines();

        currentRound = 1;
        currentState = RoundState.Setup;
        roundRunning = false;

        roundAbilityLogs.Clear();
        cachedUnits.Clear();

        if (combatManager != null)
            combatManager.ClearEnemyTurnLocks();

        EnablePlayerTurnInteraction();

        if (gridHighlightManager != null)
            gridHighlightManager.ClearAllHighlights();

        if (lineWalkPreview != null)
            lineWalkPreview.ClearPreview();

        if (canvasJuiceManager != null)
            canvasJuiceManager.MoveCameraToNormalPosition();

        OnRoundChanged?.Invoke(currentRound);
        OnRoundStateChanged?.Invoke(currentState);
    }

    // ============================================================
    // CACHE UNITS
    // ============================================================

    private void RefreshCachedUnits()
    {
        cachedUnits.Clear();

        List<AttackUnit> units = CombatUtility.GetAllAliveUnits();

        if (units == null)
            return;

        for (int i = 0; i < units.Count; i++)
        {
            AttackUnit unit = units[i];

            if (unit != null)
                cachedUnits.Add(unit);
        }
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

            UnitMoveBrain moveBrain = unit.GetComponent<UnitMoveBrain>();

            if (moveBrain != null)
                moveBrain.ResetMovement();
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

            turretbehav turret = unit.GetComponent<turretbehav>();

            if (turret != null)
                turret.ConsumeChainLightningCharge();
        }
    }

    // ============================================================
    // ENSURE ENEMIES
    // ============================================================

    private bool EnsureEnemiesExist()
    {
        if (encounterManager != null && encounterManager.IsEncounterRunning())
            return false;

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
    // PLAYER CHECK
    // ============================================================

    private bool IsPlayerOnField()
    {
        List<AttackUnit> players = CombatUtility.GetUnitsByTeam(Team.Player);

        if (players == null || players.Count == 0)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            AttackUnit player = players[i];

            if (player == null)
                continue;

            if (!CombatUtility.IsAlive(player))
                continue;

            return true;
        }

        return false;
    }

    // ============================================================
    // UNIT VALIDATION
    // ============================================================

    private bool IsValidUnit(AttackUnit unit)
    {
        return CombatUtility.IsAlive(unit);
    }

    // ============================================================
    // ABILITY CHECK
    // ============================================================

    private bool HasAnyAbility(AttackUnit unit)
    {
        if (unit == null)
            return false;

        return unit.GetAbilityCount() > 0;
    }

    // ============================================================
    // ROUND STATE
    // ============================================================

    private void SetRoundState(RoundState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnRoundStateChanged?.Invoke(currentState);
    }

    // ============================================================
    // PUBLIC STATE
    // ============================================================

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

    public bool IsEnemyTurn()
    {
        return currentState == RoundState.EnemyTurn;
    }

    public bool HasPlayerOnField()
    {
        return IsPlayerOnField();
    }

    // ============================================================
    // ABILITY LOG
    // ============================================================

    public void LogAbilityUse(string unitName, string abilityName)
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

    public List<AbilityLogEntry> GetAbilityLogsForRound(int round)
    {
        return roundAbilityLogs.FindAll(log => log.round == round);
    }

    public List<AbilityLogEntry> GetAllAbilityLogs()
    {
        return new List<AbilityLogEntry>(roundAbilityLogs);
    }
}
