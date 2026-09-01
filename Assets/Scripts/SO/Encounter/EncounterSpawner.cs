using UnityEngine;

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
    // ENCOUNTER
    // ============================================================

    public void SpawnEncounter(
        EncounterDefinition encounter)
    {
        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            Debug.Log(
                "[EncounterSpawner] Encounter is finished — " +
                "spawning blocked.",
                this
            );

            return;
        }


        if (encounter == null)
        {
            Debug.LogError(
                "[EncounterSpawner] Encounter definition is NULL!",
                this
            );

            return;
        }


        // ========================================================
        // IMPORTANT
        // ========================================================
        //
        // Player units are NOT spawned here anymore.
        //
        // They are created by CardUI when the player
        // drags a squad card onto the grid.
        //
        // ========================================================


        SpawnEnemies(
            encounter
        );
    }


    // ============================================================
    // ENEMIES
    // ============================================================

    public void SpawnEnemies(
        EncounterDefinition encounter)
    {
        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            Debug.Log(
                "[EncounterSpawner] Encounter is finished — " +
                "enemy spawning blocked.",
                this
            );

            return;
        }


        if (
            encounter == null ||
            encounter.enemies == null ||
            encounter.enemies.Count == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < encounter.enemies.Count;
            i++
        )
        {
            EnemySpawnData data =
                encounter.enemies[i];


            if (
                data == null ||
                data.prefab == null
            )
            {
                continue;
            }


            string enemyId =
                data.enemyId;


            if (
                string.IsNullOrWhiteSpace(
                    enemyId
                )
            )
            {
                enemyId =
                    $"Enemy_{i}";
            }


            GameObject spawnedEnemy =
                SpawnRandomUnit(
                    data.prefab,
                    $"Enemy_{i}",
                    enemyId
                );


            // ====================================================
            // LOCK NEW ENEMY
            // ====================================================

            if (spawnedEnemy != null)
            {
                AttackUnit attackUnit =
                    spawnedEnemy.GetComponent<
                        AttackUnit
                    >();


                if (attackUnit != null)
                {
                    if (combatManager == null)
                    {
                        combatManager =
                            FindFirstObjectByType<
                                CombatManager
                            >();
                    }


                    if (combatManager != null)
                    {
                        combatManager
                            .LockEnemyForCurrentRound(
                                attackUnit
                            );
                    }
                }
            }
        }
    }


    // ============================================================
    // RANDOM SPAWN
    // ============================================================

    private GameObject SpawnRandomUnit(
        GameObject prefab,
        string identifier,
        string encounterUnitId)
    {
        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            Debug.Log(
                $"[EncounterSpawner] Encounter is finished — " +
                $"spawn blocked for {identifier}.",
                this
            );

            return null;
        }


        if (gridManager == null)
        {
            Debug.LogError(
                "[EncounterSpawner] GridManager is missing!",
                this
            );

            return null;
        }


        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            Debug.LogError(
                $"[EncounterSpawner] No free grid cell available " +
                $"for {identifier}!",
                this
            );

            return null;
        }


        GameObject unit =
            Instantiate(
                prefab
            );


        if (unit == null)
        {
            Debug.LogError(
                $"[EncounterSpawner] Failed to instantiate " +
                $"{identifier}.",
                this
            );

            return null;
        }


        unit.name =
            $"{identifier}_{prefab.name}";


        // ========================================================
        // ENCOUNTER ID
        // ========================================================

        EncounterUnit encounterUnit =
            unit.GetComponent<
                EncounterUnit
            >();


        if (encounterUnit == null)
        {
            encounterUnit =
                unit.AddComponent<
                    EncounterUnit
                >();
        }


        encounterUnit.SetEncounterUnitId(
            encounterUnitId
        );


        // ========================================================
        // PLACE ON GRID
        // ========================================================

        if (
            !gridManager.PlaceUnit(
                unit,
                position
            )
        )
        {
            Destroy(
                unit
            );


            Debug.LogError(
                $"[EncounterSpawner] Failed to place " +
                $"{identifier} at {position}.",
                this
            );


            return null;
        }


        Debug.Log(
            $"[EncounterSpawner] Spawned {unit.name} " +
            $"at {position}.",
            unit
        );


        return unit;
    }
}