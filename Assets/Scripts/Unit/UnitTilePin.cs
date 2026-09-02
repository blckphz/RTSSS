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
    // SETTINGS
    // ==================================================

    [Header("Pin Settings")]
    [SerializeField]
    private bool pinEveryFrame = true;


    // ==================================================
    // ROTATION
    // ==================================================

    [Header("Rotation")]

    [Tooltip(
        "The rotation the unit should ALWAYS have. " +
        "The unit will never inherit board rotation."
    )]
    [SerializeField]
    private Vector3 fixedRotation = Vector3.zero;

    [Tooltip(
        "If enabled, the unit's rotation is corrected every frame."
    )]
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

    // This is the authoritative logical grid position
    // of this unit.
    //
    // Other systems such as:
    //
    // GridHighlightBrain
    // GridManager
    // UnitMoveBrain
    //
    // should use this logical position instead of
    // converting transform.position whenever possible.
    private Vector2Int logicalTile;

    private bool hasTile;


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

        RegisterCurrentTile();

        ForceRotation();
    }

    private void LateUpdate()
    {
        BoardViewController board =
            BoardViewController.Instance;

        // --------------------------------------------------
        // BOARD ROTATION
        // --------------------------------------------------

        if (
            board != null &&
            board.IsRotating()
        )
        {
            if (lockRotationEveryFrame)
            {
                ForceRotation();
            }

            return;
        }

        // --------------------------------------------------
        // KEEP UNIT PINNED TO ITS LOGICAL TILE
        // --------------------------------------------------

        if (pinEveryFrame)
        {
            PinToTile();
        }

        // --------------------------------------------------
        // KEEP UNIT ROTATION FIXED
        // --------------------------------------------------

        if (lockRotationEveryFrame)
        {
            ForceRotation();
        }
    }


    // ==================================================
    // UNIQUE ID
    // ==================================================

    private void GenerateUniqueId()
    {
        if (!string.IsNullOrEmpty(unitId))
        {
            return;
        }

        unitId =
            "UNIT_" +
            nextUnitId.ToString("D4");

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
        {
            return;
        }

        gridManager =
            FindFirstObjectByType<GridManager>();
    }


    // ==================================================
    // REGISTER CURRENT TILE
    // ==================================================

    public void RegisterCurrentTile()
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            return;
        }

        // Convert the current world position into the
        // logical grid position ONCE when registering.
        //
        // After this, logicalTile becomes the authoritative
        // position for this unit.
        logicalTile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        hasTile = true;

        PinToTile();
    }


    // ==================================================
    // SET TILE
    // ==================================================

    public void SetTile(
        Vector2Int tile
    )
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(tile))
        {
            return;
        }

        logicalTile =
            tile;

        hasTile =
            true;

        PinToTile();
    }


    // ==================================================
    // UPDATE TILE AFTER MOVEMENT
    // ==================================================

    public void UpdateTileAfterMovement(
        Vector2Int newTile
    )
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(newTile))
        {
            return;
        }

        // --------------------------------------------------
        // UPDATE AUTHORITATIVE LOGICAL TILE
        // --------------------------------------------------

        logicalTile =
            newTile;

        hasTile =
            true;

        // --------------------------------------------------
        // UPDATE WORLD POSITION
        // --------------------------------------------------

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                newTile
            );

        transform.position =
            targetPosition;

        // --------------------------------------------------
        // KEEP ROTATION FIXED
        // --------------------------------------------------

        ForceRotation();
    }


    // ==================================================
    // PIN TO TILE
    // ==================================================

    public void PinToTile()
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            return;
        }

        if (!hasTile)
        {
            return;
        }

        BoardViewController board =
            BoardViewController.Instance;

        // Do not reposition the unit while the board itself
        // is rotating.
        if (
            board != null &&
            board.IsRotating()
        )
        {
            return;
        }

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                logicalTile
            );

        transform.position =
            targetPosition;
    }


    // ==================================================
    // FORCE ROTATION
    // ==================================================

    public void ForceRotation()
    {
        Quaternion targetRotation =
            Quaternion.Euler(
                fixedRotation
            );

        if (
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            ) > 0.01f
        )
        {
            transform.rotation =
                targetRotation;
        }
    }


    // ==================================================
    // GET TILE
    // ==================================================

    public Vector2Int GetTile()
    {
        return logicalTile;
    }


    // ==================================================
    // GET GRID POSITION
    // ==================================================

    // This is an alias for GetTile().
    //
    // GridHighlightBrain can therefore safely call:
    //
    // pin.GetGridPosition()
    //
    // while older scripts can continue using:
    //
    // pin.GetTile()
    //
    // Both return the exact same logical tile.

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

    public void SetFixedRotation(
        Vector3 rotation
    )
    {
        fixedRotation =
            rotation;

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
    // FORCE REGISTER TILE
    // ==================================================

    // Useful if another system has moved this unit and
    // you want UnitTilePin to immediately recognize the
    // new logical tile.
    public void RefreshTileFromWorldPosition()
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            return;
        }

        logicalTile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        hasTile = true;
    }
}