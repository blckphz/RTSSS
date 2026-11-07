
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
    [SerializeField] private WaterManager waterManager;

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

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (combatManager == null)
            combatManager = FindAnyObjectByType<CombatManager>();

        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();

        if (encounterManager == null)
            encounterManager = FindAnyObjectByType<EncounterManager>();

        if (canvasInfoManager == null)
            canvasInfoManager = FindAnyObjectByType<CanvasInfoManager>();

        if (canvasJuiceManager == null)
            canvasJuiceManager = FindAnyObjectByType<CanvasJuiceManager>();

        if (nextRoundTextManager == null)
            nextRoundTextManager = FindAnyObjectByType<NextRoundTextManager>();

        if (gridHighlightManager == null)
            gridHighlightManager = FindAnyObjectByType<GridHighlightManager>();

        if (lineWalkPreview == null)
            lineWalkPreview = FindAnyObjectByType<LineWalkPreview>();

        if (waterManager == null)
            waterManager = FindAnyObjectByType<WaterManager>();

        if (bossLobster == null)
            bossLobster = FindAnyObjectByType<BossLobsterBrain>();

        unitDelay = new WaitForSeconds(
            Mathf.Max(0f, delayBetweenUnits)
        );

        EnablePlayerTurnInteraction();
    }

    private void OnDestroy()
    {
        EnablePlayerTurnInteraction();
    }

    // ==================================================
    // START ROUND
    // ==================================================

    public void StartRound()
    {
        if (roundRunning)
            return;

        if (currentState != RoundState.Setup)
            return;

        if (!IsPlayerOnField())
            return;

        if (encounterManager != null &&
            encounterManager.IsFinished())
        {
            return;
        }

        roundRunning = true;

        EnablePlayerTurnInteraction();
        RefreshCachedUnits();
        ResetAllUnitMovement();
        UpdateAllUnitCooldowns();

        StartCoroutine(RunRoundPipeline());
    }

    // ==================================================
    // ROUND PIPELINE
    // ==================================================

    private IEnumerator RunRoundPipeline()
    {
        if (gridManager != null)
            gridManager.CleanupDeadUnits();

        EnsureEnemiesExist();

        SetRoundState(RoundState.PlayerAndAllyTurn);
        EnablePlayerTurnInteraction();

        if (canvasInfoManager != null)
            canvasInfoManager.RefreshCurrentSelection();

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn());

        if (encounterManager != null &&
            encounterManager.IsFinished())
        {
            EndRound();
            yield break;
        }

        SetRoundState(RoundState.EnemyTurn);
        DisablePlayerTurnInteraction();

        if (waterManager != null)
            waterManager.AdvanceTide();

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

    // ==================================================
    // PLAYER + ALLY TURN
    // ==================================================

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

            if (!HasAnyAbility(unit))
                continue;

            yield return StartCoroutine(
                CombatUtility.ExecuteUnitTurnCoroutine(
                    unit,
                    gridManager)
            );

            if (delayBetweenUnits > 0f)
                yield return unitDelay;

            if (encounterManager != null &&
                encounterManager.IsFinished())
            {
                yield break;
            }
        }
    }

    // ==================================================
    // ENEMY TURN
    // ==================================================

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
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy = enemies[i];

            if (enemy == null ||
                !CombatUtility.IsAlive(enemy))
            {
                continue;
            }

            if (enemy.GetComponent<EggBehav>() != null)
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

            BossLobsterBrain bossBrain =
                enemy.GetComponent<BossLobsterBrain>();

            bool isRetreatingBoss =
                bossBrain != null &&
                bossBrain.IsRetreating();

            if (!isRetreatingBoss && !HasAnyAbility(enemy))
                continue;

            if (canvasJuiceManager != null)
                canvasJuiceManager.MoveCameraToUnit(
                    enemy.transform);

            yield return null;

            if (isRetreatingBoss)
            {
                // ======================================
                // RETREATING LOBSTER TURN
                // ======================================

                // Use abilities BEFORE retreat movement.
                // EggAbil can therefore detect that the
                // lobster is retreating and target tiles
                // toward its pond.

                UnitAttackBrain attackBrain =
                    enemy.GetComponent<UnitAttackBrain>();

                if (attackBrain != null &&
                    !enemy.IsDead())
                {
                    Debug.Log(
                        "[RoundManager] Retreating lobster is using abilities."
                    );

                    yield return attackBrain
                        .UseAllAvailableAbilitiesCoroutine();
                }

                // Check that the encounter is still active.
                if (encounterManager != null &&
                    encounterManager.IsFinished())
                {
                    combatManager.ClearEnemyTurnLocks();
                    yield break;
                }

                // Move toward the pond after using abilities.
                if (!enemy.IsDead() &&
                    bossBrain != null &&
                    bossBrain.IsRetreating())
                {
                    Debug.Log(
                        "[RoundManager] Retreating lobster is moving toward the pond."
                    );

                    yield return StartCoroutine(
                        ExecuteBossRetreat(bossBrain)
                    );
                }
            }
            else
            {
                // ======================================
                // NORMAL ENEMY TURN
                // ======================================

                yield return StartCoroutine(
                    CombatUtility.ExecuteEnemyTurn(
                        enemy,
                        !combatManager.EnemiesMoveAfterRound,
                        combatManager.EnemiesAttackAfterMoving
                    )
                );
            }

            yield return null;

            if (encounterManager != null &&
                encounterManager.IsFinished())
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

    // ==================================================
    // BOSS RETREAT
    // ==================================================

    private IEnumerator ExecuteBossRetreat(
        BossLobsterBrain boss)
    {
        if (boss == null || !boss.IsRetreating())
            yield break;

        UnitMoveBrain moveBrain =
            boss.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
            yield break;

        yield return StartCoroutine(boss.TryRetreat());

        while (moveBrain != null && moveBrain.IsMoving())
            yield return null;
    }

    // ==================================================
    // PLAYER INTERACTION CONTROL
    // ==================================================

    private void DisablePlayerTurnInteraction()
    {
        CombatUtility.SetPlayerInputLocked(true);

        if (gridHighlightManager != null)
        {
            gridHighlightManager.SetPlayerHighlightsDisabled(true);
        }

        if (lineWalkPreview != null)
        {
            lineWalkPreview.SetPlayerWalkPreviewDisabled(true);
        }
    }

    private void EnablePlayerTurnInteraction()
    {
        CombatUtility.SetPlayerInputLocked(false);

        if (gridHighlightManager != null)
        {
            gridHighlightManager.SetPlayerHighlightsDisabled(false);
        }

        if (lineWalkPreview != null)
        {
            lineWalkPreview.SetPlayerWalkPreviewDisabled(false);
        }
    }

    // ==================================================
    // END ROUND
    // ==================================================

    private void EndRound()
    {
        EnablePlayerTurnInteraction();

        if (canvasJuiceManager != null)
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }

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

        // ==================================================
        // BOSS LOBSTER WATER HEALING
        // ==================================================

        if (bossLobster == null)
        {
            bossLobster =
                FindAnyObjectByType<BossLobsterBrain>();
        }

        if (bossLobster != null)
        {
            bossLobster.RefreshWaterStatus();

            OnBossWaterStatusChecked?.Invoke(
                bossLobster.IsOnWater());

            bossLobster.HealFromWater();
        }

        currentRound++;
        roundRunning = false;

        SetRoundState(RoundState.Setup);
        OnRoundChanged?.Invoke(currentRound);
    }

    // ==================================================
    // RESET
    // ==================================================

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
        {
            canvasJuiceManager.MoveCameraToNormalPosition();
        }

        OnRoundChanged?.Invoke(currentRound);
        OnRoundStateChanged?.Invoke(currentState);
    }

    // ==================================================
    // CACHE UNITS
    // ==================================================

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
                cachedUnits.Add(units[i]);
        }
    }

    // ==================================================
    // RESET MOVEMENT
    // ==================================================

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
                moveBrain.ResetMovement();
        }
    }

    // ==================================================
    // COOLDOWNS
    // ==================================================

    private void UpdateAllUnitCooldowns()
    {
        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit = cachedUnits[i];

            if (unit == null)
                continue;

            unit.StartNewRound();

            turretbehav turret =
                unit.GetComponent<turretbehav>();

            if (turret != null)
                turret.ConsumeChainLightningCharge();
        }
    }

    // ==================================================
    // ENSURE ENEMIES
    // ==================================================

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

    // ==================================================
    // PLAYER CHECK
    // ==================================================

    private bool IsPlayerOnField()
    {
        List<AttackUnit> players =
            CombatUtility.GetUnitsByTeam(Team.Player);

        if (players == null || players.Count == 0)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            AttackUnit player = players[i];

            if (player != null &&
                CombatUtility.IsAlive(player))
            {
                return true;
            }
        }

        return false;
    }

    // ==================================================
    // UNIT VALIDATION
    // ==================================================

    private bool IsValidUnit(AttackUnit unit)
    {
        return CombatUtility.IsAlive(unit);
    }

    // ==================================================
    // ABILITY CHECK
    // ==================================================

    private bool HasAnyAbility(AttackUnit unit)
    {
        return unit != null &&
               unit.GetAbilityCount() > 0;
    }

    // ==================================================
    // ROUND STATE
    // ==================================================

    private void SetRoundState(RoundState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnRoundStateChanged?.Invoke(currentState);
    }

    // ==================================================
    // PUBLIC STATE
    // ==================================================

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

    // ==================================================
    // ABILITY LOG
    // ==================================================

    public void LogAbilityUse(
        string unitName,
        string abilityName)
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
        int round)
    {
        return roundAbilityLogs.FindAll(
            log => log.round == round);
    }

    public List<AbilityLogEntry> GetAllAbilityLogs()
    {
        return new List<AbilityLogEntry>(
            roundAbilityLogs);
    }
}
