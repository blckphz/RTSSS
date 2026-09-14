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
        // ==================================================
        // BOARD ROTATION
        // ==================================================

        BoardViewController board =
            BoardViewController.Instance;

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

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        if (!gridManager.IsInsideGrid(
                calculatedTile))
        {
            return;
        }

        logicalTile =
            calculatedTile;

        hasTile = true;

        // Initial placement only.
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

        // This is now called explicitly
        // after movement has finished.
        PinToTile();

        ForceRotation();
    }


    // ==================================================
    // UPDATE TILE AFTER MOVEMENT
    // ==================================================

    public void UpdateTileAfterMovement(
        Vector2Int newTile
    )
    {
        SetTile(newTile);
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

        Vector2Int calculatedTile =
            gridManager.WorldToGridPosition(
                transform.position
            );

        if (!gridManager.IsInsideGrid(
                calculatedTile))
        {
            return;
        }

        logicalTile =
            calculatedTile;

        hasTile = true;
    }
}