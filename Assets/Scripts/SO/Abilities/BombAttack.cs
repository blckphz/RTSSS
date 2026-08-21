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
    // USE AT TILE
    // ==================================================
    //
    // IMPORTANT:
    //
    // Bombs do NOT need a unit on the target tile.
    //
    // The clicked tile itself becomes the center of
    // the explosion.
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
                "[BombAttack] UseAtTile failed: user is NULL."
            );

            return false;
        }


        if (gridManager == null)
        {
            Debug.LogWarning(
                "[BombAttack] UseAtTile failed: GridManager is NULL."
            );

            return false;
        }


        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombAttack] UseAtTile failed: explosionPrefab is NULL."
            );

            return false;
        }


        if (!gridManager.IsInsideGrid(
                targetTile))
        {
            Debug.LogWarning(
                $"[BombAttack] UseAtTile failed: " +
                $"{targetTile} is outside the grid."
            );

            return false;
        }


        // --------------------------------------------------
        // RANGE
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
        // EXPLOSION POSITION
        // --------------------------------------------------

        Vector3 explosionPosition =
            gridManager.GridToWorldPosition(
                targetTile
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
                "[BombAttack] UseAtTile failed: " +
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
                $"[BombAttack] UseAtTile failed: " +
                $"explosion prefab '{explosionPrefab.name}' " +
                $"does not contain an ExplosionAttack component."
            );


            Object.Destroy(
                explosion
            );


            return false;
        }


        // --------------------------------------------------
        // CONFIGURE EXPLOSION
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
            $"[BombAttack] Bomb placed at tile " +
            $"{targetTile}. " +
            $"Radius={explosionRadius}, " +
            $"Delay={explosionDelay}"
        );


        return true;
    }


    // ==================================================
    // USE ON GAMEOBJECT
    // ==================================================
    //
    // Kept for compatibility with other systems that
    // may call Use(user, target).
    //
    // UIManager uses UseAtTile() for bombs.
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
        // TARGET TILE
        // --------------------------------------------------

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );


        // --------------------------------------------------
        // USE AS TILE TARGET
        // --------------------------------------------------

        return UseAtTile(
            user,
            gridManager,
            targetPosition
        );
    }
}