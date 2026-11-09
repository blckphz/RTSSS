using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuClassSelector : MonoBehaviour
{
    // ============================================================
    // SELECTED SQUAD
    // ============================================================

    [Header("Selected Squad")]
    public SquadSO selectedSquad;

    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]
    public GameObject squadSelectionCanvas;

    // ============================================================
    // SCENE
    // ============================================================

    [Header("Scene")]
    [SerializeField]
    private string gameSceneName = "GameScene";

    // ============================================================
    // PLAY BUTTON
    // ============================================================

    public void OnPlayClicked()
    {
        if (squadSelectionCanvas != null)
        {
            squadSelectionCanvas.SetActive(true);
        }
    }

    // ============================================================
    // SQUAD SELECTION
    // ============================================================

    public void OnClassIconClicked(SquadSO clickedSquad)
    {
        if (clickedSquad == null)
        {
            Debug.LogWarning(
                "[MainMenuClassSelector] Clicked squad is null."
            );

            return;
        }

        selectedSquad = clickedSquad;
    }

    // ============================================================
    // START GAME
    // ============================================================

    public void StartGamePressed()
    {
        if (selectedSquad == null)
        {
            Debug.LogWarning(
                "[MainMenuClassSelector] No squad selected!"
            );

            return;
        }

        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "[MainMenuClassSelector] GameSession does not exist!"
            );

            return;
        }

        // --------------------------------------------------------
        // RESET PREVIOUS RUN UPGRADES
        // --------------------------------------------------------

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.BeginNewRun();
        }
        else
        {
            Debug.Log(
                "[MainMenuClassSelector] No persistent UpdateManager found. " +
                "A new UpdateManager should initialize in the game scene."
            );
        }

        // --------------------------------------------------------
        // SAVE SELECTED SQUAD
        // --------------------------------------------------------

        GameSession.Instance.SetSelectedSquad(selectedSquad);

        Debug.Log(
            "[MainMenuClassSelector] Starting new run with squad: " +
            selectedSquad.name
        );

        // --------------------------------------------------------
        // LOAD GAME SCENE
        // --------------------------------------------------------

        SceneManager.LoadScene(gameSceneName);
    }
}
