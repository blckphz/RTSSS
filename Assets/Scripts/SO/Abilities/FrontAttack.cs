using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FrontAttack",
    menuName = "Combat/Abilities/FrontAttack"
)]
public class FrontAttack : AbilitySO
{
    public override List<Vector2Int> GetRangeTiles(
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

        Vector2Int userPosition =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        int abilityRange =
            Mathf.Max(
                1,
                GetRange()
            );

        int minimumDistance =
            Mathf.Clamp(
                GetMinDistance(),
                0,
                abilityRange
            );

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
                    userPosition +
                    new Vector2Int(x, y);

                AddValidTile(
                    gridManager,
                    tiles,
                    position
                );
            }
        }

        return tiles;
    }

    public override bool CanHit(
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

        int differenceX =
            Mathf.Abs(
                targetPosition.x -
                userPosition.x
            );

        int differenceY =
            Mathf.Abs(
                targetPosition.y -
                userPosition.y
            );

        int distance =
            Mathf.Max(
                differenceX,
                differenceY
            );

        int minimumDistance =
            Mathf.Clamp(
                GetMinDistance(),
                0,
                GetRange()
            );

        int maximumDistance =
            GetRange();

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

        return rangeTiles.Contains(
            targetPosition
        );
    }

    public override List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null
    )
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

        int abilityRange =
            Mathf.Max(
                1,
                GetRange()
            );

        Vector2Int direction =
            Vector2Int.zero;

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

        if (direction == Vector2Int.zero)
        {
            direction =
                DetectNearbyEnemyDirection(
                    gridManager,
                    user,
                    userPosition
                );
        }

        if (direction == Vector2Int.zero)
        {
            direction =
                GetUserFacingDirection(
                    user
                );
        }

        Vector2Int sideDirection =
            GetSideDirection(
                direction
            );

        for (
            int distance = 1;
            distance <= abilityRange;
            distance++
        )
        {
            Vector2Int center =
                userPosition +
                direction * distance;

            Vector2Int left =
                center -
                sideDirection;

            Vector2Int middle =
                center;

            Vector2Int right =
                center +
                sideDirection;

            AddValidTile(
                gridManager,
                tiles,
                left
            );

            AddValidTile(
                gridManager,
                tiles,
                middle
            );

            AddValidTile(
                gridManager,
                tiles,
                right
            );
        }

        return tiles;
    }

    private Vector2Int GetAttackDirection(
        Vector2Int userPosition,
        Vector2Int targetPosition
    )
    {
        Vector2Int difference =
            targetPosition -
            userPosition;

        if (difference == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }

        if (
            Mathf.Abs(difference.x) >
            Mathf.Abs(difference.y)
        )
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        if (
            Mathf.Abs(difference.y) >
            Mathf.Abs(difference.x)
        )
        {
            return difference.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        if (difference.x != 0)
        {
            return difference.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        return difference.y > 0
            ? Vector2Int.up
            : Vector2Int.down;
    }

    private Vector2Int DetectNearbyEnemyDirection(
        GridManager gridManager,
        GameObject user,
        Vector2Int userPosition
    )
    {
        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        GameObject closestEnemy =
            null;

        Vector2Int closestEnemyPosition =
            Vector2Int.zero;

        int closestDistance =
            int.MaxValue;

        for (
            int x = -1;
            x <= 1;
            x++
        )
        {
            for (
                int y = -1;
                y <= 1;
                y++
            )
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector2Int position =
                    userPosition +
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(position))
                {
                    continue;
                }

                GameObject unit =
                    gridManager.GetUnitAt(position);

                if (unit == null ||
                    unit == user)
                {
                    continue;
                }

                HealthManager health =
                    unit.GetComponent<HealthManager>();

                if (health == null ||
                    health.IsDead())
                {
                    continue;
                }

                if (
                    userHealth != null &&
                    health.GetTeam() ==
                    userHealth.GetTeam()
                )
                {
                    continue;
                }

                int distance =
                    Mathf.Abs(x) +
                    Mathf.Abs(y);

                if (distance < closestDistance)
                {
                    closestDistance =
                        distance;

                    closestEnemy =
                        unit;

                    closestEnemyPosition =
                        position;
                }
            }
        }

        if (closestEnemy == null)
        {
            return Vector2Int.zero;
        }

        return GetAttackDirection(
            userPosition,
            closestEnemyPosition
        );
    }

    private Vector2Int GetUserFacingDirection(
        GameObject user
    )
    {
        Vector3 forward =
            user.transform.up;

        if (
            Mathf.Abs(forward.x) >
            Mathf.Abs(forward.y)
        )
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

    private Vector2Int GetSideDirection(
        Vector2Int direction
    )
    {
        if (
            direction == Vector2Int.up ||
            direction == Vector2Int.down
        )
        {
            return Vector2Int.right;
        }

        return Vector2Int.up;
    }

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null ||
            target == null)
        {
            return false;
        }

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            return false;
        }

        if (!CanHit(
                gridManager,
                user,
                target
            ))
        {
            return false;
        }

        List<Vector2Int> hitbox =
            GetHitboxTiles(
                gridManager,
                user,
                target
            );

        if (
            hitbox == null ||
            hitbox.Count == 0
        )
        {
            return false;
        }

        bool hitSomething =
            false;

        foreach (
            Vector2Int position in hitbox
        )
        {
            if (
                AttackTile(
                    gridManager,
                    position,
                    user
                )
            )
            {
                hitSomething = true;
            }
        }

        return hitSomething;
    }

    private bool AttackTile(
        GridManager gridManager,
        Vector2Int position,
        GameObject user
    )
    {
        if (!gridManager.IsInsideGrid(position))
        {
            return false;
        }

        GameObject target =
            gridManager.GetUnitAt(position);

        if (
            target == null ||
            target == user
        )
        {
            return false;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (
            targetHealth == null ||
            targetHealth.IsDead()
        )
        {
            return false;
        }

        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        if (
            userHealth != null &&
            targetHealth.GetTeam() ==
            userHealth.GetTeam()
        )
        {
            return false;
        }

        int damage =
            GetDamage();

        targetHealth.TakeDamage(damage);

        return true;
    }
}