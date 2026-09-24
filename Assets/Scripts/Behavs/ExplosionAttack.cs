using UnityEngine;

public class ExplosionAttack : MonoBehaviour
{
    // ============================================================
    // EXPLOSION
    // ============================================================

    [Header("Explosion")]

    [SerializeField, Min(0)]
    private int radius = 1;

    [SerializeField, Min(0)]
    private int damage = 10;


    // ============================================================
    // DAMAGE FALLOFF
    // ============================================================

    [Header("Damage Falloff")]

    [Tooltip(
        "Damage reduction per tile of Manhattan distance."
    )]
    [SerializeField, Min(0)]
    private int damageFalloff = 0;


    // ============================================================
    // BEHAVIOUR
    // ============================================================

    [Header("Behaviour")]

    [SerializeField]
    private bool explodeOnStart = true;

    [SerializeField]
    private bool destroyAfterExplosion = true;

    [SerializeField, Min(0f)]
    private float destroyDelay = 0.1f;


    // ============================================================
    // VISUALS
    // ============================================================

    [Header("Tile Juice")]

    [SerializeField]
    private bool animateTiles = true;

    [Header("Flash")]

    [SerializeField]
    private bool flashExplosion = true;


    // ============================================================
    // PRIVATE
    // ============================================================

    private GameObject owner;

    private float explosionDelay;

    private bool exploded;

    private GridManager gridManager;

    private GridHighlightManager highlightManager;

    private Animator cachedAnimator;

    private ParticleSystem cachedParticleSystem;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        cachedAnimator =
            GetComponent<Animator>();

        cachedParticleSystem =
            GetComponent<ParticleSystem>();
    }


    private void Start()
    {
        if (owner == null)
        {
            FindManagers();

            TryStartExplosion();
        }
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject explosionOwner
    )
    {
        owner =
            explosionOwner;

        FindManagers();

        TryStartExplosion();
    }


    // ============================================================
    // SETTERS
    // ============================================================

    public void SetExplosionDelay(
        float delay
    )
    {
        explosionDelay =
            Mathf.Max(
                0f,
                delay
            );
    }


    public void SetExplosionRadius(
        int newRadius
    )
    {
        radius =
            Mathf.Max(
                0,
                newRadius
            );
    }


    public void SetExplosionDamage(
        int newDamage
    )
    {
        damage =
            Mathf.Max(
                0,
                newDamage
            );


        Debug.Log(
            $"[ExplosionAttack] Damage set to {damage}",
            this
        );
    }


    public void SetDamageFalloff(
        int newFalloff
    )
    {
        damageFalloff =
            Mathf.Max(
                0,
                newFalloff
            );
    }


    public void SetOwner(
        GameObject explosionOwner
    )
    {
        owner =
            explosionOwner;
    }


    // ============================================================
    // FIND MANAGERS
    // ============================================================

    private void FindManagers()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }
    }


    // ============================================================
    // START EXPLOSION
    // ============================================================

    private void TryStartExplosion()
    {
        if (
            !explodeOnStart ||
            exploded
        )
        {
            return;
        }


        if (explosionDelay > 0f)
        {
            Invoke(
                nameof(Explode),
                explosionDelay
            );
        }
        else
        {
            Explode();
        }
    }


    // ============================================================
    // EXPLODE
    // ============================================================

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


        Vector2Int center =
            gridManager.WorldToGridPosition(
                transform.position
            );


        // --------------------------------------------------------
        // FLASH
        // --------------------------------------------------------

        if (flashExplosion)
        {
            PlayExplosionFlash();
        }


        // --------------------------------------------------------
        // OWNER TEAM
        // --------------------------------------------------------

        HealthManager ownerHealth =
            owner != null
                ? owner.GetComponent<HealthManager>()
                : null;


        Team ownerTeam =
            ownerHealth != null
                ? ownerHealth.GetTeam()
                : (Team)(-1);


        bool hasOwnerTeam =
            ownerHealth != null;


        // --------------------------------------------------------
        // EXPLOSION TILES
        // --------------------------------------------------------

        for (
            int x = -radius;
            x <= radius;
            x++
        )
        {
            for (
                int y = -radius;
                y <= radius;
                y++
            )
            {
                int distance =
                    Mathf.Abs(x) +
                    Mathf.Abs(y);


                if (distance > radius)
                {
                    continue;
                }


                Vector2Int position =
                    center +
                    new Vector2Int(
                        x,
                        y
                    );


                if (
                    !gridManager.IsInsideGrid(
                        position
                    )
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // DAMAGE
                // ------------------------------------------------

                int tileDamage =
                    CalculateDamageAtDistance(
                        distance
                    );


                AttackTile(
                    position,
                    hasOwnerTeam,
                    ownerTeam,
                    tileDamage
                );


                // ------------------------------------------------
                // VISUAL
                // ------------------------------------------------

                if (
                    animateTiles &&
                    highlightManager != null
                )
                {
                    highlightManager.FlashExplosionTile(
                        position
                    );
                }
            }
        }


        DestroyExplosion();
    }


    // ============================================================
    // DAMAGE CALCULATION
    // ============================================================

    private int CalculateDamageAtDistance(
        int distance
    )
    {
        int calculatedDamage =
            damage -
            (
                distance *
                damageFalloff
            );


        return
            Mathf.Max(
                0,
                calculatedDamage
            );
    }


    // ============================================================
    // ATTACK TILE
    // ============================================================

    private void AttackTile(
        Vector2Int position,
        bool hasOwnerTeam,
        Team ownerTeam,
        int tileDamage
    )
    {
        GameObject target =
            gridManager.GetUnitAt(
                position
            );


        if (
            target == null ||
            target == owner
        )
        {
            return;
        }


        if (
            !target.TryGetComponent<HealthManager>(
                out var targetHealth
            )
        )
        {
            return;
        }


        if (targetHealth.IsDead())
        {
            return;
        }


        // --------------------------------------------------------
        // FRIENDLY FIRE
        // --------------------------------------------------------

        if (
            hasOwnerTeam &&
            ownerTeam ==
            targetHealth.GetTeam()
        )
        {
            return;
        }


        if (tileDamage <= 0)
        {
            return;
        }


        Debug.Log(
            $"[ExplosionAttack] DAMAGE | " +
            $"Owner={owner?.name} | " +
            $"Target={target.name} | " +
            $"Damage={tileDamage}",
            target
        );


        targetHealth.TakeDamage(
            tileDamage
        );
    }


    // ============================================================
    // FLASH
    // ============================================================

    private void PlayExplosionFlash()
    {
        if (cachedAnimator != null)
        {
            cachedAnimator.Play(
                0,
                0,
                0f
            );
        }


        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Stop(true);

            cachedParticleSystem.Play(true);
        }
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void DestroyExplosion()
    {
        if (!destroyAfterExplosion)
        {
            return;
        }


        Destroy(
            gameObject,
            destroyDelay
        );
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public int GetRadius()
    {
        return radius;
    }


    public int GetDamage()
    {
        return damage;
    }


    public int GetDamageFalloff()
    {
        return damageFalloff;
    }


    public GameObject GetOwner()
    {
        return owner;
    }


    public bool HasExploded()
    {
        return exploded;
    }
}