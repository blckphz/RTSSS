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

    // Used to tell the transition what should happen at the peak
    private bool returningToMap;


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
    // MAP -> COMBAT
    // ============================================================

    public void TransitionPeakProcess()
    {
        returningToMap = false;

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
    }


    // ============================================================
    // COMBAT -> MAP
    // ============================================================

    public void TransitionToMap()
    {
        returningToMap = true;

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
        // RETURNING TO MAP
        // --------------------------------------------------------

        if (returningToMap)
        {
            if (mapCanvas != null)
            {
                mapCanvas.SetActive(true);
            }

            Debug.Log(
                "[transitionGameManager] Map enabled."
            );

            return;
        }


        // --------------------------------------------------------
        // STARTING COMBAT
        // --------------------------------------------------------

        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
        }

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