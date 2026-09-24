using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    private GameObject user;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private GridManager gridManager;
    private GameObject explosionPrefab;

    private int explosionRadius;
    private float explosionDelay;
    private float speed;

    // IMPORTANT:
    // This is the damage calculated by BombAttack when
    // the bomb was fired.
    private int explosionDamage;

    private float flightTime;
    private float elapsedTime;

    private float arcHeight = 2f;

    private Quaternion originalRotation;

    private bool initialized;


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject user,
        Vector3 targetPosition,
        GridManager gridManager,
        GameObject explosionPrefab,
        int explosionRadius,
        float explosionDelay,
        float speed,
        int explosionDamage
    )
    {
        this.user =
            user;

        this.targetPosition =
            targetPosition;

        this.gridManager =
            gridManager;

        this.explosionPrefab =
            explosionPrefab;

        this.explosionRadius =
            Mathf.Max(
                0,
                explosionRadius
            );

        this.explosionDelay =
            Mathf.Max(
                0f,
                explosionDelay
            );

        this.speed =
            Mathf.Max(
                0.01f,
                speed
            );

        this.explosionDamage =
            Mathf.Max(
                0,
                explosionDamage
            );


        startPosition =
            transform.position;

        originalRotation =
            transform.rotation;


        float distance =
            Vector3.Distance(
                startPosition,
                targetPosition
            );


        flightTime =
            Mathf.Max(
                0.05f,
                distance / this.speed
            );


        elapsedTime = 0f;

        initialized = true;


        Debug.Log(
            $"[BombProjectile] Initialized | " +
            $"User={user?.name} | " +
            $"ExplosionDamage={this.explosionDamage}",
            user
        );
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        elapsedTime +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                elapsedTime /
                flightTime
            );


        // --------------------------------------------------------
        // LINEAR POSITION
        // --------------------------------------------------------

        Vector3 position =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );


        // --------------------------------------------------------
        // ARC
        // --------------------------------------------------------

        float arc =
            Mathf.Sin(
                t * Mathf.PI
            ) * arcHeight;


        position.y += arc;


        // --------------------------------------------------------
        // MOVE
        // --------------------------------------------------------

        transform.position =
            position;


        // --------------------------------------------------------
        // KEEP ORIGINAL ROTATION
        // --------------------------------------------------------

        transform.rotation =
            originalRotation;


        // --------------------------------------------------------
        // ARRIVED
        // --------------------------------------------------------

        if (t >= 1f)
        {
            Explode();
        }
    }


    // ============================================================
    // EXPLODE
    // ============================================================

    private void Explode()
    {
        if (!initialized)
        {
            return;
        }

        initialized = false;


        // --------------------------------------------------------
        // SPAWN EXPLOSION
        // --------------------------------------------------------

        if (explosionPrefab != null)
        {
            GameObject explosion =
                Instantiate(
                    explosionPrefab,
                    targetPosition,
                    Quaternion.identity
                );


            if (explosion != null)
            {
                ExplosionAttack explosionAttack =
                    explosion.GetComponent<ExplosionAttack>();


                if (explosionAttack != null)
                {
                    // Set ALL explosion values before Initialize().
                    //
                    // Initialize() can immediately call Explode(),
                    // so these must already be configured.

                    explosionAttack.SetExplosionRadius(
                        explosionRadius
                    );

                    explosionAttack.SetExplosionDelay(
                        explosionDelay
                    );

                    explosionAttack.SetExplosionDamage(
                        explosionDamage
                    );

                    explosionAttack.Initialize(
                        user
                    );


                    Debug.Log(
                        $"[BombProjectile] Explosion created | " +
                        $"Damage={explosionDamage} | " +
                        $"Radius={explosionRadius}",
                        user
                    );
                }
                else
                {
                    Debug.LogError(
                        "[BombProjectile] Explosion prefab does not " +
                        "contain ExplosionAttack.",
                        explosion
                    );
                }
            }
        }


        // --------------------------------------------------------
        // DESTROY BOMB
        // --------------------------------------------------------

        Destroy(
            gameObject
        );
    }
}