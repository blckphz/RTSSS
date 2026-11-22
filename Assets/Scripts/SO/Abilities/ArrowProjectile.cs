
using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform spriteTransform;

    [Header("Rotation")]
    [SerializeField] private float rotationOffset = 90f;
    [SerializeField] private bool rotateWhileFlying = false;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Piercing")]
    [SerializeField] private float collisionRadius = 0.15f;

    [Header("Target Hitbox")]
    [SerializeField] private string attackHitboxName = "AttackHitbox";

    private GameObject owner;
    private AbilitySO ability;

    private Vector2 direction;

    private float speed;
    private int damage;
    private int maxTargets;

    private int targetsHit;

    private bool initialized;

    private readonly HashSet<GameObject> hitTargets =
        new HashSet<GameObject>();

    private void Update()
    {
        if (!initialized)
            return;

        Move();
        CheckForTargets();

        if (rotateWhileFlying)
            RotateWhileFlying();
        else
            RefreshRotation();
    }

    public void Initialize(
        GameObject owner,
        Vector3 direction,
        float speed,
        int damage,
        int maxTargets,
        AbilitySO ability)
    {
        this.owner = owner;

        this.direction = new Vector2(
            direction.x,
            direction.y
        ).normalized;

        this.speed = Mathf.Max(0.01f, speed);
        this.damage = damage;
        this.maxTargets = Mathf.Max(1, maxTargets);
        this.ability = ability;

        targetsHit = 0;
        hitTargets.Clear();

        initialized = true;

        RefreshRotation();
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private void Move()
    {
        Vector3 movement =
            (Vector3)(direction * speed * Time.deltaTime);

        transform.position += movement;
    }

    // ============================================================
    // ROTATION
    // ============================================================

    private void RotateWhileFlying()
    {
        Transform target =
            spriteTransform != null
                ? spriteTransform
                : transform;

        target.Rotate(
            0f,
            0f,
            rotationSpeed * Time.deltaTime
        );
    }

    private void RefreshRotation()
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        float finalAngle = angle + rotationOffset;

        Quaternion rotation =
            Quaternion.Euler(0f, 0f, finalAngle);

        if (spriteTransform != null)
            spriteTransform.rotation = rotation;
        else
            transform.rotation = rotation;
    }

    // ============================================================
    // CHECK TARGET COLLISIONS
    // ============================================================

    private void CheckForTargets()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                collisionRadius
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (!hit.gameObject.activeInHierarchy)
                continue;

            // Find the unit associated with this collider.
            AttackUnit targetUnit =
                hit.GetComponentInParent<AttackUnit>();

            if (targetUnit == null)
                continue;

            GameObject targetObject = targetUnit.gameObject;

            // Ignore the owner.
            if (targetObject == owner)
                continue;

            // Prevent hitting the same unit more than once.
            if (hitTargets.Contains(targetObject))
                continue;

            // Find the dedicated AttackHitbox collider.
            Collider2D attackHitbox =
                GetAttackHitbox(targetObject);

            if (attackHitbox == null)
                continue;

            // Ignore every collider except AttackHitbox.
            if (hit != attackHitbox)
                continue;

            // Check whether the target is alive.
            HealthManager health =
                targetObject.GetComponent<HealthManager>();

            if (health == null || health.IsDead())
                continue;

            // Check team and ability targeting restrictions.
            if (!CanDamage(targetObject))
                continue;

            // Register the hit.
            hitTargets.Add(targetObject);

            DealDamage(targetObject);

            targetsHit++;

            // Destroy projectile after reaching maximum targets.
            if (targetsHit >= maxTargets)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    // ============================================================
    // FIND ATTACK HITBOX
    // ============================================================

    private Collider2D GetAttackHitbox(GameObject target)
    {
        if (target == null)
            return null;

        Transform hitboxTransform = null;

        // Search the complete hierarchy.
        foreach (Transform child in
                 target.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == attackHitboxName)
            {
                hitboxTransform = child;
                break;
            }
        }

        if (hitboxTransform == null)
            return null;

        return hitboxTransform.GetComponent<Collider2D>();
    }

    // ============================================================
    // DAMAGE VALIDATION
    // ============================================================

    private bool CanDamage(GameObject target)
    {
        if (owner == null || target == null)
            return false;

        if (ability != null)
        {
            return ability.CanTargetObject(
                owner,
                target
            );
        }

        AttackUnit ownerUnit =
            owner.GetComponent<AttackUnit>();

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (ownerUnit == null || targetUnit == null)
            return false;

        return ownerUnit.GetTeam() != targetUnit.GetTeam();
    }

    // ============================================================
    // DEAL DAMAGE
    // ============================================================

    private void DealDamage(GameObject target)
    {
        if (target == null)
            return;

        HealthManager health =
            target.GetComponent<HealthManager>();

        if (health == null)
            return;

        health.TakeDamage(damage);
    }

    // ============================================================
    // DEBUG
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            collisionRadius
        );
    }
}
