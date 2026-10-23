
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Ability")]
    [SerializeField] private string abilityName;
    [SerializeField] private Sprite abilityIcon;

    [TextArea]
    [SerializeField] private string description;

    public AudioClip launchSFX;

    [Tooltip("If TRUE, this ability requires an enemy/object target. If FALSE, it can be used directly on a tile.")]
    public bool NeedEnemy;

    [Header("Combat")]
    [SerializeField] private int damage = 10;

    [SerializeField, Min(0)]
    private int cooldown = 0;

    [SerializeField]
    private bool canAttackWithThisAfterMove = false;

    [SerializeField, Min(0)]
    private int usesPerTurn = 1;

    [SerializeField, Min(0f)]
    private float useDuration = 0.25f;

    [Header("Targeting")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;

    [Header("Targeting Range")]
    [SerializeField, Min(1)] private int range = 1;
    [SerializeField, Min(0)] private int minDistance = 0;
    [SerializeField] private RangeShape rangeShape = RangeShape.Diamond;

    public string GetAbilityName() => abilityName;
    public Sprite GetAbilityIcon() => abilityIcon;
    public string GetDescription() => description;
    public int GetDamage() => damage;
    public int GetCooldown() => cooldown;
    public int GetUsesPerTurn() => usesPerTurn;
    public float GetUseDuration() => useDuration;
    public int GetRange() => range;
    public int GetMinDistance() => minDistance;
    public RangeShape GetRangeShape() => rangeShape;
    public TargetType GetTargetType() => targetType;
    public bool CanAttackWithThisAfterMove() => canAttackWithThisAfterMove;
    public bool NeedsEnemy() => NeedEnemy;

    public virtual int GetEffectiveDamage(GameObject user)
    {
        if (user == null)
            return damage;

        UpgradeableCombatUnit upgradeable =
            FindComponent<UpgradeableCombatUnit>(user);

        return upgradeable == null
            ? damage
            : upgradeable.GetEffectiveDamage(this);
    }

    public virtual int GetEffectiveRange(GameObject user)
    {
        if (user == null)
            return Mathf.Max(1, range);

        UpgradeableCombatUnit upgradeable =
            FindComponent<UpgradeableCombatUnit>(user);

        return upgradeable == null
            ? Mathf.Max(1, range)
            : upgradeable.GetEffectiveRange(this);
    }

    private static T FindComponent<T>(GameObject obj)
        where T : Component
    {
        T component = obj.GetComponent<T>();

        if (component == null)
            component = obj.GetComponentInParent<T>();

        if (component == null)
            component = obj.GetComponentInChildren<T>();

        return component;
    }

    protected Vector2Int GetUserGridPosition(
        GridManager gridManager,
        GameObject user)
    {
        if (gridManager == null || user == null)
            return Vector2Int.zero;

        AttackUnit unit = FindComponent<AttackUnit>(user);

        if (unit != null)
            return unit.GetLogicalGridPosition();

        return gridManager.WorldToGridPosition(user.transform.position);
    }

    protected Vector2Int GetFacingDirection(GameObject user)
    {
        if (user == null)
            return Vector2Int.up;

        UnitMoveBrain moveBrain = FindComponent<UnitMoveBrain>(user);

        if (moveBrain != null)
        {
            Vector2Int direction = moveBrain.GetFacingDirection();

            if (direction != Vector2Int.zero)
                return direction;
        }

        return Vector2Int.up;
    }

    protected Vector2Int GetMouseDirection(
        GridManager gridManager,
        GameObject user)
    {
        if (gridManager == null ||
            user == null ||
            Camera.main == null ||
            Mouse.current == null)
            return Vector2Int.up;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                Mathf.Abs(Camera.main.transform.position.z)
            )
        );

        Vector2Int mouseTile =
            gridManager.WorldToGridPosition(mouseWorldPosition);

        Vector2Int userTile =
            GetUserGridPosition(gridManager, user);

        Vector2Int difference = mouseTile - userTile;

        if (difference == Vector2Int.zero)
            return Vector2Int.up;

        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
            return difference.x > 0 ? Vector2Int.right : Vector2Int.left;

        return difference.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    public bool CanUseAfterMovement(GameObject user)
    {
        if (user == null)
            return false;

        UnitMoveBrain moveBrain = FindComponent<UnitMoveBrain>(user);

        if (moveBrain == null)
            return true;

        if (moveBrain.CanMoveThisTurn())
            return true;

        return canAttackWithThisAfterMove;
    }

    public virtual List<Vector2Int> GetRangeTiles(
        GridManager gridManager,
        GameObject user)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (gridManager == null || user == null)
            return tiles;

        Vector2Int origin = GetUserGridPosition(gridManager, user);

        int abilityRange = Mathf.Max(1, GetEffectiveRange(user));
        int minimumDistance = Mathf.Clamp(minDistance, 0, abilityRange);

        switch (rangeShape)
        {
            case RangeShape.Diamond:
                for (int x = -abilityRange; x <= abilityRange; x++)
                {
                    for (int y = -abilityRange; y <= abilityRange; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        int distance = Mathf.Abs(x) + Mathf.Abs(y);

                        if (distance > abilityRange || distance < minimumDistance)
                            continue;

                        AddValidTile(gridManager, tiles, origin + new Vector2Int(x, y));
                    }
                }
                break;

            case RangeShape.Box:
                for (int x = -abilityRange; x <= abilityRange; x++)
                {
                    for (int y = -abilityRange; y <= abilityRange; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        int distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                        if (distance > abilityRange || distance < minimumDistance)
                            continue;

                        AddValidTile(gridManager, tiles, origin + new Vector2Int(x, y));
                    }
                }
                break;

            case RangeShape.FourDirections:
                for (int i = 1; i <= abilityRange; i++)
                {
                    if (i < minimumDistance)
                        continue;

                    AddValidTile(gridManager, tiles, origin + Vector2Int.up * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.down * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.left * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.right * i);
                }
                break;

            case RangeShape.Diagonal:
                for (int i = 1; i <= abilityRange; i++)
                {
                    if (i < minimumDistance)
                        continue;

                    AddValidTile(gridManager, tiles, origin + new Vector2Int(i, i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(-i, i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(i, -i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(-i, -i));
                }
                break;

            case RangeShape.FourdirectionsAndDiragonal:
                for (int i = 1; i <= abilityRange; i++)
                {
                    if (i < minimumDistance)
                        continue;

                    AddValidTile(gridManager, tiles, origin + Vector2Int.up * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.down * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.left * i);
                    AddValidTile(gridManager, tiles, origin + Vector2Int.right * i);
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(i, i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(-i, i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(i, -i));
                    AddValidTile(gridManager, tiles, origin + new Vector2Int(-i, -i));
                }
                break;

            case RangeShape.Shotgun:
                Vector2Int direction = GetMouseDirection(gridManager, user);

                for (int distance = 1; distance <= abilityRange; distance++)
                {
                    if (distance < minimumDistance)
                        continue;

                    int halfWidth = distance - 1;

                    for (int side = -halfWidth; side <= halfWidth; side++)
                    {
                        Vector2Int offset;

                        if (direction == Vector2Int.up ||
                            direction == Vector2Int.down)
                        {
                            offset = new Vector2Int(side, direction.y * distance);
                        }
                        else
                        {
                            offset = new Vector2Int(direction.x * distance, side);
                        }

                        AddValidTile(gridManager, tiles, origin + offset);
                    }
                }
                break;
        }

        return tiles;
    }

    public virtual List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null)
    {
        return GetRangeTiles(gridManager, user);
    }

    protected void AddValidTile(
        GridManager gridManager,
        List<Vector2Int> tiles,
        Vector2Int position)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position))
            return;

        if (!tiles.Contains(position))
            tiles.Add(position);
    }

    public virtual bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target)
    {
        if (gridManager == null || user == null || target == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        if (NeedEnemy && !CanTargetObject(user, target))
            return false;

        AttackUnit targetUnit = FindComponent<AttackUnit>(target);

        if (targetUnit == null)
        {
            Vector2Int targetPosition =
                gridManager.WorldToGridPosition(target.transform.position);

            return CanHitTile(gridManager, user, targetPosition);
        }

        Vector2Int anchor = targetUnit.GetLogicalGridPosition();

        CharacterSO character = targetUnit.GetCharacterData();

        int footprintSize = character != null
            ? Mathf.Max(1, character.GetFootprintSize())
            : Mathf.Max(
                1,
                gridManager.GetUnitFootprintSize(targetUnit.gameObject)
            );

        // The grid uses a centered footprint around the logical anchor.
        int minOffset = -(footprintSize / 2);

        for (int x = 0; x < footprintSize; x++)
        {
            for (int y = 0; y < footprintSize; y++)
            {
                Vector2Int occupiedTile = anchor + new Vector2Int(
                    minOffset + x,
                    minOffset + y
                );

                if (!gridManager.IsInsideGrid(occupiedTile))
                    continue;

                if (CanHitTile(gridManager, user, occupiedTile))
                    return true;
            }
        }

        return false;
    }

    public virtual bool CanTargetObject(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return false;

        AttackUnit userUnit = FindComponent<AttackUnit>(user);
        AttackUnit targetUnit = FindComponent<AttackUnit>(target);

        if (userUnit == null || targetUnit == null)
            return false;

        Team userTeam = userUnit.GetTeam();
        Team targetTeam = targetUnit.GetTeam();

        if (targetType == TargetType.Enemy)
        {
            if (userTeam == Team.Player || userTeam == Team.Ally)
                return targetTeam == Team.Enemy;

            if (userTeam == Team.Enemy)
                return targetTeam == Team.Player || targetTeam == Team.Ally;

            return false;
        }

        if (targetType == TargetType.Ally)
        {
            if (userTeam == Team.Player || userTeam == Team.Ally)
                return targetTeam == Team.Player || targetTeam == Team.Ally;

            if (userTeam == Team.Enemy)
                return targetTeam == Team.Enemy;

            return false;
        }

        return targetType == TargetType.Any;
    }

    public virtual bool CanHitTile(
        GridManager gridManager,
        GameObject user,
        Vector2Int targetPosition)
    {
        if (gridManager == null || user == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        if (!gridManager.IsInsideGrid(targetPosition))
            return false;

        List<Vector2Int> rangeTiles = GetRangeTiles(gridManager, user);

        return rangeTiles != null && rangeTiles.Contains(targetPosition);
    }

    public virtual bool Use(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        return true;
    }

    public virtual bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        if (user == null || gridManager == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        return CanHitTile(gridManager, user, targetTile);
    }
}
