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

    [SerializeField]
    private bool spawnEnemiesAutomatically = false;

    private readonly HashSet<AttackUnit> lockedEnemies =
        new HashSet<AttackUnit>();


    private void Awake()
    {
        FindReferences();
    }


    private void Start()
    {
        if (spawnEnemiesAutomatically)
        {
            return;
        }
    }


    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }


    public IEnumerator RunEnemyRound()
    {
        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        if (enemies == null || enemies.Count == 0)
        {
            yield break;
        }

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

            if (IsEnemyLocked(enemy))
            {
                continue;
            }

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !enemiesMoveAfterRound,
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


    public void CheckForEnemies()
    {
        int enemyCount =
            GetEnemyCount();

        if (enemyCount > 0)
        {
            return;
        }

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
        Vector2Int gridPosition
    )
    {
        if (gridManager == null)
        {
            return false;
        }

        if (!gridManager.IsInsideGrid(gridPosition))
        {
            return false;
        }

        if (gridManager.IsCellOccupied(gridPosition))
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
            $"{enemyCharacter.name}_Enemy";


        HealthManager health =
            enemy.GetComponent<HealthManager>();

        AttackUnit attackUnit =
            enemy.GetComponent<AttackUnit>();

        if (
            health == null ||
            attackUnit == null
        )
        {
            Destroy(enemy);

            return false;
        }


        if (
            !gridManager.PlaceUnit(
                enemy,
                gridPosition
            )
        )
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


        LockEnemyForCurrentRound(
            attackUnit
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
