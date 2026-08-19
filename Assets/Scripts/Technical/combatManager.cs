using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class combatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private CharacterSO enemyCharacter;

    [Header("Enemy Spawning")]
    [SerializeField] private int minEnemiesToSpawn = 1;
    [SerializeField] private int maxEnemiesToSpawn = 3;

    [Header("Enemy Movement")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private bool enemiesMoveAfterRound = true;
    [SerializeField] private bool enemiesAttackAfterMoving = true;

    [Header("Movement")]
    [SerializeField] private bool allowAlternativeMovement = true;

    [Header("Testing")]
    [SerializeField] private bool spawnEnemiesAutomatically = true;

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

        if (enemies.Count == 0)
            yield break;

        foreach (GameObject enemy in enemies)
        {
            if (!IsValidActiveUnit(enemy))
                continue;

            AttackUnit attackUnit =
                enemy.GetComponent<AttackUnit>();

            if (attackUnit == null)
                continue;

            // ==================================================
            // 1. ATTACK FROM CURRENT POSITION
            // ==================================================

            if (enemiesAttackAfterMoving &&
                attackUnit.HasAnyTargetInAbilityRange())
            {
                Debug.Log(
                    $"[Combat] {enemy.name} can attack. Staying in place."
                );

                attackUnit.AttackAnyTargetInAbilityRange();

                yield return null;
                continue;
            }

            // ==================================================
            // 2. FIND TARGET TO MOVE TOWARD
            // ==================================================

            GameObject closestAlly =
                FindClosestAlly(enemy);

            if (closestAlly == null)
                continue;

            Vector2Int enemyPosition =
                gridManager.WorldToGridPosition(
                    enemy.transform.position
                );

            Vector2Int allyPosition =
                gridManager.WorldToGridPosition(
                    closestAlly.transform.position
                );

            // ==================================================
            // 3. MOVE CLOSER
            // ==================================================

            if (enemiesMoveAfterRound)
            {
                if (FindBestMovementTile(
                    enemyPosition,
                    allyPosition,
                    out Vector2Int targetPosition))
                {
                    Debug.Log(
                        $"[Combat] {enemy.name} moving {enemyPosition} -> {targetPosition}."
                    );

                    bool moved = false;

                    yield return StartCoroutine(
                        MoveEnemyUsingRigidbody(
                            enemy,
                            enemyPosition,
                            targetPosition,
                            result => moved = result
                        )
                    );

                    yield return new WaitForSeconds(
                        0.05f
                    );
                }
            }

            // ==================================================
            // 4. CHECK ATTACK AGAIN AFTER MOVING
            // ==================================================

            if (enemiesAttackAfterMoving &&
                IsValidActiveUnit(enemy) &&
                attackUnit.HasAnyTargetInAbilityRange())
            {
                Debug.Log(
                    $"[Combat] {enemy.name} is now in ability range after moving."
                );

                attackUnit.AttackAnyTargetInAbilityRange();
            }

            yield return null;
        }
    }

    // ==================================================
    // FIND BEST MOVEMENT TILE
    // ==================================================

    private bool FindBestMovementTile(
        Vector2Int enemyPosition,
        Vector2Int allyPosition,
        out Vector2Int bestPosition)
    {
        bestPosition =
            enemyPosition;

        if (gridManager == null)
            return false;

        int currentDistance =
            gridManager.GetDistance(
                enemyPosition,
                allyPosition
            );

        Vector2Int preferredPosition =
            enemyPosition +
            GetOneTileDirection(
                enemyPosition,
                allyPosition
            );

        if (IsValidMovementTile(
            preferredPosition,
            allyPosition,
            currentDistance))
        {
            bestPosition =
                preferredPosition;

            return true;
        }

        if (!allowAlternativeMovement)
            return false;

        Vector2Int[] alternatives =
        {
            enemyPosition + new Vector2Int(1, 0),
            enemyPosition + new Vector2Int(-1, 0),
            enemyPosition + new Vector2Int(0, 1),
            enemyPosition + new Vector2Int(0, -1)
        };

        int bestDistance =
            int.MaxValue;

        bool found = false;

        foreach (Vector2Int position in alternatives)
        {
            if (position == preferredPosition)
                continue;

            if (!gridManager.IsInsideGrid(position))
                continue;

            if (gridManager.IsCellOccupied(position))
                continue;

            int distance =
                gridManager.GetDistance(
                    position,
                    allyPosition
                );

            if (distance >= currentDistance ||
                distance >= bestDistance)
            {
                continue;
            }

            bestDistance =
                distance;

            bestPosition =
                position;

            found = true;
        }

        return found;
    }

    // ==================================================
    // VALID MOVEMENT TILE
    // ==================================================

    private bool IsValidMovementTile(
        Vector2Int position,
        Vector2Int allyPosition,
        int currentDistance)
    {
        if (gridManager == null)
            return false;

        if (!gridManager.IsInsideGrid(position))
            return false;

        if (gridManager.IsCellOccupied(position))
            return false;

        return gridManager.GetDistance(
            position,
            allyPosition
        ) < currentDistance;
    }

    // ==================================================
    // ONE TILE DIRECTION
    // ==================================================

    private Vector2Int GetOneTileDirection(
        Vector2Int from,
        Vector2Int to)
    {
        int xDiff =
            to.x - from.x;

        int yDiff =
            to.y - from.y;

        if (Mathf.Abs(xDiff) >
            Mathf.Abs(yDiff))
        {
            return new Vector2Int(
                xDiff > 0 ? 1 : -1,
                0
            );
        }

        if (Mathf.Abs(yDiff) >
            Mathf.Abs(xDiff))
        {
            return new Vector2Int(
                0,
                yDiff > 0 ? 1 : -1
            );
        }

        if (xDiff != 0)
        {
            return new Vector2Int(
                xDiff > 0 ? 1 : -1,
                0
            );
        }

        if (yDiff != 0)
        {
            return new Vector2Int(
                0,
                yDiff > 0 ? 1 : -1
            );
        }

        return Vector2Int.zero;
    }

    // ==================================================
    // MOVE ENEMY
    // ==================================================

    private IEnumerator MoveEnemyUsingRigidbody(
        GameObject enemy,
        Vector2Int oldPosition,
        Vector2Int newPosition,
        System.Action<bool> result)
    {
        result?.Invoke(false);

        if (enemy == null ||
            gridManager == null)
        {
            yield break;
        }

        if (!gridManager.IsInsideGrid(
            newPosition))
        {
            yield break;
        }

        if (gridManager.IsCellOccupied(
            newPosition))
        {
            yield break;
        }

        Rigidbody2D rb =
            enemy.GetComponent<Rigidbody2D>() ??
            enemy.GetComponentInChildren<Rigidbody2D>();

        if (rb == null)
            yield break;

        Vector3 oldWorldPosition =
            gridManager.GridToWorldPosition(
                oldPosition
            );

        Vector3 targetWorldPosition =
            gridManager.GridToWorldPosition(
                newPosition
            );

        oldWorldPosition.z =
            enemy.transform.position.z;

        targetWorldPosition.z =
            enemy.transform.position.z;

        bool wasKinematic =
            rb.isKinematic;

        rb.isKinematic = true;

        if (!gridManager.MoveUnit(
            enemy,
            oldPosition,
            newPosition))
        {
            rb.isKinematic =
                wasKinematic;

            yield break;
        }

        rb.position =
            oldWorldPosition;

        enemy.transform.position =
            oldWorldPosition;

        Physics2D.SyncTransforms();

        float duration =
            Mathf.Max(
                0.01f,
                moveDuration
            );

        float timer = 0f;

        Vector2 startPosition =
            oldWorldPosition;

        Vector2 endPosition =
            targetWorldPosition;

        while (timer < duration)
        {
            if (enemy == null ||
                rb == null)
            {
                yield break;
            }

            timer +=
                Time.fixedDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            rb.MovePosition(
                Vector2.Lerp(
                    startPosition,
                    endPosition,
                    smoothT
                )
            );

            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
        {
            rb.position =
                endPosition;

            rb.isKinematic =
                wasKinematic;
        }

        enemy.transform.position =
            targetWorldPosition;

        Physics2D.SyncTransforms();

        result?.Invoke(true);
    }

    // ==================================================
    // FIND CLOSEST ALLY
    // ==================================================

    private GameObject FindClosestAlly(
        GameObject enemy)
    {
        if (enemy == null ||
            gridManager == null)
        {
            return null;
        }

        List<GameObject> allies =
            GetAllAllies();

        if (allies.Count == 0)
            return null;

        Vector2Int enemyPosition =
            gridManager.WorldToGridPosition(
                enemy.transform.position
            );

        GameObject closestAlly = null;

        int closestDistance =
            int.MaxValue;

        foreach (GameObject ally in allies)
        {
            if (ally == null)
                continue;

            HealthManager health =
                ally.GetComponent<HealthManager>();

            if (health == null ||
                !health.IsAlive())
            {
                continue;
            }

            Vector2Int allyPosition =
                gridManager.WorldToGridPosition(
                    ally.transform.position
                );

            int distance =
                gridManager.GetDistance(
                    enemyPosition,
                    allyPosition
                );

            if (distance < closestDistance)
            {
                closestDistance =
                    distance;

                closestAlly =
                    ally;
            }
        }

        return closestAlly;
    }

    // ==================================================
    // UNIT QUERIES
    // ==================================================

    private bool IsValidActiveUnit(
        GameObject unit)
    {
        if (unit == null)
            return false;

        if (!unit.activeInHierarchy)
            return false;

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
                continue;

            HealthManager health =
                unit.GetHealthManager();

            if (health == null)
                continue;

            if (!health.IsAlive())
                continue;

            if (health.GetTeam() != targetTeam)
                continue;

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
                continue;

            HealthManager health =
                unit.GetHealthManager();

            if (health == null)
                continue;

            if (health.GetTeam() != Team.Enemy)
                continue;

            if (!health.IsAlive())
                continue;

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
            return;

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

            Vector2Int spawnPosition =
                availableCells[
                    randomIndex
                ];

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
            return false;

        enemy.name =
            $"{enemyCharacter.name}_Enemy_{Random.Range(1000, 9999)}";

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
            return cells;

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                if (!gridManager.IsCellOccupied(
                    position))
                {
                    cells.Add(
                        position
                    );
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