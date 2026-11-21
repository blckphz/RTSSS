
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
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
    // BOARD ROTATION
    // ============================================================

    [Header("Board Rotation")]
    [SerializeField] private bool refreshAfterBoardRotation = true;
    [SerializeField, Min(0f)] private float boardRotationThreshold = 0.1f;

    // ============================================================
    // CURRENT STATE
    // ============================================================

    [SerializeField]
    private HighlightState currentState = HighlightState.None;

    private HighlightState cachedState = HighlightState.None;

    // ============================================================
    // MOVEMENT VISIBILITY
    // ============================================================

    private bool movementRangeHidden;
    private bool movementRangeHiddenWhileMoving;

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

    private readonly List<Vector2Int> cachedCustomPositions =
        new List<Vector2Int>(64);

    private readonly List<Vector2Int> cachedOffsetCells =
        new List<Vector2Int>(64);

    // ============================================================
    // USER COMPONENT CACHE
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

    private readonly List<Vector2Int> reusableTileList =
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

        lastBoardRotation = transform.rotation;

        StartCoroutine(DelayedInitialRefresh());
    }

    private void OnEnable()
    {
        UnitMoveBrain.OnMovementActionsChanged +=
            HandleMovementActionsChanged;

        UnitMoveBrain.OnMovementStarted +=
            HandleMovementStarted;

        UnitMoveBrain.OnMovementFinished +=
            HandleMovementFinished;
    }

    private void OnDisable()
    {
        UnitMoveBrain.OnMovementActionsChanged -=
            HandleMovementActionsChanged;

        UnitMoveBrain.OnMovementStarted -=
            HandleMovementStarted;

        UnitMoveBrain.OnMovementFinished -=
            HandleMovementFinished;

        if (boardRotationCoroutine != null)
        {
            StopCoroutine(boardRotationCoroutine);
            boardRotationCoroutine = null;
        }
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
            gridManager = FindFirstObjectByType<GridManager>();

        if (highlightManager == null)
            highlightManager = FindFirstObjectByType<GridHighlightManager>();
    }

    private bool HasReferences()
    {
        if (gridManager == null || highlightManager == null)
            FindReferences();

        return gridManager != null &&
               highlightManager != null;
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetForNewEncounter()
    {
        if (boardRotationCoroutine != null)
        {
            StopCoroutine(boardRotationCoroutine);
            boardRotationCoroutine = null;
        }

        isBoardRotating = false;

        if (highlightManager != null)
            highlightManager.ClearAllHighlights();

        currentState = HighlightState.None;
        cachedState = HighlightState.None;

        cachedCenterPos = Vector2Int.zero;
        cachedUserTile = Vector2Int.zero;

        cachedRange = 0;
        cachedUser = null;

        cachedAbility = null;

        cachedCustomPositions.Clear();
        cachedOffsetCells.Clear();

        cachedUserTilePin = null;
        cachedMoveBrain = null;

        movementRangeHidden = false;
        movementRangeHiddenWhileMoving = false;

        gridManager = null;
        highlightManager = null;

        FindReferences();
        RefreshGridBounds();

        lastBoardRotation = transform.rotation;
    }

    // ============================================================
    // GRID BOUNDS
    // ============================================================

    public void RefreshGridBounds()
    {
        if (gridManager == null)
            FindReferences();

        if (gridManager == null)
            return;

        minGridX = gridManager.GetMinX();
        maxGridX = gridManager.GetMaxX();

        minGridY = gridManager.GetMinY();
        maxGridY = gridManager.GetMaxY();
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= minGridX &&
               position.x <= maxGridX &&
               position.y >= minGridY &&
               position.y <= maxGridY;
    }

    // ============================================================
    // BOARD ROTATION
    // ============================================================

    private void CheckBoardRotationChange()
    {
        Quaternion currentRotation = transform.rotation;

        float angle = Quaternion.Angle(
            lastBoardRotation,
            currentRotation
        );

        if (angle < boardRotationThreshold)
            return;

        lastBoardRotation = currentRotation;
        isBoardRotating = true;

        if (boardRotationCoroutine != null)
            StopCoroutine(boardRotationCoroutine);

        boardRotationCoroutine =
            StartCoroutine(HandleBoardRotationRefresh());
    }

    private IEnumerator HandleBoardRotationRefresh()
    {
        if (refreshAfterBoardRotation)
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            RefreshGridBounds();

            if (BoardViewController.Instance == null ||
                !BoardViewController.Instance.IsRotating())
            {
                RefreshActiveHighlights();
            }
        }

        isBoardRotating = false;
        boardRotationCoroutine = null;
    }

    // ============================================================
    // MOVEMENT EVENTS
    // ============================================================

    private void HandleMovementStarted(UnitMoveBrain movingUnit)
    {
        if (movingUnit == null ||
            cachedUser != movingUnit.gameObject ||
            currentState != HighlightState.MovementRange)
            return;

        movementRangeHiddenWhileMoving = true;

        if (highlightManager != null)
            highlightManager.ClearMovementRange();
    }

    private void HandleMovementFinished(UnitMoveBrain movingUnit)
    {
        if (movingUnit == null ||
            cachedUser != movingUnit.gameObject ||
            currentState != HighlightState.MovementRange)
            return;

        movementRangeHiddenWhileMoving = false;

        if (!CanShowMovementRange())
        {
            HideMovementRange();
            return;
        }

        if (movingUnit.HasUsedAllMovement())
        {
            HideMovementRange();
            return;
        }

        RefreshMovementRangeFromCache();
    }

    private void HandleMovementActionsChanged(UnitMoveBrain changedUnit)
    {
        if (changedUnit == null ||
            cachedUser != changedUnit.gameObject ||
            currentState != HighlightState.MovementRange)
            return;

        if (changedUnit.IsMoving())
            return;

        if (!CanShowMovementRange() ||
            changedUnit.HasUsedAllMovement())
        {
            HideMovementRange();
            return;
        }

        RefreshMovementRangeFromCache();
    }

    // ============================================================
    // MOVEMENT VALIDATION
    // ============================================================

    private bool CanShowMovementRange()
    {
        if (cachedMoveBrain == null)
            return false;

        return cachedMoveBrain.GetMoveRange() > 0;
    }

    // ============================================================
    // HIGHLIGHTED UNIT TILE TRACKING
    // ============================================================

    private void CheckHighlightedUnitTileChanged()
    {
        if (!UsesUnitPosition() || cachedUser == null)
            return;

        if (currentState == HighlightState.MovementRange &&
            cachedMoveBrain != null &&
            cachedMoveBrain.IsMoving())
            return;

        if (!TryGetUserLogicalTile(
            cachedUser,
            out Vector2Int currentTile))
            return;

        if (currentTile == cachedUserTile)
            return;

        cachedUserTile = currentTile;
        cachedCenterPos = currentTile;

        if (!isBoardRotating)
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
        return currentState != HighlightState.None;
    }

    // ============================================================
    // CLEAR ALL HIGHLIGHTS
    // ============================================================

    public void ClearAllHighlights()
    {
        currentState = HighlightState.None;
        cachedState = HighlightState.None;

        cachedUser = null;
        cachedAbility = null;

        cachedRange = 0;

        cachedCenterPos = Vector2Int.zero;
        cachedUserTile = Vector2Int.zero;

        cachedCustomPositions.Clear();
        cachedOffsetCells.Clear();

        cachedUserTilePin = null;
        cachedMoveBrain = null;

        movementRangeHidden = false;
        movementRangeHiddenWhileMoving = false;

        if (highlightManager != null)
            highlightManager.ClearAllHighlights();
    }

    // ============================================================
    // REFRESH ACTIVE HIGHLIGHTS
    // ============================================================

    public void RefreshActiveHighlights()
    {
        if (!HasReferences() || isBoardRotating)
            return;

        switch (currentState)
        {
            case HighlightState.MovementRange:
                RefreshMovementRangeFromCache();
                break;

            case HighlightState.BasicAbilityRange:
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
            return;

        // Zero or negative movement range: clear and stop.
        if (range <= 0)
        {
            HideMovementRange();
            return;
        }

        movementRangeHidden = false;
        movementRangeHiddenWhileMoving = false;

        currentState = HighlightState.MovementRange;
        cachedState = currentState;

        cachedCenterPos = centerPosition;
        cachedRange = range;

        cachedUser = user;
        cachedUserTile = centerPosition;

        CacheUserComponents(user);

        // Runtime movement range is the source of truth.
        if (!CanShowMovementRange())
        {
            HideMovementRange();
            return;
        }

        RefreshGridBounds();
        RefreshMovementRangeFromCache();
    }

    // ============================================================
    // REFRESH MOVEMENT RANGE
    // ============================================================

    private void RefreshMovementRangeFromCache()
    {
        if (!HasReferences())
            return;

        if (movementRangeHidden ||
            movementRangeHiddenWhileMoving)
            return;

        // NEW: Never display movement tiles for stationary units.
        if (!CanShowMovementRange())
        {
            HideMovementRange();
            return;
        }

        RefreshGridBounds();

        gridManager.CleanupDeadUnits();

        Vector2Int center = ResolveCachedUserCenter();

        if (!IsInsideGrid(center))
        {
            highlightManager.ClearMovementRange();
            return;
        }

        if (cachedMoveBrain == null)
        {
            if (cachedUser != null)
                cachedMoveBrain =
                    cachedUser.GetComponent<UnitMoveBrain>();
        }

        if (cachedMoveBrain == null)
        {
            highlightManager.ClearMovementRange();
            return;
        }

        if (cachedMoveBrain.IsMoving())
            return;

        if (cachedMoveBrain.HasUsedAllMovement())
        {
            HideMovementRange();
            return;
        }

        UnitMoveBrainManager moveBrainManager =
            UnitMoveBrainManager.Instance;

        if (moveBrainManager == null)
        {
            highlightManager.ClearMovementRange();
            return;
        }

        int remainingSteps =
            cachedMoveBrain.GetStepsRemaining();

        if (remainingSteps <= 0)
        {
            HideMovementRange();
            return;
        }

        moveBrainManager.GetReachableCells(
            center,
            remainingSteps,
            cachedMoveBrain.CanWalkDiagonally(),
            reusableTileList,
            cachedUser
        );

        reusableTileList.Remove(center);

        highlightManager.ShowMovementTiles(
            reusableTileList,
            cachedUser
        );
    }

    // ============================================================
    // HIDE MOVEMENT RANGE
    // ============================================================

    public void HideMovementRange()
    {
        movementRangeHidden = true;
        movementRangeHiddenWhileMoving = false;

        if (highlightManager != null)
            highlightManager.ClearMovementRange();
    }

    public bool IsMovementRangeHidden()
    {
        return movementRangeHidden;
    }

    // ============================================================
    // MOVEMENT CACHE STATE
    // ============================================================

    public void ClearMovementHighlightState()
    {
        if (currentState == HighlightState.MovementRange)
            currentState = HighlightState.None;

        cachedState = currentState;

        cachedCenterPos = Vector2Int.zero;
        cachedUserTile = Vector2Int.zero;

        cachedRange = 0;
        cachedUser = null;

        cachedUserTilePin = null;
        cachedMoveBrain = null;

        movementRangeHidden = false;
        movementRangeHiddenWhileMoving = false;

        if (highlightManager != null)
            highlightManager.ClearMovementRange();
    }

    // ============================================================
    // UNIT STATE CHANGE REFRESH
    // ============================================================

    public void RefreshAfterUnitStateChanged()
    {
        if (isBoardRotating || !HasReferences())
            return;

        RefreshActiveHighlights();
    }

    // ============================================================
    // USER TILE
    // ============================================================

    private bool TryGetUserLogicalTile(
        GameObject user,
        out Vector2Int tile)
    {
        tile = Vector2Int.zero;

        if (user == null)
            return false;

        UnitTilePin pin = user.GetComponent<UnitTilePin>();

        if (pin == null)
            return false;

        tile = pin.GetGridPosition();

        return true;
    }

    private Vector2Int ResolveCachedUserCenter()
    {
        if (cachedUser != null &&
            TryGetUserLogicalTile(
                cachedUser,
                out Vector2Int currentTile))
        {
            cachedUserTile = currentTile;
            cachedCenterPos = currentTile;
        }

        return cachedCenterPos;
    }

    private bool UsesUnitPosition()
    {
        return currentState == HighlightState.MovementRange ||
               currentState == HighlightState.ScriptableObjectAbility;
    }

    // ============================================================
    // CACHE USER COMPONENTS
    // ============================================================

    private void CacheUserComponents(GameObject user)
    {
        cachedUserTilePin = null;
        cachedMoveBrain = null;

        if (user == null)
            return;

        cachedUserTilePin = user.GetComponent<UnitTilePin>();
        cachedMoveBrain = user.GetComponent<UnitMoveBrain>();
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
            return;

        currentState = HighlightState.BasicAbilityRange;
        cachedState = currentState;

        cachedCenterPos = centerPosition;
        cachedRange = range;
        cachedUser = user;

        CacheUserComponents(user);

        RefreshGridBounds();
        RefreshAbilityRangeFromCache();
    }

    private void RefreshAbilityRangeFromCache()
    {
        if (!HasReferences())
            return;

        RefreshGridBounds();

        reusableTileList.Clear();

        Vector2Int center = ResolveCachedUserCenter();

        for (int x = minGridX; x <= maxGridX; x++)
        {
            for (int y = minGridY; y <= maxGridY; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (position == center)
                    continue;

                if (gridManager.GetDistance(center, position) > cachedRange)
                    continue;

                reusableTileList.Add(position);
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
            return;

        currentState = HighlightState.ScriptableObjectAbility;
        cachedState = currentState;

        cachedAbility = ability;
        cachedUser = user;

        CacheUserComponents(user);

        if (ability == null)
            return;

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
            return;

        currentState = HighlightState.CustomTiles;
        cachedState = currentState;

        cachedUser = user;

        cachedCustomPositions.Clear();

        if (positions != null)
            cachedCustomPositions.AddRange(positions);

        highlightManager.ShowAbilityTiles(
            cachedCustomPositions,
            user
        );
    }

    private void RefreshCustomTilesFromCache()
    {
        if (!HasReferences())
            return;

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
            return;

        currentState = HighlightState.OffsetCells;
        cachedState = currentState;

        cachedCenterPos = centerPosition;
        cachedUser = user;

        cachedOffsetCells.Clear();

        if (offsets != null)
            cachedOffsetCells.AddRange(offsets);

        RefreshGridBounds();
        RefreshOffsetCellsFromCache();
    }

    private void RefreshOffsetCellsFromCache()
    {
        if (!HasReferences())
            return;

        reusableTileList.Clear();

        foreach (Vector2Int offset in cachedOffsetCells)
        {
            Vector2Int position = cachedCenterPos + offset;

            if (gridManager.IsInsideGrid(position))
                reusableTileList.Add(position);
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
            return;

        currentState = HighlightState.SingleCell;
        cachedState = currentState;

        cachedCenterPos = position;
        cachedUser = user;

        RefreshGridBounds();
        RefreshSingleCellFromCache();
    }

    private void RefreshSingleCellFromCache()
    {
        if (!HasReferences())
            return;

        reusableTileList.Clear();

        if (gridManager.IsInsideGrid(cachedCenterPos))
            reusableTileList.Add(cachedCenterPos);

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
}
