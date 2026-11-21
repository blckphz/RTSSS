using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Enemy Turn")]
    [SerializeField] private bool enemiesMoveAfterRound = true;

    private readonly HashSet<AttackUnit> lockedEnemies =
        new HashSet<AttackUnit>();

    private readonly List<Vector2Int> availableCellsBuffer =
        new List<Vector2Int>(64);

    private readonly List<AttackUnit> enemyTurnBuffer =
        new List<AttackUnit>(16);

    private void Awake()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();
    }

    // =========================================================
    // ENEMY ROUND
    // =========================================================

    public IEnumerator RunEnemyRound()
    {
        enemyTurnBuffer.Clear();

        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(Team.Enemy);

        if (enemies == null || enemies.Count == 0)
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy = enemies[i];

            if (enemy != null)
                enemyTurnBuffer.Add(enemy);
        }

        for (int i = 0; i < enemyTurnBuffer.Count; i++)
        {
            AttackUnit enemy = enemyTurnBuffer[i];

            if (!CombatUtility.IsAlive(enemy))
                continue;

            if (IsEnemyLocked(enemy))
                continue;

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !enemiesMoveAfterRound
                )
            );

            yield return null;
        }
    }

    public void StartEnemyRound()
    {
        StartCoroutine(RunEnemyRound());
    }

    // =========================================================
    // ENEMY LOCKS
    // =========================================================

    public void LockEnemyForCurrentRound(
        AttackUnit enemy
    )
    {
        if (
            enemy == null ||
            enemy.GetTeam() != Team.Enemy
        )
        {
            return;
        }

        lockedEnemies.Add(enemy);
    }

    public void LockEnemiesForCurrentRound(
        List<AttackUnit> enemies
    )
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy = enemies[i];

            if (enemy == null)
                continue;

            LockEnemyForCurrentRound(enemy);
        }
    }

    public void UnlockEnemy(
        AttackUnit enemy
    )
    {
        if (enemy == null)
            return;

        lockedEnemies.Remove(enemy);
    }

    public void ClearEnemyTurnLocks()
    {
        lockedEnemies.Clear();
    }

    public bool IsEnemyLocked(
        AttackUnit enemy
    )
    {
        return enemy != null &&
               lockedEnemies.Contains(enemy);
    }

    public int GetLockedEnemyCount()
    {
        return lockedEnemies.Count;
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public bool EnemiesMoveAfterRound
    {
        get => enemiesMoveAfterRound;
    }


    // =========================================================
    // ENEMY CHECK
    // =========================================================

    public void CheckForEnemies()
    {
        if (GetEnemyCount() > 0)
            return;

    }

    public int GetEnemyCount()
    {
        return CombatUtility.GetUnitCount(Team.Enemy);
    }

    public List<GameObject> GetAllEnemies()
    {
        return CombatUtility.GetObjectsByTeam(Team.Enemy);
    }

    public List<GameObject> GetAllAllies()
    {
        return CombatUtility.GetObjectsByTeam(Team.Ally);
    }



    // =========================================================
    // AVAILABLE CELLS
    // =========================================================

    private List<Vector2Int> GetAvailableCells()
    {
        availableCellsBuffer.Clear();

        if (gridManager == null)
            return availableCellsBuffer;

        int minX = gridManager.GetMinX();
        int maxX = gridManager.GetMaxX();
        int minY = gridManager.GetMinY();
        int maxY = gridManager.GetMaxY();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (!gridManager.IsInsideGrid(position))
                    continue;

                if (gridManager.IsCellOccupied(position))
                    continue;

                availableCellsBuffer.Add(position);
            }
        }

        return availableCellsBuffer;
    }

    public bool TryGetRandomAvailableCell(
        out Vector2Int position
    )
    {
        position = Vector2Int.zero;

        if (gridManager == null)
            FindReferences();

        if (gridManager == null)
            return false;

        List<Vector2Int> cells =
            GetAvailableCells();

        if (cells.Count == 0)
            return false;

        position =
            cells[Random.Range(0, cells.Count)];

        return true;
    }

    // =========================================================
    // PUBLIC ACCESS
    // =========================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public bool AreEnemiesAlive()
    {
        return GetEnemyCount() > 0;
    }

    public bool HasEnemies()
    {
        return GetEnemyCount() > 0;
    }
}
