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
}
