
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

        // Choose a random unoccupied tile instead of the supplied target.
        if (!TryGetRandomUnoccupiedTile(gridManager, out targetTile))
        {
            Debug.LogWarning(
                "[EggAbil] No unoccupied tile is available."
            );
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

        EggBehav eggBehaviour = egg.GetComponent<EggBehav>();

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
        if (user == null)
            return false;

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[EggAbil] GridManager not found."
            );
            return false;
        }

        return UseAtTile(
            user,
            gridManager,
            Vector2Int.zero
        );
    }

    private bool TryGetRandomUnoccupiedTile(
        GridManager gridManager,
        out Vector2Int selectedTile
    )
    {
        selectedTile = Vector2Int.zero;

        int minX = gridManager.GetMinX();
        int maxX = gridManager.GetMaxX();
        int minY = gridManager.GetMinY();
        int maxY = gridManager.GetMaxY();

        // Collect all currently valid tiles first.
        System.Collections.Generic.List<Vector2Int> availableTiles =
            new System.Collections.Generic.List<Vector2Int>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int tile = new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(tile))
                    continue;

                if (gridManager.IsWalkableObstacle(tile))
                    continue;

                if (gridManager.IsCellOccupied(tile))
                    continue;

                availableTiles.Add(tile);
            }
        }

        if (availableTiles.Count == 0)
            return false;

        selectedTile = availableTiles[
            Random.Range(0, availableTiles.Count)
        ];

        return true;
    }
}
