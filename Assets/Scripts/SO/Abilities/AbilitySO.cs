using System.Collections.Generic;
using UnityEngine;

public abstract class AbilitySO : ScriptableObject
{
    public enum RangeShape
    {
        Diamond,
        Box,
        FourDirections,
        Diagonal
    }

    [Header("Ability")]
    [SerializeField] private string abilityName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Combat")]
    [SerializeField] private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [Header("Range")]
    [SerializeField, Min(1)]
    private int range = 1;

    [SerializeField, Min(0)]
    private int minDistance = 0;

    [SerializeField]
    private RangeShape rangeShape = RangeShape.Diamond;

    // ==================================================
    // GETTERS
    // ==================================================

    public string GetAbilityName()
    {
        return abilityName;
    }

    public string GetDescription()
    {
        return description;
    }

    public int GetDamage()
    {
        return damage;
    }

    public int GetCooldown()
    {
        return cooldown;
    }

    public int GetRange()
    {
        return range;
    }

    public int GetMinDistance()
    {
        return minDistance;
    }

    public RangeShape GetRangeShape()
    {
        return rangeShape;
    }

    // ==================================================
    // GET RANGE TILES
    // ==================================================

    /// <summary>
    /// Returns the tiles affected by this ability's range.
    ///
    /// The returned positions are ABSOLUTE grid positions.
    /// Minimum distance is excluded.
    /// Maximum distance is defined by range.
    /// </summary>
    public virtual List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null)
    {
        List<Vector2Int> tiles =
            new List<Vector2Int>();

        if (gridManager == null ||
            user == null)
        {
            return tiles;
        }

        Vector2Int origin =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        int abilityRange =
            Mathf.Max(1, range);

        int minimumDistance =
            Mathf.Clamp(
                minDistance,
                0,
                abilityRange
            );

        switch (rangeShape)
        {
            // ==========================================
            // DIAMOND
            // ==========================================

            case RangeShape.Diamond:

                for (int x = -abilityRange;
                     x <= abilityRange;
                     x++)
                {
                    for (int y = -abilityRange;
                         y <= abilityRange;
                         y++)
                    {
                        Vector2Int offset =
                            new Vector2Int(x, y);

                        int distance =
                            Mathf.Abs(x) +
                            Mathf.Abs(y);

                        // Outside maximum range
                        if (distance > abilityRange)
                        {
                            continue;
                        }

                        // Inside minimum range
                        if (distance < minimumDistance)
                        {
                            continue;
                        }

                        AddValidTile(
                            gridManager,
                            tiles,
                            origin + offset
                        );
                    }
                }

                break;

            // ==========================================
            // BOX
            // ==========================================

            case RangeShape.Box:

                for (int x = -abilityRange;
                     x <= abilityRange;
                     x++)
                {
                    for (int y = -abilityRange;
                         y <= abilityRange;
                         y++)
                    {
                        Vector2Int offset =
                            new Vector2Int(x, y);

                        int distance =
                            Mathf.Max(
                                Mathf.Abs(x),
                                Mathf.Abs(y)
                            );

                        // Outside maximum range
                        if (distance > abilityRange)
                        {
                            continue;
                        }

                        // Inside minimum range
                        if (distance < minimumDistance)
                        {
                            continue;
                        }

                        AddValidTile(
                            gridManager,
                            tiles,
                            origin + offset
                        );
                    }
                }

                break;

            // ==========================================
            // FOUR DIRECTIONS
            // ==========================================

            case RangeShape.FourDirections:

                for (int i = 1;
                     i <= abilityRange;
                     i++)
                {
                    if (i < minimumDistance)
                    {
                        continue;
                    }

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        Vector2Int.up * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        Vector2Int.down * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        Vector2Int.left * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        Vector2Int.right * i
                    );
                }

                break;

            // ==========================================
            // DIAGONAL
            // ==========================================

            case RangeShape.Diagonal:

                for (int i = 1;
                     i <= abilityRange;
                     i++)
                {
                    if (i < minimumDistance)
                    {
                        continue;
                    }

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(i, i)
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(-i, i)
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(i, -i)
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(-i, -i)
                    );
                }

                break;
        }

        return tiles;
    }

    // ==================================================
    // ADD VALID TILE
    // ==================================================

    private void AddValidTile(
        GridManager gridManager,
        List<Vector2Int> tiles,
        Vector2Int position)
    {
        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        if (!tiles.Contains(position))
        {
            tiles.Add(position);
        }
    }

    // ==================================================
    // CAN HIT
    // ==================================================

    public virtual bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target)
    {
        if (gridManager == null ||
            user == null ||
            target == null)
        {
            return false;
        }

        Vector2Int userPosition =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        // ==============================================
        // DISTANCE
        // ==============================================

        int distance =
            gridManager.GetDistance(
                userPosition,
                targetPosition
            );

        int minimumDistance =
            Mathf.Clamp(
                minDistance,
                0,
                range
            );

        int maximumDistance =
            Mathf.Max(
                minimumDistance,
                range
            );

        // ==============================================
        // MINIMUM DISTANCE CHECK
        // ==============================================

        if (distance < minimumDistance)
        {
            return false;
        }

        // ==============================================
        // MAXIMUM DISTANCE CHECK
        // ==============================================

        if (distance > maximumDistance)
        {
            return false;
        }

        // ==============================================
        // HITBOX CHECK
        // ==============================================

        List<Vector2Int> hitbox =
            GetHitboxTiles(
                gridManager,
                user,
                target
            );

        bool canHit =
            hitbox.Contains(targetPosition);

        return canHit;
    }

    // ==================================================
    // USE ABILITY
    // ==================================================

    public virtual bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null)
        {
            Debug.LogWarning(
                $"[Ability] {abilityName}: user is null."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[Ability] {abilityName}: target is null."
            );

            return false;
        }

        Debug.Log(
            $"[Ability] {user.name} used " +
            $"{abilityName} on {target.name}."
        );

        return true;
    }
}