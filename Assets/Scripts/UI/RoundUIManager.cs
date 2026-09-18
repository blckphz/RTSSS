using TMPro;
using UnityEngine;

public class RoundUIManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RoundManager roundManager;

    [SerializeField]
    private TextMeshProUGUI currentRoundText;


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

        roundManager.OnRoundChanged +=
            UpdateRoundText;
    }


    private void Start()
    {
        if (roundManager == null)
        {
            UpdateRoundText(1);
            return;
        }

        UpdateRoundText(
            roundManager.GetCurrentRound()
        );
    }


    private void OnDisable()
    {
        if (roundManager == null)
        {
            return;
        }

        roundManager.OnRoundChanged -=
            UpdateRoundText;
    }


    // ============================================================
    // ROUND TEXT
    // ============================================================

    private void UpdateRoundText(int round)
    {
        if (currentRoundText == null)
        {
            return;
        }

        currentRoundText.text =
            roundPrefix + round;
    }
}