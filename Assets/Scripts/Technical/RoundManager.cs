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

    // ==================================================
    // ROUND STATE
    // ==================================================

    [Header("Round State")]
    [SerializeField]
    private int currentRound = 0;

    [SerializeField]
    private RoundState currentState =
        RoundState.Setup;

    [SerializeField]
    private bool autoBattle = false;

    // ==================================================
    // DEPENDENCIES
    // ==================================================

    [Header("Dependencies")]
    [SerializeField]
    private CombatManager combatManager;

    [SerializeField]
    private GridManager gridManager;

    // ==================================================
    // UI / SETTINGS
    // ==================================================

    [Header("UI & Settings")]
    [SerializeField]
    private Toggle autoBattleToggle;

    [SerializeField]
    private float delayBetweenUnits = 0.5f;

    // ==================================================
    // INTERNAL
    // ==================================================

    private readonly List<AbilityLogEntry>
        roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit>
        cachedUnits =
        new List<AttackUnit>();

    private WaitForSeconds unitDelay;

    private bool roundRunning;

    // ==================================================
    // EVENTS
    // ==================================================

    public event Action<AbilityLogEntry>
        OnAbilityUsed;

    // ==================================================
    // UNITY
    // ==================================================

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

        unitDelay =
            new WaitForSeconds(
                Mathf.Max(
                    0f,
                    delayBetweenUnits
                )
            );

        if (autoBattleToggle != null)
        {
            autoBattleToggle.isOn =
                autoBattle;

            autoBattleToggle.onValueChanged.AddListener(
                SetAutoBattle
            );
        }
    }

    private void OnDestroy()
    {
        if (autoBattleToggle != null)
        {
            autoBattleToggle.onValueChanged.RemoveListener(
                SetAutoBattle
            );
        }
    }

    private void Update()
    {
        if (autoBattle &&
            currentState == RoundState.Setup &&
            !roundRunning)
        {
            StartRound();
        }
    }

    // ==================================================
    // START ROUND
    // ==================================================

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

        currentRound++;

        roundRunning = true;

        RefreshCachedUnits();

        UpdateAllUnitCooldowns();

        StartCoroutine(
            RunRoundPipeline()
        );
    }

    // ==================================================
    // ROUND PIPELINE
    // ==================================================

    private IEnumerator RunRoundPipeline()
    {
        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }

        EnsureEnemiesExist();

        // ------------------------------------------
        // PLAYER / ALLY
        // ------------------------------------------

        currentState =
            RoundState.PlayerAndAllyTurn;

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );

        // ------------------------------------------
        // ENEMY
        // ------------------------------------------

        currentState =
            RoundState.EnemyTurn;

        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );

        // ------------------------------------------
        // END
        // ------------------------------------------

        currentState =
            RoundState.Setup;

        roundRunning = false;
    }

    // ==================================================
    // PLAYER / ALLY TURN
    // ==================================================

    private IEnumerator ExecutePlayerAndAllyTurn()
    {
        RefreshCachedUnits();

        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit =
                cachedUnits[i];

            if (!IsValidUnit(unit))
            {
                continue;
            }

            Team team =
                unit.GetTeam();

            if (team != Team.Player &&
                team != Team.Ally)
            {
                continue;
            }

            string result =
                CombatUtility.ExecuteUnitTurn(
                    unit,
                    gridManager
                );

            if (!string.IsNullOrEmpty(result))
            {
                LogAbilityUse(
                    unit.name,
                    result
                );
            }

            yield return unitDelay;
        }
    }

    // ==================================================
    // ENEMY TURN
    // ==================================================

    private IEnumerator ExecuteEnemyTurn()
    {
        if (combatManager == null)
        {
            Debug.LogWarning(
                "[RoundManager] CombatManager is NULL.",
                this
            );

            yield break;
        }

        yield return StartCoroutine(
            combatManager.RunEnemyRound()
        );
    }

    // ==================================================
    // VALIDATION
    // ==================================================

    private bool IsValidUnit(
        AttackUnit unit)
    {
        return CombatUtility.IsAlive(unit);
    }

    // ==================================================
    // REFRESH
    // ==================================================

    private void RefreshCachedUnits()
    {
        cachedUnits.Clear();

        List<AttackUnit> units =
            CombatUtility.GetAllAliveUnits();

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null)
            {
                cachedUnits.Add(
                    units[i]
                );
            }
        }
    }

    // ==================================================
    // ENEMIES
    // ==================================================

    private void EnsureEnemiesExist()
    {
        if (combatManager == null)
        {
            return;
        }

        if (CombatUtility.GetUnitCount(
                Team.Enemy) == 0)
        {
            combatManager.CheckForEnemies();
        }
    }

    // ==================================================
    // COOLDOWNS
    // ==================================================

    private void UpdateAllUnitCooldowns()
    {
        for (int i = 0; i < cachedUnits.Count; i++)
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

    // ==================================================
    // LOGGING
    // ==================================================

    private void LogAbilityUse(
        string unitName,
        string abilityName)
    {
        AbilityLogEntry entry =
            new AbilityLogEntry
            {
                round = currentRound,
                unitName = unitName,
                abilityName = abilityName
            };

        roundAbilityLogs.Add(entry);

        OnAbilityUsed?.Invoke(entry);
    }

    // ==================================================
    // AUTO BATTLE
    // ==================================================

    public void SetAutoBattle(
        bool enabled)
    {
        autoBattle = enabled;
    }

    public void ToggleAutoBattle()
    {
        SetAutoBattle(
            !autoBattle
        );

        if (autoBattleToggle != null)
        {
            autoBattleToggle.SetIsOnWithoutNotify(
                autoBattle
            );
        }
    }

    // ==================================================
    // ACCESSORS
    // ==================================================

    public List<AbilityLogEntry>
        GetAbilityLogsForRound(
            int round)
    {
        return roundAbilityLogs.FindAll(
            log => log.round == round
        );
    }

    public List<AbilityLogEntry>
        GetAllAbilityLogs()
    {
        return roundAbilityLogs;
    }

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
}