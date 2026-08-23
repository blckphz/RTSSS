using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ChainLightning",
    menuName = "Abilities/Rusty/Chain Lightning"
)]
public class ChainLightning : AbilitySO
{
    [Header("Chain Lightning")]

    [SerializeField, Min(1)]
    private int maxJumps = 5;

    [SerializeField, Min(0f)]
    private float jumpDelay = 0.15f;

    [SerializeField, Min(0f)]
    private float maxJumpDistance = 4f;


    // ============================================================
    // USE
    // ============================================================

    public override bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null || target == null)
        {
            return false;
        }

        if (!CanUseAfterMovement(user))
        {
            return false;
        }

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return false;
        }

        if (!CanTargetObject(user, target))
        {
            return false;
        }

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "[ChainLightning] No GridManager found!"
            );

            return false;
        }

        MonoBehaviour runner =
            user.GetComponent<MonoBehaviour>();

        if (runner == null)
        {
            return false;
        }

        runner.StartCoroutine(
            ChainRoutine(
                user,
                target,
                gridManager
            )
        );

        return true;
    }


    // ============================================================
    // CHAIN ROUTINE
    // ============================================================

    private IEnumerator ChainRoutine(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager)
    {
        GameObject currentTarget =
            firstTarget;

        HashSet<GameObject> hitTargets =
            new HashSet<GameObject>();

        int jumps = 0;


        while (
            currentTarget != null &&
            jumps < maxJumps
        )
        {
            // ----------------------------------------------------
            // MAKE SURE TARGET IS STILL VALID
            // ----------------------------------------------------

            if (!IsValidChainTarget(
                    user,
                    currentTarget,
                    hitTargets))
            {
                yield break;
            }


            // ----------------------------------------------------
            // REMEMBER TARGET
            // ----------------------------------------------------

            hitTargets.Add(currentTarget);


            // ----------------------------------------------------
            // DEAL DAMAGE
            // ----------------------------------------------------

            DealDamage(
                currentTarget
            );


            jumps++;


            // ----------------------------------------------------
            // WAIT FOR THE HIT
            // ----------------------------------------------------

            if (jumpDelay > 0f)
            {
                yield return new WaitForSeconds(
                    jumpDelay
                );
            }


            // ----------------------------------------------------
            // FIND NEXT TARGET
            // ----------------------------------------------------

            currentTarget =
                FindClosestEnemy(
                    user,
                    currentTarget,
                    gridManager,
                    hitTargets
                );
        }
    }


    // ============================================================
    // PREVIEW
    // ============================================================

    /*
     * Returns the exact chain of enemies that Chain Lightning
     * would currently hit if it started on firstTarget.
     *
     * This uses the same FindClosestEnemy() logic as the
     * actual ability, so the preview and attack stay synchronized.
     */

    public List<GameObject> GetChainPreview(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager)
    {
        List<GameObject> chain =
            new List<GameObject>();

        if (
            user == null ||
            firstTarget == null ||
            gridManager == null
        )
        {
            return chain;
        }


        HashSet<GameObject> hitTargets =
            new HashSet<GameObject>();

        GameObject currentTarget =
            firstTarget;

        int jumps = 0;


        while (
            currentTarget != null &&
            jumps < maxJumps
        )
        {
            // ----------------------------------------------------
            // MAKE SURE TARGET IS VALID
            // ----------------------------------------------------

            if (!IsValidChainTarget(
                    user,
                    currentTarget,
                    hitTargets))
            {
                break;
            }


            // ----------------------------------------------------
            // REMEMBER TARGET
            // ----------------------------------------------------

            hitTargets.Add(
                currentTarget
            );


            // ----------------------------------------------------
            // ADD TO PREVIEW
            // ----------------------------------------------------

            chain.Add(
                currentTarget
            );


            jumps++;


            // ----------------------------------------------------
            // FIND NEXT TARGET
            // ----------------------------------------------------

            currentTarget =
                FindClosestEnemy(
                    user,
                    currentTarget,
                    gridManager,
                    hitTargets
                );
        }


        return chain;
    }


    // ============================================================
    // FIND CLOSEST ENEMY
    // ============================================================

    private GameObject FindClosestEnemy(
        GameObject user,
        GameObject currentTarget,
        GridManager gridManager,
        HashSet<GameObject> hitTargets)
    {
        if (
            user == null ||
            currentTarget == null ||
            gridManager == null
        )
        {
            return null;
        }


        Vector2Int origin =
            gridManager.GetUnitGridPosition(
                currentTarget
            );


        GameObject closestEnemy = null;

        int closestDistance =
            int.MaxValue;


        // --------------------------------------------------------
        // CHECK EVERY CELL
        // --------------------------------------------------------

        for (
            int x = 0;
            x < gridManager.GetWidth();
            x++
        )
        {
            for (
                int y = 0;
                y < gridManager.GetHeight();
                y++
            )
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


                GameObject candidate =
                    gridManager.GetUnitAt(
                        position
                    );


                if (candidate == null)
                {
                    continue;
                }


                // ------------------------------------------------
                // DON'T HIT SAME UNIT TWICE
                // ------------------------------------------------

                if (hitTargets.Contains(candidate))
                {
                    continue;
                }


                // ------------------------------------------------
                // MUST BE A VALID ENEMY
                // ------------------------------------------------

                if (!CanTargetObject(
                        user,
                        candidate))
                {
                    continue;
                }


                // ------------------------------------------------
                // DISTANCE
                // ------------------------------------------------

                int distance =
                    gridManager.GetDistance(
                        origin,
                        position
                    );


                if (
                    maxJumpDistance > 0f &&
                    distance > maxJumpDistance
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // CLOSEST
                // ------------------------------------------------

                if (distance < closestDistance)
                {
                    closestDistance =
                        distance;

                    closestEnemy =
                        candidate;
                }
            }
        }


        return closestEnemy;
    }


    // ============================================================
    // VALID TARGET
    // ============================================================

    private bool IsValidChainTarget(
        GameObject user,
        GameObject target,
        HashSet<GameObject> hitTargets)
    {
        if (
            user == null ||
            target == null
        )
        {
            return false;
        }


        if (hitTargets.Contains(target))
        {
            return false;
        }


        if (!target.activeInHierarchy)
        {
            return false;
        }


        return CanTargetObject(
            user,
            target
        );
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
            target.GetComponent<HealthManager>();


        if (health == null)
        {
            return;
        }


        // --------------------------------------------------------
        // CHANGE THIS IF YOUR HEALTHMANAGER USES A DIFFERENT API
        // --------------------------------------------------------

        health.TakeDamage(
            GetDamage()
        );
    }
}