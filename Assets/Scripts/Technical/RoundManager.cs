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
    private int currentRound = 0;

    [SerializeField]
    private RoundState currentState =
        RoundState.Setup;

    [SerializeField]
    private bool autoBattle = false;


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


    // ============================================================
    // UI
    // ============================================================

    [Header("UI & Settings")]
    [SerializeField]
    private Toggle autoBattleToggle;

    [SerializeField]
    private float delayBetweenUnits = 0.1f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private readonly List<AbilityLogEntry> roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit> cachedUnits =
        new List<AttackUnit>();

    private WaitForSeconds unitDelay;

    private bool roundRunning;

    private CanvasInfoManager canvasInfoManager;


    public event Action<AbilityLogEntry> OnAbilityUsed;


    // ============================================================
    // DEBUG
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


        if (encounterManager == null)
        {
            encounterManager =
                FindObjectOfType<EncounterManager>();
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
        {
            return;
        }


        if (currentState != RoundState.Setup)
        {
            return;
        }


        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            RoundTiming(
                "StartRound blocked — encounter is finished."
            );

            return;
        }


        float startTime =
            Time.realtimeSinceStartup;


        currentRound++;

        roundRunning = true;


        RoundTiming(
            "ROUND START"
        );


        RefreshCachedUnits();

        ResetAllUnitMovement();

        UpdateAllUnitCooldowns();


        RoundTiming(
            $"SETUP COMPLETE " +
            $"duration=" +
            $"{Time.realtimeSinceStartup - startTime:F3}s"
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


    // ============================================================
    // ROUND PIPELINE
    // ============================================================

    private IEnumerator RunRoundPipeline()
    {
        float roundStart =
            Time.realtimeSinceStartup;


        RoundTiming(
            "PIPELINE START"
        );


        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }


        // --------------------------------------------------------
        // ENEMY SPAWN
        // --------------------------------------------------------

        bool enemiesWereSpawned =
            EnsureEnemiesExist();


        if (enemiesWereSpawned)
        {
            RoundTiming(
                "ENEMIES WERE SPAWNED THIS ROUND"
            );
        }


        // --------------------------------------------------------
        // PLAYER / ALLY
        // --------------------------------------------------------

        currentState =
            RoundState.PlayerAndAllyTurn;


        if (canvasInfoManager != null)
        {
            canvasInfoManager.RefreshCurrentSelection();
        }


        RoundTiming(
            "PLAYER/ALLY TURN START"
        );


        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );


        // --------------------------------------------------------
        // STOP IF ENCOUNTER FINISHED
        // --------------------------------------------------------

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            EndRound();

            yield break;
        }


        // --------------------------------------------------------
        // ENEMY
        // --------------------------------------------------------

        currentState =
            RoundState.EnemyTurn;


        if (enemiesWereSpawned)
        {
            RoundTiming(
                "ENEMIES SPAWNED THIS ROUND — " +
                "SKIPPING ENEMY TURN"
            );
        }
        else
        {
            RoundTiming(
                "ENEMY TURN START"
            );


            yield return StartCoroutine(
                ExecuteEnemyTurn()
            );


            RoundTiming(
                "ENEMY TURN END"
            );
        }


        // --------------------------------------------------------
        // ROUND END
        // --------------------------------------------------------

        EndRound();


        RoundTiming(
            $"ROUND END " +
            $"total=" +
            $"{Time.realtimeSinceStartup - roundStart:F3}s"
        );
    }


    // ============================================================
    // END ROUND
    // ============================================================

    private void EndRound()
    {
        RoundTiming(
            "ROUND COMPLETE — CHECKING VICTORY CONDITIONS"
        );


        // ========================================================
        // IMPORTANT
        //
        // Check victory BEFORE changing the EncounterManager
        // or RoundManager state.
        //
        // This allows SurvivalRounds to see:
        //
        // Round 1 / 1
        //
        // instead of accidentally returning because the state
        // was already changed to Setup.
        // ========================================================

        if (encounterManager != null)
        {
            encounterManager.CheckVictoryAfterRound();


            // ----------------------------------------------------
            // If victory happened, DO NOT continue normal round
            // cleanup/start logic.
            // ----------------------------------------------------

            if (encounterManager.IsFinished())
            {
                roundRunning = false;

                RoundTiming(
                    "ROUND ENDED — ENCOUNTER FINISHED"
                );

                return;
            }
        }


        // ========================================================
        // NORMAL ROUND RESET
        // ========================================================

        currentState =
            RoundState.Setup;


        roundRunning = false;


        RoundTiming(
            "ROUND READY FOR NEXT INPUT"
        );
    }


    // ============================================================
    // PLAYER / ALLY
    // ============================================================

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


            if (
                team != Team.Player &&
                team != Team.Ally
            )
            {
                continue;
            }


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
                $"UNIT END: {unit.name}"
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


        yield return StartCoroutine(
            combatManager.RunEnemyRound()
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
    // REFRESH UNITS
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

    private bool EnsureEnemiesExist()
    {
        // --------------------------------------------------------
        // Encounter mode owns enemy spawning.
        // --------------------------------------------------------

        if (
            encounterManager != null &&
            encounterManager.IsEncounterRunning()
        )
        {
            if (
                CombatUtility.GetUnitCount(
                    Team.Enemy
                ) == 0
            )
            {
                RoundTiming(
                    "No enemies remain. " +
                    "EncounterManager will evaluate victory."
                );
            }


            return false;
        }


        // --------------------------------------------------------
        // Legacy/test mode
        // --------------------------------------------------------

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
            RoundTiming(
                "NO ENEMIES FOUND — SPAWNING TEST ENEMIES"
            );


            combatManager.CheckForEnemies();


            return true;
        }


        return false;
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
            {
                continue;
            }


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


        roundAbilityLogs.Add(
            entry
        );


        OnAbilityUsed?.Invoke(
            entry
        );
    }


    // ============================================================
    // AUTO BATTLE
    // ============================================================

    public void SetAutoBattle(
        bool enabled)
    {
        autoBattle =
            enabled;
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