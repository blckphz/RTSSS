using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CombatUtility
{
    // ==================================================
    // UNITS
    // ==================================================

    public static List<AttackUnit> GetAllUnits()
    {
        List<AttackUnit> units =
            new List<AttackUnit>();

        AttackUnit[] foundUnits =
            Object.FindObjectsOfType<AttackUnit>();

        for (
            int i = 0;
            i < foundUnits.Length;
            i++
        )
        {
            AttackUnit unit =
                foundUnits[i];

            if (IsValidUnit(unit))
            {
                units.Add(unit);
            }
        }

        return units;
    }


    public static List<AttackUnit> GetAllAliveUnits()
    {
        List<AttackUnit> units =
            new List<AttackUnit>();

        AttackUnit[] foundUnits =
            Object.FindObjectsOfType<AttackUnit>();

        for (
            int i = 0;
            i < foundUnits.Length;
            i++
        )
        {
            AttackUnit unit =
                foundUnits[i];

            if (
                IsValidUnit(unit) &&
                !unit.IsDead()
            )
            {
                units.Add(unit);
            }
        }

        return units;
    }


    public static List<GameObject> GetAllUnitObjects()
    {
        List<GameObject> objects =
            new List<GameObject>();

        List<AttackUnit> units =
            GetAllAliveUnits();

        for (
            int i = 0;
            i < units.Count;
            i++
        )
        {
            if (units[i] != null)
            {
                objects.Add(
                    units[i].gameObject
                );
            }
        }

        return objects;
    }


    public static List<AttackUnit> GetUnitsByTeam(
        Team team)
    {
        List<AttackUnit> units =
            new List<AttackUnit>();

        List<AttackUnit> allUnits =
            GetAllAliveUnits();

        for (
            int i = 0;
            i < allUnits.Count;
            i++
        )
        {
            AttackUnit unit =
                allUnits[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.GetTeam() == team)
            {
                units.Add(unit);
            }
        }

        return units;
    }


    public static List<GameObject> GetObjectsByTeam(
        Team team)
    {
        List<GameObject> objects =
            new List<GameObject>();

        List<AttackUnit> units =
            GetUnitsByTeam(team);

        for (
            int i = 0;
            i < units.Count;
            i++
        )
        {
            if (units[i] != null)
            {
                objects.Add(
                    units[i].gameObject
                );
            }
        }

        return objects;
    }


    public static int GetUnitCount(
        Team team)
    {
        return GetUnitsByTeam(team).Count;
    }


    public static bool IsValidUnit(
        AttackUnit unit)
    {
        if (unit == null)
        {
            return false;
        }

        if (!unit.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }


    public static bool IsAlive(
        AttackUnit unit)
    {
        return
            IsValidUnit(unit) &&
            !unit.IsDead();
    }


    // ==================================================
    // TARGETS
    // ==================================================

    public static List<AttackUnit> GetEnemiesFor(
        AttackUnit attacker)
    {
        List<AttackUnit> enemies =
            new List<AttackUnit>();

        if (attacker == null)
        {
            return enemies;
        }

        List<AttackUnit> allUnits =
            GetAllAliveUnits();

        Team attackerTeam =
            attacker.GetTeam();

        for (
            int i = 0;
            i < allUnits.Count;
            i++
        )
        {
            AttackUnit candidate =
                allUnits[i];

            if (
                candidate == null ||
                candidate == attacker
            )
            {
                continue;
            }

            if (
                candidate.GetTeam() ==
                attackerTeam
            )
            {
                continue;
            }

            enemies.Add(candidate);
        }

        return enemies;
    }


    public static GameObject FindNearestEnemy(
        AttackUnit attacker,
        GridManager gridManager)
    {
        if (attacker == null)
        {
            return null;
        }

        List<AttackUnit> enemies =
            GetEnemiesFor(attacker);

        if (enemies.Count == 0)
        {
            return null;
        }

        AttackUnit closest =
            null;

        int closestDistance =
            int.MaxValue;

        Vector2Int attackerPosition =
            gridManager != null
                ? gridManager.WorldToGridPosition(
                    attacker.transform.position
                )
                : Vector2Int.zero;

        for (
            int i = 0;
            i < enemies.Count;
            i++
        )
        {
            AttackUnit candidate =
                enemies[i];

            if (candidate == null)
            {
                continue;
            }

            int distance;

            if (gridManager != null)
            {
                Vector2Int candidatePosition =
                    gridManager.WorldToGridPosition(
                        candidate.transform.position
                    );

                distance =
                    gridManager.GetDistance(
                        attackerPosition,
                        candidatePosition
                    );
            }
            else
            {
                distance =
                    Mathf.RoundToInt(
                        Vector3.Distance(
                            attacker.transform.position,
                            candidate.transform.position
                        )
                    );
            }

            if (distance < closestDistance)
            {
                closestDistance =
                    distance;

                closest =
                    candidate;
            }
        }

        return closest != null
            ? closest.gameObject
            : null;
    }


    // ==================================================
    // ATTACK
    // ==================================================

    public static bool CanAttackTarget(
        AttackUnit attacker,
        GameObject target)
    {
        if (
            attacker == null ||
            target == null
        )
        {
            return false;
        }

        if (attacker.IsDead())
        {
            return false;
        }

        return attacker.IsValidTarget(target);
    }


    public static int ExecuteAllAvailableAttacks(
        AttackUnit attacker)
    {
        if (attacker == null)
        {
            return 0;
        }

        UnitAttackBrain brain =
            attacker.GetComponent<UnitAttackBrain>();

        if (brain == null)
        {
            return 0;
        }

        return brain.UseAllAvailableAbilities();
    }


    // ==================================================
    // FULL UNIT TURN
    // ==================================================

    /// <summary>
    /// Full turn sequence:
    ///
    /// 1. Attack with anything currently available.
    /// 2. Move toward enemy.
    /// 3. Wait until movement has actually finished.
    /// 4. Attack again, but only with abilities where
    ///    canattackwiththisaftermove is enabled.
    /// </summary>
    public static IEnumerator ExecuteUnitTurnCoroutine(
        AttackUnit unit,
        GridManager gridManager)
    {
        if (
            unit == null ||
            unit.IsDead()
        )
        {
            yield break;
        }

        UnitAttackBrain attackBrain =
            unit.GetComponent<UnitAttackBrain>();

        UnitMoveBrain moveBrain =
            unit.GetComponent<UnitMoveBrain>();


        // ==================================================
        // 1. ATTACK BEFORE MOVING
        // ==================================================

        if (
            attackBrain != null &&
            !unit.IsDead()
        )
        {
            int attacks =
                attackBrain.UseAllAvailableAbilities();

            if (attacks > 0)
            {
                yield return null;
            }
        }


        // ==================================================
        // 2. MOVE
        // ==================================================

        if (
            moveBrain != null &&
            !unit.IsDead()
        )
        {
            yield return moveBrain.MoveTowardsEnemy();
        }


        // ==================================================
        // 3. ATTACK AFTER MOVING
        // ==================================================

        if (
            attackBrain != null &&
            !unit.IsDead()
        )
        {
            int attacks =
                attackBrain.UseAllAvailableAbilities(
                    true
                );

            if (attacks > 0)
            {
                yield return null;
            }
        }
    }


    // ==================================================
    // LEGACY NON-COROUTINE TURN
    // ==================================================

    /// <summary>
    /// Kept for compatibility with existing code.
    ///
    /// For proper attack -> move -> attack sequencing,
    /// use ExecuteUnitTurnCoroutine().
    /// </summary>
    public static string ExecuteUnitTurn(
        AttackUnit unit,
        GridManager gridManager)
    {
        if (
            unit == null ||
            unit.IsDead()
        )
        {
            return string.Empty;
        }

        UnitAttackBrain attackBrain =
            unit.GetComponent<UnitAttackBrain>();

        if (attackBrain != null)
        {
            int attacks =
                attackBrain.UseAllAvailableAbilities();

            if (attacks > 0)
            {
                return attackBrain.GetPrimaryAbilityName();
            }
        }

        return string.Empty;
    }


    // ==================================================
    // ENEMY TURN
    // ==================================================

    public static IEnumerator ExecuteEnemyTurn(
        AttackUnit enemy,
        bool moveFirst,
        bool attackAfterMoving)
    {
        if (
            enemy == null ||
            enemy.IsDead()
        )
        {
            yield break;
        }

        UnitAttackBrain attackBrain =
            enemy.GetComponent<UnitAttackBrain>();

        UnitMoveBrain moveBrain =
            enemy.GetComponent<UnitMoveBrain>();

        if (attackBrain == null)
        {
            yield break;
        }


        // ==================================================
        // 1. ATTACK BEFORE MOVING
        // ==================================================

        if (!enemy.IsDead())
        {
            attackBrain.UseAllAvailableAbilities();

            yield return null;
        }


        // ==================================================
        // 2. MOVE
        // ==================================================

        if (
            moveBrain != null &&
            !enemy.IsDead()
        )
        {
            yield return moveBrain.MoveTowardsEnemy();
        }


        // ==================================================
        // 3. ATTACK AFTER MOVING
        // ==================================================

        if (
            attackAfterMoving &&
            !enemy.IsDead()
        )
        {
            attackBrain.UseAllAvailableAbilities(
                true
            );
        }

        yield return null;
    }


    // ==================================================
    // GRID
    // ==================================================

    public static GridManager FindGridManager()
    {
        return Object.FindObjectOfType<GridManager>();
    }


    public static Vector2Int GetGridPosition(
        AttackUnit unit,
        GridManager gridManager)
    {
        if (
            unit == null ||
            gridManager == null
        )
        {
            return Vector2Int.zero;
        }

        return gridManager.WorldToGridPosition(
            unit.transform.position
        );
    }


    // ==================================================
    // WIN / LOSE
    // ==================================================

    public static bool HasTeamAlive(
        Team team)
    {
        return GetUnitCount(team) > 0;
    }


    public static bool AreEnemiesDefeated()
    {
        return !HasTeamAlive(Team.Enemy);
    }


    public static bool ArePlayerUnitsDefeated()
    {
        bool playerAlive =
            HasTeamAlive(Team.Player);

        bool allyAlive =
            HasTeamAlive(Team.Ally);

        return
            !playerAlive &&
            !allyAlive;
    }
}