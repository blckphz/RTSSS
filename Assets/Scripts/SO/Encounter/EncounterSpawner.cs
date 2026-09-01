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


    public void SpawnEncounter(
        EncounterDefinition encounter)
    {
        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
            return;
        }

        if (encounter == null)
        {
            return;
        }

        SpawnEnemies(encounter);
    }


    public void SpawnEnemies(
        EncounterDefinition encounter)
    {
        if (
            encounterManager != null &&
            encounterManager.IsFinished()
        )
        {
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
            return null;
        }

        if (gridManager == null)
        {
            return null;
        }

        if (
            !gridManager.TryGetRandomFreeCell(
                out Vector2Int position
            )
        )
        {
            return null;
        }

        GameObject unit =
            Instantiate(
                prefab
            );

        if (unit == null)
        {
            return null;
        }

        unit.name =
            $"{identifier}_{prefab.name}";


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


        if (
            !gridManager.PlaceUnit(
                unit,
                position
            )
        )
        {
            Destroy(unit);
            return null;
        }

        return unit;
    }
}
