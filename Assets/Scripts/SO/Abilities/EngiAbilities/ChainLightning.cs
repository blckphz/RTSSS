using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    private float jumpDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float maxJumpDistance = 4f;


    [Header("Stun")]

    [FormerlySerializedAs("stunprercenatge")]
    [SerializeField, Range(0f, 100f)]
    private float stunPercentage = 0f;

    [FormerlySerializedAs("stunduration")]
    [SerializeField, Min(0)]
    private int stunDuration = 1;


    [Header("Projectile")]

    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 12f;

    [SerializeField]
    private string abilitySpawnPointName =
        "AbilitySpawnPoint";


    // ============================================================
    // JUMPS
    // ============================================================

    public int GetBaseMaxJumps()
    {
        return maxJumps;
    }


    public int GetBonusJumps(UnitData unitData)
    {
        if (unitData == null)
        {
            return 0;
        }

        return unitData.GetBonusJumps(this);
    }


    public int GetMaxJumps(UnitData unitData)
    {
        int bonus = GetBonusJumps(unitData);

        int total =
            maxJumps +
            bonus;

        Debug.Log(
            "[ChainLightning] " +
            "GetMaxJumps | " +
            "Ability=" + name +
            " | ID=" + GetInstanceID() +
            " | Unit=" +
            (unitData != null
                ? unitData.name
                : "NULL") +
            " | Base=" + maxJumps +
            " | Bonus=" + bonus +
            " | Total=" + total
        );

        return total;
    }


    // ============================================================
    // STUN
    // ============================================================

    public float GetStunPercentage()
    {
        return stunPercentage;
    }


    public int GetStunDuration()
    {
        return stunDuration;
    }


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
                "[ChainLightning] " +
                "GridManager not found."
            );

            return false;
        }


        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[ChainLightning] " +
                "Projectile prefab is missing."
            );

            return false;
        }


        // ========================================================
        // GET UNIT DATA
        // ========================================================

        UnitData unitData =
            FindUnitData(user);


        if (unitData == null)
        {
            Debug.LogWarning(
                "[ChainLightning] " +
                "UnitData not found on " +
                user.name +
                ". Using base jump count."
            );
        }
        else
        {
            Debug.Log(
                "[ChainLightning] " +
                "Using UnitData: " +
                unitData.name +
                " | ID=" +
                unitData.GetInstanceID() +
                " | Bonus=" +
                GetBonusJumps(unitData)
            );
        }


        // ========================================================
        // BUILD CHAIN
        // ========================================================

        List<GameObject> chain =
            GetChainPreview(
                user,
                target,
                gridManager,
                unitData
            );


        if (
            chain == null ||
            chain.Count == 0
        )
        {
            return false;
        }


        Debug.Log(
            "[ChainLightning] " +
            "Chain created with " +
            chain.Count +
            " target(s)."
        );


        // ========================================================
        // SPAWN POINT
        // ========================================================

        Transform spawnPoint =
            FindAbilitySpawnPoint(user);


        Vector3 spawnPosition =
            spawnPoint != null
                ? spawnPoint.position
                : user.transform.position;


        // ========================================================
        // PROJECTILE
        // ========================================================

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


        ChainLightningProjectile
            projectileComponent =
                projectile.GetComponent<
                    ChainLightningProjectile
                >();


        if (projectileComponent == null)
        {
            Debug.LogError(
                "[ChainLightning] " +
                "Projectile prefab requires " +
                "ChainLightningProjectile."
            );

            Destroy(projectile);

            return false;
        }


        projectileComponent.Initialize(
            user,
            chain,
            projectileSpeed,
            GetDamage(),
            stunPercentage,
            stunDuration
        );


        return true;
    }


    // ============================================================
    // FIND UNIT DATA
    // ============================================================

    private UnitData FindUnitData(
        GameObject user)
    {
        if (user == null)
        {
            return null;
        }


        UnitData unitData =
            user.GetComponent<UnitData>();


        if (unitData != null)
        {
            return unitData;
        }


        unitData =
            user.GetComponentInChildren<UnitData>();


        if (unitData != null)
        {
            return unitData;
        }


        unitData =
            user.GetComponentInParent<UnitData>();


        return unitData;
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
    // CHAIN PREVIEW
    // ============================================================

    public List<GameObject> GetChainPreview(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager)
    {
        UnitData unitData =
            FindUnitData(user);


        return GetChainPreview(
            user,
            firstTarget,
            gridManager,
            unitData
        );
    }


    public List<GameObject> GetChainPreview(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager,
        UnitData unitData)
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


        int maximumJumps =
            GetMaxJumps(unitData);


        while (
            currentTarget != null &&
            jump < maximumJumps
        )
        {
            if (
                !IsValidChainTarget(
                    user,
                    currentTarget,
                    hitTargets
                )
            )
            {
                break;
            }


            hitTargets.Add(
                currentTarget
            );


            chain.Add(
                currentTarget
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


        for (
            int i = 0;
            i < allUnits.Length;
            i++
        )
        {
            AttackUnit candidateUnit =
                allUnits[i];


            if (candidateUnit == null)
            {
                continue;
            }


            GameObject candidate =
                candidateUnit.gameObject;


            if (
                candidate == null ||
                candidate == user ||
                !candidate.activeInHierarchy ||
                candidateUnit.IsDead() ||
                hitTargets.Contains(candidate)
            )
            {
                continue;
            }


            if (
                !CanTargetObject(
                    user,
                    candidate
                )
            )
            {
                continue;
            }


            Vector2Int candidatePosition =
                gridManager.GetUnitGridPosition(
                    candidate
                );


            float distance =
                Vector2.Distance(
                    origin,
                    candidatePosition
                );


            if (
                maxJumpDistance > 0f &&
                distance > maxJumpDistance
            )
            {
                continue;
            }


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
            else if (
                Mathf.Approximately(
                    distance,
                    closestDistance
                )
            )
            {
                if (
                    closestEnemy == null ||
                    candidate.GetInstanceID() <
                    closestEnemy.GetInstanceID()
                )
                {
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
            target == null ||
            hitTargets.Contains(target) ||
            !target.activeInHierarchy
        )
        {
            return false;
        }


        AttackUnit attackUnit =
            target.GetComponent<AttackUnit>();


        if (
            attackUnit == null ||
            attackUnit.IsDead()
        )
        {
            return false;
        }


        return CanTargetObject(
            user,
            target
        );
    }
}