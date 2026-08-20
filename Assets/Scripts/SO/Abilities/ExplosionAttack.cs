using UnityEngine;

public class ExplosionAttack : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField, Min(0)]
    private int radius = 1;

    [SerializeField, Min(0)]
    private int damage = 10;

    [Header("Behaviour")]
    [SerializeField]
    private bool explodeOnStart = true;

    [SerializeField]
    private bool destroyAfterExplosion = true;

    [SerializeField, Min(0f)]
    private float destroyDelay = 0.1f;

    [Header("Tile Juice")]
    [SerializeField]
    private bool animateTiles = true;

    [Header("Flash")]
    [SerializeField]
    private bool flashExplosion = true;

    private GameObject owner;

    private float explosionDelay = 0f;

    private bool exploded = false;

    private GridManager gridManager;

    private GridHighlightManager highlightManager;


    // ==================================================
    // INITIALIZATION
    // ==================================================

    public void Initialize(
        GameObject explosionOwner)
    {
        owner = explosionOwner;

        FindManagers();

        TryStartExplosion();
    }


    public void SetExplosionDelay(
        float delay)
    {
        explosionDelay =
            Mathf.Max(0f, delay);
    }


    public void SetExplosionRadius(
        int newRadius)
    {
        radius =
            Mathf.Max(0, newRadius);
    }


    public void SetExplosionDamage(
        int newDamage)
    {
        damage =
            Mathf.Max(0, newDamage);
    }


    public void SetOwner(
        GameObject explosionOwner)
    {
        owner = explosionOwner;
    }


    // ==================================================
    // UNITY
    // ==================================================

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
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }
    }


    // ==================================================
    // START EXPLOSION
    // ==================================================

    private void TryStartExplosion()
    {
        if (!explodeOnStart)
        {
            return;
        }

        if (exploded)
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
            Debug.LogError(
                "[ExplosionAttack] GridManager not found."
            );

            DestroyExplosion();

            return;
        }

        Vector2Int center =
            gridManager.WorldToGridPosition(
                transform.position
            );

        Debug.Log(
            $"[ExplosionAttack] Explosion at {center}, " +
            $"Radius={radius}, Damage={damage}"
        );


        // ----------------------------------------------
        // EXPLOSION PREFAB FLASH
        // ----------------------------------------------

        if (flashExplosion)
        {
            PlayExplosionFlash();
        }


        // ----------------------------------------------
        // AFFECT EVERY TILE
        // ----------------------------------------------

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
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(
                        position))
                {
                    continue;
                }


                // --------------------------------------
                // DAMAGE UNIT
                // --------------------------------------

                AttackTile(position);


                // --------------------------------------
                // FLASH FLOOR TILE
                // --------------------------------------

                if (
                    animateTiles &&
                    highlightManager != null
                )
                {
                    highlightManager
                        .FlashExplosionTile(position);
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

    private void AttackTile(
        Vector2Int position)
    {
        if (gridManager == null)
        {
            return;
        }

        GameObject target =
            gridManager.GetUnitAt(position);

        if (target == null)
        {
            return;
        }

        if (target == owner)
        {
            return;
        }

        HealthManager targetHealth =
            target.GetComponent<HealthManager>();

        if (targetHealth == null)
        {
            return;
        }

        if (targetHealth.IsDead())
        {
            return;
        }


        // ----------------------------------------------
        // TEAM CHECK
        // ----------------------------------------------

        if (owner != null)
        {
            HealthManager ownerHealth =
                owner.GetComponent<HealthManager>();

            if (ownerHealth != null)
            {
                if (
                    ownerHealth.GetTeam() ==
                    targetHealth.GetTeam()
                )
                {
                    return;
                }
            }
        }


        // ----------------------------------------------
        // DAMAGE
        // ----------------------------------------------

        targetHealth.TakeDamage(damage);

        Debug.Log(
            $"[ExplosionAttack] {target.name} took " +
            $"{damage} explosion damage."
        );
    }


    // ==================================================
    // EXPLOSION FLASH
    // ==================================================

    private void PlayExplosionFlash()
    {
        Animator animator =
            GetComponent<Animator>();

        if (animator != null)
        {
            animator.Play(
                0,
                0,
                0f
            );
        }


        ParticleSystem particles =
            GetComponent<ParticleSystem>();

        if (particles != null)
        {
            particles.Stop(true);

            particles.Play(true);
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

        Destroy(
            gameObject,
            destroyDelay
        );
    }


    // ==================================================
    // GETTERS
    // ==================================================

    public int GetRadius()
    {
        return radius;
    }


    public int GetDamage()
    {
        return damage;
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