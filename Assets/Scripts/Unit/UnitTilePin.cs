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
                PinToTile();
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

        Vector3 worldBefore =
            transform.position;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                worldBefore
            );

        logicalTile =
            calculatedTile;

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

        logicalTile =
            newTile;

        hasTile =
            true;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(
                newTile
            );

        transform.position =
            targetPosition;

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
            return;
        }

        Vector3 worldBefore =
            transform.position;

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                worldBefore
            );

        logicalTile =
            calculatedTile;

        hasTile = true;
    }
}