
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FrontAttack", menuName = "Combat/Abilities/FrontAttack")]
public class FrontAttack : AbilitySO
{
    [Header("Front Attack Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset;

    [Header("Attack Debug")]
    [SerializeField] private bool enableAttackDebug = true;
    [SerializeField] private bool logEveryHitboxTile = true;
    [SerializeField] private bool logFootprintChecks = true;



    // ==================================================
    // UNIT HELPERS
    // ==================================================

    private AttackUnit FindAttackUnit(GameObject obj)
    {
        if (obj == null) return null;

        AttackUnit unit = obj.GetComponent<AttackUnit>();
        if (unit == null) unit = obj.GetComponentInParent<AttackUnit>();
        if (unit == null) unit = obj.GetComponentInChildren<AttackUnit>();

        return unit;
    }

    private HealthManager FindHealthManager(GameObject obj)
    {
        if (obj == null) return null;

        HealthManager health = obj.GetComponent<HealthManager>();
        if (health == null) health = obj.GetComponentInParent<HealthManager>();
        if (health == null) health = obj.GetComponentInChildren<HealthManager>();

        return health;
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

        // GridManager knows the logical destination of a moving unit.
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

        // Matches GridManager's centered footprint convention.
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

    private string TilesToString(List<Vector2Int> tiles)
    {
        return tiles == null
            ? "NULL"
            : $"[{string.Join(", ", tiles)}]";
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
        {
            return tiles;
        }

        Vector2Int origin = GetLogicalPosition(gridManager, user);
        int range = GetRuntimeRange(user);
        int minDistance = Mathf.Clamp(GetMinDistance(), 0, range);

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
        {
            return tiles;
        }

        Vector2Int origin = GetLogicalPosition(gridManager, user);
        int range = GetRuntimeRange(user);
        Vector2Int direction = Vector2Int.zero;

        if (target != null)
        {
            List<Vector2Int> targetTiles =
                GetOccupiedTiles(gridManager, target);

            int closestDistance = int.MaxValue;
            Vector2Int closestTile = Vector2Int.zero;

            foreach (Vector2Int tile in targetTiles)
            {
                int distance =
                    Mathf.Abs(tile.x - origin.x) +
                    Mathf.Abs(tile.y - origin.y);

                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTile = tile;
            }

            if (targetTiles.Count > 0)
            {
                direction = GetAttackDirection(origin, closestTile);
            }
            else
            {
                Vector2Int fallback =
                    gridManager.WorldToGridPosition(target.transform.position);

                direction = GetAttackDirection(origin, fallback);
            }
        }
        else
        {
            direction = GetMouseDirection(gridManager, user);
        }

        if (direction == Vector2Int.zero)
            direction = GetFacingDirection(user);

        if (direction == Vector2Int.zero)
            direction = Vector2Int.up;

        Vector2Int sideDirection =
            direction.x != 0
                ? Vector2Int.up
                : Vector2Int.right;

        for (int distance = 1; distance <= range; distance++)
        {
            Vector2Int center = origin + direction * distance;

            AddValidTile(
                gridManager,
                tiles,
                center - sideDirection
            );

            AddValidTile(
                gridManager,
                tiles,
                center
            );

            AddValidTile(
                gridManager,
                tiles,
                center + sideDirection
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
            return delta.x > 0
                ? Vector2Int.right
                : Vector2Int.left;

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

        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);


        bool friendlyTurretHeal =
            IsFriendlyTurretHealTarget(userRoot, targetRoot);

        if (NeedsEnemy() &&
            !friendlyTurretHeal &&
            !CanTargetObject(userRoot, targetRoot))
        {

            return false;
        }

        HealthManager targetHealth = FindHealthManager(targetRoot);

        if (targetHealth != null && targetHealth.IsDead())
        {
            return false;
        }

        List<Vector2Int> targetTiles =
            GetOccupiedTiles(gridManager, targetRoot);

        if (targetTiles.Count == 0)
        {

            return false;
        }

        Vector2Int userPosition =
            GetLogicalPosition(gridManager, userRoot);

        int range = GetRuntimeRange(userRoot);
        int minDistance = Mathf.Clamp(GetMinDistance(), 0, range);
        int closestDistance =
            GetClosestDistance(userPosition, targetTiles);


        if (closestDistance < minDistance ||
            closestDistance > range)
        {

            return false;
        }

        List<Vector2Int> hitbox =
            GetHitboxTiles(gridManager, userRoot, targetRoot);

        bool intersects = TilesIntersect(hitbox, targetTiles);


        if (!intersects)
        {

            return false;
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

        turretbehav turret = target.GetComponent<turretbehav>();

        if (turret == null)
            turret = target.GetComponentInParent<turretbehav>();

        if (turret == null)
            return false;

        UnitData userData = user.GetComponent<UnitData>();

        if (userData == null)
            userData = user.GetComponentInParent<UnitData>();

        if (userData == null ||
            !userData.HasMeleeTurretHealUpgrade())
        {
            return false;
        }

        HealthManager userHealth = FindHealthManager(user);
        HealthManager targetHealth = FindHealthManager(target);

        if (userHealth == null ||
            targetHealth == null ||
            targetHealth.IsDead())
        {
            return false;
        }

        return userHealth.GetTeam() == targetHealth.GetTeam();
    }

    // ==================================================
    // USE
    // ==================================================

    public override bool Use(GameObject user, GameObject target)
    {


        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>();


        GameObject userRoot = GetUnitRoot(user);
        GameObject targetRoot = GetUnitRoot(target);

        List<Vector2Int> hitbox =
            GetHitboxTiles(gridManager, userRoot, targetRoot);

        if (hitbox.Count == 0)
        {
            return false;
        }

        // Collect candidates from the grid AND from all AttackUnit
        // footprints. The second pass makes detection robust when a
        // multi-tile unit is registered only at its anchor.
        HashSet<GameObject> candidates = new HashSet<GameObject>();

        foreach (Vector2Int tile in hitbox)
        {
            GameObject occupant = gridManager.GetUnitAt(tile);

            if (logEveryHitboxTile)
            {
            }

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
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            GameObject unitRoot = unit.gameObject;

            if (unitRoot == userRoot)
                continue;

            List<Vector2Int> footprint =
                GetOccupiedTiles(gridManager, unitRoot);

            bool intersects =
                TilesIntersect(hitbox, footprint);

            if (logFootprintChecks)
            {
            }

            if (intersects)
                candidates.Add(unitRoot);
        }

        // Ensure the selected target is considered when its footprint
        // intersects the hitbox, even if it wasn't returned by GetUnitAt.
        List<Vector2Int> selectedTargetTiles =
            GetOccupiedTiles(gridManager, targetRoot);

        if (TilesIntersect(hitbox, selectedTargetTiles))
            candidates.Add(targetRoot);

        HashSet<GameObject> processed = new HashSet<GameObject>();
        bool hitSomething = false;

        foreach (GameObject candidate in candidates)
        {
            if (candidate == null)
                continue;

            GameObject candidateRoot = GetUnitRoot(candidate);

            if (candidateRoot == userRoot)
                continue;

            if (!processed.Add(candidateRoot))
                continue;

            List<Vector2Int> footprint =
                GetOccupiedTiles(gridManager, candidateRoot);

            if (!TilesIntersect(hitbox, footprint))
            {

                continue;
            }


            if (AttackUnitAtTile(userRoot, candidateRoot))
            {
                hitSomething = true;
            }
        }

        if (hitSomething)
        {
            PlayHitEffect(targetRoot);
        }

        return hitSomething;
    }

    // ==================================================
    // DAMAGE / HEAL
    // ==================================================

    private bool AttackUnitAtTile(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return false;

        HealthManager userHealth = FindHealthManager(user);
        HealthManager targetHealth = FindHealthManager(target);

        if (targetHealth == null)
        {

            return false;
        }

        if (targetHealth.IsDead())
        {
            return false;
        }

        turretbehav turret = target.GetComponent<turretbehav>();

        if (turret == null)
            turret = target.GetComponentInParent<turretbehav>();

        UnitData userData = user.GetComponent<UnitData>();

        if (userData == null)
            userData = user.GetComponentInParent<UnitData>();

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


            return true;
        }

        if (userHealth != null &&
            targetHealth.GetTeam() == userHealth.GetTeam())
        {

            return false;
        }

        int damage = GetRuntimeDamage(user);


        if (damage <= 0)
        {
            return false;
        }

        targetHealth.TakeDamage(damage);


        return true;
    }

    // ==================================================
    // EFFECT
    // ==================================================

    private void PlayHitEffect(GameObject target)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        Vector3 position =
            target.transform.position + hitEffectOffset;


        GameObject effect = Object.Instantiate(
            hitEffectPrefab,
            position,
            Quaternion.identity
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
