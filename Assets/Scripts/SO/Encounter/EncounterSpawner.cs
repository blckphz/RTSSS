
using System.Collections.Generic;
using UnityEngine;

public class EncounterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private CombatManager combatManager;

    [Header("Survival")]
    [SerializeField] private int currentWave = 0;

    [Header("Bushes")]
    [SerializeField, Min(1)] private int minimumBushSpacing = 3;
    [SerializeField, Min(1)] private int bushSpawnAttempts = 50;

    private readonly List<Vector2Int> spawnedBushPositions =
        new List<Vector2Int>();

    private void Awake()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (encounterManager == null)
            encounterManager = FindFirstObjectByType<EncounterManager>();

        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();
    }

    // =========================================================
    // WAVES
    // =========================================================

    public void ResetWaves()
    {
        currentWave = 0;
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    // =========================================================
    // INITIAL ENCOUNTER
    // =========================================================

    public void SpawnEncounter(EncounterDefinition encounter)
    {
        if (encounter == null)
            return;

        FindReferences();

        if (gridManager == null)
            return;

        ResetWaves();
        spawnedBushPositions.Clear();

        SpawnBosses(encounter, false);
        SpawnObstacles(encounter);
        SpawnBushes(encounter);

        // Initial wave enemies can act during Round 1.
        SpawnWave(encounter, false);
    }

    // =========================================================
    // BOSS SPAWNING
    // =========================================================

    private int SpawnBosses(
        EncounterDefinition encounter,
        bool lockForCurrentRound)
    {
        if (encounter == null ||
            encounter.bosses == null ||
            gridManager == null)
        {
            return 0;
        }

        Vector2Int centre = Vector2Int.zero;
        int spawnedCount = 0;

        for (int i = 0; i < encounter.bosses.Count; i++)
        {
            EnemySpawnData bossData = encounter.bosses[i];

            if (bossData == null || bossData.prefab == null)
                continue;

            bool spawned = SpawnUnitAtPosition(
                bossData.prefab,
                bossData.character,
                bossData.enemyId,
                lockForCurrentRound,
                centre
            );

            if (spawned)
            {
                spawnedCount++;
            }
            else
            {
                Debug.LogWarning(
                    $"Could not spawn boss '{bossData.enemyId}' " +
                    $"at grid centre {centre}. The centre may be occupied.",
                    this
                );
            }
        }

        return spawnedCount;
    }

    // =========================================================
    // SPAWN WAVE
    // =========================================================

    public int SpawnWave(
        EncounterDefinition encounter,
        bool lockForCurrentRound = false)
    {
        if (encounter == null || encounter.enemies == null)
            return 0;

        FindReferences();

        if (gridManager == null)
            return 0;

        gridManager.CleanupDeadUnits();

        currentWave++;

        int spawnedCount = 0;

        for (int i = 0; i < encounter.enemies.Count; i++)
        {
            EnemySpawnData enemyData = encounter.enemies[i];

            if (enemyData == null || enemyData.prefab == null)
                continue;

            bool spawned = SpawnRandomUnit(
                enemyData.prefab,
                enemyData.character,
                enemyData.enemyId,
                lockForCurrentRound
            );

            if (spawned)
                spawnedCount++;
        }

        return spawnedCount;
    }

    // =========================================================
    // OBSTACLES
    // =========================================================

    public void SpawnObstacles(EncounterDefinition encounter)
    {
        if (encounter == null || encounter.obstacles == null)
            return;

        for (int i = 0; i < encounter.obstacles.Count; i++)
        {
            ObstacleSpawnData obstacleData = encounter.obstacles[i];

            if (obstacleData == null || obstacleData.prefab == null)
                continue;

            int amount = Mathf.Max(1, obstacleData.amount);

            for (int j = 0; j < amount; j++)
            {
                SpawnRandomObstacle(
                    obstacleData.prefab,
                    i,
                    j
                );
            }
        }
    }

    // =========================================================
    // BUSHES
    // =========================================================

    public void SpawnBushes(EncounterDefinition encounter)
    {
        if (encounter == null || encounter.bushes == null)
            return;

        spawnedBushPositions.Clear();

        for (int i = 0; i < encounter.bushes.Count; i++)
        {
            ObstacleSpawnData bushData = encounter.bushes[i];

            if (bushData == null || bushData.prefab == null)
                continue;

            int amount = Mathf.Max(1, bushData.amount);

            for (int j = 0; j < amount; j++)
            {
                SpawnRandomBush(
                    bushData.prefab,
                    i,
                    j
                );
            }
        }
    }

    // =========================================================
    // SPAWN RANDOM OBSTACLE
    // =========================================================

    private void SpawnRandomObstacle(
        GameObject prefab,
        int obstacleIndex,
        int instanceIndex)
    {
        if (prefab == null || gridManager == null)
            return;

        if (!gridManager.TryGetRandomFreeCell(out Vector2Int position))
            return;

        GameObject obstacle = Instantiate(prefab);

        if (obstacle == null)
            return;

        obstacle.name =
            $"Obstacle_{obstacleIndex}_{instanceIndex}";

        UnitTilePin tilePin =
            obstacle.GetComponent<UnitTilePin>();

        if (tilePin == null)
            tilePin = obstacle.AddComponent<UnitTilePin>();

        tilePin.SetTile(position);

        obstacle.transform.position =
            gridManager.GridToWorldPosition(position);

        // Legacy BushBehav prefabs are also treated as walkable.
        BushBehav bush = obstacle.GetComponent<BushBehav>();

        if (bush != null)
            tilePin.SetWalkable(true);

        // Walkable obstacles reserve their tile but do not occupy
        // the blocking unit grid.
        if (tilePin.IsWalkable)
        {
            gridManager.RegisterWalkableObstacle(position);
            return;
        }

        // Non-walkable obstacles block movement and spawning.
        bool placed = gridManager.PlaceUnit(obstacle, position);

        if (!placed)
            Destroy(obstacle);
    }

    // =========================================================
    // SPAWN RANDOM BUSH
    // =========================================================

    private void SpawnRandomBush(
        GameObject prefab,
        int bushIndex,
        int instanceIndex)
    {
        if (prefab == null || gridManager == null)
            return;

        if (!TryGetRandomBushPosition(out Vector2Int position))
            return;

        GameObject bush = Instantiate(prefab);

        if (bush == null)
            return;

        bush.name = $"Bush_{bushIndex}_{instanceIndex}";

        UnitTilePin tilePin =
            bush.GetComponent<UnitTilePin>();

        if (tilePin == null)
            tilePin = bush.AddComponent<UnitTilePin>();

        // Bushes are always walkable.
        tilePin.SetWalkable(true);
        tilePin.SetTile(position);

        bush.transform.position =
            gridManager.GridToWorldPosition(position);

        // Reserve the tile for spawning, but allow movement onto it.
        gridManager.RegisterWalkableObstacle(position);

        spawnedBushPositions.Add(position);
    }

    // =========================================================
    // RANDOM BUSH POSITION
    // =========================================================

    private bool TryGetRandomBushPosition(out Vector2Int position)
    {
        position = default;

        if (gridManager == null)
            return false;

        int attempts = Mathf.Max(1, bushSpawnAttempts);

        for (int i = 0; i < attempts; i++)
        {
            if (!gridManager.TryGetRandomFreeCell(out Vector2Int candidate))
                return false;

            if (IsFarEnoughFromOtherBushes(candidate))
            {
                position = candidate;
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // BUSH SPACING
    // =========================================================

    private bool IsFarEnoughFromOtherBushes(Vector2Int candidate)
    {
        if (spawnedBushPositions.Count == 0)
            return true;

        float minimumDistance = Mathf.Max(1, minimumBushSpacing);
        float minimumDistanceSquared =
            minimumDistance * minimumDistance;

        for (int i = 0; i < spawnedBushPositions.Count; i++)
        {
            Vector2Int bushPosition = spawnedBushPositions[i];

            int deltaX = candidate.x - bushPosition.x;
            int deltaY = candidate.y - bushPosition.y;

            float distanceSquared =
                deltaX * deltaX + deltaY * deltaY;

            if (distanceSquared < minimumDistanceSquared)
                return false;
        }

        return true;
    }

    // =========================================================
    // ENEMY SPAWNING
    // =========================================================

    private bool SpawnRandomUnit(
        GameObject prefab,
        CharacterSO character,
        string enemyId,
        bool lockForCurrentRound)
    {
        if (prefab == null || gridManager == null)
            return false;

        if (!gridManager.TryGetRandomFreeCell(out Vector2Int position))
            return false;

        return SpawnUnitAtPosition(
            prefab,
            character,
            enemyId,
            lockForCurrentRound,
            position
        );
    }

    // =========================================================
    // SPAWN UNIT AT SPECIFIC GRID POSITION
    // =========================================================

    private bool SpawnUnitAtPosition(
        GameObject prefab,
        CharacterSO character,
        string enemyId,
        bool lockForCurrentRound,
        Vector2Int position)
    {
        if (prefab == null || gridManager == null)
            return false;

        if (encounterManager != null &&
            encounterManager.IsFinished())
        {
            return false;
        }

        GameObject unit = Instantiate(prefab);

        if (unit == null)
            return false;

        unit.name = string.IsNullOrWhiteSpace(enemyId)
            ? "EncounterEnemy"
            : enemyId + "_Enemy";

        UnitData unitData = unit.GetComponent<UnitData>();

        if (unitData == null)
            unitData = unit.AddComponent<UnitData>();

        unitData.Initialize(character);

        EncounterUnit encounterUnit =
            unit.GetComponent<EncounterUnit>();

        if (encounterUnit == null)
            encounterUnit = unit.AddComponent<EncounterUnit>();

        encounterUnit.SetEncounterUnitId(enemyId);

        bool placed = gridManager.PlaceUnit(unit, position);

        if (!placed)
        {
            Destroy(unit);
            return false;
        }

        if (lockForCurrentRound && combatManager != null)
        {
            AttackUnit attackUnit =
                unit.GetComponent<AttackUnit>();

            if (attackUnit != null)
            {
                combatManager.LockEnemyForCurrentRound(
                    attackUnit
                );
            }
        }

        return true;
    }
}
