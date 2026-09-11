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
    // UNITY
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
        // SET INITIAL STATE
        // --------------------------------------------------------

        currentState =
            startingState;


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


        Debug.Log(
            $"[GameStateManager] Game state changed to: {currentState}"
        );


        // --------------------------------------------------------
        // NOTIFY LISTENERS
        // --------------------------------------------------------

        OnGameStateChanged?.Invoke(
            currentState
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
            Debug.LogError(
                "[GameStateManager] EncounterManager is missing!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK CURRENT STATE
        // --------------------------------------------------------
        //
        // Combat can begin from:
        //
        // MainMenu
        // Map
        // Victory
        // Defeat
        //
        // Normally the map will be the state used when
        // selecting another node.
        //
        // --------------------------------------------------------

        if (
            currentState != GameState.MainMenu &&
            currentState != GameState.Map &&
            currentState != GameState.Victory &&
            currentState != GameState.Defeat
        )
        {
            Debug.LogWarning(
                $"[GameStateManager] Cannot start combat " +
                $"from state {currentState}",
                this
            );

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