using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightBrain : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GridHighlightManager highlightManager;


    // ============================================================
    // BOARD ROTATION
    // ============================================================

    [Header("Board Rotation")]

    [SerializeField]
    private Transform boardRotationTransform;

    [SerializeField]
    private bool compensateForBoardRotation = true;

    [SerializeField, Min(0.01f)]
    private float rotationSettleDelay = 0.08f;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;


    // ============================================================
    // ROTATION TRACKING
    // ============================================================

    private Vector3 lastKnownBoardRotation;

    private bool isBoardRotating;

    private Coroutine rotationRefreshCoroutine;


    // ============================================================
    // CACHED HIGHLIGHT STATE
    // ============================================================

    private HighlightState currentHighlightState =
        HighlightState.None;

    private Vector2Int cachedCenterPos;

    private int cachedRange;

    private GameObject cachedUser;

    private AbilitySO cachedAbility;

    private List<Vector2Int> cachedCustomPositions;

    private List<Vector2Int> cachedOffsets;


    // ============================================================
    // CACHED USER TILE
    // ============================================================

    private Vector2Int cachedUserTile;

    private bool hasCachedUserTile;


    // ============================================================
    // HIGHLIGHT STATE
    // ============================================================

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


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();

        FindBoardRotationTransform();

        CacheBoardRotation();
    }


    private void Update()
    {
        CheckBoardRotationChange();

        CheckHighlightedUnitTileChanged();
    }


    private void OnDisable()
    {
        if (rotationRefreshCoroutine != null)
        {
            StopCoroutine(
                rotationRefreshCoroutine
            );

            rotationRefreshCoroutine = null;
        }

        isBoardRotating = false;
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                GetComponent<GridManager>();

            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<GridManager>();
            }
        }


        if (highlightManager == null)
        {
            highlightManager =
                GetComponent<GridHighlightManager>();

            if (highlightManager == null)
            {
                highlightManager =
                    FindFirstObjectByType<GridHighlightManager>();
            }
        }


        if (gridManager == null)
        {
            Debug.LogError(
                "[GridHighlightBrain] GridManager reference missing.",
                this
            );
        }


        if (highlightManager == null)
        {
            Debug.LogError(
                "[GridHighlightBrain] GridHighlightManager reference missing.",
                this
            );
        }
    }


    // ============================================================
    // BOARD ROTATION TRANSFORM
    // ============================================================

    private void FindBoardRotationTransform()
    {
        if (boardRotationTransform != null)
        {
            return;
        }


        BoardViewController boardController =
            FindFirstObjectByType<BoardViewController>();


        if (boardController != null)
        {
            boardRotationTransform =
                boardController.transform;

            return;
        }


        if (gridManager != null)
        {
            boardRotationTransform =
                gridManager.transform;
        }
        else
        {
            boardRotationTransform =
                transform;
        }
    }


    // ============================================================
    // ROTATION
    // ============================================================

    private void CacheBoardRotation()
    {
        lastKnownBoardRotation =
            GetBoardRotation();
    }


    private Vector3 GetBoardRotation()
    {
        if (boardRotationTransform != null)
        {
            return boardRotationTransform.eulerAngles;
        }


        if (gridManager != null)
        {
            return gridManager.transform.eulerAngles;
        }


        return transform.eulerAngles;
    }


    private void CheckBoardRotationChange()
    {
        Vector3 currentRotation =
            GetBoardRotation();


        if (
            Vector3.Distance(
                currentRotation,
                lastKnownBoardRotation
            ) <= 0.01f
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


        if (rotationRefreshCoroutine != null)
        {
            StopCoroutine(
                rotationRefreshCoroutine
            );
        }


        rotationRefreshCoroutine =
            StartCoroutine(
                RefreshAfterRotationStops()
            );
    }


    // ============================================================
    // HIDE DURING ROTATION
    // ============================================================

    private void HideHighlightsDuringRotation()
    {
        if (highlightManager == null)
        {
            return;
        }


        highlightManager.ClearMovementRange();

        highlightManager.ClearAbilityRange();
    }


    // ============================================================
    // WAIT FOR ROTATION TO STOP
    // ============================================================

    private IEnumerator RefreshAfterRotationStops()
    {
        yield return new WaitForSeconds(
            rotationSettleDelay
        );


        isBoardRotating = false;

        rotationRefreshCoroutine = null;


        RefreshActiveHighlights();
    }


    // ============================================================
    // CHECK UNIT TILE CHANGED
    // ============================================================

    private void CheckHighlightedUnitTileChanged()
    {
        if (cachedUser == null)
        {
            return;
        }


        if (isBoardRotating)
        {
            return;
        }


        if (
            currentHighlightState !=
                HighlightState.MovementRange &&
            currentHighlightState !=
                HighlightState.ScriptableObjectAbility
        )
        {
            return;
        }


        UnitTilePin tilePin =
            cachedUser.GetComponent<UnitTilePin>();


        if (tilePin == null)
        {
            return;
        }


        if (!tilePin.HasTile())
        {
            return;
        }


        Vector2Int currentTile =
            tilePin.GetTile();


        if (!hasCachedUserTile)
        {
            cachedUserTile =
                currentTile;

            hasCachedUserTile =
                true;

            return;
        }


        if (currentTile != cachedUserTile)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    "[GridHighlightBrain] Highlighted unit moved.\n" +
                    "OLD TILE: " +
                    cachedUserTile +
                    "\n" +
                    "NEW TILE: " +
                    currentTile +
                    "\n" +
                    "Refreshing highlight.",
                    cachedUser
                );
            }


            cachedUserTile =
                currentTile;


            RefreshActiveHighlights();
        }
    }


    // ============================================================
    // REFRESH ACTIVE HIGHLIGHTS
    // ============================================================

    public void RefreshActiveHighlights()
    {
        if (isBoardRotating)
        {
            return;
        }


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


            case HighlightState.None:

            default:

                break;
        }
    }


    // ============================================================
    // GET USER LOGICAL TILE
    // ============================================================

    private bool TryGetUserLogicalTile(
        GameObject user,
        out Vector2Int tile
    )
    {
        tile = Vector2Int.zero;


        if (user == null)
        {
            return false;
        }


        UnitTilePin tilePin =
            user.GetComponent<UnitTilePin>();


        if (
            tilePin != null &&
            tilePin.HasTile()
        )
        {
            /*
             * IMPORTANT:
             *
             * UnitTilePin already stores the logical tile.
             *
             * NEVER apply board rotation to this value.
             */

            tile =
                tilePin.GetTile();

            return true;
        }


        return false;
    }


    // ============================================================
    // REFRESH MOVEMENT FROM CACHE
    // ============================================================

    private void RefreshMovementRangeFromCache()
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        Vector2Int center =
            cachedCenterPos;


        // ========================================================
        // IMPORTANT FIX
        // ========================================================
        //
        // If this movement preview belongs to a unit, its
        // UnitTilePin is the authoritative logical centre.
        //
        // DO NOT call ConvertToLogicalGridPosition() here.
        //
        // UnitTilePin.GetTile() is already logical.
        // ========================================================

        if (
            TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int userTile
            )
        )
        {
            center =
                userTile;

            cachedCenterPos =
                center;
        }


        if (
            !gridManager.IsInsideGrid(
                center
            )
        )
        {
            highlightManager.ClearMovementRange();

            return;
        }


        List<Vector2Int> movementCells =
            CalculateMovementRange(
                center,
                cachedRange
            );


        highlightManager.ShowMovementTiles(
            movementCells,
            cachedUser
        );
    }


    // ============================================================
    // REFRESH BASIC ABILITY FROM CACHE
    // ============================================================

    private void RefreshBasicAbilityRangeFromCache()
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        Vector2Int center =
            cachedCenterPos;


        if (
            TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int userTile
            )
        )
        {
            center =
                userTile;

            cachedCenterPos =
                center;
        }


        if (
            !gridManager.IsInsideGrid(
                center
            )
        )
        {
            highlightManager.ClearAbilityRange();

            return;
        }


        List<Vector2Int> cells =
            CalculateAbilityRange(
                center,
                cachedRange
            );


        highlightManager.ShowAbilityTiles(
            cells,
            cachedUser
        );
    }


    // ============================================================
    // REFRESH SCRIPTABLE ABILITY FROM CACHE
    // ============================================================

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


    // ============================================================
    // REFRESH CUSTOM TILES FROM CACHE
    // ============================================================

    private void RefreshCustomTilesFromCache()
    {
        if (cachedCustomPositions == null)
        {
            return;
        }


        List<Vector2Int> validTiles =
            FilterValidTiles(
                cachedCustomPositions
            );


        highlightManager.ShowAbilityTiles(
            validTiles,
            cachedUser
        );
    }


    // ============================================================
    // REFRESH OFFSET CELLS FROM CACHE
    // ============================================================

    private void RefreshOffsetCellsFromCache()
    {
        if (cachedOffsets == null)
        {
            return;
        }


        Vector2Int origin =
            cachedCenterPos;


        List<Vector2Int> cells =
            new List<Vector2Int>();


        foreach (
            Vector2Int offset
            in cachedOffsets
        )
        {
            Vector2Int pos =
                origin + offset;


            if (
                gridManager.IsInsideGrid(pos) &&
                !cells.Contains(pos)
            )
            {
                cells.Add(pos);
            }
        }


        highlightManager.ShowAbilityTiles(
            cells,
            cachedUser
        );
    }


    // ============================================================
    // REFRESH SINGLE CELL
    // ============================================================

    private void RefreshSingleCellFromCache()
    {
        if (
            !gridManager.IsInsideGrid(
                cachedCenterPos
            )
        )
        {
            return;
        }


        highlightManager.ShowAbilityCell(
            cachedCenterPos
        );
    }


    // ============================================================
    // CLEAR
    // ============================================================

    public void ClearAllHighlights()
    {
        if (rotationRefreshCoroutine != null)
        {
            StopCoroutine(
                rotationRefreshCoroutine
            );

            rotationRefreshCoroutine = null;
        }


        isBoardRotating = false;


        currentHighlightState =
            HighlightState.None;


        cachedUser = null;

        cachedAbility = null;

        cachedCustomPositions = null;

        cachedOffsets = null;


        hasCachedUserTile =
            false;


        if (highlightManager != null)
        {
            highlightManager.ClearMovementRange();

            highlightManager.ClearAbilityRange();
        }
    }


    // ============================================================
    // MOVEMENT RANGE
    // ============================================================

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null
    )
    {
        if (!HasReferences())
        {
            return;
        }


        if (isBoardRotating)
        {
            return;
        }


        // ========================================================
        // IMPORTANT FIX
        // ========================================================
        //
        // When we have a unit, ALWAYS get its logical tile from
        // UnitTilePin.
        //
        // The unit's tile is already independent of board rotation.
        // ========================================================

        Vector2Int logicalCenter;


        if (
            TryGetUserLogicalTile(
                user,
                out Vector2Int userTile
            )
        )
        {
            logicalCenter =
                userTile;
        }
        else
        {
            /*
             * No UnitTilePin available.
             *
             * In this case centerPosition is treated as an
             * external/visual coordinate and converted once.
             */

            logicalCenter =
                ConvertToLogicalGridPosition(
                    centerPosition
                );
        }


        currentHighlightState =
            HighlightState.MovementRange;


        cachedCenterPos =
            logicalCenter;


        cachedRange =
            Mathf.Max(
                0,
                range
            );


        cachedUser =
            user;


        CacheUserTile(
            user,
            logicalCenter
        );


        if (user != null)
        {
            UnitMoveBrain moveBrain =
                user.GetComponent<UnitMoveBrain>();


            if (
                moveBrain != null &&
                !moveBrain.CanMoveThisTurn()
            )
            {
                highlightManager.ClearMovementRange();

                return;
            }
        }


        if (
            !gridManager.IsInsideGrid(
                logicalCenter
            )
        )
        {
            highlightManager.ClearMovementRange();

            return;
        }


        List<Vector2Int> movementCells =
            CalculateMovementRange(
                logicalCenter,
                cachedRange
            );


        highlightManager.ShowMovementTiles(
            movementCells,
            user
        );


        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightBrain] MOVEMENT RANGE\n" +
                "USER: " +
                (user != null
                    ? user.name
                    : "None") +
                "\n" +
                "LOGICAL CENTRE: " +
                logicalCenter +
                "\n" +
                "RANGE: " +
                cachedRange,
                user
            );
        }
    }


    // ============================================================
    // MOVEMENT CALCULATION
    // ============================================================

    private List<Vector2Int> CalculateMovementRange(
        Vector2Int centerPosition,
        int range
    )
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();


        int minX =
            gridManager.GetMinX();

        int maxX =
            gridManager.GetMaxX();

        int minY =
            gridManager.GetMinY();

        int maxY =
            gridManager.GetMaxY();


        for (
            int x = minX;
            x <= maxX;
            x++
        )
        {
            for (
                int y = minY;
                y <= maxY;
                y++
            )
            {
                Vector2Int pos =
                    new Vector2Int(
                        x,
                        y
                    );


                if (pos == centerPosition)
                {
                    continue;
                }


                if (
                    gridManager.GetDistance(
                        centerPosition,
                        pos
                    ) > range
                )
                {
                    continue;
                }


                if (
                    gridManager.IsCellOccupied(
                        pos
                    )
                )
                {
                    continue;
                }


                cells.Add(pos);
            }
        }


        return cells;
    }


    // ============================================================
    // BASIC ABILITY RANGE
    // ============================================================

    public void ShowAbilityRange(
        Vector2Int centerPosition,
        int range
    )
    {
        if (!HasReferences())
        {
            return;
        }


        if (isBoardRotating)
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
            Mathf.Max(
                0,
                range
            );


        if (
            !gridManager.IsInsideGrid(
                logicalCenter
            )
        )
        {
            highlightManager.ClearAbilityRange();

            return;
        }


        List<Vector2Int> cells =
            CalculateAbilityRange(
                logicalCenter,
                cachedRange
            );


        highlightManager.ShowAbilityTiles(
            cells
        );
    }


    // ============================================================
    // BASIC ABILITY CALCULATION
    // ============================================================

    private List<Vector2Int> CalculateAbilityRange(
        Vector2Int centerPosition,
        int range
    )
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();


        int minX =
            gridManager.GetMinX();

        int maxX =
            gridManager.GetMaxX();

        int minY =
            gridManager.GetMinY();

        int maxY =
            gridManager.GetMaxY();


        for (
            int x = minX;
            x <= maxX;
            x++
        )
        {
            for (
                int y = minY;
                y <= maxY;
                y++
            )
            {
                Vector2Int pos =
                    new Vector2Int(
                        x,
                        y
                    );


                if (pos == centerPosition)
                {
                    continue;
                }


                int distance =
                    Mathf.Max(
                        Mathf.Abs(
                            pos.x -
                            centerPosition.x
                        ),
                        Mathf.Abs(
                            pos.y -
                            centerPosition.y
                        )
                    );


                if (
                    distance <= range
                )
                {
                    cells.Add(pos);
                }
            }
        }


        return cells;
    }


    // ============================================================
    // SCRIPTABLE OBJECT ABILITY
    // ============================================================

    public void ShowAbilityRange(
        AbilitySO ability,
        GameObject user
    )
    {
        if (!HasReferences())
        {
            return;
        }


        if (isBoardRotating)
        {
            return;
        }


        currentHighlightState =
            HighlightState.ScriptableObjectAbility;


        cachedAbility =
            ability;


        cachedUser =
            user;


        hasCachedUserTile =
            false;


        if (
            ability == null ||
            user == null
        )
        {
            highlightManager.ClearAbilityRange();

            return;
        }


        // ========================================================
        // GET UNIT LOGICAL TILE
        // ========================================================

        Vector2Int userGridPos;


        if (
            TryGetUserLogicalTile(
                user,
                out Vector2Int userTile
            )
        )
        {
            /*
             * UnitTilePin is already logical.
             *
             * DO NOT rotate this coordinate.
             */

            userGridPos =
                userTile;
        }
        else
        {
            userGridPos =
                gridManager.WorldToGridPosition(
                    user.transform.position
                );


            userGridPos =
                ConvertToLogicalGridPosition(
                    userGridPos
                );
        }


        cachedCenterPos =
            userGridPos;


        CacheUserTile(
            user,
            userGridPos
        );


        if (enableDebugLogs)
        {
            Debug.Log(
                "[GridHighlightBrain]\n" +
                "UNIT TILE: " +
                userGridPos +
                "\n" +
                "ATTACK HIGHLIGHT CENTRE: " +
                userGridPos,
                user
            );
        }


        // ========================================================
        // ABILITY VALIDATION
        // ========================================================

        if (
            !ability.CanUseAfterMovement(
                user
            )
        )
        {
            highlightManager.ClearAbilityRange();

            return;
        }


        // ========================================================
        // GET RANGE
        // ========================================================

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


        // ========================================================
        // FILTER
        // ========================================================

        List<Vector2Int> validTiles =
            FilterValidAbilityTiles(
                ability,
                user,
                rangeTiles
            );


        bool isHeal =
            ability is HealAbilitySO;


        // ========================================================
        // SHOW
        // ========================================================

        highlightManager.ShowAbilityTiles(
            validTiles,
            user,
            isHeal
        );


        highlightManager.SetCurrentAbility(
            ability
        );
    }


    // ============================================================
    // FILTER ABILITY TILES
    // ============================================================

    private List<Vector2Int> FilterValidAbilityTiles(
        AbilitySO ability,
        GameObject user,
        List<Vector2Int> positions
    )
    {
        List<Vector2Int> validTiles =
            new List<Vector2Int>();


        if (
            ability == null ||
            user == null ||
            positions == null
        )
        {
            return validTiles;
        }


        foreach (
            Vector2Int pos
            in positions
        )
        {
            if (
                !gridManager.IsInsideGrid(
                    pos
                )
            )
            {
                continue;
            }


            if (
                validTiles.Contains(
                    pos
                )
            )
            {
                continue;
            }


            if (
                ability.CanHitTile(
                    gridManager,
                    user,
                    pos
                )
            )
            {
                validTiles.Add(pos);
            }
        }


        return validTiles;
    }


    // ============================================================
    // CUSTOM TILES
    // ============================================================

    public void ShowAbilityTiles(
        List<Vector2Int> positions,
        GameObject user = null
    )
    {
        if (!HasReferences())
        {
            return;
        }


        if (isBoardRotating)
        {
            return;
        }


        currentHighlightState =
            HighlightState.CustomTiles;


        cachedCustomPositions =
            positions != null
                ? new List<Vector2Int>(
                    positions
                )
                : null;


        cachedUser =
            user;


        hasCachedUserTile =
            false;


        List<Vector2Int> validTiles =
            FilterValidTiles(
                positions
            );


        highlightManager.ShowAbilityTiles(
            validTiles,
            user
        );
    }


    // ============================================================
    // OFFSET CELLS
    // ============================================================

    public void ShowAbilityCells(
        Vector2Int origin,
        List<Vector2Int> offsets,
        GameObject user = null
    )
    {
        if (
            !HasReferences() ||
            offsets == null
        )
        {
            return;
        }


        if (isBoardRotating)
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
            new List<Vector2Int>(
                offsets
            );


        cachedUser =
            user;


        hasCachedUserTile =
            false;


        List<Vector2Int> cells =
            new List<Vector2Int>();


        foreach (
            Vector2Int offset
            in offsets
        )
        {
            Vector2Int pos =
                logicalOrigin +
                offset;


            if (
                gridManager.IsInsideGrid(
                    pos
                ) &&
                !cells.Contains(
                    pos
                )
            )
            {
                cells.Add(pos);
            }
        }


        highlightManager.ShowAbilityTiles(
            cells,
            user
        );
    }


    // ============================================================
    // SINGLE CELL
    // ============================================================

    public void ShowAbilityCell(
        Vector2Int position
    )
    {
        if (!HasReferences())
        {
            return;
        }


        if (isBoardRotating)
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

        hasCachedUserTile =
            false;


        if (
            !gridManager.IsInsideGrid(
                logicalPosition
            )
        )
        {
            return;
        }


        highlightManager.ShowAbilityCell(
            logicalPosition
        );
    }


    // ============================================================
    // FILTER CUSTOM TILES
    // ============================================================

    private List<Vector2Int> FilterValidTiles(
        List<Vector2Int> positions
    )
    {
        List<Vector2Int> valid =
            new List<Vector2Int>();


        if (positions == null)
        {
            return valid;
        }


        foreach (
            Vector2Int receivedPos
            in positions
        )
        {
            Vector2Int logicalPos =
                ConvertToLogicalGridPosition(
                    receivedPos
                );


            if (
                gridManager.IsInsideGrid(
                    logicalPos
                ) &&
                !valid.Contains(
                    logicalPos
                )
            )
            {
                valid.Add(logicalPos);
            }
        }


        return valid;
    }


    // ============================================================
    // CACHE USER TILE
    // ============================================================

    private void CacheUserTile(
        GameObject user,
        Vector2Int fallbackTile
    )
    {
        hasCachedUserTile =
            false;


        if (user == null)
        {
            return;
        }


        UnitTilePin tilePin =
            user.GetComponent<UnitTilePin>();


        if (
            tilePin != null &&
            tilePin.HasTile()
        )
        {
            cachedUserTile =
                tilePin.GetTile();

            hasCachedUserTile =
                true;

            return;
        }


        cachedUserTile =
            fallbackTile;

        hasCachedUserTile =
            true;
    }


    // ============================================================
    // COORDINATE CORRECTION
    // ============================================================

    private Vector2Int ConvertToLogicalGridPosition(
        Vector2Int receivedPosition
    )
    {
        if (!compensateForBoardRotation)
        {
            return receivedPosition;
        }


        float zRotation =
            NormalizeAngle(
                GetBoardRotation().z
            );


        int rotation =
            Mathf.RoundToInt(
                zRotation / 90f
            );


        rotation =
            (
                (rotation % 4) +
                4
            ) % 4;


        Vector2Int corrected =
            receivedPosition;


        switch (rotation)
        {
            case 0:

                corrected =
                    receivedPosition;

                break;


            case 1:

                corrected =
                    new Vector2Int(
                        receivedPosition.y,
                        -receivedPosition.x
                    );

                break;


            case 2:

                corrected =
                    new Vector2Int(
                        -receivedPosition.x,
                        -receivedPosition.y
                    );

                break;


            case 3:

                corrected =
                    new Vector2Int(
                        -receivedPosition.y,
                        receivedPosition.x
                    );

                break;
        }


        return corrected;
    }


    // ============================================================
    // TEAM / ENEMY
    // ============================================================

    public bool IsEnemyUnit(
        GameObject unit,
        GameObject sourceUser
    )
    {
        if (
            unit == null ||
            sourceUser == null
        )
        {
            return false;
        }


        AttackUnit targetAtk =
            unit.GetComponent<AttackUnit>();


        AttackUnit sourceAtk =
            sourceUser.GetComponent<AttackUnit>();


        if (
            targetAtk != null &&
            sourceAtk != null &&
            !targetAtk.IsDead()
        )
        {
            return
                targetAtk.GetTeam() !=
                sourceAtk.GetTeam();
        }


        HealthManager targetHP =
            unit.GetComponent<HealthManager>();


        HealthManager sourceHP =
            sourceUser.GetComponent<HealthManager>();


        if (
            targetHP != null &&
            sourceHP != null &&
            !targetHP.IsDead()
        )
        {
            return
                targetHP.GetTeam() !=
                sourceHP.GetTeam();
        }


        return false;
    }


    // ============================================================
    // GRID HELPERS
    // ============================================================

    private Vector3 GetGridWorldPosition(
        Vector2Int position
    )
    {
        if (gridManager == null)
        {
            return Vector3.zero;
        }


        return gridManager.GridToWorldPosition(
            position
        );
    }


    private float NormalizeAngle(
        float angle
    )
    {
        angle %= 360f;


        if (angle < 0f)
        {
            angle += 360f;
        }


        return angle;
    }


    // ============================================================
    // REFERENCES VALIDATION
    // ============================================================

    private bool HasReferences()
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            FindReferences();
        }


        if (boardRotationTransform == null)
        {
            FindBoardRotationTransform();
        }


        return
            gridManager != null &&
            highlightManager != null;
    }


    // ============================================================
    // PUBLIC GETTERS
    // ============================================================

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


    // ============================================================
    // ROTATION STATUS
    // ============================================================

    public bool IsBoardRotating()
    {
        return isBoardRotating;
    }
}