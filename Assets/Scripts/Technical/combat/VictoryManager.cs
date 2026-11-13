
using UnityEngine;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private transitionGameManager transitionManager;
    [SerializeField] private musicManager musicManager;

    [Header("UI")]
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private Button continueButton;

    private bool rewardPending;

    private void Awake()
    {
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();

        if (transitionManager == null)
            transitionManager = FindFirstObjectByType<transitionGameManager>();

        if (musicManager == null)
            musicManager = FindFirstObjectByType<musicManager>();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);

        rewardPending = false;

        if (continueButton != null)
            continueButton.interactable = true;
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();

        if (gameStateManager != null)
            gameStateManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if (gameStateManager != null)
            gameStateManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameStateManager.GameState newState)
    {
        if (newState == GameStateManager.GameState.Victory)
            ShowVictoryCanvas();
        else
            HideVictoryCanvas();
    }

    private void ShowVictoryCanvas()
    {
        if (victoryCanvas == null)
        {
            Debug.LogError("[VictoryManager] Victory Canvas is not assigned!");
            return;
        }

        victoryCanvas.SetActive(true);
        UpdateContinueButton();
    }

    private void HideVictoryCanvas()
    {
        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);
    }

    public void SetContinueButtonInteractable(bool interactable)
    {
        rewardPending = !interactable;
        UpdateContinueButton();
    }

    private void UpdateContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.interactable = !rewardPending;
    }

    private void ResetNextRoundButton()
    {
        NextRoundButtonManager nextRoundButton =
            FindFirstObjectByType<NextRoundButtonManager>();

        if (nextRoundButton != null)
        {
            nextRoundButton.ResetNextRoundButton();
        }
        else
        {
            Debug.LogWarning(
                "[VictoryManager] NextRoundButtonManager not found."
            );
        }
    }

    public void ContinueButton()
    {
        if (rewardPending)
        {
            Debug.LogWarning(
                "[VictoryManager] Cannot continue while reward is pending."
            );
            return;
        }

        if (continueButton != null && !continueButton.interactable)
            return;

        HideVictoryCanvas();

        // Reset the Next Round button for the next level.
        ResetNextRoundButton();

        if (gameStateManager != null)
            gameStateManager.SetGameState(GameStateManager.GameState.Map);

        if (musicManager != null)
            musicManager.PlayMapMusic();

        if (transitionManager != null)
            transitionManager.TransitionToMap();
    }
}
