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


    // ============================================================
    // MAP MANAGER
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
    // OPTIONAL VISUALS
    // ============================================================

    [Header("Visuals")]
    [SerializeField]
    private GameObject lockedVisual;

    [SerializeField]
    private GameObject unlockedVisual;

    [SerializeField]
    private GameObject completedVisual;


    // ============================================================
    // CLICK PROTECTION
    // ============================================================

    private bool clickedThisFrame;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        RefreshVisuals();
    }


    private void LateUpdate()
    {
        clickedThisFrame =
            false;
    }


    // ============================================================
    // UI CLICK
    // ============================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        StartLevel();
    }


    // ============================================================
    // WORLD SPACE CLICK
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
        if (clickedThisFrame)
        {
            return;
        }


        clickedThisFrame =
            true;


        // --------------------------------------------------------
        // LOCKED
        // --------------------------------------------------------

        if (!isUnlocked)
        {
            return;
        }


        // --------------------------------------------------------
        // COMPLETED
        // --------------------------------------------------------

        if (isCompleted)
        {
            return;
        }


        // --------------------------------------------------------
        // MAP MANAGER
        // --------------------------------------------------------

        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        if (mapManager == null)
        {
            return;
        }


        // --------------------------------------------------------
        // ENCOUNTER
        // --------------------------------------------------------

        if (encounter == null)
        {
            return;
        }


        // --------------------------------------------------------
        // SEND CLICK TO MAP MANAGER
        // --------------------------------------------------------

        mapManager.HandleNodeClicked(
            this
        );
    }


    // ============================================================
    // SET ENCOUNTER
    // ============================================================

    public void SetEncounter(
        EncounterDefinition newEncounter)
    {
        encounter =
            newEncounter;
    }


    // ============================================================
    // GET ENCOUNTER
    // ============================================================

    public EncounterDefinition GetEncounter()
    {
        return encounter;
    }


    // ============================================================
    // SET MAP MANAGER
    // ============================================================

    public void SetMapManager(
        LevelMapManager newMapManager)
    {
        mapManager =
            newMapManager;
    }


    // ============================================================
    // SET NODE STATE
    // ============================================================

    public void SetNodeState(
        bool unlocked,
        bool completed)
    {
        isUnlocked =
            unlocked;


        isCompleted =
            completed;


        RefreshVisuals();
    }


    // ============================================================
    // REFRESH VISUALS
    // ============================================================

    private void RefreshVisuals()
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(
                !isUnlocked &&
                !isCompleted
            );
        }


        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(
                isUnlocked &&
                !isCompleted
            );
        }


        if (completedVisual != null)
        {
            completedVisual.SetActive(
                isCompleted
            );
        }
    }


    // ============================================================
    // ACCESSORS
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