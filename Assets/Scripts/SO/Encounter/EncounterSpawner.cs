using System.Collections.Generic;
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

    [Header("Bushes")]
    [SerializeField]
    [Min(1)]
    private int minimumBushSpacing = 3;

    [SerializeField]
    [Min(1)]
    private int bushSpawnAttempts = 50;


    // =========================================================
    // INTERNAL BUSH POSITIONS
    // =========================================================

    private readonly List<Vector2Int> spawnedBushPositions =
        new List<Vector2Int>();


    // =========================================================
    // REFERENCES
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }


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

        spawnedBushPositions.Clear();

        // -----------------------------------------------------
        // BOSSES
        // -----------------------------------------------------
        // Spawn bosses first so obstacles, bushes, and regular
        // enemies cannot occupy the centre cell beforehand.
        // -----------------------------------------------------

        SpawnBosses(
            encounter,
            false
        );

        // -----------------------------------------------------
        // NORMAL OBSTACLES
        // -----------------------------------------------------

        SpawnObstacles(
            encounter
        );

        // -----------------------------------------------------
        // BUSHES
        // -----------------------------------------------------
        //
        // Bushes are spawned separately so they can use their
        // own spacing rules.
        //
        // They are still non-blocking just like BushBehav
        // objects previously were.
        // -----------------------------------------------------

        SpawnBushes(
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
        SpawnWave(
            encounter,
            false
        );
    }


    // =========================================================
    // BOSS SPAWNING
    // =========================================================

    private int SpawnBosses(
        EncounterDefinition encounter,
        bool lockForCurrentRound
    )
    {
        if (
            encounter == null ||
            encounter.bosses == null ||
            gridManager == null
        )
        {
            return 0;
        }

        // GridManager uses logical coordinates centred at (0, 0).
        Vector2Int centre = Vector2Int.zero;

        int spawnedCount = 0;

        for (
            int bossIndex = 0;
            bossIndex < encounter.bosses.Count;
            bossIndex++
        )
        {
            EnemySpawnData bossData =
                encounter.bosses[bossIndex];

            if (
                bossData == null ||
                bossData.prefab == null
            )
            {
                continue;
            }

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
                    $"at grid centre {centre}. The centre cell may " +
                    "be unavailable, or another boss may already " +
                    "occupy it.",
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


    // =========================================================
    // BUSHES
    // =========================================================

    public void SpawnBushes(
        EncounterDefinition encounter
    )
    {
        if (encounter == null)
        {
            return;
        }

        if (encounter.bushes == null)
        {
            return;
        }

        spawnedBushPositions.Clear();

        for (
            int bushIndex = 0;
            bushIndex < encounter.bushes.Count;
            bushIndex++
        )
        {
            ObstacleSpawnData bushData =
                encounter.bushes[
                    bushIndex
                ];

            if (bushData == null)
            {
                continue;
            }

            if (bushData.prefab == null)
            {
                continue;
            }

            int amount =
                Mathf.Max(
                    1,
                    bushData.amount
                );

            for (
                int instanceIndex = 0;
                instanceIndex < amount;
                instanceIndex++
            )
            {
                SpawnRandomBush(
                    bushData.prefab,
                    bushIndex,
                    instanceIndex
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


        // -----------------------------------------------------
        // TILE PIN
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        obstacle.transform.position =
            gridManager.GridToWorldPosition(
                position
            );


        // -----------------------------------------------------
        // BUSH COMPATIBILITY
        // -----------------------------------------------------
        //
        // This is intentionally retained so old encounter
        // definitions that still have a BushBehav prefab inside
        // "obstacles" continue to work.
        // -----------------------------------------------------

        BushBehav bush =
            obstacle.GetComponent<BushBehav>();

        if (bush != null)
        {
            gridManager.RegisterWalkableObstacle(
                position
            );

            return;
        }


        // -----------------------------------------------------
        // NORMAL OBSTACLE
        // -----------------------------------------------------
        //
        // Normal obstacles ARE blocking.
        // -----------------------------------------------------

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
    // SPAWN RANDOM BUSH
    // =========================================================

    private void SpawnRandomBush(
        GameObject prefab,
        int bushIndex,
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
            !TryGetRandomBushPosition(
                out Vector2Int position
            )
        )
        {
            return;
        }

        GameObject bush =
            Instantiate(
                prefab
            );

        if (bush == null)
        {
            return;
        }

        bush.name =
            $"Bush_{bushIndex}_{instanceIndex}";


        // -----------------------------------------------------
        // TILE PIN
        // -----------------------------------------------------

        UnitTilePin tilePin =
            bush.GetComponent<UnitTilePin>();

        if (tilePin == null)
        {
            tilePin =
                bush.AddComponent<UnitTilePin>();
        }

        tilePin.SetTile(
            position
        );


        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        bush.transform.position =
            gridManager.GridToWorldPosition(
                position
            );


        // -----------------------------------------------------
        // BUSH
        // -----------------------------------------------------
        //
        // Bushes are NON-BLOCKING.
        //
        // The player can move onto their tile.
        //
        // The tile is still registered so another spawned
        // object does not occupy the exact same cell.
        // -----------------------------------------------------

        gridManager.RegisterWalkableObstacle(
            position
        );


        // -----------------------------------------------------
        // REMEMBER POSITION
        // -----------------------------------------------------

        spawnedBushPositions.Add(
            position
        );
    }


    // =========================================================
    // RANDOM BUSH POSITION
    // =========================================================
    //
    // Bushes must be farther apart from OTHER BUSHES.
    //
    // They can still spawn normally relative to obstacles and
    // other grid occupants because TryGetRandomFreeCell()
    // handles normal grid occupancy.
    // =========================================================

    private bool TryGetRandomBushPosition(
        out Vector2Int position
    )
    {
        position = default;

        if (gridManager == null)
        {
            return false;
        }

        int attempts =
            Mathf.Max(
                1,
                bushSpawnAttempts
            );

        for (
            int attempt = 0;
            attempt < attempts;
            attempt++
        )
        {
            if (
                !gridManager.TryGetRandomFreeCell(
                    out Vector2Int candidate
                )
            )
            {
                return false;
            }

            if (
                IsFarEnoughFromOtherBushes(
                    candidate
                )
            )
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

    private bool IsFarEnoughFromOtherBushes(
        Vector2Int candidate
    )
    {
        if (spawnedBushPositions.Count == 0)
        {
            return true;
        }

        float minimumDistance =
            Mathf.Max(
                1,
                minimumBushSpacing
            );

        float minimumDistanceSquared =
            minimumDistance *
            minimumDistance;

        for (
            int i = 0;
            i < spawnedBushPositions.Count;
            i++
        )
        {
            Vector2Int bushPosition =
                spawnedBushPositions[i];

            int deltaX =
                candidate.x -
                bushPosition.x;

            int deltaY =
                candidate.y -
                bushPosition.y;

            float distanceSquared =
                (deltaX * deltaX) +
                (deltaY * deltaY);

            if (
                distanceSquared <
                minimumDistanceSquared
            )
            {
                return false;
            }
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
        bool lockForCurrentRound
    )
    {
        if (prefab == null || gridManager == null)
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

        return SpawnUnitAtPosition(
            prefab,
            character,
            enemyId,
            lockForCurrentRound,
            position
        );
    }


    // =========================================================
    // SPAWN UNIT AT A SPECIFIC GRID POSITION
    // =========================================================

    private bool SpawnUnitAtPosition(
        GameObject prefab,
        CharacterSO character,
        string enemyId,
        bool lockForCurrentRound,
        Vector2Int position
    )
    {
        if (prefab == null || gridManager == null)
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

        GameObject unit = Instantiate(prefab);

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

        UnitData unitData = unit.GetComponent<UnitData>();

        if (unitData == null)
        {
            unitData = unit.AddComponent<UnitData>();
        }

        unitData.Initialize(character);

        // -----------------------------------------------------
        // ENCOUNTER UNIT
        // -----------------------------------------------------

        EncounterUnit encounterUnit =
            unit.GetComponent<EncounterUnit>();

        if (encounterUnit == null)
        {
            encounterUnit = unit.AddComponent<EncounterUnit>();
        }

        encounterUnit.SetEncounterUnitId(enemyId);

        // -----------------------------------------------------
        // GRID PLACEMENT
        // -----------------------------------------------------

        bool placed = gridManager.PlaceUnit(unit, position);

        if (!placed)
        {
            Destroy(unit);
            return false;
        }

        // -----------------------------------------------------
        // WAVE TURN LOCK
        // -----------------------------------------------------

        /*
         * Initial encounter wave passes false, so units can act
         * during Round 1. Later waves may pass true to lock the
         * spawned enemy for the current round.
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
