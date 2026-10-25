using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class WaterManager : MonoBehaviour
{
    [SerializeField] private Tilemap waterTilemap;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GrowWater();
        }
    }

    private void GrowWater()
    {
        BoundsInt bounds = waterTilemap.cellBounds;

        // Collect all currently occupied water tiles
        var waterPositions = new System.Collections.Generic.List<Vector3Int>();

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (waterTilemap.HasTile(pos))
            {
                waterPositions.Add(pos);
            }
        }

        // Grow each water tile in all 8 directions
        foreach (Vector3Int pos in waterPositions)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int neighbor = pos + new Vector3Int(x, y, 0);

                    if (!waterTilemap.HasTile(neighbor))
                    {
                        TileBase tile = waterTilemap.GetTile(pos);
                        waterTilemap.SetTile(neighbor, tile);
                    }
                }
            }
        }
    }
}