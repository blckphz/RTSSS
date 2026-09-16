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
        int bonus =
            GetBonusJumps(unitData);

        int total =
            maxJumps + bonus;

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
            return false;
        }


        if (projectilePrefab == null)
        {
            return false;
        }


        // ========================================================
        // UNIT DATA
        // ========================================================

        UnitData unitData =
            FindUnitData(user);


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
    // FIND SPAWN POINT
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


        // ========================================================
        // PLAYER -> FIRST ENEMY
        // ========================================================

        Vector2Int playerTile =
            gridManager.GetUnitGridPosition(
                user
            );


        Vector2Int firstEnemyTile =
            gridManager.GetUnitGridPosition(
                firstTarget
            );


        if (
            HasObjectOnLine(
                gridManager,
                playerTile,
                firstEnemyTile
            )
        )
        {
            return chain;
        }


        // ========================================================
        // CHAIN
        // ========================================================

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
            // ----------------------------------------------------
            // VALID TARGET
            // ----------------------------------------------------

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


            // ----------------------------------------------------
            // ADD TARGET
            // ----------------------------------------------------

            hitTargets.Add(
                currentTarget
            );


            chain.Add(
                currentTarget
            );


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


        // ========================================================
        // CURRENT TARGET TILE
        // ========================================================

        Vector2Int currentTile =
            gridManager.GetUnitGridPosition(
                currentTarget
            );


        GameObject closestEnemy =
            null;


        float closestDistance =
            float.MaxValue;


        // ========================================================
        // ALL ENEMIES
        // ========================================================

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


            if (candidate == null)
            {
                continue;
            }


            // Don't target the player.
            if (candidate == user)
            {
                continue;
            }


            // Must be active.
            if (!candidate.activeInHierarchy)
            {
                continue;
            }


            // Must be alive.
            if (candidateUnit.IsDead())
            {
                continue;
            }


            // Don't hit the same enemy twice.
            if (hitTargets.Contains(candidate))
            {
                continue;
            }


            // Must be a valid enemy.
            if (
                !CanTargetObject(
                    user,
                    candidate
                )
            )
            {
                continue;
            }


            // ====================================================
            // CANDIDATE TILE
            // ====================================================

            Vector2Int candidateTile =
                gridManager.GetUnitGridPosition(
                    candidate
                );


            // ====================================================
            // DISTANCE
            // ====================================================

            float distance =
                Vector2.Distance(
                    currentTile,
                    candidateTile
                );


            if (
                maxJumpDistance > 0f &&
                distance > maxJumpDistance
            )
            {
                continue;
            }


            // ====================================================
            // LINE CHECK
            // ====================================================

            if (
                HasObjectOnLine(
                    gridManager,
                    currentTile,
                    candidateTile
                )
            )
            {
                continue;
            }


            // ====================================================
            // CLOSEST VALID ENEMY
            // ====================================================

            if (
                closestEnemy == null ||
                distance < closestDistance
            )
            {
                closestEnemy =
                    candidate;

                closestDistance =
                    distance;
            }
            else if (
                Mathf.Approximately(
                    distance,
                    closestDistance
                )
            )
            {
                if (
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
    // GRID LINE CHECK
    // ============================================================

    private bool HasObjectOnLine(
        GridManager gridManager,
        Vector2Int start,
        Vector2Int end)
    {
        if (gridManager == null)
        {
            return false;
        }


        int x =
            start.x;


        int y =
            start.y;


        int targetX =
            end.x;


        int targetY =
            end.y;


        int deltaX =
            Mathf.Abs(
                targetX - x
            );


        int deltaY =
            Mathf.Abs(
                targetY - y
            );


        int stepX =
            x < targetX
                ? 1
                : -1;


        int stepY =
            y < targetY
                ? 1
                : -1;


        int error =
            deltaX - deltaY;


        while (true)
        {
            // ====================================================
            // REACHED DESTINATION
            // ====================================================

            if (
                x == targetX &&
                y == targetY
            )
            {
                break;
            }


            int error2 =
                error * 2;


            // ====================================================
            // MOVE X
            // ====================================================

            if (
                error2 > -deltaY
            )
            {
                error -= deltaY;
                x += stepX;
            }


            // ====================================================
            // MOVE Y
            // ====================================================

            if (
                error2 < deltaX
            )
            {
                error += deltaX;
                y += stepY;
            }


            Vector2Int tile =
                new Vector2Int(
                    x,
                    y
                );


            // ====================================================
            // DESTINATION IS ALLOWED
            // ====================================================

            if (tile == end)
            {
                break;
            }


            // ====================================================
            // OUTSIDE GRID
            // ====================================================

            if (
                !gridManager.IsInsideGrid(
                    tile
                )
            )
            {
                continue;
            }


            // ====================================================
            // CHECK OBJECT
            // ====================================================

            GameObject objectOnTile =
                gridManager.GetUnitAt(
                    tile
                );


            if (objectOnTile != null)
            {
                return true;
            }
        }


        return false;
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