
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
    [SerializeField, Min(0)] private int maxKnockbackDistance = 3;
    [SerializeField, Min(0)] private int minKnockbackDistance = 1;
    [SerializeField, Min(0f)] private float knockbackStepDuration = 0.08f;

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

        if (explodeOnStart && !exploded)
            ScheduleExplosion();
    }

    public void Initialize(GameObject explosionOwner)
    {
        owner = explosionOwner;
        FindManagers();

        if (explodeOnStart && !exploded)
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

    public void SetMaxKnockbackDistance(int distance)
    {
        maxKnockbackDistance = Mathf.Max(0, distance);
    }

    public void SetMinKnockbackDistance(int distance)
    {
        minKnockbackDistance = Mathf.Max(0, distance);
    }

    public void SetKnockbackStepDuration(float duration)
    {
        knockbackStepDuration = Mathf.Max(0f, duration);
    }

    public void SetKnockbackPlayerWithoutDamage(bool enabled)
    {
        knockbackPlayerWithoutDamage = enabled;
    }

    private void ScheduleExplosion()
    {
        if (explosionDelay > 0f)
            Invoke(nameof(Explode), explosionDelay);
        else
            Explode();
    }

    private void FindManagers()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (highlightManager == null)
            highlightManager = FindFirstObjectByType<GridHighlightManager>();

        if (gridManager == null)
            Debug.LogError("[ExplosionAttack] No GridManager found.", this);
    }

    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;
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

        if (flashExplosion)
            PlayExplosionFlash();

        // Handle the player separately from grid occupancy.
        // This also works when GetUnitAt() doesn't return the player.
        if (applyKnockback && knockbackPlayerWithoutDamage)
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
                    center
                );

                if (animateTiles && highlightManager != null)
                    highlightManager.FlashExplosionTile(position);
            }
        }

        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

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

            if (player == owner)
                continue;

            Vector2Int playerTile =
                gridManager.WorldToGridPosition(player.transform.position);

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
            {
                Debug.LogWarning(
                    "[ExplosionAttack] Player has no UnitMoveBrain.",
                    player
                );
                continue;
            }

            int knockbackDistance =
                CalculateKnockbackDistance(tileDistance);

            if (knockbackDistance <= 0)
                continue;

            Debug.Log(
                $"[ExplosionAttack] Player knockback requested. " +
                $"Center={explosionCenter}, " +
                $"PlayerTile={playerTile}, " +
                $"Distance={knockbackDistance}",
                player
            );

            moveBrain.ApplyKnockback(
                explosionCenter,
                knockbackDistance,
                knockbackStepDuration
            );
        }
    }

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

    private int CalculateKnockbackDistance(int distanceFromCenter)
    {
        if (radius <= 0)
            return maxKnockbackDistance;

        float normalizedDistance =
            Mathf.Clamp01((float)distanceFromCenter / radius);

        return Mathf.RoundToInt(
            Mathf.Lerp(
                maxKnockbackDistance,
                minKnockbackDistance,
                normalizedDistance
            )
        );
    }

    private void AttackTile(
        Vector2Int position,
        bool hasOwnerTeam,
        Team ownerTeam,
        int tileDamage,
        Vector2Int explosionCenter)
    {
        GameObject target = gridManager.GetUnitAt(position);

        if (target == null || target == owner)
            return;

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        bool isPlayer =
            target.tag == playerTag ||
            (targetHealth != null &&
             targetHealth.IsPlayerCharacter());

        if (targetHealth != null && targetHealth.IsDead())
            return;

        if (!isPlayer && targetHealth == null)
            return;

        // The player was already handled independently above.
        // Never apply explosion damage when this option is enabled.
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

        if (applyKnockback && knockbackDistance > 0)
        {
            UnitMoveBrain moveBrain =
                target.GetComponent<UnitMoveBrain>();

            if (moveBrain != null)
            {
                moveBrain.ApplyKnockback(
                    explosionCenter,
                    knockbackDistance,
                    knockbackStepDuration
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[ExplosionAttack] {target.name} has no UnitMoveBrain.",
                    target
                );
            }
        }

        if (targetHealth != null && tileDamage > 0)
            targetHealth.TakeDamage(tileDamage);
    }

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

    public void AnimationEvent_DisableExplosion()
    {
        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

    private void DestroyExplosion()
    {
        if (destroyAfterExplosion)
            Destroy(gameObject, destroyDelay);
    }

    public int GetRadius() => radius;
    public int GetDamage() => damage;
    public AbilitySO.RangeShape GetExplosionShape() => explosionShape;
    public float GetTotalDamageFalloff() => totalDamageFalloff;
    public GameObject GetOwner() => owner;
    public bool GetAllowFriendlyFire() => allowFriendlyFire;
    public bool HasExploded() => exploded;
}
