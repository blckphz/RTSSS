
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
    private string abilitySpawnPointName = "AbilitySpawnPoint";

    // ============================================================
    // EFFECTIVE JUMPS
    // ============================================================

    private int GetEffectiveMaxJumps(GameObject user)
    {
        int bonusJumps = 0;

        if (user != null)
        {
            UnitData unitData = user.GetComponent<UnitData>();

            if (unitData == null)
                unitData = user.GetComponentInParent<UnitData>();

            if (unitData == null)
                unitData = user.GetComponentInChildren<UnitData>();

            if (unitData != null)
                bonusJumps = unitData.GetBonusJumps(this);
        }

        return Mathf.Max(1, maxJumps + bonusJumps);
    }

    // ============================================================
    // COMPONENT HELPERS
    // ============================================================

    private AttackUnit GetAttackUnit(GameObject obj)
    {
        if (obj == null)
            return null;

        AttackUnit unit = obj.GetComponent<AttackUnit>();

        if (unit == null)
            unit = obj.GetComponentInParent<AttackUnit>();

        if (unit == null)
            unit = obj.GetComponentInChildren<AttackUnit>();

        return unit;
    }

    private turretbehav GetTurret(GameObject obj)
    {
        if (obj == null)
            return null;

        turretbehav turret = obj.GetComponent<turretbehav>();

        if (turret == null)
            turret = obj.GetComponentInParent<turretbehav>();

        if (turret == null)
            turret = obj.GetComponentInChildren<turretbehav>();

        return turret;
    }

    private bool IsUnlockedPlayerTurret(
        AttackUnit targetUnit,
        turretbehav turret)
    {
        return targetUnit != null
            && turret != null
            && turret.IsChainLightningUnlocked()
            && targetUnit.GetTeam() == Team.Player;
    }

    // ============================================================
    // CAN HIT
    // ============================================================

    public override bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target)
    {
        if (gridManager == null || user == null || target == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        AttackUnit userUnit = GetAttackUnit(user);
        AttackUnit targetUnit = GetAttackUnit(target);

        if (userUnit == null || targetUnit == null)
        {
            Debug.LogWarning(
                "[ChainLightning] CanHit failed: missing AttackUnit."
            );
            return false;
        }

        if (userUnit == targetUnit || targetUnit.IsDead())
            return false;

        if (!CanTargetObject(userUnit.gameObject, targetUnit.gameObject))
        {
            turretbehav turret = GetTurret(targetUnit.gameObject);

            if (!IsUnlockedPlayerTurret(targetUnit, turret))
            {
                Debug.LogWarning(
                    "[ChainLightning] CanHit failed: target team is invalid."
                );
                return false;
            }
        }

        Vector2Int userPosition = userUnit.GetLogicalGridPosition();
        Vector2Int targetPosition = targetUnit.GetLogicalGridPosition();

        CharacterSO character = targetUnit.GetCharacterData();

        int footprintSize = character != null
            ? Mathf.Max(1, character.GetFootprintSize())
            : Mathf.Max(
                1,
                gridManager.GetUnitFootprintSize(targetUnit.gameObject)
            );

        int minOffset = -(footprintSize / 2);
        bool anyTileInRange = false;

        for (int x = 0; x < footprintSize; x++)
        {
            for (int y = 0; y < footprintSize; y++)
            {
                Vector2Int tile = targetPosition + new Vector2Int(
                    minOffset + x,
                    minOffset + y
                );

                if (!gridManager.IsInsideGrid(tile))
                    continue;

                if (!CanHitTile(gridManager, userUnit.gameObject, tile))
                    continue;

                anyTileInRange = true;

                // Ignore both the caster's and target's own occupied
                // footprint tiles. Other units still block line of sight.
                if (!HasObjectOnLine(
                    gridManager,
                    userPosition,
                    tile,
                    targetUnit.gameObject,
                    userUnit.gameObject))
                {
                    return true;
                }
            }
        }

        if (!anyTileInRange)
        {
            Debug.LogWarning(
                "[ChainLightning] CanHit failed: no footprint tile is in range."
            );
        }
        else
        {
            Debug.LogWarning(
                "[ChainLightning] CanHit failed: line of sight is blocked."
            );
        }

        return false;
    }

    // ============================================================
    // USE
    // ============================================================

    public override bool Use(GameObject user, GameObject target)
    {
        if (user == null || target == null)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: user or target is null."
            );
            return false;
        }

        if (!CanUseAfterMovement(user))
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: movement restriction."
            );
            return false;
        }

        AttackUnit userUnit = GetAttackUnit(user);
        AttackUnit targetUnit = GetAttackUnit(target);

        if (userUnit == null || targetUnit == null)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: missing AttackUnit."
            );
            return false;
        }

        if (targetUnit.IsDead())
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: target is dead."
            );
            return false;
        }

        turretbehav turret = GetTurret(targetUnit.gameObject);

        if (turret != null && !turret.IsChainLightningUnlocked())
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: turret is locked."
            );
            return false;
        }

        if (!CanTargetObject(userUnit.gameObject, targetUnit.gameObject))
        {
            if (!IsUnlockedPlayerTurret(targetUnit, turret))
            {
                Debug.LogWarning(
                    "[ChainLightning] Use failed: target team is invalid."
                );
                return false;
            }
        }

        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: GridManager not found."
            );
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: projectile prefab is not assigned."
            );
            return false;
        }

        // Validate the actual target footprint, not just its anchor.
        if (!CanHit(gridManager, userUnit.gameObject, targetUnit.gameObject))
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: initial target failed CanHit."
            );
            return false;
        }

        int effectiveMaxJumps = GetEffectiveMaxJumps(userUnit.gameObject);

        List<GameObject> chain = GetChainPreview(
            userUnit.gameObject,
            targetUnit.gameObject,
            gridManager,
            effectiveMaxJumps
        );

        if (chain == null || chain.Count == 0)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: no valid chain targets."
            );
            return false;
        }

        Transform spawnPoint = FindAbilitySpawnPoint(userUnit.gameObject);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : userUnit.transform.position;

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (projectile == null)
        {
            Debug.LogWarning(
                "[ChainLightning] Use failed: projectile instantiation failed."
            );
            return false;
        }

        ChainLightningProjectile projectileComponent =
            projectile.GetComponent<ChainLightningProjectile>();

        if (projectileComponent == null)
        {
            Debug.LogError(
                "[ChainLightning] Projectile prefab is missing " +
                "ChainLightningProjectile. Destroying spawned projectile."
            );

            Destroy(projectile);
            return false;
        }

        int turretCharges = GetTurretCharges(
            chain,
            effectiveMaxJumps
        );

        projectileComponent.Initialize(
            userUnit.gameObject,
            chain,
            projectileSpeed,
            GetEffectiveDamage(userUnit.gameObject),
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
        if (chain == null || chain.Count == 0)
            return 0;

        for (int i = 0; i < chain.Count; i++)
        {
            GameObject target = chain[i];

            if (target == null)
                continue;

            turretbehav turret = GetTurret(target);

            if (turret == null || !turret.IsChainLightningUnlocked())
                continue;

            return Mathf.Max(1, effectiveMaxJumps - i);
        }

        return 0;
    }

    // ============================================================
    // SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(GameObject user)
    {
        if (user == null)
            return null;

        Transform spawnPoint = user.transform.Find(abilitySpawnPointName);

        if (spawnPoint != null)
            return spawnPoint;

        Transform[] children =
            user.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == abilitySpawnPointName)
                return children[i];
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
        return GetChainPreview(
            user,
            firstTarget,
            gridManager,
            GetEffectiveMaxJumps(user)
        );
    }

    public List<GameObject> GetChainPreview(
        GameObject user,
        GameObject firstTarget,
        GridManager gridManager,
        int effectiveMaxJumps)
    {
        List<GameObject> chain = new List<GameObject>();

        if (user == null || firstTarget == null || gridManager == null)
            return chain;

        effectiveMaxJumps = Mathf.Max(1, effectiveMaxJumps);

        HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        GameObject currentTarget = firstTarget;
        int jump = 0;

        while (currentTarget != null && jump < effectiveMaxJumps)
        {
            bool validTarget = jump == 0
                ? IsValidInitialTarget(
                    user,
                    currentTarget,
                    gridManager,
                    hitTargets)
                : IsValidChainTarget(user, currentTarget, hitTargets);

            if (!validTarget)
                break;

            turretbehav turret = GetTurret(currentTarget);

            if (turret != null)
            {
                if (!turret.IsChainLightningUnlocked())
                    break;

                chain.Add(currentTarget);
                break;
            }

            hitTargets.Add(currentTarget);
            chain.Add(currentTarget);

            currentTarget = FindClosestEnemy(
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
        if (user == null || target == null || gridManager == null)
            return false;

        if (hitTargets.Contains(target) || !target.activeInHierarchy)
            return false;

        AttackUnit userUnit = GetAttackUnit(user);
        AttackUnit targetUnit = GetAttackUnit(target);

        if (userUnit == null || targetUnit == null)
            return false;

        if (targetUnit.IsDead())
            return false;

        turretbehav turret = GetTurret(target);

        if (turret != null && !turret.IsChainLightningUnlocked())
            return false;

        bool validTarget = CanTargetObject(
            userUnit.gameObject,
            targetUnit.gameObject
        );

        if (!validTarget)
            validTarget = IsUnlockedPlayerTurret(targetUnit, turret);

        if (!validTarget)
            return false;

        Vector2Int userTile = userUnit.GetLogicalGridPosition();
        Vector2Int targetTile = targetUnit.GetLogicalGridPosition();

        // Ignore the caster and target's own occupied footprint tiles.
        return !HasObjectOnLine(
            gridManager,
            userTile,
            targetTile,
            targetUnit.gameObject,
            userUnit.gameObject
        );
    }

    // ============================================================
    // CHAIN TARGET VALIDATION
    // ============================================================

    private bool IsValidChainTarget(
        GameObject user,
        GameObject target,
        HashSet<GameObject> hitTargets)
    {
        if (user == null || target == null)
            return false;

        if (hitTargets.Contains(target) || !target.activeInHierarchy)
            return false;

        AttackUnit targetUnit = GetAttackUnit(target);

        if (targetUnit == null || targetUnit.IsDead())
            return false;

        turretbehav turret = GetTurret(target);

        if (turret != null)
        {
            return turret.IsChainLightningUnlocked();
        }

        AttackUnit userUnit = GetAttackUnit(user);

        if (userUnit == null)
            return false;

        return CanTargetObject(
            userUnit.gameObject,
            targetUnit.gameObject
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
        if (user == null || currentTarget == null || gridManager == null)
            return null;

        AttackUnit currentUnit = GetAttackUnit(currentTarget);

        if (currentUnit == null)
            return null;

        Vector2Int currentTile = currentUnit.GetLogicalGridPosition();

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(
            FindObjectsSortMode.None
        );

        for (int i = 0; i < allUnits.Length; i++)
        {
            AttackUnit candidateUnit = allUnits[i];

            if (candidateUnit == null)
                continue;

            GameObject candidate = candidateUnit.gameObject;

            if (candidate == null ||
                candidate == user ||
                candidateUnit == currentUnit ||
                !candidate.activeInHierarchy ||
                candidateUnit.IsDead() ||
                hitTargets.Contains(candidate))
            {
                continue;
            }

            turretbehav candidateTurret = GetTurret(candidate);

            if (candidateTurret != null &&
                !candidateTurret.IsChainLightningUnlocked())
            {
                continue;
            }

            if (candidateTurret == null &&
                !CanTargetObject(user, candidate))
            {
                continue;
            }

            Vector2Int candidateTile =
                candidateUnit.GetLogicalGridPosition();

            float distance = Vector2.Distance(
                currentTile,
                candidateTile
            );

            if (maxJumpDistance > 0f &&
                distance > maxJumpDistance)
            {
                continue;
            }

            // Ignore both units' footprints during this jump's LOS test.
            if (HasObjectOnLine(
                gridManager,
                currentTile,
                candidateTile,
                candidate,
                currentTarget))
            {
                continue;
            }

            if (closestEnemy == null || distance < closestDistance)
            {
                closestEnemy = candidate;
                closestDistance = distance;
            }
            else if (Mathf.Approximately(distance, closestDistance))
            {
                if (candidate.GetEntityId() < closestEnemy.GetEntityId())
                {
                    closestEnemy = candidate;
                    closestDistance = distance;
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
        return HasObjectOnLine(
            gridManager,
            start,
            end,
            null,
            null
        );
    }

    private bool HasObjectOnLine(
        GridManager gridManager,
        Vector2Int start,
        Vector2Int end,
        GameObject targetToIgnore,
        GameObject startUnitToIgnore)
    {
        if (gridManager == null)
            return true;

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            // The destination tile is not an obstruction.
            if (x0 == x1 && y0 == y1)
                break;

            // The starting tile is not an obstruction either.
            if (!(x0 == start.x && y0 == start.y))
            {
                Vector2Int tile = new Vector2Int(x0, y0);
                GameObject occupied = gridManager.GetUnitAt(tile);

                if (occupied != null &&
                    !IsSameUnit(occupied, targetToIgnore) &&
                    !IsSameUnit(occupied, startUnitToIgnore))
                {
                    return true;
                }
            }

            int e2 = 2 * err;

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

    private bool IsSameUnit(
        GameObject occupied,
        GameObject unitToIgnore)
    {
        if (occupied == null || unitToIgnore == null)
            return false;

        if (occupied == unitToIgnore)
            return true;

        AttackUnit occupiedUnit = GetAttackUnit(occupied);
        AttackUnit ignoredUnit = GetAttackUnit(unitToIgnore);

        return occupiedUnit != null &&
               ignoredUnit != null &&
               occupiedUnit == ignoredUnit;
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
