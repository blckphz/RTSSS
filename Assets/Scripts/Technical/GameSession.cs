
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
        // Check for an existing session.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set singleton instance.
        Instance = this;

        // Keep session between scenes.
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GameSession] Initialized.");
    }


    // ============================================================
    // SET SELECTED SQUAD
    // ============================================================

    public void SetSelectedSquad(SquadSO squad)
    {
        if (squad == null)
        {
            Debug.LogWarning(
                "[GameSession] Cannot start run. Selected squad is null."
            );

            return;
        }

        SelectedSquad = squad;

        // Every new squad selection marks a new run as pending.
        NewRunPending = true;

        Debug.Log(
            "[GameSession] New run marked as pending. Squad: " +
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
        if (SelectedSquad == null)
        {
            Debug.LogWarning(
                "[GameSession] Cannot start new run. No squad selected."
            );

            return;
        }

        NewRunPending = true;

        Debug.Log(
            "[GameSession] Starting new run with squad: " +
            SelectedSquad.name
        );
    }


    // ============================================================
    // STARTING UPGRADES COMPLETE
    // ============================================================

    public void StartingUpgradesComplete()
    {
        NewRunPending = false;

    }
}
