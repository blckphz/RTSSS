using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BombAttack",
    menuName = "Combat/Abilities/BombAttack"
)]
public class BombAttack : AbilitySO
{
    [Header("Bomb Projectile")]

    [SerializeField]
    private GameObject bombPrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;


    [Header("Explosion")]

    [SerializeField]
    private GameObject explosionPrefab;

    [SerializeField, Min(0f)]
    private float explosionDelay = 0f;

    [SerializeField, Min(0)]
    private int explosionRadius = 1;


    // ============================================================
    // GETTERS
    // ============================================================

    public GameObject GetBombPrefab()
    {
        return bombPrefab;
    }

    public GameObject GetExplosionPrefab()
    {
        return explosionPrefab;
    }

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }

    public float GetExplosionDelay()
    {
        return explosionDelay;
    }

    public int GetExplosionRadius()
    {
        return explosionRadius;
    }


    // ============================================================
    // EXPLOSION TILES
    // ============================================================

    public List<Vector2Int> GetExplosionTiles(
        GridManager gridManager,
        Vector2Int centerPosition
    )
    {
        List<Vector2Int> tiles =
            new List<Vector2Int>();

        if (gridManager == null)
        {
            return tiles;
        }

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
                    centerPosition +
                    new Vector2Int(x, y);

                if (
                    gridManager.IsInsideGrid(
                        position
                    )
                )
                {
                    tiles.Add(position);
                }
            }
        }

        return tiles;
    }


    // ============================================================
    // USE AT TILE
    // ============================================================

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile
    )
    {
        if (user == null)
        {
            Debug.LogWarning(
                "[BombAttack] User is NULL."
            );

            return false;
        }

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[BombAttack] GridManager is NULL."
            );

            return false;
        }

        if (bombPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] bombPrefab is NULL."
            );

            return false;
        }

        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] explosionPrefab is NULL."
            );

            return false;
        }

        if (
            !CanHitTile(
                gridManager,
                user,
                targetTile
            )
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // FIND SPAWN POINT
        // --------------------------------------------------------

        Transform spawnPoint =
            user.transform.Find(
                "AbilitySpawnPoint"
            );


        Vector3 spawnPosition;

        if (spawnPoint != null)
        {
            spawnPosition =
                spawnPoint.position;
        }
        else
        {
            spawnPosition =
                user.transform.position;
        }


        // --------------------------------------------------------
        // TARGET POSITION
        // --------------------------------------------------------

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                targetTile
            );

        targetPosition.z =
            spawnPosition.z;


        // --------------------------------------------------------
        // CALCULATE EFFECTIVE DAMAGE
        // --------------------------------------------------------

        int effectiveDamage =
            GetEffectiveDamage(
                user
            );


        int baseDamage =
            GetDamage();


        int bonusDamage =
            effectiveDamage -
            baseDamage;


        Debug.Log(
            $"[BombAttack] FIRING | " +
            $"User={user.name} | " +
            $"BaseDamage={baseDamage} | " +
            $"BonusDamage={bonusDamage} | " +
            $"EffectiveDamage={effectiveDamage}",
            user
        );


        // --------------------------------------------------------
        // CREATE BOMB
        // --------------------------------------------------------

        GameObject bomb =
            Instantiate(
                bombPrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (bomb == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // GET PROJECTILE
        // --------------------------------------------------------

        BombProjectile projectile =
            bomb.GetComponent<BombProjectile>();


        if (projectile == null)
        {
            Debug.LogError(
                $"[BombAttack] Bomb prefab '{bombPrefab.name}' " +
                $"does not contain BombProjectile.",
                bomb
            );

            Destroy(bomb);

            return false;
        }


        // --------------------------------------------------------
        // INITIALIZE PROJECTILE
        // --------------------------------------------------------

        projectile.Initialize(
            user,
            targetPosition,
            gridManager,
            explosionPrefab,
            explosionRadius,
            explosionDelay,
            projectileSpeed,
            effectiveDamage
        );


        return true;
    }


    // ============================================================
    // USE ON TARGET
    // ============================================================

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


        GridManager gridManager =
            FindFirstObjectByType<GridManager>();


        if (gridManager == null)
        {
            Debug.LogError(
                "[BombAttack] GridManager not found."
            );

            return false;
        }


        Vector2Int targetTile =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        return
            UseAtTile(
                user,
                gridManager,
                targetTile
            );
    }
}