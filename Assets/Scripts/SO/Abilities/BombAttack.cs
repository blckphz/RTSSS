
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BombAttack", menuName = "Abilities/Bomb Attack")]
public class BombAttack : AbilitySO
{
    [Header("Projectile")]
    [SerializeField] private GameObject bombProjectilePrefab;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 8f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0)] private int explosionRadius = 3;
    [SerializeField, Min(0f)] private float explosionDelay = 1f;

    private ExplosionAttack cachedExplosionAttack;

    public int ExplosionRadius => GetExplosionRadius();

    private void OnEnable()
    {
        CacheExplosionAttack();
    }

    private void CacheExplosionAttack()
    {
        cachedExplosionAttack = null;

        if (explosionPrefab == null)
            return;

        cachedExplosionAttack =
            explosionPrefab.GetComponent<ExplosionAttack>();

        if (cachedExplosionAttack == null)
        {
            cachedExplosionAttack =
                explosionPrefab.GetComponentInChildren<ExplosionAttack>(true);
        }
    }

    private ExplosionAttack GetExplosionAttack()
    {
        if (cachedExplosionAttack == null)
            CacheExplosionAttack();

        return cachedExplosionAttack;
    }

    public GameObject GetExplosionPrefab()
    {
        return explosionPrefab;
    }

    public GameObject GetProjectilePrefab()
    {
        return bombProjectilePrefab;
    }

    public float GetExplosionDelay()
    {
        return explosionDelay;
    }

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }

    public int GetExplosionRadius()
    {
        ExplosionAttack explosion = GetExplosionAttack();

        return explosion != null
            ? Mathf.Max(0, explosion.GetRadius())
            : Mathf.Max(0, explosionRadius);
    }

    public AbilitySO.RangeShape GetExplosionShape()
    {
        ExplosionAttack explosion = GetExplosionAttack();

        return explosion != null
            ? explosion.GetExplosionShape()
            : AbilitySO.RangeShape.Diamond;
    }

    public int GetEffectiveExplosionRadius(GameObject user)
    {
        int radius = GetExplosionRadius();

        if (user != null && UpdateManager.Instance != null)
        {
            radius += UpdateManager.Instance.GetPlayerRingBonus(
                user,
                RingStat.ExplosionSize);
        }

        return Mathf.Max(0, radius);
    }

    // ============================================================
    // EXPLOSION PREVIEW
    // ============================================================

    public List<Vector2Int> GetExplosionTiles(
        GridManager gridManager,
        Vector2Int center,
        int radius)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (gridManager == null)
            return tiles;

        radius = Mathf.Max(0, radius);

        AbilitySO.RangeShape shape = GetExplosionShape();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int distance =
                    GetExplosionDistance(x, y, shape);

                if (!IsInsideExplosionShape(
                    x, y, distance, radius, shape))
                    continue;

                Vector2Int tile =
                    center + new Vector2Int(x, y);

                if (gridManager.IsInsideGrid(tile))
                    tiles.Add(tile);
            }
        }

        return tiles;
    }

    public List<Vector2Int> GetExplosionTiles(
        GridManager gridManager,
        Vector2Int center)
    {
        return GetExplosionTiles(
            gridManager,
            center,
            GetEffectiveExplosionRadius(null));
    }

    // ============================================================
    // SHARED SHAPE CALCULATIONS
    // ============================================================

    public static int GetExplosionDistance(
        int x,
        int y,
        AbilitySO.RangeShape shape)
    {
        if (shape == AbilitySO.RangeShape.Circle)
        {
            return Mathf.RoundToInt(
                Mathf.Sqrt(x * x + y * y));
        }

        return Mathf.Abs(x) + Mathf.Abs(y);
    }

    public static bool IsInsideExplosionShape(
        int x,
        int y,
        int distance,
        int radius,
        AbilitySO.RangeShape shape)
    {
        switch (shape)
        {
            case AbilitySO.RangeShape.Diamond:
                return Mathf.Abs(x) + Mathf.Abs(y) <= radius;

            case AbilitySO.RangeShape.Circle:
                return x * x + y * y <= radius * radius;

            case AbilitySO.RangeShape.Box:
                return Mathf.Abs(x) <= radius &&
                       Mathf.Abs(y) <= radius;

            case AbilitySO.RangeShape.FourDirections:
                return (x == 0 || y == 0) &&
                       distance <= radius;

            case AbilitySO.RangeShape.Diagonal:
                return Mathf.Abs(x) == Mathf.Abs(y) &&
                       distance <= radius;

            case AbilitySO.RangeShape.FourdirectionsAndDiragonal:
                return (x == 0 ||
                        y == 0 ||
                        Mathf.Abs(x) == Mathf.Abs(y)) &&
                       distance <= radius;

            case AbilitySO.RangeShape.Shotgun:
                return Mathf.Abs(x) + Mathf.Abs(y) <= radius;

            default:
                return distance <= radius;
        }
    }

    // ============================================================
    // USE ON OBJECT
    // ============================================================

    public override bool Use(GameObject user, GameObject target)
    {
        if (!base.Use(user, target))
            return false;

        if (target == null)
            return false;

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
            targetUnit = target.GetComponentInParent<AttackUnit>();

        AttackUnit userUnit = user.GetComponent<AttackUnit>();

        GridManager gridManager =
            userUnit != null
                ? userUnit.GetGridManager()
                : FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        Vector2Int targetTile = targetUnit != null
            ? targetUnit.GetLogicalGridPosition()
            : gridManager.WorldToGridPosition(
                target.transform.position);

        if (!CanHitTile(gridManager, user, targetTile))
            return false;

        return LaunchProjectile(user, gridManager, targetTile);
    }

    // ============================================================
    // USE ON TILE
    // ============================================================

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        if (!base.UseAtTile(user, gridManager, targetTile))
            return false;

        return LaunchProjectile(user, gridManager, targetTile);
    }

    // ============================================================
    // LAUNCH PROJECTILE
    // ============================================================

    private bool LaunchProjectile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        if (user == null || gridManager == null)
            return false;

        if (bombProjectilePrefab == null)
        {
            Debug.LogWarning(
                "BombAttack: Bomb projectile prefab is not assigned.");
            return false;
        }

        if (explosionPrefab == null)
        {
            Debug.LogWarning(
                "BombAttack: Explosion prefab is not assigned.");
            return false;
        }

        if (!gridManager.IsInsideGrid(targetTile))
            return false;

        Vector3 targetWorldPosition =
            gridManager.GridToWorldPosition(targetTile);

        GameObject projectileObject = Instantiate(
            bombProjectilePrefab,
            user.transform.position,
            Quaternion.identity);

        BombProjectile projectile =
            projectileObject.GetComponent<BombProjectile>();

        if (projectile == null)
        {
            projectile =
                projectileObject.GetComponentInChildren<BombProjectile>();
        }

        if (projectile == null)
        {
            Debug.LogError(
                "BombAttack: Projectile prefab has no BombProjectile component.",
                projectileObject);

            Destroy(projectileObject);
            return false;
        }

        int effectiveRadius =
            GetEffectiveExplosionRadius(user);

        int effectiveDamage =
            GetEffectiveDamage(user);

        AbilitySO.RangeShape shape =
            GetExplosionShape();

        projectile.Initialize(
            user,
            targetWorldPosition,
            gridManager,
            explosionPrefab,
            effectiveRadius,
            explosionDelay,
            projectileSpeed,
            effectiveDamage,
            shape);

        return true;
    }
}
