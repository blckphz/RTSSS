
using UnityEngine;

public abstract class UpgradeSO : ScriptableObject
{
    [Header("Upgrade Information")]
    public string UpgradeName;

    [TextArea]
    public string Description;

    public enum Rarity
    {
        Normal,
        Epic,
        Artifact
    }

    [Header("Upgrade Category")]
    [SerializeField] private Rarity rarity = Rarity.Normal;

    public Rarity GetRarity()
    {
        return rarity;
    }

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
