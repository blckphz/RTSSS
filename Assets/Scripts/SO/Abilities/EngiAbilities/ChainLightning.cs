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
    private string abilitySpawnPointName = "AbilitySpawnPoint";


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = false;


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
                "[ChainLightning] GridManager not found."
            );

            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[ChainLightning] Projectile prefab is missing."
            );

            return false;
        }

        List<GameObject> chain =
            GetChainPreview(
                user,
                target,
                gridManager
            );

        if (chain == null || chain.Count == 0)
        {
            DebugLog("No valid chain.");
            return false;
        }

        Transform spawnPoint =
            FindAbilitySpawnPoint(user);

        Vector3 spawnPosition =
            spawnPoint != null
                ? spawnPoint.position
                : user.transform.position;

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

        ChainLightningProjectile projectileComponent =
            projectile.GetComponent<ChainLightningProjectile>();

        if (projectileComponent == null)
        {
            Debug.LogError(
                "[ChainLightning] Projectile prefab requires " +
                "ChainLightningProjectile."
            );

            Destroy(projectile);
            return false;
        }

        projectileComponent.Initialize(
            user,
            chain,
            projectileSpeed,
            jumpDelay,
            GetDamage()
        );

        DebugLog(
            $"Chain created. Targets={chain.Count}"
        );

        return true;
    }


    // ============================================================
    // SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(
        GameObject user)
    {
        if (user == null)
        {
            return null;
        }

        Transform[] children =
            user.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null)
            {
                continue;
            }

            if (child.name == abilitySpawnPointName)
            {
                return child;
            }
        }

        return null;
    }


    // ============================================================
    // CHAIN PREVIEW
    // ============================================================

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

        int jump = 0;

        while (
            currentTarget != null &&
            jump < maxJumps
        )
        {
            if (!IsValidChainTarget(
                    user,
                    currentTarget,
                    hitTargets))
            {
                break;
            }

            hitTargets.Add(currentTarget);
            chain.Add(currentTarget);

            DebugLog(
                $"Chain {jump + 1}: {currentTarget.name}"
            );

            currentTarget =
                FindClosestEnemy(
                    user,
                    currentTarget,
                    gridManager,
                    hitTargets
                );

            jump++;
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

        float closestDistance =
            float.MaxValue;

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < allUnits.Length; i++)
        {
            AttackUnit candidateUnit =
                allUnits[i];

            if (candidateUnit == null)
            {
                continue;
            }

            GameObject candidate =
                candidateUnit.gameObject;

            if (candidate == null)
            {
                continue;
            }

            if (candidate == user)
            {
                continue;
            }

            if (!candidate.activeInHierarchy)
            {
                continue;
            }

            if (candidateUnit.IsDead())
            {
                continue;
            }

            if (hitTargets.Contains(candidate))
            {
                continue;
            }

            if (!CanTargetObject(
                    user,
                    candidate))
            {
                continue;
            }

            Vector2Int candidatePosition =
                gridManager.GetUnitGridPosition(
                    candidate
                );

            // ----------------------------------------------------
            // REAL GRID DISTANCE
            //
            // Do NOT use Manhattan distance here.
            // Chain Lightning should select the physically
            // closest unit on the grid.
            // ----------------------------------------------------

            Vector2 difference =
                candidatePosition - origin;

            float distance =
                difference.magnitude;

            if (
                maxJumpDistance > 0f &&
                distance > maxJumpDistance
            )
            {
                continue;
            }

            // ----------------------------------------------------
            // CLOSEST TARGET
            // ----------------------------------------------------

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = candidate;
            }
            else if (
                Mathf.Approximately(
                    distance,
                    closestDistance
                )
            )
            {
                // Stable tie breaker.
                //
                // This prevents the chain from changing randomly
                // when two targets are exactly the same distance.
                if (
                    closestEnemy == null ||
                    candidate.GetInstanceID() <
                    closestEnemy.GetInstanceID()
                )
                {
                    closestEnemy = candidate;
                }
            }
        }

        if (closestEnemy != null)
        {
            DebugLog(
                $"Next target: {closestEnemy.name} " +
                $"distance={closestDistance:0.00}"
            );
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

        AttackUnit attackUnit =
            target.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            return false;
        }

        if (attackUnit.IsDead())
        {
            return false;
        }

        return CanTargetObject(
            user,
            target
        );
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log(
            $"[ChainLightning] {message}"
        );
    }
}