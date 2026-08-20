using System.Collections.Generic;
using UnityEngine;

public interface IGridHighlight
{
    // ==================================================
    // PLACEMENT
    // ==================================================

    void SetPlacementTile(
        Vector2Int position
    );

    void ClearPlacementTile();


    // ==================================================
    // ABILITY
    // ==================================================

    void ShowAbilityRange(
        Vector2Int centerPosition,
        int range
    );

    void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets
    );

    void ShowAbilityCell(
        Vector2Int position
    );

    void ClearAbilityRange();


    // ==================================================
    // ATTACK
    // ==================================================

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