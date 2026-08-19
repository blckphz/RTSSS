using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeSwoosh",
    menuName = "Combat/Abilities/Melee Swoosh"
)]
public class MeleeSwoosh : AbilitySO
{
    // ==================================================
    // USE ABILITY
    // ==================================================

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        // ----------------------------------------------
        // USER
        // ----------------------------------------------

        if (user == null)
        {
            Debug.LogWarning(
                "[MeleeSwoosh] User is null."
            );

            return false;
        }

        // ----------------------------------------------
        // TARGET
        // ----------------------------------------------

        if (target == null)
        {
            Debug.LogWarning(
                "[MeleeSwoosh] Target is null."
            );

            return false;
        }

        // ----------------------------------------------
        // GRID MANAGER
        // ----------------------------------------------

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[MeleeSwoosh] GridManager not found."
            );

            return false;
        }

        // ----------------------------------------------
        // GRID POSITIONS
        // ----------------------------------------------

        Vector2Int userPosition =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        // ----------------------------------------------
        // RANGE
        // ----------------------------------------------

        int distance =
            gridManager.GetDistance(
                userPosition,
                targetPosition
            );

        if (distance > GetRange())
        {
            Debug.LogWarning(
                $"[MeleeSwoosh] Target {target.name} is " +
                $"out of range. Distance: {distance}, " +
                $"Range: {GetRange()}."
            );

            return false;
        }

        // ----------------------------------------------
        // ATTACK DIRECTION
        // ----------------------------------------------

        Vector2Int direction =
            GetAttackDirection(
                userPosition,
                targetPosition
            );

        // ----------------------------------------------
        // CENTER TILE
        // ----------------------------------------------

        Vector2Int centerTile =
            userPosition + direction;

        // ----------------------------------------------
        // TARGET MUST BE CENTER
        // ----------------------------------------------

        if (centerTile != targetPosition)
        {
            Debug.LogWarning(
                $"[MeleeSwoosh] {target.name} is not on " +
                "the center tile of the attack."
            );

            return false;
        }

        // ----------------------------------------------
        // SIDE DIRECTION
        // ----------------------------------------------

        Vector2Int sideDirection =
            GetSideDirection(direction);

        Vector2Int firstTile =
            centerTile + sideDirection;

        Vector2Int secondTile =
            centerTile;

        Vector2Int thirdTile =
            centerTile - sideDirection;

        // ----------------------------------------------
        // ATTACK TILES
        // ----------------------------------------------

        AttackTile(
            gridManager,
            firstTile,
            user
        );

        AttackTile(
            gridManager,
            secondTile,
            user
        );

        AttackTile(
            gridManager,
            thirdTile,
            user
        );

        // ----------------------------------------------
        // SUCCESS
        // ----------------------------------------------

        Debug.Log(
            $"[MeleeSwoosh] {user.name} used " +
            $"Melee Swoosh facing {direction}."
        );

        return true;
    }

    // ==================================================
    // GET ATTACK DIRECTION
    // ==================================================

    private Vector2Int GetAttackDirection(
        Vector2Int userPosition,
        Vector2Int targetPosition
    )
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

    // ==================================================
    // ATTACK TILE
    // ==================================================

    private void AttackTile(
        GridManager gridManager,
        Vector2Int position,
        GameObject user
    )
    {
        // ----------------------------------------------
        // GRID CHECK
        // ----------------------------------------------

        if (!gridManager.IsInsideGrid(position))
        {
            return;
        }

        // ----------------------------------------------
        // GET UNIT
        // ----------------------------------------------

        GameObject target =
            gridManager.GetUnitAt(position);

        if (target == null)
        {
            return;
        }

        // ----------------------------------------------
        // DON'T HIT USER
        // ----------------------------------------------

        if (target == user)
        {
            return;
        }

        // ----------------------------------------------
        // HEALTH
        // ----------------------------------------------

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            Debug.LogWarning(
                $"[MeleeSwoosh] {target.name} does not " +
                "have a HealthManager."
            );

            return;
        }

        // ----------------------------------------------
        // DON'T HIT DEAD TARGET
        // ----------------------------------------------

        if (targetHealth.IsDead())
        {
            Debug.Log(
                $"[MeleeSwoosh] {target.name} is already dead. " +
                "Skipping."
            );

            return;
        }

        // ----------------------------------------------
        // DAMAGE
        // ----------------------------------------------

        int damage =
            GetDamage();

        Debug.Log(
            $"[MeleeSwoosh] {user.name} hits " +
            $"{target.name} at {position} " +
            $"for {damage} damage."
        );

        targetHealth.TakeDamage(damage);
    }
}