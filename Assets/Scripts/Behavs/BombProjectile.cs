
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
    private int explosionDamage;

    private AbilitySO.RangeShape explosionShape =
        AbilitySO.RangeShape.Diamond;

    private float flightTime;
    private float elapsedTime;
    private float arcHeight = 2f;

    private Quaternion originalRotation;
    private bool initialized;
    private bool hasExploded;

    // Backwards-compatible overload.
    public void Initialize(
        GameObject user,
        Vector3 targetPosition,
        GridManager gridManager,
        GameObject explosionPrefab,
        int explosionRadius,
        float explosionDelay,
        float speed,
        int explosionDamage)
    {
        AbilitySO.RangeShape shape =
            AbilitySO.RangeShape.Diamond;

        if (explosionPrefab != null)
        {
            ExplosionAttack attack =
                explosionPrefab.GetComponent<ExplosionAttack>();

            if (attack == null)
            {
                attack =
                    explosionPrefab.GetComponentInChildren<ExplosionAttack>(true);
            }

            if (attack != null)
                shape = attack.GetExplosionShape();
        }

        Initialize(
            user,
            targetPosition,
            gridManager,
            explosionPrefab,
            explosionRadius,
            explosionDelay,
            speed,
            explosionDamage,
            shape);
    }

    // New overload: receives the exact shape from BombAttack.
    public void Initialize(
        GameObject user,
        Vector3 targetPosition,
        GridManager gridManager,
        GameObject explosionPrefab,
        int explosionRadius,
        float explosionDelay,
        float speed,
        int explosionDamage,
        AbilitySO.RangeShape explosionShape)
    {
        this.user = user;
        this.targetPosition = targetPosition;
        this.gridManager = gridManager;
        this.explosionPrefab = explosionPrefab;

        this.explosionRadius = Mathf.Max(0, explosionRadius);
        this.explosionDelay = Mathf.Max(0f, explosionDelay);
        this.speed = Mathf.Max(0.01f, speed);
        this.explosionDamage = Mathf.Max(0, explosionDamage);
        this.explosionShape = explosionShape;

        startPosition = transform.position;
        originalRotation = transform.rotation;

        float distance = Vector3.Distance(
            startPosition,
            targetPosition);

        flightTime = Mathf.Max(0.05f, distance / this.speed);

        elapsedTime = 0f;
        hasExploded = false;
        initialized = true;

        Debug.Log(
            $"[BombProjectile] Initialized | " +
            $"Radius={this.explosionRadius} | " +
            $"Damage={this.explosionDamage} | " +
            $"Shape={this.explosionShape}",
            this);
    }

    private void Update()
    {
        if (!initialized || hasExploded)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / flightTime);

        Vector3 position = Vector3.Lerp(
            startPosition,
            targetPosition,
            t);

        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

        transform.position = position;
        transform.rotation = originalRotation;

        if (t >= 1f)
            Explode();
    }

    private void Explode()
    {
        if (!initialized || hasExploded)
            return;

        hasExploded = true;
        initialized = false;

        if (explosionPrefab == null)
        {
            Debug.LogError(
                "[BombProjectile] Explosion prefab is not assigned.",
                this);

            Destroy(gameObject);
            return;
        }

        GameObject explosionObject = Instantiate(
            explosionPrefab,
            targetPosition,
            Quaternion.identity);

        if (explosionObject == null)
        {
            Debug.LogError(
                "[BombProjectile] Failed to instantiate explosion.",
                this);

            Destroy(gameObject);
            return;
        }

        ExplosionAttack explosionAttack =
            explosionObject.GetComponent<ExplosionAttack>();

        if (explosionAttack == null)
        {
            explosionAttack =
                explosionObject.GetComponentInChildren<ExplosionAttack>(true);
        }

        if (explosionAttack == null)
        {
            Debug.LogError(
                "[BombProjectile] Explosion prefab has no " +
                "ExplosionAttack component.",
                explosionObject);

            Destroy(explosionObject);
            Destroy(gameObject);
            return;
        }

        // Set every value before initializing the explosion.
        explosionAttack.SetExplosionRadius(explosionRadius);
        explosionAttack.SetExplosionDamage(explosionDamage);
        explosionAttack.SetExplosionDelay(explosionDelay);
        explosionAttack.SetExplosionShape(explosionShape);

        Debug.Log(
            $"[BombProjectile] Explosion configured | " +
            $"Radius={explosionRadius} | " +
            $"Damage={explosionDamage} | " +
            $"Shape={explosionShape}",
            explosionObject);

        explosionAttack.Initialize(user);

        Destroy(gameObject);
    }
}
