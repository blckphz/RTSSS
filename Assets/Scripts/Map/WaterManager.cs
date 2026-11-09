
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BossLobsterBrain bossLobster;

    [Header("Tide Limits")]
    [SerializeField, Min(0)] private int maxShrinkCount = 3;

    [Header("Tide")]
    [SerializeField] private bool tideAdvancing = true;

    private int shrinkCount;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        UpdateBossWaterStatus();
    }

    // ========================================================
    // REFERENCES
    // ========================================================

    private void FindReferences()
    {
        if (encounterManager == null)
            encounterManager = FindFirstObjectByType<EncounterManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();
    }

    // ========================================================
    // TIDE VALIDATION
    // ========================================================

    private bool CanUseTide()
    {
        if (encounterManager == null)
            FindReferences();

        if (encounterManager == null)
            return false;

        EncounterDefinition encounter =
            encounterManager.CurrentEncounter;

        return encounter != null && encounter.useTide;
    }

    // ========================================================
    // ADVANCE TIDE
    // ========================================================

    public void AdvanceTide()
    {
        if (!CanUseTide())
            return;

        if (tideAdvancing)
        {
            // Water retreats.
            RetreatWater();

            // Grid resizing happens in EncounterManager.
            encounterManager.TideComesIn();

            shrinkCount++;

            if (shrinkCount >= maxShrinkCount)
                tideAdvancing = false;
        }
        else
        {
            // Water expands.
            ExpandWater();

            // Grid resizing happens in EncounterManager.
            encounterManager.TideGoesOut();

            shrinkCount--;

            if (shrinkCount <= 0)
            {
                shrinkCount = 0;
                tideAdvancing = true;
            }
        }

        UpdateBossWaterStatus();
    }

    // ========================================================
    // RESET TIDE
    // ========================================================

    public void ResetTide()
    {
        shrinkCount = 0;
        tideAdvancing = true;

        UpdateBossWaterStatus();
    }

    // ========================================================
    // BOSS WATER STATUS
    // ========================================================

    public void UpdateBossWaterStatus()
    {
        if (bossLobster == null)
            bossLobster = FindFirstObjectByType<BossLobsterBrain>();

        if (bossLobster != null)
            bossLobster.RefreshWaterStatus();
    }

    // ========================================================
    // WATER CHECKS
    // ========================================================

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

        return IsPositionOnWater(worldPosition);
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

    // ========================================================
    // EXPAND WATER
    // ========================================================

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

    // ========================================================
    // RETREAT WATER
    // ========================================================

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

    // ========================================================
    // WATER POSITIONS
    // ========================================================

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

    // ========================================================
    // DIRECTIONS
    // ========================================================

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
