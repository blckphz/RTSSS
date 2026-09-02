using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private transitionGameManager transitionManager;

    [Header("UI")]
    [SerializeField] private GameObject victoryCanvas;

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

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(
        GameStateManager.GameState newState)
    {
        if (newState == GameStateManager.GameState.Victory)
        {
            ShowVictoryCanvas();
        }
    }

    private void ShowVictoryCanvas()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
        }
    }

    public void ContinueButton()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }

        if (transitionManager != null)
        {
            transitionManager.TransitionToMap();
        }
        else
        {
            Debug.LogError(
                "[VictoryManager] transitionGameManager not found!"
            );
        }
    }
}