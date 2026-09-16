using UnityEngine;

public class EncounterUnit : MonoBehaviour
{
    [Header("Encounter")]
    [SerializeField]
    private string encounterUnitId;

    public void SetEncounterUnitId(
        string id
    )
    {
        encounterUnitId = id;
    }

    public string GetEncounterUnitId()
    {
        return encounterUnitId;
    }

    public bool HasEncounterUnitId(
        string id
    )
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return encounterUnitId == id;
    }
}