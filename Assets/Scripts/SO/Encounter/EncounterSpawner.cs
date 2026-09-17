using UnityEngine;

public class EncounterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private EncounterManager encounterManager;

    [SerializeField]
    private CombatManager combatManager;

    [Header("Survival")]
    [SerializeField]
    private int currentWave = 0;

    private void Awake()
    {
        FindReferences();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }

        if (combatManager == null)
        {
            combatManager =
                FindFirstObjectByType<CombatManager>();
        }
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

    public void SpawnEncounter(
        EncounterDefinition encounter
    )
    {
        if (encounter == null)
        {
            return;
        }

        FindReferences();

        if (gridManager == null)
        {
            return;
        }

        ResetWaves();

        SpawnObstacles(
            encounter
        );

        /*
         * =====================================================
         * IMPORTANT
         * =====================================================
         *
         * Initial Wave 1 is NOT locked.
         *
         * These enemies are present from the beginning of the
         * encounter and therefore CAN act during Round 1.
         */
        int spawned =
            SpawnWave(
                encounter,
                false
            );
    }

    // =========================================================
    // SPAWN WAVE
    // =========================================================

    public int SpawnWave(
        EncounterDefinition encounter,
        bool lockForCurrentRound = false
    )
    {
        if (encounter == null)
        {
            return 0;
        }

        if (encounter.enemies == null)
        {
            return 0;
        }

        FindReferences();

        if (gridManager == null)
        {
            return 0;
        }

        gridManager.CleanupDeadUnits();

        currentWave++;

        int spawnedCount = 0;

        for (
            int enemyIndex = 0;
            enemyIndex < encounter.enemies.Count;
            enemyIndex++
        )
        {
            EnemySpawnData enemyData =
                encounter.enemies[
                    enemyIndex
                ];

            if (enemyData == null)
            {
                continue;
            }

            if (enemyData.prefab == null)
            {
                continue;
            }

            bool spawned =
                SpawnRandomUnit(
                    enemyData.prefab,
                    enemyData.character,
                    enemyData.enemyId,
                    lockForCurrentRound
                );

            if (spawned)
            {
                spawnedCount++;
            }
        }

        return spawnedCount;
    }

    // =========================================================
    // OBSTACLES
    // =========================================================

    public void SpawnObstacles(
        EncounterDefinition encounter
    )
    {
        if (encounter == null)
        {
            return;
        }

        if (encounter.obstacles == null)
        {
            return;
        }

        for (
            int obstacleIndex = 0;
            obstacleIndex < encounter.obstacles.Count;
            obstacleIndex++
        )
        {
            ObstacleSpawnData obstacleData =
                encounter.obstacles[
                    obstacleIndex
                ];

            if (obstacleData == null)
            {
                continue;
            }

            if (obstacleData.prefab == null)
            {
                continue;
            }

            int amount =
                Mathf.Max(
                    1,
                    obstacleData.amount
                );

            for (
                int instanceIndex = 0;
                instanceIndex < amount;
                instanceIndex++
            )
            {
                SpawnRandomObstacle(
                    obstacleData.prefab,
                    obstacleIndex,
                    instanceIndex
                );
            }
        }
    }

    private void SpawnRandomObstacle(
        GameObject prefab,
        int obstacleIndex,
        int instanceIndex
    )
    {
        if (prefab == null)
        {
            return;
        }

        if (gridManager == null)
        {
            return;
        }

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            return;
        }

        GameObject obstacle =
            Instantiate(
                prefab
            );

        if (obstacle == null)
        {
            return;
        }

        obstacle.name =
            $"Obstacle_{obstacleIndex}_{instanceIndex}";

        UnitTilePin tilePin =
            obstacle.GetComponent<UnitTilePin>();

        if (tilePin == null)
        {
            tilePin =
                obstacle.AddComponent<UnitTilePin>();
        }

        tilePin.SetTile(
            position
        );

        bool placed =
            gridManager.PlaceUnit(
                obstacle,
                position
            );

        if (!placed)
        {
            Destroy(
                obstacle
            );

            return;
        }
    }

    // =========================================================
    // ENEMY SPAWNING
    // =========================================================

    private bool SpawnRandomUnit(
        GameObject prefab,
        CharacterSO character,
        string enemyId,
        bool lockForCurrentRound
    )
    {
        if (prefab == null)
        {
            return false;
        }

        if (gridManager == null)
        {
            return false;
        }

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            return false;
        }

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            return false;
        }

        GameObject unit =
            Instantiate(
                prefab
            );

        if (unit == null)
        {
            return false;
        }

        unit.name =
            string.IsNullOrWhiteSpace(enemyId)
                ? "EncounterEnemy"
                : enemyId + "_Enemy";

        // -----------------------------------------------------
        // UNIT DATA
        // -----------------------------------------------------

        UnitData unitData =
            unit.GetComponent<UnitData>();

        if (unitData == null)
        {
            unitData =
                unit.AddComponent<UnitData>();
        }

        unitData.Initialize(
            character
        );

        // -----------------------------------------------------
        // ENCOUNTER UNIT
        // -----------------------------------------------------

        EncounterUnit encounterUnit =
            unit.GetComponent<EncounterUnit>();

        if (encounterUnit == null)
        {
            encounterUnit =
                unit.AddComponent<EncounterUnit>();
        }

        encounterUnit.SetEncounterUnitId(
            enemyId
        );

        // -----------------------------------------------------
        // GRID
        // -----------------------------------------------------

        bool placed =
            gridManager.PlaceUnit(
                unit,
                position
            );

        if (!placed)
        {
            Destroy(
                unit
            );

            return false;
        }

        // -----------------------------------------------------
        // WAVE TURN LOCK
        // -----------------------------------------------------

        /*
         * Only Wave 2+ passes TRUE here.
         *
         * Wave 1:
         *     false -> can act in Round 1
         *
         * Wave 2:
         *     true -> skips Round 2
         *
         * Wave 3:
         *     true -> skips Round 3
         */
        if (
            lockForCurrentRound &&
            combatManager != null
        )
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