using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private CharacterSO enemyCharacter;

    [Header("Enemy Spawning")]
    [SerializeField]
    private int minEnemiesToSpawn = 1;

    [SerializeField]
    private int maxEnemiesToSpawn = 3;

    [Header("Enemy Turn")]
    [SerializeField]
    private bool enemiesMoveAfterRound = true;

    [SerializeField]
    private bool enemiesAttackAfterMoving = true;

    [Header("Testing")]
    [SerializeField]
    private bool spawnEnemiesAutomatically = true;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager =
                FindObjectOfType<GridManager>();
        }
    }

    private void Start()
    {
        if (spawnEnemiesAutomatically)
        {
            CheckForEnemies();
        }
    }

    // ==================================================
    // ENEMY ROUND
    // ==================================================

    public IEnumerator RunEnemyRound()
    {
        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy =
                enemies[i];

            if (!CombatUtility.IsAlive(enemy))
            {
                continue;
            }

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    enemiesMoveAfterRound,
                    enemiesAttackAfterMoving
                )
            );

            yield return null;
        }
    }

    public void StartEnemyRound()
    {
        StartCoroutine(
            RunEnemyRound()
        );
    }

    // ==================================================
    // ENEMIES
    // ==================================================

    public void CheckForEnemies()
    {
        if (GetEnemyCount() == 0)
        {
            SpawnTestEnemies();
        }
    }

    public int GetEnemyCount()
    {
        return CombatUtility.GetUnitCount(
            Team.Enemy
        );
    }

    public List<GameObject> GetAllEnemies()
    {
        return CombatUtility.GetObjectsByTeam(
            Team.Enemy
        );
    }

    public List<GameObject> GetAllAllies()
    {
        return CombatUtility.GetObjectsByTeam(
            Team.Ally
        );
    }

    // ==================================================
    // SPAWN
    // ==================================================

    private void SpawnTestEnemies()
    {
        if (gridManager == null ||
            enemyPrefab == null ||
            enemyCharacter == null)
        {
            return;
        }

        int amount =
            Random.Range(
                minEnemiesToSpawn,
                maxEnemiesToSpawn + 1
            );

        List<Vector2Int> availableCells =
            GetAvailableCells();

        if (availableCells.Count == 0)
        {
            return;
        }

        amount =
            Mathf.Min(
                amount,
                availableCells.Count
            );

        for (int i = 0; i < amount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availableCells.Count
                );

            Vector2Int position =
                availableCells[randomIndex];

            availableCells.RemoveAt(
                randomIndex
            );

            SpawnEnemy(position);
        }
    }

    private bool SpawnEnemy(
        Vector2Int gridPosition)
    {
        if (gridManager == null)
        {
            return false;
        }

        if (!gridManager.IsInsideGrid(
                gridPosition))
        {
            return false;
        }

        if (gridManager.IsCellOccupied(
                gridPosition))
        {
            return false;
        }

        GameObject enemy =
            Instantiate(enemyPrefab);

        if (enemy == null)
        {
            return false;
        }

        enemy.name =
            $"{enemyCharacter.name}_Enemy_" +
            $"{Random.Range(1000, 9999)}";

        HealthManager health =
            enemy.GetComponent<HealthManager>();

        AttackUnit attackUnit =
            enemy.GetComponent<AttackUnit>();

        if (health == null ||
            attackUnit == null)
        {
            Destroy(enemy);
            return false;
        }

        if (!gridManager.PlaceUnit(
                enemy,
                gridPosition))
        {
            Destroy(enemy);
            return false;
        }

        health.Initialize(
            enemyCharacter
        );

        attackUnit.Initialize(
            enemyCharacter
        );

        return true;
    }

    private List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();

        if (gridManager == null)
        {
            return cells;
        }

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(x, y);

                if (!gridManager.IsCellOccupied(
                        position))
                {
                    cells.Add(position);
                }
            }
        }

        return cells;
    }

    // ==================================================
    // ACCESSORS
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}