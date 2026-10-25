using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Shotgun",
    menuName = "Combat/Abilities/Shotgun"
)]
public class ShotgunBehav : AbilitySO
{
    // ============================================================
    // SHOTGUN DAMAGE FALLOFF
    // ============================================================

    [Header("Shotgun Damage Falloff")]

    [Tooltip(
        "Damage multiplier at the maximum shotgun range. " +
        "For example, 0.8 means the furthest enemies take 80% damage."
    )]
    [SerializeField, Range(0f, 1f)]
    private float minimumDamageMultiplier = 0.8f;


    // ============================================================
    // USE
    // ============================================================

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        if (user == null)
        {
            return false;
        }

        if (!CanUseAfterMovement(user))
        {
            return false;
        }

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            return false;
        }

        // --------------------------------------------------------
        // FIND ALL ENEMIES INSIDE SHOTGUN AFFECTED AREA
        // --------------------------------------------------------

        List<GameObject> enemies =
            GetAffectedEnemies(
                gridManager,
                user
            );

        if (enemies.Count == 0)
        {
            return false;
        }

        // --------------------------------------------------------
        // BASE DAMAGE
        // --------------------------------------------------------

        int baseDamage =
            GetEffectiveDamage(user);

        // --------------------------------------------------------
        // USER POSITION
        // --------------------------------------------------------

        Vector2Int userTile =
            gridManager.WorldToGridPosition(
                user.transform.position
            );

        // --------------------------------------------------------
        // MAX SHOTGUN RANGE
        // --------------------------------------------------------

        int maxRange =
            Mathf.Max(
                1,
                GetEffectiveRange(user)
            );

        // --------------------------------------------------------
        // DAMAGE EACH ENEMY
        // --------------------------------------------------------

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            HealthManager healthManager =
                enemy.GetComponent<HealthManager>();

            if (healthManager == null)
            {
                continue;
            }

            if (healthManager.IsDead())
            {
                continue;
            }

            // ----------------------------------------------------
            // GET ENEMY GRID POSITION
            // ----------------------------------------------------

            Vector2Int enemyTile =
                gridManager.WorldToGridPosition(
                    enemy.transform.position
                );

            // ----------------------------------------------------
            // CALCULATE DISTANCE
            //
            // Uses Manhattan/grid distance.
            //
            // 1 tile away = 1
            // 2 tiles away = 2
            // 3 tiles away = 3
            // etc.
            // ----------------------------------------------------

            int distance =
                Mathf.Abs(
                    enemyTile.x - userTile.x
                )
                +
                Mathf.Abs(
                    enemyTile.y - userTile.y
                );

            // ----------------------------------------------------
            // DAMAGE FALLOFF
            //
            // The falloff ALWAYS goes from:
            //
            // Near = 100%
            // Far  = minimumDamageMultiplier
            //
            // Example with minimum = 0.8:
            //
            // Range 3:
            // 100%
            // 90%
            // 80%
            //
            // Range 5:
            // 100%
            // 95%
            // 90%
            // 85%
            // 80%
            //
            // ----------------------------------------------------

            float t =
                (float)(distance - 1)
                /
                Mathf.Max(
                    1,
                    maxRange - 1
                );

            t =
                Mathf.Clamp01(t);

            float damageMultiplier =
                Mathf.Lerp(
                    1f,
                    minimumDamageMultiplier,
                    t
                );

            // ----------------------------------------------------
            // FINAL DAMAGE
            // ----------------------------------------------------

            int finalDamage =
                Mathf.RoundToInt(
                    baseDamage *
                    damageMultiplier
                );

            // ----------------------------------------------------
            // APPLY DAMAGE
            // ----------------------------------------------------

            healthManager.TakeDamage(
                finalDamage
            );
        }

        return true;
    }


    // ============================================================
    // GET AFFECTED ENEMIES
    // ============================================================

    public List<GameObject> GetAffectedEnemies(
        GridManager gridManager,
        GameObject user
    )
    {
        List<GameObject> enemies =
            new List<GameObject>();

        if (
            gridManager == null ||
            user == null
        )
        {
            return enemies;
        }

        // --------------------------------------------------------
        // GET SHOTGUN AFFECTED TILES
        //
        // AbilitySO.GetRangeTiles() handles:
        //
        // - Mouse direction
        // - Shotgun width
        // - Range
        // - Minimum distance
        // --------------------------------------------------------

        List<Vector2Int> affectedTiles =
            GetHitboxTiles(
                gridManager,
                user
            );

        if (affectedTiles == null)
        {
            return enemies;
        }

        // --------------------------------------------------------
        // CHECK EVERY AFFECTED TILE
        // --------------------------------------------------------

        foreach (Vector2Int tile in affectedTiles)
        {
            GameObject unit =
                gridManager.GetUnitAt(tile);

            if (unit == null)
            {
                continue;
            }

            // ----------------------------------------------------
            // DON'T HIT THE SHOOTER
            // ----------------------------------------------------

            if (unit == user)
            {
                continue;
            }

            // ----------------------------------------------------
            // UNIT MUST BE ACTIVE
            // ----------------------------------------------------

            if (!unit.activeInHierarchy)
            {
                continue;
            }

            // ----------------------------------------------------
            // MUST BE AN ATTACK UNIT
            // ----------------------------------------------------

            AttackUnit attackUnit =
                unit.GetComponent<AttackUnit>();

            if (attackUnit == null)
            {
                continue;
            }

            // ----------------------------------------------------
            // DON'T HIT DEAD UNITS
            // ----------------------------------------------------

            if (attackUnit.IsDead())
            {
                continue;
            }

            // ----------------------------------------------------
            // CHECK TEAM
            // ----------------------------------------------------

            if (!CanTargetObject(user, unit))
            {
                continue;
            }

            // ----------------------------------------------------
            // AVOID DUPLICATES
            // ----------------------------------------------------

            if (!enemies.Contains(unit))
            {
                enemies.Add(unit);
            }
        }

        return enemies;
    }
}