using TMPro;
using UnityEngine;

public class RoundUIManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RoundManager roundManager;

    [SerializeField]
    private TextMeshProUGUI currentRoundText;

    [SerializeField]
    private GameObject enemyTurnGameObject;


    [Header("Display")]

    [SerializeField]
    private string roundPrefix = "Round ";


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (roundManager == null)
        {
            roundManager =
                FindFirstObjectByType<RoundManager>();
        }
    }


    private void OnEnable()
    {
        if (roundManager == null)
        {
            return;
        }


        // Listen for round number changes.
        roundManager.OnRoundChanged +=
            UpdateRoundText;


        // Listen for Player/Enemy turn changes.
        roundManager.OnRoundStateChanged +=
            UpdateTurnUI;
    }


    private void Start()
    {
        if (roundManager == null)
        {
            UpdateRoundText(1);

            UpdateTurnUI(
                RoundManager.RoundState.Setup
            );

            return;
        }


        // ========================================================
        // INITIAL ROUND TEXT
        // ========================================================

        UpdateRoundText(
            roundManager.GetCurrentRound()
        );


        // ========================================================
        // INITIAL TURN UI
        // ========================================================

        UpdateTurnUI(
            roundManager.GetCurrentState()
        );
    }


    private void OnDisable()
    {
        if (roundManager == null)
        {
            return;
        }


        // Stop listening to round changes.
        roundManager.OnRoundChanged -=
            UpdateRoundText;


        // Stop listening to state changes.
        roundManager.OnRoundStateChanged -=
            UpdateTurnUI;
    }


    // ============================================================
    // ROUND TEXT
    // ============================================================

    private void UpdateRoundText(
        int round
    )
    {
        if (currentRoundText == null)
        {
            return;
        }

        currentRoundText.text =
            roundPrefix +
            round;
    }


    // ============================================================
    // TURN UI
    // ============================================================

    private void UpdateTurnUI(
        RoundManager.RoundState state
    )
    {
        if (enemyTurnGameObject == null)
        {
            return;
        }


        bool isEnemyTurn =
            state ==
            RoundManager.RoundState.EnemyTurn;


        enemyTurnGameObject.SetActive(
            isEnemyTurn
        );
    }
}
