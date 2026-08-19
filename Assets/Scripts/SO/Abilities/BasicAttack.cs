using UnityEngine;

[CreateAssetMenu(
    fileName = "BasicAttack",
    menuName = "Combat/Abilities/Basic Attack"
)]
public class BasicAttack : AbilitySO
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
                "[BasicAttack] User is null."
            );

            return false;
        }

        // ----------------------------------------------
        // TARGET
        // ----------------------------------------------

        if (target == null)
        {
            Debug.LogWarning(
                "[BasicAttack] Target is null."
            );

            return false;
        }

        // ----------------------------------------------
        // HEALTH
        // ----------------------------------------------

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            Debug.LogWarning(
                $"[BasicAttack] {target.name} does not " +
                "have a HealthManager."
            );

            return false;
        }

        // ----------------------------------------------
        // TARGET ALIVE
        // ----------------------------------------------

        if (targetHealth.IsDead())
        {
            Debug.LogWarning(
                $"[BasicAttack] {target.name} is already dead."
            );

            return false;
        }

        // ----------------------------------------------
        // DAMAGE
        // ----------------------------------------------

        int damage =
            GetDamage();

        Debug.Log(
            $"[BasicAttack] {user.name} attacks " +
            $"{target.name} for {damage} damage."
        );

        targetHealth.TakeDamage(damage);

        // ----------------------------------------------
        // SUCCESS
        // ----------------------------------------------

        return true;
    }
}