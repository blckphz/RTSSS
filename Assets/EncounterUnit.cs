using UnityEngine;

public class EncounterUnit : MonoBehaviour
{
    [Header("Encounter")]
    [SerializeField]
    private string encounterUnitId;


    // ============================================================
    // SET
    // ============================================================

    public void SetEncounterUnitId(
        string id)
    {
        encounterUnitId = id;
    }


    // ============================================================
    // GET
    // ============================================================

    public string GetEncounterUnitId()
    {
        return encounterUnitId;
    }


    public bool HasEncounterUnitId(
        string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return encounterUnitId == id;
    }
}