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

    [SerializeField]
    private Sprite abilityIcon;

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
        Debug.Log(
            "[AbilitySO DEBUG] GetUserGridPosition START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            )
        );

        if (
            gridManager == null ||
            user == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] GetUserGridPosition FAILED | " +
                "GridManager=" + (
                    gridManager != null
                        ? "VALID"
                        : "NULL"
                ) +
                " | User=" + (
                    user != null
                        ? "VALID"
                        : "NULL"
                )
            );

            return Vector2Int.zero;
        }


        // ========================================================
        // AUTHORITATIVE POSITION
        // ========================================================
        //
        // The actual transform position is converted through the
        // GridManager.
        //
        // This is intentionally NOT:
        //
        //     pin.GetTile()
        //
        // because UnitTilePin can become stale if another system
        // changes transform.position without updating the pin.
        //
        // Example of the bug this prevents:
        //
        // UnitTilePin = (-1,-3)
        // Transform   = (-3.90,-3.90,0)
        // WorldToGrid  = (-3,-3)
        //
        // In that situation (-3,-3) is the position that the
        // ability system should use.
        // ========================================================

        Vector3 worldPosition =
            user.transform.position;

        Vector2Int worldTile =
            gridManager.WorldToGridPosition(
                worldPosition
            );


        // ========================================================
        // UNIT TILE PIN DEBUG
        // ========================================================

        UnitTilePin pin =
            user.GetComponent<UnitTilePin>();

        if (
            pin != null &&
            pin.HasTile()
        )
        {
            Vector2Int pinnedTile =
                pin.GetTile();

            if (pinnedTile != worldTile)
            {
                Debug.LogWarning(
                    "[AbilitySO DEBUG] GetUserGridPosition | " +
                    "UNIT TILE DESYNC DETECTED | " +
                    "User=" + user.name +
                    " | UnitTilePin=" + pinnedTile +
                    " | WorldToGrid=" + worldTile +
                    " | WorldPosition=" + worldPosition +
                    " | USING WorldToGrid"
                );
            }
            else
            {
                Debug.Log(
                    "[AbilitySO DEBUG] GetUserGridPosition | " +
                    "UnitTilePin agrees with WorldToGrid | " +
                    "User=" + user.name +
                    " | Tile=" + worldTile
                );
            }
        }
        else
        {
            Debug.Log(
                "[AbilitySO DEBUG] GetUserGridPosition | " +
                "No valid UnitTilePin | " +
                "User=" + user.name +
                " | Tile=" + worldTile
            );
        }


        Debug.Log(
            "[AbilitySO DEBUG] GetUserGridPosition RESULT | " +
            "User=" + user.name +
            " | World=" + worldPosition +
            " | Grid=" + worldTile
        );

        return worldTile;
    }


    // ============================================================
    // MOVEMENT RESTRICTION
    // ============================================================

    public bool CanUseAfterMovement(
        GameObject user
    )
    {
        Debug.Log(
            "[AbilitySO DEBUG] CanUseAfterMovement START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | canAttackWithThisAfterMove=" +
            canAttackWithThisAfterMove
        );

        if (user == null)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanUseAfterMovement FALSE | " +
                "Reason=User is NULL"
            );

            return false;
        }

        UnitMoveBrain moveBrain =
            user.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanUseAfterMovement TRUE | " +
                "Reason=No UnitMoveBrain found"
            );

            return true;
        }

        bool canMove =
            moveBrain.CanMoveThisTurn();

        Debug.Log(
            "[AbilitySO DEBUG] CanUseAfterMovement | " +
            "CanMoveThisTurn=" + canMove +
            " | canAttackWithThisAfterMove=" +
            canAttackWithThisAfterMove
        );

        if (canMove)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanUseAfterMovement TRUE | " +
                "Reason=Unit can still move this turn"
            );

            return true;
        }

        bool result =
            canAttackWithThisAfterMove;

        Debug.Log(
            "[AbilitySO DEBUG] CanUseAfterMovement RESULT | " +
            "Result=" + result +
            " | Reason=Unit already moved"
        );

        return result;
    }


    // ============================================================
    // RANGE
    // ============================================================

    public virtual List<Vector2Int> GetRangeTiles(
        GridManager gridManager,
        GameObject user
    )
    {
        Debug.Log(
            "[AbilitySO DEBUG] GetRangeTiles START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | Range=" + range +
            " | MinDistance=" + minDistance +
            " | Shape=" + rangeShape
        );

        List<Vector2Int> tiles =
            new List<Vector2Int>();


        if (
            gridManager == null ||
            user == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] GetRangeTiles RETURN EMPTY | " +
                "GridManager=" + (
                    gridManager != null
                        ? "VALID"
                        : "NULL"
                ) +
                " | User=" + (
                    user != null
                        ? "VALID"
                        : "NULL"
                )
            );

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


        Debug.Log(
            "[AbilitySO DEBUG] GetRangeTiles PARAMETERS | " +
            "Ability=" + abilityName +
            " | Origin=" + origin +
            " | RawRange=" + range +
            " | EffectiveRange=" + abilityRange +
            " | RawMinDistance=" + minDistance +
            " | EffectiveMinDistance=" + minimumDistance +
            " | Shape=" + rangeShape
        );


        switch (rangeShape)
        {
            // ====================================================
            // DIAMOND
            // ====================================================

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


            // ====================================================
            // BOX
            // ====================================================

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


            // ====================================================
            // FOUR DIRECTIONS
            // ====================================================

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


            // ====================================================
            // DIAGONAL
            // ====================================================

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


        Debug.Log(
            "[AbilitySO DEBUG] GetRangeTiles RESULT | " +
            "Ability=" + abilityName +
            " | Origin=" + origin +
            " | TileCount=" + tiles.Count +
            " | Tiles=" + string.Join(
                ", ",
                tiles
            )
        );

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
        Debug.Log(
            "[AbilitySO DEBUG] GetHitboxTiles | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | Target=" + (
                target != null
                    ? target.name
                    : "NULL"
            )
        );

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
            Debug.Log(
                "[AbilitySO DEBUG] AddValidTile SKIPPED | " +
                "Reason=GridManager NULL | " +
                "Position=" + position
            );

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
        Debug.Log(
            "[AbilitySO DEBUG] ============================="
        );

        Debug.Log(
            "[AbilitySO DEBUG] CanHit START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | Target=" + (
                target != null
                    ? target.name
                    : "NULL"
            ) +
            " | Range=" + range +
            " | MinDistance=" + minDistance +
            " | Shape=" + rangeShape +
            " | TargetType=" + targetType +
            " | CanAttackAfterMove=" +
            canAttackWithThisAfterMove
        );

        if (
            gridManager == null ||
            user == null ||
            target == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHit FALSE | " +
                "Reason=GridManager/User/Target NULL"
            );

            return false;
        }


        bool canUseAfterMovement =
            CanUseAfterMovement(user);

        Debug.Log(
            "[AbilitySO DEBUG] CanHit | " +
            "CanUseAfterMovement=" +
            canUseAfterMovement
        );

        if (!canUseAfterMovement)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHit FALSE | " +
                "Reason=CanUseAfterMovement returned FALSE"
            );

            return false;
        }


        bool canTarget =
            CanTargetObject(
                user,
                target
            );

        Debug.Log(
            "[AbilitySO DEBUG] CanHit | " +
            "CanTargetObject=" +
            canTarget
        );

        if (!canTarget)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHit FALSE | " +
                "Reason=CanTargetObject returned FALSE"
            );

            return false;
        }


        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        Debug.Log(
            "[AbilitySO DEBUG] CanHit | " +
            "TargetWorldPosition=" +
            target.transform.position +
            " | TargetGridPosition=" +
            targetPosition
        );


        bool canHitTile =
            CanHitTile(
                gridManager,
                user,
                targetPosition
            );

        Debug.Log(
            "[AbilitySO DEBUG] CanHit RESULT | " +
            "CanHitTile=" +
            canHitTile +
            " | TargetGridPosition=" +
            targetPosition
        );

        return canHitTile;
    }


    // ============================================================
    // CAN TARGET OBJECT
    // ============================================================

    public virtual bool CanTargetObject(
        GameObject user,
        GameObject target
    )
    {
        Debug.Log(
            "[AbilitySO DEBUG] CanTargetObject START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | Target=" + (
                target != null
                    ? target.name
                    : "NULL"
            ) +
            " | TargetType=" + targetType
        );

        if (
            user == null ||
            target == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanTargetObject FALSE | " +
                "Reason=User or Target NULL"
            );

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
            Debug.Log(
                "[AbilitySO DEBUG] CanTargetObject FALSE | " +
                "Reason=Missing AttackUnit | " +
                "UserAttackUnit=" + (
                    userUnit != null
                        ? "VALID"
                        : "NULL"
                ) +
                " | TargetAttackUnit=" + (
                    targetUnit != null
                        ? "VALID"
                        : "NULL"
                )
            );

            return false;
        }


        Team userTeam =
            userUnit.GetTeam();

        Team targetTeam =
            targetUnit.GetTeam();


        Debug.Log(
            "[AbilitySO DEBUG] CanTargetObject TEAMS | " +
            "User=" + user.name +
            " | UserTeam=" + userTeam +
            " | Target=" + target.name +
            " | TargetTeam=" + targetTeam +
            " | RequiredTargetType=" + targetType
        );


        // ========================================================
        // ENEMY
        // ========================================================

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
                bool result =
                    targetTeam == Team.Enemy;

                Debug.Log(
                    "[AbilitySO DEBUG] CanTargetObject RESULT | " +
                    "Enemy targeting from Player/Ally | " +
                    "Result=" + result
                );

                return result;
            }


            if (
                userTeam ==
                Team.Enemy
            )
            {
                bool result =
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;

                Debug.Log(
                    "[AbilitySO DEBUG] CanTargetObject RESULT | " +
                    "Enemy targeting from Enemy | " +
                    "Result=" + result
                );

                return result;
            }


            Debug.Log(
                "[AbilitySO DEBUG] CanTargetObject FALSE | " +
                "Reason=Unsupported user team"
            );

            return false;
        }


        // ========================================================
        // ALLY
        // ========================================================

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
                bool result =
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;

                Debug.Log(
                    "[AbilitySO DEBUG] CanTargetObject RESULT | " +
                    "Ally targeting from Player/Ally | " +
                    "Result=" + result
                );

                return result;
            }


            if (
                userTeam ==
                Team.Enemy
            )
            {
                bool result =
                    targetTeam == Team.Enemy;

                Debug.Log(
                    "[AbilitySO DEBUG] CanTargetObject RESULT | " +
                    "Ally targeting from Enemy | " +
                    "Result=" + result
                );

                return result;
            }


            Debug.Log(
                "[AbilitySO DEBUG] CanTargetObject FALSE | " +
                "Reason=Unsupported user team"
            );

            return false;
        }


        // ========================================================
        // ANY
        // ========================================================

        if (
            targetType ==
            TargetType.Any
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanTargetObject TRUE | " +
                "TargetType=Any"
            );

            return true;
        }


        Debug.Log(
            "[AbilitySO DEBUG] CanTargetObject FALSE | " +
            "Reason=Unsupported TargetType"
        );

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
        Debug.Log(
            "[AbilitySO DEBUG] CanHitTile START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | TargetPosition=" + targetPosition +
            " | Range=" + range +
            " | MinDistance=" + minDistance +
            " | Shape=" + rangeShape
        );

        if (
            gridManager == null ||
            user == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHitTile FALSE | " +
                "Reason=GridManager or User NULL"
            );

            return false;
        }


        bool canUseAfterMovement =
            CanUseAfterMovement(user);

        Debug.Log(
            "[AbilitySO DEBUG] CanHitTile | " +
            "CanUseAfterMovement=" +
            canUseAfterMovement
        );

        if (!canUseAfterMovement)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHitTile FALSE | " +
                "Reason=CanUseAfterMovement returned FALSE"
            );

            return false;
        }


        bool insideGrid =
            gridManager.IsInsideGrid(
                targetPosition
            );

        Debug.Log(
            "[AbilitySO DEBUG] CanHitTile | " +
            "TargetPosition=" + targetPosition +
            " | IsInsideGrid=" + insideGrid
        );

        if (!insideGrid)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHitTile FALSE | " +
                "Reason=Target tile is outside grid"
            );

            return false;
        }


        List<Vector2Int> rangeTiles =
            GetRangeTiles(
                gridManager,
                user
            );


        if (rangeTiles == null)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHitTile FALSE | " +
                "Reason=GetRangeTiles returned NULL"
            );

            return false;
        }


        bool containsTarget =
            rangeTiles.Contains(
                targetPosition
            );

        Debug.Log(
            "[AbilitySO DEBUG] CanHitTile | " +
            "RangeTileCount=" + rangeTiles.Count +
            " | TargetPosition=" + targetPosition +
            " | ContainsTarget=" + containsTarget
        );

        if (!containsTarget)
        {
            Debug.Log(
                "[AbilitySO DEBUG] CanHitTile FALSE | " +
                "Reason=Target tile is NOT in ability range"
            );

            return false;
        }


        Debug.Log(
            "[AbilitySO DEBUG] CanHitTile TRUE | " +
            "Target tile is inside ability range"
        );

        return true;
    }


    // ============================================================
    // USE GAMEOBJECT
    // ============================================================

    public virtual bool Use(
        GameObject user,
        GameObject target
    )
    {
        Debug.Log(
            "[AbilitySO DEBUG] Use START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | Target=" + (
                target != null
                    ? target.name
                    : "NULL"
            )
        );

        if (
            user == null ||
            target == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] Use FALSE | " +
                "Reason=User or Target NULL"
            );

            return false;
        }


        bool canUseAfterMovement =
            CanUseAfterMovement(user);

        Debug.Log(
            "[AbilitySO DEBUG] Use | " +
            "CanUseAfterMovement=" +
            canUseAfterMovement
        );

        if (!canUseAfterMovement)
        {
            Debug.Log(
                "[AbilitySO DEBUG] Use FALSE | " +
                "Reason=CanUseAfterMovement returned FALSE"
            );

            return false;
        }


        Debug.Log(
            "[AbilitySO DEBUG] Use TRUE | " +
            "Ability=" + abilityName
        );

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
        Debug.Log(
            "[AbilitySO DEBUG] UseAtTile START | " +
            "Ability=" + abilityName +
            " | User=" + (
                user != null
                    ? user.name
                    : "NULL"
            ) +
            " | TargetTile=" + targetTile
        );

        if (
            user == null ||
            gridManager == null
        )
        {
            Debug.Log(
                "[AbilitySO DEBUG] UseAtTile FALSE | " +
                "Reason=User or GridManager NULL"
            );

            return false;
        }


        bool canUseAfterMovement =
            CanUseAfterMovement(user);

        Debug.Log(
            "[AbilitySO DEBUG] UseAtTile | " +
            "CanUseAfterMovement=" +
            canUseAfterMovement
        );

        if (!canUseAfterMovement)
        {
            Debug.Log(
                "[AbilitySO DEBUG] UseAtTile FALSE | " +
                "Reason=CanUseAfterMovement returned FALSE"
            );

            return false;
        }


        bool canHitTile =
            CanHitTile(
                gridManager,
                user,
                targetTile
            );

        Debug.Log(
            "[AbilitySO DEBUG] UseAtTile | " +
            "CanHitTile=" + canHitTile
        );

        if (!canHitTile)
        {
            Debug.Log(
                "[AbilitySO DEBUG] UseAtTile FALSE | " +
                "Reason=CanHitTile returned FALSE"
            );

            return false;
        }


        Debug.Log(
            "[AbilitySO DEBUG] UseAtTile TRUE | " +
            "Ability=" + abilityName +
            " | TargetTile=" + targetTile
        );

        return true;
    }


    // ============================================================
    // ICON
    // ============================================================

    public Sprite GetAbilityIcon()
    {
        return abilityIcon;
    }
}