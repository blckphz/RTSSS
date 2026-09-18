using UnityEngine;

[CreateAssetMenu(
    fileName = "ArrowAttack",
    menuName = "Combat/Abilities/Arrow Attack"
)]
public class ArrowAttack : AbilitySO
{
    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 12f;
    [SerializeField] private int maxTargets = 99;

    [Header("Spawn Point")]
    [SerializeField]
    private string abilitySpawnPointName = "AbilitySpawnPoint";

    public override bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null || target == null)
            return false;

        if (arrowPrefab == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
            return false;

        if (targetUnit.IsDead())
            return false;

        if (!CanTargetObject(user, target))
            return false;

        // Find spawn point
        Transform spawnPoint =
            FindAbilitySpawnPoint(user);

        Vector3 spawnPosition =
            spawnPoint != null
                ? spawnPoint.position
                : user.transform.position;

        // Calculate direction
        Vector3 direction =
            target.transform.position - spawnPosition;

        direction.z = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();

        // Spawn with no rotation.
        // ArrowProjectile will handle the visual rotation.
        GameObject projectile =
            Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (projectile == null)
            return false;

        ArrowProjectile arrow =
            projectile.GetComponent<ArrowProjectile>();

        if (arrow == null)
        {
            Destroy(projectile);
            return false;
        }

        arrow.Initialize(
            user,
            direction,
            arrowSpeed,
            GetDamage(),
            maxTargets,
            this
        );

        return true;
    }

    private Transform FindAbilitySpawnPoint(
        GameObject user)
    {
        if (user == null)
            return null;

        Transform[] children =
            user.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (child.name == abilitySpawnPointName)
                return child;
        }

        return null;
    }
}