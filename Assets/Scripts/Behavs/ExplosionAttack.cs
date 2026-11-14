
using UnityEngine;

public class ExplosionAttack : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField, Min(0)] private int radius = 3;
    [SerializeField, Min(0)] private int damage = 10;

    [Header("Explosion Shape")]
    [SerializeField]
    private AbilitySO.RangeShape explosionShape =
        AbilitySO.RangeShape.Diamond;

    [Header("Damage Falloff")]
    [SerializeField, Range(0f, 100f)]
    private float totalDamageFalloff = 80f;

    [Header("Friendly Fire")]
    [SerializeField] private bool allowFriendlyFire = false;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockback = true;

    [SerializeField, Min(0f)]
    private float knockbackStepDuration = 0.08f;

    [Header("Player Knockback")]
    [SerializeField] private bool knockbackPlayerWithoutDamage = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Behaviour")]
    [SerializeField] private bool explodeOnStart = true;
    [SerializeField] private bool destroyAfterExplosion = true;
    [SerializeField, Min(0f)] private float destroyDelay = 0.1f;
    [SerializeField, Min(0f)] private float explosionDelay = 0f;

    [Header("Tile Effects")]
    [SerializeField] private bool animateTiles = true;

    [Header("Flash")]
    [SerializeField] private bool flashExplosion = true;

    private GameObject owner;
    private bool exploded;
    private bool explosionScheduled;

    private GridManager gridManager;
    private GridHighlightManager highlightManager;
    private Animator cachedAnimator;
    private ParticleSystem cachedParticleSystem;

    private void Awake()
    {
        cachedAnimator = GetComponent<Animator>();
        cachedParticleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        FindManagers();

        if (explodeOnStart && !exploded && !explosionScheduled)
            ScheduleExplosion();
    }

    public void Initialize(GameObject explosionOwner)
    {
        owner = explosionOwner;
        FindManagers();

        if (explodeOnStart && !exploded && !explosionScheduled)
            ScheduleExplosion();
    }

    public void SetOwner(GameObject explosionOwner)
    {
        owner = explosionOwner;
    }

    public void SetExplosionDelay(float delay)
    {
        explosionDelay = Mathf.Max(0f, delay);
    }

    public void SetExplosionRadius(int newRadius)
    {
        radius = Mathf.Max(0, newRadius);
    }

    public void SetExplosionDamage(int newDamage)
    {
        damage = Mathf.Max(0, newDamage);
    }

    public void SetExplosionShape(AbilitySO.RangeShape newShape)
    {
        explosionShape = newShape;
    }

    public void SetTotalDamageFalloff(float newFalloff)
    {
        totalDamageFalloff = Mathf.Clamp(newFalloff, 0f, 100f);
    }

    public void SetAllowFriendlyFire(bool enabled)
    {
        allowFriendlyFire = enabled;
    }

    public void SetApplyKnockback(bool enabled)
    {
        applyKnockback = enabled;
    }

    public void SetKnockbackStepDuration(float duration)
    {
        knockbackStepDuration = Mathf.Max(0f, duration);
    }

    public void SetKnockbackPlayerWithoutDamage(bool enabled)
    {
        knockbackPlayerWithoutDamage = enabled;
    }

    // ============================================================
    // UPGRADE CHECK
    // ============================================================

    private bool IsKnockbackUnlocked()
    {
        if (!applyKnockback || owner == null)
            return false;

        UnitData unitData = owner.GetComponent<UnitData>();

        if (unitData == null)
            unitData = owner.GetComponentInParent<UnitData>();

        if (unitData == null)
            unitData = owner.GetComponentInChildren<UnitData>();

        if (unitData == null)
        {
            Debug.LogWarning(
                "[ExplosionAttack] Cannot check knockback upgrade: " +
                "owner has no UnitData component.",
                owner
            );

            return false;
        }

        bool unlocked = unitData.HasExplosionKnockbackUpgrade();

        Debug.Log(
            $"[ExplosionAttack] Knockback upgrade unlocked: {unlocked}",
            owner
        );

        return unlocked;
    }

    // ============================================================
    // SCHEDULE EXPLOSION
    // ============================================================

    private void ScheduleExplosion()
    {
        if (explosionScheduled || exploded)
            return;

        explosionScheduled = true;

        if (explosionDelay > 0f)
            Invoke(nameof(Explode), explosionDelay);
        else
            Explode();
    }

    // ============================================================
    // FIND MANAGERS
    // ============================================================

    private void FindManagers()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (highlightManager == null)
            highlightManager = FindFirstObjectByType<GridHighlightManager>();

        if (gridManager == null)
            Debug.LogError("[ExplosionAttack] No GridManager found.", this);
    }

    // ============================================================
    // EXPLODE
    // ============================================================

    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;
        explosionScheduled = false;

        FindManagers();

        if (gridManager == null)
        {
            DestroyExplosion();
            return;
        }

        Vector2Int center =
            gridManager.WorldToGridPosition(transform.position);

        HealthManager ownerHealth = owner != null
            ? owner.GetComponent<HealthManager>()
            : null;

        bool hasOwnerTeam = ownerHealth != null;

        Team ownerTeam = hasOwnerTeam
            ? ownerHealth.GetTeam()
            : (Team)(-1);

        // Knockback requires the owner's upgrade.
        bool knockbackUnlocked = IsKnockbackUnlocked();

        if (flashExplosion)
            PlayExplosionFlash();

        // Player knockback is handled separately.
        if (knockbackUnlocked)
            KnockbackPlayer(center);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);

                if (!IsInsideExplosionShape(x, y, distance))
                    continue;

                Vector2Int position =
                    center + new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(position))
                    continue;

                int tileDamage = CalculateDamageAtDistance(distance);

                AttackTile(
                    position,
                    hasOwnerTeam,
                    ownerTeam,
                    tileDamage,
                    center,
                    knockbackUnlocked
                );

                if (animateTiles && highlightManager != null)
                    highlightManager.FlashExplosionTile(position);
            }
        }

        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

    // ============================================================
    // PLAYER KNOCKBACK
    // ============================================================

    private void KnockbackPlayer(Vector2Int explosionCenter)
    {
        HealthManager[] healthManagers =
            FindObjectsByType<HealthManager>(
                FindObjectsSortMode.None
            );

        foreach (HealthManager health in healthManagers)
        {
            if (health == null ||
                !health.gameObject.activeInHierarchy ||
                !health.IsPlayerCharacter() ||
                health.IsDead())
            {
                continue;
            }

            GameObject player = health.gameObject;

            Vector2Int playerTile =
                gridManager.WorldToGridPosition(
                    player.transform.position
                );

            int dx = playerTile.x - explosionCenter.x;
            int dy = playerTile.y - explosionCenter.y;

            int tileDistance = Mathf.Abs(dx) + Mathf.Abs(dy);

            if (!IsInsideExplosionShape(dx, dy, tileDistance))
                continue;

            if (!gridManager.IsInsideGrid(playerTile))
                continue;

            UnitMoveBrain moveBrain =
                player.GetComponent<UnitMoveBrain>();

            if (moveBrain == null)
                moveBrain = player.GetComponentInParent<UnitMoveBrain>();

            if (moveBrain == null)
                moveBrain = player.GetComponentInChildren<UnitMoveBrain>();

            if (moveBrain == null)
                continue;

            int knockbackDistance =
                CalculateKnockbackDistance(tileDistance);

            if (knockbackDistance <= 0)
                continue;

            moveBrain.ApplyKnockback(
                explosionCenter,
                knockbackDistance,
                knockbackStepDuration
            );
        }
    }

    // ============================================================
    // EXPLOSION SHAPE
    // ============================================================

    private bool IsInsideExplosionShape(
        int x,
        int y,
        int distance)
    {
        switch (explosionShape)
        {
            case AbilitySO.RangeShape.Diamond:
                return distance <= radius;

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
                return (x == 0 || y == 0 ||
                        Mathf.Abs(x) == Mathf.Abs(y)) &&
                       distance <= radius;

            case AbilitySO.RangeShape.Shotgun:
                return distance <= radius;

            default:
                return distance <= radius;
        }
    }

    // ============================================================
    // DAMAGE FALLOFF
    // ============================================================

    private int CalculateDamageAtDistance(int distance)
    {
        if (radius <= 0)
            return damage;

        float normalizedDistance =
            Mathf.Clamp01((float)distance / radius);

        float multiplier =
            1f - normalizedDistance * (totalDamageFalloff / 100f);

        return Mathf.RoundToInt(
            damage * Mathf.Clamp01(multiplier)
        );
    }

    // ============================================================
    // DYNAMIC KNOCKBACK DISTANCE
    // ============================================================

    private int CalculateKnockbackDistance(int distanceFromCenter)
    {
        if (radius <= 0)
            return 1;

        if (distanceFromCenter > radius)
            return 0;

        float normalizedDistance =
            Mathf.Clamp01((float)distanceFromCenter / radius);

        // Center = full explosion radius.
        // Edge = 1 tile.
        int knockbackDistance = Mathf.RoundToInt(
            Mathf.Lerp(
                radius,
                1f,
                normalizedDistance
            )
        );

        return Mathf.Max(1, knockbackDistance);
    }

    // ============================================================
    // ATTACK TILE
    // ============================================================

    private void AttackTile(
        Vector2Int position,
        bool hasOwnerTeam,
        Team ownerTeam,
        int tileDamage,
        Vector2Int explosionCenter,
        bool knockbackUnlocked)
    {
        GameObject target = gridManager.GetUnitAt(position);

        if (target == null)
            return;

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        bool isPlayer =
            target.CompareTag(playerTag) ||
            (targetHealth != null &&
             targetHealth.IsPlayerCharacter());

        if (targetHealth != null && targetHealth.IsDead())
            return;

        if (!isPlayer && targetHealth == null)
            return;

        if (isPlayer && knockbackPlayerWithoutDamage)
            return;

        if (!isPlayer &&
            !allowFriendlyFire &&
            hasOwnerTeam &&
            targetHealth != null &&
            ownerTeam == targetHealth.GetTeam())
        {
            return;
        }

        int distanceFromCenter =
            Mathf.Abs(position.x - explosionCenter.x) +
            Mathf.Abs(position.y - explosionCenter.y);

        int knockbackDistance =
            CalculateKnockbackDistance(distanceFromCenter);

        // Enemy knockback requires the upgrade.
        if (knockbackUnlocked &&
            !isPlayer &&
            knockbackDistance > 0)
        {
            UnitMoveBrain moveBrain =
                target.GetComponent<UnitMoveBrain>();

            if (moveBrain == null)
                moveBrain = target.GetComponentInParent<UnitMoveBrain>();

            if (moveBrain == null)
                moveBrain = target.GetComponentInChildren<UnitMoveBrain>();

            if (moveBrain != null)
            {
                moveBrain.ApplyKnockback(
                    explosionCenter,
                    knockbackDistance,
                    knockbackStepDuration
                );
            }
        }

        if (targetHealth != null && tileDamage > 0)
            targetHealth.TakeDamage(tileDamage);
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void PlayExplosionFlash()
    {
        if (cachedAnimator != null)
            cachedAnimator.Play(0, 0, 0f);

        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Stop(true);
            cachedParticleSystem.Play(true);
        }
    }

    // ============================================================
    // ANIMATION EVENT
    // ============================================================

    public void AnimationEvent_DisableExplosion()
    {
        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void DestroyExplosion()
    {
        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

    // ============================================================
    // GETTERS
    // ============================================================

    public int GetRadius() => radius;
    public int GetDamage() => damage;
    public AbilitySO.RangeShape GetExplosionShape() => explosionShape;
    public float GetTotalDamageFalloff() => totalDamageFalloff;
    public GameObject GetOwner() => owner;
    public bool GetAllowFriendlyFire() => allowFriendlyFire;
    public bool HasExploded() => exploded;
}
