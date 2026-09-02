using UnityEngine;

public class transitionGameManager : MonoBehaviour
{
    [Header("Map")]
    [SerializeField]
    private GameObject mapCanvas;

    [Header("Transition")]
    [SerializeField]
    private GameObject transitionObject;

    [Header("Managers")]
    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private EncounterManager encounterManager;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }
    }


    // ============================================================
    // START TRANSITION
    // ============================================================

    public void TransitionPeakProcess()
    {
        // Make sure transition is visible
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[transitionGameManager] Transition Object is not assigned!",
                this
            );
        }

        // IMPORTANT:
        // We do NOT start the encounter here.
        //
        // The animation will call
        // StartEncounterAtPeak()
        // when it reaches the peak.
    }


    // ============================================================
    // CALLED BY TRANSITION ANIMATION AT PEAK
    // ============================================================

    public void StartEncounterAtPeak()
    {
        Debug.Log(
            "[transitionGameManager] Transition reached peak."
        );


        // --------------------------------------------------------
        // HIDE MAP
        // --------------------------------------------------------

        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
        }


        // --------------------------------------------------------
        // START ENCOUNTER
        // --------------------------------------------------------

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }


        if (gameStateManager == null)
        {
            Debug.LogError(
                "[transitionGameManager] GameStateManager not found!",
                this
            );

            return;
        }


        gameStateManager.StartCombat();
    }


    // ============================================================
    // END TRANSITION
    // ============================================================

    public void EndTransition()
    {
        Debug.Log(
            "[transitionGameManager] Transition ended."
        );


        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
    }
}