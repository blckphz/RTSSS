using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuClassSelector : MonoBehaviour
{
    [Header("Selected Squad")]
    public SquadSO selectedSquad;


    [Header("UI")]
    public GameObject squadSelectionCanvas;


    [Header("Scene")]
    [SerializeField]
    private string gameSceneName = "GameScene";


    // ==================================================
    // PLAY BUTTON
    // ==================================================

    public void OnPlayClicked()
    {
        if (squadSelectionCanvas != null)
        {
            squadSelectionCanvas.SetActive(
                true
            );
        }
    }


    // ==================================================
    // SQUAD SELECTION
    // ==================================================

    public void OnClassIconClicked(
        SquadSO clickedSquad)
    {
        if (clickedSquad == null)
        {
            Debug.LogWarning(
                "[MainMenuClassSelector] Clicked squad is null."
            );

            return;
        }


        selectedSquad =
            clickedSquad;


        Debug.Log(
            "[MainMenuClassSelector] Selected Squad: " +
            selectedSquad.name
        );
    }


    // ==================================================
    // START GAME
    // ==================================================

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


        GameSession.Instance.SetSelectedSquad(
            selectedSquad
        );


        SceneManager.LoadScene(
            gameSceneName
        );
    }
}