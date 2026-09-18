using UnityEngine;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private transitionGameManager transitionManager;

    [SerializeField]
    private musicManager musicManager;

    [Header("UI")]
    [SerializeField]
    private GameObject victoryCanvas;

    [SerializeField]
    private Button continueButton;

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }

        if (musicManager == null)
        {
            musicManager =
                FindFirstObjectByType<musicManager>();
        }

        if (victoryCanvas == null)
        {
            Debug.LogError(
                "[VictoryManager] Victory Canvas is not assigned!",
                this
            );
        }
        else
        {
            victoryCanvas.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged +=
                HandleGameStateChanged;

            Debug.Log(
                "[VictoryManager] Subscribed to GameStateManager.",
                this
            );
        }
        else
        {
            Debug.LogError(
                "[VictoryManager] Could not find GameStateManager.",
                this
            );
        }
    }

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged -=
                HandleGameStateChanged;

            Debug.Log(
                "[VictoryManager] Unsubscribed from GameStateManager.",
                this
            );
        }
    }

    private void HandleGameStateChanged(
        GameStateManager.GameState newState
    )
    {
        Debug.Log(
            "[VictoryManager] Received game state: " +
            newState,
            this
        );

        if (
            newState ==
            GameStateManager.GameState.Victory
        )
        {
            Debug.Log(
                "[VictoryManager] Victory state reached.",
                this
            );

            ShowVictoryCanvas();

            return;
        }

        HideVictoryCanvas();
    }

    private void ShowVictoryCanvas()
    {
        if (victoryCanvas == null)
        {
            Debug.LogError(
                "[VictoryManager] Cannot show Victory Canvas. " +
                "Reference is missing.",
                this
            );

            return;
        }

        // Continue is immediately available.
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        victoryCanvas.SetActive(true);

        Debug.Log(
            "[VictoryManager] Victory Canvas shown.",
            this
        );
    }

    private void HideVictoryCanvas()
    {
        if (victoryCanvas == null)
        {
            return;
        }

        if (!victoryCanvas.activeSelf)
        {
            return;
        }

        victoryCanvas.SetActive(false);

        Debug.Log(
            "[VictoryManager] Victory Canvas hidden.",
            this
        );
    }

    public void ContinueButton()
    {
        Debug.Log(
            "[VictoryManager] Continue pressed. " +
            "Returning to map.",
            this
        );

        HideVictoryCanvas();

        if (gameStateManager != null)
        {
            gameStateManager.SetGameState(
                GameStateManager.GameState.Map
            );
        }
        else
        {
            Debug.LogError(
                "[VictoryManager] GameStateManager not found!",
                this
            );
        }

        if (musicManager != null)
        {
            musicManager.PlayMapMusic();
        }
        else
        {
            Debug.LogWarning(
                "[VictoryManager] MusicManager not found. " +
                "Map music will not play.",
                this
            );
        }

        if (transitionManager != null)
        {
            transitionManager.TransitionToMap();
        }
        else
        {
            Debug.LogError(
                "[VictoryManager] " +
                "transitionGameManager not found!",
                this
            );
        }
    }
}