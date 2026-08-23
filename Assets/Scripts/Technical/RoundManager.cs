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

    [Header("Round State")]
    [SerializeField]
    private int currentRound = 0;

    [SerializeField]
    private RoundState currentState = RoundState.Setup;

    [SerializeField]
    private bool autoBattle = false;

    [Header("Dependencies")]
    [SerializeField]
    private CombatManager combatManager;

    [SerializeField]
    private GridManager gridManager;

    [Header("UI & Settings")]
    [SerializeField]
    private Toggle autoBattleToggle;

    [SerializeField]
    private float delayBetweenUnits = 0.1f;

    private readonly List<AbilityLogEntry> roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit> cachedUnits =
        new List<AttackUnit>();

    private WaitForSeconds unitDelay;

    private bool roundRunning;

    private CanvasInfoManager canvasInfoManager;

    public event Action<AbilityLogEntry> OnAbilityUsed;


    // ============================================================
    // ESSENTIAL DEBUG
    // ============================================================

    private void RoundTiming(string message)
    {
        Debug.Log(
            $"<color=yellow>[ROUND]</color> " +
            $"[T:{Time.time:F3}] " +
            $"[F:{Time.frameCount}] " +
            $"[R:{currentRound}] " +
            message,
            this
        );
    }


    // ============================================================
    // UNITY
    // ============================================================

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

        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();

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
        if (
            autoBattle &&
            currentState == RoundState.Setup &&
            !roundRunning
        )
        {
            StartRound();
        }
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

        float startTime =
            Time.realtimeSinceStartup;

        currentRound++;
        roundRunning = true;

        RoundTiming("ROUND START");

        RefreshCachedUnits();
        ResetAllUnitMovement();
        UpdateAllUnitCooldowns();

        RoundTiming(
            $"SETUP COMPLETE " +
            $"duration={Time.realtimeSinceStartup - startTime:F3}s"
        );

        StartCoroutine(
            RunRoundPipeline()
        );
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
        float roundStart =
            Time.realtimeSinceStartup;

        RoundTiming("PIPELINE START");

        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }

        EnsureEnemiesExist();


        // --------------------------------------------------------
        // PLAYER / ALLY
        // --------------------------------------------------------

        currentState =
            RoundState.PlayerAndAllyTurn;

        if (canvasInfoManager != null)
        {
            canvasInfoManager.RefreshCurrentSelection();
        }

        float playerPhaseStart =
            Time.realtimeSinceStartup;

        RoundTiming("PLAYER/ALLY TURN START");

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );

        RoundTiming(
            $"PLAYER/ALLY TURN END " +
            $"duration={Time.realtimeSinceStartup - playerPhaseStart:F3}s"
        );


        // --------------------------------------------------------
        // ENEMY
        // --------------------------------------------------------

        currentState =
            RoundState.EnemyTurn;

        float enemyPhaseStart =
            Time.realtimeSinceStartup;

        RoundTiming("ENEMY TURN START");

        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );

        RoundTiming(
            $"ENEMY TURN END " +
            $"duration={Time.realtimeSinceStartup - enemyPhaseStart:F3}s"
        );


        // --------------------------------------------------------
        // ROUND END
        // --------------------------------------------------------

        currentState =
            RoundState.Setup;

        roundRunning = false;

        RoundTiming(
            $"ROUND END " +
            $"total={Time.realtimeSinceStartup - roundStart:F3}s"
        );
    }


    // ============================================================
    // PLAYER / ALLY TURN
    // ============================================================

    private IEnumerator ExecutePlayerAndAllyTurn()
    {
        RefreshCachedUnits();

        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit =
                cachedUnits[i];

            if (!IsValidUnit(unit))
                continue;

            Team team =
                unit.GetTeam();

            if (
                team != Team.Player &&
                team != Team.Ally
            )
            {
                continue;
            }

            float unitStart =
                Time.realtimeSinceStartup;

            RoundTiming(
                $"UNIT START: {unit.name}"
            );

            yield return StartCoroutine(
                CombatUtility.ExecuteUnitTurnCoroutine(
                    unit,
                    gridManager
                )
            );

            RoundTiming(
                $"UNIT END: {unit.name} " +
                $"duration={Time.realtimeSinceStartup - unitStart:F3}s"
            );

            if (delayBetweenUnits > 0f)
            {
                yield return unitDelay;
            }
        }
    }


    // ============================================================
    // ENEMY TURN
    // ============================================================

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

        float startTime =
            Time.realtimeSinceStartup;

        RoundTiming(
            "CombatManager.RunEnemyRound START"
        );

        yield return StartCoroutine(
            combatManager.RunEnemyRound()
        );

        RoundTiming(
            "CombatManager.RunEnemyRound END " +
            $"duration={Time.realtimeSinceStartup - startTime:F3}s"
        );
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool IsValidUnit(
        AttackUnit unit)
    {
        return CombatUtility.IsAlive(unit);
    }


    // ============================================================
    // REFRESH
    // ============================================================

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


    // ============================================================
    // ENEMIES
    // ============================================================

    private void EnsureEnemiesExist()
    {
        if (combatManager == null)
            return;

        if (
            CombatUtility.GetUnitCount(
                Team.Enemy
            ) == 0
        )
        {
            combatManager.CheckForEnemies();
        }
    }


    // ============================================================
    // COOLDOWNS
    // ============================================================

    private void UpdateAllUnitCooldowns()
    {
        for (int i = 0; i < cachedUnits.Count; i++)
        {
            AttackUnit unit =
                cachedUnits[i];

            if (unit == null)
                continue;

            unit.StartNewRound();
        }
    }


    // ============================================================
    // LOGGING
    // ============================================================

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


    // ============================================================
    // AUTO BATTLE
    // ============================================================

    public void SetAutoBattle(
        bool enabled)
    {
        autoBattle = enabled;
    }


    public void ToggleAutoBattle()
    {
        SetAutoBattle(!autoBattle);

        if (autoBattleToggle != null)
        {
            autoBattleToggle.SetIsOnWithoutNotify(
                autoBattle
            );
        }
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

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