using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [Header("Optional Test Enemy")]
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
    [Tooltip(
        "If enabled, CombatManager can spawn test enemies when " +
        "CheckForEnemies() is explicitly called. " +
        "It does NOT spawn enemies automatically on Start."
    )]
    [SerializeField]
    private bool spawnEnemiesAutomatically = false;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();
    }


    private void Start()
    {
        // IMPORTANT:
        //
        // Do NOT automatically spawn enemies here.
        //
        // EncounterManager / EncounterSpawner is responsible
        // for creating the encounter.
        //
        // This prevents:
        //
        // Main Menu
        //     ↓
        // CombatManager.Start()
        //     ↓
        // Spawn enemies
        //
        // before EncounterManager has finished preparing
        // the encounter.
        //
        if (spawnEnemiesAutomatically)
        {
            Debug.Log(
                "[CombatManager] Automatic enemy spawning is enabled, " +
                "but enemy spawning is now intended to be controlled " +
                "by EncounterManager.",
                this
            );

            // Deliberately NOT calling CheckForEnemies().
        }
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[CombatManager] GridManager not found yet.",
                this
            );
        }
    }


    // ============================================================
    // ENEMY TURN
    // ============================================================

    /// <summary>
    /// Executes the enemy turn for every living enemy.
    ///
    /// This is called by RoundManager after the Player/Ally phase.
    /// </summary>
    public IEnumerator RunEnemyRound()
    {
        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log(
                "[CombatManager] No enemies available for enemy turn.",
                this
            );

            yield break;
        }

        Debug.Log(
            $"[CombatManager] Enemy turn starting. " +
            $"Enemies={enemies.Count}",
            this
        );

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackUnit enemy =
                enemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (!CombatUtility.IsAlive(enemy))
            {
                continue;
            }

            Debug.Log(
                $"[CombatManager] Enemy acting: {enemy.name}",
                enemy
            );

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !enemiesMoveAfterRound,
                    enemiesAttackAfterMoving
                )
            );

            yield return null;
        }

        Debug.Log(
            "[CombatManager] Enemy turn complete.",
            this
        );
    }


    /// <summary>
    /// Manual helper for UI buttons/testing.
    /// Normally RoundManager controls enemy turns.
    /// </summary>
    public void StartEnemyRound()
    {
        StartCoroutine(
            RunEnemyRound()
        );
    }


    // ============================================================
    // ENEMIES
    // ============================================================

    /// <summary>
    /// Checks whether enemies currently exist.
    ///
    /// This does NOT spawn enemies automatically unless explicitly
    /// requested through the test spawning functionality.
    /// </summary>
    public void CheckForEnemies()
    {
        int enemyCount =
            GetEnemyCount();

        if (enemyCount > 0)
        {
            Debug.Log(
                $"[CombatManager] Enemies already exist: {enemyCount}",
                this
            );

            return;
        }

        Debug.Log(
            "[CombatManager] No enemies currently exist.",
            this
        );

        SpawnTestEnemies();
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


    // ============================================================
    // TEST ENEMY SPAWNING
    // ============================================================

    /// <summary>
    /// Spawns a random number of test enemies on random valid cells.
    ///
    /// This is retained for testing.
    ///
    /// EncounterManager / EncounterSpawner should normally be used
    /// for real encounters.
    /// </summary>
    public void SpawnTestEnemiesNow()
    {
        SpawnTestEnemies();
    }


    private void SpawnTestEnemies()
    {
        if (gridManager == null)
        {
            FindReferences();
        }

        if (
            gridManager == null ||
            enemyPrefab == null ||
            enemyCharacter == null
        )
        {
            Debug.LogWarning(
                "[CombatManager] Cannot spawn test enemies. " +
                "GridManager, enemyPrefab, or enemyCharacter is missing.",
                this
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
            Debug.LogWarning(
                "[CombatManager] No available cells for test enemies.",
                this
            );

            return;
        }

        amount =
            Mathf.Min(
                amount,
                availableCells.Count
            );

        Debug.Log(
            $"[CombatManager] Spawning {amount} test enemies.",
            this
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
        Vector2Int gridPosition
    )
    {
        if (gridManager == null)
        {
            return false;
        }

        if (!gridManager.IsInsideGrid(gridPosition))
        {
            Debug.LogWarning(
                $"[CombatManager] Cannot spawn enemy at " +
                $"{gridPosition}: outside grid.",
                this
            );

            return false;
        }

        if (gridManager.IsCellOccupied(gridPosition))
        {
            Debug.LogWarning(
                $"[CombatManager] Cannot spawn enemy at " +
                $"{gridPosition}: cell occupied.",
                this
            );

            return false;
        }


        // --------------------------------------------------------
        // CREATE ENEMY
        // --------------------------------------------------------

        GameObject enemy =
            Instantiate(enemyPrefab);

        if (enemy == null)
        {
            return false;
        }

        enemy.name =
            $"{enemyCharacter.name}_Enemy";


        // --------------------------------------------------------
        // COMPONENTS
        // --------------------------------------------------------

        HealthManager health =
            enemy.GetComponent<HealthManager>();

        AttackUnit attackUnit =
            enemy.GetComponent<AttackUnit>();

        if (
            health == null ||
            attackUnit == null
        )
        {
            Debug.LogError(
                "[CombatManager] Enemy prefab requires " +
                "HealthManager and AttackUnit.",
                enemy
            );

            Destroy(enemy);

            return false;
        }


        // --------------------------------------------------------
        // PLACE ON GRID
        // --------------------------------------------------------

        if (
            !gridManager.PlaceUnit(
                enemy,
                gridPosition
            )
        )
        {
            Debug.LogWarning(
                $"[CombatManager] Failed to place enemy at " +
                $"{gridPosition}.",
                enemy
            );

            Destroy(enemy);

            return false;
        }


        // --------------------------------------------------------
        // INITIALIZE
        // --------------------------------------------------------

        health.Initialize(
            enemyCharacter
        );

        attackUnit.Initialize(
            enemyCharacter
        );


        Debug.Log(
            $"[CombatManager] Test enemy spawned at " +
            $"{gridPosition}: {enemy.name}",
            enemy
        );

        return true;
    }


    // ============================================================
    // AVAILABLE CELLS
    // ============================================================

    private List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();

        if (gridManager == null)
        {
            return cells;
        }

        int minX =
            gridManager.GetMinX();

        int maxX =
            gridManager.GetMaxX();

        int minY =
            gridManager.GetMinY();

        int maxY =
            gridManager.GetMaxY();


        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                // IMPORTANT:
                //
                // Use the GridManager's logical coordinates.
                //
                // Do NOT use:
                //
                // new Vector2Int(x, y)
                //
                // with x/y starting at zero.
                //
                // Your grid is centered around (0,0).
                //
                if (
                    !gridManager.IsInsideGrid(
                        position
                    )
                )
                {
                    continue;
                }

                if (
                    gridManager.IsCellOccupied(
                        position
                    )
                )
                {
                    continue;
                }

                cells.Add(
                    position
                );
            }
        }

        return cells;
    }


    // ============================================================
    // RANDOM AVAILABLE CELL
    // ============================================================

    /// <summary>
    /// Returns a random valid, unoccupied logical grid position.
    /// Returns false if no position exists.
    /// </summary>
    public bool TryGetRandomAvailableCell(
        out Vector2Int position
    )
    {
        position =
            Vector2Int.zero;

        if (gridManager == null)
        {
            FindReferences();
        }

        if (gridManager == null)
        {
            return false;
        }

        List<Vector2Int> cells =
            GetAvailableCells();

        if (cells.Count == 0)
        {
            return false;
        }

        int randomIndex =
            Random.Range(
                0,
                cells.Count
            );

        position =
            cells[randomIndex];

        return true;
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

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