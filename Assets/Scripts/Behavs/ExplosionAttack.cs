using UnityEngine;

public class ExplosionAttack : MonoBehaviour
{
    // ============================================================
    // EXPLOSION
    // ============================================================

    [Header("Explosion")]

    [SerializeField, Min(0)]
    private int radius;

    [SerializeField, Min(0)]
    private int damage = 10;


    // ============================================================
    // EXPLOSION SHAPE
    // ============================================================

    [Header("Explosion Shape")]

    [SerializeField]
    private AbilitySO.RangeShape explosionShape =
        AbilitySO.RangeShape.Diamond;


    // ============================================================
    // DAMAGE FALLOFF
    // ============================================================

    [Header("Damage Falloff")]

    [Tooltip(
        "Total damage reduction from the center to the outer edge " +
        "of the explosion, as a percentage."
    )]

    [SerializeField, Range(0f, 100f)]
    private float totalDamageFalloff = 80f;


    // ============================================================
    // FRIENDLY FIRE
    // ============================================================

    [Header("Friendly Fire")]

    [Tooltip(
        "If enabled, this explosion damages units on the same team " +
        "as the owner. The owner itself is still immune."
    )]

    [SerializeField]
    private bool allowFriendlyFire = false;


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
            $"[ExplosionAttack] DAMAGE SET | " +
            $"Object={name} | " +
            $"Damage={damage}",
            this
        );
    }


    public void SetExplosionShape(
        AbilitySO.RangeShape newShape
    )
    {
        explosionShape =
            newShape;

        Debug.Log(
            $"[ExplosionAttack] SHAPE SET | " +
            $"Object={name} | " +
            $"Shape={explosionShape}",
            this
        );
    }


    public void SetTotalDamageFalloff(
        float newFalloff
    )
    {
        totalDamageFalloff =
            Mathf.Clamp(
                newFalloff,
                0f,
                100f
            );
    }


    public void SetOwner(
        GameObject explosionOwner
    )
    {
        owner =
            explosionOwner;
    }


    public void SetAllowFriendlyFire(
        bool enabled
    )
    {
        allowFriendlyFire =
            enabled;
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

        if (gridManager == null)
        {
            Debug.LogError(
                $"[ExplosionAttack] FAILED | " +
                $"No GridManager found | " +
                $"Explosion={name}",
                this
            );
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
            Debug.LogError(
                $"[ExplosionAttack] FAILED | " +
                $"Cannot explode without GridManager | " +
                $"Explosion={name}",
                this
            );

            DestroyExplosion();

            return;
        }


        // ========================================================
        // CENTER TILE
        // ========================================================

        Vector2Int center =
            gridManager.WorldToGridPosition(
                transform.position
            );


        // ========================================================
        // OWNER INFORMATION
        // ========================================================

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


        // ========================================================
        // EXPLOSION START DEBUG
        // ========================================================

        Debug.Log(
            $"[ExplosionAttack] EXPLOSION START | " +
            $"Explosion={name} | " +
            $"WorldPosition={transform.position} | " +
            $"CenterTile={center} | " +
            $"Radius={radius} | " +
            $"Damage={damage} | " +
            $"Shape={explosionShape} | " +
            $"FriendlyFire={allowFriendlyFire} | " +
            $"Owner={(owner != null ? owner.name : "NULL")} | " +
            $"OwnerTeam={(hasOwnerTeam ? ownerTeam.ToString() : "NONE")}",
            this
        );


        // ========================================================
        // FLASH
        // ========================================================

        if (flashExplosion)
        {
            PlayExplosionFlash();
        }


        // ========================================================
        // EXPLOSION TILES
        // ========================================================

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


                // ------------------------------------------------
                // SHAPE CHECK
                // ------------------------------------------------

                if (
                    !IsInsideExplosionShape(
                        x,
                        y,
                        distance
                    )
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // GRID POSITION
                // ------------------------------------------------

                Vector2Int position =
                    center +
                    new Vector2Int(
                        x,
                        y
                    );


                // ------------------------------------------------
                // GRID CHECK
                // ------------------------------------------------

                if (
                    !gridManager.IsInsideGrid(
                        position
                    )
                )
                {
                    Debug.Log(
                        $"[ExplosionAttack] TILE SKIP | " +
                        $"Outside grid | " +
                        $"Tile={position}",
                        this
                    );

                    continue;
                }


                // ------------------------------------------------
                // DAMAGE
                // ------------------------------------------------

                int tileDamage =
                    CalculateDamageAtDistance(
                        distance
                    );


                // ------------------------------------------------
                // ATTACK
                // ------------------------------------------------

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


        // ========================================================
        // EXPLOSION COMPLETE
        // ========================================================

        Debug.Log(
            $"[ExplosionAttack] EXPLOSION COMPLETE | " +
            $"Explosion={name} | " +
            $"Center={center}",
            this
        );


        // ========================================================
        // DESTROY
        // ========================================================
        //
        // IMPORTANT:
        //
        // The explosion is NOT destroyed here anymore.
        //
        // The Animator Animation Event calls:
        //
        // AnimationEvent_DisableExplosion()
        //
        // when the explosion animation has finished.
        //
        // ========================================================
    }


    // ============================================================
    // EXPLOSION SHAPE
    // ============================================================

    private bool IsInsideExplosionShape(
        int x,
        int y,
        int distance
    )
    {
        switch (explosionShape)
        {
            case AbilitySO.RangeShape.Diamond:

                return
                    distance <= radius;


            case AbilitySO.RangeShape.Box:

                return
                    Mathf.Abs(x) <= radius &&
                    Mathf.Abs(y) <= radius;


            case AbilitySO.RangeShape.FourDirections:

                return
                    (
                        x == 0 ||
                        y == 0
                    ) &&
                    distance <= radius;


            case AbilitySO.RangeShape.Diagonal:

                return
                    Mathf.Abs(x) ==
                    Mathf.Abs(y) &&
                    distance <= radius;


            case AbilitySO.RangeShape.FourdirectionsAndDiragonal:

                return
                    (
                        x == 0 ||
                        y == 0 ||
                        Mathf.Abs(x) ==
                        Mathf.Abs(y)
                    ) &&
                    distance <= radius;


            case AbilitySO.RangeShape.Shotgun:

                return
                    distance <= radius;


            default:

                return
                    distance <= radius;
        }
    }


    // ============================================================
    // DAMAGE CALCULATION
    // ============================================================

    private int CalculateDamageAtDistance(
        int distance
    )
    {
        if (radius <= 0)
        {
            return damage;
        }

        float normalizedDistance =
            (float)distance /
            radius;

        float damageMultiplier =
            1f -
            (
                normalizedDistance *
                (totalDamageFalloff / 100f)
            );

        damageMultiplier =
            Mathf.Clamp01(
                damageMultiplier
            );

        return Mathf.RoundToInt(
            damage *
            damageMultiplier
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

        if (target == null)
        {
            return;
        }


        // ========================================================
        // NEVER DAMAGE THE OWNER
        // ========================================================

        if (target == owner)
        {
            Debug.Log(
                $"[ExplosionAttack] TARGET SKIPPED | " +
                $"Target is explosion owner | " +
                $"Target={target.name} | " +
                $"Tile={position}",
                target
            );

            return;
        }


        // ========================================================
        // HEALTH MANAGER
        // ========================================================

        if (
            !target.TryGetComponent<HealthManager>(
                out var targetHealth
            )
        )
        {
            Debug.LogWarning(
                $"[ExplosionAttack] TARGET SKIPPED | " +
                $"Target has no HealthManager | " +
                $"Target={target.name} | " +
                $"Tile={position}",
                target
            );

            return;
        }


        // ========================================================
        // DEAD TARGET
        // ========================================================

        if (targetHealth.IsDead())
        {
            Debug.Log(
                $"[ExplosionAttack] TARGET SKIPPED | " +
                $"Target is already dead | " +
                $"Target={target.name} | " +
                $"Tile={position}",
                target
            );

            return;
        }


        // ========================================================
        // TEAM
        // ========================================================

        Team targetTeam =
            targetHealth.GetTeam();


        Debug.Log(
            $"[ExplosionAttack] TEAM CHECK | " +
            $"Target={target.name} | " +
            $"TargetTeam={targetTeam} | " +
            $"OwnerTeam={(hasOwnerTeam ? ownerTeam.ToString() : "NONE")} | " +
            $"FriendlyFire={allowFriendlyFire}",
            target
        );


        // ========================================================
        // FRIENDLY FIRE
        // ========================================================

        if (
            !allowFriendlyFire &&
            hasOwnerTeam &&
            ownerTeam == targetTeam
        )
        {
            Debug.Log(
                $"[ExplosionAttack] TARGET SKIPPED | " +
                $"Friendly fire disabled | " +
                $"Target={target.name} | " +
                $"Team={targetTeam}",
                target
            );

            return;
        }


        // ========================================================
        // ZERO DAMAGE
        // ========================================================

        if (tileDamage <= 0)
        {
            Debug.Log(
                $"[ExplosionAttack] TARGET SKIPPED | " +
                $"Calculated damage is {tileDamage} | " +
                $"Target={target.name}",
                target
            );

            return;
        }


        // ========================================================
        // DAMAGE BEFORE
        // ========================================================

        int healthBefore =
            targetHealth.GetHealth();


        // ========================================================
        // APPLY DAMAGE
        // ========================================================

        Debug.Log(
            $"[ExplosionAttack] APPLY DAMAGE | " +
            $"Target={target.name} | " +
            $"Tile={position} | " +
            $"Damage={tileDamage} | " +
            $"HealthBefore={healthBefore} | " +
            $"FriendlyFire={allowFriendlyFire}",
            target
        );


        targetHealth.TakeDamage(
            tileDamage
        );


        // ========================================================
        // DAMAGE AFTER
        // ========================================================

        int healthAfter =
            targetHealth.GetHealth();


        Debug.Log(
            $"[ExplosionAttack] DAMAGE RESULT | " +
            $"Target={target.name} | " +
            $"RequestedDamage={tileDamage} | " +
            $"HealthBefore={healthBefore} | " +
            $"HealthAfter={healthAfter} | " +
            $"ActualDamage={healthBefore - healthAfter}",
            target
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
            cachedParticleSystem.Stop(
                true
            );

            cachedParticleSystem.Play(
                true
            );
        }
    }


    // ============================================================
    // ANIMATION EVENT
    // ============================================================

    public void AnimationEvent_DisableExplosion()
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


    public AbilitySO.RangeShape GetExplosionShape()
    {
        return explosionShape;
    }


    public float GetTotalDamageFalloff()
    {
        return totalDamageFalloff;
    }


    public GameObject GetOwner()
    {
        return owner;
    }


    public bool GetAllowFriendlyFire()
    {
        return allowFriendlyFire;
    }


    public bool HasExploded()
    {
        return exploded;
    }
}