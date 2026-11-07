
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private GridShapeManager gridShapeManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BossLobsterBrain bossLobster;

    [Header("Grid Size Changes")]
    [SerializeField, Min(0)] private int widthChange = 1;
    [SerializeField, Min(0)] private int heightChange = 1;

    [Header("Tide Limits")]
    [SerializeField, Min(0)] private int maxShrinkCount = 3;

    [Header("Tide")]
    [SerializeField] private bool tideAdvancing = true;

    private int shrinkCount = 0;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridShapeManager == null)
            gridShapeManager = FindFirstObjectByType<GridShapeManager>();

        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();
    }

    private void Start()
    {
        UpdateBossWaterStatus();
    }

    public void AdvanceTide()
    {
        if (tideAdvancing)
        {
            // Tide comes in: remove water from the edges.
            RetreatWater();

            // Shrink one cell from every edge.
            if (gridShapeManager != null)
            {
                gridShapeManager.ResizeGridRelative(
                    -widthChange * 2,
                    -heightChange * 2
                );
            }

            shrinkCount++;

            if (encounterManager != null)
                encounterManager.TideComesIn();

            // Reverse tide after reaching maximum shrink.
            if (shrinkCount >= maxShrinkCount)
            {
                tideAdvancing = false;
            }
        }
        else
        {
            // Tide goes out: restore water at the edges.
            ExpandWater();

            // Expand one cell on every edge.
            if (gridShapeManager != null)
            {
                gridShapeManager.ResizeGridRelative(
                    widthChange * 2,
                    heightChange * 2
                );
            }

            shrinkCount--;

            if (encounterManager != null)
                encounterManager.TideGoesOut();

            // Reverse tide once the original size is restored.
            if (shrinkCount <= 0)
            {
                shrinkCount = 0;
                tideAdvancing = true;
            }
        }

        UpdateBossWaterStatus();
    }

    public void UpdateBossWaterStatus()
    {
        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();

        if (bossLobster != null)
            bossLobster.RefreshWaterStatus();
    }

    public bool IsGridPositionOnWater(Vector2Int gridPosition)
    {
        if (waterTilemap == null)
            return false;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(gridPosition);

        Vector3Int cellPosition =
            waterTilemap.WorldToCell(worldPosition);

        TileBase tile =
            waterTilemap.GetTile(cellPosition);

        return tile != null &&
               (waterTile == null || tile == waterTile);
    }

    public bool IsPositionOnWater(Vector3 worldPosition)
    {
        if (waterTilemap == null)
            return false;

        Vector3Int cellPosition =
            waterTilemap.WorldToCell(worldPosition);

        TileBase tile =
            waterTilemap.GetTile(cellPosition);

        return tile != null &&
               (waterTile == null || tile == waterTile);
    }

    private void ExpandWater()
    {
        if (waterTilemap == null || waterTile == null)
            return;

        List<Vector3Int> waterPositions = GetWaterPositions();

        HashSet<Vector3Int> positionsToAdd =
            new HashSet<Vector3Int>();

        foreach (Vector3Int position in waterPositions)
        {
            foreach (Vector3Int direction in GetDirections())
            {
                Vector3Int neighbor = position + direction;

                if (!waterTilemap.cellBounds.Contains(neighbor))
                    continue;

                if (!waterTilemap.HasTile(neighbor))
                    positionsToAdd.Add(neighbor);
            }
        }

        foreach (Vector3Int position in positionsToAdd)
        {
            waterTilemap.SetTile(position, waterTile);
        }
    }

    private void RetreatWater()
    {
        if (waterTilemap == null)
            return;

        List<Vector3Int> waterPositions = GetWaterPositions();

        List<Vector3Int> positionsToRemove =
            new List<Vector3Int>();

        foreach (Vector3Int position in waterPositions)
        {
            foreach (Vector3Int direction in GetDirections())
            {
                Vector3Int neighbor = position + direction;

                if (!waterTilemap.cellBounds.Contains(neighbor) ||
                    !waterTilemap.HasTile(neighbor))
                {
                    positionsToRemove.Add(position);
                    break;
                }
            }
        }

        foreach (Vector3Int position in positionsToRemove)
        {
            waterTilemap.SetTile(position, null);
        }
    }

    private List<Vector3Int> GetWaterPositions()
    {
        List<Vector3Int> positions =
            new List<Vector3Int>();

        if (waterTilemap == null)
            return positions;

        BoundsInt bounds = waterTilemap.cellBounds;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = waterTilemap.GetTile(position);

            if (tile != null &&
                (waterTile == null || tile == waterTile))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private Vector3Int[] GetDirections()
    {
        return new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };
    }
}
