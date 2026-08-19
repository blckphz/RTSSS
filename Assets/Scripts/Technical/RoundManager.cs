using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    public enum RoundState { Setup, PlayerAndAllyTurn, EnemyTurn }

    [Header("Round State")]
    [SerializeField] private int currentRound = 0;
    [SerializeField] private RoundState currentState = RoundState.Setup;
    [SerializeField] private bool autoBattle = false;

    [Header("Dependencies")]
    [SerializeField] private combatManager combatManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TargetingSystem targetingSystem;
    [SerializeField] private PlayerAction playerAction;

    [Header("UI & Settings")]
    [SerializeField] private Toggle autoBattleToggle;
    [SerializeField] private float delayBetweenAttacks = 0.5f;
    [SerializeField] private bool enableDebugLogs = true;

    private bool roundRunning;

    private void Awake()
    {
        combatManager = combatManager ?? FindFirstObjectByType<combatManager>();
        gridManager = gridManager ?? FindFirstObjectByType<GridManager>();
        targetingSystem = targetingSystem ?? GetComponent<TargetingSystem>() ?? FindFirstObjectByType<TargetingSystem>();
        playerAction = playerAction ?? GetComponent<PlayerAction>() ?? FindFirstObjectByType<PlayerAction>();

        if (autoBattleToggle != null)
        {
            autoBattleToggle.isOn = autoBattle;
            autoBattleToggle.onValueChanged.AddListener(SetAutoBattle);
        }
    }

    private void OnDestroy()
    {
        if (autoBattleToggle != null)
            autoBattleToggle.onValueChanged.RemoveListener(SetAutoBattle);
    }

    private void Update()
    {
        if (autoBattle && currentState == RoundState.Setup && !roundRunning)
        {
            StartRound();
        }
    }

    public void StartRound()
    {
        if (roundRunning || currentState != RoundState.Setup) return;

        currentRound++;
        roundRunning = true;

        DebugLog($"===== ROUND {currentRound} START =====");
        UpdateAllUnitCooldowns();
        StartCoroutine(RunRoundPipeline());
    }

    private IEnumerator RunRoundPipeline()
    {
        gridManager?.CleanupDeadUnits();
        EnsureEnemiesExist();

        // 1. Player & Allies Turn
        currentState = RoundState.PlayerAndAllyTurn;
        yield return StartCoroutine(ExecuteTeamTurn(isPlayerSide: true));

        // 2. Enemy Turn
        currentState = RoundState.EnemyTurn;
        yield return StartCoroutine(ExecuteTeamTurn(isPlayerSide: false));

        // Finish Phase
        DebugLog($"===== ROUND {currentRound} FINISHED =====");
        roundRunning = false;
        currentState = RoundState.Setup;
    }

    private IEnumerator ExecuteTeamTurn(bool isPlayerSide)
    {
        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);
        Team targetTeam = isPlayerSide ? Team.Enemy : Team.Player;

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null || !unit.CanAttack()) continue;

            Team team = unit.GetTeam();
            bool isCorrectTeam = isPlayerSide
                ? (team == Team.Player || team == Team.Ally)
                : (team == Team.Enemy);

            if (!isCorrectTeam) continue;

            GameObject target = targetingSystem.FindNearestTarget(unit, targetTeam);
            if (target == null) continue;

            playerAction.TryExecuteUnitTurn(unit, target);

            yield return new WaitForSeconds(delayBetweenAttacks);
        }
    }

    private void EnsureEnemiesExist()
    {
        if (combatManager == null) return;

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);
        foreach (AttackUnit unit in allUnits)
        {
            if (unit != null && unit.GetTeam() == Team.Enemy)
            {
                HealthManager health = unit.GetHealthManager();
                if (health != null && health.IsAlive()) return; // Living enemy found
            }
        }

        DebugLog("No living enemies found. Triggering enemy spawn.");
        combatManager.CheckForEnemies();
    }

    private void UpdateAllUnitCooldowns()
    {
        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);
        foreach (AttackUnit unit in allUnits)
        {
            unit?.StartNewRound();
        }
    }

    // --- Public API & AutoBattle ---
    public void SetAutoBattle(bool enabled)
    {
        autoBattle = enabled;
        DebugLog($"Auto Battle {(enabled ? "ENABLED" : "DISABLED")}.");
    }

    public void ToggleAutoBattle()
    {
        SetAutoBattle(!autoBattle);
        autoBattleToggle?.SetIsOnWithoutNotify(autoBattle);
    }

    public bool IsSetupPhase() => currentState == RoundState.Setup;
    public bool IsRoundRunning() => roundRunning;
    public int GetCurrentRound() => currentRound;

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[RoundManager] {message}");
    }
}