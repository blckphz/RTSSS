using UnityEngine;
using UnityEngine.EventSystems;

public class IconBehav :
    MonoBehaviour,
    IPointerClickHandler
{
    // ============================================================
    // ENCOUNTER
    // ============================================================

    [Header("Encounter")]
    [SerializeField]
    private EncounterDefinition encounter;

    [SerializeField]
    private EncounterManager encounterManager;


    // ============================================================
    // MAP
    // ============================================================

    [Header("Map")]
    [SerializeField]
    private LevelMapManager mapManager;


    // ============================================================
    // NODE STATE
    // ============================================================

    [Header("Node State")]
    [SerializeField]
    private bool isUnlocked;

    [SerializeField]
    private bool isCompleted;


    // ============================================================
    // VISUALS
    // ============================================================

    [Header("Visuals")]
    [SerializeField]
    private GameObject lockedVisual;

    [SerializeField]
    private GameObject unlockedVisual;

    [SerializeField]
    private GameObject completedVisual;


    // ============================================================
    // TRANSITION
    // ============================================================

    [Header("Transition")]
    [SerializeField]
    private transitionGameManager transitionManager;


    // ============================================================
    // CLICK PROTECTION
    // ============================================================

    private bool clickedThisFrame;


    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // MAP MANAGER
        // --------------------------------------------------------

        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        // --------------------------------------------------------
        // ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        // --------------------------------------------------------
        // TRANSITION MANAGER
        // --------------------------------------------------------

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        // --------------------------------------------------------
        // VISUALS
        // --------------------------------------------------------

        RefreshVisuals();
    }


    private void LateUpdate()
    {
        clickedThisFrame = false;
    }


    // ============================================================
    // POINTER CLICK
    // ============================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        StartLevel();
    }


    // ============================================================
    // MOUSE CLICK
    // ============================================================

    private void OnMouseDown()
    {
        StartLevel();
    }


    // ============================================================
    // START LEVEL
    // ============================================================

    public void StartLevel()
    {
        // --------------------------------------------------------
        // PREVENT DOUBLE CLICK
        // --------------------------------------------------------

        if (clickedThisFrame)
            return;

        clickedThisFrame = true;


        // --------------------------------------------------------
        // CHECK UNLOCKED
        // --------------------------------------------------------

        if (!isUnlocked)
        {
            Debug.Log(
                "[IconBehav] Node is locked.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK COMPLETED
        // --------------------------------------------------------

        if (isCompleted)
        {
            Debug.Log(
                "[IconBehav] Node is already completed.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK ENCOUNTER
        // --------------------------------------------------------

        if (encounter == null)
        {
            Debug.LogError(
                "[IconBehav] No EncounterDefinition assigned!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // FIND ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (encounterManager == null)
        {
            Debug.LogError(
                "[IconBehav] EncounterManager was not found in the scene!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // SET CURRENT ENCOUNTER
        // --------------------------------------------------------
        //
        // We do NOT start the encounter here.
        //
        // We only tell EncounterManager which encounter was
        // selected.
        //
        // The actual encounter will start when the transition
        // reaches its peak.
        // --------------------------------------------------------

        encounterManager.SetCurrentEncounter(
            encounter
        );


        // --------------------------------------------------------
        // FIND TRANSITION MANAGER
        // --------------------------------------------------------

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        if (transitionManager == null)
        {
            Debug.LogError(
                "[IconBehav] transitionGameManager was not found in the scene!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // START TRANSITION
        // --------------------------------------------------------
        //
        // IMPORTANT:
        // This should ONLY start the transition animation.
        //
        // It should NOT start the encounter yet.
        //
        // The transition animation will call:
        //
        // StartEncounterAtPeak()
        //
        // using an Animation Event.
        // --------------------------------------------------------

        transitionManager.TransitionPeakProcess();
    }


    // ============================================================
    // ENCOUNTER
    // ============================================================

    public void SetEncounter(
        EncounterDefinition newEncounter)
    {
        encounter = newEncounter;
    }


    public EncounterDefinition GetEncounter()
    {
        return encounter;
    }


    // ============================================================
    // ENCOUNTER MANAGER
    // ============================================================

    public void SetEncounterManager(
        EncounterManager newEncounterManager)
    {
        encounterManager =
            newEncounterManager;
    }


    // ============================================================
    // MAP MANAGER
    // ============================================================

    public void SetMapManager(
        LevelMapManager newMapManager)
    {
        mapManager =
            newMapManager;
    }


    // ============================================================
    // NODE STATE
    // ============================================================

    public void SetNodeState(
        bool unlocked,
        bool completed)
    {
        isUnlocked = unlocked;
        isCompleted = completed;

        RefreshVisuals();
    }


    // ============================================================
    // REFRESH VISUALS
    // ============================================================

    private void RefreshVisuals()
    {
        // --------------------------------------------------------
        // LOCKED
        // --------------------------------------------------------

        if (lockedVisual != null)
        {
            lockedVisual.SetActive(
                !isUnlocked &&
                !isCompleted
            );
        }


        // --------------------------------------------------------
        // UNLOCKED
        // --------------------------------------------------------

        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(
                isUnlocked &&
                !isCompleted
            );
        }


        // --------------------------------------------------------
        // COMPLETED
        // --------------------------------------------------------

        if (completedVisual != null)
        {
            completedVisual.SetActive(
                isCompleted
            );
        }
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public bool IsUnlocked()
    {
        return isUnlocked;
    }


    public bool IsCompleted()
    {
        return isCompleted;
    }
}