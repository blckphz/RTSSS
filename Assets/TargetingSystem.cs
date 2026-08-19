using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    public GameObject FindNearestTarget(AttackUnit attacker, Team targetTeam)
    {
        if (attacker == null || gridManager == null) return null;

        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);
        Vector2Int attackerPos = gridManager.WorldToGridPosition(attacker.transform.position);

        GameObject nearestTarget = null;
        int nearestDistance = int.MaxValue;

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null || unit == attacker || !unit.gameObject.activeInHierarchy)
                continue;

            HealthManager health = unit.GetHealthManager();
            if (health == null || !health.IsAlive())
                continue;

            Team unitTeam = health.GetTeam();
            bool isEnemyTarget = (targetTeam == Team.Enemy) ? (unitTeam == Team.Enemy) : (unitTeam == Team.Player || unitTeam == Team.Ally);

            if (!isEnemyTarget) continue;

            Vector2Int targetPos = gridManager.WorldToGridPosition(unit.transform.position);
            int distance = gridManager.GetDistance(attackerPos, targetPos);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = unit.gameObject;
            }
        }

        return nearestTarget;
    }

    public int GetDistanceBetweenUnits(AttackUnit a, GameObject b)
    {
        if (a == null || b == null || gridManager == null) return int.MaxValue;

        Vector2Int aPos = gridManager.WorldToGridPosition(a.transform.position);
        Vector2Int bPos = gridManager.WorldToGridPosition(b.transform.position);

        return gridManager.GetDistance(aPos, bPos);
    }
}