using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    public bool TryExecuteUnitTurn(AttackUnit attacker, GameObject target)
    {
        if (attacker == null || target == null || !attacker.CanAttack())
            return false;

        int distance = GetDistanceToTarget(attacker, target);
        int attackRange = attacker.GetAttackRange();
        bool isRanged = IsRangedAttacker(attacker);

        // Move if out of range and melee
        if (!isRanged && distance > attackRange)
        {
            ProcessMovement(attacker, target);
            distance = GetDistanceToTarget(attacker, target); // Recalculate after move
        }

        // Perform Attack if in range
        if (distance <= attackRange)
        {
            attacker.Attack(target);
            return true;
        }

        return false;
    }

    public void ProcessMovement(AttackUnit unit, GameObject target)
    {
        if (unit == null || target == null || gridManager == null) return;

        gridManager.CleanupDeadUnits();

        Vector2Int startPos = gridManager.WorldToGridPosition(unit.transform.position);
        Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
        int attackRange = unit.GetAttackRange();

        List<Vector2Int> path = FindPath(startPos, targetPos, attackRange);

        if (path == null || path.Count < 2) return;

        Vector2Int nextPos = path[1];

        if (CanMoveIntoCell(nextPos, targetPos))
        {
            gridManager.MoveUnit(unit.gameObject, startPos, nextPos);
        }
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, int attackRange)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(start) || !gridManager.IsInsideGrid(target))
            return null;

        if (gridManager.GetDistance(start, target) <= attackRange)
            return new List<Vector2Int> { start };

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (gridManager.GetDistance(current, target) <= attackRange)
                return ReconstructPath(cameFrom, start, current);

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbour = current + dir;

                if (visited.Contains(neighbour) || !gridManager.IsInsideGrid(neighbour))
                    continue;

                if (neighbour != target && !CanMoveIntoCell(neighbour, target))
                    continue;

                visited.Add(neighbour);
                cameFrom[neighbour] = current;
                queue.Enqueue(neighbour);
            }
        }

        return null;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int destination)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = destination;
        path.Add(current);

        while (current != start)
        {
            if (!cameFrom.TryGetValue(current, out current))
                return null;

            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private bool CanMoveIntoCell(Vector2Int position, Vector2Int targetPosition)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position)) return false;
        if (position == targetPosition) return true;

        GameObject occupant = gridManager.GetUnitAt(position);
        if (occupant == null || !occupant.activeInHierarchy) return true;

        HealthManager health = occupant.GetComponent<HealthManager>();
        return health == null || health.IsDead();
    }

    private int GetDistanceToTarget(AttackUnit a, GameObject b)
    {
        if (a == null || b == null || gridManager == null) return int.MaxValue;
        return gridManager.GetDistance(
            gridManager.WorldToGridPosition(a.transform.position),
            gridManager.WorldToGridPosition(b.transform.position)
        );
    }

    private bool IsRangedAttacker(AttackUnit unit)
    {
        CharacterSO character = unit?.GetCharacterData();
        return character != null && character.RangedAttacker;
    }
}