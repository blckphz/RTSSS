using UnityEngine;
using static GameStateManager;

public class GameOverManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private UpdateManager updateManager;


    [Header("Game Over UI")]

    [SerializeField]
    private GameObject gameOverCanvas;


    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
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

            HandleGameStateChanged(
                gameStateManager.CurrentState
            );
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


    // ============================================================
    // GAME STATE
    // ============================================================

    private void HandleGameStateChanged(
        GameState newState
    )
    {
        if (
            newState ==
            GameState.GameOver
        )
        {
            ShowGameOverCanvas();
        }
        else
        {
            HideGameOverCanvas();
        }
    }


    // ============================================================
    // SHOW
    // ============================================================

    private void ShowGameOverCanvas()
    {
        if (gameOverCanvas == null)
        {
            return;
        }

        gameOverCanvas.SetActive(true);
    }


    // ============================================================
    // HIDE
    // ============================================================

    private void HideGameOverCanvas()
    {
        if (gameOverCanvas == null)
        {
            return;
        }

        gameOverCanvas.SetActive(false);
    }


    // ============================================================
    // BACK TO MENU
    // ============================================================

    public void BackToMenu()
    {
        Debug.Log(
            "[GameOverManager] " +
            "Returning to menu. Resetting run upgrades."
        );


        // --------------------------------------------------------
        // RESET RUNTIME UPGRADES
        // --------------------------------------------------------

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }

        if (updateManager != null)
        {
            updateManager.ResetAllUpgrades();
        }


        // --------------------------------------------------------
        // CHANGE GAME STATE
        // --------------------------------------------------------

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (gameStateManager != null)
        {
            gameStateManager.SetGameState(
                GameState.MainMenu
            );
        }


        // --------------------------------------------------------
        // HIDE GAME OVER
        // --------------------------------------------------------

        HideGameOverCanvas();


        // --------------------------------------------------------
        // TODO:
        // LOAD YOUR MENU SCENE HERE IF NEEDED
        // --------------------------------------------------------

        Debug.Log(
            "[GameOverManager] " +
            "Returned to Main Menu."
        );
    }
}
