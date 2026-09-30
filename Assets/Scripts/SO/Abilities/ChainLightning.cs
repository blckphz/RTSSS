using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "ChainLightning",
    menuName = "Combat/Abilities/Chain Lightning"
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
    // EFFECTIVE JUMPS
    // ============================================================

    private int GetEffectiveMaxJumps(
        GameObject user
    )
    {
        int bonusJumps = 0;

        if (user != null)
        {
            UnitData unitData =
                user.GetComponent<UnitData>();

            if (unitData == null)
            {
                unitData =
                    user.GetComponentInParent<UnitData>();
            }

            if (unitData == null)
            {
                unitData =
                    user.GetComponentInChildren<UnitData>();
            }

            if (unitData != null)
            {
                bonusJumps =
                    unitData.GetBonusJumps(this);
            }
        }

        return Mathf.Max(
            1,
            maxJumps + bonusJumps
        );
    }


    // ============================================================
    // CAN HIT
    // ============================================================

    public override bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target)
    {
        if (
            gridManager == null ||
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


        // ========================================================
        // TURRET
        // ========================================================

        turretbehav turret =
            target.GetComponent<turretbehav>();

        if (turret != null)
        {
            /*
             * The turret is ONLY a valid Chain Lightning
             * target after the Turret Charge upgrade has
             * been purchased.
             */
            if (!turret.IsChainLightningUnlocked())
            {
                return false;
            }
        }


        // ========================================================
        // ATTACK UNIT
        // ========================================================

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return false;
        }

        if (targetUnit.IsDead())
        {
            return false;
        }


        // ========================================================
        // NORMAL TARGET VALIDATION
        // ========================================================

        bool validTarget =
            CanTargetObject(
                user,
                target
            );


        // ========================================================
        // PLAYER TARGET
        // ========================================================

        /*
         * Chain Lightning can target a Player-team object
         * specifically when it is an unlocked turret.
         */
        if (!validTarget)
        {
            if (targetUnit.GetTeam() == Team.Player)
            {
                turretbehav playerTurret =
                    target.GetComponent<turretbehav>();

                if (
                    playerTurret != null &&
                    playerTurret.IsChainLightningUnlocked()
                )
                {
                    validTarget = true;
                }
            }
        }

        if (!validTarget)
        {
            return false;
        }


        // ========================================================
        // INITIAL LINE OF SIGHT
        // ========================================================

        Vector2Int userPosition =
            gridManager.GetUnitGridPosition(user);

        Vector2Int targetPosition =
            gridManager.GetUnitGridPosition(target);

        if (
            HasObjectOnLine(
                gridManager,
                userPosition,
                targetPosition
            )
        )
        {
            return false;
        }


        // ========================================================
        // RANGE
        // ========================================================

        return CanHitTile(
            gridManager,
            user,
            targetPosition
        );
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


        // ========================================================
        // TURRET
        // ========================================================

        turretbehav turret =
            target.GetComponent<turretbehav>();

        if (turret != null)
        {
            /*
             * Locked turrets cannot be targeted.
             */
            if (!turret.IsChainLightningUnlocked())
            {
                return false;
            }
        }


        // ========================================================
        // ATTACK UNIT
        // ========================================================

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return false;
        }

        if (targetUnit.IsDead())
        {
            return false;
        }


        // ========================================================
        // TARGET VALIDATION
        // ========================================================

        if (
            !CanTargetObject(
                user,
                target
            )
        )
        {
            /*
             * Player targets are allowed ONLY if they
             * are an unlocked turret.
             */
            if (
                targetUnit.GetTeam() != Team.Player ||
                turret == null ||
                !turret.IsChainLightningUnlocked()
            )
            {
                return false;
            }
        }


        // ========================================================
        // GRID
        // ========================================================

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
        // INITIAL LINE OF SIGHT
        // ========================================================

        Vector2Int userPosition =
            gridManager.GetUnitGridPosition(user);

        Vector2Int targetPosition =
            gridManager.GetUnitGridPosition(target);

        if (
            HasObjectOnLine(
                gridManager,
                userPosition,
                targetPosition
            )
        )
        {
            return false;
        }


        // ========================================================
        // EFFECTIVE JUMP COUNT
        // ========================================================

        int effectiveMaxJumps =
            GetEffectiveMaxJumps(user);


        // ========================================================
        // CREATE CHAIN
        // ========================================================

        List<GameObject> chain =
            GetChainPreview(
                user,
                target,
                gridManager,
                effectiveMaxJumps
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

        ChainLightningProjectile projectileComponent =
            projectile.GetComponent<
                ChainLightningProjectile>();

        if (projectileComponent == null)
        {
            Destroy(projectile);
            return false;
        }


        // ========================================================
        // TURRET CHARGES
        // ========================================================

        int turretCharges =
            GetTurretCharges(
                chain,
                effectiveMaxJumps
            );


        // ========================================================
        // INITIALIZE PROJECTILE
        // ========================================================

        projectileComponent.Initialize(
            user,
            chain,
            projectileSpeed,
            GetEffectiveDamage(user),
            stunPercentage,
            stunDuration,
            turretCharges
        );

        return true;
    }


    // ============================================================
    // TURRET CHARGES
    // ============================================================

    private int GetTurretCharges(
        List<GameObject> chain,
        int effectiveMaxJumps)
    {
        if (
            chain == null ||
            chain.Count == 0
        )
        {
            return 0;
        }

        for (
            int i = 0;
            i < chain.Count;
            i++
        )
        {
            GameObject target =
                chain[i];

            if (target == null)
            {
                continue;
            }

            turretbehav turret =
                target.GetComponent<
                    turretbehav>();

            if (turret == null)
            {
                continue;
            }


            // ====================================================
            // ONLY UNLOCKED TURRETS CAN RECEIVE CHARGES
            // ====================================================

            if (!turret.IsChainLightningUnlocked())
            {
                continue;
            }


            int remainingBounces =
                effectiveMaxJumps - i;

            return Mathf.Max(
                1,
                remainingBounces
            );
        }

        return 0;
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

        Transform spawnPoint =
            user.transform.Find(
                abilitySpawnPointName
            );

        if (spawnPoint != null)
        {
            return spawnPoint;
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
            if (
                children[i].name ==
                abilitySpawnPointName
            )
            {
                return children[i];
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
        int effectiveMaxJumps =
            GetEffectiveMaxJumps(user);

        return GetChainPreview(
            user,
            firstTarget,
            gridManager,
            effectiveMaxJumps
        );
    }


    public List<GameObject> GetChainPreview(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager,
        int effectiveMaxJumps)
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

        effectiveMaxJumps =
            Mathf.Max(
                1,
                effectiveMaxJumps
            );

        HashSet<GameObject> hitTargets =
            new HashSet<GameObject>();

        GameObject currentTarget =
            firstTarget;

        int jump = 0;

        int maximumJumps =
            effectiveMaxJumps;


        // ========================================================
        // BUILD CHAIN
        // ========================================================

        while (
            currentTarget != null &&
            jump < maximumJumps
        )
        {
            bool validTarget;

            if (jump == 0)
            {
                validTarget =
                    IsValidInitialTarget(
                        user,
                        currentTarget,
                        gridManager,
                        hitTargets
                    );
            }
            else
            {
                validTarget =
                    IsValidChainTarget(
                        user,
                        currentTarget,
                        hitTargets
                    );
            }

            if (!validTarget)
            {
                break;
            }


            // ====================================================
            // TURRET
            // ====================================================

            turretbehav turret =
                currentTarget.GetComponent<
                    turretbehav>();

            if (turret != null)
            {
                /*
                 * Only an unlocked turret can ever reach
                 * this point.
                 */

                if (!turret.IsChainLightningUnlocked())
                {
                    break;
                }

                chain.Add(
                    currentTarget
                );

                break;
            }


            // ====================================================
            // NORMAL TARGET
            // ====================================================

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
    // INITIAL TARGET VALIDATION
    // ============================================================

    private bool IsValidInitialTarget(
        GameObject user,
        GameObject target,
        GridManager gridManager,
        HashSet<GameObject> hitTargets)
    {
        if (
            user == null ||
            target == null ||
            gridManager == null
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


        // ========================================================
        // TURRET
        // ========================================================

        turretbehav turret =
            target.GetComponent<turretbehav>();

        if (turret != null)
        {
            /*
             * LOCKED TURRET = NOT A TARGET
             */
            if (!turret.IsChainLightningUnlocked())
            {
                return false;
            }
        }


        // ========================================================
        // ATTACK UNIT
        // ========================================================

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return false;
        }

        if (targetUnit.IsDead())
        {
            return false;
        }


        // ========================================================
        // NORMAL ENEMY
        // ========================================================

        bool validTarget =
            CanTargetObject(
                user,
                target
            );


        // ========================================================
        // UNLOCKED PLAYER TURRET
        // ========================================================

        if (!validTarget)
        {
            if (
                targetUnit.GetTeam() ==
                Team.Player
            )
            {
                if (
                    turret != null &&
                    turret.IsChainLightningUnlocked()
                )
                {
                    validTarget = true;
                }
            }
        }

        if (!validTarget)
        {
            return false;
        }


        // ========================================================
        // INITIAL LINE OF SIGHT
        // ========================================================

        Vector2Int userTile =
            gridManager.GetUnitGridPosition(user);

        Vector2Int targetTile =
            gridManager.GetUnitGridPosition(target);

        /*
         * Unlike the later bounce checks, the initial target
         * must also have clear line of sight from the player.
         *
         * Any occupied tile between the player and the target
         * blocks the Chain Lightning.
         */
        if (
            HasObjectOnLine(
                gridManager,
                userTile,
                targetTile
            )
        )
        {
            return false;
        }

        return true;
    }


    // ============================================================
    // CHAIN TARGET VALIDATION
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


        // ========================================================
        // TURRET
        // ========================================================

        turretbehav turret =
            target.GetComponent<turretbehav>();

        if (turret != null)
        {
            /*
             * Locked turret cannot be selected by the chain.
             */
            if (!turret.IsChainLightningUnlocked())
            {
                return false;
            }

            /*
             * An unlocked turret is allowed as the final
             * destination of the chain.
             */
            AttackUnit turretUnit =
                target.GetComponent<AttackUnit>();

            if (turretUnit == null)
            {
                return false;
            }

            if (turretUnit.IsDead())
            {
                return false;
            }

            return true;
        }


        // ========================================================
        // NORMAL TARGET
        // ========================================================

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            return false;
        }

        if (targetUnit.IsDead())
        {
            return false;
        }


        return CanTargetObject(
            user,
            target
        );
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

        Vector2Int currentTile =
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


            // ====================================================
            // TURRET
            // ====================================================

            turretbehav candidateTurret =
                candidate.GetComponent<
                    turretbehav>();

            if (candidateTurret != null)
            {
                /*
                 * IMPORTANT:
                 *
                 * Locked turret is completely invisible
                 * to Chain Lightning targeting.
                 */
                if (
                    !candidateTurret
                        .IsChainLightningUnlocked()
                )
                {
                    continue;
                }

                /*
                 * An unlocked turret is a special final
                 * destination, not a normal enemy.
                 *
                 * We can return it as a possible target.
                 */
                Vector2Int turretTile =
                    gridManager.GetUnitGridPosition(
                        candidate
                    );

                float turretDistance =
                    Vector2.Distance(
                        currentTile,
                        turretTile
                    );

                if (
                    maxJumpDistance > 0f &&
                    turretDistance > maxJumpDistance
                )
                {
                    continue;
                }

                if (
                    HasObjectOnLine(
                        gridManager,
                        currentTile,
                        turretTile
                    )
                )
                {
                    continue;
                }

                if (
                    closestEnemy == null ||
                    turretDistance < closestDistance
                )
                {
                    closestEnemy =
                        candidate;

                    closestDistance =
                        turretDistance;
                }

                continue;
            }


            // ====================================================
            // NORMAL ENEMY
            // ====================================================

            if (
                !CanTargetObject(
                    user,
                    candidate
                )
            )
            {
                continue;
            }

            Vector2Int candidateTile =
                gridManager.GetUnitGridPosition(
                    candidate
                );

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
    // LINE OF SIGHT
    // ============================================================

    private bool HasObjectOnLine(
        GridManager gridManager,
        Vector2Int start,
        Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;

        int x1 = end.x;
        int y1 = end.y;

        int dx =
            Mathf.Abs(
                x1 - x0
            );

        int dy =
            Mathf.Abs(
                y1 - y0
            );

        int sx =
            x0 < x1
                ? 1
                : -1;

        int sy =
            y0 < y1
                ? 1
                : -1;

        int err =
            dx - dy;

        while (true)
        {
            if (
                x0 == x1 &&
                y0 == y1
            )
            {
                break;
            }

            /*
             * Don't check the starting tile because it contains
             * the unit casting Chain Lightning.
             */
            if (!(
                x0 == start.x &&
                y0 == start.y
            ))
            {
                Vector2Int tile =
                    new Vector2Int(
                        x0,
                        y0
                    );

                GameObject occupied =
                    gridManager.GetUnitAt(
                        tile
                    );

                if (occupied != null)
                {
                    return true;
                }
            }

            int e2 =
                2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return false;
    }


    // ============================================================
    // STUN GETTERS
    // ============================================================

    public float GetStunPercentage()
    {
        return stunPercentage;
    }


    public int GetStunDuration()
    {
        return stunDuration;
    }
}

