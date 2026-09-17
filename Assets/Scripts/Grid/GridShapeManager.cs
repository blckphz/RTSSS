using UnityEngine;

public class GridShapeManager : MonoBehaviour
{
    // ============================================================
    // GRID REFERENCE
    // ============================================================

    [Header("Grid Reference")]
    [SerializeField]
    private GridManager gridManager;


    // ============================================================
    // HIGHLIGHT REFERENCES
    // ============================================================

    [Header("Highlight References")]
    [SerializeField]
    private GridHighlightBrain highlightBrain;


    // ============================================================
    // DONUT SETTINGS
    // ============================================================

    [Header("Donut Shape Settings")]

    [Tooltip("Inner empty radius for Donut shape")]
    [SerializeField, Min(0)]
    private int minRadius = 2;

    [Tooltip("Outer edge radius for Donut shape")]
    [SerializeField, Min(1)]
    private int maxRadius = 5;


    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        FindReferences();
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (highlightBrain == null)
        {
            highlightBrain =
                FindFirstObjectByType<GridHighlightBrain>();
        }


        if (gridManager == null)
        {
            Debug.LogError(
                "[GridShapeManager] No GridManager instance found in scene!",
                this
            );
        }


        if (highlightBrain == null)
        {
            Debug.LogWarning(
                "[GridShapeManager] No GridHighlightBrain instance found in scene!",
                this
            );
        }
    }


    // ============================================================
    // RELATIVE RESIZE
    // ============================================================

    public void ResizeGridRelative(
        int widthDelta,
        int heightDelta)
    {
        if (gridManager == null)
            return;


        int newWidth =
            Mathf.Max(
                1,
                gridManager.GetWidth() +
                widthDelta
            );


        int newHeight =
            Mathf.Max(
                1,
                gridManager.GetHeight() +
                heightDelta
            );


        gridManager.SetGridDimensions(
            newWidth,
            newHeight
        );


        RefreshHighlightGrid();
    }


    // ============================================================
    // DIRECT RESIZE
    // ============================================================

    public void SetGridDimensionsDirect(
        int width,
        int height)
    {
        if (gridManager == null)
            return;


        gridManager.SetGridDimensions(
            width,
            height
        );


        RefreshHighlightGrid();
    }


    // ============================================================
    // DIRECT SHAPE SETTING
    // ============================================================

    public void SetGridShapeDirect(
        GridShapeType shape)
    {
        if (gridManager == null)
            return;


        gridManager.SetGridShape(
            shape,
            gridManager.GetWidth(),
            gridManager.GetHeight(),
            gridManager.GetMinRadius(),
            gridManager.GetMaxRadius()
        );


        RefreshHighlightGrid();
    }


    // ============================================================
    // DONUT SETTINGS
    // ============================================================

    public void SetDonutSettings(
        int newMinRadius,
        int newMaxRadius)
    {
        minRadius =
            Mathf.Max(
                0,
                newMinRadius
            );


        maxRadius =
            Mathf.Max(
                minRadius,
                newMaxRadius
            );


        if (gridManager == null)
            return;


        gridManager.SetGridShape(
            GridShapeType.Donut,
            gridManager.GetWidth(),
            gridManager.GetHeight(),
            minRadius,
            maxRadius
        );


        RefreshHighlightGrid();
    }


    // ============================================================
    // HIGHLIGHT REFRESH
    // ============================================================

    private void RefreshHighlightGrid()
    {
        if (highlightBrain == null)
        {
            highlightBrain =
                FindFirstObjectByType<GridHighlightBrain>();
        }


        if (highlightBrain == null)
            return;


        highlightBrain.RefreshGridBounds();
        highlightBrain.RefreshActiveHighlights();
    }
}
