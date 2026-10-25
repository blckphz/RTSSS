
using UnityEngine;

[CreateAssetMenu(
    fileName = "EggAttack",
    menuName = "Combat/Abilities/EggAttack"
)]
public class EggAbil : AbilitySO
{
    [Header("Egg Projectile")]

    [SerializeField]
    private GameObject eggPrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;

    public GameObject GetEggPrefab()
    {
        return eggPrefab;
    }

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile
    )
    {
        if (user == null || gridManager == null)
        {
            Debug.LogWarning(
                "[EggAbil] User or GridManager is null."
            );
            return false;
        }

        if (eggPrefab == null)
        {
            Debug.LogError(
                "[EggAbil] eggPrefab is not assigned."
            );
            return false;
        }

        if (!CanHitTile(gridManager, user, targetTile))
        {
            return false;
        }

        Transform spawnPoint =
            user.transform.Find("AbilitySpawnPoint");

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : user.transform.position;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(targetTile);

        targetPosition.z = spawnPosition.z;

        GameObject egg = Instantiate(
            eggPrefab,
            spawnPosition,
            Quaternion.identity
        );

        EggBehav eggBehaviour =
            egg.GetComponent<EggBehav>();

        if (eggBehaviour == null)
        {
            Debug.LogError(
                "[EggAbil] Egg prefab needs an EggBehav component.",
                egg
            );

            Destroy(egg);
            return false;
        }

        eggBehaviour.Initialize(
            targetPosition,
            projectileSpeed
        );

        return true;
    }

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null || target == null)
        {
            return false;
        }

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[EggAbil] GridManager not found."
            );
            return false;
        }

        Vector2Int targetTile =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        return UseAtTile(
            user,
            gridManager,
            targetTile
        );
    }
}
