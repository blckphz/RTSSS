using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Pause UI")]

    [SerializeField]
    private GameObject pauseMenu;


    // ============================================================
    // MAIN MENU
    // ============================================================

    [Header("Main Menu")]

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";


    // ============================================================
    // INPUT
    // ============================================================

    [Header("Input")]

    [SerializeField]
    private InputActionReference pauseAction;


    // ============================================================
    // GAME SPEED
    // ============================================================

    [Header("Game Speed")]

    [SerializeField]
    private Image speedButtonImage;

    [SerializeField]
    private Sprite normalSpeedSprite;

    [SerializeField]
    private Sprite fastSpeedSprite;


    // ============================================================
    // INTERNAL
    // ============================================================

    private bool isPaused;

    private bool isFastSpeed;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // START GAME
        // --------------------------------------------------------

        Time.timeScale = 1f;

        isPaused = false;
        isFastSpeed = false;


        // --------------------------------------------------------
        // HIDE PAUSE MENU
        // --------------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "[EscManager] Pause Menu is not assigned.",
                this
            );
        }


        // --------------------------------------------------------
        // SET INITIAL SPEED SPRITE
        // --------------------------------------------------------

        if (speedButtonImage != null)
        {
            speedButtonImage.sprite = normalSpeedSprite;
        }
        else
        {
            Debug.LogWarning(
                "[EscManager] Speed Button Image is not assigned.",
                this
            );
        }
    }


    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();

            pauseAction.action.performed += OnPausePressed;
        }
    }


    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;

            pauseAction.action.Disable();
        }
    }


    // ============================================================
    // PAUSE INPUT
    // ============================================================

    private void OnPausePressed(
        InputAction.CallbackContext context
    )
    {
        TogglePause();
    }


    // ============================================================
    // TOGGLE PAUSE
    // ============================================================

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }


    // ============================================================
    // PAUSE
    // ============================================================

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }


        // --------------------------------------------------------
        // PAUSE
        // --------------------------------------------------------

        isPaused = true;

        Time.timeScale = 0f;


        // --------------------------------------------------------
        // SHOW UI
        // --------------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }


        Debug.Log("[EscManager] Game Paused.");
    }


    // ============================================================
    // RESUME
    // ============================================================

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }


        // --------------------------------------------------------
        // RESUME
        // --------------------------------------------------------

        isPaused = false;


        // Restore the selected game speed
        if (isFastSpeed)
        {
            Time.timeScale = 1.5f;
        }
        else
        {
            Time.timeScale = 1f;
        }


        // --------------------------------------------------------
        // HIDE UI
        // --------------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }


        Debug.Log("[EscManager] Game Resumed.");
    }


    // ============================================================
    // GAME SPEED
    // ============================================================

    public void ToggleGameSpeed()
    {
        // Switch between normal and fast speed

        isFastSpeed = !isFastSpeed;


        if (isFastSpeed)
        {
            // ----------------------------------------------------
            // FAST SPEED
            // ----------------------------------------------------

            Time.timeScale = 1.5f;


            if (speedButtonImage != null)
            {
                speedButtonImage.sprite = fastSpeedSprite;
            }


            Debug.Log("[EscManager] Game Speed: 1.5x");
        }
        else
        {
            // ----------------------------------------------------
            // NORMAL SPEED
            // ----------------------------------------------------

            Time.timeScale = 1f;


            if (speedButtonImage != null)
            {
                speedButtonImage.sprite = normalSpeedSprite;
            }


            Debug.Log("[EscManager] Game Speed: 1x");
        }
    }


    // ============================================================
    // MAIN MENU
    // ============================================================

    public void GoToMainMenu()
    {
        // --------------------------------------------------------
        // RESTORE TIME
        // --------------------------------------------------------

        Time.timeScale = 1f;


        // --------------------------------------------------------
        // LOAD MAIN MENU
        // --------------------------------------------------------

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }
}