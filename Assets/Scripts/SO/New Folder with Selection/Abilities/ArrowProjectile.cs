using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform spriteTransform;

    [Header("Rotation")]
    [SerializeField]
    private float rotationOffset = 90f;

    [Header("Piercing")]
    [SerializeField] private float collisionRadius = 0.15f;

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

        this.direction =
            new Vector2(
                direction.x,
                direction.y
            ).normalized;

        this.speed =
            Mathf.Max(0.01f, speed);

        this.damage = damage;

        this.maxTargets =
            Mathf.Max(1, maxTargets);

        this.ability = ability;

        targetsHit = 0;

        hitTargets.Clear();

        initialized = true;

        RefreshRotation();
    }

    private void Move()
    {
        Vector3 movement =
            (Vector3)(direction * speed * Time.deltaTime);

        transform.position += movement;
    }

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

            GameObject target =
                hit.gameObject;

            if (target == null)
                continue;

            if (target == owner)
                continue;

            if (!target.activeInHierarchy)
                continue;

            if (hitTargets.Contains(target))
                continue;

            AttackUnit targetUnit =
                target.GetComponent<AttackUnit>();

            if (targetUnit == null)
                continue;

            HealthManager health =
                target.GetComponent<HealthManager>();

            if (health == null)
                continue;

            if (health.IsDead())
                continue;

            if (!CanDamage(target))
                continue;

            hitTargets.Add(target);

            DealDamage(target);

            targetsHit++;

            if (targetsHit >= maxTargets)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

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

        if (ownerUnit == null ||
            targetUnit == null)
        {
            return false;
        }

        return ownerUnit.GetTeam() !=
               targetUnit.GetTeam();
    }

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

    private void RefreshRotation()
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        // rotationOffset compensates for the
        // direction the arrow sprite faces by default.
        float finalAngle =
            angle + rotationOffset;

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                finalAngle
            );

        if (spriteTransform != null)
        {
            spriteTransform.rotation = rotation;
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            collisionRadius
        );
    }
}