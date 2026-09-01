using UnityEngine;

public class UnitData : MonoBehaviour
{
    // ==================================================
    // CHARACTER
    // ==================================================

    [Header("Character")]
    [SerializeField]
    private CharacterSO character;


    // ==================================================
    // INITIALIZE
    // ==================================================

    public void Initialize(
        CharacterSO characterData)
    {
        character =
            characterData;
    }


    // ==================================================
    // GETTER
    // ==================================================

    public CharacterSO GetCharacter()
    {
        return character;
    }
}