
using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// GRID SHAPE TYPE
// ============================================================

public enum GridShapeType
{
    Box,
    Manhattan,
    Pyramid,
    Donut
}

// ============================================================
// GRID SHAPE EVALUATOR
// ============================================================

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
        int minX = -(width / 2);
        int maxX = minX + width - 1;
        int minY = -(height / 2);
        int maxY = minY + height - 1;

        if (position.x < minX || position.x > maxX ||
            position.y < minY || position.y > maxY)
        {
            return false;
        }

        switch (shapeType)
        {
            case GridShapeType.Box:
                return true;

            case GridShapeType.Manhattan:
                int radius = Mathf.Min(width, height) / 2;
                return Mathf.Abs(position.x) + Mathf.Abs(position.y) <= radius;

            case GridShapeType.Pyramid:
                int rowOffset = position.y - minY;
                int currentHalfWidth = (width / 2) - rowOffset;

                if (currentHalfWidth < 0)
                    return false;

                return position.x >= -currentHalfWidth &&
                       position.x <= currentHalfWidth;

            case GridShapeType.Donut:
                int distSq = position.x * position.x + position.y * position.y;
                int minSq = minRadius * minRadius;
                int maxSq = maxRadius * maxRadius;

                return distSq >= minSq && distSq <= maxSq;

            default:
                return false;
        }
    }
}

// ============================================================
// GRID MANAGER
// ============================================================

public class GridManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Grid References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private Transform floorParent;

    // ============================================================
    // GRID CONFIGURATION
    // ============================================================

    [Header("Grid Configuration")]
    [SerializeField, Min(1)] private int width = 11;
    [SerializeField, Min(1)] private int height = 11;
    [SerializeField] private GridShapeType gridShape = GridShapeType.Box;

    private int minRadius = 2;
    private int maxRadius = 5;

    // ============================================================
    // GRID CENTERING
    // ============================================================

    [Header("Grid Centering")]
    [SerializeField] private bool centerGridAtWorldOrigin = true;

    // ============================================================
    // HIGHLIGHT MANAGER
    // ============================================================

    [Header("Highlight Manager")]
    [SerializeField] private GridHighlightManager highlightManager;

    // ============================================================
    // GIZMOS
    // ============================================================

    [Header("Gizmos")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private bool showCenterGizmo = true;

    // ============================================================
    // DATA
    // ============================================================

    private GameObject[,] occupiedCells;
    private GameObject[,] floorTiles;
    private Transform gridTransform;
    private bool initialized;

    // ============================================================
    // MOVEMENT TRACKING
    // ============================================================

    private readonly HashSet<GameObject> movingUnits =
        new HashSet<GameObject>();

    // ============================================================
    // FOOTPRINT CACHE
    // ============================================================

    private readonly Dictionary<GameObject, int> footprintSizeCache =
        new Dictionary<GameObject, int>();

    private int maxCachedFootprintSize = 1;

    // ============================================================
    // WALKABLE OBSTACLES
    // ============================================================

    private readonly HashSet<Vector2Int> walkableObstacleCells =
        new HashSet<Vector2Int>();

    // ============================================================
    // ORIGINAL GRID TRANSFORM POSITION
    // ============================================================

    private Vector3 originalGridLocalPosition;
    private bool originalGridPositionCached;

    // ============================================================
    // GRID CHANGED EVENT
    // ============================================================

    public event Action OnGridChanged;

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (grid == null)
            grid = GetComponent<Grid>();

        if (grid == null)
            return;

        gridTransform = grid.transform;

        if (!originalGridPositionCached)
        {
            originalGridLocalPosition = gridTransform.localPosition;
            originalGridPositionCached = true;
        }

        if (width < 1 || height < 1)
            return;

        occupiedCells = new GameObject[width, height];
        floorTiles = new GameObject[width, height];

        if (floorParent == null)
            floorParent = transform;

        if (highlightManager == null)
            highlightManager = GetComponent<GridHighlightManager>();

        CenterGrid();
        CreateFloor();

        initialized = true;
    }

    // ============================================================
    // RUNTIME SHAPE & DIMENSION MUTATORS
    // ============================================================

    public void SetGridDimensions(
        int newWidth,
        int newHeight,
        bool destroyInvalidUnits = true)
    {
        SetGridShape(
            gridShape,
            newWidth,
            newHeight,
            minRadius,
            maxRadius,
            destroyInvalidUnits
        );
    }

    public void SetGridSizeAndShape(
        GridShapeType newShape,
        int newWidth,
        int newHeight,
        bool destroyInvalidUnits = true)
    {
        SetGridShape(
            newShape,
            newWidth,
            newHeight,
            minRadius,
            maxRadius,
            destroyInvalidUnits
        );
    }

    public void SetGridShape(
        GridShapeType newShape,
        int newWidth = -1,
        int newHeight = -1,
        int newMinRadius = -1,
        int newMaxRadius = -1,
        bool destroyInvalidUnits = true)
    {
        if (newWidth > 0)
            width = newWidth;

        if (newHeight > 0)
            height = newHeight;

        if (newMinRadius >= 0)
            minRadius = newMinRadius;

        if (newMaxRadius > 0)
            maxRadius = newMaxRadius;

        gridShape = newShape;

        if (grid == null)
            grid = GetComponent<Grid>();

        if (grid == null)
            return;

        if (gridTransform == null)
            gridTransform = grid.transform;

        if (!originalGridPositionCached)
        {
            originalGridLocalPosition = gridTransform.localPosition;
            originalGridPositionCached = true;
        }

        ClearGridHighlights();

        GameObject[,] previousOccupants = occupiedCells;

        ClearFloorTiles();

        occupiedCells = new GameObject[width, height];
        floorTiles = new GameObject[width, height];

        CenterGrid();
        CreateFloor();

        if (previousOccupants != null)
        {
            RestorePreviousOccupants(
                previousOccupants,
                destroyInvalidUnits
            );
        }

        RemoveInvalidWalkableObstacleCells();

        initialized = true;
        NotifyGridChanged();
    }

    // ============================================================
    // WALKABLE OBSTACLES
    // ============================================================

    public void RegisterWalkableObstacle(Vector2Int position)
    {
        if (!IsInsideGrid(position))
            return;

        walkableObstacleCells.Add(position);
    }

    public void UnregisterWalkableObstacle(Vector2Int position)
    {
        walkableObstacleCells.Remove(position);
    }

    public bool IsWalkableObstacle(Vector2Int position)
    {
        return walkableObstacleCells.Contains(position);
    }

    private void RemoveInvalidWalkableObstacleCells()
    {
        if (walkableObstacleCells.Count == 0)
            return;

        List<Vector2Int> invalidCells = new List<Vector2Int>();

        foreach (Vector2Int position in walkableObstacleCells)
        {
            if (!IsInsideGrid(position))
                invalidCells.Add(position);
        }

        for (int i = 0; i < invalidCells.Count; i++)
            walkableObstacleCells.Remove(invalidCells[i]);
    }

    // ============================================================
    // RESTORE PREVIOUS OCCUPANTS
    // ============================================================

    private void RestorePreviousOccupants(
        GameObject[,] previousOccupants,
        bool destroyInvalidUnits)
    {
        if (previousOccupants == null)
            return;

        int oldWidth = previousOccupants.GetLength(0);
        int oldHeight = previousOccupants.GetLength(1);
        int oldMinX = -(oldWidth / 2);
        int oldMinY = -(oldHeight / 2);

        for (int x = 0; x < oldWidth; x++)
        {
            for (int y = 0; y < oldHeight; y++)
            {
                GameObject unit = previousOccupants[x, y];

                if (unit == null)
                    continue;

                GetUnitFootprintSize(unit);

                Vector2Int logicalPos = new Vector2Int(
                    oldMinX + x,
                    oldMinY + y
                );

                if (IsInsideGrid(logicalPos))
                {
                    Vector2Int newArrayPos =
                        LogicalToArrayPosition(logicalPos);

                    if (IsArrayPositionValid(newArrayPos, occupiedCells))
                    {
                        GameObject existing =
                            occupiedCells[newArrayPos.x, newArrayPos.y];

                        if (existing == null || existing == unit)
                        {
                            occupiedCells[newArrayPos.x, newArrayPos.y] = unit;
                            SynchronizeUnitPosition(unit, logicalPos);
                        }
                        else
                        {
                            if (destroyInvalidUnits)
                                Destroy(unit);
                            else
                                unit.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (destroyInvalidUnits)
                        Destroy(unit);
                    else
                        unit.SetActive(false);
                }
            }
        }
    }

    // ============================================================
    // ARRAY BOUNDS HELPER
    // ============================================================

    private bool IsArrayPositionValid(
        Vector2Int position,
        GameObject[,] array)
    {
        return array != null &&
               position.x >= 0 &&
               position.x < array.GetLength(0) &&
               position.y >= 0 &&
               position.y < array.GetLength(1);
    }

    // ============================================================
    // CLEAR FLOOR TILES
    // ============================================================

    private void ClearFloorTiles()
    {
        if (floorTiles == null)
            return;

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

    // ============================================================
    // CLEAR GRID HIGHLIGHTS
    // ============================================================

    private void ClearGridHighlights()
    {
        if (highlightManager == null && gridTransform != null)
        {
            highlightManager =
                gridTransform.GetComponent<GridHighlightManager>();
        }

        if (highlightManager != null)
            highlightManager.ClearAllHighlights();

        GridHighlightBrain highlightBrain =
            FindFirstObjectByType<GridHighlightBrain>();

        if (highlightBrain != null)
            highlightBrain.ClearAllHighlights();
    }

    // ============================================================
    // NOTIFY GRID CHANGED
    // ============================================================

    private void NotifyGridChanged()
    {
        OnGridChanged?.Invoke();

        GridHighlightBrain highlightBrain =
            FindFirstObjectByType<GridHighlightBrain>();

        if (highlightBrain != null)
            highlightBrain.ResetForNewEncounter();
    }

    // ============================================================
    // CENTER GRID
    // ============================================================

    private void CenterGrid()
    {
        if (!centerGridAtWorldOrigin || grid == null)
            return;

        if (!originalGridPositionCached)
        {
            originalGridLocalPosition = gridTransform.localPosition;
            originalGridPositionCached = true;
        }

        gridTransform.localPosition = originalGridLocalPosition;

        Vector3 centerWorld = grid.GetCellCenterWorld(
            new Vector3Int(width / 2, height / 2, 0)
        );

        Vector3 localOffset =
            gridTransform.InverseTransformVector(centerWorld);

        gridTransform.localPosition =
            originalGridLocalPosition - localOffset;
    }

    // ============================================================
    // LOGICAL RANGE & MAPPINGS
    // ============================================================

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

    private Vector2Int LogicalToArrayPosition(Vector2Int logicalPos)
    {
        return new Vector2Int(
            logicalPos.x - GetMinX(),
            logicalPos.y - GetMinY()
        );
    }

    private Vector2Int ArrayToLogicalPosition(Vector2Int arrayPos)
    {
        return new Vector2Int(
            arrayPos.x + GetMinX(),
            arrayPos.y + GetMinY()
        );
    }

    private Vector3Int LogicalToUnityCell(Vector2Int logicalPos)
    {
        return new Vector3Int(
            logicalPos.x + width / 2,
            logicalPos.y + height / 2,
            0
        );
    }

    private Vector2Int UnityCellToLogical(Vector3Int unityCell)
    {
        return new Vector2Int(
            unityCell.x - width / 2,
            unityCell.y - height / 2
        );
    }

    // ============================================================
    // WORLD / GRID SPACE CONVERSIONS
    // ============================================================

    private bool IsGridControlledByBoard()
    {
        if (BoardViewController.Instance == null)
            return false;

        Transform board =
            BoardViewController.Instance.GetBoardTransform();

        return board != null &&
               gridTransform != null &&
               gridTransform.IsChildOf(board);
    }

    private Vector3 ConvertWorldToGridSpace(Vector3 worldPosition)
    {
        if (BoardViewController.Instance == null ||
            IsGridControlledByBoard())
        {
            return worldPosition;
        }

        Vector3 center =
            BoardViewController.Instance.GetRotationCenter();

        int rotation =
            BoardViewController.Instance.GetCurrentRotation();

        Quaternion inverseRotation = Quaternion.AngleAxis(
            -rotation,
            Vector3.forward
        );

        return center +
               inverseRotation * (worldPosition - center);
    }

    private Vector3 ConvertGridToWorldSpace(Vector3 gridWorldPosition)
    {
        if (BoardViewController.Instance == null ||
            IsGridControlledByBoard())
        {
            return gridWorldPosition;
        }

        Vector3 center =
            BoardViewController.Instance.GetRotationCenter();

        int rotation =
            BoardViewController.Instance.GetCurrentRotation();

        Quaternion rotationQuaternion = Quaternion.AngleAxis(
            rotation,
            Vector3.forward
        );

        return center +
               rotationQuaternion * (gridWorldPosition - center);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (grid == null)
            return Vector2Int.zero;

        Vector3 gridSpace = ConvertWorldToGridSpace(worldPosition);
        Vector3Int unityCell = grid.WorldToCell(gridSpace);

        return UnityCellToLogical(unityCell);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        if (grid == null)
            return Vector3.zero;

        Vector3Int unityCell = LogicalToUnityCell(gridPosition);

        Vector3 unrotatedWorld =
            grid.GetCellCenterWorld(unityCell);

        return ConvertGridToWorldSpace(unrotatedWorld);
    }

    // ============================================================
    // CHECKS & FLOOR CREATION
    // ============================================================

    public bool IsInsideGrid(Vector2Int position)
    {
        int minX = GetMinX();
        int maxX = GetMaxX();
        int minY = GetMinY();
        int maxY = GetMaxY();

        if (position.x < minX || position.x > maxX ||
            position.y < minY || position.y > maxY)
        {
            return false;
        }

        return GridShapeEvaluator.IsCellInShape(
            position,
            gridShape,
            width,
            height,
            minRadius,
            maxRadius
        );
    }

    private void CreateFloor()
    {
        if (floorTilePrefab == null || floorTiles == null)
            return;

        int minX = GetMinX();
        int maxX = GetMaxX();
        int minY = GetMinY();
        int maxY = GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int logicalPos = new Vector2Int(x, y);

                if (!IsInsideGrid(logicalPos))
                    continue;

                Vector3 worldPos = GridToWorldPosition(logicalPos);

                GameObject tile = Instantiate(
                    floorTilePrefab,
                    worldPos,
                    Quaternion.identity,
                    floorParent
                );

                tile.name = $"Floor_{x}_{y}";

                Vector2Int arrayPos =
                    LogicalToArrayPosition(logicalPos);

                if (IsArrayPositionValid(arrayPos, floorTiles))
                {
                    floorTiles[arrayPos.x, arrayPos.y] = tile;
                }
                else
                {
                    Destroy(tile);
                }
            }
        }
    }

    // ============================================================
    // UNIT FOOTPRINT HELPERS
    // ============================================================

    public int GetUnitFootprintSize(GameObject unit)
    {
        if (unit == null)
            return 1;

        if (footprintSizeCache.TryGetValue(unit, out int cachedSize))
            return cachedSize;

        int size = 1;

        AttackUnit attackUnit = unit.GetComponent<AttackUnit>();

        if (attackUnit != null)
        {
            CharacterSO data = attackUnit.GetCharacterData();

            if (data != null)
                size = Mathf.Max(1, data.GetFootprintSize());
        }

        footprintSizeCache[unit] = size;

        if (size > maxCachedFootprintSize)
            maxCachedFootprintSize = size;

        return size;
    }

    private Vector2Int GetFootprintMinOffset(int size)
    {
        return new Vector2Int(-(size / 2), -(size / 2));
    }

    public bool IsCellInUnitFootprint(
        GameObject unit,
        Vector2Int anchor,
        Vector2Int cell)
    {
        int size = GetUnitFootprintSize(unit);
        Vector2Int minOffset = GetFootprintMinOffset(size);

        return cell.x >= anchor.x + minOffset.x &&
               cell.x < anchor.x + minOffset.x + size &&
               cell.y >= anchor.y + minOffset.y &&
               cell.y < anchor.y + minOffset.y + size;
    }

    public bool IsFootprintInsideGrid(
        GameObject unit,
        Vector2Int anchor)
    {
        int size = GetUnitFootprintSize(unit);
        Vector2Int minOffset = GetFootprintMinOffset(size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int cell =
                    anchor + minOffset + new Vector2Int(x, y);

                if (!IsInsideGrid(cell))
                    return false;
            }
        }

        return true;
    }

    // ============================================================
    // OPTIMIZED FOOTPRINT OCCUPANT LOOKUP
    // ============================================================

    private GameObject FindFootprintOccupant(
        Vector2Int position,
        GameObject ignoreUnit = null)
    {
        if (occupiedCells == null)
            return null;

        int searchRadius = Mathf.Max(1, maxCachedFootprintSize);

        int minX = Mathf.Max(GetMinX(), position.x - searchRadius);
        int maxX = Mathf.Min(GetMaxX(), position.x + searchRadius);
        int minY = Mathf.Max(GetMinY(), position.y - searchRadius);
        int maxY = Mathf.Min(GetMaxY(), position.y + searchRadius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int anchor = new Vector2Int(x, y);
                Vector2Int array = LogicalToArrayPosition(anchor);

                if (!IsArrayPositionValid(array, occupiedCells))
                    continue;

                GameObject candidate = occupiedCells[array.x, array.y];

                if (candidate == null ||
                    candidate == ignoreUnit ||
                    anchor == position)
                {
                    continue;
                }

                if (!candidate.activeInHierarchy)
                    continue;

                HealthManager health =
                    candidate.GetComponent<HealthManager>();

                if (health != null && health.IsDead())
                    continue;

                if (!IsCellInUnitFootprint(candidate, anchor, position))
                    continue;

                if (!movingUnits.Contains(candidate) &&
                    WorldToGridPosition(candidate.transform.position) != anchor)
                {
                    continue;
                }

                return candidate;
            }
        }

        return null;
    }

    // ============================================================
    // OCCUPANT VALIDATION
    // ============================================================

    private bool IsOccupantValid(
        GameObject occupant,
        Vector2Int position)
    {
        if (occupant == null)
            return false;

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

    // ============================================================
    // REMOVE STALE REFERENCES TO A UNIT
    // ============================================================

    private void RemoveUnitReferences(GameObject unit)
    {
        if (unit == null || occupiedCells == null)
            return;

        int lenX = occupiedCells.GetLength(0);
        int lenY = occupiedCells.GetLength(1);

        for (int x = 0; x < lenX; x++)
        {
            for (int y = 0; y < lenY; y++)
            {
                if (occupiedCells[x, y] == unit)
                    occupiedCells[x, y] = null;
            }
        }
    }

    // ============================================================
    // FIND ACTUAL UNIT AT POSITION
    // Retained for explicit recovery operations only.
    // ============================================================

    private GameObject FindUnitAtActualPosition(Vector2Int position)
    {
        HealthManager[] healthManagers =
            FindObjectsByType<HealthManager>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < healthManagers.Length; i++)
        {
            HealthManager health = healthManagers[i];

            if (health == null)
                continue;

            GameObject candidate = health.gameObject;

            if (candidate == null || !candidate.activeInHierarchy)
                continue;

            if (health.IsDead())
                continue;

            if (movingUnits.Contains(candidate))
                continue;

            if (WorldToGridPosition(candidate.transform.position) != position)
                continue;

            return candidate;
        }

        return null;
    }

    // ============================================================
    // RECOVER AND REGISTER UNIT
    // ============================================================

    private GameObject RecoverUnitAtPosition(Vector2Int position)
    {
        GameObject recovered = FindUnitAtActualPosition(position);

        if (recovered == null)
            return null;

        GetUnitFootprintSize(recovered);
        RemoveUnitReferences(recovered);

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return null;

        GameObject existing = occupiedCells[array.x, array.y];

        if (existing != null &&
            existing != recovered &&
            IsOccupantValid(existing, position))
        {
            return existing;
        }

        occupiedCells[array.x, array.y] = recovered;

        return recovered;
    }

    // ============================================================
    // IS CELL OCCUPIED — OPTIMIZED
    // ============================================================

    public bool IsCellOccupied(Vector2Int position)
    {
        if (occupiedCells == null || !IsInsideGrid(position))
            return false;

        GameObject footprintOccupant = FindFootprintOccupant(position);

        if (footprintOccupant != null)
            return true;

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return false;

        GameObject occupant = occupiedCells[array.x, array.y];

        return IsOccupantValid(occupant, position);
    }

    // ============================================================
    // GET UNIT AT — OPTIMIZED
    // ============================================================

    public GameObject GetUnitAt(Vector2Int position)
    {
        if (occupiedCells == null || !IsInsideGrid(position))
            return null;

        GameObject footprintOccupant = FindFootprintOccupant(position);

        if (footprintOccupant != null)
            return footprintOccupant;

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return null;

        GameObject unit = occupiedCells[array.x, array.y];

        if (!IsOccupantValid(unit, position))
            return null;

        if (movingUnits.Contains(unit))
            return unit;

        Vector2Int actualPosition =
            WorldToGridPosition(unit.transform.position);

        if (actualPosition == position)
            return unit;

        occupiedCells[array.x, array.y] = null;

        return null;
    }

    // ============================================================
    // GET UNIT GRID POSITION
    // ============================================================

    public Vector2Int GetUnitGridPosition(GameObject unit)
    {
        if (unit == null)
            return Vector2Int.zero;

        if (movingUnits.Contains(unit) && occupiedCells != null)
        {
            int lenX = occupiedCells.GetLength(0);
            int lenY = occupiedCells.GetLength(1);

            for (int x = 0; x < lenX; x++)
            {
                for (int y = 0; y < lenY; y++)
                {
                    if (occupiedCells[x, y] == unit)
                    {
                        return ArrayToLogicalPosition(
                            new Vector2Int(x, y)
                        );
                    }
                }
            }
        }

        return WorldToGridPosition(unit.transform.position);
    }

    // ============================================================
    // CAN MOVE TO CELL
    // ============================================================

    public bool CanMoveToCell(GameObject unit, Vector2Int position)
    {
        if (unit == null || !IsFootprintInsideGrid(unit, position))
            return false;

        int size = GetUnitFootprintSize(unit);
        Vector2Int minOffset = GetFootprintMinOffset(size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int cell =
                    position + minOffset + new Vector2Int(x, y);

                GameObject occupant = GetUnitAt(cell);

                if (occupant != null && occupant != unit)
                    return false;
            }
        }

        return true;
    }

    // ============================================================
    // PLACE UNIT
    // ============================================================

    public bool PlaceUnit(
        GameObject unit,
        Vector2Int position,
        bool playSound = true)
    {
        if (unit == null ||
            occupiedCells == null ||
            !IsFootprintInsideGrid(unit, position))
        {
            return false;
        }

        int footprintSize = GetUnitFootprintSize(unit);
        Vector2Int footprintOffset =
            GetFootprintMinOffset(footprintSize);

        for (int fx = 0; fx < footprintSize; fx++)
        {
            for (int fy = 0; fy < footprintSize; fy++)
            {
                Vector2Int cell =
                    position + footprintOffset + new Vector2Int(fx, fy);

                GameObject occupant = GetUnitAt(cell);

                if (occupant != null && occupant != unit)
                    return false;
            }
        }

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return false;

        RemoveUnitReferences(unit);

        GameObject existing = occupiedCells[array.x, array.y];

        if (existing != null)
        {
            if (existing == unit)
            {
                SynchronizeUnitPosition(unit, position);
                return true;
            }

            if (IsOccupantValid(existing, position))
                return false;
        }

        occupiedCells[array.x, array.y] = unit;

        SynchronizeUnitPosition(unit, position);

        return true;
    }

    // ============================================================
    // SYNCHRONIZE UNIT POSITION
    // ============================================================

    private void SynchronizeUnitPosition(
        GameObject unit,
        Vector2Int position)
    {
        if (unit == null)
            return;

        UnitTilePin pin = unit.GetComponent<UnitTilePin>();

        if (pin != null)
        {
            pin.SetTile(position);
        }
        else
        {
            unit.transform.position = GridToWorldPosition(position);
        }
    }

    // ============================================================
    // REGISTER UNIT IF MISSING
    // ============================================================

    private bool EnsureUnitRegistered(
        GameObject unit,
        Vector2Int position)
    {
        if (unit == null ||
            occupiedCells == null ||
            !IsInsideGrid(position))
        {
            return false;
        }

        GetUnitFootprintSize(unit);

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return false;

        GameObject occupant = occupiedCells[array.x, array.y];

        if (occupant == unit)
            return true;

        if (occupant != null && IsOccupantValid(occupant, position))
            return false;

        RemoveUnitReferences(unit);

        occupiedCells[array.x, array.y] = unit;

        return true;
    }

    // ============================================================
    // REMOVE UNIT
    // ============================================================

    public void RemoveUnit(Vector2Int position)
    {
        if (occupiedCells == null || !IsInsideGrid(position))
            return;

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
            return;

        occupiedCells[array.x, array.y] = null;
    }

    public void RemoveUnit(GameObject unit)
    {
        if (unit == null)
            return;

        movingUnits.Remove(unit);
        footprintSizeCache.Remove(unit);
        RemoveUnitReferences(unit);
    }

    // ============================================================
    // CLEANUP DEAD UNITS
    // ============================================================

    public void CleanupDeadUnits()
    {
        if (occupiedCells == null)
            return;

        int lenX = occupiedCells.GetLength(0);
        int lenY = occupiedCells.GetLength(1);

        for (int x = 0; x < lenX; x++)
        {
            for (int y = 0; y < lenY; y++)
            {
                GameObject unit = occupiedCells[x, y];

                if (unit == null)
                    continue;

                Vector2Int logical = ArrayToLogicalPosition(
                    new Vector2Int(x, y)
                );

                IsOccupantValid(unit, logical);
            }
        }
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    public bool StartMoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (unit == null ||
            occupiedCells == null ||
            !IsInsideGrid(oldPosition) ||
            !IsInsideGrid(newPosition) ||
            oldPosition == newPosition)
        {
            return false;
        }

        if (!CanMoveToCell(unit, newPosition))
            return false;

        GetUnitFootprintSize(unit);

        Vector2Int oldArray = LogicalToArrayPosition(oldPosition);
        Vector2Int newArray = LogicalToArrayPosition(newPosition);

        if (!IsArrayPositionValid(oldArray, occupiedCells) ||
            !IsArrayPositionValid(newArray, occupiedCells))
        {
            return false;
        }

        GameObject oldOccupant =
            occupiedCells[oldArray.x, oldArray.y];

        if (oldOccupant != unit)
        {
            bool registered = EnsureUnitRegistered(unit, oldPosition);

            if (!registered)
                return false;
        }

        GameObject destinationOccupant =
            occupiedCells[newArray.x, newArray.y];

        if (destinationOccupant != null)
        {
            if (destinationOccupant != unit &&
                IsOccupantValid(destinationOccupant, newPosition))
            {
                return false;
            }

            if (destinationOccupant != null &&
                destinationOccupant != unit)
            {
                occupiedCells[newArray.x, newArray.y] = null;
            }
        }

        occupiedCells[oldArray.x, oldArray.y] = null;
        occupiedCells[newArray.x, newArray.y] = unit;

        movingUnits.Add(unit);

        return true;
    }

    // ============================================================
    // FINISH MOVE
    // ============================================================

    public void FinishMoveUnit(GameObject unit, Vector2Int position)
    {
        if (unit == null ||
            occupiedCells == null ||
            !IsInsideGrid(position))
        {
            if (unit != null)
                movingUnits.Remove(unit);

            return;
        }

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, occupiedCells))
        {
            movingUnits.Remove(unit);
            return;
        }

        if (occupiedCells[array.x, array.y] != unit)
        {
            movingUnits.Remove(unit);
            return;
        }

        UnitTilePin pin = unit.GetComponent<UnitTilePin>();

        if (pin != null)
        {
            pin.UpdateTileAfterMovement(position);
        }
        else
        {
            unit.transform.position = GridToWorldPosition(position);
        }

        movingUnits.Remove(unit);
    }

    // ============================================================
    // IS UNIT MOVING
    // ============================================================

    public bool IsUnitMoving(GameObject unit)
    {
        return unit != null && movingUnits.Contains(unit);
    }

    // ============================================================
    // MOVE UNIT
    // ============================================================

    public bool MoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (!StartMoveUnit(unit, oldPosition, newPosition))
            return false;

        FinishMoveUnit(unit, newPosition);

        return true;
    }

    // ============================================================
    // RANDOM FREE CELL
    // ============================================================

    public bool TryGetRandomFreeCell(out Vector2Int position)
    {
        position = Vector2Int.zero;

        if (occupiedCells == null)
            return false;

        int minX = GetMinX();
        int maxX = GetMaxX();
        int minY = GetMinY();
        int maxY = GetMaxY();

        int freeCellCount = 0;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!IsInsideGrid(cell) ||
                    IsWalkableObstacle(cell) ||
                    IsCellOccupied(cell))
                {
                    continue;
                }

                freeCellCount++;
            }
        }

        if (freeCellCount == 0)
            return false;

        int randomIndex = UnityEngine.Random.Range(0, freeCellCount);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!IsInsideGrid(cell) ||
                    IsWalkableObstacle(cell) ||
                    IsCellOccupied(cell))
                {
                    continue;
                }

                if (randomIndex == 0)
                {
                    position = cell;
                    return true;
                }

                randomIndex--;
            }
        }

        return false;
    }

    // ============================================================
    // GETTERS & HELPERS
    // ============================================================

    public GameObject GetFloorTile(Vector2Int position)
    {
        if (floorTiles == null || !IsInsideGrid(position))
            return null;

        Vector2Int array = LogicalToArrayPosition(position);

        if (!IsArrayPositionValid(array, floorTiles))
            return null;

        return floorTiles[array.x, array.y];
    }

    public int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }

    public GridShapeType GetShape()
    {
        return gridShape;
    }

    public Grid GetGrid()
    {
        return grid;
    }

    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }

    public int GetMinRadius()
    {
        return minRadius;
    }

    public int GetMaxRadius()
    {
        return maxRadius;
    }

    public bool IsInitialized()
    {
        return initialized;
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmos()
    {
        if (!showGridGizmos)
            return;

        if (grid == null)
            grid = GetComponent<Grid>();

        if (grid == null)
            return;

        Gizmos.color = Color.gray;

        int minX = GetMinX();
        int maxX = GetMaxX();
        int minY = GetMinY();
        int maxY = GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!IsInsideGrid(new Vector2Int(x, y)))
                    continue;

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

    private Vector3 GetLogicalGridCorner(int x, int y)
    {
        Vector3Int cell = new Vector3Int(
            x + width / 2,
            y + height / 2,
            0
        );

        return ConvertGridToWorldSpace(grid.CellToWorld(cell));
    }
}
