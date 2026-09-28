using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // ============================================================
    // GAME STATES
    // ============================================================

    public enum GameState
    {
        MainMenu,
        Map,
        PreparingEncounter,
        Combat,
        Victory,
        Defeat,
        Rewards,
        GameOver
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private EncounterManager encounterManager;

    [SerializeField]
    private UpgradeChoiceUI upgradeChoiceUI;


    // ============================================================
    // INITIAL STATE
    // ============================================================

    [Header("Initial State")]

    [SerializeField]
    private GameState startingState =
        GameState.MainMenu;


    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private GameState currentState;


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public GameState CurrentState =>
        currentState;


    // ============================================================
    // EVENTS
    // ============================================================

    public event Action<GameState>
        OnGameStateChanged;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }

        if (upgradeChoiceUI == null)
        {
            upgradeChoiceUI =
                FindFirstObjectByType<UpgradeChoiceUI>();
        }

        currentState =
            startingState;
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        if (
            GameSession.Instance != null &&
            GameSession.Instance.NewRunPending
        )
        {
            ShowStartingUpgrades();
        }
    }


    // ============================================================
    // SET GAME STATE
    // ============================================================

    public void SetGameState(
        GameState newState
    )
    {
        if (currentState == newState)
        {
            return;
        }

        currentState =
            newState;

        OnGameStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // STARTING UPGRADES
    // ============================================================

    private void ShowStartingUpgrades()
    {
        if (upgradeChoiceUI == null)
        {
            upgradeChoiceUI =
                FindFirstObjectByType<UpgradeChoiceUI>();
        }

        if (upgradeChoiceUI == null)
        {
            return;
        }

        upgradeChoiceUI.ShowStartingUpgradeChoice(
            OnStartingUpgradeSelected
        );
    }


    // ============================================================
    // STARTING UPGRADE SELECTED
    // ============================================================

    private void OnStartingUpgradeSelected()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.StartingUpgradesComplete();
        }

        SetGameState(
            GameState.Map
        );
    }


    // ============================================================
    // ENTER MAP
    // ============================================================

    public void EnterMap()
    {
        SetGameState(
            GameState.Map
        );
    }


    // ============================================================
    // START COMBAT
    // ============================================================

    public void StartCombat()
    {
        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }

        if (encounterManager == null)
        {
            return;
        }

        if (
            currentState != GameState.MainMenu &&
            currentState != GameState.Map &&
            currentState != GameState.Victory &&
            currentState != GameState.Defeat
        )
        {
            return;
        }

        SetGameState(
            GameState.PreparingEncounter
        );

        encounterManager.StartEncounter();
    }


    // ============================================================
    // ENCOUNTER STARTED
    // ============================================================

    public void EncounterStarted()
    {
        SetGameState(
            GameState.Combat
        );
    }


    // ============================================================
    // ENCOUNTER VICTORY
    // ============================================================

    public void EncounterVictory()
    {
        SetGameState(
            GameState.Victory
        );
    }


    // ============================================================
    // ENCOUNTER DEFEAT
    // ============================================================

    public void EncounterDefeat()
    {
        SetGameState(
            GameState.Defeat
        );
    }


    // ============================================================
    // REWARDS
    // ============================================================

    public void OpenRewards()
    {
        SetGameState(
            GameState.Rewards
        );
    }


    // ============================================================
    // GAME OVER
    // ============================================================

    public void GameOver()
    {
        SetGameState(
            GameState.GameOver
        );
    }
}