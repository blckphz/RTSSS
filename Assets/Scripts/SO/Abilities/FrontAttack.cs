
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "FrontAttack", menuName = "Combat/Abilities/FrontAttack")]
public class FrontAttack : AbilitySO
{
    [Header("Front Attack Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset;

    [Header("Attack Start Effect")]
    [SerializeField] private GameObject attackStartPrefab;
    [SerializeField] private Vector3 attackStartOffset;
    [SerializeField] private float attackStartDuration = 2f;

    [Header("Melee Position")]
    [SerializeField] private int meleeForwardOffset = 0;

    [Header("Sprite Direction")]
    [SerializeField] private float rotationOffset = 90f;

    [Header("Melee Effect Rotation")]
    [Tooltip("Rotation of the attack effect when facing UP.")]
    [SerializeField] private float upRotation = 0f;

    [SerializeField] private float rightRotation = -90f;
    [SerializeField] private float downRotation = 180f;
    [SerializeField] private float leftRotation = 90f;

    [Header("Attack Debug")]
    [SerializeField] private bool enableAttackDebug = false;
    [SerializeField] private bool debugHitbox = true;
    [SerializeField] private bool debugTargetValidation = true;
    [SerializeField] private bool debugDamage = true;

    // ==================================================
    // DEBUG
    // ==================================================

    private void DebugLog(string message)
    {
        if (enableAttackDebug)
            Debug.Log($"[FrontAttack] {message}");
    }

    // ==================================================
    // UNIT HELPERS
    // ==================================================

    private AttackUnit FindAttackUnit(GameObject obj)
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

    private HealthManager FindHealthManager(GameObject obj)
    {
        if (obj == null)
            return null;

        HealthManager health = obj.GetComponent<HealthManager>();

        if (health == null)
            health = obj.GetComponentInParent<HealthManager>();

        if (health == null)
            health = obj.GetComponentInChildren<HealthManager>();

        return health;
    }

    private turretbehav FindTurret(GameObject obj)
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

    private UnitData FindUnitData(GameObject obj)
    {
        if (obj == null)
            return null;

        UnitData data = obj.GetComponent<UnitData>();

        if (data == null)
            data = obj.GetComponentInParent<UnitData>();

        if (data == null)
            data = obj.GetComponentInChildren<UnitData>();

        return data;
    }

    private GameObject GetUnitRoot(GameObject obj)
    {
        AttackUnit unit = FindAttackUnit(obj);

        return unit != null ? unit.gameObject : obj;
    }

    private Vector2Int GetLogicalPosition(GridManager grid, GameObject obj)
    {
        if (grid == null || obj == null)
            return Vector2Int.zero;

        GameObject root = GetUnitRoot(obj);

        return grid.GetUnitGridPosition(root);
    }

    private int GetFootprintSize(GameObject obj)
    {
        if (obj == null)
            return 1;

        AttackUnit unit = FindAttackUnit(obj);

        if (unit == null)
            return 1;

        CharacterSO character = unit.GetCharacterData();

        return character != null
            ? Mathf.Max(1, character.GetFootprintSize())
            : 1;
    }

    private int GetRuntimeRange(GameObject user)
    {
        AttackUnit unit = FindAttackUnit(user);

        return unit != null
            ? Mathf.Max(1, unit.GetEffectiveRange(this))
            : Mathf.Max(1, GetEffectiveRange(user));
    }

    private int GetRuntimeDamage(GameObject user)
    {
        AttackUnit unit = FindAttackUnit(user);

        return unit != null
            ? Mathf.Max(0, unit.GetEffectiveDamage(this))
            : Mathf.Max(0, GetEffectiveDamage(user));
    }

    // ==================================================
    // DIRECTIONAL ROTATION
    // ==================================================

    private float GetMeleeRotation(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return upRotation;

        if (direction == Vector2Int.right)
            return rightRotation;

        if (direction == Vector2Int.down)
            return downRotation;

        if (direction == Vector2Int.left)
            return leftRotation;

        return upRotation;
    }

    private Vector2Int GetDirectionToTarget(
        GridManager grid,
        GameObject user,
        GameObject target)
    {
        if (grid == null || user == null)
            return Vector2Int.up;

        if (target != null)
        {
            List<Vector2Int> targetTiles =
                GetOccupiedTiles(grid, target);

            Vector2Int origin =
                GetLogicalPosition(grid, user);

            Vector2Int closestTile = Vector2Int.zero;
            int closestDistance = int.MaxValue;

            foreach (Vector2Int tile in targetTiles)
            {
                int distance =
                    Mathf.Abs(tile.x - origin.x) +
                    Mathf.Abs(tile.y - origin.y);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTile = tile;
                }
            }

            if (targetTiles.Count > 0)
                return GetAttackDirection(origin, closestTile);
        }

        return GetMouseDirection(grid, user);
    }

    // ==================================================
    // ATTACK START AND SPRITE ROTATION
    // ==================================================

    public void PlayAttackStart(GameObject user, GameObject target)
    {
        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        if (userRoot == null)
            return;

        GridManager grid =
            Object.FindFirstObjectByType<GridManager>();

        Vector2Int direction =
            GetDirectionToTarget(grid, userRoot, targetRoot);

        if (direction == Vector2Int.zero)
            direction = GetFacingDirection(userRoot);

        RotateTowardsDirection(userRoot, direction);

        PlayAttackStartEffect(userRoot, direction);
    }

    private void RotateTowardsDirection(
        GameObject user,
        Vector2Int direction)
    {
        if (user == null || direction == Vector2Int.zero)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Transform spriteTransform = user.transform;

        SpriteRenderer sprite =
            user.GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            spriteTransform = sprite.transform;

        spriteTransform.rotation =
            Quaternion.Euler(0f, 0f, angle + rotationOffset);

        DebugLog($"Rotating {user.name} towards {direction}");
    }

    // ==================================================
    // FOOTPRINT HELPERS
    // ==================================================

    private List<Vector2Int> GetOccupiedTiles(
        GridManager grid,
        GameObject obj)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (grid == null || obj == null)
            return tiles;

        GameObject root = GetUnitRoot(obj);

        Vector2Int anchor = GetLogicalPosition(grid, root);
        int size = GetFootprintSize(root);

        Vector2Int minOffset = new Vector2Int(
            -(size / 2),
            -(size / 2)
        );

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int cell =
                    anchor + minOffset + new Vector2Int(x, y);

                if (!grid.IsInsideGrid(cell))
                    continue;

                if (grid.IsCellInUnitFootprint(root, anchor, cell))
                    tiles.Add(cell);
            }
        }

        return tiles;
    }

    private int GetClosestDistance(
        Vector2Int origin,
        List<Vector2Int> targetTiles)
    {
        int closest = int.MaxValue;

        foreach (Vector2Int tile in targetTiles)
        {
            int distance = Mathf.Max(
                Mathf.Abs(tile.x - origin.x),
                Mathf.Abs(tile.y - origin.y)
            );

            if (distance < closest)
                closest = distance;
        }

        return closest;
    }

    private bool TilesIntersect(
        List<Vector2Int> first,
        List<Vector2Int> second)
    {
        if (first == null || second == null)
            return false;

        foreach (Vector2Int tile in first)
        {
            if (second.Contains(tile))
                return true;
        }

        return false;
    }

    // ==================================================
    // RANGE DISPLAY
    // ==================================================

    public override List<Vector2Int> GetRangeTiles(
        GridManager gridManager,
        GameObject user)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (gridManager == null || user == null)
            return tiles;

        Vector2Int origin =
            GetLogicalPosition(gridManager, user);

        int range = GetRuntimeRange(user);

        int minDistance =
            Mathf.Clamp(GetMinDistance(), 0, range);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int distance = Mathf.Max(
                    Mathf.Abs(x),
                    Mathf.Abs(y)
                );

                if (distance < minDistance || distance > range)
                    continue;

                AddValidTile(
                    gridManager,
                    tiles,
                    origin + new Vector2Int(x, y)
                );
            }
        }

        return tiles;
    }

    // ==================================================
    // HITBOX
    // ==================================================

    public override List<Vector2Int> GetHitboxTiles(
        GridManager gridManager,
        GameObject user,
        GameObject target = null)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (gridManager == null || user == null)
            return tiles;

        Vector2Int origin =
            GetLogicalPosition(gridManager, user);

        int range = GetRuntimeRange(user);

        Vector2Int direction =
            GetDirectionToTarget(gridManager, user, target);

        if (direction == Vector2Int.zero)
            direction = GetFacingDirection(user);

        if (direction == Vector2Int.zero)
            direction = Vector2Int.up;

        Vector2Int sideDirection =
            direction.x != 0
                ? Vector2Int.up
                : Vector2Int.right;

        Vector2Int forwardOffset =
            direction * Mathf.Max(0, meleeForwardOffset);

        Vector2Int hitboxOrigin =
            origin + forwardOffset;

        DebugLog(
            $"Attack direction: {direction} | Origin: {origin} | " +
            $"Hitbox origin: {hitboxOrigin}"
        );

        for (int distance = 1; distance <= range; distance++)
        {
            Vector2Int center =
                hitboxOrigin + direction * distance;

            AddValidTile(gridManager, tiles, center - sideDirection);
            AddValidTile(gridManager, tiles, center);
            AddValidTile(gridManager, tiles, center + sideDirection);
        }

        if (enableAttackDebug && debugHitbox)
        {
            DebugLog(
                $"Generated hitbox ({tiles.Count} tiles): " +
                string.Join(", ", tiles)
            );
        }

        return tiles;
    }

    private Vector2Int GetAttackDirection(
        Vector2Int origin,
        Vector2Int target)
    {
        Vector2Int delta = target - origin;

        if (delta == Vector2Int.zero)
            return Vector2Int.zero;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        return delta.y > 0
            ? Vector2Int.up
            : Vector2Int.down;
    }

    // ==================================================
    // TARGET VALIDATION
    // ==================================================

    public override bool CanHit(
        GridManager gridManager,
        GameObject user,
        GameObject target)
    {
        if (gridManager == null || user == null || target == null)
            return false;

        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        bool friendlyTurretHeal =
            IsFriendlyTurretHealTarget(userRoot, targetRoot);

        if (NeedsEnemy() &&
            !friendlyTurretHeal &&
            !CanTargetObject(userRoot, targetRoot))
        {
            if (enableAttackDebug && debugTargetValidation)
                DebugLog($"Rejected {targetRoot.name}: invalid target/team.");

            return false;
        }

        HealthManager targetHealth =
            FindHealthManager(targetRoot);

        if (targetHealth == null)
        {
            if (enableAttackDebug && debugTargetValidation)
                DebugLog($"Rejected {targetRoot.name}: no HealthManager found.");

            return false;
        }

        if (targetHealth.IsDead())
        {
            if (enableAttackDebug && debugTargetValidation)
                DebugLog($"Rejected {targetRoot.name}: target is dead.");

            return false;
        }

        List<Vector2Int> targetTiles =
            GetOccupiedTiles(gridManager, targetRoot);

        if (targetTiles.Count == 0)
        {
            if (enableAttackDebug && debugTargetValidation)
                DebugLog($"Rejected {targetRoot.name}: no occupied grid tiles.");

            return false;
        }

        Vector2Int userPosition =
            GetLogicalPosition(gridManager, userRoot);

        int range = GetRuntimeRange(userRoot);

        int minDistance =
            Mathf.Clamp(GetMinDistance(), 0, range);

        int closestDistance =
            GetClosestDistance(userPosition, targetTiles);

        if (closestDistance < minDistance ||
            closestDistance > range)
        {
            if (enableAttackDebug && debugTargetValidation)
            {
                DebugLog(
                    $"Rejected {targetRoot.name}: distance {closestDistance}, " +
                    $"allowed {minDistance}-{range}."
                );
            }

            return false;
        }

        List<Vector2Int> hitbox =
            GetHitboxTiles(gridManager, userRoot, targetRoot);

        bool intersects =
            TilesIntersect(hitbox, targetTiles);

        if (!intersects)
        {
            if (enableAttackDebug && debugTargetValidation)
                DebugLog($"Rejected {targetRoot.name}: outside melee hitbox.");

            return false;
        }

        if (enableAttackDebug && debugTargetValidation)
        {
            DebugLog(
                friendlyTurretHeal
                    ? $"CanHit accepted friendly turret: {targetRoot.name}"
                    : $"CanHit accepted target: {targetRoot.name}"
            );
        }

        return true;
    }

    // ==================================================
    // TEAM / TURRET HEAL
    // ==================================================

    private bool IsFriendlyTurretHealTarget(
        GameObject user,
        GameObject target)
    {
        if (user == null || target == null)
            return false;

        turretbehav turret = FindTurret(target);

        if (turret == null)
            return false;

        UnitData userData = FindUnitData(user);

        if (userData == null ||
            !userData.HasMeleeTurretHealUpgrade())
            return false;

        HealthManager userHealth =
            FindHealthManager(user);

        HealthManager targetHealth =
            FindHealthManager(target);

        if (userHealth == null ||
            targetHealth == null ||
            targetHealth.IsDead())
            return false;

        bool sameTeam =
            userHealth.GetTeam() == targetHealth.GetTeam();

        if (!sameTeam)
            return false;

        return true;
    }

    // ==================================================
    // ATTACK START EFFECT
    // ==================================================

    private void PlayAttackStartEffect(
        GameObject user,
        Vector2Int direction)
    {
        if (attackStartPrefab == null || user == null)
            return;

        Vector3 position =
            user.transform.position + attackStartOffset;

        float rotation =
            GetMeleeRotation(direction);

        GameObject effect = Object.Instantiate(
            attackStartPrefab,
            position,
            Quaternion.Euler(0f, 0f, rotation)
        );

        if (effect != null)
            Object.Destroy(effect, attackStartDuration);
    }

    // ==================================================
    // USE - CALLED AT IMPACT
    // ==================================================

    public override bool Use(GameObject user, GameObject target)
    {
        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        if (userRoot == null)
            return false;

        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            DebugLog("Attack failed: no GridManager found.");
            return false;
        }

        Vector2Int attackDirection =
            GetDirectionToTarget(gridManager, userRoot, targetRoot);

        if (attackDirection == Vector2Int.zero)
            attackDirection = GetFacingDirection(userRoot);

        List<Vector2Int> hitbox =
            GetHitboxTiles(gridManager, userRoot, targetRoot);

        if (hitbox.Count == 0)
        {
            DebugLog("Attack failed: hitbox contains no valid tiles.");
            return false;
        }

        HashSet<GameObject> candidates =
            new HashSet<GameObject>();

        foreach (Vector2Int tile in hitbox)
        {
            GameObject occupant =
                gridManager.GetUnitAt(tile);

            if (occupant != null)
                candidates.Add(GetUnitRoot(occupant));
        }

        AttackUnit[] allUnits =
            Object.FindObjectsByType<AttackUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null ||
                !unit.gameObject.activeInHierarchy)
                continue;

            GameObject unitRoot = unit.gameObject;

            if (unitRoot == userRoot)
                continue;

            List<Vector2Int> footprint =
                GetOccupiedTiles(gridManager, unitRoot);

            if (TilesIntersect(hitbox, footprint))
                candidates.Add(unitRoot);
        }

        // Explicitly include the selected target.
        // This is important for turrets that do not have AttackUnit.
        if (targetRoot != null)
        {
            List<Vector2Int> selectedTargetTiles =
                GetOccupiedTiles(gridManager, targetRoot);

            if (TilesIntersect(hitbox, selectedTargetTiles))
                candidates.Add(targetRoot);
        }

        HashSet<GameObject> processed =
            new HashSet<GameObject>();

        bool hitSomething = false;

        foreach (GameObject candidate in candidates)
        {
            if (candidate == null)
                continue;

            GameObject candidateRoot =
                GetUnitRoot(candidate);

            if (candidateRoot == userRoot)
                continue;

            if (!processed.Add(candidateRoot))
                continue;

            List<Vector2Int> footprint =
                GetOccupiedTiles(gridManager, candidateRoot);

            if (!TilesIntersect(hitbox, footprint))
                continue;

            if (AttackUnitAtTile(userRoot, candidateRoot))
                hitSomething = true;
        }

        if (hitSomething && targetRoot != null)
            PlayHitEffect(targetRoot, attackDirection);

        DebugLog(
            $"Attack finished. Candidates: {candidates.Count}, " +
            $"Successful hits: {hitSomething}"
        );

        return hitSomething;
    }

    // ==================================================
    // DAMAGE / HEAL
    // ==================================================

    private bool AttackUnitAtTile(
        GameObject user,
        GameObject target)
    {
        if (user == null || target == null)
            return false;

        HealthManager userHealth =
            FindHealthManager(user);

        HealthManager targetHealth =
            FindHealthManager(target);

        if (targetHealth == null || targetHealth.IsDead())
            return false;

        turretbehav turret =
            FindTurret(target);

        UnitData userData =
            FindUnitData(user);

        // FRIENDLY TURRET HEAL
        if (turret != null &&
            userData != null &&
            userData.HasMeleeTurretHealUpgrade() &&
            userHealth != null &&
            targetHealth.GetTeam() == userHealth.GetTeam())
        {
            int healAmount =
                GetRuntimeDamage(user) +
                userData.GetMeleeTurretHealAmount();

            targetHealth.Heal(healAmount);

            if (enableAttackDebug && debugDamage)
            {
                DebugLog(
                    $"Healed friendly turret {target.name} " +
                    $"for {healAmount}."
                );
            }

            return true;
        }

        // NEVER DAMAGE A FRIENDLY UNIT
        if (userHealth != null &&
            targetHealth.GetTeam() == userHealth.GetTeam())
        {
            if (enableAttackDebug && debugDamage)
                DebugLog($"Ignored friendly unit: {target.name}");

            return false;
        }

        // NORMAL ENEMY DAMAGE
        int damage = GetRuntimeDamage(user);

        if (damage <= 0)
            return false;

        targetHealth.TakeDamage(damage);

        if (enableAttackDebug && debugDamage)
            DebugLog($"Damaged {target.name} for {damage}.");

        return true;
    }

    // ==================================================
    // HIT EFFECT
    // ==================================================

    private void PlayHitEffect(
        GameObject target,
        Vector2Int direction)
    {
        if (hitEffectPrefab == null || target == null)
            return;

        Vector3 position =
            target.transform.position + hitEffectOffset;

        float rotation =
            GetMeleeRotation(direction);

        GameObject effect = Object.Instantiate(
            hitEffectPrefab,
            position,
            Quaternion.Euler(0f, 0f, rotation)
        );

        if (effect == null)
            return;

        ParticleSystem particles =
            effect.GetComponent<ParticleSystem>();

        if (particles != null)
        {
            particles.Stop(true);
            particles.Play(true);
        }
    }
}
