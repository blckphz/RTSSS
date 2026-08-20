using UnityEngine;

[CreateAssetMenu(
    fileName = "RadiusAttack",
    menuName = "Combat/Abilities/RadiusAttack"
)]
public class RadiusAttack : AbilitySO
{
    [Header("Explosion Effect")]
    [SerializeField]
    private GameObject explosionPrefab;

    [SerializeField, Min(0)]
    private int explosionRadius = 1;

    [SerializeField]
    private bool useAbilityDamage = true;

    [SerializeField, Min(0)]
    private int explosionDamage = 10;

    // ==================================================
    // USE ABILITY
    // ==================================================

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null)
        {
            Debug.LogWarning(
                "[RadiusAttack] User is null."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[RadiusAttack] Target is null."
            );

            return false;
        }

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[RadiusAttack] GridManager not found."
            );

            return false;
        }

        if (!CanHit(
                gridManager,
                user,
                target))
        {
            return false;
        }

        int damage =
            useAbilityDamage
                ? GetDamage()
                : explosionDamage;

        // ----------------------------------------------
        // SPAWN EXPLOSION
        // ----------------------------------------------

        if (explosionPrefab != null)
        {
            Vector3 explosionPosition =
                target.transform.position;

            GameObject explosion =
                Object.Instantiate(
                    explosionPrefab,
                    explosionPosition,
                    Quaternion.identity
                );

            if (explosion == null)
            {
                return false;
            }

            ExplosionAttack explosionAttack =
                explosion.GetComponent<ExplosionAttack>();

            if (explosionAttack == null)
            {
                Debug.LogWarning(
                    "[RadiusAttack] Explosion prefab " +
                    "does not contain ExplosionAttack."
                );

                Object.Destroy(
                    explosion
                );

                return false;
            }

            explosionAttack.SetExplosionRadius(
                explosionRadius
            );

            explosionAttack.SetExplosionDamage(
                damage
            );

            explosionAttack.Initialize(
                user
            );

            return true;
        }

        // ----------------------------------------------
        // FALLBACK
        // ----------------------------------------------
        // If there is no explosion prefab,
        // still perform direct radius damage.

        bool hitSomething =
            false;

        Vector2Int center =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        HealthManager userHealth =
            user.GetComponent<HealthManager>();

        for (
            int x = -explosionRadius;
            x <= explosionRadius;
            x++
        )
        {
            for (
                int y = -explosionRadius;
                y <= explosionRadius;
                y++
            )
            {
                int distance =
                    Mathf.Abs(x) +
                    Mathf.Abs(y);

                if (distance > explosionRadius)
                {
                    continue;
                }

                Vector2Int position =
                    center +
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(
                        position))
                {
                    continue;
                }

                GameObject unit =
                    gridManager.GetUnitAt(
                        position
                    );

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

                health.TakeDamage(
                    damage
                );

                hitSomething = true;
            }
        }

        return hitSomething;
    }
}