using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private GameObject owner;

    private Vector3 direction;

    private float speed;

    private int damage;

    private int maxTargets;


    private readonly HashSet<GameObject> hitTargets =
        new HashSet<GameObject>();


    private bool initialized;


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject owner,
        Vector3 direction,
        float speed,
        int damage,
        int maxTargets)
    {
        this.owner =
            owner;

        this.direction =
            direction.normalized;

        this.speed =
            speed;

        this.damage =
            damage;

        this.maxTargets =
            Mathf.Max(
                1,
                maxTargets
            );


        initialized = true;


        // Face the direction the arrow is travelling.
        RotateArrow();
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


        transform.position +=
            direction *
            speed *
            Time.deltaTime;
    }


    // ============================================================
    // ROTATION
    // ============================================================

    private void RotateArrow()
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }


        transform.rotation =
            Quaternion.LookRotation(
                direction
            );
    }


    // ============================================================
    // COLLISION
    // ============================================================

    private void OnTriggerEnter(
        Collider other)
    {
        if (!initialized)
        {
            return;
        }


        GameObject target =
            other.gameObject;


        if (target == null)
        {
            return;
        }


        // --------------------------------------------------------
        // Don't hit owner
        // --------------------------------------------------------

        if (target == owner)
        {
            return;
        }


        // --------------------------------------------------------
        // Don't hit the same unit twice
        // --------------------------------------------------------

        if (
            hitTargets.Contains(
                target
            )
        )
        {
            return;
        }


        // --------------------------------------------------------
        // Find unit
        // --------------------------------------------------------

        AttackUnit attackUnit =
            target.GetComponent<AttackUnit>();


        if (attackUnit == null)
        {
            return;
        }


        // --------------------------------------------------------
        // Ignore dead units
        // --------------------------------------------------------

        if (attackUnit.IsDead())
        {
            return;
        }


        // --------------------------------------------------------
        // Check team
        // --------------------------------------------------------

        if (
            !IsValidTarget(
                owner,
                target
            )
        )
        {
            return;
        }


        // --------------------------------------------------------
        // Damage
        // --------------------------------------------------------

        HealthManager health =
            target.GetComponent<HealthManager>();


        if (health == null)
        {
            return;
        }


        hitTargets.Add(
            target
        );


        health.TakeDamage(
            damage
        );


        // --------------------------------------------------------
        // Piercing
        //
        // IMPORTANT:
        // We DON'T destroy the arrow here.
        // It continues flying through the enemy.
        // --------------------------------------------------------

        if (
            hitTargets.Count >=
            maxTargets
        )
        {
            Destroy(
                gameObject
            );
        }
    }


    // ============================================================
    // TARGET CHECK
    // ============================================================

    private bool IsValidTarget(
        GameObject user,
        GameObject target)
    {
        if (
            user == null ||
            target == null
        )
        {
            return false;
        }


        AttackUnit userUnit =
            user.GetComponent<AttackUnit>();


        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();


        if (
            userUnit == null ||
            targetUnit == null
        )
        {
            return false;
        }


        Team userTeam =
            userUnit.GetTeam();


        Team targetTeam =
            targetUnit.GetTeam();


        if (
            userTeam == Team.Player ||
            userTeam == Team.Ally
        )
        {
            return targetTeam == Team.Enemy;
        }


        if (
            userTeam == Team.Enemy
        )
        {
            return
                targetTeam == Team.Player ||
                targetTeam == Team.Ally;
        }


        return false;
    }
}