using System.Collections.Generic;
using UnityEngine;


// ==================================================
// SAVED UNIT DATA
// ==================================================

[System.Serializable]
public class SavedUnitData
{
    public string characterName;

    public int currentHealth;

    public Vector2Int gridPosition;
}


// ==================================================
// PLAYER DATA MANAGER
// ==================================================

public class PlayerDataManager : MonoBehaviour
{
    // ==================================================
    // SINGLETON
    // ==================================================

    public static PlayerDataManager Instance { get; private set; }


    // ==================================================
    // SAVED DATA
    // ==================================================

    [Header("Saved Player Units")]
    [SerializeField]
    private List<SavedUnitData> savedUnits =
        new List<SavedUnitData>();


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;


        DontDestroyOnLoad(gameObject);
    }


    // ==================================================
    // SAVE PLAYER UNITS
    // ==================================================

    public void SavePlayerUnits(
        GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError(
                "[PlayerDataManager] " +
                "GridManager is missing."
            );

            return;
        }


        // Clear previous saved data.

        savedUnits.Clear();


        // --------------------------------------------------
        // GRID BOUNDS
        // --------------------------------------------------

        int minX =
            gridManager.GetMinX();

        int maxX =
            gridManager.GetMaxX();

        int minY =
            gridManager.GetMinY();

        int maxY =
            gridManager.GetMaxY();


        // --------------------------------------------------
        // SEARCH GRID
        // --------------------------------------------------

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPosition =
                    new Vector2Int(x, y);


                // --------------------------------------------------
                // CHECK GRID
                // --------------------------------------------------

                if (!gridManager.IsInsideGrid(
                        gridPosition))
                {
                    continue;
                }


                // --------------------------------------------------
                // GET UNIT
                // --------------------------------------------------

                GameObject unit =
                    gridManager.GetUnitAt(
                        gridPosition
                    );


                if (unit == null)
                {
                    continue;
                }


                // --------------------------------------------------
                // HEALTH MANAGER
                // --------------------------------------------------

                HealthManager healthManager =
                    unit.GetComponent<HealthManager>();


                if (healthManager == null)
                {
                    Debug.LogWarning(
                        "[PlayerDataManager] " +
                        "Unit is missing HealthManager.",
                        unit
                    );

                    continue;
                }


                // --------------------------------------------------
                // ONLY SAVE ALLY UNITS
                // --------------------------------------------------

                if (healthManager.GetTeam() != Team.Ally)
                {
                    continue;
                }


                // --------------------------------------------------
                // UNIT DATA
                // --------------------------------------------------

                UnitData unitData =
                    unit.GetComponent<UnitData>();


                if (unitData == null)
                {
                    Debug.LogWarning(
                        "[PlayerDataManager] " +
                        "Unit is missing UnitData.",
                        unit
                    );

                    continue;
                }


                // --------------------------------------------------
                // CHARACTER
                // --------------------------------------------------

                CharacterSO character =
                    unitData.GetCharacter();


                if (character == null)
                {
                    Debug.LogWarning(
                        "[PlayerDataManager] " +
                        "Unit has no CharacterSO.",
                        unit
                    );

                    continue;
                }


                // --------------------------------------------------
                // CREATE SAVE DATA
                // --------------------------------------------------

                SavedUnitData data =
                    new SavedUnitData();


                data.characterName =
                    character.characterName;


                data.currentHealth =
                    healthManager.GetHealth();


                data.gridPosition =
                    gridPosition;


                // --------------------------------------------------
                // ADD TO LIST
                // --------------------------------------------------

                savedUnits.Add(
                    data
                );
            }
        }


        Debug.Log(
            "[PlayerDataManager] " +
            "Saved " +
            savedUnits.Count +
            " player units."
        );
    }


    // ==================================================
    // GET SAVED UNITS
    // ==================================================

    public List<SavedUnitData> GetSavedUnits()
    {
        return savedUnits;
    }


    // ==================================================
    // GET SAVED UNIT COUNT
    // ==================================================

    public int GetSavedUnitCount()
    {
        return savedUnits.Count;
    }


    // ==================================================
    // CLEAR SAVED UNITS
    // ==================================================

    public void ClearSavedUnits()
    {
        savedUnits.Clear();

        Debug.Log(
            "[PlayerDataManager] " +
            "Saved player units cleared."
        );
    }


    // ==================================================
    // DEBUG
    // ==================================================

    public void DebugPrintSavedUnits()
    {
        Debug.Log(
            "[PlayerDataManager] " +
            "Saved Units: " +
            savedUnits.Count
        );


        for (int i = 0; i < savedUnits.Count; i++)
        {
            SavedUnitData unit =
                savedUnits[i];


            Debug.Log(
                "Unit " +
                i +
                " | Character: " +
                unit.characterName +
                " | Health: " +
                unit.currentHealth +
                " | Grid: " +
                unit.gridPosition
            );
        }
    }
}