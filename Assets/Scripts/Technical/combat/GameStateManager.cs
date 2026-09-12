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
        // --------------------------------------------------------
        // FIND ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        // --------------------------------------------------------
        // FIND UPGRADE UI
        // --------------------------------------------------------

        if (upgradeChoiceUI == null)
        {
            upgradeChoiceUI =
                FindFirstObjectByType<UpgradeChoiceUI>();
        }


        // --------------------------------------------------------
        // SET INITIAL STATE
        // --------------------------------------------------------

        currentState =
            startingState;
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // CHECK FOR NEW RUN
        // --------------------------------------------------------

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
        GameState newState)
    {
        // --------------------------------------------------------
        // DON'T DO ANYTHING IF STATE IS ALREADY THE SAME
        // --------------------------------------------------------

        if (currentState == newState)
        {
            return;
        }


        // --------------------------------------------------------
        // SAVE NEW STATE
        // --------------------------------------------------------

        currentState =
            newState;


        // --------------------------------------------------------
        // NOTIFY LISTENERS
        // --------------------------------------------------------

        OnGameStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // STARTING UPGRADES
    // ============================================================

    private void ShowStartingUpgrades()
    {
        // --------------------------------------------------------
        // FIND UPGRADE UI
        // --------------------------------------------------------

        if (upgradeChoiceUI == null)
        {
            upgradeChoiceUI =
                FindFirstObjectByType<UpgradeChoiceUI>();
        }


        // --------------------------------------------------------
        // CHECK UPGRADE UI
        // --------------------------------------------------------

        if (upgradeChoiceUI == null)
        {
            return;
        }


        // --------------------------------------------------------
        // SHOW STARTING UPGRADES
        // --------------------------------------------------------

        upgradeChoiceUI.ShowStartingUpgradeChoice(
            OnStartingUpgradeSelected
        );
    }


    // ============================================================
    // STARTING UPGRADE SELECTED
    // ============================================================

    private void OnStartingUpgradeSelected()
    {
        // --------------------------------------------------------
        // TELL GAME SESSION
        // --------------------------------------------------------

        if (GameSession.Instance != null)
        {
            GameSession.Instance.StartingUpgradesComplete();
        }


        // --------------------------------------------------------
        // ENTER MAP
        // --------------------------------------------------------

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
        // --------------------------------------------------------
        // CHECK ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (encounterManager == null)
        {
            return;
        }


        // --------------------------------------------------------
        // CHECK CURRENT STATE
        // --------------------------------------------------------

        if (
            currentState != GameState.MainMenu &&
            currentState != GameState.Map &&
            currentState != GameState.Victory &&
            currentState != GameState.Defeat
        )
        {
            return;
        }


        // --------------------------------------------------------
        // PREPARING ENCOUNTER
        // --------------------------------------------------------

        SetGameState(
            GameState.PreparingEncounter
        );


        // --------------------------------------------------------
        // START ENCOUNTER
        // --------------------------------------------------------

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