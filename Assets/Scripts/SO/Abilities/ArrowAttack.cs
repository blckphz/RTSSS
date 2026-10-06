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
        "Name of the child transform used as the visual point to aim at. " +
        "If not found, the enemy root position is used."
    )]
    [SerializeField]
    private string targetPointName = "VisualTargetPoint";


    public override bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null || target == null)
            return false;

        if (arrowPrefab == null)
            return false;

        if (!CanUseAfterMovement(user))
            return false;

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
            return false;

        if (targetUnit.IsDead())
            return false;

        if (!CanTargetObject(user, target))
            return false;


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
        // TARGET POINT
        // ========================================================

        Transform targetPoint =
            FindTargetPoint(target);

        Vector3 targetPosition =
            targetPoint != null
                ? targetPoint.position
                : target.transform.position;


        // ========================================================
        // DIRECTION
        // ========================================================

        Vector3 direction =
            targetPosition - spawnPosition;

        direction.z = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();


        // ========================================================
        // GET TURRET
        // ========================================================

        turretbehav turret =
            FindTurretBehaviour(user);


        // ========================================================
        // DAMAGE
        // ========================================================

        int effectiveDamage =
            GetEffectiveDamage(user);

        if (turret != null)
        {
            effectiveDamage +=
                turret.GetChainLightningDamageBonus();
        }


        // ========================================================
        // MAX TARGETS
        // ========================================================

        int effectiveMaxTargets =
            maxTargets;

        if (turret != null)
        {
            effectiveMaxTargets +=
                turret.GetChainLightningMaxTargetsBonus();
        }


        // ========================================================
        // SPAWN PROJECTILE
        // ========================================================

        GameObject projectile =
            Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (projectile == null)
            return false;


        // ========================================================
        // GET ARROW PROJECTILE
        // ========================================================

        ArrowProjectile arrow =
            projectile.GetComponent<ArrowProjectile>();

        if (arrow == null)
        {
            Destroy(projectile);
            return false;
        }


        // ========================================================
        // DEBUG
        // ========================================================

        Debug.Log(
            $"[ArrowAttack] FIRING | " +
            $"User={user.name} | " +
            $"Target={target.name} | " +
            $"TargetPoint={targetPosition} | " +
            $"BaseDamage={GetDamage()} | " +
            $"EffectiveDamage={effectiveDamage} | " +
            $"BaseMaxTargets={maxTargets} | " +
            $"EffectiveMaxTargets={effectiveMaxTargets} | " +
            $"ChainLightningCharged=" +
            $"{(turret != null && turret.IsChainLightningCharged())}",
            user
        );


        // ========================================================
        // INITIALIZE
        // ========================================================

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
    // FIND TURRET
    // ============================================================

    private turretbehav FindTurretBehaviour(
        GameObject user)
    {
        if (user == null)
            return null;

        turretbehav turret =
            user.GetComponent<turretbehav>();

        if (turret != null)
            return turret;


        turret =
            user.GetComponentInParent<turretbehav>();

        if (turret != null)
            return turret;


        turret =
            user.GetComponentInChildren<turretbehav>();

        return turret;
    }


    // ============================================================
    // FIND SPAWN POINT
    // ============================================================

    private Transform FindAbilitySpawnPoint(
        GameObject user)
    {
        if (user == null)
            return null;

        Transform[] children =
            user.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (child.name == abilitySpawnPointName)
                return child;
        }

        return null;
    }


    // ============================================================
    // FIND TARGET POINT
    // ============================================================

    private Transform FindTargetPoint(
        GameObject target)
    {
        if (target == null)
            return null;

        Transform[] children =
            target.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (child.name == targetPointName)
                return child;
        }

        return null;
    }


    public int GetMaxTargets()
    {
        return maxTargets;
    }
}
