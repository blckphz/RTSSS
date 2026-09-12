using UnityEngine;

[CreateAssetMenu(
    fileName = "ChainBounceUpgrade",
    menuName = "Update/Rusty/Chain Upgrade"
)]
public class ChainBounceUpgrade : RustyUpgrades
{
    [Header("Chain Bounce")]
    [SerializeField, Min(1)]
    private int additionalBounces = 1;


    public override void Apply(CharacterSO character)
    {
        if (character == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] Character is null."
            );

            return;
        }

        Debug.Log(
            "[ChainBounceUpgrade] " +
            "Character received Chain Bounce upgrade: " +
            character.characterName
        );
    }


    public void ApplyToUnit(UnitData unitData)
    {
        if (unitData == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] UnitData is null."
            );

            return;
        }


        CharacterSO character =
            unitData.GetCharacter();

        if (character == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] " +
                "Unit has no CharacterSO."
            );

            return;
        }


        ChainLightning chainLightning =
            character.GetAbility<ChainLightning>();

        if (chainLightning == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] " +
                "ChainLightning not found on " +
                character.characterName
            );

            return;
        }


        // IMPORTANT:
        //
        // We are NOT changing the ChainLightning asset.
        //
        // The bonus is stored inside this specific UnitData.
        unitData.AddBonusJumps(
            chainLightning,
            additionalBounces
        );


        Debug.Log(
            "[ChainBounceUpgrade] " +
            character.characterName +
            " (" +
            unitData.name +
            ") gained +" +
            additionalBounces +
            " Chain Lightning bounce(s)."
        );
    }
}