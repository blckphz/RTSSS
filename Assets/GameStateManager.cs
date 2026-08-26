using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        PreparingEncounter,
        Combat,
        Victory,
        Defeat,
        Rewards,
        GameOver
    }

    [Header("References")]
    [SerializeField] private EncounterManager encounterManager;

    [Header("Initial State")]
    [SerializeField]
    private GameState startingState =
        GameState.MainMenu;

    private GameState currentState;

    public GameState CurrentState => currentState;

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }

        SetGameState(startingState);
    }

    // ============================================================
    // STATE
    // ============================================================

    public void SetGameState(GameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log(
            $"[GameStateManager] State changed to {currentState}",
            this
        );

        OnGameStateChanged?.Invoke(currentState);
    }

    // ============================================================
    // ENCOUNTER
    // ============================================================

    public void StartCombat()
    {
        if (encounterManager == null)
        {
            Debug.LogError(
                "[GameStateManager] EncounterManager is missing!",
                this
            );

            return;
        }

        if (currentState != GameState.MainMenu &&
            currentState != GameState.Victory &&
            currentState != GameState.Defeat)
        {
            Debug.LogWarning(
                $"[GameStateManager] Cannot start combat from state {currentState}",
                this
            );

            return;
        }

        SetGameState(
            GameState.PreparingEncounter
        );

        encounterManager.StartEncounter();
    }

    public void EncounterStarted()
    {
        SetGameState(
            GameState.Combat
        );
    }

    public void EncounterVictory()
    {
        SetGameState(
            GameState.Victory
        );
    }

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