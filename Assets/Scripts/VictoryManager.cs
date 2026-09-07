using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private transitionGameManager transitionManager;

    [Header("UI")]
    [SerializeField]
    private GameObject victoryCanvas;

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

        // Always start hidden.
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
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
        }
    }

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged -=
                HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(
        GameStateManager.GameState newState)
    {
        Debug.Log(
            $"[VictoryManager] Handling state change: {newState}"
        );

        if (
            newState ==
            GameStateManager.GameState.Victory
        )
        {
            ShowVictoryCanvas();
        }
        else
        {
            HideVictoryCanvas();
        }
    }

    private void ShowVictoryCanvas()
    {
        if (victoryCanvas == null)
        {
            Debug.LogWarning(
                "[VictoryManager] Victory Canvas is not assigned!",
                this
            );

            return;
        }

        victoryCanvas.SetActive(true);

        Debug.Log(
            "[VictoryManager] Victory screen shown."
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
            "[VictoryManager] Victory screen hidden."
        );
    }

    public void ContinueButton()
    {
        Debug.Log(
            "[VictoryManager] Continue pressed. " +
            "Returning to map."
        );

        // Immediately hide Victory UI.
        HideVictoryCanvas();

        // Change the logical game state back to Map.
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

        // Start the visual transition back to the map.
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