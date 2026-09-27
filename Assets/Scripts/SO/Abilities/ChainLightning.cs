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

        bool validTarget =
            CanTargetObject(
                user,
                target
            );

        /*
         * Chain Lightning can also initially target
         * a Player-team object such as the turret.
         */
        if (!validTarget)
        {
            validTarget =
                targetUnit.GetTeam() ==
                Team.Player;
        }

        if (!validTarget)
        {
            return false;
        }

        Vector2Int targetPosition =
            gridManager.WorldToGridPosition(
                target.transform.position
            );

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

        /*
         * Normal Chain Lightning targets must pass
         * CanTargetObject().
         *
         * Player targets are allowed specifically so
         * the turret can be targeted.
         */
        if (
            !CanTargetObject(
                user,
                target
            ) &&
            targetUnit.GetTeam() !=
            Team.Player
        )
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
            projectile.GetComponent<
                ChainLightningProjectile>();

        if (projectileComponent == null)
        {
            Destroy(projectile);
            return false;
        }

        /*
         * Determine how many bounces remain when
         * the turret is encountered.
         */
        int turretCharges =
            GetTurretCharges(
                chain
            );

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
        List<GameObject> chain)
    {
        if (
            chain == null ||
            chain.Count == 0
        )
        {
            return 0;
        }

        /*
         * Find the turret in the chain.
         *
         * Example with maxJumps = 2:
         *
         * chain:
         *
         * [0] Turret
         *
         * turretIndex = 0
         * remaining = 2
         *
         *
         * chain:
         *
         * [0] Enemy
         * [1] Turret
         *
         * turretIndex = 1
         * remaining = 1
         */

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

            int remainingBounces =
                maxJumps - i;

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
            maxJumps;

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
                 * Add the turret as the final visual
                 * destination.
                 *
                 * IMPORTANT:
                 *
                 * We do NOT calculate another enemy after
                 * the turret.
                 *
                 * The remaining bounce count will later
                 * be converted into turret charges.
                 */

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

        /*
         * Normal enemy target.
         */
        if (
            CanTargetObject(
                user,
                target
            )
        )
        {
            return true;
        }

        /*
         * Special Player target.
         *
         * This allows the turret to be selected
         * as the initial target.
         */
        if (
            targetUnit.GetTeam() ==
            Team.Player
        )
        {
            return true;
        }

        return false;
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

        /*
         * Subsequent normal chain targets remain
         * enemy-only.
         *
         * The turret is handled separately by the
         * initial target logic or by the targeting
         * system.
         */
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

            /*
             * Only normal valid enemy targets can
             * become subsequent chain targets.
             */
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
            /*
             * Destination is not an obstacle.
             */
            if (
                x0 == x1 &&
                y0 == y1
            )
            {
                break;
            }

            /*
             * Ignore starting tile.
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