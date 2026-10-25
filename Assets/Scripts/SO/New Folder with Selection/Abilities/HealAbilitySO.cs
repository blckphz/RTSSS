using UnityEngine;

[CreateAssetMenu(
    fileName = "HealAbility",
    menuName = "Combat/Abilities/Heal Ability"
)]
public class HealAbilitySO : AbilitySO
{
    [Header("Healing")]
    [SerializeField, Min(0)]
    private int healAmount = 10;


    // ==================================================
    // GETTER
    // ==================================================

    public int GetHealAmount()
    {
        return healAmount;
    }


    // ==================================================
    // CAN HIT
    // ==================================================

    public override bool CanHit(
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


        if (targetUnit.IsDead())
        {
            return false;
        }


        if (
            !IsFriendlyTarget(
                userUnit,
                targetUnit
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


    // ==================================================
    // CAN HIT TILE
    // ==================================================

    public override bool CanHitTile(
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


        if (
            !gridManager.IsInsideGrid(
                targetPosition
            )
        )
        {
            return false;
        }


        GameObject target =
            gridManager.GetUnitAt(
                targetPosition
            );


        if (target == null)
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


        if (targetUnit.IsDead())
        {
            return false;
        }


        if (
            !IsFriendlyTarget(
                userUnit,
                targetUnit
            )
        )
        {
            return false;
        }


        return GetRangeTiles(
            gridManager,
            user
        ).Contains(
            targetPosition
        );
    }


    // ==================================================
    // FRIENDLY TARGET
    // ==================================================

    private bool IsFriendlyTarget(
        AttackUnit healer,
        AttackUnit target
    )
    {
        if (
            healer == null ||
            target == null
        )
        {
            return false;
        }


        Team healerTeam =
            healer.GetTeam();


        Team targetTeam =
            target.GetTeam();


        if (
            healerTeam ==
            Team.Enemy
        )
        {
            return targetTeam ==
                Team.Enemy;
        }


        if (
            healerTeam == Team.Player ||
            healerTeam == Team.Ally
        )
        {
            return
                targetTeam == Team.Player ||
                targetTeam == Team.Ally;
        }


        return healerTeam ==
            targetTeam;
    }


    // ==================================================
    // USE
    // ==================================================

    public override bool Use(
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


        AttackUnit healer =
            user.GetComponent<AttackUnit>();


        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();


        if (
            healer == null ||
            targetUnit == null
        )
        {
            return false;
        }


        if (targetUnit.IsDead())
        {
            return false;
        }


        if (
            !IsFriendlyTarget(
                healer,
                targetUnit
            )
        )
        {
            return false;
        }


        HealthManager health =
            target.GetComponent<HealthManager>();


        if (health == null)
        {
            return false;
        }


        if (health.IsDead())
        {
            return false;
        }


        health.Heal(
            healAmount
        );


        return true;
    }


    // ==================================================
    // USE AT TILE
    // ==================================================

    public override bool UseAtTile(
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


        if (
            !gridManager.IsInsideGrid(
                targetTile
            )
        )
        {
            return false;
        }


        GameObject target =
            gridManager.GetUnitAt(
                targetTile
            );


        if (target == null)
        {
            return false;
        }


        if (
            !CanHit(
                gridManager,
                user,
                target
            )
        )
        {
            return false;
        }


        return Use(
            user,
            target
        );
    }
}