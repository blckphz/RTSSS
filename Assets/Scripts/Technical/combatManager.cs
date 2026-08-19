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

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
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
        List<GameObject> enemies = GetAllEnemies();
        if (enemies.Count == 0) yield break;

        // ==================================================
        // MOVEMENT PHASE
        // ==================================================
        if (enemiesMoveAfterRound)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                GameObject enemy = enemies[i];
                if (!IsValidActiveUnit(enemy)) continue;

                if (GetAllAllies().Count == 0) continue;

                GameObject closestAlly = FindClosestAlly(enemy);
                if (closestAlly == null) continue;

                Vector2Int enemyPosition = gridManager.WorldToGridPosition(enemy.transform.position);
                Vector2Int allyPosition = gridManager.WorldToGridPosition(closestAlly.transform.position);

                if (gridManager.GetDistance(enemyPosition, allyPosition) <= 1) continue;

                if (!FindBestMovementTile(enemyPosition, allyPosition, out Vector2Int targetPosition)) continue;

                bool moved = false;
                yield return StartCoroutine(MoveEnemyUsingRigidbody(enemy, enemyPosition, targetPosition, result => moved = result));

                yield return new WaitForSeconds(0.05f);
            }
        }

        // ==================================================
        // ATTACK PHASE
        // ==================================================
        if (enemiesAttackAfterMoving)
        {
            List<GameObject> attackEnemies = GetAllEnemies();
            foreach (GameObject enemy in attackEnemies)
            {
                if (!IsValidActiveUnit(enemy)) continue;

                AttackClosestAlly(enemy);
                yield return null;
            }
        }
    }

    // ==================================================
    // FIND BEST MOVEMENT TILE
    // ==================================================

    private bool FindBestMovementTile(Vector2Int enemyPosition, Vector2Int allyPosition, out Vector2Int bestPosition)
    {
        bestPosition = enemyPosition;
        if (gridManager == null) return false;

        int currentDistance = gridManager.GetDistance(enemyPosition, allyPosition);
        Vector2Int preferredPosition = enemyPosition + GetOneTileDirection(enemyPosition, allyPosition);

        if (IsValidMovementTile(preferredPosition, allyPosition, currentDistance))
        {
            bestPosition = preferredPosition;
            return true;
        }

        if (!allowAlternativeMovement) return false;

        Vector2Int[] alternatives = {
            enemyPosition + new Vector2Int(1, 0),
            enemyPosition + new Vector2Int(-1, 0),
            enemyPosition + new Vector2Int(0, 1),
            enemyPosition + new Vector2Int(0, -1)
        };

        int bestDistance = int.MaxValue;
        bool found = false;

        foreach (Vector2Int position in alternatives)
        {
            if (position == preferredPosition) continue;
            if (!gridManager.IsInsideGrid(position) || gridManager.IsCellOccupied(position)) continue;

            int distance = gridManager.GetDistance(position, allyPosition);
            if (distance >= currentDistance || distance >= bestDistance) continue;

            bestDistance = distance;
            bestPosition = position;
            found = true;
        }

        return found;
    }

    private bool IsValidMovementTile(Vector2Int position, Vector2Int allyPosition, int currentDistance)
    {
        if (gridManager == null || !gridManager.IsInsideGrid(position) || gridManager.IsCellOccupied(position))
            return false;

        return gridManager.GetDistance(position, allyPosition) < currentDistance;
    }

    // ==================================================
    // GET ONE TILE DIRECTION
    // ==================================================

    private Vector2Int GetOneTileDirection(Vector2Int from, Vector2Int to)
    {
        int xDiff = to.x - from.x;
        int yDiff = to.y - from.y;

        if (Mathf.Abs(xDiff) > Mathf.Abs(yDiff))
            return new Vector2Int(xDiff > 0 ? 1 : -1, 0);

        if (Mathf.Abs(yDiff) > Mathf.Abs(xDiff))
            return new Vector2Int(0, yDiff > 0 ? 1 : -1);

        if (xDiff != 0)
            return new Vector2Int(xDiff > 0 ? 1 : -1, 0);

        if (yDiff != 0)
            return new Vector2Int(0, yDiff > 0 ? 1 : -1);

        return Vector2Int.zero;
    }

    // ==================================================
    // MOVE ENEMY USING RIGIDBODY
    // ==================================================

    private IEnumerator MoveEnemyUsingRigidbody(GameObject enemy, Vector2Int oldPosition, Vector2Int newPosition, System.Action<bool> result)
    {
        result?.Invoke(false);

        if (enemy == null || gridManager == null || !gridManager.IsInsideGrid(newPosition) || gridManager.IsCellOccupied(newPosition))
            yield break;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>() ?? enemy.GetComponentInChildren<Rigidbody2D>();
        if (rb == null) yield break;

        Vector3 oldWorldPos = gridManager.GridToWorldPosition(oldPosition);
        Vector3 targetWorldPos = gridManager.GridToWorldPosition(newPosition);

        oldWorldPos.z = enemy.transform.position.z;
        targetWorldPos.z = enemy.transform.position.z;

        bool wasKinematic = rb.isKinematic;
        rb.isKinematic = true;

        if (!gridManager.MoveUnit(enemy, oldPosition, newPosition))
        {
            rb.isKinematic = wasKinematic;
            yield break;
        }

        rb.position = oldWorldPos;
        enemy.transform.position = oldWorldPos;
        Physics2D.SyncTransforms();

        float duration = Mathf.Max(0.01f, moveDuration);
        float timer = 0f;
        Vector2 startPos = oldWorldPos;
        Vector2 endPos = targetWorldPos;

        while (timer < duration)
        {
            if (enemy == null || rb == null) yield break;

            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            rb.MovePosition(Vector2.Lerp(startPos, endPos, smoothT));
            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
        {
            rb.position = endPos;
            rb.isKinematic = wasKinematic;
        }

        enemy.transform.position = targetWorldPos;
        Physics2D.SyncTransforms();
        result?.Invoke(true);
    }

    // ==================================================
    // ATTACK & TARGETING HELPERS
    // ==================================================

    private void AttackClosestAlly(GameObject enemy)
    {
        GameObject closestAlly = FindClosestAlly(enemy);
        if (closestAlly == null) return;

        AttackUnit attackUnit = enemy.GetComponent<AttackUnit>();
        attackUnit?.Attack(closestAlly);
    }

    private GameObject FindClosestAlly(GameObject enemy)
    {
        if (enemy == null || gridManager == null) return null;

        List<GameObject> allies = GetAllAllies();
        if (allies.Count == 0) return null;

        Vector2Int enemyPos = gridManager.WorldToGridPosition(enemy.transform.position);
        GameObject closestAlly = null;
        int closestDistance = int.MaxValue;

        foreach (GameObject ally in allies)
        {
            if (ally == null) continue;

            HealthManager allyHealth = ally.GetComponent<HealthManager>();
            if (allyHealth == null || !allyHealth.IsAlive()) continue;

            Vector2Int allyPos = gridManager.WorldToGridPosition(ally.transform.position);
            int distance = gridManager.GetDistance(enemyPos, allyPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAlly = ally;
            }
        }

        return closestAlly;
    }

    // ==================================================
    // UNIT QUERY UTILITIES
    // ==================================================

    private bool IsValidActiveUnit(GameObject unitObj)
    {
        if (unitObj == null) return false;
        HealthManager health = unitObj.GetComponent<HealthManager>();
        return health != null && health.IsAlive();
    }

    private List<GameObject> GetAllEnemies() => GetUnitsByTeam(Team.Enemy);
    private List<GameObject> GetAllAllies() => GetUnitsByTeam(Team.Ally);

    private List<GameObject> GetUnitsByTeam(Team targetTeam)
    {
        List<GameObject> filteredUnits = new List<GameObject>();
        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null) continue;

            HealthManager health = unit.GetHealthManager();
            if (health != null && health.GetTeam() == targetTeam && health.IsAlive())
            {
                filteredUnits.Add(unit.gameObject);
            }
        }

        return filteredUnits;
    }

    // ==================================================
    // SPAWNING SYSTEM
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
        AttackUnit[] allUnits = FindObjectsByType<AttackUnit>(FindObjectsSortMode.None);
        int count = 0;

        foreach (AttackUnit unit in allUnits)
        {
            if (unit == null) continue;

            HealthManager health = unit.GetHealthManager();
            if (health != null && health.GetTeam() == Team.Enemy && health.IsAlive())
            {
                count++;
            }
        }

        return count;
    }

    private void SpawnTestEnemies()
    {
        if (gridManager == null || enemyPrefab == null || enemyCharacter == null) return;

        int amount = Random.Range(minEnemiesToSpawn, maxEnemiesToSpawn + 1);
        List<Vector2Int> availableCells = GetAvailableCells();

        if (availableCells.Count == 0) return;

        amount = Mathf.Min(amount, availableCells.Count);

        for (int i = 0; i < amount; i++)
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int spawnPosition = availableCells[randomIndex];
            availableCells.RemoveAt(randomIndex);

            SpawnEnemy(spawnPosition);
        }
    }

    private bool SpawnEnemy(Vector2Int gridPosition)
    {
        if (!gridManager.IsInsideGrid(gridPosition) || gridManager.IsCellOccupied(gridPosition))
            return false;

        GameObject enemy = Instantiate(enemyPrefab);
        if (enemy == null) return false;

        enemy.name = $"{enemyCharacter.name}_Enemy_{Random.Range(1000, 9999)}";

        HealthManager healthManager = enemy.GetComponent<HealthManager>();
        AttackUnit attackUnit = enemy.GetComponent<AttackUnit>();

        if (healthManager == null || attackUnit == null || !gridManager.PlaceUnit(enemy, gridPosition))
        {
            Destroy(enemy);
            return false;
        }

        healthManager.Initialize(enemyCharacter);
        attackUnit.Initialize(enemyCharacter);
        return true;
    }

    private List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (gridManager == null) return cells;

        int width = gridManager.GetWidth();
        int height = gridManager.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!gridManager.IsCellOccupied(pos))
                {
                    cells.Add(pos);
                }
            }
        }

        return cells;
    }

    // ==================================================
    // PUBLIC INTERFACE
    // ==================================================

    public void StartEnemyRound() => StartCoroutine(RunEnemyRound());

    public GridManager GetGridManager() => gridManager;
}