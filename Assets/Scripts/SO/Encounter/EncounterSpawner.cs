using UnityEngine;


// ============================================================
// ENCOUNTER SPAWNER
// ============================================================

public class EncounterSpawner : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private EncounterManager encounterManager;

    [SerializeField]
    private CombatManager combatManager;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();
    }


    // ============================================================
    // FIND REFERENCES
    // ============================================================

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


    // ============================================================
    // SPAWN COMPLETE ENCOUNTER
    // ============================================================
    //
    // Called when an encounter starts.
    //
    // ORDER:
    //
    // 1. Spawn obstacles
    // 2. Spawn enemies
    //
    // Obstacles therefore occupy their cells before enemies
    // search for free cells.
    //
    // ============================================================

    public void SpawnEncounter(
        EncounterDefinition encounter
    )
    {
        if (encounter == null)
        {
            Debug.LogWarning(
                "EncounterSpawner: Encounter is null."
            );

            return;
        }


        FindReferences();


        if (gridManager == null)
        {
            Debug.LogError(
                "EncounterSpawner: GridManager is missing."
            );

            return;
        }


        // ========================================================
        // OBSTACLES
        // ========================================================

        SpawnObstacles(
            encounter
        );


        // ========================================================
        // ENEMIES
        // ========================================================

        SpawnEnemies(
            encounter
        );
    }


    // ============================================================
    // SPAWN OBSTACLES
    // ============================================================
    //
    // Obstacles are spawned ONCE when the encounter begins.
    //
    // Survival rounds DO NOT call this method.
    //
    // ============================================================

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
                    $"EncounterSpawner: Encounter " +
                    $"'{encounter.encounterName}' has obstacle " +
                    $"entry {obstacleIndex} without a prefab."
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


    // ============================================================
    // SPAWN RANDOM OBSTACLE
    // ============================================================

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


        // ========================================================
        // FIND FREE GRID CELL
        // ========================================================

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            Debug.LogWarning(
                $"EncounterSpawner: No free grid cell " +
                $"available for obstacle '{prefab.name}'."
            );

            return;
        }


        // ========================================================
        // CREATE OBSTACLE
        // ========================================================

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


        // ========================================================
        // UNIT TILE PIN
        // ========================================================
        //
        // Obstacles use the same logical grid positioning
        // system as units.
        //
        // ========================================================

        UnitTilePin tilePin =
            obstacle.GetComponent<UnitTilePin>();


        if (tilePin == null)
        {
            tilePin =
                obstacle.AddComponent<UnitTilePin>();
        }


        // ========================================================
        // SET TILE
        // ========================================================

        tilePin.SetTile(
            position
        );


        // ========================================================
        // REGISTER OCCUPIED CELL
        // ========================================================
        //
        // This prevents enemies from spawning on this cell.
        //
        // ========================================================

        gridManager.PlaceUnit(
            obstacle,
            position
        );
    }


    // ============================================================
    // SPAWN ENEMIES
    // ============================================================
    //
    // PUBLIC because EncounterManager calls this for subsequent
    // survival rounds.
    //
    // IMPORTANT:
    //
    // This method does NOT spawn obstacles.
    //
    // ============================================================

    public void SpawnEnemies(
        EncounterDefinition encounter
    )
    {
        if (encounter == null)
        {
            return;
        }


        if (encounter.enemies == null)
        {
            return;
        }


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
                Debug.LogWarning(
                    $"EncounterSpawner: Encounter " +
                    $"'{encounter.encounterName}' has enemy " +
                    $"entry {enemyIndex} without a prefab."
                );

                continue;
            }


            SpawnRandomUnit(
                enemyData.prefab,
                enemyData.character,
                enemyData.enemyId
            );
        }
    }


    // ============================================================
    // SPAWN RANDOM ENEMY
    // ============================================================

    private void SpawnRandomUnit(
        GameObject prefab,
        CharacterSO character,
        string enemyId
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


        // ========================================================
        // DO NOT SPAWN IF ENCOUNTER IS FINISHED
        // ========================================================

        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            return;
        }


        // ========================================================
        // FIND FREE CELL
        // ========================================================

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            Debug.LogWarning(
                $"EncounterSpawner: No free grid cell " +
                $"available for enemy '{enemyId}'."
            );

            return;
        }


        // ========================================================
        // CREATE ENEMY
        // ========================================================

        GameObject unit =
            Instantiate(
                prefab
            );


        if (unit == null)
        {
            return;
        }


        // ========================================================
        // UNIT DATA
        // ========================================================
        //
        // Your UnitData.character is private.
        //
        // Therefore we correctly use the existing
        // Initialize(CharacterSO) method.
        //
        // This also creates the unit's runtime AbilityData.
        //
        // ========================================================

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


        // ========================================================
        // ENCOUNTER UNIT
        // ========================================================

        EncounterUnit encounterUnit =
            unit.GetComponent<EncounterUnit>();


        if (encounterUnit == null)
        {
            encounterUnit =
                unit.AddComponent<EncounterUnit>();
        }


        // ========================================================
        // SET ENCOUNTER UNIT ID
        // ========================================================
        //
        // Uses your actual EncounterUnit API.
        //
        // ========================================================

        encounterUnit.SetEncounterUnitId(
            enemyId
        );


        // ========================================================
        // PLACE ON GRID
        // ========================================================

        gridManager.PlaceUnit(
            unit,
            position
        );


        // ========================================================
        // LOCK ENEMY FOR CURRENT ROUND
        // ========================================================

        if (combatManager != null)
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
    }
}