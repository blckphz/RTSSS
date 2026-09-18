using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }


    // ==================================================
    // FIND NEAREST TARGET
    // ==================================================

    public GameObject FindNearestTarget(
        AttackUnit attacker,
        Team targetTeam)
    {
        if (attacker == null ||
            gridManager == null)
        {
            return null;
        }

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        Vector2Int attackerPosition =
            gridManager.WorldToGridPosition(
                attacker.transform.position
            );

        GameObject nearestTarget = null;

        int nearestDistance =
            int.MaxValue;

        for (int i = 0;
             i < allUnits.Length;
             i++)
        {
            AttackUnit unit =
                allUnits[i];

            if (unit == null ||
                unit == attacker)
            {
                continue;
            }

            if (!unit.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (unit.IsDead())
            {
                continue;
            }

            if (!IsValidTargetTeam(
                    unit.GetTeam(),
                    targetTeam))
            {
                continue;
            }

            Vector2Int targetPosition =
                gridManager.WorldToGridPosition(
                    unit.transform.position
                );

            int distance =
                gridManager.GetDistance(
                    attackerPosition,
                    targetPosition
                );

            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestTarget =
                    unit.gameObject;
            }
        }

        return nearestTarget;
    }


    // ==================================================
    // TEAM VALIDATION
    // ==================================================

    private bool IsValidTargetTeam(
        Team unitTeam,
        Team targetTeam)
    {
        // Enemy targeting player/ally side.
        if (targetTeam == Team.Player)
        {
            return unitTeam == Team.Player ||
                   unitTeam == Team.Ally;
        }

        // Ally targeting enemy side.
        if (targetTeam == Team.Enemy)
        {
            return unitTeam == Team.Enemy;
        }

        // Direct team targeting.
        return unitTeam == targetTeam;
    }


    // ==================================================
    // DISTANCE
    // ==================================================

    public int GetDistanceBetweenUnits(
        AttackUnit a,
        GameObject b)
    {
        if (a == null ||
            b == null ||
            gridManager == null)
        {
            return int.MaxValue;
        }

        Vector2Int aPosition =
            gridManager.WorldToGridPosition(
                a.transform.position
            );

        Vector2Int bPosition =
            gridManager.WorldToGridPosition(
                b.transform.position
            );

        return gridManager.GetDistance(
            aPosition,
            bPosition
        );
    }


    // ==================================================
    // GRID
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}