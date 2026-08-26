using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightningProjectile : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Visual")]
    [SerializeField]
    private Transform spriteTransform;

    // ============================================================
    // STATE
    // ============================================================

    private GameObject user;
    private List<GameObject> chainTargets;
    private float speed;
    private float jumpDelay;
    private int damage;
    private int currentTargetIndex;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightTime;
    private float elapsedTime;
    private bool initialized;

    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject user,
        List<GameObject> chainTargets,
        float speed,
        float jumpDelay,
        int damage)
    {
        this.user = user;
        this.chainTargets = chainTargets != null
            ? new List<GameObject>(chainTargets)
            : new List<GameObject>();

        this.speed = Mathf.Max(0.01f, speed);
        this.jumpDelay = Mathf.Max(0f, jumpDelay);
        this.damage = damage;
        currentTargetIndex = 0;
        initialized = false;

        if (this.user == null || this.chainTargets == null || this.chainTargets.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        StartNextTarget();
    }

    // ============================================================
    // START NEXT TARGET
    // ============================================================

    private void StartNextTarget()
    {
        initialized = false;

        while (currentTargetIndex < chainTargets.Count)
        {
            GameObject target = chainTargets[currentTargetIndex];

            if (target != null && target.activeInHierarchy)
            {
                break;
            }

            currentTargetIndex++;
        }

        if (currentTargetIndex >= chainTargets.Count)
        {
            Destroy(gameObject);
            return;
        }

        startPosition = transform.position;

        GameObject currentTarget = chainTargets[currentTargetIndex];
        targetPosition = currentTarget.transform.position;

        float distance = Vector3.Distance(startPosition, targetPosition);
        flightTime = Mathf.Max(0.01f, distance / speed);
        elapsedTime = 0f;

        RefreshRotation();

        initialized = true;
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

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / flightTime);

        transform.position = Vector3.Lerp(startPosition, targetPosition, t);

        RefreshRotation();

        if (t >= 1f)
        {
            HitTarget();
        }
    }

    // ============================================================
    // REFRESH ROTATION
    // ============================================================

    private void RefreshRotation()
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        float rawAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float finalAngle = rawAngle + 90f;

        Quaternion rotation = Quaternion.Euler(0f, 0f, finalAngle);

        if (spriteTransform != null)
        {
            spriteTransform.rotation = rotation;
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    // ============================================================
    // HIT TARGET
    // ============================================================

    private void HitTarget()
    {
        if (!initialized)
        {
            return;
        }

        initialized = false;

        GameObject target = null;

        if (currentTargetIndex >= 0 && currentTargetIndex < chainTargets.Count)
        {
            target = chainTargets[currentTargetIndex];
        }

        if (target != null)
        {
            DealDamage(target);
        }

        currentTargetIndex++;

        if (jumpDelay > 0f)
        {
            StartCoroutine(ContinueChainAfterDelay());
        }
        else
        {
            StartNextTarget();
        }
    }

    // ============================================================
    // DAMAGE
    // ============================================================

    private void DealDamage(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        HealthManager health = target.GetComponent<HealthManager>();

        if (health == null)
        {
            return;
        }

        health.TakeDamage(damage);
    }

    // ============================================================
    // CONTINUE CHAIN
    // ============================================================

    private IEnumerator ContinueChainAfterDelay()
    {
        yield return new WaitForSeconds(jumpDelay);
        StartNextTarget();
    }
}