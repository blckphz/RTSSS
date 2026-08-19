using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCharacter",
    menuName = "Characters/Character"
)]
public class CharacterSO : ScriptableObject, ICharacterHolder
{
    [Header("Character Info")]
    public string characterName;

    public Sprite icon;

    [Header("Prefab")]
    public GameObject prefabToSpawn;

    [Header("Combat")]
    public Team team;

    public int maxHealth = 100;

    [Header("Abilities")]
    [SerializeField]
    private List<AbilitySO> abilities =
        new List<AbilitySO>();

    [Header("Attack Type")]
    public bool RangedAttacker;


    // ============================================================
    // GET ABILITIES
    // ============================================================

    public List<AbilitySO> GetAbilities()
    {
        return abilities;
    }


    // ============================================================
    // GET ABILITY COUNT
    // ============================================================

    public int GetAbilityCount()
    {
        return abilities.Count;
    }


    // ============================================================
    // GET CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        return this;
    }
}