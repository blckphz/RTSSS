
using UnityEngine;

public class UnitTilePin : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    // ==================================================
    // WALKABILITY
    // ==================================================

    [Header("Tile Walkability")]
    [SerializeField]
    private bool isWalkable = false;

    public bool IsWalkable
    {
        get { return isWalkable; }
    }

    // ==================================================
    // ROTATION
    // ==================================================

    [Header("Rotation")]
    [SerializeField]
    private Vector3 fixedRotation = Vector3.zero;

    [SerializeField]
    private bool lockRotationEveryFrame = true;

    // ==================================================
    // UNIQUE UNIT ID
    // ==================================================

    [Header("Runtime Unit ID")]
    [SerializeField]
    private string unitId;

    private static int nextUnitId = 1;

    // ==================================================
    // TILE STATE
    // ==================================================

    private Vector2Int logicalTile;
    private bool hasTile;
    private bool tileWasExplicitlyAssigned;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        GenerateUniqueId();
        FindGridManager();
        ForceRotation();
    }

    private void Start()
    {
        FindGridManager();

        if (tileWasExplicitlyAssigned && hasTile)
        {
            PinToTile();
        }
        else
        {
            RegisterCurrentTile();
        }

        ForceRotation();
    }

    private void LateUpdate()
    {
        BoardViewController board =
            BoardViewController.Instance;

        if (board != null && board.IsRotating())
        {
            if (lockRotationEveryFrame)
                ForceRotation();

            return;
        }

        if (lockRotationEveryFrame)
            ForceRotation();
    }

    private void OnDestroy()
    {
        if (gridManager == null)
            return;

        // Release reserved spawn position when a bush
        // or another walkable obstacle is destroyed.
        if (isWalkable && hasTile)
        {
            gridManager.UnregisterWalkableObstacle(logicalTile);
        }
    }

    // ==================================================
    // UNIQUE ID
    // ==================================================

    private void GenerateUniqueId()
    {
        if (!string.IsNullOrEmpty(unitId))
            return;

        unitId = "UNIT_" + nextUnitId.ToString("D4");

        nextUnitId++;
    }

    public string GetUnitId()
    {
        return unitId;
    }

    // ==================================================
    // FIND GRID MANAGER
    // ==================================================

    private void FindGridManager()
    {
        if (gridManager != null)
            return;

        gridManager = FindFirstObjectByType<GridManager>();
    }

    // ==================================================
    // REGISTER CURRENT TILE
    // ==================================================

    public void RegisterCurrentTile()
    {
        if (gridManager == null)
            FindGridManager();

        if (gridManager == null)
            return;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(transform.position);

        if (!gridManager.IsInsideGrid(calculatedTile))
            return;

        logicalTile = calculatedTile;

        hasTile = true;
        tileWasExplicitlyAssigned = false;

        PinToTile();
    }

    // ==================================================
    // SET TILE
    // ==================================================

    public void SetTile(Vector2Int tile)
    {
        if (gridManager == null)
            FindGridManager();

        if (gridManager == null)
            return;

        if (!gridManager.IsInsideGrid(tile))
            return;

        logicalTile = tile;

        hasTile = true;
        tileWasExplicitlyAssigned = true;

        PinToTile();

        ForceRotation();
    }

    // ==================================================
    // UPDATE TILE AFTER MOVEMENT
    // ==================================================

    public void UpdateTileAfterMovement(Vector2Int newTile)
    {
        SetTile(newTile);
    }

    // ==================================================
    // PIN TO TILE
    // ==================================================

    public void PinToTile()
    {
        if (gridManager == null)
            FindGridManager();

        if (gridManager == null || !hasTile)
            return;

        BoardViewController board =
            BoardViewController.Instance;

        if (board != null && board.IsRotating())
            return;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(logicalTile);

        transform.position = targetPosition;
    }

    // ==================================================
    // FORCE ROTATION
    // ==================================================

    public void ForceRotation()
    {
        Quaternion targetRotation =
            Quaternion.Euler(fixedRotation);

        if (Quaternion.Angle(
            transform.rotation,
            targetRotation) > 0.01f)
        {
            transform.rotation = targetRotation;
        }
    }

    // ==================================================
    // GET TILE
    // ==================================================

    public Vector2Int GetTile()
    {
        return logicalTile;
    }

    public Vector2Int GetGridPosition()
    {
        return logicalTile;
    }

    // ==================================================
    // HAS TILE
    // ==================================================

    public bool HasTile()
    {
        return hasTile;
    }

    // ==================================================
    // GET GRID MANAGER
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    // ==================================================
    // SET FIXED ROTATION
    // ==================================================

    public void SetFixedRotation(Vector3 rotation)
    {
        fixedRotation = rotation;
        ForceRotation();
    }

    // ==================================================
    // GET FIXED ROTATION
    // ==================================================

    public Vector3 GetFixedRotation()
    {
        return fixedRotation;
    }

    // ==================================================
    // REFRESH TILE FROM WORLD
    // ==================================================

    public void RefreshTileFromWorldPosition()
    {
        if (gridManager == null)
            FindGridManager();

        if (gridManager == null)
            return;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(transform.position);

        if (!gridManager.IsInsideGrid(calculatedTile))
            return;

        logicalTile = calculatedTile;

        hasTile = true;
        tileWasExplicitlyAssigned = false;
    }

    // ==================================================
    // SET WALKABILITY
    // ==================================================

    public void SetWalkable(bool value)
    {
        isWalkable = value;
    }
}
