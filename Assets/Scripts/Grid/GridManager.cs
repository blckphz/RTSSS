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
    private int width = 11;

    [SerializeField, Min(1)]
    private int height = 11;


    // ==================================================
    // GRID CENTERING
    // ==================================================

    [Header("Grid Centering")]
    [SerializeField]
    private bool centerGridAtWorldOrigin = true;


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

    [SerializeField]
    private bool showCenterGizmo = true;


    // ==================================================
    // DATA
    // ==================================================

    private GameObject[,] occupiedCells;
    private GameObject[,] floorTiles;

    private bool initialized;


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

        if (width < 1 || height < 1)
        {
            Debug.LogError(
                "[GridManager] Width and Height must be at least 1!",
                this
            );

            return;
        }

        if (width % 2 == 0 || height % 2 == 0)
        {
            Debug.LogWarning(
                "[GridManager] Odd dimensions are recommended because " +
                "they provide a single logical center cell.\n" +
                $"Current Size: {width} x {height}",
                this
            );
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

        CenterGrid();

        CreateFloor();

        initialized = true;

        Debug.Log(
            "[GridManager] INITIALIZED\n" +
            "========================================\n" +
            $"Grid Size: {width} x {height}\n" +
            $"Logical X: {GetMinX()} -> {GetMaxX()}\n" +
            $"Logical Y: {GetMinY()} -> {GetMaxY()}\n" +
            $"(0,0) World Position: {GridToWorldPosition(Vector2Int.zero)}\n" +
            "========================================",
            this
        );
    }


    // ==================================================
    // CENTER GRID
    // ==================================================

    private void CenterGrid()
    {
        if (!centerGridAtWorldOrigin)
        {
            return;
        }

        /*
         * IMPORTANT
         *
         * We calculate the center in WORLD space and move the
         * Grid transform using WORLD position.
         *
         * This avoids subtracting a world-space vector from a
         * local-space transform when the Grid is under a rotated
         * Board.
         */

        Vector3 centerWorld =
            grid.GetCellCenterWorld(
                new Vector3Int(
                    width / 2,
                    height / 2,
                    0
                )
            );

        Transform gridTransform =
            grid.transform;

        gridTransform.position -= centerWorld;

        Debug.Log(
            "[GridManager] GRID CENTERED\n" +
            $"Grid Size: {width} x {height}\n" +
            $"Logical Center: (0,0)\n" +
            $"World Position of (0,0): {GridToWorldPosition(Vector2Int.zero)}",
            this
        );
    }


    // ==================================================
    // LOGICAL RANGE
    // ==================================================

    public int GetMinX()
    {
        return -(width / 2);
    }

    public int GetMaxX()
    {
        return GetMinX() + width - 1;
    }

    public int GetMinY()
    {
        return -(height / 2);
    }

    public int GetMaxY()
    {
        return GetMinY() + height - 1;
    }


    // ==================================================
    // LOGICAL <-> ARRAY
    // ==================================================

    private Vector2Int LogicalToArrayPosition(
        Vector2Int logicalPosition)
    {
        return new Vector2Int(
            logicalPosition.x - GetMinX(),
            logicalPosition.y - GetMinY()
        );
    }


    private Vector2Int ArrayToLogicalPosition(
        Vector2Int arrayPosition)
    {
        return new Vector2Int(
            arrayPosition.x + GetMinX(),
            arrayPosition.y + GetMinY()
        );
    }


    // ==================================================
    // LOGICAL <-> UNITY CELL
    // ==================================================

    private Vector3Int LogicalToUnityCell(
        Vector2Int logicalPosition)
    {
        return new Vector3Int(
            logicalPosition.x + width / 2,
            logicalPosition.y + height / 2,
            0
        );
    }


    private Vector2Int UnityCellToLogical(
        Vector3Int unityCell)
    {
        return new Vector2Int(
            unityCell.x - width / 2,
            unityCell.y - height / 2
        );
    }


    // ==================================================
    // BOARD RELATIONSHIP
    // ==================================================

    private bool IsGridControlledByBoard()
    {
        if (BoardViewController.Instance == null)
        {
            return false;
        }

        Transform board =
            BoardViewController.Instance.GetBoardTransform();

        if (board == null)
        {
            return false;
        }

        return grid.transform.IsChildOf(board);
    }


    // ==================================================
    // WORLD -> ORIGINAL GRID SPACE
    // ==================================================

    private Vector3 ConvertWorldToGridSpace(
        Vector3 worldPosition)
    {
        if (BoardViewController.Instance == null)
        {
            return worldPosition;
        }

        /*
         * If the Grid is physically underneath the Board,
         * WorldToCell already uses the rotated Grid transform.
         */

        if (IsGridControlledByBoard())
        {
            return worldPosition;
        }

        Vector3 center =
            BoardViewController.Instance
                .GetRotationCenter();

        int rotation =
            BoardViewController.Instance
                .GetCurrentRotation();

        Quaternion inverseRotation =
            Quaternion.AngleAxis(
                -rotation,
                Vector3.forward
            );

        Vector3 offset =
            worldPosition - center;

        offset =
            inverseRotation * offset;

        return center + offset;
    }


    // ==================================================
    // ORIGINAL GRID SPACE -> WORLD
    // ==================================================

    private Vector3 ConvertGridToWorldSpace(
        Vector3 gridWorldPosition)
    {
        if (BoardViewController.Instance == null)
        {
            return gridWorldPosition;
        }

        /*
         * Child of board = Unity already applies board rotation.
         */

        if (IsGridControlledByBoard())
        {
            return gridWorldPosition;
        }

        Vector3 center =
            BoardViewController.Instance
                .GetRotationCenter();

        int rotation =
            BoardViewController.Instance
                .GetCurrentRotation();

        Quaternion rotationQuaternion =
            Quaternion.AngleAxis(
                rotation,
                Vector3.forward
            );

        Vector3 offset =
            gridWorldPosition - center;

        offset =
            rotationQuaternion * offset;

        return center + offset;
    }


    // ==================================================
    // WORLD -> GRID
    // ==================================================

    public Vector2Int WorldToGridPosition(
        Vector3 worldPosition)
    {
        if (grid == null)
        {
            Debug.LogError(
                "[GridManager] Grid is missing.",
                this
            );

            return Vector2Int.zero;
        }

        Vector3 gridSpace =
            ConvertWorldToGridSpace(
                worldPosition
            );

        Vector3Int unityCell =
            grid.WorldToCell(
                gridSpace
            );

        return UnityCellToLogical(
            unityCell
        );
    }


    // ==================================================
    // GRID -> WORLD
    // ==================================================

    public Vector3 GridToWorldPosition(
        Vector2Int gridPosition)
    {
        if (grid == null)
        {
            return Vector3.zero;
        }

        Vector3Int unityCell =
            LogicalToUnityCell(
                gridPosition
            );

        Vector3 unrotatedWorld =
            grid.GetCellCenterWorld(
                unityCell
            );

        return ConvertGridToWorldSpace(
            unrotatedWorld
        );
    }


    // ==================================================
    // GRID CHECKS
    // ==================================================

    public bool IsInsideGrid(
        Vector2Int position)
    {
        return
            position.x >= GetMinX() &&
            position.x <= GetMaxX() &&
            position.y >= GetMinY() &&
            position.y <= GetMaxY();
    }


    // ==================================================
    // FLOOR
    // ==================================================

    private void CreateFloor()
    {
        if (floorTilePrefab == null)
        {
            Debug.LogWarning(
                "[GridManager] Floor Tile Prefab is not assigned.",
                this
            );

            return;
        }

        for (int x = GetMinX(); x <= GetMaxX(); x++)
        {
            for (int y = GetMinY(); y <= GetMaxY(); y++)
            {
                Vector2Int logicalPosition =
                    new Vector2Int(x, y);

                Vector3 worldPosition =
                    GridToWorldPosition(
                        logicalPosition
                    );

                GameObject tile =
                    Instantiate(
                        floorTilePrefab,
                        worldPosition,
                        Quaternion.identity,
                        floorParent
                    );

                tile.name =
                    $"Floor_{x}_{y}";

                Vector2Int arrayPosition =
                    LogicalToArrayPosition(
                        logicalPosition
                    );

                floorTiles[
                    arrayPosition.x,
                    arrayPosition.y
                ] = tile;
            }
        }

        Debug.Log(
            $"[GridManager] Created {width * height} floor tiles.",
            this
        );
    }


    // ==================================================
    // FLOOR ACCESS
    // ==================================================

    public GameObject GetFloorTile(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return null;
        }

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        return floorTiles[
            array.x,
            array.y
        ];
    }


    // ==================================================
    // OCCUPANT VALIDATION
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
            RemoveUnit(position);
            return false;
        }

        HealthManager health =
            occupant.GetComponent<HealthManager>();

        if (health != null && health.IsDead())
        {
            RemoveUnit(position);
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

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        return IsOccupantValid(
            occupiedCells[array.x, array.y],
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

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        GameObject unit =
            occupiedCells[
                array.x,
                array.y
            ];

        return IsOccupantValid(
            unit,
            position
        )
            ? unit
            : null;
    }


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
    // MOVEMENT VALIDATION
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

        return occupant == null || occupant == unit;
    }


    // ==================================================
    // PLACE UNIT
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

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        occupiedCells[
            array.x,
            array.y
        ] = unit;

        unit.transform.position =
            GridToWorldPosition(
                position
            );

        Debug.Log(
            $"[GridManager] UNIT PLACED: {unit.name} at {position}",
            unit
        );

        return true;
    }


    // ==================================================
    // REMOVE BY POSITION
    // ==================================================

    public void RemoveUnit(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            return;
        }

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        occupiedCells[
            array.x,
            array.y
        ] = null;
    }


    // ==================================================
    // REMOVE BY OBJECT
    // ==================================================

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
                }
            }
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    public void CleanupDeadUnits()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject unit =
                    occupiedCells[x, y];

                if (unit == null)
                {
                    continue;
                }

                Vector2Int logical =
                    ArrayToLogicalPosition(
                        new Vector2Int(x, y)
                    );

                IsOccupantValid(
                    unit,
                    logical
                );
            }
        }
    }


    // ==================================================
    // START MOVE
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

        Vector2Int oldArray =
            LogicalToArrayPosition(
                oldPosition
            );

        Vector2Int newArray =
            LogicalToArrayPosition(
                newPosition
            );

        if (
            occupiedCells[
                oldArray.x,
                oldArray.y
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
            oldArray.x,
            oldArray.y
        ] = null;

        occupiedCells[
            newArray.x,
            newArray.y
        ] = unit;

        return true;
    }


    // ==================================================
    // FINISH MOVE
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

        Vector2Int array =
            LogicalToArrayPosition(
                position
            );

        if (
            occupiedCells[
                array.x,
                array.y
            ] != unit
        )
        {
            return;
        }

        unit.transform.position =
            GridToWorldPosition(
                position
            );
    }


    // ==================================================
    // INSTANT MOVE
    // ==================================================

    public bool MoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (!StartMoveUnit(
            unit,
            oldPosition,
            newPosition))
        {
            return false;
        }

        FinishMoveUnit(
            unit,
            newPosition
        );

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
    // DEBUG CENTER
    // ==================================================

    [ContextMenu("Debug Grid Center")]
    public void DebugGridCenter()
    {
        if (grid == null)
        {
            return;
        }

        Vector3 center =
            GridToWorldPosition(
                Vector2Int.zero
            );

        int rotation = 0;

        if (BoardViewController.Instance != null)
        {
            rotation =
                BoardViewController.Instance
                    .GetCurrentRotation();
        }

        Debug.Log(
            "========================================\n" +
            "[GridManager] CENTER DEBUG\n" +
            "========================================\n" +
            $"Grid Size: {width} x {height}\n" +
            $"X Range: {GetMinX()} -> {GetMaxX()}\n" +
            $"Y Range: {GetMinY()} -> {GetMaxY()}\n" +
            $"Logical Center: (0,0)\n" +
            $"World Center: {center}\n" +
            $"Board Rotation: {rotation}\n" +
            $"Grid Controlled By Board: {IsGridControlledByBoard()}\n" +
            $"Distance From Origin: {Vector3.Distance(center, Vector3.zero)}\n" +
            "========================================",
            this
        );
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
        }

        if (grid == null)
        {
            return;
        }

        Gizmos.color = Color.gray;

        for (
            int x = GetMinX();
            x <= GetMaxX() + 1;
            x++
        )
        {
            Gizmos.DrawLine(
                GetLogicalGridCorner(x, GetMinY()),
                GetLogicalGridCorner(x, GetMaxY() + 1)
            );
        }

        for (
            int y = GetMinY();
            y <= GetMaxY() + 1;
            y++
        )
        {
            Gizmos.DrawLine(
                GetLogicalGridCorner(GetMinX(), y),
                GetLogicalGridCorner(GetMaxX() + 1, y)
            );
        }

        Gizmos.color = Color.white;

        Vector3 bottomLeft =
            GetLogicalGridCorner(
                GetMinX(),
                GetMinY()
            );

        Vector3 bottomRight =
            GetLogicalGridCorner(
                GetMaxX() + 1,
                GetMinY()
            );

        Vector3 topLeft =
            GetLogicalGridCorner(
                GetMinX(),
                GetMaxY() + 1
            );

        Vector3 topRight =
            GetLogicalGridCorner(
                GetMaxX() + 1,
                GetMaxY() + 1
            );

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        if (showCenterGizmo)
        {
            Gizmos.color = Color.yellow;

            Vector3 center =
                GridToWorldPosition(
                    Vector2Int.zero
                );

            Gizmos.DrawSphere(
                center,
                0.15f
            );

            Gizmos.DrawLine(
                center + Vector3.left * 0.5f,
                center + Vector3.right * 0.5f
            );

            Gizmos.DrawLine(
                center + Vector3.down * 0.5f,
                center + Vector3.up * 0.5f
            );
        }
    }


    // ==================================================
    // GRID CORNER
    // ==================================================

    private Vector3 GetLogicalGridCorner(
        int x,
        int y)
    {
        Vector3Int cell =
            new Vector3Int(
                x + width / 2,
                y + height / 2,
                0
            );

        Vector3 world =
            grid.CellToWorld(cell);

        return ConvertGridToWorldSpace(world);
    }
}