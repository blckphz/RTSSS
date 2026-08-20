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
    [SerializeField] private int currentRound = 0;
    [SerializeField]
    private RoundState currentState =
        RoundState.Setup;

    [SerializeField] private bool autoBattle = false;


    // ==================================================
    // DEPENDENCIES
    // ==================================================

    [Header("Dependencies")]
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerAction playerAction;


    // ==================================================
    // UI / SETTINGS
    // ==================================================

    [Header("UI & Settings")]
    [SerializeField] private Toggle autoBattleToggle;
    [SerializeField] private float delayBetweenUnits = 0.5f;


    // ==================================================
    // INTERNAL
    // ==================================================

    private readonly List<AbilityLogEntry> roundAbilityLogs =
        new List<AbilityLogEntry>();

    private readonly List<AttackUnit> cachedUnits =
        new List<AttackUnit>();

    private WaitForSeconds unitDelay;

    private bool roundRunning;


    // ==================================================
    // EVENTS
    // ==================================================

    public event Action<AbilityLogEntry> OnAbilityUsed;


    // ==================================================
    // UNITY
    // ==================================================

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

        if (playerAction == null)
        {
            playerAction =
                GetComponent<PlayerAction>();

            if (playerAction == null)
            {
                playerAction =
                    FindFirstObjectByType<PlayerAction>();
            }
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
        // --------------------------------------------------
        // CLEAN DEAD UNITS
        // --------------------------------------------------

        if (gridManager != null)
        {
            gridManager.CleanupDeadUnits();
        }


        // --------------------------------------------------
        // ENSURE ENEMIES
        // --------------------------------------------------

        EnsureEnemiesExist();


        // --------------------------------------------------
        // PLAYER / ALLY TURN
        // --------------------------------------------------

        currentState =
            RoundState.PlayerAndAllyTurn;

        yield return StartCoroutine(
            ExecutePlayerAndAllyTurn()
        );


        // --------------------------------------------------
        // ENEMY TURN
        // --------------------------------------------------

        currentState =
            RoundState.EnemyTurn;

        yield return StartCoroutine(
            ExecuteEnemyTurn()
        );


        // --------------------------------------------------
        // END ROUND
        // --------------------------------------------------

        currentState =
            RoundState.Setup;

        roundRunning = false;
    }


    // ==================================================
    // PLAYER / ALLY TURN
    // ==================================================

    private IEnumerator ExecutePlayerAndAllyTurn()
    {
        if (playerAction == null)
        {
            Debug.LogWarning(
                "[RoundManager] PlayerAction is NULL.",
                this
            );

            yield break;
        }

        RefreshCachedUnits();

        for (int i = 0;
             i < cachedUnits.Count;
             i++)
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

            GameObject target =
                FindNearestEnemy(unit);

            string result =
                playerAction.TryExecuteUnitTurn(
                    unit,
                    target
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
    // FIND TARGET
    // ==================================================

    private GameObject FindNearestEnemy(
        AttackUnit attacker)
    {
        if (attacker == null)
        {
            return null;
        }

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        GameObject closestTarget = null;

        int closestDistance =
            int.MaxValue;

        Vector2Int attackerPosition =
            gridManager != null
                ? gridManager.WorldToGridPosition(
                    attacker.transform.position
                )
                : Vector2Int.zero;

        for (int i = 0;
             i < allUnits.Length;
             i++)
        {
            AttackUnit candidate =
                allUnits[i];

            if (candidate == null ||
                candidate == attacker)
            {
                continue;
            }

            if (candidate.IsDead())
            {
                continue;
            }

            if (candidate.GetTeam() ==
                attacker.GetTeam())
            {
                continue;
            }

            int distance;

            if (gridManager != null)
            {
                Vector2Int candidatePosition =
                    gridManager.WorldToGridPosition(
                        candidate.transform.position
                    );

                distance =
                    gridManager.GetDistance(
                        attackerPosition,
                        candidatePosition
                    );
            }
            else
            {
                distance =
                    Mathf.RoundToInt(
                        Vector3.Distance(
                            attacker.transform.position,
                            candidate.transform.position
                        )
                    );
            }

            if (distance < closestDistance)
            {
                closestDistance =
                    distance;

                closestTarget =
                    candidate.gameObject;
            }
        }

        return closestTarget;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private bool IsValidUnit(
        AttackUnit unit)
    {
        if (unit == null)
        {
            return false;
        }

        if (!unit.gameObject.activeInHierarchy)
        {
            return false;
        }

        return !unit.IsDead();
    }


    // ==================================================
    // REFRESH UNITS
    // ==================================================

    private void RefreshCachedUnits()
    {
        cachedUnits.Clear();

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < units.Length;
             i++)
        {
            AttackUnit unit =
                units[i];

            if (IsValidUnit(unit))
            {
                cachedUnits.Add(unit);
            }
        }
    }


    // ==================================================
    // ENEMY SPAWNING
    // ==================================================

    private void EnsureEnemiesExist()
    {
        if (combatManager == null)
        {
            return;
        }

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < units.Length;
             i++)
        {
            AttackUnit unit =
                units[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() != Team.Enemy)
            {
                continue;
            }

            if (!unit.IsDead())
            {
                return;
            }
        }

        combatManager.CheckForEnemies();
    }


    // ==================================================
    // COOLDOWNS
    // ==================================================

    private void UpdateAllUnitCooldowns()
    {
        for (int i = 0;
             i < cachedUnits.Count;
             i++)
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
    // ABILITY LOGGING
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