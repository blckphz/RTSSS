using UnityEngine;

[CreateAssetMenu(
    fileName = "ChainBounceUpgrade",
    menuName = "Update/Rusty/Chain Upgrade"
)]
public class ChainBounceUpgrade : RustyUpgrades
{
    // ============================================================
    // SETTINGS
    // ============================================================

    [Header("Chain Bounce")]

    [SerializeField, Min(1)]
    private int additionalBounces = 1;


    // ============================================================
    // APPLY
    // ============================================================

    public override void Apply(
        CharacterSO character)
    {
        if (character == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] " +
                "Character is null."
            );

            return;
        }

        ChainLightning chainLightning =
            character.GetAbility<
                ChainLightning
            >();

        if (chainLightning == null)
        {
            Debug.LogError(
                "[ChainBounceUpgrade] " +
                "ChainLightning not found on " +
                character.characterName
            );

            return;
        }

        chainLightning.AddBonusJumps(
            additionalBounces
        );

        Debug.Log(
            "[ChainBounceUpgrade] " +
            character.characterName +
            " gained +" +
            additionalBounces +
            " Chain Lightning bounce."
        );
    }
}