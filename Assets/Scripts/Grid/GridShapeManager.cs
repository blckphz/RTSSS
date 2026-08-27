using UnityEngine;
using UnityEngine.InputSystem;

public class GridShapeManager : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Highlight References")]
    [SerializeField] private GridHighlightBrain highlightBrain;

    [Header("Shape Input Controls")]
    [Tooltip("Press this key to switch to Box shape")]
    [SerializeField] private Key boxKey = Key.Digit1;

    [Tooltip("Press this key to switch to Manhattan (Diamond) shape")]
    [SerializeField] private Key manhattanKey = Key.Digit2;

    [Tooltip("Press this key to switch to Pyramid shape")]
    [SerializeField] private Key pyramidKey = Key.Digit3;

    [Tooltip("Press this key to switch to Donut shape")]
    [SerializeField] private Key donutKey = Key.Digit4;

    [Header("Dynamic Resizing Controls")]
    [Tooltip("Amount to change dimensions per keypress")]
    [SerializeField, Min(1)] private int resizeStep = 2;

    [Tooltip("Key to expand grid dimensions")]
    [SerializeField] private Key expandKey = Key.Equals;

    [Tooltip("Key to shrink grid dimensions")]
    [SerializeField] private Key shrinkKey = Key.Minus;

    [Header("Donut Shape Settings")]
    [Tooltip("Inner empty radius for Donut shape")]
    [SerializeField, Min(0)] private int minRadius = 2;

    [Tooltip("Outer edge radius for Donut shape")]
    [SerializeField, Min(1)] private int maxRadius = 5;

    private void Start()
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

    private void Update()
    {
        if (Keyboard.current == null ||
            gridManager == null)
        {
            return;
        }

        // Shape switching controls

        if (Keyboard.current[boxKey].wasPressedThisFrame)
        {
            gridManager.SetGridShape(
                GridShapeType.Box
            );

            RefreshHighlightGrid();
        }

        if (Keyboard.current[manhattanKey].wasPressedThisFrame)
        {
            gridManager.SetGridShape(
                GridShapeType.Manhattan
            );

            RefreshHighlightGrid();
        }

        if (Keyboard.current[pyramidKey].wasPressedThisFrame)
        {
            gridManager.SetGridShape(
                GridShapeType.Pyramid
            );

            RefreshHighlightGrid();
        }

        if (Keyboard.current[donutKey].wasPressedThisFrame)
        {
            gridManager.SetGridShape(
                GridShapeType.Donut,
                newMinRadius: minRadius,
                newMaxRadius: maxRadius
            );

            RefreshHighlightGrid();
        }

        // Dynamic dimension resizing

        if (Keyboard.current[expandKey].wasPressedThisFrame ||
            Keyboard.current[Key.NumpadPlus].wasPressedThisFrame)
        {
            ResizeGridRelative(
                resizeStep,
                resizeStep
            );
        }

        if (Keyboard.current[shrinkKey].wasPressedThisFrame ||
            Keyboard.current[Key.NumpadMinus].wasPressedThisFrame)
        {
            ResizeGridRelative(
                -resizeStep,
                -resizeStep
            );
        }
    }

    /// <summary>
    /// Resizes the grid relative to its current dimensions.
    /// </summary>
    public void ResizeGridRelative(
        int widthDelta,
        int heightDelta)
    {
        if (gridManager == null)
            return;

        int newWidth =
            Mathf.Max(
                1,
                gridManager.GetWidth() + widthDelta
            );

        int newHeight =
            Mathf.Max(
                1,
                gridManager.GetHeight() + heightDelta
            );

        gridManager.SetGridDimensions(
            newWidth,
            newHeight
        );

        RefreshHighlightGrid();
    }

    /// <summary>
    /// Sets exact dimensions for the grid directly.
    /// </summary>
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

    /// <summary>
    /// Refreshes the highlight system after the grid
    /// has changed dimensions or shape.
    /// </summary>
    private void RefreshHighlightGrid()
    {
        if (highlightBrain == null)
        {
            highlightBrain =
                FindFirstObjectByType<GridHighlightBrain>();
        }

        if (highlightBrain != null)
        {
            highlightBrain.RefreshGridBounds();
        }
    }
}