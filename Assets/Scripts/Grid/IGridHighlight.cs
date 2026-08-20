using System.Collections.Generic;
using UnityEngine;

public interface IGridHighlight
{
    // =========================================================
    // PLACEMENT
    // =========================================================

    void SetPlacementTile(
        Vector2Int position
    );

    void ClearPlacementTile();


    // =========================================================
    // ABILITY RANGE
    //
    // This is the TARGETING / PREVIEW area.
    //
    // Example Range 4:
    //
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X O X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    // X X X X X X X X X
    //
    // =========================================================

    void ShowAbilityRange(
        Vector2Int centerPosition,
        int range
    );

    void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> positions
    );

    void ShowAbilityCell(
        Vector2Int position
    );

    void ClearAbilityRange();


    // =========================================================
    // ATTACK
    //
    // This is the ACTUAL ATTACK AREA.
    //
    // FrontAttack might produce:
    //
    //     XXX
    //     XXX
    //     XXX
    //      O
    //
    // =========================================================

    void ShowAttackCell(
        Vector2Int position
    );

    void ShowAttackCells(
        List<Vector2Int> positions
    );

    void ShowAttackRange(
        Vector2Int centerPosition,
        int range
    );

    void ClearAttackCells();
}