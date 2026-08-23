using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ChainLightning",
    menuName = "Abilities/Rusty/Chain Lightning"
)]
public class ChainLightning : AbilitySO
{
    // ============================================================
    // CHAIN LIGHTNING
    // ============================================================

    [Header("Chain Lightning")]

    [SerializeField, Min(1)]
    private int maxJumps = 5;

    [SerializeField, Min(0f)]
    private float jumpDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float maxJumpDistance = 4f;


    // ============================================================
    // PROJECTILE
    // ============================================================

    [Header("Projectile")]

    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 12f;

    [SerializeField]
    private string abilitySpawnPointName =
        "AbilitySpawnPoint";


    // ============================================================
    // USE
    // ============================================================

    public override bool Use(
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


        // --------------------------------------------------------
        // MOVEMENT CHECK
        // --------------------------------------------------------

        if (!CanUseAfterMovement(user))
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET UNIT
        // --------------------------------------------------------

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();


        if (targetUnit == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET VALIDATION
        // --------------------------------------------------------

        if (!CanTargetObject(
                user,
                target))
        {
            return false;
        }


        // --------------------------------------------------------
        // GRID
        // --------------------------------------------------------

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();


        if (gridManager == null)
        {
            Debug.LogError(
                "[ChainLightning] No GridManager found!"
            );

            return false;
        }


        // --------------------------------------------------------
        // PROJECTILE PREFAB
        // --------------------------------------------------------

        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[ChainLightning] Projectile Prefab " +
                "has not been assigned!"
            );

            return false;
        }


        // --------------------------------------------------------
        // GET COMPLETE CHAIN
        // --------------------------------------------------------

        List<GameObject> chain =
            GetChainPreview(
                user,
                target,
                gridManager
            );


        if (
            chain == null ||
            chain.Count == 0
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // FIND ABILITY SPAWN POINT
        // --------------------------------------------------------

        Transform spawnPoint =
            FindAbilitySpawnPoint(
                user
            );


        Vector3 spawnPosition;


        if (spawnPoint != null)
        {
            spawnPosition =
                spawnPoint.position;
        }
        else
        {
            Debug.LogWarning(
                "[ChainLightning] Could not find '" +
                abilitySpawnPointName +
                "' on " +
                user.name +
                ". Using unit position."
            );

            spawnPosition =
                user.transform.position;
        }


        // --------------------------------------------------------
        // SPAWN PROJECTILE
        // --------------------------------------------------------

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (projectile == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // GET PROJECTILE COMPONENT
        // --------------------------------------------------------

        ChainLightningProjectile projectileComponent =
            projectile.GetComponent<
                ChainLightningProjectile
            >();


        if (projectileComponent == null)
        {
            Debug.LogError(
                "[ChainLightning] The projectile prefab " +
                "does not contain a " +
                "ChainLightningProjectile component!"
            );

            Destroy(projectile);

            return false;
        }


        // --------------------------------------------------------
        // INITIALIZE PROJECTILE
        // --------------------------------------------------------

        projectileComponent.Initialize(
            user,
            chain,
            projectileSpeed,
            jumpDelay,
            GetDamage()
        );


        return true;
    }


    // ============================================================
    // FIND ABILITY SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(
        GameObject user)
    {
        if (user == null)
        {
            return null;
        }


        Transform[] children =
            user.GetComponentsInChildren<Transform>(
                true
            );


        for (
            int i = 0;
            i < children.Length;
            i++
        )
        {
            Transform child =
                children[i];


            if (child == null)
            {
                continue;
            }


            if (
                child.name ==
                abilitySpawnPointName
            )
            {
                return child;
            }
        }


        return null;
    }


    // ============================================================
    // PREVIEW
    // ============================================================

    /*
     * Returns the exact chain of enemies that Chain Lightning
     * would currently hit if it started on firstTarget.
     *
     * The projectile uses this same list.
     *
     * Example:
     *
     * AbilitySpawnPoint
     *        |
     *        v
     *     Enemy 1
     *        |
     *        v
     *     Enemy 2
     *        |
     *        v
     *     Enemy 3
     *
     * No arc is involved.
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


        // --------------------------------------------------------
        // BUILD CHAIN
        // --------------------------------------------------------

        while (
            currentTarget != null &&
            jumps < maxJumps
        )
        {
            // ----------------------------------------------------
            // VALID TARGET
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
            // ADD TARGET
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


        // --------------------------------------------------------
        // CURRENT GRID POSITION
        // --------------------------------------------------------

        Vector2Int origin =
            gridManager.GetUnitGridPosition(
                currentTarget
            );


        GameObject closestEnemy =
            null;


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

                if (
                    hitTargets.Contains(
                        candidate
                    )
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // MUST BE VALID ENEMY
                // ------------------------------------------------

                if (!CanTargetObject(
                        user,
                        candidate))
                {
                    continue;
                }


                // ------------------------------------------------
                // GRID DISTANCE
                // ------------------------------------------------

                int distance =
                    gridManager.GetDistance(
                        origin,
                        position
                    );


                // ------------------------------------------------
                // MAX JUMP DISTANCE
                // ------------------------------------------------

                if (
                    maxJumpDistance > 0f &&
                    distance > maxJumpDistance
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // CLOSEST TARGET
                // ------------------------------------------------

                if (
                    distance <
                    closestDistance
                )
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
    // VALID CHAIN TARGET
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


        // --------------------------------------------------------
        // ALREADY HIT
        // --------------------------------------------------------

        if (
            hitTargets.Contains(
                target
            )
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // INACTIVE
        // --------------------------------------------------------

        if (!target.activeInHierarchy)
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET VALIDATION
        // --------------------------------------------------------

        return CanTargetObject(
            user,
            target
        );
    }
}