using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeSwoosh",
    menuName = "Combat/Abilities/Melee Swoosh"
)]
public class MeleeSwoosh : AbilitySO
{
    // ==================================================
    // GET HITBOX TILES
    // ==================================================

    public override List<Vector2Int> GetHitboxTiles(
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

        Vector2Int direction;

        // --------------------------------------------------
        // Determine attack direction
        // --------------------------------------------------

        if (target != null)
        {
            Vector2Int targetPosition =
                gridManager.WorldToGridPosition(
                    target.transform.position
                );

            direction =
                GetAttackDirection(
                    userPosition,
                    targetPosition
                );
        }
        else
        {
            direction = Vector2Int.up;
        }

        if (direction == Vector2Int.zero)
        {
            return tiles;
        }

        // --------------------------------------------------
        // Center tile
        // --------------------------------------------------

        Vector2Int centerTile =
            userPosition + direction;

        // --------------------------------------------------
        // Side direction
        // --------------------------------------------------

        Vector2Int sideDirection =
            GetSideDirection(direction);

        // --------------------------------------------------
        // Three attack tiles
        // --------------------------------------------------

        Vector2Int firstTile =
            centerTile + sideDirection;

        Vector2Int secondTile =
            centerTile;

        Vector2Int thirdTile =
            centerTile - sideDirection;

        tiles.Add(firstTile);
        tiles.Add(secondTile);
        tiles.Add(thirdTile);

        return tiles;
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

        if (!CanHit(
            gridManager,
            user,
            target))
        {
            Debug.LogWarning(
                $"[MeleeSwoosh] {target.name} is not inside the hitbox."
            );

            return false;
        }

        List<Vector2Int> hitboxTiles =
            GetHitboxTiles(
                gridManager,
                user,
                target
            );

        if (hitboxTiles == null ||
            hitboxTiles.Count == 0)
        {
            return false;
        }

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

        Debug.Log(
            $"[MeleeSwoosh] {user.name} attacked {target.name}."
        );

        return hitSomething;
    }

    // ==================================================
    // GET ATTACK DIRECTION
    // ==================================================

    private Vector2Int GetAttackDirection(
        Vector2Int userPosition,
        Vector2Int targetPosition)
    {
        Vector2Int difference =
            targetPosition - userPosition;

        if (Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y))
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        if (difference.y != 0)
        {
            return difference.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return Vector2Int.zero;
    }

    // ==================================================
    // GET SIDE DIRECTION
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

        if (targetHealth == null)
        {
            return false;
        }

        if (targetHealth.IsDead())
        {
            return false;
        }

        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        if (userHealth != null &&
            targetHealth.GetTeam() ==
            userHealth.GetTeam())
        {
            return false;
        }

        int damage =
            GetDamage();

        targetHealth.TakeDamage(damage);

        Debug.Log(
            $"[MeleeSwoosh] {user.name} hit {target.name} for {damage} damage."
        );

        return true;
    }
}