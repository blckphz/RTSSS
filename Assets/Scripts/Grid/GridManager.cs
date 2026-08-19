using System.Collections.Generic;
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
    [SerializeField, Range(0f, 1f)]
    private float hoveredTileAlpha = 0.5f;

    [Header("Ability Range")]
    [SerializeField]
    private Color abilityRangeColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float abilityRangeAlpha = 0.35f;

    [Header("Gizmos")]
    [SerializeField]
    private bool showGridGizmos = true;

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
    // ABILITY RANGE STATE
    // ==================================================

    private readonly List<GameObject> abilityRangeTiles =
        new List<GameObject>();

    private readonly List<SpriteRenderer[]> abilityRangeRenderers =
        new List<SpriteRenderer[]>();

    private readonly List<Color[]> abilityRangeOriginalColors =
        new List<Color[]>();

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
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

        CreateFloor();

        Debug.Log(
            $"[GridManager] Initialized {width}x{height} grid.",
            this
        );
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

                Vector3 worldPosition =
                    GridToWorldPosition(position);

                GameObject floorTile =
                    Instantiate(
                        floorTilePrefab,
                        worldPosition,
                        Quaternion.identity,
                        floorParent
                    );

                floorTile.name =
                    $"Floor_{x}_{y}";

                floorTiles[x, y] =
                    floorTile;
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

        Vector3Int cellPosition =
            grid.WorldToCell(worldPosition);

        return new Vector2Int(
            cellPosition.x,
            cellPosition.y
        );
    }

    public Vector3 GridToWorldPosition(
        Vector2Int gridPosition)
    {
        if (grid == null)
        {
            return Vector3.zero;
        }

        Vector3Int cellPosition =
            new Vector3Int(
                gridPosition.x,
                gridPosition.y,
                0
            );

        return grid.GetCellCenterWorld(
            cellPosition
        );
    }

    // ==================================================
    // GRID CHECKS
    // ==================================================

    public bool IsInsideGrid(
        Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < width &&
               position.y >= 0 &&
               position.y < height;
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
            occupiedCells[
                position.x,
                position.y
            ] = null;

            return false;
        }

        HealthManager health =
            occupant.GetComponent<HealthManager>();

        if (health != null &&
            health.IsDead())
        {
            occupiedCells[
                position.x,
                position.y
            ] = null;

            return false;
        }

        return true;
    }

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
    // HOVERED TILE
    // ==================================================

    public void SetHoveredTile(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            ClearHoveredTile();
            return;
        }

        GameObject tile =
            floorTiles[
                position.x,
                position.y
            ];

        if (tile == null)
        {
            ClearHoveredTile();
            return;
        }

        if (tile == hoveredTile)
        {
            return;
        }

        ClearHoveredTile();

        hoveredTile =
            tile;

        hoveredTileRenderers =
            tile.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (hoveredTileRenderers == null ||
            hoveredTileRenderers.Length == 0)
        {
            hoveredTile = null;
            return;
        }

        hoveredTileOriginalColors =
            new Color[
                hoveredTileRenderers.Length
            ];

        for (
            int i = 0;
            i < hoveredTileRenderers.Length;
            i++
        )
        {
            SpriteRenderer sr =
                hoveredTileRenderers[i];

            if (sr == null)
            {
                continue;
            }

            hoveredTileOriginalColors[i] =
                sr.color;

            Color modifiedColor =
                sr.color;

            modifiedColor.a *=
                hoveredTileAlpha;

            sr.color =
                modifiedColor;
        }
    }

    public void ClearHoveredTile()
    {
        if (hoveredTileRenderers != null &&
            hoveredTileOriginalColors != null)
        {
            int count =
                Mathf.Min(
                    hoveredTileRenderers.Length,
                    hoveredTileOriginalColors.Length
                );

            for (int i = 0; i < count; i++)
            {
                if (hoveredTileRenderers[i] != null)
                {
                    hoveredTileRenderers[i].color =
                        hoveredTileOriginalColors[i];
                }
            }
        }

        hoveredTile = null;
        hoveredTileRenderers = null;
        hoveredTileOriginalColors = null;
    }

    // ==================================================
    // NORMAL ABILITY RANGE
    // ==================================================

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        ClearAbilityRange();

        if (!IsInsideGrid(centerPosition))
        {
            return;
        }

        range =
            Mathf.Max(0, range);

        Debug.Log(
            $"[GridManager] Showing normal ability range. " +
            $"Center={centerPosition}, Range={range}"
        );

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                int distance =
                    GetDistance(
                        centerPosition,
                        position
                    );

                if (distance > range)
                {
                    continue;
                }

                HighlightAbilityCell(position);
            }
        }
    }

    // ==================================================
    // CUSTOM HITBOX
    // ==================================================

    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets)
    {
        ClearAbilityRange();

        if (offsets == null ||
            offsets.Count == 0)
        {
            Debug.LogWarning(
                "[GridManager] ShowAbilityCells received " +
                "an empty hitbox."
            );

            return;
        }

        Debug.Log(
            $"[GridManager] Showing custom hitbox. " +
            $"Origin={origin}, Cells={offsets.Count}"
        );

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int position =
                origin + offset;

            if (!IsInsideGrid(position))
            {
                Debug.Log(
                    $"[GridManager] Hitbox cell {position} " +
                    $"is outside the grid."
                );

                continue;
            }

            Debug.Log(
                $"[GridManager] Highlighting hitbox cell " +
                $"{position} from offset {offset}"
            );

            HighlightAbilityCell(position);
        }
    }

    // ==================================================
    // SHOW SINGLE ABILITY CELL
    // ==================================================

    public void ShowAbilityCell(
        Vector2Int position)
    {
        if (!IsInsideGrid(position))
        {
            Debug.LogWarning(
                $"[GridManager] Cannot highlight " +
                $"outside-grid cell {position}."
            );

            return;
        }

        HighlightAbilityCell(position);
    }

    // ==================================================
    // INTERNAL CELL HIGHLIGHT
    // ==================================================

    private void HighlightAbilityCell(
        Vector2Int position)
    {
        GameObject tile =
            floorTiles[
                position.x,
                position.y
            ];

        if (tile == null)
        {
            return;
        }

        SpriteRenderer[] renderers =
            tile.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (renderers == null ||
            renderers.Length == 0)
        {
            return;
        }

        Color[] originalColors =
            new Color[
                renderers.Length
            ];

        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            SpriteRenderer sr =
                renderers[i];

            if (sr == null)
            {
                continue;
            }

            originalColors[i] =
                sr.color;

            sr.color =
                Color.Lerp(
                    sr.color,
                    abilityRangeColor,
                    abilityRangeAlpha
                );
        }

        abilityRangeTiles.Add(tile);

        abilityRangeRenderers.Add(
            renderers
        );

        abilityRangeOriginalColors.Add(
            originalColors
        );
    }

    // ==================================================
    // CLEAR ABILITY RANGE
    // ==================================================

    public void ClearAbilityRange()
    {
        for (
            int i = 0;
            i < abilityRangeRenderers.Count;
            i++
        )
        {
            SpriteRenderer[] renderers =
                abilityRangeRenderers[i];

            Color[] originalColors =
                abilityRangeOriginalColors[i];

            if (renderers == null ||
                originalColors == null)
            {
                continue;
            }

            int count =
                Mathf.Min(
                    renderers.Length,
                    originalColors.Length
                );

            for (int j = 0; j < count; j++)
            {
                if (renderers[j] != null)
                {
                    renderers[j].color =
                        originalColors[j];
                }
            }
        }

        abilityRangeTiles.Clear();
        abilityRangeRenderers.Clear();
        abilityRangeOriginalColors.Clear();
    }

    // ==================================================
    // UNIT MANAGEMENT
    // ==================================================

    public bool PlaceUnit(
        GameObject unit,
        Vector2Int position)
    {
        if (unit == null ||
            !IsInsideGrid(position) ||
            IsCellOccupied(position))
        {
            return false;
        }

        occupiedCells[
            position.x,
            position.y
        ] = unit;

        unit.transform.position =
            GridToWorldPosition(position);

        Debug.Log(
            $"[GridManager] Placed {unit.name} at {position}"
        );

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
                if (
                    occupiedCells[x, y] ==
                    unit
                )
                {
                    occupiedCells[x, y] =
                        null;

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

    public bool MoveUnit(
        GameObject unit,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        if (unit == null ||
            !IsInsideGrid(oldPosition) ||
            !IsInsideGrid(newPosition))
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
            GridToWorldPosition(
                newPosition
            );

        Debug.Log(
            $"[GridManager] Moved {unit.name}: " +
            $"{oldPosition} -> {newPosition}"
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
        return Mathf.Abs(a.x - b.x) +
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
            grid =
                GetComponent<Grid>();

            if (grid == null)
            {
                return;
            }
        }

        DrawGridLines();
        DrawGridBorder();
    }

    private void DrawGridLines()
    {
        Gizmos.color =
            Color.gray;

        for (
            int x = 0;
            x <= width;
            x++
        )
        {
            Gizmos.DrawLine(
                GetGridCorner(x, 0),
                GetGridCorner(x, height)
            );
        }

        for (
            int y = 0;
            y <= height;
            y++
        )
        {
            Gizmos.DrawLine(
                GetGridCorner(0, y),
                GetGridCorner(width, y)
            );
        }
    }

    private void DrawGridBorder()
    {
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
        return grid != null
            ? grid.CellToWorld(
                new Vector3Int(
                    x,
                    y,
                    0
                )
            )
            : Vector3.zero;
    }
}