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

        Debug.Log(
            "[EncounterSpawner] Waves reset.",
            this
        );
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
            Debug.LogError(
                "[EncounterSpawner] Cannot spawn encounter. " +
                "Encounter is null.",
                this
            );

            return;
        }

        FindReferences();

        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterSpawner] Cannot spawn encounter. " +
                "GridManager is missing.",
                this
            );

            return;
        }

        ResetWaves();

        Debug.Log(
            "[EncounterSpawner] Starting encounter spawn.",
            this
        );

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

        Debug.Log(
            "[EncounterSpawner] Initial Wave 1 spawned " +
            spawned +
            " enemies. Initial enemies are UNLOCKED.",
            this
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
            Debug.LogError(
                "[EncounterSpawner] SpawnWave failed: " +
                "encounter is null.",
                this
            );

            return 0;
        }

        if (encounter.enemies == null)
        {
            Debug.LogError(
                "[EncounterSpawner] SpawnWave failed: " +
                "encounter.enemies is null.",
                this
            );

            return 0;
        }

        FindReferences();

        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterSpawner] SpawnWave failed: " +
                "GridManager is missing.",
                this
            );

            return 0;
        }

        gridManager.CleanupDeadUnits();

        currentWave++;

        Debug.Log(
            "[EncounterSpawner] =========================",
            this
        );

        Debug.Log(
            "[EncounterSpawner] SPAWNING WAVE " +
            currentWave +
            " | Enemy definitions: " +
            encounter.enemies.Count +
            " | Locked: " +
            lockForCurrentRound,
            this
        );

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
                Debug.LogWarning(
                    "[EncounterSpawner] Enemy entry " +
                    enemyIndex +
                    " is null.",
                    this
                );

                continue;
            }

            if (enemyData.prefab == null)
            {
                Debug.LogWarning(
                    "[EncounterSpawner] Enemy entry " +
                    enemyIndex +
                    " has no prefab.",
                    this
                );

                continue;
            }

            Debug.Log(
                "[EncounterSpawner] Attempting enemy " +
                enemyIndex +
                " | ID: " +
                enemyData.enemyId,
                this
            );

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

        Debug.Log(
            "[EncounterSpawner] WAVE " +
            currentWave +
            " COMPLETE | Spawned: " +
            spawnedCount +
            " / " +
            encounter.enemies.Count +
            " | Locked: " +
            lockForCurrentRound,
            this
        );

        Debug.Log(
            "[EncounterSpawner] =========================",
            this
        );

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
                Debug.LogWarning(
                    "[EncounterSpawner] Obstacle " +
                    obstacleIndex +
                    " has no prefab.",
                    this
                );

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
            Debug.LogWarning(
                "[EncounterSpawner] Could not find a free cell " +
                "for obstacle.",
                this
            );

            return;
        }

        GameObject obstacle =
            Instantiate(
                prefab
            );

        if (obstacle == null)
        {
            Debug.LogError(
                "[EncounterSpawner] Failed to instantiate obstacle.",
                this
            );

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
            Debug.LogWarning(
                "[EncounterSpawner] Failed to place obstacle at " +
                position +
                ". Destroying it.",
                this
            );

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
            Debug.LogWarning(
                "[EncounterSpawner] Enemy prefab is null.",
                this
            );

            return false;
        }

        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterSpawner] GridManager is missing.",
                this
            );

            return false;
        }

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            Debug.LogWarning(
                "[EncounterSpawner] Encounter is already finished. " +
                "Enemy will not spawn.",
                this
            );

            return false;
        }

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            Debug.LogWarning(
                "[EncounterSpawner] NO FREE GRID CELL FOUND. " +
                "Cannot spawn enemy '" +
                enemyId +
                "'.",
                this
            );

            return false;
        }

        GameObject unit =
            Instantiate(
                prefab
            );

        if (unit == null)
        {
            Debug.LogError(
                "[EncounterSpawner] Failed to instantiate enemy '" +
                enemyId +
                "'.",
                this
            );

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
            Debug.LogWarning(
                "[EncounterSpawner] GridManager refused to place " +
                "enemy '" +
                enemyId +
                "' at " +
                position +
                ". Destroying enemy.",
                this
            );

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

                Debug.Log(
                    "[EncounterSpawner] Locked newly spawned enemy '" +
                    enemyId +
                    "' for current round.",
                    unit
                );
            }
            else
            {
                Debug.LogWarning(
                    "[EncounterSpawner] Enemy '" +
                    enemyId +
                    "' has no AttackUnit. " +
                    "Could not apply wave lock.",
                    unit
                );
            }
        }

        Debug.Log(
            "[EncounterSpawner] SPAWNED enemy '" +
            enemyId +
            "' at " +
            position +
            " | Wave " +
            currentWave +
            " | Locked: " +
            lockForCurrentRound,
            unit
        );

        return true;
    }
}