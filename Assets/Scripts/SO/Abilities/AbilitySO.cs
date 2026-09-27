using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static AbilitySO;

public abstract class AbilitySO : ScriptableObject
{
    public enum RangeShape
    {
        Diamond,
        Box,
        FourDirections,
        Diagonal,
        FourdirectionsAndDiragonal,
        Shotgun
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

    public AudioClip launchSFX;

    [Tooltip(
        "If TRUE, this ability requires an enemy/object target. " +
        "If FALSE, it can be used directly on a tile."
    )]
    public bool NeedEnemy;


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

    public Sprite GetAbilityIcon()
    {
        return abilityIcon;
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

    public bool NeedsEnemy()
    {
        return NeedEnemy;
    }


    // ============================================================
    // EFFECTIVE DAMAGE
    // ============================================================

    public virtual int GetEffectiveDamage(GameObject user)
    {
        if (user == null)
        {
            return damage;
        }

        UpgradeableCombatUnit upgradeable =
            user.GetComponent<UpgradeableCombatUnit>();

        if (upgradeable == null)
        {
            upgradeable =
                user.GetComponentInParent<UpgradeableCombatUnit>();
        }

        if (upgradeable == null)
        {
            upgradeable =
                user.GetComponentInChildren<UpgradeableCombatUnit>();
        }

        if (upgradeable == null)
        {
            return damage;
        }

        return upgradeable.GetEffectiveDamage(this);
    }


    // ============================================================
    // EFFECTIVE RANGE
    // ============================================================

    public virtual int GetEffectiveRange(GameObject user)
    {
        if (user == null)
        {
            return Mathf.Max(1, range);
        }

        UpgradeableCombatUnit upgradeable =
            user.GetComponent<UpgradeableCombatUnit>();

        if (upgradeable == null)
        {
            upgradeable =
                user.GetComponentInParent<UpgradeableCombatUnit>();
        }

        if (upgradeable == null)
        {
            upgradeable =
                user.GetComponentInChildren<UpgradeableCombatUnit>();
        }

        if (upgradeable == null)
        {
            return Mathf.Max(1, range);
        }

        return upgradeable.GetEffectiveRange(this);
    }


    // ============================================================
    // GET USER GRID POSITION
    // ============================================================

    protected Vector2Int GetUserGridPosition(
        GridManager gridManager,
        GameObject user
    )
    {
        if (gridManager == null || user == null)
        {
            return Vector2Int.zero;
        }

        Vector3 worldPosition =
            user.transform.position;

        Vector2Int worldTile =
            gridManager.WorldToGridPosition(
                worldPosition
            );

        return worldTile;
    }


    // ============================================================
    // GET USER FACING DIRECTION
    // ============================================================

    protected Vector2Int GetFacingDirection(
        GameObject user
    )
    {
        if (user == null)
        {
            return Vector2Int.up;
        }

        UnitMoveBrain moveBrain =
            user.GetComponent<UnitMoveBrain>();

        if (moveBrain != null)
        {
            Vector2Int direction =
                moveBrain.GetFacingDirection();

            if (direction != Vector2Int.zero)
            {
                return direction;
            }
        }

        return Vector2Int.up;
    }


    // ============================================================
    // GET MOUSE DIRECTION
    // ============================================================
    //
    // Uses Unity's New Input System.
    //
    // The mouse determines whether the shotgun points:
    //
    // UP
    // DOWN
    // LEFT
    // RIGHT
    //
    // based on the mouse's position relative to the unit.
    //
    // ============================================================

    protected Vector2Int GetMouseDirection(
        GridManager gridManager,
        GameObject user
    )
    {
        if (gridManager == null || user == null)
        {
            return Vector2Int.up;
        }

        if (Camera.main == null)
        {
            return Vector2Int.up;
        }

        if (Mouse.current == null)
        {
            return Vector2Int.up;
        }

        // --------------------------------------------------------
        // Get mouse screen position
        // --------------------------------------------------------

        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();


        // --------------------------------------------------------
        // Convert screen position to world position
        // --------------------------------------------------------

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    Mathf.Abs(
                        Camera.main.transform.position.z
                    )
                )
            );


        // --------------------------------------------------------
        // Convert mouse world position to grid position
        // --------------------------------------------------------

        Vector2Int mouseTile =
            gridManager.WorldToGridPosition(
                mouseWorldPosition
            );


        // --------------------------------------------------------
        // Get user's grid position
        // --------------------------------------------------------

        Vector2Int userTile =
            GetUserGridPosition(
                gridManager,
                user
            );


        // --------------------------------------------------------
        // Calculate direction from unit to mouse
        // --------------------------------------------------------

        Vector2Int difference =
            mouseTile - userTile;


        if (difference == Vector2Int.zero)
        {
            return Vector2Int.up;
        }


        // --------------------------------------------------------
        // Determine dominant axis
        // --------------------------------------------------------
        //
        // Example:
        //
        // Mouse is far to the right:
        // RIGHT
        //
        // Mouse is far above:
        // UP
        //
        // Mouse is diagonal:
        // whichever axis is stronger wins.
        //
        // --------------------------------------------------------

        if (
            Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y)
        )
        {
            if (difference.x > 0)
            {
                return Vector2Int.right;
            }

            return Vector2Int.left;
        }


        if (difference.y > 0)
        {
            return Vector2Int.up;
        }

        return Vector2Int.down;
    }


    // ============================================================
    // MOVEMENT RESTRICTION
    // ============================================================

    public bool CanUseAfterMovement(GameObject user)
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

        bool canMove =
            moveBrain.CanMoveThisTurn();

        if (canMove)
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

        if (gridManager == null || user == null)
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
                GetEffectiveRange(user)
            );

        int minimumDistance =
            Mathf.Clamp(
                minDistance,
                0,
                abilityRange
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

                        AddValidTile(
                            gridManager,
                            tiles,
                            origin +
                            new Vector2Int(x, y)
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

                        AddValidTile(
                            gridManager,
                            tiles,
                            origin +
                            new Vector2Int(x, y)
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


            // ====================================================
            // FOUR DIRECTIONS + DIAGONALS
            // ====================================================

            case RangeShape.FourdirectionsAndDiragonal:

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


            // ====================================================
            // SHOTGUN
            // ====================================================
            //
            // Direction is determined by the mouse.
            //
            // Distance 1 = 1 tile
            // Distance 2 = 3 tiles
            // Distance 3 = 5 tiles
            // Distance 4 = 7 tiles
            //
            // Example facing UP:
            //
            //         X
            //       X X X
            //     X X X X X
            //
            // ====================================================

            case RangeShape.Shotgun:

                Vector2Int direction =
                    GetMouseDirection(
                        gridManager,
                        user
                    );

                for (
                    int distance = 1;
                    distance <= abilityRange;
                    distance++
                )
                {
                    if (distance < minimumDistance)
                    {
                        continue;
                    }

                    // Width grows with distance.
                    //
                    // Distance 1 = 1 tile
                    // Distance 2 = 3 tiles
                    // Distance 3 = 5 tiles
                    // Distance 4 = 7 tiles

                    int halfWidth =
                        distance - 1;

                    for (
                        int side = -halfWidth;
                        side <= halfWidth;
                        side++
                    )
                    {
                        Vector2Int offset;

                        // ========================================
                        // UP / DOWN
                        // ========================================

                        if (
                            direction == Vector2Int.up ||
                            direction == Vector2Int.down
                        )
                        {
                            offset =
                                new Vector2Int(
                                    side,
                                    direction.y * distance
                                );
                        }

                        // ========================================
                        // LEFT / RIGHT
                        // ========================================

                        else
                        {
                            offset =
                                new Vector2Int(
                                    direction.x * distance,
                                    side
                                );
                        }

                        AddValidTile(
                            gridManager,
                            tiles,
                            origin + offset
                        );
                    }
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

        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        if (!tiles.Contains(position))
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

        if (NeedEnemy && !CanTargetObject(user, target))
        {
            return false;
        }

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        if (
            !CanHitTile(
                gridManager,
                user,
                targetPosition
            )
        )
        {
            return false;
        }

        return true;
    }


    // ============================================================
    // CAN TARGET OBJECT
    // ============================================================

    public virtual bool CanTargetObject(
        GameObject user,
        GameObject target
    )
    {
        if (user == null || target == null)
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


        if (targetType == TargetType.Enemy)
        {
            if (
                userTeam == Team.Player ||
                userTeam == Team.Ally
            )
            {
                return targetTeam == Team.Enemy;
            }

            if (userTeam == Team.Enemy)
            {
                return
                    targetTeam == Team.Player ||
                    targetTeam == Team.Ally;
            }

            return false;
        }


        if (targetType == TargetType.Ally)
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

            if (userTeam == Team.Enemy)
            {
                return targetTeam == Team.Enemy;
            }

            return false;
        }


        if (targetType == TargetType.Any)
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
