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
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLogs = true;

    [SerializeField]
    private bool debugMovement = false;

    [SerializeField]
    private bool debugRotation = true;

    [SerializeField]
    private bool drawDebugLine = true;


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
        this.user =
            user;

        this.chainTargets =
            chainTargets != null
                ? new List<GameObject>(chainTargets)
                : new List<GameObject>();

        this.speed =
            Mathf.Max(
                0.01f,
                speed
            );

        this.jumpDelay =
            Mathf.Max(
                0f,
                jumpDelay
            );

        this.damage =
            damage;

        currentTargetIndex =
            0;

        initialized =
            false;


        DebugLog(
            "============================================================"
        );

        DebugLog(
            "INITIALIZE"
        );

        DebugLog(
            "Projectile: " +
            gameObject.name
        );

        DebugLog(
            "User: " +
            (user != null ? user.name : "NULL")
        );

        DebugLog(
            "Chain target count: " +
            this.chainTargets.Count
        );

        DebugLog(
            "Speed: " +
            this.speed
        );

        DebugLog(
            "Jump delay: " +
            this.jumpDelay
        );

        DebugLog(
            "Damage: " +
            this.damage
        );

        DebugLog(
            "Projectile position: " +
            transform.position
        );

        DebugLog(
            "Sprite Transform: " +
            (spriteTransform != null
                ? spriteTransform.name
                : "NULL")
        );


        // --------------------------------------------------------
        // VALIDATION
        // --------------------------------------------------------

        if (
            this.user == null ||
            this.chainTargets == null ||
            this.chainTargets.Count == 0
        )
        {
            Debug.LogWarning(
                "[ChainLightningProjectile] Initialization failed. " +
                "User or chain targets are invalid."
            );

            Destroy(
                gameObject
            );

            return;
        }


        // --------------------------------------------------------
        // START FIRST TARGET
        // --------------------------------------------------------

        StartNextTarget();
    }


    // ============================================================
    // START NEXT TARGET
    // ============================================================

    private void StartNextTarget()
    {
        initialized =
            false;


        DebugLog(
            "------------------------------------------------------------"
        );

        DebugLog(
            "START NEXT TARGET"
        );

        DebugLog(
            "Current target index: " +
            currentTargetIndex
        );


        // --------------------------------------------------------
        // FIND NEXT VALID TARGET
        // --------------------------------------------------------

        while (
            currentTargetIndex <
            chainTargets.Count
        )
        {
            GameObject target =
                chainTargets[
                    currentTargetIndex
                ];


            DebugLog(
                "Checking target [" +
                currentTargetIndex +
                "]: " +
                (target != null
                    ? target.name
                    : "NULL")
            );


            if (
                target != null &&
                target.activeInHierarchy
            )
            {
                break;
            }


            DebugLog(
                "Target invalid/inactive. Skipping."
            );


            currentTargetIndex++;
        }


        // --------------------------------------------------------
        // CHAIN FINISHED
        // --------------------------------------------------------

        if (
            currentTargetIndex >=
            chainTargets.Count
        )
        {
            DebugLog(
                "CHAIN FINISHED -> Destroy projectile"
            );

            Destroy(
                gameObject
            );

            return;
        }


        // --------------------------------------------------------
        // NEW START POSITION
        // --------------------------------------------------------

        startPosition =
            transform.position;


        // --------------------------------------------------------
        // NEW TARGET
        // --------------------------------------------------------

        GameObject currentTarget =
            chainTargets[
                currentTargetIndex
            ];


        targetPosition =
            currentTarget.transform.position;


        DebugLog(
            "New target: " +
            currentTarget.name
        );

        DebugLog(
            "Start position: " +
            startPosition
        );

        DebugLog(
            "Target position: " +
            targetPosition
        );


        // --------------------------------------------------------
        // DIRECTION
        // --------------------------------------------------------

        Vector3 direction =
            targetPosition -
            startPosition;


        DebugLog(
            "Initial direction: " +
            direction
        );

        DebugLog(
            "Distance: " +
            direction.magnitude
        );


        // --------------------------------------------------------
        // NEW FLIGHT TIME
        // --------------------------------------------------------

        float distance =
            Vector3.Distance(
                startPosition,
                targetPosition
            );


        flightTime =
            Mathf.Max(
                0.01f,
                distance / speed
            );


        elapsedTime =
            0f;


        DebugLog(
            "Flight time: " +
            flightTime
        );


        // ========================================================
        // REFRESH ROTATION
        // ========================================================

        RefreshRotation();


        // --------------------------------------------------------
        // START MOVEMENT
        // --------------------------------------------------------

        initialized =
            true;


        DebugLog(
            "Movement started."
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


        // --------------------------------------------------------
        // ADVANCE TIME
        // --------------------------------------------------------

        elapsedTime +=
            Time.deltaTime;


        // --------------------------------------------------------
        // PROGRESS
        // --------------------------------------------------------

        float t =
            Mathf.Clamp01(
                elapsedTime /
                flightTime
            );


        // --------------------------------------------------------
        // STRAIGHT MOVEMENT
        // --------------------------------------------------------

        transform.position =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );


        // --------------------------------------------------------
        // DEBUG MOVEMENT
        // --------------------------------------------------------

        if (debugMovement)
        {
            DebugLog(
                "Movement | " +
                "t=" +
                t.ToString("F3") +
                " | position=" +
                transform.position +
                " | target=" +
                targetPosition
            );
        }


        // --------------------------------------------------------
        // DEBUG LINE
        // --------------------------------------------------------

        if (drawDebugLine)
        {
            Debug.DrawLine(
                transform.position,
                targetPosition,
                Color.yellow
            );
        }


        // --------------------------------------------------------
        // KEEP FACING CURRENT FLYING DIRECTION
        // --------------------------------------------------------

        RefreshRotation();


        // --------------------------------------------------------
        // ARRIVED
        // --------------------------------------------------------

        if (t >= 1f)
        {
            DebugLog(
                "ARRIVED AT TARGET: " +
                (
                    currentTargetIndex <
                    chainTargets.Count
                        ? chainTargets[
                            currentTargetIndex
                        ]?.name
                        : "UNKNOWN"
                )
            );

            HitTarget();
        }
    }


    // ============================================================
    // REFRESH ROTATION
    // ============================================================

    private void RefreshRotation()
    {
        Vector3 direction =
            targetPosition -
            transform.position;


        if (
            direction.sqrMagnitude <=
            0.0001f
        )
        {
            DebugLogRotation(
                "Rotation skipped - direction is almost zero."
            );

            return;
        }


        direction.Normalize();


        // --------------------------------------------------------
        // CALCULATE ANGLE
        // --------------------------------------------------------

        float rawAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;


        // --------------------------------------------------------
        // SPRITE OFFSET
        //
        // Sprite points DOWN by default.
        // Therefore +90 degrees is applied.
        // --------------------------------------------------------

        float finalAngle =
            rawAngle +
            90f;


        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                finalAngle
            );


        // --------------------------------------------------------
        // DEBUG ROTATION
        // --------------------------------------------------------

        DebugLogRotation(
            "Rotation | " +
            "direction=" +
            direction +
            " | rawAngle=" +
            rawAngle.ToString("F2") +
            " | finalAngle=" +
            finalAngle.ToString("F2") +
            " | target=" +
            targetPosition
        );


        // --------------------------------------------------------
        // ROTATE SPRITE
        // --------------------------------------------------------

        if (spriteTransform != null)
        {
            spriteTransform.rotation =
                rotation;


            DebugLogRotation(
                "Applied rotation to spriteTransform: " +
                spriteTransform.name +
                " | worldEuler=" +
                spriteTransform.rotation.eulerAngles
            );
        }
        else
        {
            transform.rotation =
                rotation;


            DebugLogRotation(
                "WARNING: spriteTransform is NULL. " +
                "Applied rotation to projectile root instead."
            );
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


        // --------------------------------------------------------
        // STOP CURRENT FLIGHT
        // --------------------------------------------------------

        initialized =
            false;


        // --------------------------------------------------------
        // GET CURRENT TARGET
        // --------------------------------------------------------

        GameObject target =
            null;


        if (
            currentTargetIndex >= 0 &&
            currentTargetIndex <
            chainTargets.Count
        )
        {
            target =
                chainTargets[
                    currentTargetIndex
                ];
        }


        DebugLog(
            "HIT TARGET | index=" +
            currentTargetIndex +
            " | target=" +
            (target != null
                ? target.name
                : "NULL")
        );


        // --------------------------------------------------------
        // DAMAGE
        // --------------------------------------------------------

        if (target != null)
        {
            DealDamage(
                target
            );
        }


        // --------------------------------------------------------
        // MOVE TO NEXT TARGET
        // --------------------------------------------------------

        currentTargetIndex++;


        DebugLog(
            "Moving to next target. " +
            "New index=" +
            currentTargetIndex
        );


        // --------------------------------------------------------
        // NEXT TARGET
        // --------------------------------------------------------

        if (jumpDelay > 0f)
        {
            DebugLog(
                "Waiting " +
                jumpDelay +
                " seconds before next jump."
            );

            StartCoroutine(
                ContinueChainAfterDelay()
            );
        }
        else
        {
            StartNextTarget();
        }
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


        HealthManager health =
            target.GetComponent<
                HealthManager
            >();


        if (health == null)
        {
            Debug.LogWarning(
                "[ChainLightningProjectile] Target " +
                target.name +
                " has no HealthManager."
            );

            return;
        }


        DebugLog(
            "Applying " +
            damage +
            " damage to " +
            target.name
        );


        health.TakeDamage(
            damage
        );
    }


    // ============================================================
    // CONTINUE CHAIN
    // ============================================================

    private IEnumerator ContinueChainAfterDelay()
    {
        yield return new WaitForSeconds(
            jumpDelay
        );


        DebugLog(
            "Jump delay finished."
        );


        StartNextTarget();
    }


    // ============================================================
    // DEBUG LOG
    // ============================================================

    private void DebugLog(
        string message)
    {
        if (!debugLogs)
        {
            return;
        }


        Debug.Log(
            "[ChainLightningProjectile] " +
            message,
            gameObject
        );
    }


    // ============================================================
    // DEBUG ROTATION LOG
    // ============================================================

    private void DebugLogRotation(
        string message)
    {
        if (!debugLogs ||
            !debugRotation)
        {
            return;
        }


        Debug.Log(
            "[ChainLightningProjectile][ROTATION] " +
            message,
            gameObject
        );
    }
}