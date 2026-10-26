
using UnityEngine;

[CreateAssetMenu(
    fileName = "ArrowAttack",
    menuName = "Combat/Abilities/Arrow Attack"
)]
public class ArrowAttack : AbilitySO
{
    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 12f;
    [SerializeField] private int maxTargets = 99;

    [Header("Spawn Point")]
    [SerializeField]
    private string abilitySpawnPointName = "AbilitySpawnPoint";

    [Header("Target Point")]
    [Tooltip(
        "Name of the child transform used as the visual point. " +
        "Used only if grid-based direction cannot be calculated."
    )]
    [SerializeField]
    private string targetPointName = "VisualTargetPoint";

    public override bool Use(GameObject user, GameObject target)
    {
        if (user == null || target == null)
            return false;

        if (arrowPrefab == null)
        {
            Debug.LogWarning(
                "[ArrowAttack] Arrow prefab is not assigned.",
                this
            );
            return false;
        }

        if (!CanUseAfterMovement(user))
            return false;

        // Find the actual target unit, even when a child collider
        // or visual object was clicked.
        AttackUnit targetUnit = FindAttackUnit(target);

        if (targetUnit == null)
        {
            Debug.LogWarning(
                $"[ArrowAttack] No AttackUnit found for target: {target.name}",
                target
            );
            return false;
        }

        if (targetUnit.IsDead())
            return false;

        GameObject targetObject = targetUnit.gameObject;

        if (!CanTargetObject(user, targetObject))
            return false;

        // --------------------------------------------------------
        // SPAWN POINT
        // --------------------------------------------------------

        Transform spawnPoint = FindAbilitySpawnPoint(user);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : user.transform.position;

        // --------------------------------------------------------
        // DIRECTION
        // --------------------------------------------------------

        Vector2 direction = GetFootprintAttackDirection(
            user,
            targetUnit,
            spawnPosition
        );

        if (direction.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning(
                "[ArrowAttack] Could not calculate a valid firing direction.",
                user
            );
            return false;
        }

        direction.Normalize();

        // --------------------------------------------------------
        // TURRET
        // --------------------------------------------------------

        turretbehav turret = FindTurretBehaviour(user);

        // --------------------------------------------------------
        // DAMAGE
        // --------------------------------------------------------

        int effectiveDamage = GetEffectiveDamage(user);

        if (turret != null)
        {
            effectiveDamage += turret.GetChainLightningDamageBonus();
        }

        // --------------------------------------------------------
        // MAX TARGETS
        // --------------------------------------------------------

        int effectiveMaxTargets = maxTargets;

        if (turret != null)
        {
            effectiveMaxTargets +=
                turret.GetChainLightningMaxTargetsBonus();
        }

        // --------------------------------------------------------
        // SPAWN PROJECTILE
        // --------------------------------------------------------

        GameObject projectile = Instantiate(
            arrowPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (projectile == null)
            return false;

        ArrowProjectile arrow =
            projectile.GetComponent<ArrowProjectile>();

        if (arrow == null)
        {
            Debug.LogError(
                "[ArrowAttack] The arrow prefab is missing ArrowProjectile.",
                projectile
            );

            Destroy(projectile);
            return false;
        }

   

        arrow.Initialize(
            user,
            direction,
            arrowSpeed,
            effectiveDamage,
            effectiveMaxTargets,
            this
        );

        return true;
    }

    // ============================================================
    // CALCULATE DIRECTION USING THE TARGET FOOTPRINT
    // ============================================================

    private Vector2 GetFootprintAttackDirection(
        GameObject user,
        AttackUnit targetUnit,
        Vector3 spawnPosition)
    {
        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        AttackUnit userUnit = FindAttackUnit(user);

        // If grid data is unavailable, use the visual target point.
        if (gridManager == null ||
            userUnit == null ||
            targetUnit == null)
        {
            return GetFallbackDirection(
                user,
                targetUnit != null ? targetUnit.gameObject : null,
                spawnPosition
            );
        }

        Vector2Int userGrid = userUnit.GetLogicalGridPosition();
        Vector2Int targetGrid = targetUnit.GetLogicalGridPosition();

        // Get the target's footprint size.
        CharacterSO character = targetUnit.GetCharacterData();

        int footprintSize = character != null
            ? Mathf.Max(1, character.GetFootprintSize())
            : Mathf.Max(
                1,
                gridManager.GetUnitFootprintSize(targetUnit.gameObject)
            );

        // Match the centered footprint convention in AbilitySO.
        int minOffset = -(footprintSize / 2);

        int minX = targetGrid.x + minOffset;
        int minY = targetGrid.y + minOffset;
        int maxX = minX + footprintSize - 1;
        int maxY = minY + footprintSize - 1;

        bool userWithinX =
            userGrid.x >= minX && userGrid.x <= maxX;

        bool userWithinY =
            userGrid.y >= minY && userGrid.y <= maxY;

        Vector2Int gridDirection;

        if (userWithinX && !userWithinY)
        {
            // User is above or below the target footprint.
            gridDirection = userGrid.y > maxY
                ? Vector2Int.down
                : Vector2Int.up;
        }
        else if (userWithinY && !userWithinX)
        {
            // User is left or right of the target footprint.
            gridDirection = userGrid.x > maxX
                ? Vector2Int.left
                : Vector2Int.right;
        }
        else
        {
            // User is diagonally positioned relative to the footprint.
            // Choose the direction corresponding to the nearest side.
            int gapX = userGrid.x < minX
                ? minX - userGrid.x
                : userGrid.x > maxX
                    ? userGrid.x - maxX
                    : 0;

            int gapY = userGrid.y < minY
                ? minY - userGrid.y
                : userGrid.y > maxY
                    ? userGrid.y - maxY
                    : 0;

            if (gapX > gapY)
            {
                gridDirection = userGrid.x > maxX
                    ? Vector2Int.left
                    : Vector2Int.right;
            }
            else if (gapY > gapX)
            {
                gridDirection = userGrid.y > maxY
                    ? Vector2Int.down
                    : Vector2Int.up;
            }
            else
            {
                // Equal separation: use the dominant anchor difference.
                Vector2Int difference = targetGrid - userGrid;

                if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
                {
                    gridDirection = difference.x > 0
                        ? Vector2Int.right
                        : Vector2Int.left;
                }
                else
                {
                    gridDirection = difference.y > 0
                        ? Vector2Int.up
                        : Vector2Int.down;
                }
            }
        }

        // Convert the grid direction into world space.
        // This supports a rotated board.
        Vector3 worldStart =
            gridManager.GridToWorldPosition(userGrid);

        Vector3 worldNext =
            gridManager.GridToWorldPosition(userGrid + gridDirection);

        Vector3 worldDirection = worldNext - worldStart;
        worldDirection.z = 0f;

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            return GetFallbackDirection(
                user,
                targetUnit.gameObject,
                spawnPosition
            );
        }

        return ((Vector2)worldDirection).normalized;
    }

    // ============================================================
    // FALLBACK DIRECTION
    // ============================================================

    private Vector2 GetFallbackDirection(
        GameObject user,
        GameObject target,
        Vector3 spawnPosition)
    {
        if (user == null || target == null)
            return Vector2.zero;

        Transform targetPoint = FindTargetPoint(target);

        Vector3 targetPosition = targetPoint != null
            ? targetPoint.position
            : target.transform.position;

        Vector3 direction = targetPosition - spawnPosition;
        direction.z = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        return ((Vector2)direction).normalized;
    }

    // ============================================================
    // FIND ATTACK UNIT
    // ============================================================

    private AttackUnit FindAttackUnit(GameObject target)
    {
        if (target == null)
            return null;

        AttackUnit unit = target.GetComponent<AttackUnit>();

        if (unit != null)
            return unit;

        unit = target.GetComponentInParent<AttackUnit>();

        if (unit != null)
            return unit;

        return target.GetComponentInChildren<AttackUnit>(true);
    }

    // ============================================================
    // FIND TURRET
    // ============================================================

    private turretbehav FindTurretBehaviour(GameObject user)
    {
        if (user == null)
            return null;

        turretbehav turret = user.GetComponent<turretbehav>();

        if (turret != null)
            return turret;

        turret = user.GetComponentInParent<turretbehav>();

        if (turret != null)
            return turret;

        return user.GetComponentInChildren<turretbehav>(true);
    }

    // ============================================================
    // FIND SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(GameObject user)
    {
        if (user == null)
            return null;

        Transform[] children =
            user.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null &&
                child.name == abilitySpawnPointName)
            {
                return child;
            }
        }

        return null;
    }

    // ============================================================
    // FIND TARGET POINT
    // ============================================================

    private Transform FindTargetPoint(GameObject target)
    {
        if (target == null)
            return null;

        Transform[] children =
            target.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null &&
                child.name == targetPointName)
            {
                return child;
            }
        }

        return null;
    }

    public int GetMaxTargets()
    {
        return maxTargets;
    }
}
