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
    // ENEMY TURN LOCK
    // ============================================================

    private readonly HashSet<AttackUnit> lockedEnemies =
        new HashSet<AttackUnit>();


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();
    }


    private void Start()
    {
        if (spawnEnemiesAutomatically)
        {
            Debug.Log(
                "[CombatManager] Automatic enemy spawning is enabled, " +
                "but enemy spawning is intended to be controlled " +
                "by EncounterManager.",
                this
            );

            // Deliberately do not spawn here.
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
            $"Enemies={enemies.Count} | " +
            $"Locked={lockedEnemies.Count}",
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

            // ----------------------------------------------------
            // NEWLY SPAWNED ENEMY LOCK
            // ----------------------------------------------------

            if (IsEnemyLocked(enemy))
            {
                Debug.Log(
                    $"[CombatManager] Skipping newly spawned enemy: " +
                    $"{enemy.name}",
                    enemy
                );

                continue;
            }

            // ----------------------------------------------------
            // NORMAL ENEMY TURN
            // ----------------------------------------------------

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


    // ============================================================
    // MANUAL ENEMY ROUND
    // ============================================================

    public void StartEnemyRound()
    {
        StartCoroutine(
            RunEnemyRound()
        );
    }


    // ============================================================
    // ENEMY TURN LOCK
    // ============================================================

    public void LockEnemyForCurrentRound(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        if (enemy.GetTeam() != Team.Enemy)
        {
            return;
        }

        lockedEnemies.Add(
            enemy
        );

        Debug.Log(
            $"[CombatManager] Enemy locked for current round: " +
            $"{enemy.name}",
            enemy
        );
    }


    public void LockEnemiesForCurrentRound(
        List<AttackUnit> enemies
    )
    {
        if (enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            LockEnemyForCurrentRound(
                enemies[i]
            );
        }
    }


    public void UnlockEnemy(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        lockedEnemies.Remove(
            enemy
        );
    }


    public void ClearEnemyTurnLocks()
    {
        lockedEnemies.Clear();

        Debug.Log(
            "[CombatManager] Enemy turn locks cleared.",
            this
        );
    }


    public bool IsEnemyLocked(
        AttackUnit enemy
    )
    {
        if (enemy == null)
        {
            return false;
        }

        return lockedEnemies.Contains(
            enemy
        );
    }


    public int GetLockedEnemyCount()
    {
        return lockedEnemies.Count;
    }


    // ============================================================
    // ENEMY TURN SETTINGS
    // ============================================================
    //
    // PUBLIC READ-ONLY ACCESSORS.
    //
    // RoundManager uses these instead of trying to access
    // the private serialized fields directly.
    //
    // ============================================================

    public bool EnemiesMoveAfterRound
    {
        get
        {
            return enemiesMoveAfterRound;
        }
    }


    public bool EnemiesAttackAfterMoving
    {
        get
        {
            return enemiesAttackAfterMoving;
        }
    }


    // ============================================================
    // ENEMIES
    // ============================================================

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


        // --------------------------------------------------------
        // LOCK TEST ENEMY
        // --------------------------------------------------------
        //
        // If this is spawned during a round, prevent it from
        // immediately receiving an enemy turn.
        //
        // --------------------------------------------------------

        LockEnemyForCurrentRound(
            attackUnit
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