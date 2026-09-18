using UnityEngine;

[CreateAssetMenu(
    fileName = "ArrowAbility",
    menuName = "Combat/Abilities/Arrow"
)]
public class ArrowAttack : AbilitySO
{
    [Header("Arrow")]

    [SerializeField]
    private GameObject arrowPrefab;

    [SerializeField, Min(0.01f)]
    private float arrowSpeed = 15f;

    public int Pierce;

    [SerializeField, Min(1)]
    private int maxTargets = 99;

    [SerializeField]
    private string abilitySpawnPointName = "AbilitySpawnPoint";


    // ============================================================
    // USE
    // ============================================================

    public override bool Use(
        GameObject user,
        GameObject target)
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


        if (!CanTargetObject(user, target))
        {
            return false;
        }


        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();


        if (gridManager == null)
        {
            return false;
        }


        Vector2Int targetTile =
            gridManager.GetUnitGridPosition(
                target
            );


        if (!CanHitTile(
            gridManager,
            user,
            targetTile))
        {
            return false;
        }


        if (arrowPrefab == null)
        {
            return false;
        }


        // ========================================================
        // SPAWN POINT
        // ========================================================

        Transform spawnPoint =
            FindAbilitySpawnPoint(user);


        Vector3 spawnPosition =
            spawnPoint != null
                ? spawnPoint.position
                : user.transform.position;


        // ========================================================
        // DIRECTION
        // ========================================================

        Vector3 targetPosition =
            target.transform.position;


        Vector3 direction =
            targetPosition - spawnPosition;


        direction.y = 0f;


        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }


        direction.Normalize();


        // ========================================================
        // SPAWN ARROW
        // ========================================================

        GameObject arrow =
            Object.Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (arrow == null)
        {
            return false;
        }


        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();


        if (projectile == null)
        {
            Object.Destroy(arrow);
            return false;
        }


        projectile.Initialize(
            user,
            direction,
            arrowSpeed,
            GetDamage(),
            maxTargets
        );


        return true;
    }


    // ============================================================
    // SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(
        GameObject user)
    {
        if (user == null)
        {
            return null;
        }


        Transform[] children =
            user.GetComponentsInChildren<Transform>(
                true
            );


        for (
            int i = 0;
            i < children.Length;
            i++
        )
        {
            Transform child =
                children[i];


            if (child == null)
            {
                continue;
            }


            if (
                child.name ==
                abilitySpawnPointName
            )
            {
                return child;
            }
        }


        return null;
    }
}