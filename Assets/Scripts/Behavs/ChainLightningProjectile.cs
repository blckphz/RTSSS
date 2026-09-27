using System.Collections.Generic;
using UnityEngine;

public class ChainLightningProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField]
    private Transform spriteTransform;

    private GameObject user;

    private List<GameObject> chainTargets =
        new List<GameObject>();

    private float speed;
    private int damage;

    private float stunPercentage;
    private int stunDuration;

    /*
     * Number of charges the turret receives
     * when the projectile reaches it.
     */
    private int turretCharges;

    private int currentTargetIndex;

    private GameObject currentTarget;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float travelTime;
    private float elapsedTime;


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject user,
        List<GameObject> chainTargets,
        float speed,
        int damage,
        float stunPercentage,
        int stunDuration,
        int turretCharges)
    {
        this.user = user;

        this.chainTargets =
            chainTargets != null
                ? new List<GameObject>(
                    chainTargets
                )
                : new List<GameObject>();

        this.speed = speed;

        this.damage = damage;

        this.stunPercentage =
            stunPercentage;

        this.stunDuration =
            stunDuration;

        this.turretCharges =
            Mathf.Max(
                1,
                turretCharges
            );

        currentTargetIndex = 0;

        StartNextTarget();
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (currentTarget == null)
        {
            StartNextTarget();
            return;
        }

        elapsedTime +=
            Time.deltaTime;

        float t =
            travelTime > 0f
                ? elapsedTime / travelTime
                : 1f;

        t =
            Mathf.Clamp01(t);

        transform.position =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

        if (t >= 1f)
        {
            HitTarget();
        }
    }


    // ============================================================
    // NEXT TARGET
    // ============================================================

    private void StartNextTarget()
    {
        currentTarget = null;

        while (
            currentTargetIndex <
            chainTargets.Count
        )
        {
            GameObject candidate =
                chainTargets[
                    currentTargetIndex
                ];

            currentTargetIndex++;

            if (candidate == null)
            {
                continue;
            }

            if (!candidate.activeInHierarchy)
            {
                continue;
            }

            AttackUnit attackUnit =
                candidate.GetComponent<AttackUnit>();

            if (
                attackUnit != null &&
                attackUnit.IsDead()
            )
            {
                continue;
            }

            currentTarget =
                candidate;

            break;
        }

        if (currentTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        startPosition =
            transform.position;

        targetPosition =
            currentTarget.transform.position;

        float distance =
            Vector3.Distance(
                startPosition,
                targetPosition
            );

        travelTime =
            speed > 0f
                ? distance / speed
                : 0f;

        elapsedTime = 0f;
    }


    // ============================================================
    // HIT TARGET
    // ============================================================

    private void HitTarget()
    {
        if (currentTarget == null)
        {
            StartNextTarget();
            return;
        }


        // ========================================================
        // TURRET
        // ========================================================

        turretbehav turret =
            currentTarget.GetComponent<
                turretbehav>();

        if (turret != null)
        {
            /*
             * The turret does not take damage.
             *
             * The turret does not get stunned.
             *
             * Instead, ALL remaining bounces are converted
             * into charges.
             *
             * Example:
             *
             * maxJumps = 5
             * turret is first target
             * turretCharges = 5
             *
             * Result:
             *
             * Turret +5 charges
             *
             * Chain immediately ends.
             */

            turret.AddChainLightningCharges(
                turretCharges
            );

            Destroy(gameObject);

            return;
        }


        // ========================================================
        // NORMAL TARGET
        // ========================================================

        DealDamage(
            currentTarget
        );

        TryApplyStun(
            currentTarget
        );

        currentTarget = null;

        StartNextTarget();
    }


    // ============================================================
    // DAMAGE
    // ============================================================

    private void DealDamage(
        GameObject target)
    {
        if (target == null)
        {
            return;
        }

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        /*
         * Player targets are allowed to be aimed at,
         * but Chain Lightning does NOT damage them.
         */
        if (
            targetUnit != null &&
            targetUnit.GetTeam() ==
            Team.Player
        )
        {
            return;
        }

        HealthManager health =
            target.GetComponent<HealthManager>();

        if (health == null)
        {
            return;
        }

        health.TakeDamage(
            damage
        );
    }


    // ============================================================
    // STUN
    // ============================================================

    private void TryApplyStun(
        GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (stunPercentage <= 0f)
        {
            return;
        }

        if (stunDuration <= 0)
        {
            return;
        }

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return;
        }

        /*
         * Player targets cannot be stunned.
         */
        if (
            targetUnit.GetTeam() ==
            Team.Player
        )
        {
            return;
        }

        if (targetUnit.IsDead())
        {
            return;
        }

        float roll =
            Random.Range(
                0f,
                100f
            );

        if (roll > stunPercentage)
        {
            return;
        }

        ConditionManager.ApplyStun(
            target,
            stunDuration
        );
    }
}