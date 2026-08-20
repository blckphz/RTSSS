using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeSwoosh",
    menuName = "Combat/Abilities/FrontAttack"
)]
public class FrontAttack : AbilitySO
{
    // ==================================================
    // GET HITBOX TILES
    // ==================================================

    public override List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (gridManager == null || user == null)
        {
            return tiles;
        }

        Vector2Int userPosition =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        int abilityRange = Mathf.Max(1, GetRange());

        Vector2Int direction = Vector2Int.zero;

        // ==================================================
        // 1. CHECK SPECIFIED TARGET
        // ==================================================

        if (target != null)
        {
            Vector2Int targetPosition =
                gridManager.WorldToGridPosition(
                    target.transform.position
                );

            direction = GetAttackDirection(
                userPosition,
                targetPosition
            );
        }

        // ==================================================
        // 2. CHECK FOR ENEMIES IN 3x3 RADIUS
        // ==================================================

        if (direction == Vector2Int.zero)
        {
            direction = DetectNearbyEnemyDirection(
                gridManager,
                user,
                userPosition
            );
        }

        // ==================================================
        // 3. FALLBACK TO USER FACING DIRECTION
        // ==================================================

        if (direction == Vector2Int.zero)
        {
            direction = GetUserFacingDirection(user);
        }

        // ==================================================
        // GET SIDE DIRECTION
        // ==================================================

        Vector2Int sideDirection =
            GetSideDirection(direction);

        // ==================================================
        // CREATE ROTATED 3-WIDE SWOOSH
        // ==================================================

        for (int distance = 1;
             distance <= abilityRange;
             distance++)
        {
            Vector2Int centerTile =
                userPosition + direction * distance;

            Vector2Int firstTile =
                centerTile + sideDirection;

            Vector2Int secondTile =
                centerTile;

            Vector2Int thirdTile =
                centerTile - sideDirection;

            // First tile
            if (gridManager.IsInsideGrid(firstTile) &&
                !tiles.Contains(firstTile))
            {
                tiles.Add(firstTile);
            }

            // Center tile
            if (gridManager.IsInsideGrid(secondTile) &&
                !tiles.Contains(secondTile))
            {
                tiles.Add(secondTile);
            }

            // Third tile
            if (gridManager.IsInsideGrid(thirdTile) &&
                !tiles.Contains(thirdTile))
            {
                tiles.Add(thirdTile);
            }
        }

        return tiles;
    }

    // ==================================================
    // DETECT NEARBY ENEMY
    //
    // Checks the entire 3x3 area around the user.
    //
    // This includes:
    //
    // X X X
    // X O X
    // X X X
    //
    // O = user
    // X = possible enemy
    // ==================================================

    private Vector2Int DetectNearbyEnemyDirection(
        GridManager gridManager,
        GameObject user,
        Vector2Int userPosition)
    {
        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        GameObject closestEnemy = null;

        Vector2Int closestEnemyPosition =
            Vector2Int.zero;

        int closestDistance =
            int.MaxValue;

        // ==================================================
        // CHECK ENTIRE 3x3 AREA
        // ==================================================

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Don't check the user's own tile.
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector2Int checkPosition =
                    userPosition +
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(checkPosition))
                {
                    continue;
                }

                GameObject unitOnTile =
                    gridManager.GetUnitAt(checkPosition);

                if (unitOnTile == null ||
                    unitOnTile == user)
                {
                    continue;
                }

                HealthManager targetHealth =
                    unitOnTile.GetComponent<HealthManager>();

                if (targetHealth == null ||
                    targetHealth.IsDead())
                {
                    continue;
                }

                // ==================================================
                // IGNORE ALLIES
                // ==================================================

                if (userHealth != null &&
                    targetHealth.GetTeam() ==
                    userHealth.GetTeam())
                {
                    continue;
                }

                // ==================================================
                // FIND CLOSEST ENEMY
                // ==================================================

                int distance =
                    Mathf.Abs(x) +
                    Mathf.Abs(y);

                if (distance < closestDistance)
                {
                    closestDistance = distance;

                    closestEnemy = unitOnTile;

                    closestEnemyPosition =
                        checkPosition;
                }
            }
        }

        // ==================================================
        // NO ENEMY FOUND
        // ==================================================

        if (closestEnemy == null)
        {
            return Vector2Int.zero;
        }

        // ==================================================
        // GET ENEMY DIRECTION
        // ==================================================

        Vector2Int difference =
            closestEnemyPosition -
            userPosition;

        // ==================================================
        // CONVERT DIAGONAL INTO CARDINAL DIRECTION
        //
        // Example:
        //
        // Enemy:
        //
        // X .
        // . O
        //
        // becomes LEFT
        //
        // Example:
        //
        // . X
        // O .
        //
        // becomes RIGHT
        // ==================================================

        if (Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y))
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        if (Mathf.Abs(difference.y) > 0)
        {
            return difference.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return Vector2Int.zero;
    }

    // ==================================================
    // USE ABILITY
    // ==================================================

    public override bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null)
        {
            Debug.LogWarning(
                "[MeleeSwoosh] Cannot attack. User is null."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[MeleeSwoosh] Cannot attack. Target is null."
            );

            return false;
        }

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[MeleeSwoosh] GridManager not found."
            );

            return false;
        }

        // ==================================================
        // CHECK IF TARGET CAN BE HIT
        // ==================================================

        if (!CanHit(
                gridManager,
                user,
                target))
        {
            Debug.LogWarning(
                $"[MeleeSwoosh] {target.name} is outside the ability range."
            );

            return false;
        }

        // ==================================================
        // GET HITBOX
        // ==================================================

        List<Vector2Int> hitboxTiles =
            GetHitboxTiles(
                gridManager,
                user,
                target
            );

        if (hitboxTiles == null ||
            hitboxTiles.Count == 0)
        {
            Debug.LogWarning(
                "[MeleeSwoosh] Hitbox contains no valid tiles."
            );

            return false;
        }

        // ==================================================
        // ATTACK ALL TILES
        // ==================================================

        bool hitSomething = false;

        foreach (Vector2Int position in hitboxTiles)
        {
            if (AttackTile(
                    gridManager,
                    position,
                    user))
            {
                hitSomething = true;
            }
        }

        // ==================================================
        // RESULT
        // ==================================================

        if (hitSomething)
        {
            Debug.Log(
                $"[MeleeSwoosh] {user.name} attacked {target.name}."
            );
        }
        else
        {
            Debug.Log(
                $"[MeleeSwoosh] {user.name} used the ability, " +
                "but no enemy was found in the hitbox."
            );
        }

        return hitSomething;
    }

    // ==================================================
    // DIRECTION CALCULATIONS
    // ==================================================

    private Vector2Int GetAttackDirection(
        Vector2Int userPosition,
        Vector2Int targetPosition)
    {
        Vector2Int difference =
            targetPosition - userPosition;

        // ==================================================
        // HORIZONTAL TARGET
        // ==================================================

        if (Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y))
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        // ==================================================
        // VERTICAL TARGET
        // ==================================================

        if (difference.y != 0)
        {
            return difference.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return Vector2Int.zero;
    }

    // ==================================================
    // GET USER FACING DIRECTION
    // ==================================================

    private Vector2Int GetUserFacingDirection(
        GameObject user)
    {
        Vector3 forward =
            user.transform.up;

        if (Mathf.Abs(forward.x) >
            Mathf.Abs(forward.y))
        {
            return forward.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        if (forward.y != 0)
        {
            return forward.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return Vector2Int.up;
    }

    // ==================================================
    // GET SIDE DIRECTION
    //
    // UP/DOWN -> side is LEFT/RIGHT
    //
    // LEFT/RIGHT -> side is UP/DOWN
    // ==================================================

    private Vector2Int GetSideDirection(
        Vector2Int direction)
    {
        if (direction == Vector2Int.up ||
            direction == Vector2Int.down)
        {
            return Vector2Int.right;
        }

        return Vector2Int.up;
    }

    // ==================================================
    // ATTACK TILE
    // ==================================================

    private bool AttackTile(
        GridManager gridManager,
        Vector2Int position,
        GameObject user)
    {
        if (!gridManager.IsInsideGrid(position))
        {
            return false;
        }

        GameObject target =
            gridManager.GetUnitAt(position);

        if (target == null ||
            target == user)
        {
            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null ||
            targetHealth.IsDead())
        {
            return false;
        }

        // ==================================================
        // DON'T HIT ALLIES
        // ==================================================

        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        if (userHealth != null &&
            targetHealth.GetTeam() ==
            userHealth.GetTeam())
        {
            return false;
        }

        // ==================================================
        // DEAL DAMAGE
        // ==================================================

        int damage = GetDamage();

        targetHealth.TakeDamage(damage);

        Debug.Log(
            $"[MeleeSwoosh] {user.name} hit " +
            $"{target.name} for {damage} damage."
        );

        return true;
    }
}