using UnityEngine;

public enum GridShapeType
{
    Box,        // Standard rectangle / square
    Manhattan,  // Diamond shape based on Manhattan distance from center (0,0)
    Pyramid,    // Triangle tapering upward from the bottom base row to the top
    Donut       // Ring shape with a hollow center (min/max radius)
}

public static class GridShapeEvaluator
{
    public static bool IsCellInShape(
        Vector2Int position,
        GridShapeType shapeType,
        int width,
        int height,
        int minRadius = 2,
        int maxRadius = 5)
    {
        switch (shapeType)
        {
            case GridShapeType.Box:
                {
                    int minX = -(width / 2);
                    int maxX = minX + width - 1;
                    int minY = -(height / 2);
                    int maxY = minY + height - 1;

                    return position.x >= minX && position.x <= maxX &&
                           position.y >= minY && position.y <= maxY;
                }

            case GridShapeType.Manhattan:
                {
                    int radius = Mathf.Min(width, height) / 2;
                    return (Mathf.Abs(position.x) + Mathf.Abs(position.y)) <= radius;
                }

            case GridShapeType.Pyramid:
                {
                    int minY = -(height / 2);
                    int maxY = minY + height - 1;

                    if (position.y < minY || position.y > maxY)
                        return false;

                    int rowOffset = position.y - minY;
                    int currentHalfWidth = (width / 2) - rowOffset;

                    if (currentHalfWidth < 0)
                        return false;

                    return position.x >= -currentHalfWidth && position.x <= currentHalfWidth;
                }

            case GridShapeType.Donut:
                {
                    int distSq = (position.x * position.x) + (position.y * position.y);
                    int minSq = minRadius * minRadius;
                    int maxSq = maxRadius * maxRadius;

                    return distSq >= minSq && distSq <= maxSq;
                }

            default:
                return true;
        }
    }
}

public class GridManager : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("Grid References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private Transform floorParent;


    // ==================================================
    // GRID CONFIGURATION
    // ==================================================

    [Header("Grid Configuration")]
    [SerializeField, Min(1)] private int width = 11;
    [SerializeField, Min(1)] private int height = 11;
    [SerializeField] private GridShapeType gridShape = GridShapeType.Box;

    private int minRadius = 2;
    private int maxRadius = 5;


    // ==================================================
    // GRID CENTERING & HIGHLIGHTS
    // ==================================================

    [Header("Grid Centering")]
    [SerializeField] private bool centerGridAtWorldOrigin = true;

    [Header("Highlight Manager")]
    [SerializeField] private GridHighlightManager highlightManager;


    // ==================================================
    // GIZMOS
    // ==================================================

    [Header("Gizmos")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private bool showCenterGizmo = true;


    // ==================================================
    // DATA
    // ==================================================

    private GameObject[,] occupiedCells;
    private GameObject[,] floorTiles;
    private Transform gridTransform;
    private bool initialized;


    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (grid == null) grid = GetComponent<Grid>();

        if (grid == null)
        {
            Debug.LogError("[GridManager] No Grid component found!", this);
            return;
        }

        gridTransform = grid.transform;

        if (width < 1 || height < 1)
        {
            Debug.LogError("[GridManager] Width and Height must be at least 1!", this);
            return;
        }

        occupiedCells = new GameObject[width, height];
        floorTiles = new GameObject[width, height];

        if (floorParent == null) floorParent = transform;
        if (highlightManager == null) highlightManager = GetComponent<GridHighlightManager>();

        CenterGrid();
        CreateFloor();

        initialized = true;
    }


    // ==================================================
    // RUNTIME SHAPE & DIMENSION MUTATORS
    // ==================================================

    /// <summary>
    /// Dynamic helper to update only the dimensions (width and height) at runtime.
    /// </summary>
    public void SetGridDimensions(int newWidth, int newHeight, bool destroyInvalidUnits = true)
    {
        SetGridShape(gridShape, newWidth, newHeight, minRadius, maxRadius, destroyInvalidUnits);
    }

    /// <summary>
    /// Dynamic helper to update both size dimensions and grid shape at the same time.
    /// </summary>
    public void SetGridSizeAndShape(GridShapeType newShape, int newWidth, int newHeight, bool destroyInvalidUnits = true)
    {
        SetGridShape(newShape, newWidth, newHeight, minRadius, maxRadius, destroyInvalidUnits);
    }

    public void SetGridShape(
        GridShapeType newShape,
        int newWidth = -1,
        int newHeight = -1,
        int newMinRadius = -1,
        int newMaxRadius = -1,
        bool destroyInvalidUnits = true)
    {
        if (newWidth > 0) width = newWidth;
        if (newHeight > 0) height = newHeight;
        if (newMinRadius >= 0) minRadius = newMinRadius;
        if (newMaxRadius > 0) maxRadius = newMaxRadius;

        gridShape = newShape;

        GameObject[,] previousOccupants = occupiedCells;

        ClearFloorTiles();

        occupiedCells = new GameObject[width, height];
        floorTiles = new GameObject[width, height];

        CenterGrid();
        CreateFloor();

        if (previousOccupants != null)
        {
            int oldWidth = previousOccupants.GetLength(0);
            int oldHeight = previousOccupants.GetLength(1);

            int oldMinX = -(oldWidth / 2);
            int oldMinY = -(oldHeight / 2);

            for (int x = 0; x < oldWidth; x++)
            {
                for (int y = 0; y < oldHeight; y++)
                {
                    GameObject unit = previousOccupants[x, y];
                    if (unit == null) continue;

                    Vector2Int logicalPos = new Vector2Int(oldMinX + x, oldMinY + y);

                    if (IsInsideGrid(logicalPos))
                    {
                        Vector2Int newArrayPos = LogicalToArrayPosition(logicalPos);
                        occupiedCells[newArrayPos.x, newArrayPos.y] = unit;
                        unit.transform.position = GridToWorldPosition(logicalPos);
                    }
                    else
                    {
                        if (destroyInvalidUnits) Destroy(unit);
                        else unit.SetActive(false);
                    }
                }
            }
        }
    }

    private void ClearFloorTiles()
    {
        if (floorTiles == null) return;

        int lenX = floorTiles.GetLength(0);
        int lenY = floorTiles.GetLength(1);

        for (int x = 0; x < lenX; x++)
        {
            for (int y = 0; y < lenY; y++)
            {
                if (floorTiles[x, y] != null)
                {
                    Destroy(floorTiles[x, y]);
                    floorTiles[x, y] = null;
                }
            }
        }
    }


    // ==================================================
    // CENTER GRID
    // ==================================================

    private void CenterGrid()
    {
        if (!centerGridAtWorldOrigin) return;

        Vector3 centerWorld = grid.GetCellCenterWorld(new Vector3Int(width / 2, height / 2, 0));
        gridTransform.position -= centerWorld;
    }


    // ==================================================
    // LOGICAL RANGE & MAPPINGS
    // ==================================================

    public int GetMinX() => -(width / 2);
    public int GetMaxX() => GetMinX() + width - 1;
    public int GetMinY() => -(height / 2);
    public int GetMaxY() => GetMinY() + height - 1;

    private Vector2Int LogicalToArrayPosition(Vector2Int logicalPos)
    {
        return new Vector2Int(logicalPos.x - GetMinX(), logicalPos.y - GetMinY());
    }

    private Vector2Int ArrayToLogicalPosition(Vector2Int arrayPos)
    {
        return new Vector2Int(arrayPos.x + GetMinX(), arrayPos.y + GetMinY());
    }

    private Vector3Int LogicalToUnityCell(Vector2Int logicalPos)
    {
        return new Vector3Int(logicalPos.x + width / 2, logicalPos.y + height / 2, 0);
    }

    private Vector2Int UnityCellToLogical(Vector3Int unityCell)
    {
        return new Vector2Int(unityCell.x - width / 2, unityCell.y - height / 2);
    }


    // ==================================================
    // WORLD / GRID SPACE CONVERSIONS
    // ==================================================

    private bool IsGridControlledByBoard()
    {
        if (BoardViewController.Instance == null) return false;
        Transform board = BoardViewController.Instance.GetBoardTransform();
        return board != null && gridTransform.IsChildOf(board);
    }

    private Vector3 ConvertWorldToGridSpace(Vector3 worldPosition)
    {
        if (BoardViewController.Instance == null || IsGridControlledByBoard()) return worldPosition;

        Vector3 center = BoardViewController.Instance.GetRotationCenter();
        int rotation = BoardViewController.Instance.GetCurrentRotation();

        Quaternion inverseRotation = Quaternion.AngleAxis(-rotation, Vector3.forward);
        return center + (inverseRotation * (worldPosition - center));
    }

    private Vector3 ConvertGridToWorldSpace(Vector3 gridWorldPosition)
    {
        if (BoardViewController.Instance == null || IsGridControlledByBoard()) return gridWorldPosition;

        Vector3 center = BoardViewController.Instance.GetRotationCenter();
        int rotation = BoardViewController.Instance.GetCurrentRotation();

        Quaternion rotationQuaternion = Quaternion.AngleAxis(rotation, Vector3.forward);
        return center + (rotationQuaternion * (gridWorldPosition - center));
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (grid == null) return Vector2Int.zero;

        Vector3 gridSpace = ConvertWorldToGridSpace(worldPosition);
        Vector3Int unityCell = grid.WorldToCell(gridSpace);

        return UnityCellToLogical(unityCell);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        if (grid == null) return Vector3.zero;

        Vector3Int unityCell = LogicalToUnityCell(gridPosition);
        Vector3 unrotatedWorld = grid.GetCellCenterWorld(unityCell);

        return ConvertGridToWorldSpace(unrotatedWorld);
    }


    // ==================================================
    // CHECKS & FLOOR CREATION
    // ==================================================

    public bool IsInsideGrid(Vector2Int position)
    {
        return GridShapeEvaluator.IsCellInShape(position, gridShape, width, height, minRadius, maxRadius);
    }

    private void CreateFloor()
    {
        if (floorTilePrefab == null) return;

        int minX = GetMinX(), maxX = GetMaxX();
        int minY = GetMinY(), maxY = GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int logicalPos = new Vector2Int(x, y);
                if (!IsInsideGrid(logicalPos)) continue;

                Vector3 worldPos = GridToWorldPosition(logicalPos);
                GameObject tile = Instantiate(floorTilePrefab, worldPos, Quaternion.identity, floorParent);
                tile.name = $"Floor_{x}_{y}";

                Vector2Int arrayPos = LogicalToArrayPosition(logicalPos);
                floorTiles[arrayPos.x, arrayPos.y] = tile;
            }
        }
    }


    // ==================================================
    // OCCUPANTS & PLACEMENT
    // ==================================================

    private bool IsOccupantValid(GameObject occupant, Vector2Int position)
    {
        if (occupant == null) return false;

        if (!occupant.activeInHierarchy)
        {
            RemoveUnit(position);
            return false;
        }

        HealthManager health = occupant.GetComponent<HealthManager>();
        if (health != null && health.IsDead())
        {
            RemoveUnit(position);
            return false;
        }

        return true;
    }

    public bool IsCellOccupied(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return false;

        Vector2Int array = LogicalToArrayPosition(position);
        return IsOccupantValid(occupiedCells[array.x, array.y], position);
    }

    public GameObject GetUnitAt(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return null;

        Vector2Int array = LogicalToArrayPosition(position);
        GameObject unit = occupiedCells[array.x, array.y];

        return IsOccupantValid(unit, position) ? unit : null;
    }

    public Vector2Int GetUnitGridPosition(GameObject unit)
    {
        if (unit == null) return Vector2Int.zero;
        return WorldToGridPosition(unit.transform.position);
    }

    public bool CanMoveToCell(GameObject unit, Vector2Int position)
    {
        if (unit == null || !IsInsideGrid(position)) return false;

        GameObject occupant = GetUnitAt(position);
        return occupant == null || occupant == unit;
    }

    public bool PlaceUnit(GameObject unit, Vector2Int position, bool playSound = true)
    {
        if (unit == null || !IsInsideGrid(position) || IsCellOccupied(position)) return false;

        Vector2Int array = LogicalToArrayPosition(position);
        occupiedCells[array.x, array.y] = unit;
        unit.transform.position = GridToWorldPosition(position);

        return true;
    }

    public void RemoveUnit(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return;

        Vector2Int array = LogicalToArrayPosition(position);
        occupiedCells[array.x, array.y] = null;
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
                if (unit == null) continue;

                Vector2Int logical = ArrayToLogicalPosition(new Vector2Int(x, y));
                IsOccupantValid(unit, logical);
            }
        }
    }


    // ==================================================
    // MOVEMENT
    // ==================================================

    public bool StartMoveUnit(GameObject unit, Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (unit == null || !IsInsideGrid(oldPosition) || !IsInsideGrid(newPosition) || oldPosition == newPosition)
        {
            return false;
        }

        Vector2Int oldArray = LogicalToArrayPosition(oldPosition);
        Vector2Int newArray = LogicalToArrayPosition(newPosition);

        if (occupiedCells[oldArray.x, oldArray.y] != unit || IsCellOccupied(newPosition))
        {
            return false;
        }

        occupiedCells[oldArray.x, oldArray.y] = null;
        occupiedCells[newArray.x, newArray.y] = unit;

        return true;
    }

    public void FinishMoveUnit(GameObject unit, Vector2Int position)
    {
        if (unit == null || !IsInsideGrid(position)) return;

        Vector2Int array = LogicalToArrayPosition(position);
        if (occupiedCells[array.x, array.y] != unit) return;

        unit.transform.position = GridToWorldPosition(position);
    }

    public bool MoveUnit(GameObject unit, Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (!StartMoveUnit(unit, oldPosition, newPosition)) return false;
        FinishMoveUnit(unit, newPosition);
        return true;
    }


    // ==================================================
    // GETTERS & HELPERS
    // ==================================================

    public GameObject GetFloorTile(Vector2Int position)
    {
        if (!IsInsideGrid(position)) return null;
        Vector2Int array = LogicalToArrayPosition(position);
        return floorTiles[array.x, array.y];
    }

    public int GetDistance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    public int GetWidth() => width;
    public int GetHeight() => height;
    public GridShapeType GetShape() => gridShape;
    public Grid GetGrid() => grid;
    public GridHighlightManager GetHighlightManager() => highlightManager;


    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        if (grid == null) grid = GetComponent<Grid>();
        if (grid == null) return;

        Gizmos.color = Color.gray;

        int minX = GetMinX(), maxX = GetMaxX();
        int minY = GetMinY(), maxY = GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!IsInsideGrid(new Vector2Int(x, y))) continue;

                Vector3 bl = GetLogicalGridCorner(x, y);
                Vector3 br = GetLogicalGridCorner(x + 1, y);
                Vector3 tr = GetLogicalGridCorner(x + 1, y + 1);
                Vector3 tl = GetLogicalGridCorner(x, y + 1);

                Gizmos.DrawLine(bl, br);
                Gizmos.DrawLine(br, tr);
                Gizmos.DrawLine(tr, tl);
                Gizmos.DrawLine(tl, bl);
            }
        }

        if (showCenterGizmo)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = GridToWorldPosition(Vector2Int.zero);

            Gizmos.DrawSphere(center, 0.15f);
            Gizmos.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f);
            Gizmos.DrawLine(center + Vector3.down * 0.5f, center + Vector3.up * 0.5f);
        }
    }

    private Vector3 GetLogicalGridCorner(int x, int y)
    {
        Vector3Int cell = new Vector3Int(x + width / 2, y + height / 2, 0);
        return ConvertGridToWorldSpace(grid.CellToWorld(cell));
    }
}