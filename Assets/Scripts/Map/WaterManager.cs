
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
        if (gridShapeManager == null)
            gridShapeManager = FindFirstObjectByType<GridShapeManager>();
    }

    public void AdvanceTide()
    {
        if (tideAdvancing)
        {
            // Stop shrinking after 3 advances.
            if (shrinkCount < maxShrinkCount)
            {
                RetreatWater();

                if (gridShapeManager != null)
                {
                    gridShapeManager.ResizeGridRelative(
                        -widthChange,
                        -heightChange);
                }

                shrinkCount++;

                Debug.Log(
                    "[WATER] Shrink count: " +
                    shrinkCount + "/" + maxShrinkCount);
            }
            else
            {
                Debug.Log("[WATER] Maximum shrink limit reached.");
            }

            if (encounterManager != null)
                encounterManager.TideComesIn();
        }
        else
        {
            ExpandWater();

            if (gridShapeManager != null)
            {
                gridShapeManager.ResizeGridRelative(
                    widthChange,
                    heightChange);
            }

            if (encounterManager != null)
                encounterManager.TideGoesOut();
        }
    }

    private void ExpandWater()
    {
        List<Vector3Int> waterPositions = GetWaterPositions();
        HashSet<Vector3Int> positionsToAdd = new HashSet<Vector3Int>();

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
            if (waterTile != null)
                waterTilemap.SetTile(position, waterTile);
        }
    }

    private void RetreatWater()
    {
        List<Vector3Int> waterPositions = GetWaterPositions();
        List<Vector3Int> positionsToRemove = new List<Vector3Int>();

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
        List<Vector3Int> positions = new List<Vector3Int>();

        if (waterTilemap == null)
            return positions;

        BoundsInt bounds = waterTilemap.cellBounds;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = waterTilemap.GetTile(position);

            if (tile != null && (waterTile == null || tile == waterTile))
                positions.Add(position);
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
