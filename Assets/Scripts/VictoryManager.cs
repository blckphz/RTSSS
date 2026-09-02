using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameStateManager gameStateManager;

    [Header("UI")]
    [SerializeField] private GameObject victoryCanvas;

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        // Make sure it starts hidden
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
        if (newState ==
            GameStateManager.GameState.Victory)
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

        // Put your map-opening logic here
        Debug.Log("Continue to map!");
    }
}