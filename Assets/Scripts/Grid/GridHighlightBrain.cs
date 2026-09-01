using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightBrain : MonoBehaviour
{
    private enum HighlightState
    {
        None,
        MovementRange,
        BasicAbilityRange,
        ScriptableObjectAbility,
        CustomTiles,
        OffsetCells,
        SingleCell
    }

    #region Inspector Fields

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;

    [Header("Board Rotation")]
    [SerializeField] private Transform boardRotationTransform;
    [SerializeField] private bool compensateForBoardRotation = true;
    [SerializeField, Min(0.01f)] private float rotationSettleDelay = 0.08f;

    #endregion

    #region Private State & Cache

    private const float RotationThresholdSqr = 0.0001f;

    private HighlightState currentHighlightState = HighlightState.None;

    private Vector3 lastKnownBoardRotation;
    private bool isBoardRotating;
    private Coroutine rotationRefreshCoroutine;
    private WaitForSeconds settleDelayWait;

    private Vector2Int cachedCenterPos;
    private Vector2Int cachedUserTile;
    private int cachedRange;
    private GameObject cachedUser;
    private AbilitySO cachedAbility;
    private List<Vector2Int> cachedCustomPositions;
    private List<Vector2Int> cachedOffsets;
    private bool hasCachedUserTile;

    private UnitTilePin cachedUserTilePin;
    private UnitMoveBrain cachedMoveBrain;

    private int minGridX;
    private int maxGridX;
    private int minGridY;
    private int maxGridY;

    private readonly List<Vector2Int> reusableTileList =
        new List<Vector2Int>(64);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeReferences();
        CacheGridBounds();
        CacheBoardRotation();

        settleDelayWait =
            new WaitForSeconds(rotationSettleDelay);
    }

    private void Update()
    {
        CheckBoardRotationChange();
        CheckHighlightedUnitTileChanged();
    }

    private void OnDisable()
    {
        StopRotationRefresh();
        isBoardRotating = false;
    }

    #endregion

    #region Initialization & Setup

    private void InitializeReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                GetComponent<GridManager>() ??
                FindFirstObjectByType<GridManager>();

            if (gridManager == null)
            {
                Debug.LogError(
                    "[GridHighlightBrain] GridManager reference missing.",
                    this
                );
            }
        }

        if (highlightManager == null)
        {
            highlightManager =
                GetComponent<GridHighlightManager>() ??
                FindFirstObjectByType<GridHighlightManager>();

            if (highlightManager == null)
            {
                Debug.LogError(
                    "[GridHighlightBrain] GridHighlightManager reference missing.",
                    this
                );
            }
        }

        FindBoardRotationTransform();
    }

    private void FindBoardRotationTransform()
    {
        if (boardRotationTransform != null)
            return;

        BoardViewController boardController =
            FindFirstObjectByType<BoardViewController>();

        if (boardController != null)
        {
            boardRotationTransform =
                boardController.transform;

            return;
        }

        boardRotationTransform =
            gridManager != null
                ? gridManager.transform
                : transform;
    }

    private void CacheGridBounds()
    {
        if (gridManager == null)
            return;

        minGridX = gridManager.GetMinX();
        maxGridX = gridManager.GetMaxX();
        minGridY = gridManager.GetMinY();
        maxGridY = gridManager.GetMaxY();
    }

    public void RefreshGridBounds()
    {
        if (gridManager == null)
        {
            InitializeReferences();

            if (gridManager == null)
                return;
        }

        CacheGridBounds();

        if (highlightManager != null)
            highlightManager.RebuildTileCache();

        RefreshActiveHighlights();
    }

    #endregion

    #region Board Rotation Handling

    private void CacheBoardRotation()
    {
        lastKnownBoardRotation =
            GetBoardRotation();
    }

    private Vector3 GetBoardRotation()
    {
        if (boardRotationTransform != null)
            return boardRotationTransform.eulerAngles;

        if (gridManager != null)
            return gridManager.transform.eulerAngles;

        return transform.eulerAngles;
    }

    private void CheckBoardRotationChange()
    {
        Vector3 currentRotation =
            GetBoardRotation();

        if (
            (currentRotation - lastKnownBoardRotation).sqrMagnitude
            <= RotationThresholdSqr
        )
        {
            return;
        }

        lastKnownBoardRotation =
            currentRotation;

        if (!isBoardRotating)
        {
            isBoardRotating = true;
            HideHighlightsDuringRotation();
        }

        RestartRotationRefresh();
    }

    private void RestartRotationRefresh()
    {
        StopRotationRefresh();

        rotationRefreshCoroutine =
            StartCoroutine(
                RefreshAfterRotationStops()
            );
    }

    private void StopRotationRefresh()
    {
        if (rotationRefreshCoroutine == null)
            return;

        StopCoroutine(rotationRefreshCoroutine);
        rotationRefreshCoroutine = null;
    }

    private void HideHighlightsDuringRotation()
    {
        if (highlightManager == null)
            return;

        highlightManager.ClearMovementRange();
        highlightManager.ClearAbilityRange();
    }

    private IEnumerator RefreshAfterRotationStops()
    {
        yield return settleDelayWait;

        isBoardRotating = false;
        rotationRefreshCoroutine = null;

        RefreshActiveHighlights();
    }

    #endregion

    #region Unit Tracking

    private void CheckHighlightedUnitTileChanged()
    {
        if (
            cachedUser == null ||
            isBoardRotating ||
            !UsesUnitPosition()
        )
        {
            return;
        }

        if (cachedUserTilePin == null)
        {
            CacheUserComponents(cachedUser);

            if (cachedUserTilePin == null)
                return;
        }

        if (!cachedUserTilePin.HasTile())
            return;

        Vector2Int currentTile =
            cachedUserTilePin.GetTile();

        if (!hasCachedUserTile)
        {
            cachedUserTile = currentTile;
            hasCachedUserTile = true;

            return;
        }

        if (currentTile == cachedUserTile)
            return;

        cachedUserTile = currentTile;
        cachedCenterPos = currentTile;

        RefreshActiveHighlights();
    }

    private bool UsesUnitPosition()
    {
        return currentHighlightState ==
                   HighlightState.MovementRange ||
               currentHighlightState ==
                   HighlightState.ScriptableObjectAbility;
    }

    private void CacheUserComponents(GameObject user)
    {
        if (user == null)
        {
            cachedUserTilePin = null;
            cachedMoveBrain = null;
            return;
        }

        cachedUserTilePin =
            user.GetComponent<UnitTilePin>();

        cachedMoveBrain =
            user.GetComponent<UnitMoveBrain>();
    }

    private void CacheUserTile(
        GameObject user,
        Vector2Int fallbackTile)
    {
        hasCachedUserTile = false;

        if (user == null)
            return;

        CacheUserComponents(user);

        if (
            cachedUserTilePin != null &&
            cachedUserTilePin.HasTile()
        )
        {
            cachedUserTile =
                cachedUserTilePin.GetTile();
        }
        else
        {
            cachedUserTile =
                fallbackTile;
        }

        hasCachedUserTile = true;
    }

    private bool TryGetUserLogicalTile(
        GameObject user,
        out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        if (user == null)
            return false;

        if (
            user != cachedUser ||
            cachedUserTilePin == null
        )
        {
            CacheUserComponents(user);
        }

        if (
            cachedUserTilePin == null ||
            !cachedUserTilePin.HasTile()
        )
        {
            return false;
        }

        tile =
            cachedUserTilePin.GetTile();

        return true;
    }

    #endregion

    #region Highlight Refresh Execution

    public void RefreshActiveHighlights()
    {
        if (isBoardRotating)
            return;

        switch (currentHighlightState)
        {
            case HighlightState.MovementRange:
                RefreshMovementRangeFromCache();
                break;

            case HighlightState.BasicAbilityRange:
                RefreshBasicAbilityRangeFromCache();
                break;

            case HighlightState.ScriptableObjectAbility:
                RefreshScriptableAbilityFromCache();
                break;

            case HighlightState.CustomTiles:
                RefreshCustomTilesFromCache();
                break;

            case HighlightState.OffsetCells:
                RefreshOffsetCellsFromCache();
                break;

            case HighlightState.SingleCell:
                RefreshSingleCellFromCache();
                break;
        }
    }

    private void RefreshMovementRangeFromCache()
    {
        if (!HasReferences())
            return;

        Vector2Int center =
            ResolveCachedUserCenter();

        if (!IsInsideGrid(center))
        {
            highlightManager.ClearMovementRange();
            return;
        }

        CalculateMovementRange(
            center,
            cachedRange,
            reusableTileList
        );

        highlightManager.ShowMovementTiles(
            reusableTileList,
            cachedUser
        );
    }

    private void RefreshBasicAbilityRangeFromCache()
    {
        if (!HasReferences())
            return;

        if (!IsInsideGrid(cachedCenterPos))
        {
            highlightManager.ClearAbilityRange();
            return;
        }

        CalculateAbilityRange(
            cachedCenterPos,
            cachedRange,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }

    private void RefreshScriptableAbilityFromCache()
    {
        if (
            cachedAbility == null ||
            cachedUser == null
        )
        {
            return;
        }

        ShowAbilityRange(
            cachedAbility,
            cachedUser
        );
    }

    private void RefreshCustomTilesFromCache()
    {
        if (
            cachedCustomPositions == null ||
            highlightManager == null
        )
        {
            return;
        }

        FilterValidTiles(
            cachedCustomPositions,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }

    private void RefreshOffsetCellsFromCache()
    {
        if (
            cachedOffsets == null ||
            !HasReferences()
        )
        {
            return;
        }

        BuildOffsetCells(
            cachedCenterPos,
            cachedOffsets,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }

    private void RefreshSingleCellFromCache()
    {
        if (
            !HasReferences() ||
            !IsInsideGrid(cachedCenterPos)
        )
        {
            return;
        }

        highlightManager.ShowAbilityCell(
            cachedCenterPos
        );
    }

    private Vector2Int ResolveCachedUserCenter()
    {
        if (
            TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int userTile
            )
        )
        {
            cachedCenterPos = userTile;
            cachedUserTile = userTile;
            hasCachedUserTile = true;
        }

        return cachedCenterPos;
    }

    #endregion

    #region Movement Range Logic

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        if (
            !HasReferences() ||
            isBoardRotating
        )
        {
            return;
        }

        Vector2Int logicalCenter =
            TryGetUserLogicalTile(
                user,
                out Vector2Int userTile
            )
                ? userTile
                : ConvertToLogicalGridPosition(
                    centerPosition
                );

        currentHighlightState =
            HighlightState.MovementRange;

        cachedCenterPos =
            logicalCenter;

        cachedRange =
            Mathf.Max(0, range);

        cachedUser =
            user;

        CacheUserTile(
            user,
            logicalCenter
        );

        if (
            cachedMoveBrain != null &&
            !cachedMoveBrain.CanMoveThisTurn()
        )
        {
            highlightManager.ClearMovementRange();
            return;
        }

        if (!IsInsideGrid(logicalCenter))
        {
            highlightManager.ClearMovementRange();
            return;
        }

        CalculateMovementRange(
            logicalCenter,
            cachedRange,
            reusableTileList
        );

        highlightManager.ShowMovementTiles(
            reusableTileList,
            user
        );
    }

    private void CalculateMovementRange(
        Vector2Int center,
        int range,
        List<Vector2Int> results)
    {
        results.Clear();

        for (
            int x = minGridX;
            x <= maxGridX;
            x++
        )
        {
            for (
                int y = minGridY;
                y <= maxGridY;
                y++
            )
            {
                Vector2Int pos =
                    new Vector2Int(x, y);

                if (pos == center)
                    continue;

                if (
                    gridManager.GetDistance(
                        center,
                        pos
                    ) > range
                )
                {
                    continue;
                }

                if (
                    gridManager.IsCellOccupied(pos)
                )
                {
                    continue;
                }

                results.Add(pos);
            }
        }
    }

    #endregion

    #region Ability Range Logic

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range)
    {
        if (
            !HasReferences() ||
            isBoardRotating
        )
        {
            return;
        }

        Vector2Int logicalCenter =
            ConvertToLogicalGridPosition(
                centerPosition
            );

        currentHighlightState =
            HighlightState.BasicAbilityRange;

        cachedCenterPos =
            logicalCenter;

        cachedRange =
            Mathf.Max(0, range);

        if (!IsInsideGrid(logicalCenter))
        {
            highlightManager.ClearAbilityRange();
            return;
        }

        CalculateAbilityRange(
            logicalCenter,
            cachedRange,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList
        );
    }

    private void CalculateAbilityRange(
        Vector2Int center,
        int range,
        List<Vector2Int> results)
    {
        results.Clear();

        for (
            int x = minGridX;
            x <= maxGridX;
            x++
        )
        {
            for (
                int y = minGridY;
                y <= maxGridY;
                y++
            )
            {
                Vector2Int pos =
                    new Vector2Int(x, y);

                if (pos == center)
                    continue;

                int distance =
                    Mathf.Max(
                        Mathf.Abs(
                            pos.x - center.x
                        ),
                        Mathf.Abs(
                            pos.y - center.y
                        )
                    );

                if (distance <= range)
                    results.Add(pos);
            }
        }
    }

    #endregion

    #region Scriptable Object Ability

    public void ShowAbilityRange(
        AbilitySO ability,
        GameObject user)
    {
        if (
            !HasReferences() ||
            isBoardRotating
        )
        {
            return;
        }

        currentHighlightState =
            HighlightState.ScriptableObjectAbility;

        cachedAbility =
            ability;

        cachedUser =
            user;

        hasCachedUserTile = false;

        CacheUserComponents(user);

        if (
            ability == null ||
            user == null
        )
        {
            highlightManager.ClearAbilityRange();
            return;
        }

        Vector2Int userGridPosition =
            TryGetUserLogicalTile(
                user,
                out Vector2Int userTile
            )
                ? userTile
                : ConvertToLogicalGridPosition(
                    gridManager.WorldToGridPosition(
                        user.transform.position
                    )
                );

        cachedCenterPos =
            userGridPosition;

        CacheUserTile(
            user,
            userGridPosition
        );

        if (!ability.CanUseAfterMovement(user))
        {
            highlightManager.ClearAbilityRange();
            return;
        }

        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                user
            );

        if (rangeTiles == null)
        {
            highlightManager.ClearAbilityRange();
            return;
        }

        FilterValidAbilityTiles(
            ability,
            user,
            rangeTiles,
            reusableTileList
        );

        bool isHeal =
            ability is HealAbilitySO;

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            user,
            isHeal
        );

        highlightManager.SetCurrentAbility(
            ability
        );
    }

    private void FilterValidAbilityTiles(
        AbilitySO ability,
        GameObject user,
        List<Vector2Int> positions,
        List<Vector2Int> results)
    {
        results.Clear();

        if (
            ability == null ||
            user == null ||
            positions == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < positions.Count;
            i++
        )
        {
            Vector2Int pos =
                positions[i];

            if (!IsInsideGrid(pos))
                continue;

            if (
                ability.CanHitTile(
                    gridManager,
                    user,
                    pos
                ) &&
                !results.Contains(pos)
            )
            {
                results.Add(pos);
            }
        }
    }

    #endregion

    #region Custom Tiles & Offsets

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        if (
            !HasReferences() ||
            isBoardRotating
        )
        {
            return;
        }

        currentHighlightState =
            HighlightState.CustomTiles;

        cachedCustomPositions =
            positions != null
                ? new List<Vector2Int>(positions)
                : null;

        cachedUser =
            user;

        hasCachedUserTile = false;

        FilterValidTiles(
            positions,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            user
        );
    }

    private void FilterValidTiles(
        List<Vector2Int> positions,
        List<Vector2Int> results)
    {
        results.Clear();

        if (positions == null)
            return;

        for (
            int i = 0;
            i < positions.Count;
            i++
        )
        {
            Vector2Int logicalPos =
                ConvertToLogicalGridPosition(
                    positions[i]
                );

            if (
                IsInsideGrid(logicalPos) &&
                !results.Contains(logicalPos)
            )
            {
                results.Add(logicalPos);
            }
        }
    }

    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets,
        GameObject user = null)
    {
        if (
            !HasReferences() ||
            offsets == null ||
            isBoardRotating
        )
        {
            return;
        }

        Vector2Int logicalOrigin =
            ConvertToLogicalGridPosition(
                origin
            );

        currentHighlightState =
            HighlightState.OffsetCells;

        cachedCenterPos =
            logicalOrigin;

        cachedOffsets =
            new List<Vector2Int>(offsets);

        cachedUser =
            user;

        hasCachedUserTile = false;

        BuildOffsetCells(
            logicalOrigin,
            cachedOffsets,
            reusableTileList
        );

        highlightManager.ShowAbilityTiles(
            reusableTileList,
            user
        );
    }

    private void BuildOffsetCells(
        Vector2Int origin,
        List<Vector2Int> offsets,
        List<Vector2Int> results)
    {
        results.Clear();

        for (
            int i = 0;
            i < offsets.Count;
            i++
        )
        {
            Vector2Int targetPos =
                origin + offsets[i];

            if (
                IsInsideGrid(targetPos) &&
                !results.Contains(targetPos)
            )
            {
                results.Add(targetPos);
            }
        }
    }

    #endregion

    #region Single Cell & Clear

    public void ShowAbilityCell(
        Vector2Int position)
    {
        if (
            !HasReferences() ||
            isBoardRotating
        )
        {
            return;
        }

        Vector2Int logicalPosition =
            ConvertToLogicalGridPosition(
                position
            );

        currentHighlightState =
            HighlightState.SingleCell;

        cachedCenterPos =
            logicalPosition;

        cachedUser = null;
        hasCachedUserTile = false;

        if (IsInsideGrid(logicalPosition))
        {
            highlightManager.ShowAbilityCell(
                logicalPosition
            );
        }
    }

    public void ClearMovementHighlightState()
    {
        if (
            currentHighlightState !=
            HighlightState.MovementRange
        )
        {
            return;
        }

        currentHighlightState =
            HighlightState.None;

        cachedCenterPos =
            Vector2Int.zero;

        cachedUserTile =
            Vector2Int.zero;

        cachedRange = 0;

        cachedUser = null;
        cachedUserTilePin = null;
        cachedMoveBrain = null;
        hasCachedUserTile = false;
    }

    public void ClearAllHighlights()
    {
        StopRotationRefresh();

        isBoardRotating = false;

        currentHighlightState =
            HighlightState.None;

        cachedCenterPos =
            Vector2Int.zero;

        cachedUserTile =
            Vector2Int.zero;

        cachedRange = 0;

        cachedUser = null;
        cachedAbility = null;
        cachedCustomPositions = null;
        cachedOffsets = null;

        cachedUserTilePin = null;
        cachedMoveBrain = null;
        hasCachedUserTile = false;

        if (highlightManager != null)
        {
            highlightManager.ClearMovementRange();
            highlightManager.ClearAbilityRange();
        }
    }

    #endregion

    #region Coordinate & Math Conversions

    private Vector2Int ConvertToLogicalGridPosition(
        Vector2Int receivedPosition)
    {
        if (!compensateForBoardRotation)
            return receivedPosition;

        int rotation =
            Mathf.RoundToInt(
                NormalizeAngle(
                    GetBoardRotation().z
                ) / 90f
            ) & 3;

        return rotation switch
        {
            1 => new Vector2Int(
                receivedPosition.y,
                -receivedPosition.x
            ),

            2 => new Vector2Int(
                -receivedPosition.x,
                -receivedPosition.y
            ),

            3 => new Vector2Int(
                -receivedPosition.y,
                receivedPosition.x
            ),

            _ => receivedPosition
        };
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        return angle < 0f
            ? angle + 360f
            : angle;
    }

    #endregion

    #region Team Utilities

    public bool IsEnemyUnit(
        GameObject unit,
        GameObject sourceUser)
    {
        if (
            unit == null ||
            sourceUser == null
        )
        {
            return false;
        }

        AttackUnit targetAttack =
            unit.GetComponent<AttackUnit>();

        AttackUnit sourceAttack =
            sourceUser.GetComponent<AttackUnit>();

        if (
            targetAttack != null &&
            sourceAttack != null &&
            !targetAttack.IsDead()
        )
        {
            return targetAttack.GetTeam() !=
                   sourceAttack.GetTeam();
        }

        HealthManager targetHealth =
            unit.GetComponent<HealthManager>();

        HealthManager sourceHealth =
            sourceUser.GetComponent<HealthManager>();

        if (
            targetHealth != null &&
            sourceHealth != null &&
            !targetHealth.IsDead()
        )
        {
            return targetHealth.GetTeam() !=
                   sourceHealth.GetTeam();
        }

        return false;
    }

    #endregion

    #region Helpers & Getters

    private bool IsInsideGrid(
        Vector2Int position)
    {
        return gridManager != null &&
               gridManager.IsInsideGrid(position);
    }

    private bool HasReferences()
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            InitializeReferences();
        }

        if (boardRotationTransform == null)
            FindBoardRotationTransform();

        return
            gridManager != null &&
            highlightManager != null;
    }

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }

    public Transform GetBoardRotationTransform()
    {
        return boardRotationTransform;
    }

    public Vector2Int GetCachedCenterPosition()
    {
        return cachedCenterPos;
    }

    public bool IsBoardRotating()
    {
        return isBoardRotating;
    }

    #endregion
}