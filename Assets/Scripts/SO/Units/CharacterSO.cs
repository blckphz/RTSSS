using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCharacter",
    menuName = "Characters/Character"
)]
public class CharacterSO : ScriptableObject, ICharacterHolder
{
    // ==================================================
    // CHARACTER INFO
    // ==================================================

    [Header("Character Info")]

    public string characterName;

    public Sprite icon;


    // ==================================================
    // PREFAB
    // ==================================================

    [Header("Prefab")]

    public GameObject prefabToSpawn;


    // ==================================================
    // COMBAT
    // ==================================================

    [Header("Combat")]

    public Team team;

    public bool isPlayerCharacter;

    public int maxHealth = 100;

    public int moveRange;

    public bool canwalkdiagonally;


    // ==================================================
    // ABILITIES
    // ==================================================

    [Header("Abilities")]

    [SerializeField]
    private List<AbilitySO> abilities =
        new List<AbilitySO>();


    // ==================================================
    // ATTACK TYPE
    // ==================================================

    [Header("Attack Type")]

    public bool RangedAttacker;


    // ==================================================
    // UPGRADES
    // ==================================================

    [Header("Upgrades")]

    public UpgradeSO[] upgrades;


    // ==================================================
    // GETTERS
    // ==================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }


    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    public CharacterSO GetCharacterData()
    {
        return this;
    }


    // ==================================================
    // FIND ABILITY
    // ==================================================

    public T GetAbility<T>()
        where T : AbilitySO
    {
        if (abilities == null)
        {
            return null;
        }

        for (
            int i = 0;
            i < abilities.Count;
            i++
        )
        {
            if (
                abilities[i] is T ability
            )
            {
                return ability;
            }
        }

        return null;
    }
}