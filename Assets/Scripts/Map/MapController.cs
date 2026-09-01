using UnityEngine;


// ============================================================
// MAP CONTROLLER
// ============================================================

public class MapController : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Map References")]

    [Tooltip(
        "The root GameObject containing the entire map."
    )]
    [SerializeField]
    private GameObject mapRoot;


    // ============================================================
    // STARTUP
    // ============================================================

    [Header("Startup")]

    [Tooltip(
        "If enabled, the map will be visible when the scene starts."
    )]
    [SerializeField]
    private bool showMapOnStart = true;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (mapRoot == null)
        {
            mapRoot = gameObject;
        }
    }


    private void Start()
    {
        if (showMapOnStart)
        {
            ShowMap();
        }
        else
        {
            HideMap();
        }
    }


    // ============================================================
    // SHOW MAP
    // ============================================================

    public void ShowMap()
    {
        if (mapRoot == null)
        {
            Debug.LogError(
                "[MapController] Map Root is missing!",
                this
            );

            return;
        }


        mapRoot.SetActive(true);

    }


    // ============================================================
    // HIDE MAP
    // ============================================================

    public void HideMap()
    {
        if (mapRoot == null)
        {
            Debug.LogError(
                "[MapController] Map Root is missing!",
                this
            );

            return;
        }


        mapRoot.SetActive(false);


        Debug.Log(
            "[MapController] Map disabled.",
            this
        );
    }


    // ============================================================
    // TOGGLE MAP
    // ============================================================

    public void ToggleMap()
    {
        if (mapRoot == null)
        {
            Debug.LogError(
                "[MapController] Map Root is missing!",
                this
            );

            return;
        }


        if (mapRoot.activeSelf)
        {
            HideMap();
        }
        else
        {
            ShowMap();
        }
    }


    // ============================================================
    // STATE
    // ============================================================

    public bool IsMapVisible()
    {
        if (mapRoot == null)
        {
            return false;
        }


        return mapRoot.activeSelf;
    }
}