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


    // =========================================================
    // ABILITY
    // =========================================================

    [Header("Ability")]
    [SerializeField]
    private string abilityName;

    [TextArea]
    [SerializeField]
    private string description;


    // =========================================================
    // COMBAT
    // =========================================================

    [Header("Combat")]

    [SerializeField]
    private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;


    // =========================================================
    // RANGE
    // =========================================================

    [Header("Targeting Range")]

    [Tooltip(
        "Maximum targeting distance. " +
        "For Box range 4, the preview is 9x9."
    )]
    [SerializeField, Min(1)]
    private int range = 1;

    [Tooltip(
        "Minimum targeting distance."
    )]
    [SerializeField, Min(0)]
    private int minDistance = 0;

    [SerializeField]
    private RangeShape rangeShape = RangeShape.Diamond;


    // =========================================================
    // GETTERS
    // =========================================================

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


    // =========================================================
    // GET RANGE TILES
    //
    // IMPORTANT:
    //
    // THIS IS THE TARGETING / PREVIEW AREA.
    //
    // It is NOT the actual attack hitbox.
    //
    // Box Range 4:
    //
    // 9 x 9 area
    //
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X O X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    //
    // =========================================================

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


        // =====================================================
        // RANGE SHAPE
        // =====================================================

        switch (rangeShape)
        {
            // =================================================
            // DIAMOND
            // =================================================

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

                        if (distance >
                            abilityRange)
                        {
                            continue;
                        }

                        if (distance <
                            minimumDistance)
                        {
                            continue;
                        }

                        Vector2Int position =
                            origin +
                            new Vector2Int(
                                x,
                                y
                            );

                        AddValidTile(
                            gridManager,
                            tiles,
                            position
                        );
                    }
                }

                break;


            // =================================================
            // BOX
            //
            // Range 1 = 3x3
            // Range 2 = 5x5
            // Range 3 = 7x7
            // Range 4 = 9x9
            // =================================================

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

                        if (distance >
                            abilityRange)
                        {
                            continue;
                        }

                        if (distance <
                            minimumDistance)
                        {
                            continue;
                        }

                        Vector2Int position =
                            origin +
                            new Vector2Int(
                                x,
                                y
                            );

                        AddValidTile(
                            gridManager,
                            tiles,
                            position
                        );
                    }
                }

                break;


            // =================================================
            // FOUR DIRECTIONS
            // =================================================

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


            // =================================================
            // DIAGONAL
            // =================================================

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
                        new Vector2Int(
                            i,
                            i
                        )
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(
                            -i,
                            i
                        )
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(
                            i,
                            -i
                        )
                    );

                    AddValidTile(
                        gridManager,
                        tiles,
                        origin +
                        new Vector2Int(
                            -i,
                            -i
                        )
                    );
                }

                break;
        }

        return tiles;
    }


    // =========================================================
    // GET HITBOX TILES
    //
    // THIS IS THE ACTUAL ATTACK AREA.
    //
    // Normal abilities use their targeting range.
    //
    // Special abilities such as FrontAttack override this.
    // =========================================================

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


    // =========================================================
    // ADD VALID TILE
    // =========================================================

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


    // =========================================================
    // CAN HIT
    //
    // IMPORTANT:
    //
    // This checks the targeting range.
    //
    // FrontAttack overrides this because its targeting
    // range is different from its actual hitbox.
    // =========================================================

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


    // =========================================================
    // USE
    // =========================================================

    public virtual bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null)
        {
            Debug.LogWarning(
                $"[Ability] {abilityName}: " +
                "user is null."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[Ability] {abilityName}: " +
                "target is null."
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