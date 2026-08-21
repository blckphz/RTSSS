using UnityEngine;

public class GridManager : MonoBehaviour
{
    // ==================================================
    // GRID REFERENCES
    // ==================================================

    [Header("Grid References")]
    [SerializeField]
    private Grid grid;

    [SerializeField]
    private GameObject floorTilePrefab;

    [SerializeField]
    private Transform floorParent;


    // ==================================================
    // GRID SIZE
    // ==================================================

    [Header("Grid Size")]
    [SerializeField, Min(1)]
    private int width = 8;

    [SerializeField, Min(1)]
    private int height = 8;


    // ==================================================
    // HIGHLIGHT MANAGER
    // ==================================================

    [Header("Highlight Manager")]
    [SerializeField]
    private GridHighlightManager highlightManager;


    // ==================================================
    // GIZMOS
    // ==================================================

    [Header("Gizmos")]
    [SerializeField]
    private bool showGridGizmos = true;


    // ==================================================
    // CORE DATA
    // ==================================================

    private GameObject[,] occupiedCells;

    private GameObject[,] floorTiles;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        Initialize();
    }


    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void Initialize()
    {
        if (grid == null)
        {
            grid = GetComponent<Grid>();
        }


        if (grid == null)
        {
            Debug.LogError(
                "[GridManager] No Grid component found!",
                this
            );

            return;
        }


        occupiedCells =
            new GameObject[width, height];


        floorTiles =
            new GameObject[width, height];


        if (floorParent == null)
        {
            floorParent = transform;
        }


        if (highlightManager == null)
        {
            highlightManager =
                GetComponent<GridHighlightManager>();
        }


        CreateFloor();
    }


    // ==================================================
    // FLOOR CREATION
    // ==================================================

    private void CreateFloor()
    {
        if (floorTilePrefab == null)
        {
            Debug.LogWarning(
                "[GridManager] Floor Tile Prefab is not assigned!",
                this
            );

            return;
        }


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);


                GameObject tile =
                    Instantiate(
                        floorTilePrefab,
                        GridToWorldPosition(position),
                        Quaternion.identity,
                        floorParent
                    );


                tile.name =
                    $"Floor_{x}_{y}";


                floorTiles[x, y] =
                    tile;
            }
        }
    }


    // ==================================================
    // POSITION CONVERSION
    // ==================================================

    public Vector2Int WorldToGridPosition(
        Vector3 worldPosition)
    {
        if (grid == null)
        {
            return Vector2Int.zero;
        }


        Vector3Int cell =
            grid.WorldToCell(worldPosition);


        return new Vector2Int(
            cell.x,
            cell.y
        );
    }


    public Vector3 GridToWorldPosition(
        Vector2Int gridPosition)
    {
        if (grid == null)
        {
            return Vector3.zero;
        }


        Vector3Int cell =
            new Vector3Int(
                gridPosition.x,
                gridPosition.y,
                0
            );


        return grid.GetCellCenterWorld(cell);
    }


    // ==================================================
    // GRID CHECKS
    // ==================================================

    public bool IsInsideGrid(
        Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.x < width &&
            position.y >= 0 &&
            position.y < height;
    }


    // ==================================================
    // FLOOR
    // ==================================================

    public GameObject GetFloorTile(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return null;
        }


        return floorTiles[
            position.x,
            position.y
        ];
    }


    // ==================================================
    // UNIT VALIDATION
    // ==================================================

    private bool IsOccupantValid(
        GameObject occupant,
        Vector2Int position)
    {
        if (occupant == null)
        {
            return false;
        }


        if (!occupant.activeInHierarchy)
        {
            occupiedCells[
                position.x,
                position.y
            ] = null;

            return false;
        }


        HealthManager health =
            occupant.GetComponent<HealthManager>();


        if (
            health != null &&
            health.IsDead()
        )
        {
            occupiedCells[
                position.x,
                position.y
            ] = null;

            return false;
        }


        return true;
    }


    // ==================================================
    // UNIT QUERIES
    // ==================================================

    public bool IsCellOccupied(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return false;
        }


        GameObject occupant =
            occupiedCells[
                position.x,
                position.y
            ];


        return IsOccupantValid(
            occupant,
            position
        );
    }


    public GameObject GetUnitAt(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return null;
        }


        GameObject occupant =
            occupiedCells[
                position.x,
                position.y
            ];


        return IsOccupantValid(
            occupant,
            position
        )
            ? occupant
            : null;
    }


    // ==================================================
    // GET UNIT GRID POSITION
    // ==================================================

    public Vector2Int GetUnitGridPosition(
        GameObject unit)
    {
        if (unit == null)
        {
            return Vector2Int.zero;
        }


        return WorldToGridPosition(
            unit.transform.position
        );
    }


    // ==================================================
    // CHECK MOVEMENT
    // ==================================================

    public bool CanMoveToCell(
        GameObject unit,
        Vector2Int position)
    {
        if (unit == null)
        {
            return false;
        }


        if (!IsInsideGrid(position))
        {
            return false;
        }


        GameObject occupant =
            GetUnitAt(position);


        if (
            occupant != null &&
            occupant != unit
        )
        {
            return false;
        }


        return true;
    }


    // ==================================================
    // UNIT MANAGEMENT
    // ==================================================

    public bool PlaceUnit(
        GameObject unit,
        Vector2Int position,
        bool playSound = true)
    {
        if (unit == null)
        {
            return false;
        }


        if (!IsInsideGrid(position))
        {
            return false;
        }


        if (IsCellOccupied(position))
        {
            return false;
        }


        // --------------------------------------------------
        // REGISTER UNIT
        // --------------------------------------------------

        occupiedCells[
            position.x,
            position.y
        ] = unit;


        // --------------------------------------------------
        // MOVE UNIT TO GRID POSITION
        // --------------------------------------------------

        unit.transform.position =
            GridToWorldPosition(position);

        return true;
    }


    public void RemoveUnit(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return;
        }


        occupiedCells[
            position.x,
            position.y
        ] = null;
    }


    public void RemoveUnit(
        GameObject unit)
    {
        if (unit == null)
        {
            return;
        }


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
                GameObject unit =
                    occupiedCells[x, y];


                if (unit != null)
                {
                    IsOccupantValid(
                        unit,
                        new Vector2Int(x, y)
                    );
                }
            }
        }
    }


    // ==================================================
    // START MOVEMENT
    // ==================================================

    public bool StartMoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (unit == null)
        {
            return false;
        }


        if (
            !IsInsideGrid(oldPosition) ||
            !IsInsideGrid(newPosition)
        )
        {
            return false;
        }


        if (oldPosition == newPosition)
        {
            return false;
        }


        if (
            occupiedCells[
                oldPosition.x,
                oldPosition.y
            ] != unit
        )
        {
            return false;
        }


        if (IsCellOccupied(newPosition))
        {
            return false;
        }


        // --------------------------------------------------
        // REMOVE OLD CELL
        // --------------------------------------------------

        occupiedCells[
            oldPosition.x,
            oldPosition.y
        ] = null;


        // --------------------------------------------------
        // RESERVE DESTINATION
        // --------------------------------------------------

        occupiedCells[
            newPosition.x,
            newPosition.y
        ] = unit;


        return true;
    }


    // ==================================================
    // FINISH MOVEMENT
    // ==================================================

    public void FinishMoveUnit(
        GameObject unit,
        Vector2Int position)
    {
        if (unit == null)
        {
            return;
        }


        if (!IsInsideGrid(position))
        {
            return;
        }


        if (
            occupiedCells[
                position.x,
                position.y
            ] != unit
        )
        {
            return;
        }


        unit.transform.position =
            GridToWorldPosition(position);
    }


    // ==================================================
    // INSTANT MOVEMENT
    // ==================================================

    public bool MoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (unit == null)
        {
            return false;
        }


        if (
            !IsInsideGrid(oldPosition) ||
            !IsInsideGrid(newPosition)
        )
        {
            return false;
        }


        if (
            occupiedCells[
                oldPosition.x,
                oldPosition.y
            ] != unit
        )
        {
            return false;
        }


        if (IsCellOccupied(newPosition))
        {
            return false;
        }


        occupiedCells[
            oldPosition.x,
            oldPosition.y
        ] = null;


        occupiedCells[
            newPosition.x,
            newPosition.y
        ] = unit;


        unit.transform.position =
            GridToWorldPosition(newPosition);


        return true;
    }


    // ==================================================
    // DISTANCE
    // ==================================================

    public int GetDistance(
        Vector2Int a,
        Vector2Int b)
    {
        return
            Mathf.Abs(a.x - b.x) +
            Mathf.Abs(a.y - b.y);
    }


    // ==================================================
    // GETTERS
    // ==================================================

    public int GetWidth()
    {
        return width;
    }


    public int GetHeight()
    {
        return height;
    }


    public Grid GetGrid()
    {
        return grid;
    }


    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }


    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmos()
    {
        if (!showGridGizmos)
        {
            return;
        }


        if (grid == null)
        {
            grid = GetComponent<Grid>();


            if (grid == null)
            {
                return;
            }
        }


        Gizmos.color =
            Color.gray;


        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                GetGridCorner(x, 0),
                GetGridCorner(x, height)
            );
        }


        for (int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(
                GetGridCorner(0, y),
                GetGridCorner(width, y)
            );
        }


        Gizmos.color =
            Color.white;


        Vector3 bottomLeft =
            GetGridCorner(0, 0);


        Vector3 bottomRight =
            GetGridCorner(width, 0);


        Vector3 topLeft =
            GetGridCorner(0, height);


        Vector3 topRight =
            GetGridCorner(width, height);


        Gizmos.DrawLine(
            bottomLeft,
            bottomRight
        );


        Gizmos.DrawLine(
            bottomRight,
            topRight
        );


        Gizmos.DrawLine(
            topRight,
            topLeft
        );


        Gizmos.DrawLine(
            topLeft,
            bottomLeft
        );
    }


    private Vector3 GetGridCorner(
        int x,
        int y)
    {
        return grid.CellToWorld(
            new Vector3Int(
                x,
                y,
                0
            )
        );
    }
}