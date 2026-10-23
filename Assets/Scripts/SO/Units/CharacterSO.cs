using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Characters/Character")]
public class CharacterSO : ScriptableObject, ICharacterHolder
{
    [Header("Character Info")]
    public string characterName;
    public Sprite icon;

    [Header("Prefab")]
    public GameObject prefabToSpawn;

    [Header("Combat")]
    public Team team;
    public bool isPlayerCharacter;
    public int maxHealth = 100;
    public int moveRange;
    public bool canwalkdiagonally;

    [Header("Grid Footprint")]
    [Tooltip("Enemy footprint width/depth in grid tiles. 1 = 1x1, 3 = 3x3. Player characters and allies always use 1x1.")]
    [Min(1)] public int enemyFootprintSize = 1;

    [Header("Abilities")]
    [SerializeField] private List<AbilitySO> abilities = new List<AbilitySO>();

    [Header("Attack Type")]
    public bool RangedAttacker;

    [Header("Character Upgrades")]
    [Tooltip("Only these upgrades can appear when this character levels up.")]
    public UpgradeSO[] upgrades;

    public int GetFootprintSize()
    {
        if (isPlayerCharacter || team == Team.Player ||
            string.Equals(team.ToString(), "Ally", StringComparison.OrdinalIgnoreCase))
            return 1;
        return Mathf.Max(1, enemyFootprintSize);
    }

    public List<AbilitySO> GetAbilities() { return abilities; }
    public int GetAbilityCount() { return abilities.Count; }
    public CharacterSO GetCharacterData() { return this; }
    public int GetUpgradeCount() { return upgrades == null ? 0 : upgrades.Length; }

    public UpgradeSO GetUpgrade(int index)
    {
        if (upgrades == null || index < 0 || index >= upgrades.Length) return null;
        return upgrades[index];
    }

    public T GetAbility<T>() where T : AbilitySO
    {
        if (abilities == null) return null;
        for (int i = 0; i < abilities.Count; i++)
            if (abilities[i] is T ability) return ability;
        return null;
    }
}
