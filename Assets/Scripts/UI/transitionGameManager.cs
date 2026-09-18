using System.Collections;
using UnityEngine;

public class transitionGameManager : MonoBehaviour
{
    // ============================================================
    // MAP
    // ============================================================

    [Header("Map")]
    [SerializeField]
    private GameObject mapCanvas;


    // ============================================================
    // TRANSITION
    // ============================================================

    [Header("Transition")]
    [SerializeField]
    private GameObject transitionObject;


    // ============================================================
    // MANAGERS
    // ============================================================

    [Header("Managers")]
    [SerializeField]
    private GameStateManager gameStateManager;

    [SerializeField]
    private EncounterManager encounterManager;


    // ============================================================
    // INTERNAL
    // ============================================================

    // false = Map -> Combat
    //
    // true = Combat -> Map

    private bool returningToMap;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // FIND GAME STATE MANAGER
        // --------------------------------------------------------

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }


        // --------------------------------------------------------
        // FIND ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        // --------------------------------------------------------
        // CHECK MAP
        // --------------------------------------------------------

        if (mapCanvas == null)
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "Map Canvas is not assigned!",
                this
            );
        }


        // --------------------------------------------------------
        // CHECK TRANSITION
        // --------------------------------------------------------

        if (transitionObject == null)
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "Transition Object is not assigned!",
                this
            );
        }
    }


    // ============================================================
    // MAP -> COMBAT
    // ============================================================

    public void TransitionPeakProcess()
    {
        returningToMap = false;


        // --------------------------------------------------------
        // START TRANSITION ANIMATION
        // --------------------------------------------------------

        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "Transition Object is not assigned!",
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


        Debug.Log(
            "[transitionGameManager] " +
            "Starting transition COMBAT -> MAP."
        );


        // --------------------------------------------------------
        // START TRANSITION ANIMATION
        // --------------------------------------------------------

        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "Transition Object is not assigned!",
                this
            );
        }
    }


    // ============================================================
    // TRANSITION PEAK
    // ============================================================
    //
    // This method is called by the animation event through
    // transitionLinkManager.
    //
    // ============================================================

    public void StartEncounterAtPeak()
    {


        // ========================================================
        // RETURNING TO MAP
        // ========================================================

        if (returningToMap)
        {
            EnableMap();


            // ----------------------------------------------------
            // MAKE SURE STATE IS MAP
            // ----------------------------------------------------

            if (gameStateManager == null)
            {
                gameStateManager =
                    FindFirstObjectByType<GameStateManager>();
            }


            if (gameStateManager != null)
            {
                gameStateManager.SetGameState(
                    GameStateManager.GameState.Map
                );
            }


            return;
        }


        // ========================================================
        // STARTING COMBAT
        // ========================================================

        DisableMap();


        // --------------------------------------------------------
        // FIND GAME STATE MANAGER
        // --------------------------------------------------------

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }


        if (gameStateManager == null)
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "GameStateManager not found!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // START COMBAT
        // --------------------------------------------------------

        gameStateManager.StartCombat();
    }


    // ============================================================
    // ENABLE MAP
    // ============================================================

    private void EnableMap()
    {
        if (mapCanvas == null)
        {
            Debug.LogError(
                "[transitionGameManager] " +
                "Cannot enable map. " +
                "mapCanvas is NULL!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // ENABLE
        // --------------------------------------------------------

        mapCanvas.SetActive(true);


        Debug.Log(
            "[transitionGameManager] Map enabled.\n" +
            "Object: " +
            mapCanvas.name +
            "\n" +
            "activeSelf: " +
            mapCanvas.activeSelf +
            "\n" +
            "activeInHierarchy: " +
            mapCanvas.activeInHierarchy +
            "\n" +
            "Parent: " +
            (
                mapCanvas.transform.parent != null
                    ? mapCanvas.transform.parent.name
                    : "NONE"
            ),
            mapCanvas
        );


        // --------------------------------------------------------
        // CHECK NEXT FRAME
        // --------------------------------------------------------

        StartCoroutine(
            CheckMapNextFrame()
        );
    }


    // ============================================================
    // DISABLE MAP
    // ============================================================

    private void DisableMap()
    {
        if (mapCanvas == null)
        {
            Debug.LogWarning(
                "[transitionGameManager] " +
                "Cannot disable map. " +
                "mapCanvas is NULL!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // DISABLE
        // --------------------------------------------------------

        mapCanvas.SetActive(false);
    }


    // ============================================================
    // MAP DEBUG CHECK
    // ============================================================

    private IEnumerator CheckMapNextFrame()
    {
        yield return null;


        if (mapCanvas == null)
        {
            yield break;
        }


        Debug.Log(
            "[transitionGameManager] " +
            "MAP CHECK - NEXT FRAME\n" +
            "Object: " +
            mapCanvas.name +
            "\n" +
            "activeSelf: " +
            mapCanvas.activeSelf +
            "\n" +
            "activeInHierarchy: " +
            mapCanvas.activeInHierarchy,
            mapCanvas
        );


        // --------------------------------------------------------
        // CHECK ACTIVE SELF
        // --------------------------------------------------------

        if (!mapCanvas.activeSelf)
        {
            Debug.LogWarning(
                "[transitionGameManager] WARNING: " +
                "Map was turned OFF by another script!",
                mapCanvas
            );
        }


        // --------------------------------------------------------
        // CHECK HIERARCHY
        // --------------------------------------------------------

        if (!mapCanvas.activeInHierarchy)
        {
            Debug.LogWarning(
                "[transitionGameManager] WARNING: " +
                "Map is activeSelf TRUE but NOT active " +
                "in hierarchy. Check the Map Canvas " +
                "parent objects!",
                mapCanvas
            );
        }
    }


    // ============================================================
    // END TRANSITION
    // ============================================================

    public void EndTransition()
    {

        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "[transitionGameManager] " +
                "Transition Object is not assigned!",
                this
            );
        }
    }
}