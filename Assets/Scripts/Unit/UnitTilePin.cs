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


    // ==================================================
    // DEBUG
    // ==================================================

    [Header("DEBUG")]
    [SerializeField]
    private bool debugLogging = true;

    private Vector2Int lastLoggedTile;
    private Vector3 lastLoggedWorldPosition;


    // ==================================================
    // DEBUG HELPERS
    // ==================================================

    private string DebugName()
    {
        return string.IsNullOrEmpty(unitId)
            ? gameObject.name
            : gameObject.name + " [" + unitId + "]";
    }


    private void Log(string message)
    {
        if (!debugLogging)
        {
            return;
        }

        Debug.Log(
            "[UnitTilePin] " +
            DebugName() +
            " | " +
            message,
            this
        );
    }


    private void LogState(string source)
    {
        if (!debugLogging)
        {
            return;
        }

        Vector2Int worldGrid = logicalTile;

        if (gridManager != null)
        {
            worldGrid =
                gridManager.WorldToGridPosition(
                    transform.position
                );
        }

        Log(
            source +
            " | " +
            "logicalTile=" + logicalTile +
            " | " +
            "transform.position=" + transform.position +
            " | " +
            "WorldToGrid(transform)=" + worldGrid +
            " | " +
            "hasTile=" + hasTile
        );
    }


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        GenerateUniqueId();

        FindGridManager();

        LogState("Awake BEFORE ForceRotation");

        ForceRotation();

        LogState("Awake AFTER ForceRotation");
    }


    private void Start()
    {
        FindGridManager();

        LogState("Start BEFORE RegisterCurrentTile");

        RegisterCurrentTile();

        LogState("Start AFTER RegisterCurrentTile");

        ForceRotation();
    }


    private void LateUpdate()
    {
        BoardViewController board =
            BoardViewController.Instance;


        // ==================================================
        // BOARD ROTATION
        // ==================================================

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


        // ==================================================
        // IMPORTANT:
        // DO NOT PIN WHILE THE UNIT IS MOVING.
        //
        // UnitMoveBrain animates transform.position.
        // If we call PinToTile() every LateUpdate while
        // moving, the old logicalTile can fight the animation.
        // ==================================================

        if (pinEveryFrame)
        {
            UnitMoveBrain moveBrain =
                GetComponent<UnitMoveBrain>();

            bool isMoving =
                moveBrain != null &&
                moveBrain.IsMoving();

            if (!isMoving)
            {
                LogState(
                    "LateUpdate BEFORE PinToTile"
                );

                PinToTile();

                LogState(
                    "LateUpdate AFTER PinToTile"
                );
            }
        }


        // ==================================================
        // KEEP ROTATION FIXED
        // ==================================================

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

        Log(
            "FindGridManager | gridManager=" +
            (
                gridManager != null
                    ? gridManager.name
                    : "NULL"
            )
        );
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
            Log(
                "RegisterCurrentTile ABORTED | " +
                "gridManager=NULL"
            );

            return;
        }

        Vector3 worldBefore =
            transform.position;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                worldBefore
            );

        Log(
            "RegisterCurrentTile | " +
            "worldBefore=" + worldBefore +
            " | calculatedTile=" + calculatedTile
        );

        logicalTile =
            calculatedTile;

        hasTile = true;

        LogState(
            "RegisterCurrentTile AFTER logicalTile assignment"
        );

        PinToTile();

        LogState(
            "RegisterCurrentTile AFTER PinToTile"
        );
    }


    // ==================================================
    // SET TILE
    // ==================================================

    public void SetTile(
        Vector2Int tile
    )
    {
        Log(
            "SetTile CALLED | " +
            "requestedTile=" + tile +
            " | previousLogicalTile=" + logicalTile +
            " | transform.position=" + transform.position
        );

        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            Log(
                "SetTile ABORTED | gridManager=NULL"
            );

            return;
        }

        if (!gridManager.IsInsideGrid(tile))
        {
            Log(
                "SetTile REJECTED | " +
                "tile outside grid=" + tile
            );

            return;
        }

        logicalTile =
            tile;

        hasTile =
            true;

        LogState(
            "SetTile AFTER assignment"
        );

        PinToTile();

        LogState(
            "SetTile AFTER PinToTile"
        );
    }


    // ==================================================
    // UPDATE TILE AFTER MOVEMENT
    // ==================================================

    public void UpdateTileAfterMovement(
        Vector2Int newTile
    )
    {
        Log(
            "UpdateTileAfterMovement CALLED | " +
            "newTile=" + newTile +
            " | previousLogicalTile=" + logicalTile +
            " | transform.position=" + transform.position
        );

        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            Log(
                "UpdateTileAfterMovement ABORTED | " +
                "gridManager=NULL"
            );

            return;
        }

        if (!gridManager.IsInsideGrid(newTile))
        {
            Log(
                "UpdateTileAfterMovement REJECTED | " +
                "tile outside grid=" + newTile
            );

            return;
        }

        logicalTile =
            newTile;

        hasTile =
            true;

        LogState(
            "UpdateTileAfterMovement AFTER logicalTile assignment"
        );

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                newTile
            );

        Log(
            "UpdateTileAfterMovement | " +
            "GridToWorldPosition(" +
            newTile +
            ")=" +
            targetPosition
        );

        transform.position =
            targetPosition;

        LogState(
            "UpdateTileAfterMovement AFTER transform.position"
        );

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
            Log(
                "PinToTile ABORTED | gridManager=NULL"
            );

            return;
        }

        if (!hasTile)
        {
            Log(
                "PinToTile ABORTED | hasTile=false"
            );

            return;
        }

        BoardViewController board =
            BoardViewController.Instance;

        if (
            board != null &&
            board.IsRotating()
        )
        {
            Log(
                "PinToTile SKIPPED | board is rotating"
            );

            return;
        }

        Vector3 worldBefore =
            transform.position;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                logicalTile
            );

        Log(
            "PinToTile | " +
            "logicalTile=" + logicalTile +
            " | worldBefore=" + worldBefore +
            " | targetWorld=" + targetPosition
        );

        transform.position =
            targetPosition;

        Vector3 worldAfter =
            transform.position;

        if (worldBefore != worldAfter)
        {
            Log(
                "PinToTile MOVED TRANSFORM | " +
                "before=" + worldBefore +
                " | after=" + worldAfter
            );
        }

        lastLoggedTile =
            logicalTile;

        lastLoggedWorldPosition =
            worldAfter;
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
        if (debugLogging)
        {
            LogState("GetTile");
        }

        return logicalTile;
    }


    // ==================================================
    // GET GRID POSITION
    // ==================================================

    public Vector2Int GetGridPosition()
    {
        if (debugLogging)
        {
            LogState("GetGridPosition");
        }

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
    // REFRESH TILE FROM WORLD
    // ==================================================

    public void RefreshTileFromWorldPosition()
    {
        if (gridManager == null)
        {
            FindGridManager();
        }

        if (gridManager == null)
        {
            Log(
                "RefreshTileFromWorldPosition ABORTED | " +
                "gridManager=NULL"
            );

            return;
        }

        Vector3 worldBefore =
            transform.position;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                worldBefore
            );

        Log(
            "RefreshTileFromWorldPosition | " +
            "world=" + worldBefore +
            " | calculatedTile=" + calculatedTile +
            " | previousLogicalTile=" + logicalTile
        );

        logicalTile =
            calculatedTile;

        hasTile = true;

        LogState(
            "RefreshTileFromWorldPosition AFTER assignment"
        );
    }
}