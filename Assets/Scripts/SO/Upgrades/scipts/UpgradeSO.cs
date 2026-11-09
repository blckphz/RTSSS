
using UnityEngine;

public abstract class UpgradeSO : ScriptableObject
{
    [Header("Upgrade Information")]
    public string UpgradeName;

    [TextArea]
    public string Description;

    public string Artifact;

    public virtual void Apply(CharacterSO character)
    {
        if (character == null)
        {
            Debug.LogError("[UpgradeSO] Character is null.");
            return;
        }
    }

    public virtual void ApplyToUnit(UnitData unitData)
    {
        if (unitData == null)
        {
            Debug.LogError("[UpgradeSO] UnitData is null.");
            return;
        }
    }
}
