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

    public enum TargetType
    {
        Enemy,
        Ally,
        Any
    }


    // ============================================================
    // ABILITY
    // ============================================================

    [Header("Ability")]

    [SerializeField]
    private string abilityName;

    [TextArea]
    [SerializeField]
    private string description;


    // ============================================================
    // COMBAT
    // ============================================================

    [Header("Combat")]

    [SerializeField]
    private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [Tooltip(
        "If TRUE, this ability can be used after the unit has moved this turn."
    )]
    [SerializeField]
    private bool canAttackWithThisAfterMove = false;

    [Tooltip(
        "Number of times this ability can be used per turn. 0 = unlimited."
    )]
    [SerializeField, Min(0)]
    private int usesPerTurn = 1;

    [Tooltip(
        "Time the attack/effect takes before the next attack can happen."
    )]
    [SerializeField, Min(0f)]
    private float useDuration = 0.25f;


    // ============================================================
    // TARGETING
    // ============================================================

    [Header("Targeting")]

    [SerializeField]
    private TargetType targetType = TargetType.Enemy;


    // ============================================================
    // TARGETING RANGE
    // ============================================================

    [Header("Targeting Range")]

    [SerializeField, Min(1)]
    private int range = 1;

    [SerializeField, Min(0)]
    private int minDistance = 0;

    [SerializeField]
    private RangeShape rangeShape = RangeShape.Diamond;


    // ============================================================
    // GETTERS
    // ============================================================

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

    public int GetUsesPerTurn()
    {
        return usesPerTurn;
    }

    public float GetUseDuration()
    {
        return useDuration;
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

    public TargetType GetTargetType()
    {
        return targetType;
    }

    public bool CanAttackWithThisAfterMove()
    {
        return canAttackWithThisAfterMove;
    }


    // ============================================================
    // GET USER TILE
    // ============================================================

    protected Vector2Int GetUserGridPosition(
        GridManager gridManager,
        GameObject user
    )
    {
        if (
            gridManager == null ||
            user == null
        )
        {
            return Vector2Int.zero;
        }


        UnitTilePin pin =
            user.GetComponent<UnitTilePin>();


        if (
            pin != null &&
            pin.HasTile()
        )
        {
            return pin.GetTile();
        }


        return gridManager.WorldToGridPosition(
            user.transform.position
        );
    }


    // ============================================================
    // MOVEMENT RESTRICTION
    // ============================================================

    public bool CanUseAfterMovement(
        GameObject user
    )
    {
        if (user == null)
        {
            return false;
        }

        UnitMoveBrain moveBrain =
            user.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            return true;
        }

        if (moveBrain.CanMoveThisTurn())
        {
            return true;
        }

        return canAttackWithThisAfterMove;
    }


    // ============================================================
    // RANGE
    // ============================================================

    public virtual List<Vector2Int> GetRangeTiles(
        GridManager gridManager,
        GameObject user
    )
    {
        List<Vector2Int> tiles =
            new List<Vector2Int>();


        if (
            gridManager == null ||
            user == null
        )
        {
            return tiles;
        }


        Vector2Int origin =
            GetUserGridPosition(
                gridManager,
                user
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
                        if (
                            x == 0 &&
                            y == 0
                        )
                        {
                            continue;
                        }


                        int distance =
                            Mathf.Abs(x) +
                            Mathf.Abs(y);


                        if (
                            distance >
                            abilityRange
                        )
                        {
                            continue;
                        }


                        if (
                            distance <
                            minimumDistance
                        )
                        {
                            continue;
                        }


                        AddValidTile(
                            gridManager,
                            tiles,
                            origin +
                            new Vector2Int(
                                x,
                                y
                            )
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
                        if (
                            x == 0 &&
                            y == 0
                        )
                        {
                            continue;
                        }


                        int distance =
                            Mathf.Max(
                                Mathf.Abs(x),
                                Mathf.Abs(y)
                            );


                        if (
                            distance >
                            abilityRange
                        )
                        {
                            continue;
                        }


                        if (
                            distance <
                            minimumDistance
                        )
                        {
                            continue;
                        }


                        AddValidTile(
                            gridManager,
                            tiles,
                            origin +
                            new Vector2Int(
                                x,
                                y
                            )
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
                    if (
                        i <
                        minimumDistance
                    )
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


            case RangeShape.Diagonal:

                for (
                    int i = 1;
                    i <= abilityRange;
                    i++
                )
                {
                    if (
                        i <
                        minimumDistance
                    )
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


    // ============================================================
    // HITBOX
    // ============================================================

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


    // ============================================================
    // VALID TILE
    // ============================================================

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


        if (
            !gridManager.IsInsideGrid(
                position
            )
        )
        {
            return;
        }


        if (
            !tiles.Contains(
                position
            )
        )
        {
            tiles.Add(position);
        }
    }


    // ============================================================
    // CAN HIT GAMEOBJECT
    // ============================================================

    public virtual bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target
    )
    {
        if (
            gridManager == null ||
            user == null ||
            target == null
        )
        {
            return false;
        }


        if (!CanUseAfterMovement(user))
        {
            return false;
        }


        if (
            !CanTargetObject(
                user,
                target
            )
        )
        {
            return false;
        }


        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        return CanHitTile(
            gridManager,
            user,
            targetPosition
        );
    }


    // ============================================================
    // CAN TARGET OBJECT
    // ============================================================

    public virtual bool CanTargetObject(
        GameObject user,
        GameObject target
    )
    {
        if (
            user == null ||
            target == null
        )
        {
            return false;
        }


        AttackUnit userUnit =
            user.GetComponent<AttackUnit>();

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();


        if (
            userUnit == null ||
            targetUnit == null
        )
        {
            return false;
        }


        Team userTeam =
            userUnit.GetTeam();

        Team targetTeam =
            targetUnit.GetTeam();


        if (
            targetType ==
            TargetType.Enemy
        )
        {
            if (
                userTeam == Team.Player ||
                userTeam == Team.Ally
            )
            {
                return targetTeam == Team.Enemy;
            }


            if (
                userTeam ==
                Team.Enemy
            )
            {
                return
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;
            }


            return false;
        }


        if (
            targetType ==
            TargetType.Ally
        )
        {
            if (
                userTeam == Team.Player ||
                userTeam == Team.Ally
            )
            {
                return
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;
            }


            if (
                userTeam ==
                Team.Enemy
            )
            {
                return targetTeam == Team.Enemy;
            }


            return false;
        }


        if (
            targetType ==
            TargetType.Any
        )
        {
            return true;
        }


        return false;
    }


    // ============================================================
    // CAN HIT TILE
    // ============================================================

    public virtual bool CanHitTile(
        GridManager gridManager,
        GameObject user,
        Vector2Int targetPosition
    )
    {
        if (
            gridManager == null ||
            user == null
        )
        {
            return false;
        }


        if (!CanUseAfterMovement(user))
        {
            return false;
        }


        if (
            !gridManager.IsInsideGrid(
                targetPosition
            )
        )
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


    // ============================================================
    // USE GAMEOBJECT
    // ============================================================

    public virtual bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (
            user == null ||
            target == null
        )
        {
            return false;
        }


        if (!CanUseAfterMovement(user))
        {
            return false;
        }


        return true;
    }


    // ============================================================
    // USE AT TILE
    // ============================================================

    public virtual bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile
    )
    {
        if (
            user == null ||
            gridManager == null
        )
        {
            return false;
        }


        if (!CanUseAfterMovement(user))
        {
            return false;
        }


        if (
            !CanHitTile(
                gridManager,
                user,
                targetTile
            )
        )
        {
            return false;
        }


        return true;
    }
}