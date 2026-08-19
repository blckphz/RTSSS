using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private Transform floorParent;

    [Header("Grid Size")]
    [SerializeField, Min(1)] private int width = 8;
    [SerializeField, Min(1)] private int height = 8;

    [Header("Hovered Tile")]
    [SerializeField, Range(0f, 1f)] private float hoveredTileAlpha = 0.5f;

    [Header("Gizmos")]
    [SerializeField] private bool showGridGizmos = true;

    // ==================================================
    // CORE DATA
    // ==================================================

    private GameObject[,] occupiedCells;
    private GameObject[,] floorTiles;

    // ==================================================
    // HOVER STATE
    // ==================================================

    private GameObject hoveredTile;
    private SpriteRenderer[] hoveredTileRenderers;
    private Color[] hoveredTileOriginalColors;

    // ==================================================
    // UNITY LIFECYCLE
    // ==================================================

    private void Awake()
    {
        // Grid Verification
        if (grid == null)
        {
            grid = GetComponent<Grid>();
        }

        if (grid == null) return;

        // Array Initialization
        occupiedCells = new GameObject[width, height];
        floorTiles = new GameObject[width, height];

        // Floor Setup
        if (floorParent == null)
        {
            floorParent = transform;
        }

        CreateFloor();
    }

    // ==================================================
    // FLOOR CREATION
    // ==================================================

    private void CreateFloor()
    {
        if (floorTilePrefab == null) return;

        if (floorParent == null)
        {
            floorParent = transform;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector3 worldPosition = GridToWorldPosition(gridPosition);

                GameObject floorTile = Instantiate(
                    floorTilePrefab,
                    worldPosition,
                    Quaternion.identity,
                    floorParent
                );

                floorTile.name = $"Floor_{x}_{y}";
                floorTiles[x, y] = floorTile;
            }
        }
    }

    // ==================================================
    // POSITION CONVERSION
    // ==================================================

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (grid == null) return Vector2Int.zero;

        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        return new Vector2Int(cellPosition.x, cellPosition.y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        if (grid == null) return Vector3.zero;

        Vector3Int cellPosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        return grid.GetCellCenterWorld(cellPosition);
    }

    // ==================================================
    // GRID CHECKS & HELPER CLEANUP
    // ==================================================

    public bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 && position.x < width &&
               position.y >= 0 && position.y < height;
    }

    /// <summary>
    /// Checks if a cell occupant is active and alive. Clears cell reference if dead/inactive.
    /// </summary>
    private bool IsOccupantValid(GameObject occupant, Vector2Int position)
    {
        if (occupant == null) return false;

        // Inactive check
        if (!occupant.activeInHierarchy)
        {
            occupiedCells[position.x, position.y] = null;
            return false;
        }

        // Dead health check
        HealthManager health = occupant.GetComponent<HealthManager>();
        if (health != null && health.IsDead())
        {
            occupiedCells[position.x, position.y] = null;
            return false;
        }

        return true;
    }

    public bool IsCellOccupied(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return false;

        GameObject occupant = occupiedCells[position.x, position.y];
        return IsOccupantValid(occupant, position);
    }

    public GameObject GetUnitAt(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return null;

        GameObject occupant = occupiedCells[position.x, position.y];
        return IsOccupantValid(occupant, position) ? occupant : null;
    }

    public GameObject GetFloorTile(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return null;
        return floorTiles[position.x, position.y];
    }

    // ==================================================
    // HOVERED TILE MANAGEMENT
    // ==================================================

    public void SetHoveredTile(Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            ClearHoveredTile();
            return;
        }

        GameObject tile = floorTiles[position.x, position.y];

        if (tile == null)
        {
            ClearHoveredTile();
            return;
        }

        if (tile == hoveredTile) return;

        ClearHoveredTile();

        hoveredTile = tile;
        hoveredTileRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);

        if (hoveredTileRenderers == null || hoveredTileRenderers.Length == 0)
        {
            hoveredTile = null;
            return;
        }

        hoveredTileOriginalColors = new Color[hoveredTileRenderers.Length];

        for (int i = 0; i < hoveredTileRenderers.Length; i++)
        {
            SpriteRenderer sr = hoveredTileRenderers[i];
            if (sr == null) continue;

            hoveredTileOriginalColors[i] = sr.color;

            Color modifiedColor = sr.color;
            modifiedColor.a *= hoveredTileAlpha;
            sr.color = modifiedColor;
        }
    }

    public void ClearHoveredTile()
    {
        if (hoveredTileRenderers != null && hoveredTileOriginalColors != null)
        {
            int count = Mathf.Min(hoveredTileRenderers.Length, hoveredTileOriginalColors.Length);

            for (int i = 0; i < count; i++)
            {
                if (hoveredTileRenderers[i] != null)
                {
                    hoveredTileRenderers[i].color = hoveredTileOriginalColors[i];
                }
            }
        }

        hoveredTile = null;
        hoveredTileRenderers = null;
        hoveredTileOriginalColors = null;
    }

    // ==================================================
    // UNIT MANAGEMENT
    // ==================================================

    public bool PlaceUnit(GameObject unit, Vector2Int position)
    {
        if (unit == null || !IsInsideGrid(position) || IsCellOccupied(position))
        {
            return false;
        }

        occupiedCells[position.x, position.y] = unit;
        unit.transform.position = GridToWorldPosition(position);

        return true;
    }

    public void RemoveUnit(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return;

        occupiedCells[position.x, position.y] = null;
    }

    public void RemoveUnit(GameObject unit)
    {
        if (unit == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupiedCells[x, y] == unit)
                {
                    occupiedCells[x, y] = null;
                    return;
                }
            }
        }
    }

    public void CleanupDeadUnits()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject unit = occupiedCells[x, y];
                if (unit != null)
                {
                    IsOccupantValid(unit, new Vector2Int(x, y));
                }
            }
        }
    }

    public bool MoveUnit(GameObject unit, Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (unit == null || !IsInsideGrid(oldPosition) || !IsInsideGrid(newPosition))
        {
            return false;
        }

        if (occupiedCells[oldPosition.x, oldPosition.y] != unit)
        {
            return false;
        }

        if (IsCellOccupied(newPosition))
        {
            return false;
        }

        occupiedCells[oldPosition.x, oldPosition.y] = null;
        occupiedCells[newPosition.x, newPosition.y] = unit;
        unit.transform.position = GridToWorldPosition(newPosition);

        return true;
    }

    // ==================================================
    // DISTANCE & GETTERS
    // ==================================================

    public int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public int GetWidth() => width;
    public int GetHeight() => height;

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;

        if (grid == null)
        {
            grid = GetComponent<Grid>();
            if (grid == null) return;
        }

        DrawGridLines();
        DrawGridBorder();
    }

    private void DrawGridLines()
    {
        Gizmos.color = Color.gray;

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(GetGridCorner(x, 0), GetGridCorner(x, height));
        }

        for (int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(GetGridCorner(0, y), GetGridCorner(width, y));
        }
    }

    private void DrawGridBorder()
    {
        Gizmos.color = Color.white;

        Vector3 bottomLeft = GetGridCorner(0, 0);
        Vector3 bottomRight = GetGridCorner(width, 0);
        Vector3 topLeft = GetGridCorner(0, height);
        Vector3 topRight = GetGridCorner(width, height);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    private Vector3 GetGridCorner(int x, int y)
    {
        return grid != null
            ? grid.CellToWorld(new Vector3Int(x, y, 0))
            : Vector3.zero;
    }
}