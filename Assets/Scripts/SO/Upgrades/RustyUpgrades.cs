using UnityEngine;

public abstract class RustyUpgrades : UpgradeSO
{
    // ============================================================
    // APPLY
    // ============================================================

    public abstract void Apply(
        CharacterSO character
    );
}