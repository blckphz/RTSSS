using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public SquadSO SelectedSquad { get; private set; }


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    // ==================================================
    // SQUAD
    // ==================================================

    public void SetSelectedSquad(
        SquadSO squad)
    {
        SelectedSquad = squad;

    }


    public SquadSO GetSelectedSquad()
    {
        return SelectedSquad;
    }
}