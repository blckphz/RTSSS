using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class WaterManager : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap waterTilemap;

    [SerializeField] private InputActionReference growWaterAction;

    private void OnEnable()
    {
        growWaterAction.action.performed += OnGrowWater;
        growWaterAction.action.Enable();
    }

    private void OnDisable()
    {
        growWaterAction.action.performed -= OnGrowWater;
        growWaterAction.action.Disable();
    }

    private void OnGrowWater(InputAction.CallbackContext context)
    {
        ExpandWaterInward();
    }

    private void ExpandWaterInward()
    {
        BoundsInt bounds = groundTilemap.cellBounds;

        HashSet<Vector3Int> groundToWater = new HashSet<Vector3Int>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                // Only consider ground tiles
                if (!groundTilemap.HasTile(cell))
                    continue;

                // Already water? Ignore it.
                if (waterTilemap.HasTile(cell))
                    continue;

                // Is this ground tile touching water?
                if (IsAdjacentToWater(cell))
                {
                    groundToWater.Add(cell);
                }
            }
        }

        // Convert the entire outer layer at once.
        foreach (Vector3Int cell in groundToWater)
        {
            TileBase groundTile = groundTilemap.GetTile(cell);

            waterTilemap.SetTile(cell, groundTile);
            groundTilemap.SetTile(cell, null);
        }
    }

    private bool IsAdjacentToWater(Vector3Int cell)
    {
        Vector3Int[] directions =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        foreach (Vector3Int direction in directions)
        {
            Vector3Int neighbor = cell + direction;

            if (waterTilemap.HasTile(neighbor))
                return true;
        }

        return false;
    }
}