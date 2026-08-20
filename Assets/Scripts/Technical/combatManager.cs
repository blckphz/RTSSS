using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private CharacterSO enemyCharacter;

    [Header("Enemy Spawning")]
    [SerializeField] private int minEnemiesToSpawn = 1;
    [SerializeField] private int maxEnemiesToSpawn = 3;

    [Header("Enemy Turn")]
    [SerializeField] private bool enemiesMoveAfterRound = true;
    [SerializeField] private bool enemiesAttackAfterMoving = true;

    [Header("Testing")]
    [SerializeField] private bool spawnEnemiesAutomatically = true;

    [Header("Debug")]
    [SerializeField] private bool debugCombat = true;


    // ==================================================
    // DEBUG
    // ==================================================

    private void CombatDebug(string message)
    {
        if (!debugCombat)
        {
            return;
        }

        Debug.Log(
            $"[CombatManager] {message}",
            gameObject
        );
    }


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

        CombatDebug(
            $"Awake. GridManager=" +
            $"{(gridManager != null ? "FOUND" : "NULL")}"
        );
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
        List<GameObject> enemies =
            GetAllEnemies();

        CombatDebug(
            $"Starting enemy round. " +
            $"Enemy count={enemies.Count}"
        );

        if (enemies.Count == 0)
        {
            yield break;
        }

        foreach (GameObject enemy in enemies)
        {
            if (!IsValidActiveUnit(enemy))
            {
                continue;
            }

            yield return StartCoroutine(
                ProcessEnemyTurn(enemy)
            );

            yield return null;
        }

        CombatDebug(
            "Enemy round complete."
        );
    }


    // ==================================================
    // ENEMY TURN
    // ==================================================

    private IEnumerator ProcessEnemyTurn(
        GameObject enemy)
    {
        if (!IsValidActiveUnit(enemy))
        {
            yield break;
        }

        AttackUnit attackUnit =
            enemy.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            CombatDebug(
                $"{enemy.name}: AttackUnit missing."
            );

            yield break;
        }

        UnitAttackBrain attackBrain =
            enemy.GetComponent<UnitAttackBrain>();

        if (attackBrain == null)
        {
            CombatDebug(
                $"{enemy.name}: UnitAttackBrain missing."
            );

            yield break;
        }

        UnitMoveBrain moveBrain =
            enemy.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            CombatDebug(
                $"{enemy.name}: UnitMoveBrain missing."
            );

            yield break;
        }


        // ==================================================
        // 1. ATTACK FROM CURRENT POSITION
        // ==================================================

        if (enemiesAttackAfterMoving &&
            attackBrain.HasAnyTargetInAbilityRange())
        {
            CombatDebug(
                $"{enemy.name} can attack " +
                "from current position."
            );

            int attacks =
                attackBrain.UseAllAvailableAbilities();

            CombatDebug(
                $"{enemy.name} performed " +
                $"{attacks} attack(s)."
            );

            yield return null;

            yield break;
        }


        // ==================================================
        // 2. MOVE TOWARDS ENEMY
        // ==================================================

        if (enemiesMoveAfterRound)
        {
            bool moved =
                moveBrain.TryMoveTowardsEnemy();

            CombatDebug(
                $"{enemy.name}: " +
                $"Move result={moved}"
            );

            if (moved)
            {
                yield return new WaitForSeconds(
                    0.05f
                );
            }
        }


        // ==================================================
        // 3. ATTACK AFTER MOVING
        // ==================================================

        if (enemiesAttackAfterMoving &&
            IsValidActiveUnit(enemy))
        {
            if (attackBrain.HasAnyTargetInAbilityRange())
            {
                CombatDebug(
                    $"{enemy.name} is now " +
                    "in ability range."
                );

                int attacks =
                    attackBrain.UseAllAvailableAbilities();

                CombatDebug(
                    $"{enemy.name} performed " +
                    $"{attacks} attack(s) after moving."
                );
            }
        }

        yield return null;
    }


    // ==================================================
    // UNIT QUERIES
    // ==================================================

    private bool IsValidActiveUnit(
        GameObject unit)
    {
        if (unit == null)
        {
            return false;
        }

        if (!unit.activeInHierarchy)
        {
            return false;
        }

        HealthManager health =
            unit.GetComponent<HealthManager>();

        return health != null &&
               health.IsAlive();
    }


    private List<GameObject> GetAllEnemies()
    {
        return GetUnitsByTeam(
            Team.Enemy
        );
    }


    private List<GameObject> GetAllAllies()
    {
        return GetUnitsByTeam(
            Team.Ally
        );
    }


    private List<GameObject> GetUnitsByTeam(
        Team targetTeam)
    {
        List<GameObject> units =
            new List<GameObject>();

        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null)
            {
                continue;
            }

            HealthManager health =
                unit.GetHealthManager();

            if (health == null)
            {
                continue;
            }

            if (!health.IsAlive())
            {
                continue;
            }

            if (health.GetTeam() != targetTeam)
            {
                continue;
            }

            units.Add(
                unit.gameObject
            );
        }

        return units;
    }


    // ==================================================
    // SPAWNING
    // ==================================================

    public void CheckForEnemies()
    {
        if (GetEnemyCount() == 0)
        {
            SpawnTestEnemies();
        }
    }


    private int GetEnemyCount()
    {
        AttackUnit[] allUnits =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null)
            {
                continue;
            }

            HealthManager health =
                unit.GetHealthManager();

            if (health == null)
            {
                continue;
            }

            if (health.GetTeam() != Team.Enemy)
            {
                continue;
            }

            if (!health.IsAlive())
            {
                continue;
            }

            count++;
        }

        return count;
    }


    private void SpawnTestEnemies()
    {
        if (gridManager == null ||
            enemyPrefab == null ||
            enemyCharacter == null)
        {
            CombatDebug(
                "Cannot spawn enemies. " +
                "GridManager, enemyPrefab or " +
                "enemyCharacter is NULL."
            );

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
            CombatDebug(
                "No available cells for enemy spawning."
            );

            return;
        }

        amount =
            Mathf.Min(
                amount,
                availableCells.Count
            );

        for (int i = 0;
             i < amount;
             i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availableCells.Count
                );

            Vector2Int spawnPosition =
                availableCells[randomIndex];

            availableCells.RemoveAt(
                randomIndex
            );

            SpawnEnemy(
                spawnPosition
            );
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
            Instantiate(
                enemyPrefab
            );

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

            CombatDebug(
                $"Failed to spawn enemy. " +
                $"HealthManager or AttackUnit missing."
            );

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

        CombatDebug(
            $"Spawned enemy '{enemy.name}' " +
            $"at {gridPosition}."
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

        for (int x = 0;
             x < width;
             x++)
        {
            for (int y = 0;
                 y < height;
                 y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

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
    // PUBLIC INTERFACE
    // ==================================================

    public void StartEnemyRound()
    {
        StartCoroutine(
            RunEnemyRound()
        );
    }


    public GridManager GetGridManager()
    {
        return gridManager;
    }
}