using UnityEngine;

public class ExplosionAttack : MonoBehaviour
{
    // ==================================================
    // EXPLOSION
    // ==================================================

    [Header("Explosion")]
    [SerializeField, Min(0)]
    private int radius = 1;

    [SerializeField, Min(0)]
    private int damage = 10;

    // ==================================================
    // BEHAVIOUR
    // ==================================================

    [Header("Behaviour")]
    [SerializeField]
    private bool explodeOnStart = true;

    [SerializeField]
    private bool destroyAfterExplosion = true;

    [SerializeField, Min(0f)]
    private float destroyDelay = 0.1f;

    // ==================================================
    // JUICE & VISUALS
    // ==================================================

    [Header("Tile Juice")]
    [SerializeField]
    private bool animateTiles = true;

    [Header("Flash")]
    [SerializeField]
    private bool flashExplosion = true;

    // ==================================================
    // PRIVATE FIELDS
    // ==================================================

    private GameObject owner;
    private float explosionDelay = 0f;
    private bool exploded = false;

    private GridManager gridManager;
    private GridHighlightManager highlightManager;
    private Animator cachedAnimator;
    private ParticleSystem cachedParticleSystem;

    // ==================================================
    // INITIALIZATION
    // ==================================================

    public void Initialize(GameObject explosionOwner)
    {
        owner = explosionOwner;
        FindManagers();
        TryStartExplosion();
    }

    public void SetExplosionDelay(float delay) => explosionDelay = Mathf.Max(0f, delay);
    public void SetExplosionRadius(int newRadius) => radius = Mathf.Max(0, newRadius);
    public void SetExplosionDamage(int newDamage) => damage = Mathf.Max(0, newDamage);
    public void SetOwner(GameObject explosionOwner) => owner = explosionOwner;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        // Cache components on Awake to prevent repeated GetComponent calls
        cachedAnimator = GetComponent<Animator>();
        cachedParticleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        if (owner == null)
        {
            FindManagers();
            TryStartExplosion();
        }
    }

    // ==================================================
    // FIND MANAGERS
    // ==================================================

    private void FindManagers()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (highlightManager == null)
        {
            highlightManager = FindFirstObjectByType<GridHighlightManager>();
        }
    }

    // ==================================================
    // START EXPLOSION
    // ==================================================

    private void TryStartExplosion()
    {
        if (!explodeOnStart || exploded)
        {
            return;
        }

        if (explosionDelay > 0f)
        {
            Invoke(nameof(Explode), explosionDelay);
        }
        else
        {
            Explode();
        }
    }

    // ==================================================
    // EXPLOSION
    // ==================================================

    public void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;
        FindManagers();

        if (gridManager == null)
        {
            DestroyExplosion();
            return;
        }

        Vector2Int center = gridManager.WorldToGridPosition(transform.position);

        // ----------------------------------------------
        // EXPLOSION PREFAB FLASH
        // ----------------------------------------------
        if (flashExplosion)
        {
            PlayExplosionFlash();
        }

        // Cache team check info once to avoid fetching it on every tile loop
        HealthManager ownerHealth = (owner != null) ? owner.GetComponent<HealthManager>() : null;
        Team ownerTeam = (ownerHealth != null) ? ownerHealth.GetTeam() : (Team)(-1); // Fallback invalid team ID
        bool hasOwnerTeam = ownerHealth != null;

        // ----------------------------------------------
        // AFFECT EVERY TILE IN MANHATTAN DISTANCE
        // ----------------------------------------------
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Manhattan distance check
                if (Mathf.Abs(x) + Mathf.Abs(y) > radius)
                {
                    continue;
                }

                Vector2Int position = center + new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(position))
                {
                    continue;
                }

                // --------------------------------------
                // DAMAGE UNIT
                // --------------------------------------
                AttackTile(position, hasOwnerTeam, ownerTeam);

                // --------------------------------------
                // FLASH FLOOR TILE
                // --------------------------------------
                if (animateTiles && highlightManager != null)
                {
                    highlightManager.FlashExplosionTile(position);
                }
            }
        }

        // ----------------------------------------------
        // DESTROY
        // ----------------------------------------------
        DestroyExplosion();
    }

    // ==================================================
    // ATTACK TILE / UNIT
    // ==================================================

    private void AttackTile(Vector2Int position, bool hasOwnerTeam, Team ownerTeam)
    {
        GameObject target = gridManager.GetUnitAt(position);

        if (target == null || target == owner)
        {
            return;
        }

        if (!target.TryGetComponent<HealthManager>(out var targetHealth) || targetHealth.IsDead())
        {
            return;
        }

        // Friendly Fire Check
        if (hasOwnerTeam && ownerTeam == targetHealth.GetTeam())
        {
            return;
        }

        targetHealth.TakeDamage(damage);
    }

    // ==================================================
    // EXPLOSION FLASH
    // ==================================================

    private void PlayExplosionFlash()
    {
        if (cachedAnimator != null)
        {
            cachedAnimator.Play(0, 0, 0f);
        }

        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Stop(true);
            cachedParticleSystem.Play(true);
        }
    }

    // ==================================================
    // DESTROY
    // ==================================================

    private void DestroyExplosion()
    {
        if (!destroyAfterExplosion)
        {
            return;
        }

        Destroy(gameObject, destroyDelay);
    }

    // ==================================================
    // GETTERS
    // ==================================================

    public int GetRadius() => radius;
    public int GetDamage() => damage;
    public GameObject GetOwner() => owner;
    public bool HasExploded() => exploded;
}