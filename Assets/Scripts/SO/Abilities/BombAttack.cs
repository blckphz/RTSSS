using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BombAttack",
    menuName = "Combat/Abilities/BombAttack"
)]
public class BombAttack : AbilitySO
{
    // ==================================================
    // BOMB PROJECTILE
    // ==================================================

    [Header("Bomb Projectile")]

    [Tooltip(
        "Prefab containing the bomb SpriteRenderer, " +
        "Rigidbody2D and BombProjectile component."
    )]
    [SerializeField]
    private GameObject bombPrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;


    // ==================================================
    // EXPLOSION
    // ==================================================

    [Header("Explosion")]

    [Tooltip(
        "Prefab spawned when the bomb reaches its target. " +
        "Must contain an ExplosionAttack component."
    )]
    [SerializeField]
    private GameObject explosionPrefab;

    [SerializeField, Min(0f)]
    private float explosionDelay = 0f;

    [SerializeField, Min(0)]
    private int explosionRadius = 1;


    // ==================================================
    // GETTERS
    // ==================================================

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


    // ==================================================
    // GET EXPLOSION TILES
    // ==================================================

    public List<Vector2Int> GetExplosionTiles(
        GridManager gridManager,
        Vector2Int centerPosition)
    {
        List<Vector2Int> tiles =
            new List<Vector2Int>();

        if (gridManager == null)
        {
            return tiles;
        }

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                if (
                    gridManager.GetDistance(
                        centerPosition,
                        position
                    ) <= explosionRadius
                )
                {
                    tiles.Add(position);
                }
            }
        }

        return tiles;
    }


    // ==================================================
    // USE AT TILE
    // ==================================================
    //
    // Bombs can target empty tiles.
    //
    // The bomb:
    //
    // 1. Spawns at AbilitySpawnPoint.
    // 2. Travels toward the selected tile.
    // 3. Travels in an arc using BombProjectile.
    // 4. Creates the explosion when it arrives.
    //

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        // --------------------------------------------------
        // VALIDATION
        // --------------------------------------------------

        if (user == null)
        {
            Debug.LogWarning(
                "[BombAttack] UseAtTile failed: " +
                "user is NULL."
            );

            return false;
        }

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[BombAttack] UseAtTile failed: " +
                "GridManager is NULL."
            );

            return false;
        }

        if (bombPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] UseAtTile failed: " +
                "bombPrefab is NULL."
            );

            return false;
        }

        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] UseAtTile failed: " +
                "explosionPrefab is NULL."
            );

            return false;
        }

        if (!gridManager.IsInsideGrid(targetTile))
        {
            Debug.LogWarning(
                $"[BombAttack] UseAtTile failed: " +
                $"target tile {targetTile} is outside the grid."
            );

            return false;
        }


        // --------------------------------------------------
        // CHECK RANGE
        // --------------------------------------------------

        if (!CanHitTile(
                gridManager,
                user,
                targetTile))
        {
            Debug.LogWarning(
                $"[BombAttack] UseAtTile failed: " +
                $"target tile {targetTile} is outside range."
            );

            return false;
        }


        // --------------------------------------------------
        // FIND ABILITY SPAWN POINT
        // --------------------------------------------------

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
            Debug.LogWarning(
                $"[BombAttack] AbilitySpawnPoint was not found " +
                $"inside '{user.name}'. " +
                $"Using unit position instead."
            );

            spawnPosition =
                user.transform.position;
        }


        // --------------------------------------------------
        // GET TARGET POSITION
        // --------------------------------------------------

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                targetTile
            );


        // --------------------------------------------------
        // KEEP PROJECTILE ON SAME DEPTH
        // --------------------------------------------------

        targetPosition.z =
            spawnPosition.z;


        // --------------------------------------------------
        // SPAWN BOMB
        // --------------------------------------------------

        GameObject bomb =
            Object.Instantiate(
                bombPrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (bomb == null)
        {
            Debug.LogError(
                "[BombAttack] Failed to instantiate " +
                "bomb prefab."
            );

            return false;
        }


        // --------------------------------------------------
        // GET BOMB PROJECTILE
        // --------------------------------------------------

        BombProjectile projectile =
            bomb.GetComponent<BombProjectile>();


        if (projectile == null)
        {
            Debug.LogError(
                $"[BombAttack] Bomb prefab " +
                $"'{bombPrefab.name}' does not contain " +
                "a BombProjectile component."
            );

            Object.Destroy(
                bomb
            );

            return false;
        }


        // --------------------------------------------------
        // INITIALIZE BOMB
        // --------------------------------------------------

        projectile.Initialize(
            user,
            targetPosition,
            gridManager,
            explosionPrefab,
            explosionRadius,
            explosionDelay,
            projectileSpeed
        );


        // --------------------------------------------------
        // SUCCESS
        // --------------------------------------------------

        Debug.Log(
            $"[BombAttack] Bomb launched from " +
            $"AbilitySpawnPoint of '{user.name}' " +
            $"toward tile {targetTile}."
        );


        return true;
    }


    // ==================================================
    // USE ON GAMEOBJECT
    // ==================================================
    //
    // Compatibility with systems that call:
    //
    //     Use(user, target)
    //
    // The target GameObject is converted into its grid
    // position and then handled by UseAtTile().
    //

    public override bool Use(
        GameObject user,
        GameObject target)
    {
        // --------------------------------------------------
        // VALIDATION
        // --------------------------------------------------

        if (user == null)
        {
            Debug.LogWarning(
                "[BombAttack] Use failed: " +
                "user is NULL."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[BombAttack] Use failed: " +
                "target is NULL."
            );

            return false;
        }

        if (bombPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: " +
                "bombPrefab is NULL."
            );

            return false;
        }

        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: " +
                "explosionPrefab is NULL."
            );

            return false;
        }


        // --------------------------------------------------
        // FIND GRID
        // --------------------------------------------------

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();


        if (gridManager == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: " +
                "GridManager not found."
            );

            return false;
        }


        // --------------------------------------------------
        // GET TARGET TILE
        // --------------------------------------------------

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        // --------------------------------------------------
        // USE AT TARGET TILE
        // --------------------------------------------------

        return UseAtTile(
            user,
            gridManager,
            targetPosition
        );
    }
}