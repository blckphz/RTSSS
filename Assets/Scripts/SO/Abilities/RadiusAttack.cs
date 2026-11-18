
using UnityEngine;

[CreateAssetMenu(
    fileName = "RadiusAttack",
    menuName = "Combat/Abilities/RadiusAttack"
)]
public class RadiusAttack : AbilitySO
{
    [Header("Explosion Effect")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0)] private int explosionRadius = 1;
    [SerializeField] private bool useAbilityDamage = true;
    [SerializeField, Min(0)] private int explosionDamage = 10;

    [Header("Melee Attack Start")]
    [SerializeField] private GameObject attackStartPrefab;
    [SerializeField] private Vector3 attackStartOffset;
    [SerializeField] private float attackStartDuration = 2f;

    [Header("Sprite Direction")]
    [Tooltip("Sprite faces downward by default.")]
    [SerializeField] private float rotationOffset = 90f;

    // ==================================================
    // UNIT HELPERS
    // ==================================================

    private GameObject GetUnitRoot(GameObject obj)
    {
        if (obj == null)
            return null;

        AttackUnit unit = obj.GetComponent<AttackUnit>();

        if (unit == null)
            unit = obj.GetComponentInParent<AttackUnit>();

        if (unit == null)
            unit = obj.GetComponentInChildren<AttackUnit>();

        return unit != null ? unit.gameObject : obj;
    }

    private HealthManager GetHealthManager(GameObject obj)
    {
        if (obj == null)
            return null;

        HealthManager health = obj.GetComponent<HealthManager>();

        if (health == null)
            health = obj.GetComponentInParent<HealthManager>();

        if (health == null)
            health = obj.GetComponentInChildren<HealthManager>();

        return health;
    }

    // ==================================================
    // GRID DIRECTION TO WORLD DIRECTION
    // ==================================================

    private Vector2 ConvertDirectionToWorldDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return Vector2.zero;

        BoardViewController board =
            BoardViewController.Instance;

        if (board == null)
            return direction.normalized;

        float boardRotation =
            board.GetCurrentRotation();

        Vector3 logicalDirection = new Vector3(
            direction.x,
            direction.y,
            0f
        );

        Vector3 worldDirection =
            Quaternion.AngleAxis(
                boardRotation,
                Vector3.forward
            ) * logicalDirection;

        return new Vector2(
            worldDirection.x,
            worldDirection.y
        ).normalized;
    }

    // ==================================================
    // ATTACK START
    // ==================================================

    public void PlayAttackStart(GameObject user, GameObject target)
    {
        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        if (userRoot == null)
            return;

        if (targetRoot != null)
            RotateTowardsTarget(userRoot, targetRoot);

        PlayAttackStartEffect(userRoot);
    }

    private void RotateTowardsTarget(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return;

        // Find the target direction in world space.
        Vector2 worldDirection =
            target.transform.position - user.transform.position;

        if (worldDirection.sqrMagnitude < 0.001f)
            return;

        // Convert the world direction back to logical grid direction.
        // This prevents applying board rotation twice.
        BoardViewController board =
            BoardViewController.Instance;

        if (board != null)
        {
            float boardRotation =
                board.GetCurrentRotation();

            Vector3 logicalDirection =
                Quaternion.AngleAxis(
                    -boardRotation,
                    Vector3.forward
                ) * new Vector3(
                    worldDirection.x,
                    worldDirection.y,
                    0f
                );

            worldDirection = new Vector2(
                logicalDirection.x,
                logicalDirection.y
            );
        }

        // Convert the logical direction into the correct visual direction.
        Vector2 visualDirection =
            ConvertDirectionToWorldDirection(worldDirection);

        if (visualDirection.sqrMagnitude < 0.001f)
            return;

        float angle =
            Mathf.Atan2(
                visualDirection.y,
                visualDirection.x
            ) * Mathf.Rad2Deg;

        Transform spriteTransform = user.transform;

        SpriteRenderer sprite =
            user.GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            spriteTransform = sprite.transform;

        // Sprite faces DOWN by default.
        spriteTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle + rotationOffset
            );

        Debug.Log(
            $"[RadiusAttack] Rotating {user.name} toward {target.name}. " +
            $"Visual direction: {visualDirection}, " +
            $"Angle: {angle + rotationOffset}, " +
            $"Board rotation: " +
            (board != null ? board.GetCurrentRotation() : 0)
        );
    }

    private void PlayAttackStartEffect(GameObject user)
    {
        if (attackStartPrefab == null || user == null)
            return;

        GameObject effect = Object.Instantiate(
            attackStartPrefab,
            user.transform.position + attackStartOffset,
            Quaternion.identity
        );

        if (effect != null)
            Object.Destroy(effect, attackStartDuration);
    }

    // ==================================================
    // USE - CALLED AT IMPACT
    // ==================================================

    public override bool Use(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return false;

        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        if (userRoot == null || targetRoot == null)
            return false;

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

        if (!CanHit(gridManager, userRoot, targetRoot))
            return false;

        int damage = useAbilityDamage
            ? GetDamage()
            : explosionDamage;

        // ==================================================
        // EXPLOSION PREFAB
        // ==================================================

        if (explosionPrefab != null)
        {
            Vector3 explosionPosition =
                targetRoot.transform.position;

            GameObject explosion = Object.Instantiate(
                explosionPrefab,
                explosionPosition,
                Quaternion.identity
            );

            if (explosion == null)
                return false;

            ExplosionAttack explosionAttack =
                explosion.GetComponent<ExplosionAttack>();

            if (explosionAttack == null)
            {
                Object.Destroy(explosion);
                return false;
            }

            explosionAttack.SetExplosionRadius(explosionRadius);
            explosionAttack.SetExplosionDamage(damage);
            explosionAttack.Initialize(userRoot);

            return true;
        }

        // ==================================================
        // FALLBACK DAMAGE
        // ==================================================

        bool hitSomething = false;

        Vector2Int center =
            gridManager.WorldToGridPosition(
                targetRoot.transform.position
            );

        HealthManager userHealth =
            GetHealthManager(userRoot);

        for (int x = -explosionRadius; x <= explosionRadius; x++)
        {
            for (int y = -explosionRadius; y <= explosionRadius; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);

                if (distance > explosionRadius)
                    continue;

                Vector2Int position =
                    center + new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(position))
                    continue;

                GameObject unit =
                    gridManager.GetUnitAt(position);

                if (unit == null)
                    continue;

                GameObject unitRoot = GetUnitRoot(unit);

                if (unitRoot == null || unitRoot == userRoot)
                    continue;

                HealthManager health =
                    GetHealthManager(unitRoot);

                if (health == null || health.IsDead())
                    continue;

                if (userHealth != null &&
                    health.GetTeam() == userHealth.GetTeam())
                {
                    continue;
                }

                health.TakeDamage(damage);
                hitSomething = true;
            }
        }

        return hitSomething;
    }
}
