using UnityEngine;

public class GameSession : MonoBehaviour
{
    // ============================================================
    // SINGLETON
    // ============================================================

    public static GameSession Instance { get; private set; }


    // ============================================================
    // SELECTED SQUAD
    // ============================================================

    public SquadSO SelectedSquad { get; private set; }


    // ============================================================
    // NEW RUN
    // ============================================================

    public bool NewRunPending { get; private set; }


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // CHECK FOR EXISTING SESSION
        // --------------------------------------------------------

        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);

            return;
        }


        // --------------------------------------------------------
        // SET INSTANCE
        // --------------------------------------------------------

        Instance = this;


        // --------------------------------------------------------
        // KEEP BETWEEN SCENES
        // --------------------------------------------------------

        DontDestroyOnLoad(gameObject);
    }


    // ============================================================
    // SET SELECTED SQUAD
    // ============================================================

    public void SetSelectedSquad(
        SquadSO squad
    )
    {
        if (squad == null)
        {
            Debug.LogError(
                "[GameSession] " +
                "Cannot set selected squad to null.",
                this
            );

            return;
        }


        SelectedSquad =
            squad;


        Debug.Log(
            "[GameSession] Selected squad: " +
            squad.name
        );
    }


    // ============================================================
    // GET SELECTED SQUAD
    // ============================================================

    public SquadSO GetSelectedSquad()
    {
        return SelectedSquad;
    }


    // ============================================================
    // START NEW RUN
    // ============================================================

    public void StartNewRun()
    {
        // --------------------------------------------------------
        // MARK NEW RUN AS PENDING
        // --------------------------------------------------------

        NewRunPending = true;


        Debug.Log(
            "[GameSession] Starting new run. " +
            "NewRunPending = " +
            NewRunPending
        );


        // --------------------------------------------------------
        // LOAD GAMEPLAY SCENE
        // --------------------------------------------------------
        //
        // KEEP YOUR EXISTING SCENE-LOADING CODE HERE.
        //
        // Example:
        //
        // SceneManager.LoadScene("Gameplay");
        //
        // OR:
        //
        // Your existing transition manager call.
        //
        // --------------------------------------------------------

    }


    // ============================================================
    // STARTING UPGRADES COMPLETE
    // ============================================================

    public void StartingUpgradesComplete()
    {
        // --------------------------------------------------------
        // CLEAR NEW RUN FLAG
        // --------------------------------------------------------

        NewRunPending = false;


        Debug.Log(
            "[GameSession] Starting upgrades complete. " +
            "NewRunPending = " +
            NewRunPending
        );
    }
}