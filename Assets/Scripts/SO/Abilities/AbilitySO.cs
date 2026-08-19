using System.Collections.Generic;
using UnityEngine;

public abstract class AbilitySO : ScriptableObject
{
    [Header("Ability")]
    [SerializeField] private string abilityName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Combat")]
    [SerializeField] private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [Header("UI")]
    [SerializeField, Min(1)]
    private int range = 1;

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

    // ==================================================
    // HITBOX TILES
    // ==================================================

    /// <summary>
    /// Returns the tiles affected by the ability.
    /// Specific abilities can override this if they have
    /// special shapes such as cones, lines, areas, etc.
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

        Vector2Int userPosition =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        int abilityRange = Mathf.Max(1, range);

        // Add every tile within the ability range.
        // This means the ability is no longer limited
        // to only the immediately adjacent tiles.
        for (int x = -abilityRange; x <= abilityRange; x++)
        {
            for (int y = -abilityRange; y <= abilityRange; y++)
            {
                Vector2Int tile =
                    userPosition +
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(tile))
                    continue;

                int distance =
                    gridManager.GetDistance(
                        userPosition,
                        tile
                    );

                if (distance <= abilityRange)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    // ==================================================
    // CAN HIT
    // ==================================================

    /// <summary>
    /// Determines whether the ability can hit the target.
    ///
    /// IMPORTANT:
    /// The ability's RANGE is now the actual restriction.
    /// There is no hardcoded 1-tile restriction here.
    /// </summary>
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

        int distance =
            gridManager.GetDistance(
                userPosition,
                targetPosition
            );

        int abilityRange =
            Mathf.Max(1, range);

        bool canHit =
            distance <= abilityRange;

        Debug.Log(
            $"[Ability] {abilityName}: " +
            $"User={user.name}, " +
            $"Target={target.name}, " +
            $"Distance={distance}, " +
            $"Range={abilityRange}, " +
            $"CanHit={canHit}"
        );

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