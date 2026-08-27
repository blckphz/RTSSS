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
    private RoundState currentState = RoundState.Setup;

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


    // ============================================================
    // ENEMY SPAWN LOCK
    // ============================================================
    //
    // Enemies spawned during THIS round are locked.
    //
    // They do not receive an enemy turn until the next round.
    //
    // Existing enemies are allowed to act normally.
    //
    // ============================================================

    private readonly HashSet<AttackUnit> enemyTurnLockedUnits =
        new HashSet<AttackUnit>();


    private WaitForSeconds unitDelay;

    private bool roundRunning;

    private CanvasInfoManager canvasInfoManager;

    public event Action<AbilityLogEntry> OnAbilityUsed;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (combatManager == null)
        {
            combatManager =
                FindFirstObjectByType<CombatManager>();
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
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
        // --------------------------------------------------------
        // AUTO BATTLE
        // --------------------------------------------------------
        //
        // Auto battle can only start a round when the player is
        // actually present on the field.
        //
        // --------------------------------------------------------

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
        // --------------------------------------------------------
        // ALREADY RUNNING
        // --------------------------------------------------------

        if (roundRunning)
        {
            return;
        }


        // --------------------------------------------------------
        // WRONG STATE
        // --------------------------------------------------------

        if (currentState != RoundState.Setup)
        {
            return;
        }


        // --------------------------------------------------------
        // PLAYER MUST BE ON THE FIELD
        // --------------------------------------------------------
        //
        // This is the important check.
        //
        // Do not start a round until a living Player AttackUnit
        // exists.
        //
        // This also prevents AutoBattle from starting rounds
        // before the player has spawned.
        //
        // --------------------------------------------------------

        if (!IsPlayerOnField())
        {
            Debug.Log(
                "[RoundManager] Cannot start round: " +
                "Player is not on the field.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // ENCOUNTER FINISHED
        // --------------------------------------------------------

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            return;
        }


        // --------------------------------------------------------
        // START ROUND
        // --------------------------------------------------------

        currentRound++;

        roundRunning = true;


        // --------------------------------------------------------
        // CLEAR PREVIOUS ROUND'S LOCKS
        // --------------------------------------------------------
        //
        // Enemies that were spawned during the previous round
        // can now act.
        //
        // --------------------------------------------------------

        enemyTurnLockedUnits.Clear();


        // --------------------------------------------------------
        // CAPTURE EXISTING ENEMIES
        // --------------------------------------------------------
        //
        // These enemies existed before this round's wave spawn.
        //
        // They are allowed to act this round.
        //
        // --------------------------------------------------------

        HashSet<AttackUnit> enemiesExistingBeforeSpawn =
            CaptureLivingEnemies();


        // --------------------------------------------------------
        // REFRESH
        // --------------------------------------------------------

        RefreshCachedUnits();

        ResetAllUnitMovement();

        UpdateAllUnitCooldowns();


        // --------------------------------------------------------
        // SURVIVAL WAVE SPAWN
        // --------------------------------------------------------
        //
        // A new wave can spawn even if old enemies are still alive.
        //
        // Newly spawned enemies are locked for this round.
        //
        // --------------------------------------------------------

        if (
            encounterManager != null &&
            currentRound > 1 &&
            encounterManager.IsEncounterRunning()
        )
        {
            encounterManager.SpawnNextRoundEnemies();

            // New enemies now exist.
            RefreshCachedUnits();

            // Lock only enemies that did not exist before spawning.
            LockNewlySpawnedEnemies(
                enemiesExistingBeforeSpawn
            );
        }


        StartCoroutine(
            RunRoundPipeline()
        );
    }


    // ============================================================
    // PLAYER ON FIELD
    // ============================================================

    private bool IsPlayerOnField()
    {
        List<AttackUnit> players =
            CombatUtility.GetUnitsByTeam(
                Team.Player
            );

        if (players == null || players.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < players.Count; i++)
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

            // A living Player AttackUnit exists.
            return true;
        }

        return false;
    }


    // ============================================================
    // CAPTURE LIVING ENEMIES
    // ============================================================

    private HashSet<AttackUnit> CaptureLivingEnemies()
    {
        HashSet<AttackUnit> enemies =
            new HashSet<AttackUnit>();

        List<AttackUnit> units =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (units == null)
        {
            return enemies;
        }

        for (int i = 0; i < units.Count; i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (!CombatUtility.IsAlive(unit))
            {
                continue;
            }

            enemies.Add(
                unit
            );
        }

        return enemies;
    }


    // ============================================================
    // LOCK NEW ENEMIES
    // ============================================================

    private void LockNewlySpawnedEnemies(
        HashSet<AttackUnit> enemiesExistingBeforeSpawn
    )
    {
        if (enemiesExistingBeforeSpawn == null)
        {
            return;
        }

        List<AttackUnit> currentEnemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (currentEnemies == null)
        {
            return;
        }

        int lockedCount = 0;

        for (int i = 0; i < currentEnemies.Count; i++)
        {
            AttackUnit enemy =
                currentEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (!CombatUtility.IsAlive(enemy))
            {
                continue;
            }


            // ----------------------------------------------------
            // OLD ENEMY
            // ----------------------------------------------------

            if (
                enemiesExistingBeforeSpawn.Contains(
                    enemy
                )
            )
            {
                continue;
            }


            // ----------------------------------------------------
            // NEW ENEMY
            // ----------------------------------------------------

            enemyTurnLockedUnits.Add(
                enemy
            );

            lockedCount++;
        }

        Debug.Log(
            "[RoundManager] New enemies locked for this round: " +
            lockedCount,
            this
        );
    }


    // ============================================================
    // IS ENEMY LOCKED
    // ============================================================

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
        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }


        // --------------------------------------------------------
        // LEGACY / TEST SPAWN
        // --------------------------------------------------------

        bool enemiesWereSpawned =
            EnsureEnemiesExist();


        // --------------------------------------------------------
        // PLAYER / ALLY TURN
        // --------------------------------------------------------

        currentState =
            RoundState.PlayerAndAllyTurn;

        if (canvasInfoManager != null)
        {
            canvasInfoManager.RefreshCurrentSelection();
        }

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
        // ENEMY TURN
        // --------------------------------------------------------

        currentState =
            RoundState.EnemyTurn;


        if (enemiesWereSpawned)
        {
            Debug.Log(
                "[RoundManager] Enemies were spawned this round. " +
                "New enemies are locked; existing enemies may act.",
                this
            );
        }


        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );


        // --------------------------------------------------------
        // ROUND END
        // --------------------------------------------------------

        EndRound();
    }


    // ============================================================
    // END ROUND
    // ============================================================

    private void EndRound()
    {
        if (encounterManager != null)
        {
            encounterManager.CheckVictoryAfterRound();

            if (encounterManager.IsFinished())
            {
                roundRunning = false;

                return;
            }
        }


        // --------------------------------------------------------
        // NORMAL ROUND RESET
        // --------------------------------------------------------

        currentState =
            RoundState.Setup;

        roundRunning = false;


        // IMPORTANT:
        //
        // Do NOT clear enemyTurnLockedUnits here.
        //
        // They remain locked until the NEXT round starts.
        //
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

        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log(
                "[RoundManager] No enemies available for enemy turn.",
                this
            );

            yield break;
        }


        Debug.Log(
            "[RoundManager] Enemy turn starting. " +
            "Total enemies=" +
            enemies.Count +
            " | Locked=" +
            enemyTurnLockedUnits.Count,
            this
        );


        for (int i = 0; i < enemies.Count; i++)
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


            // ----------------------------------------------------
            // NEWLY SPAWNED THIS ROUND
            // ----------------------------------------------------

            if (IsEnemyTurnLocked(enemy))
            {
                Debug.Log(
                    "[RoundManager] Skipping newly spawned enemy: " +
                    enemy.name,
                    enemy
                );

                continue;
            }


            // ----------------------------------------------------
            // OLD ENEMY ACTS
            // ----------------------------------------------------

            Debug.Log(
                "[RoundManager] Enemy acting: " +
                enemy.name,
                enemy
            );


            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !combatManager.EnemiesMoveAfterRound,
                    combatManager.EnemiesAttackAfterMoving
                )
            );


            yield return null;


            // ----------------------------------------------------
            // STOP IF ENCOUNTER FINISHED
            // ----------------------------------------------------

            if (
                encounterManager != null &&
                encounterManager.IsFinished()
            )
            {
                yield break;
            }
        }


        Debug.Log(
            "[RoundManager] Enemy turn complete.",
            this
        );
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool IsValidUnit(
        AttackUnit unit
    )
    {
        return CombatUtility.IsAlive(
            unit
        );
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
        {
            return;
        }

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
        // ENCOUNTER MODE
        // --------------------------------------------------------
        //
        // EncounterManager owns spawning.
        //
        // Multiple waves can exist simultaneously.
        //
        // --------------------------------------------------------

        if (
            encounterManager != null &&
            encounterManager.IsEncounterRunning()
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // LEGACY / TEST MODE
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
        string abilityName
    )
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
        bool enabled
    )
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
            int round
        )
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


    // ============================================================
    // PLAYER CHECK ACCESSOR
    // ============================================================

    public bool HasPlayerOnField()
    {
        return IsPlayerOnField();
    }
}