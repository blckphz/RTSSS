using UnityEngine;

public class GridInstantiator : MonoBehaviour
{
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GridHighlightManager highlightManager;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager =
                GetComponent<GridManager>();
        }

        if (highlightManager == null)
        {
            highlightManager =
                GetComponent<GridHighlightManager>();
        }
    }

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }
}