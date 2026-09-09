using UnityEngine;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private transitionGameManager transitionManager;

    [SerializeField]
    private UpgradeChoiceUI upgradeChoiceUI;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField]
    private GameObject victoryCanvas;

    [SerializeField]
    private Button continueButton;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // FIND GAME STATE MANAGER
        // --------------------------------------------------------

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }


        // --------------------------------------------------------
        // FIND TRANSITION MANAGER
        // --------------------------------------------------------

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
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
        // START VICTORY UI HIDDEN
        // --------------------------------------------------------

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }


        // --------------------------------------------------------
        // START CONTINUE DISABLED
        // --------------------------------------------------------

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }
    }


    // ============================================================
    // ENABLE
    // ============================================================

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


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnGameStateChanged -=
                HandleGameStateChanged;
        }
    }


    // ============================================================
    // GAME STATE CHANGED
    // ============================================================

    private void HandleGameStateChanged(
        GameStateManager.GameState newState)
    {
        Debug.Log(
            $"[VictoryManager] Handling state change: {newState}"
        );


        if (newState ==
            GameStateManager.GameState.Victory)
        {
            ShowVictoryCanvas();
        }
        else
        {
            HideVictoryCanvas();
        }
    }


    // ============================================================
    // SHOW VICTORY
    // ============================================================

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


        // --------------------------------------------------------
        // RESET CONTINUE BUTTON
        // --------------------------------------------------------

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }


        // --------------------------------------------------------
        // SHOW VICTORY CANVAS
        // --------------------------------------------------------

        victoryCanvas.SetActive(true);


        Debug.Log(
            "[VictoryManager] Victory screen shown."
        );


        // --------------------------------------------------------
        // SHOW UPGRADE CHOICES
        // --------------------------------------------------------

        if (upgradeChoiceUI != null)
        {
            upgradeChoiceUI.ShowUpgradeChoices();
        }
        else
        {
            Debug.LogWarning(
                "[VictoryManager] " +
                "UpgradeChoiceUI not found!",
                this
            );

            // If there is no upgrade UI,
            // don't permanently lock Continue.

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }
    }


    // ============================================================
    // UPGRADE SELECTED
    // ============================================================

    public void OnUpgradeSelected()
    {
        Debug.Log(
            "[VictoryManager] Upgrade selected. " +
            "Continue button enabled."
        );


        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
    }


    // ============================================================
    // HIDE VICTORY
    // ============================================================

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


    // ============================================================
    // CONTINUE
    // ============================================================

    public void ContinueButton()
    {
        Debug.Log(
            "[VictoryManager] Continue pressed. " +
            "Returning to map."
        );


        // --------------------------------------------------------
        // HIDE UPGRADE UI
        // --------------------------------------------------------

        if (upgradeChoiceUI != null)
        {
            upgradeChoiceUI.HideUpgradeChoices();
        }


        // --------------------------------------------------------
        // HIDE VICTORY UI
        // --------------------------------------------------------

        HideVictoryCanvas();


        // --------------------------------------------------------
        // CHANGE GAME STATE TO MAP
        // --------------------------------------------------------

        if (gameStateManager != null)
        {
            gameStateManager.SetGameState(
                GameStateManager.GameState.Map
            );
        }
        else
        {
            Debug.LogError(
                "[VictoryManager] " +
                "GameStateManager not found!",
                this
            );
        }


        // --------------------------------------------------------
        // TRANSITION BACK TO MAP
        // --------------------------------------------------------

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
