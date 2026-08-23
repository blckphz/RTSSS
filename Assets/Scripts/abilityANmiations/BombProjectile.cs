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

    private float flightTime;
    private float elapsedTime;

    private float arcHeight = 2f;

    private Quaternion originalRotation;

    private bool initialized;


    public void Initialize(
        GameObject user,
        Vector3 targetPosition,
        GridManager gridManager,
        GameObject explosionPrefab,
        int explosionRadius,
        float explosionDelay,
        float speed)
    {
        this.user = user;
        this.targetPosition = targetPosition;
        this.gridManager = gridManager;
        this.explosionPrefab = explosionPrefab;
        this.explosionRadius = explosionRadius;
        this.explosionDelay = explosionDelay;
        this.speed = Mathf.Max(0.01f, speed);

        startPosition = transform.position;

        originalRotation = transform.rotation;

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
    }


    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                elapsedTime / flightTime
            );


        // --------------------------------------------------
        // LINEAR POSITION
        // --------------------------------------------------

        Vector3 position =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );


        // --------------------------------------------------
        // ARC
        // --------------------------------------------------

        float arc =
            Mathf.Sin(
                t * Mathf.PI
            ) * arcHeight;


        position.y += arc;


        // --------------------------------------------------
        // MOVE
        // --------------------------------------------------

        transform.position =
            position;


        // --------------------------------------------------
        // KEEP ORIGINAL ROTATION
        // --------------------------------------------------

        transform.rotation =
            originalRotation;


        // --------------------------------------------------
        // ARRIVED
        // --------------------------------------------------

        if (t >= 1f)
        {
            Explode();
        }
    }


    private void Explode()
    {
        if (!initialized)
        {
            return;
        }

        initialized = false;


        // --------------------------------------------------
        // SPAWN EXPLOSION
        // --------------------------------------------------

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
                    explosionAttack.SetExplosionRadius(
                        explosionRadius
                    );

                    explosionAttack.SetExplosionDelay(
                        explosionDelay
                    );

                    explosionAttack.Initialize(
                        user
                    );
                }
            }
        }


        // --------------------------------------------------
        // DESTROY BOMB
        // --------------------------------------------------

        Destroy(
            gameObject
        );
    }
}