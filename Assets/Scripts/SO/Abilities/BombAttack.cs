using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BombAttack",
    menuName = "Combat/Abilities/BombAttack"
)]
public class BombAttack : AbilitySO
{
    [Header("Bomb")]
    [SerializeField]
    private GameObject explosionPrefab;

    [SerializeField, Min(0f)]
    private float explosionDelay = 0f;

    [Header("Explosion Influence")]
    [SerializeField, Min(0)]
    private int explosionRadius = 1;


    // ==================================================
    // GETTERS
    // ==================================================

    public GameObject GetExplosionPrefab()
    {
        return explosionPrefab;
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
    // Kept available for other systems that may want to
    // query the bomb's actual explosion area.
    //
    // This does NOT display anything.
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
                    new Vector2Int(x, y);

                if (gridManager.GetDistance(
                        centerPosition,
                        position
                    ) <= explosionRadius)
                {
                    tiles.Add(position);
                }
            }
        }

        return tiles;
    }


    // ==================================================
    // USE ABILITY
    // ==================================================

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
                "[BombAttack] Use failed: user is NULL."
            );

            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "[BombAttack] Use failed: target is NULL."
            );

            return false;
        }

        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: explosionPrefab is NULL."
            );

            return false;
        }


        // --------------------------------------------------
        // GRID
        // --------------------------------------------------

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: GridManager not found."
            );

            return false;
        }


        // --------------------------------------------------
        // RANGE CHECK
        // --------------------------------------------------

        if (!CanHit(
                gridManager,
                user,
                target
            ))
        {
            Debug.LogWarning(
                $"[BombAttack] Use failed: " +
                $"target '{target.name}' is outside range."
            );

            return false;
        }


        // --------------------------------------------------
        // TARGET TILE
        // --------------------------------------------------

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

        if (!gridManager.IsInsideGrid(
                targetPosition
            ))
        {
            Debug.LogWarning(
                $"[BombAttack] Use failed: " +
                $"target tile {targetPosition} is outside grid."
            );

            return false;
        }


        // --------------------------------------------------
        // EXPLOSION POSITION
        // --------------------------------------------------

        Vector3 explosionPosition =
            gridManager.GridToWorldPosition(
                targetPosition
            );

        explosionPosition.z =
            user.transform.position.z;


        // --------------------------------------------------
        // SPAWN EXPLOSION
        // --------------------------------------------------

        GameObject explosion =
            Object.Instantiate(
                explosionPrefab,
                explosionPosition,
                Quaternion.identity
            );

        if (explosion == null)
        {
            Debug.LogError(
                "[BombAttack] Use failed: " +
                "Instantiate returned NULL."
            );

            return false;
        }


        // --------------------------------------------------
        // GET EXPLOSION COMPONENT
        // --------------------------------------------------

        ExplosionAttack explosionAttack =
            explosion.GetComponent<ExplosionAttack>();

        if (explosionAttack == null)
        {
            Debug.LogError(
                $"[BombAttack] Use failed: " +
                $"explosion prefab '{explosionPrefab.name}' " +
                $"does not contain an ExplosionAttack component."
            );

            Object.Destroy(explosion);

            return false;
        }


        // --------------------------------------------------
        // IMPORTANT:
        // SET EVERYTHING BEFORE INITIALIZE
        //
        // Initialize() may immediately call Explode().
        // --------------------------------------------------

        explosionAttack.SetExplosionRadius(
            explosionRadius
        );

        explosionAttack.SetExplosionDelay(
            explosionDelay
        );


        // --------------------------------------------------
        // INITIALIZE
        // --------------------------------------------------

        explosionAttack.Initialize(
            user
        );


        // --------------------------------------------------
        // SUCCESS
        // --------------------------------------------------

        Debug.Log(
            $"[BombAttack] Bomb spawned successfully. " +
            $"User={user.name}, " +
            $"Target={target.name}, " +
            $"Tile={targetPosition}, " +
            $"Radius={explosionRadius}, " +
            $"Delay={explosionDelay}"
        );

        return true;
    }
}