using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridHighlightBrain : MonoBehaviour
{
    public enum HighlightState
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
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;


    // ============================================================
    // BOARD ROTATION
    // ============================================================

    [Header("Board Rotation")]
    [SerializeField] private bool refreshAfterBoardRotation = true;

    [SerializeField, Min(0f)]
    private float boardRotationThreshold = 0.1f;


    // ============================================================
    // CURRENT STATE
    // ============================================================

    [SerializeField]
    private HighlightState currentState =
        HighlightState.None;

    private HighlightState cachedState =
        HighlightState.None;


    // ============================================================
    // MOVEMENT RANGE VISIBILITY
    // ============================================================

    private bool movementRangeHidden;


    // ============================================================
    // CACHED MOVEMENT DATA
    // ============================================================

    private Vector2Int cachedCenterPos;

    private Vector2Int cachedUserTile;

    private int cachedRange;

    private GameObject cachedUser;


    // ============================================================
    // CACHED ABILITY DATA
    // ============================================================

    private AbilitySO cachedAbility;


    // ============================================================
    // CUSTOM DATA
    // ============================================================

    private readonly List<Vector2Int>
        cachedCustomPositions =
        new List<Vector2Int>(64);

    private readonly List<Vector2Int>
        cachedOffsetCells =
        new List<Vector2Int>(64);


    // ============================================================
    // USER / MOVEMENT CACHE
    // ============================================================

    private UnitTilePin cachedUserTilePin;

    private UnitMoveBrain cachedMoveBrain;


    // ============================================================
    // GRID BOUNDS
    // ============================================================

    private int minGridX;
    private int maxGridX;
    private int minGridY;
    private int maxGridY;


    // ============================================================
    // REUSABLE LIST
    // ============================================================

    private readonly List<Vector2Int>
        reusableTileList =
        new List<Vector2Int>(128);


    // ============================================================
    // BOARD ROTATION STATE
    // ============================================================

    private Quaternion lastBoardRotation;

    private bool isBoardRotating;

    private Coroutine boardRotationCoroutine;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();

        RefreshGridBounds();

        lastBoardRotation =
            transform.rotation;

        StartCoroutine(
            DelayedInitialRefresh()
        );
    }


    private IEnumerator DelayedInitialRefresh()
    {
        yield return null;

        FindReferences();

        RefreshGridBounds();
    }


    private void Update()
    {
        CheckBoardRotationChange();

        CheckHighlightedUnitTileChanged();
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }
    }


    private bool HasReferences()
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            FindReferences();
        }

        return
            gridManager != null &&
            highlightManager != null;
    }


    // ============================================================
    // RESET FOR NEW ENCOUNTER / LEVEL
    // ============================================================

    public void ResetForNewEncounter()
    {
        DebugLog(
            "Resetting GridHighlightBrain for new encounter."
        );


        // ========================================================
        // STOP BOARD ROTATION REFRESH
        // ========================================================

        if (boardRotationCoroutine != null)
        {
            StopCoroutine(
                boardRotationCoroutine
            );

            boardRotationCoroutine = null;
        }

        isBoardRotating = false;


        // ========================================================
        // CLEAR CURRENT VISUAL HIGHLIGHTS
        // ========================================================

        if (highlightManager != null)
        {
            highlightManager.ClearAllHighlights();
        }


        // ========================================================
        // RESET HIGHLIGHT STATE
        // ========================================================

        currentState =
            HighlightState.None;

        cachedState =
            HighlightState.None;


        // ========================================================
        // RESET MOVEMENT CACHE
        // ========================================================

        cachedCenterPos =
            Vector2Int.zero;

        cachedUserTile =
            Vector2Int.zero;

        cachedRange = 0;

        cachedUser = null;


        // ========================================================
        // RESET ABILITY CACHE
        // ========================================================

        cachedAbility = null;


        // ========================================================
        // RESET CUSTOM DATA
        // ========================================================

        cachedCustomPositions.Clear();

        cachedOffsetCells.Clear();


        // ========================================================
        // RESET USER COMPONENT CACHE
        // ========================================================

        cachedUserTilePin = null;

        cachedMoveBrain = null;


        // ========================================================
        // RESET MOVEMENT VISIBILITY
        // ========================================================

        movementRangeHidden = false;


        // ========================================================
        // RE-FIND CURRENT REFERENCES
        // ========================================================
        //
        // This is important if the new encounter created a new
        // GridManager or GridHighlightManager.
        // ========================================================

        gridManager = null;

        highlightManager = null;

        FindReferences();


        // ========================================================
        // REFRESH CURRENT GRID BOUNDS
        // ========================================================

        RefreshGridBounds();


        // ========================================================
        // RESET BOARD ROTATION CACHE
        // ========================================================

        lastBoardRotation =
            transform.rotation;


        DebugLog(
            "GridHighlightBrain reset complete."
        );
    }


    // ============================================================
    // GRID BOUNDS
    // ============================================================

    public void RefreshGridBounds()
    {
        if (gridManager == null)
        {
            FindReferences();
        }

        if (gridManager == null)
        {
            return;
        }


        minGridX =
            gridManager.GetMinX();

        maxGridX =
            gridManager.GetMaxX();

        minGridY =
            gridManager.GetMinY();

        maxGridY =
            gridManager.GetMaxY();


        DebugLog(
            "Grid bounds refreshed: " +
            minGridX + " to " + maxGridX +
            ", " +
            minGridY + " to " + maxGridY
        );
    }


    private bool IsInsideGrid(
        Vector2Int position)
    {
        return
            position.x >= minGridX &&
            position.x <= maxGridX &&
            position.y >= minGridY &&
            position.y <= maxGridY;
    }


    // ============================================================
    // BOARD ROTATION
    // ============================================================

    private void CheckBoardRotationChange()
    {
        Quaternion currentRotation =
            transform.rotation;


        float angle =
            Quaternion.Angle(
                lastBoardRotation,
                currentRotation
            );


        if (
            angle <
            boardRotationThreshold
        )
        {
            return;
        }


        lastBoardRotation =
            currentRotation;

        isBoardRotating = true;


        if (boardRotationCoroutine != null)
        {
            StopCoroutine(
                boardRotationCoroutine
            );
        }


        boardRotationCoroutine =
            StartCoroutine(
                HandleBoardRotationRefresh()
            );
    }


    private IEnumerator HandleBoardRotationRefresh()
    {
        if (refreshAfterBoardRotation)
        {
            yield return null;

            yield return new WaitForEndOfFrame();

            RefreshGridBounds();

            RefreshActiveHighlights();
        }


        isBoardRotating = false;

        boardRotationCoroutine = null;
    }


    // ============================================================
    // HIGHLIGHTED UNIT TILE TRACKING
    // ============================================================

    private void CheckHighlightedUnitTileChanged()
    {
        if (!UsesUnitPosition())
        {
            return;
        }


        if (cachedUser == null)
        {
            return;
        }


        if (
            !TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int currentTile
            )
        )
        {
            return;
        }


        if (currentTile == cachedUserTile)
        {
            return;
        }


        cachedUserTile =
            currentTile;

        cachedCenterPos =
            currentTile;


        RefreshActiveHighlights();
    }


    // ============================================================
    // ACTIVE STATE
    // ============================================================

    public HighlightState GetCurrentState()
    {
        return currentState;
    }


    public bool HasActiveHighlight()
    {
        return currentState !=
               HighlightState.None;
    }


    // ============================================================
    // CLEAR ALL HIGHLIGHTS
    // ============================================================

    public void ClearAllHighlights()
    {
        currentState =
            HighlightState.None;

        cachedState =
            HighlightState.None;

        cachedUser = null;

        cachedAbility = null;

        cachedRange = 0;

        cachedCenterPos =
            Vector2Int.zero;

        cachedUserTile =
            Vector2Int.zero;

        cachedCustomPositions.Clear();

        cachedOffsetCells.Clear();

        cachedUserTilePin = null;

        cachedMoveBrain = null;

        movementRangeHidden = false;


        if (highlightManager != null)
        {
            highlightManager.ClearAllHighlights();
        }


        DebugLog(
            "All highlight state cleared."
        );
    }


    // ============================================================
    // REFRESH ACTIVE HIGHLIGHTS
    // ============================================================

    public void RefreshActiveHighlights()
    {
        if (!HasReferences())
        {
            return;
        }


        switch (currentState)
        {
            case HighlightState.MovementRange:

                RefreshMovementRangeFromCache();

                break;


            case HighlightState.BasicAbilityRange:

                RefreshAbilityRangeFromCache();

                break;


            case HighlightState.ScriptableObjectAbility:

                RefreshAbilityRangeFromCache();

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
    // MOVEMENT RANGE
    // ============================================================

    public void ShowMovementRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        // ========================================================
        // NEW MOVEMENT REQUEST
        // ========================================================

        movementRangeHidden = false;


        currentState =
            HighlightState.MovementRange;

        cachedState =
            currentState;

        cachedCenterPos =
            centerPosition;

        cachedRange =
            range;

        cachedUser =
            user;

        cachedUserTile =
            centerPosition;


        CacheUserComponents(
            user
        );


        // ========================================================
        // IMPORTANT
        // ========================================================
        //
        // Refresh grid bounds every time movement range is shown.
        //
        // This makes sure the current encounter's grid is used.
        // ========================================================

        RefreshGridBounds();


        RefreshMovementRangeFromCache();


        DebugLog(
            "Movement range shown. Center: " +
            centerPosition +
            ", Range: " +
            range
        );
    }


    // ============================================================
    // REFRESH MOVEMENT RANGE
    // ============================================================

    private void RefreshMovementRangeFromCache()
    {
        if (!HasReferences())
        {
            return;
        }


        // ========================================================
        // MOVEMENT RANGE WAS INTENTIONALLY HIDDEN
        // ========================================================

        if (movementRangeHidden)
        {
            return;
        }


        // ========================================================
        // REFRESH CURRENT GRID BOUNDS
        // ========================================================

        RefreshGridBounds();


        // ========================================================
        // CLEAN DEAD / INVALID UNITS
        // ========================================================

        gridManager.CleanupDeadUnits();


        // ========================================================
        // RESOLVE USER POSITION
        // ========================================================

        Vector2Int center =
            ResolveCachedUserCenter();


        if (!IsInsideGrid(center))
        {
            highlightManager.ClearMovementRange();

            DebugLogWarning(
                "Movement highlight center is outside current grid: " +
                center
            );

            return;
        }


        // ========================================================
        // FIND ACTUAL REACHABLE CELLS
        // ========================================================
        //
        // IMPORTANT:
        //
        // Do not use simple GetDistance() here.
        //
        // UnitMoveBrain uses the actual pathfinding system.
        // The highlight should therefore use the same system.
        // ========================================================

        reusableTileList.Clear();


        if (cachedMoveBrain == null)
        {
            if (cachedUser != null)
            {
                cachedMoveBrain =
                    cachedUser.GetComponent<UnitMoveBrain>();
            }
        }


        if (cachedMoveBrain == null)
        {
            DebugLogWarning(
                "Could not find UnitMoveBrain for movement highlight."
            );

            return;
        }


        UnitMoveBrainManager moveBrainManager =
            UnitMoveBrainManager.Instance;


        if (moveBrainManager == null)
        {
            DebugLogWarning(
                "UnitMoveBrainManager.Instance is null."
            );

            return;
        }


        moveBrainManager.GetReachableCells(
            center,
            cachedRange,
            cachedMoveBrain.CanWalkDiagonally(),
            reusableTileList,
            cachedUser
        );


        // ========================================================
        // NEVER SHOW USER'S OWN TILE
        // ========================================================

        reusableTileList.Remove(center);


        // ========================================================
        // SHOW MOVEMENT TILES
        // ========================================================

        highlightManager.ShowMovementTiles(
            reusableTileList,
            cachedUser
        );


        DebugLog(
            "Movement range refreshed. " +
            "Reachable cells: " +
            reusableTileList.Count
        );
    }


    // ============================================================
    // HIDE MOVEMENT RANGE
    // ============================================================

    public void HideMovementRange()
    {
        movementRangeHidden = true;


        if (highlightManager != null)
        {
            highlightManager.ClearMovementRange();
        }


        DebugLog(
            "Movement range hidden after destination selection."
        );
    }


    // ============================================================
    // IS MOVEMENT RANGE HIDDEN
    // ============================================================

    public bool IsMovementRangeHidden()
    {
        return movementRangeHidden;
    }


    // ============================================================
    // MOVEMENT CACHE STATE
    // ============================================================

    public void ClearMovementHighlightState()
    {
        if (
            currentState ==
            HighlightState.MovementRange
        )
        {
            currentState =
                HighlightState.None;
        }


        cachedState =
            currentState;


        cachedCenterPos =
            Vector2Int.zero;

        cachedUserTile =
            Vector2Int.zero;

        cachedRange = 0;

        cachedUser = null;

        cachedUserTilePin = null;

        cachedMoveBrain = null;

        movementRangeHidden = false;


        if (highlightManager != null)
        {
            highlightManager.ClearMovementRange();
        }


        DebugLog(
            "Movement highlight state completely cleared."
        );
    }


    // ============================================================
    // UNIT STATE CHANGE REFRESH
    // ============================================================

    public void RefreshAfterUnitStateChanged()
    {
        if (isBoardRotating)
        {
            DebugLog(
                "Unit-state refresh skipped because board is rotating."
            );

            return;
        }


        if (!HasReferences())
        {
            DebugLog(
                "Unit-state refresh failed because references are missing."
            );

            return;
        }


        DebugLog(
            "Refreshing highlights after unit state changed."
        );


        RefreshActiveHighlights();
    }


    // ============================================================
    // USER TILE
    // ============================================================

    private bool TryGetUserLogicalTile(
        GameObject user,
        out Vector2Int tile)
    {
        tile =
            Vector2Int.zero;


        if (user == null)
        {
            return false;
        }


        UnitTilePin pin =
            user.GetComponent<UnitTilePin>();


        if (pin == null)
        {
            return false;
        }


        tile =
            pin.GetGridPosition();


        return true;
    }


    private Vector2Int ResolveCachedUserCenter()
    {
        if (
            cachedUser != null &&
            TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int currentTile
            )
        )
        {
            cachedUserTile =
                currentTile;

            cachedCenterPos =
                currentTile;
        }


        return cachedCenterPos;
    }


    private bool UsesUnitPosition()
    {
        return
            currentState ==
                HighlightState.MovementRange ||
            currentState ==
                HighlightState.ScriptableObjectAbility;
    }


    // ============================================================
    // CACHE USER COMPONENTS
    // ============================================================

    private void CacheUserComponents(
        GameObject user)
    {
        cachedUserTilePin = null;

        cachedMoveBrain = null;


        if (user == null)
        {
            return;
        }


        cachedUserTilePin =
            user.GetComponent<UnitTilePin>();

        cachedMoveBrain =
            user.GetComponent<UnitMoveBrain>();
    }


    // ============================================================
    // BASIC ABILITY
    // ============================================================

    public void ShowBasicAbilityRange(
        Vector2Int centerPosition,
        int range,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        currentState =
            HighlightState.BasicAbilityRange;

        cachedState =
            currentState;

        cachedCenterPos =
            centerPosition;

        cachedRange =
            range;

        cachedUser =
            user;


        CacheUserComponents(
            user
        );


        RefreshGridBounds();

        RefreshAbilityRangeFromCache();
    }


    private void RefreshAbilityRangeFromCache()
    {
        if (!HasReferences())
        {
            return;
        }


        RefreshGridBounds();


        reusableTileList.Clear();


        Vector2Int center =
            ResolveCachedUserCenter();


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
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );


                if (position == center)
                {
                    continue;
                }


                if (
                    gridManager.GetDistance(
                        center,
                        position
                    ) > cachedRange
                )
                {
                    continue;
                }


                reusableTileList.Add(
                    position
                );
            }
        }


        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }


    // ============================================================
    // SCRIPTABLE OBJECT ABILITY
    // ============================================================

    public void ShowScriptableObjectAbility(
        AbilitySO ability,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        currentState =
            HighlightState.ScriptableObjectAbility;

        cachedState =
            currentState;

        cachedAbility =
            ability;

        cachedUser =
            user;


        CacheUserComponents(
            user
        );


        if (ability == null)
        {
            return;
        }


        RefreshGridBounds();

        RefreshAbilityRangeFromCache();
    }


    // ============================================================
    // CUSTOM TILES
    // ============================================================

    public void ShowCustomTiles(
        List<Vector2Int> positions,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        currentState =
            HighlightState.CustomTiles;

        cachedState =
            currentState;

        cachedUser =
            user;


        cachedCustomPositions.Clear();


        if (positions != null)
        {
            cachedCustomPositions.AddRange(
                positions
            );
        }


        highlightManager.ShowAbilityTiles(
            cachedCustomPositions,
            user
        );
    }


    private void RefreshCustomTilesFromCache()
    {
        if (!HasReferences())
        {
            return;
        }


        highlightManager.ShowAbilityTiles(
            cachedCustomPositions,
            cachedUser
        );
    }


    // ============================================================
    // OFFSET CELLS
    // ============================================================

    public void ShowOffsetCells(
        Vector2Int centerPosition,
        List<Vector2Int> offsets,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        currentState =
            HighlightState.OffsetCells;

        cachedState =
            currentState;

        cachedCenterPos =
            centerPosition;

        cachedUser =
            user;


        cachedOffsetCells.Clear();


        if (offsets != null)
        {
            cachedOffsetCells.AddRange(
                offsets
            );
        }


        RefreshGridBounds();

        RefreshOffsetCellsFromCache();
    }


    private void RefreshOffsetCellsFromCache()
    {
        if (!HasReferences())
        {
            return;
        }


        reusableTileList.Clear();


        foreach (
            Vector2Int offset
            in cachedOffsetCells
        )
        {
            Vector2Int position =
                cachedCenterPos +
                offset;


            if (
                gridManager.IsInsideGrid(
                    position
                )
            )
            {
                reusableTileList.Add(
                    position
                );
            }
        }


        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }


    // ============================================================
    // SINGLE CELL
    // ============================================================

    public void ShowSingleCell(
        Vector2Int position,
        GameObject user = null)
    {
        if (!HasReferences())
        {
            return;
        }


        currentState =
            HighlightState.SingleCell;

        cachedState =
            currentState;

        cachedCenterPos =
            position;

        cachedUser =
            user;


        RefreshGridBounds();

        RefreshSingleCellFromCache();
    }


    private void RefreshSingleCellFromCache()
    {
        if (!HasReferences())
        {
            return;
        }


        reusableTileList.Clear();


        if (
            gridManager.IsInsideGrid(
                cachedCenterPos
            )
        )
        {
            reusableTileList.Add(
                cachedCenterPos
            );
        }


        highlightManager.ShowAbilityTiles(
            reusableTileList,
            cachedUser
        );
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }


    public GridHighlightManager GetHighlightManager()
    {
        return highlightManager;
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }


        Debug.Log(
            "[GridHighlightBrain] " +
            message,
            this
        );
    }


    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }


        Debug.LogWarning(
            "[GridHighlightBrain] " +
            message,
            this
        );
    }
}