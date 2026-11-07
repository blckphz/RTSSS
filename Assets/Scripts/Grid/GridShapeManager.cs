
using UnityEngine;

public class GridShapeManager : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Highlight References")]
    [SerializeField] private GridHighlightBrain highlightBrain;

    [Header("Donut Shape Settings")]
    [SerializeField, Min(0)] private int minRadius = 2;
    [SerializeField, Min(1)] private int maxRadius = 5;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (highlightBrain == null)
            highlightBrain = FindFirstObjectByType<GridHighlightBrain>();
    }

    public void ResizeGridRelative(int widthDelta, int heightDelta)
    {
        FindReferences();

        if (gridManager == null)
        {
            Debug.LogError(
                "[GridShapeManager] GridManager not found!", this);
            return;
        }

        int oldWidth = gridManager.GetWidth();
        int oldHeight = gridManager.GetHeight();

        int newWidth = Mathf.Max(1, oldWidth + widthDelta);
        int newHeight = Mathf.Max(1, oldHeight + heightDelta);

        if (newWidth == oldWidth && newHeight == oldHeight)
            return;

        gridManager.SetGridDimensions(newWidth, newHeight);

        RefreshGrid();

    }

    public void SetGridDimensionsDirect(int width, int height)
    {
        FindReferences();

        if (gridManager == null)
        {
            Debug.LogError(
                "[GridShapeManager] GridManager not found!", this);
            return;
        }

        gridManager.SetGridDimensions(
            Mathf.Max(1, width),
            Mathf.Max(1, height));

        RefreshGrid();
    }

    public void SetGridShapeDirect(GridShapeType shape)
    {
        FindReferences();

        if (gridManager == null)
            return;

        gridManager.SetGridShape(
            shape,
            gridManager.GetWidth(),
            gridManager.GetHeight(),
            gridManager.GetMinRadius(),
            gridManager.GetMaxRadius());

        RefreshGrid();
    }

    public void SetDonutSettings(int newMinRadius, int newMaxRadius)
    {
        minRadius = Mathf.Max(0, newMinRadius);
        maxRadius = Mathf.Max(minRadius, newMaxRadius);

        FindReferences();

        if (gridManager == null)
            return;

        gridManager.SetGridShape(
            GridShapeType.Donut,
            gridManager.GetWidth(),
            gridManager.GetHeight(),
            minRadius,
            maxRadius);

        RefreshGrid();
    }

    public void RefreshGrid()
    {
        FindReferences();

        if (highlightBrain == null)
            return;

        highlightBrain.RefreshGridBounds();
        highlightBrain.RefreshActiveHighlights();
    }
}
