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
    [SerializeField]
    private string abilityName;

    [TextArea]
    [SerializeField]
    private string description;

    [Header("Combat")]
    [SerializeField]
    private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [Header("Targeting Range")]
    [SerializeField, Min(1)]
    private int range = 1;

    [SerializeField, Min(0)]
    private int minDistance = 0;

    [SerializeField]
    private RangeShape rangeShape = RangeShape.Diamond;

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

    public virtual List<Vector2Int> GetRangeTiles(
        GridManager gridManager,
        GameObject user
    )
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
            Mathf.Max(
                1,
                range
            );

        int minimumDistance =
            Mathf.Clamp(
                minDistance,
                0,
                abilityRange
            );

        switch (rangeShape)
        {
            case RangeShape.Diamond:

                for (
                    int x = -abilityRange;
                    x <= abilityRange;
                    x++
                )
                {
                    for (
                        int y = -abilityRange;
                        y <= abilityRange;
                        y++
                    )
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int distance =
                            Mathf.Abs(x) +
                            Mathf.Abs(y);

                        if (distance > abilityRange)
                        {
                            continue;
                        }

                        if (distance < minimumDistance)
                        {
                            continue;
                        }

                        Vector2Int position =
                            origin +
                            new Vector2Int(x, y);

                        AddValidTile(
                            gridManager,
                            tiles,
                            position
                        );
                    }
                }

                break;

            case RangeShape.Box:

                for (
                    int x = -abilityRange;
                    x <= abilityRange;
                    x++
                )
                {
                    for (
                        int y = -abilityRange;
                        y <= abilityRange;
                        y++
                    )
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int distance =
                            Mathf.Max(
                                Mathf.Abs(x),
                                Mathf.Abs(y)
                            );

                        if (distance > abilityRange)
                        {
                            continue;
                        }

                        if (distance < minimumDistance)
                        {
                            continue;
                        }

                        Vector2Int position =
                            origin +
                            new Vector2Int(x, y);

                        AddValidTile(
                            gridManager,
                            tiles,
                            position
                        );
                    }
                }

                break;

            case RangeShape.FourDirections:

                for (
                    int i = 1;
                    i <= abilityRange;
                    i++
                )
                {
                    if (i < minimumDistance)
                    {
                        continue;
                    }

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin + Vector2Int.up * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin + Vector2Int.down * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin + Vector2Int.left * i
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin + Vector2Int.right * i
                    );
                }

                break;

            case RangeShape.Diagonal:

                for (
                    int i = 1;
                    i <= abilityRange;
                    i++
                )
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

    public virtual List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null
    )
    {
        return GetRangeTiles(
            gridManager,
            user
        );
    }

    protected void AddValidTile(
        GridManager gridManager,
        List<Vector2Int> tiles,
        Vector2Int position
    )
    {
        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        if (!tiles.Contains(position))
        {
            tiles.Add(position);
        }
    }

    public virtual bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target
    )
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

        if (distance < minimumDistance)
        {
            return false;
        }

        if (distance > maximumDistance)
        {
            return false;
        }

        List<Vector2Int> rangeTiles =
            GetRangeTiles(
                gridManager,
                user
            );

        if (rangeTiles == null)
        {
            return false;
        }

        return rangeTiles.Contains(
            targetPosition
        );
    }

    public virtual bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null ||
            target == null)
        {
            return false;
        }

        return true;
    }
}