using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardViewController : MonoBehaviour
{
    public static BoardViewController Instance { get; private set; }


    // ============================================================
    // BOARD
    // ============================================================

    [Header("Board")]
    [SerializeField]
    private Transform boardTransform;


    // ============================================================
    // GRID
    // ============================================================

    [Header("Grid")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private int gridWidth = 11;

    [SerializeField]
    private int gridHeight = 11;

    [SerializeField]
    private float cellSize = 1f;


    // ============================================================
    // INPUT
    // ============================================================

    [Header("Input")]
    [SerializeField]
    private InputActionReference rotateLeftAction;

    [SerializeField]
    private InputActionReference rotateRightAction;


    // ============================================================
    // ROTATION
    // ============================================================

    [Header("Rotation")]
    [SerializeField]
    private float rotationDuration = 0.3f;

    [SerializeField]
    private bool blockInputDuringRotation = true;

    [SerializeField]
    private bool rotateUnits = true;


    // ============================================================
    // START
    // ============================================================

    [Header("Start")]
    [SerializeField]
    private int startingRotation = 0;


    // ============================================================
    // ROTATION SELECTION
    // ============================================================

    [Header("Rotation Selection")]
    [Tooltip(
        "If enabled, the currently selected unit and its range " +
        "preview are cleared as soon as board rotation starts."
    )]
    [SerializeField]
    private bool deselectUnitWhenRotating = true;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = true;


    // ============================================================
    // STATE
    // ============================================================

    private int currentRotation;

    private bool isRotating;

    private Coroutine rotationCoroutine;

    private Vector3 rotationCenter;


    private readonly List<Transform>
        externalUnits =
        new List<Transform>();


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        if (boardTransform == null)
        {
            Debug.LogError(
                "[BoardViewController] Board Transform is missing.",
                this
            );

            return;
        }


        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        currentRotation =
            NormalizeRotation(
                startingRotation
            );


        CalculateGridCenter();

        FindExternalUnits();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] INITIALIZED\n" +
                "Board: " +
                boardTransform.name +
                "\nGrid: " +
                gridWidth +
                " x " +
                gridHeight +
                "\nCell Size: " +
                cellSize +
                "\nRotation Center: " +
                rotationCenter +
                "\nStarting Rotation: " +
                currentRotation,
                this
            );
        }


        if (currentRotation != 0)
        {
            RotateEverythingInstant(
                currentRotation
            );
        }
    }


    private void OnEnable()
    {
        if (rotateLeftAction != null)
        {
            rotateLeftAction.action.performed +=
                OnRotateLeft;

            rotateLeftAction.action.Enable();
        }


        if (rotateRightAction != null)
        {
            rotateRightAction.action.performed +=
                OnRotateRight;

            rotateRightAction.action.Enable();
        }
    }


    private void OnDisable()
    {
        if (rotateLeftAction != null)
        {
            rotateLeftAction.action.performed -=
                OnRotateLeft;

            rotateLeftAction.action.Disable();
        }


        if (rotateRightAction != null)
        {
            rotateRightAction.action.performed -=
                OnRotateRight;

            rotateRightAction.action.Disable();
        }
    }


    // ============================================================
    // INPUT
    // ============================================================

    private void OnRotateLeft(
        InputAction.CallbackContext context)
    {
        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] Rotate LEFT requested.",
                this
            );
        }


        if (
            blockInputDuringRotation &&
            isRotating
        )
        {
            return;
        }


        RotateLeft();
    }


    private void OnRotateRight(
        InputAction.CallbackContext context)
    {
        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] Rotate RIGHT requested.",
                this
            );
        }


        if (
            blockInputDuringRotation &&
            isRotating
        )
        {
            return;
        }


        RotateRight();
    }


    // ============================================================
    // PUBLIC ROTATION
    // ============================================================

    public void RotateLeft()
    {
        RotateTo(
            currentRotation + 90
        );
    }


    public void RotateRight()
    {
        RotateTo(
            currentRotation - 90
        );
    }


    public void RotateTo(
        int targetRotation)
    {
        if (boardTransform == null)
        {
            return;
        }


        if (
            blockInputDuringRotation &&
            isRotating
        )
        {
            return;
        }


        targetRotation =
            NormalizeRotation(
                targetRotation
            );


        if (
            targetRotation ==
            currentRotation
        )
        {
            return;
        }


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] ROTATION START\n" +
                "From: " +
                currentRotation +
                "\nTo: " +
                targetRotation,
                this
            );
        }


        // --------------------------------------------------------
        // DESELECT BEFORE ROTATION
        //
        // This happens BEFORE the rotation coroutine begins.
        //
        // Clearing the highlights here prevents the selected unit's
        // movement/ability range from being rendered while the
        // board and units are physically rotating.
        // --------------------------------------------------------

        if (deselectUnitWhenRotating)
        {
            DeselectUnitBeforeRotation();
        }


        if (rotationCoroutine != null)
        {
            StopCoroutine(
                rotationCoroutine
            );
        }


        rotationCoroutine =
            StartCoroutine(
                RotateBoardCoroutine(
                    targetRotation
                )
            );
    }


    // ============================================================
    // DESELECT BEFORE ROTATION
    // ============================================================

    private void DeselectUnitBeforeRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();


        if (highlightManager == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "GridHighlightManager not found while " +
                    "deselecting before rotation.",
                    this
                );
            }

            return;
        }


        /*
         * Clear EVERYTHING associated with the current selection:
         *
         * - Movement range
         * - Ability range
         * - Target hover outlines
         * - Target pulse
         * - Placement highlight
         * - Current range user
         * - Current ability
         */

        highlightManager.ClearAllHighlights();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Unit deselected before board rotation.",
                this
            );
        }
    }


    // ============================================================
    // ROTATION COROUTINE
    // ============================================================

    private IEnumerator RotateBoardCoroutine(
        int targetRotation)
    {
        isRotating = true;


        CalculateGridCenter();

        FindExternalUnits();


        float totalAngle =
            Mathf.DeltaAngle(
                currentRotation,
                targetRotation
            );


        float elapsed = 0f;


        while (
            elapsed <
            rotationDuration
        )
        {
            elapsed +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    rotationDuration
                );


            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );


            float currentAngle =
                totalAngle *
                smoothProgress;


            float previousProgress =
                Mathf.Clamp01(
                    (
                        elapsed -
                        Time.deltaTime
                    ) /
                    rotationDuration
                );


            float previousSmoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    previousProgress
                );


            float previousAngle =
                totalAngle *
                previousSmoothProgress;


            float frameAngle =
                currentAngle -
                previousAngle;


            // ----------------------------------------------------
            // ROTATE BOARD
            // ----------------------------------------------------

            boardTransform.RotateAround(
                rotationCenter,
                Vector3.forward,
                frameAngle
            );


            // ----------------------------------------------------
            // ROTATE EXTERNAL UNITS
            // ----------------------------------------------------

            if (rotateUnits)
            {
                RotateExternalUnits(
                    frameAngle
                );
            }


            yield return null;
        }


        // --------------------------------------------------------
        // FORCE EXACT FINAL ANGLE
        // --------------------------------------------------------

        float finalCorrection =
            Mathf.DeltaAngle(
                boardTransform.eulerAngles.z,
                targetRotation
            );


        if (Mathf.Abs(finalCorrection) > 0.001f)
        {
            boardTransform.RotateAround(
                rotationCenter,
                Vector3.forward,
                finalCorrection
            );


            if (rotateUnits)
            {
                RotateExternalUnits(
                    finalCorrection
                );
            }
        }


        currentRotation =
            targetRotation;


        isRotating = false;

        rotationCoroutine = null;


        // --------------------------------------------------------
        // WAIT FOR TRANSFORM UPDATE
        // --------------------------------------------------------

        yield return null;

        yield return new WaitForEndOfFrame();


        Physics.SyncTransforms();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] ROTATION COMPLETE\n" +
                "Rotation: " +
                currentRotation +
                "\nCenter: " +
                rotationCenter,
                this
            );
        }


        // --------------------------------------------------------
        // REFRESH HIGHLIGHTS AFTER ROTATION
        // --------------------------------------------------------
        //
        // Normally this will simply rebuild the grid with no
        // selection/range active because we cleared it at the
        // beginning of rotation.
        //

        RefreshHighlightsAfterRotation();
    }


    // ============================================================
    // HIGHLIGHT REFRESH
    // ============================================================

    private void RefreshHighlightsAfterRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();


        if (highlightManager == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "GridHighlightManager not found after rotation.",
                    this
                );
            }

            return;
        }


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] Refreshing highlights after rotation.\n" +
                "Current Rotation: " +
                currentRotation,
                this
            );
        }


        StartCoroutine(
            highlightManager.RefreshAfterBoardRotation()
        );
    }


    // ============================================================
    // EXTERNAL UNITS
    // ============================================================

    private void RotateExternalUnits(
        float angle)
    {
        for (
            int i =
            externalUnits.Count - 1;
            i >= 0;
            i--
        )
        {
            Transform unit =
                externalUnits[i];


            if (unit == null)
            {
                externalUnits.RemoveAt(i);
                continue;
            }


            // ----------------------------------------------------
            // Units already under the board move automatically
            // because their parent is rotating.
            // ----------------------------------------------------

            if (
                unit.IsChildOf(
                    boardTransform
                )
            )
            {
                continue;
            }


            // ----------------------------------------------------
            // IMPORTANT:
            //
            // Do NOT use RotateAround() here.
            //
            // RotateAround() changes both position AND rotation.
            //
            // We only want the unit's POSITION to orbit the board.
            // UnitTilePin is responsible for keeping its rotation.
            // ----------------------------------------------------

            Vector3 offset =
                unit.position -
                rotationCenter;


            Quaternion rotation =
                Quaternion.AngleAxis(
                    angle,
                    Vector3.forward
                );


            offset =
                rotation *
                offset;


            unit.position =
                rotationCenter +
                offset;
        }
    }


    // ============================================================
    // FIND UNITS
    // ============================================================

    private void FindExternalUnits()
    {
        externalUnits.Clear();


        if (!rotateUnits)
        {
            return;
        }


        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );


        for (
            int i = 0;
            i < units.Length;
            i++
        )
        {
            if (units[i] == null)
            {
                continue;
            }


            Transform unit =
                units[i].transform;


            if (
                unit.IsChildOf(
                    boardTransform
                )
            )
            {
                continue;
            }


            externalUnits.Add(
                unit
            );
        }


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] Units found: " +
                units.Length +
                "\nExternal units: " +
                externalUnits.Count,
                this
            );
        }
    }


    // ============================================================
    // INSTANT ROTATION
    // ============================================================

    private void RotateEverythingInstant(
        int rotation)
    {
        if (rotation == 0)
        {
            return;
        }


        CalculateGridCenter();


        float angle =
            Mathf.DeltaAngle(
                0f,
                rotation
            );


        boardTransform.RotateAround(
            rotationCenter,
            Vector3.forward,
            angle
        );


        if (rotateUnits)
        {
            FindExternalUnits();


            for (
                int i = 0;
                i < externalUnits.Count;
                i++
            )
            {
                Transform unit =
                    externalUnits[i];


                if (unit == null)
                {
                    continue;
                }


                if (
                    unit.IsChildOf(
                        boardTransform
                    )
                )
                {
                    continue;
                }


                Vector3 offset =
                    unit.position -
                    rotationCenter;


                offset =
                    Quaternion.AngleAxis(
                        angle,
                        Vector3.forward
                    ) *
                    offset;


                unit.position =
                    rotationCenter +
                    offset;
            }
        }
    }


    // ============================================================
    // GRID CENTER
    // ============================================================

    private void CalculateGridCenter()
    {
        // --------------------------------------------------------
        // Prefer the actual GridManager center.
        // This guarantees the board rotation center matches the
        // logical (0,0) cell.
        // --------------------------------------------------------

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (gridManager != null)
        {
            Grid grid =
                gridManager.GetGrid();


            if (grid != null)
            {
                rotationCenter =
                    gridManager.GridToWorldPosition(
                        Vector2Int.zero
                    );

                return;
            }
        }


        // --------------------------------------------------------
        // Fallback if GridManager is not initialized yet.
        // --------------------------------------------------------

        float centerX =
            (
                gridWidth -
                1
            ) *
            cellSize *
            0.5f;


        float centerY =
            (
                gridHeight -
                1
            ) *
            cellSize *
            0.5f;


        Vector3 localCenter =
            new Vector3(
                centerX,
                centerY,
                0f
            );


        rotationCenter =
            boardTransform.TransformPoint(
                localCenter
            );
    }


    // ============================================================
    // NORMALIZE
    // ============================================================

    private int NormalizeRotation(
        int rotation)
    {
        rotation %= 360;


        if (rotation < 0)
        {
            rotation += 360;
        }


        return rotation;
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

    public int GetCurrentRotation()
    {
        return currentRotation;
    }


    public bool IsRotating()
    {
        return isRotating;
    }


    public Vector3 GetRotationCenter()
    {
        return rotationCenter;
    }


    public Transform GetBoardTransform()
    {
        return boardTransform;
    }


    // ============================================================
    // DEBUG
    // ============================================================

    [ContextMenu("Debug Board State")]
    private void DebugBoardState()
    {
        CalculateGridCenter();


        Debug.Log(
            "[BoardViewController] BOARD STATE\n" +
            "Board: " +
            (
                boardTransform != null
                    ? boardTransform.name
                    : "NULL"
            ) +
            "\nRotation: " +
            currentRotation +
            "\nIs Rotating: " +
            isRotating +
            "\nCenter: " +
            rotationCenter +
            "\nExternal Units: " +
            externalUnits.Count,
            this
        );
    }


    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmos()
    {
        if (boardTransform == null)
        {
            return;
        }


        CalculateGridCenter();


        Gizmos.color =
            Color.yellow;


        Gizmos.DrawSphere(
            rotationCenter,
            0.2f
        );


        Gizmos.DrawLine(
            rotationCenter +
            Vector3.left * 0.5f,

            rotationCenter +
            Vector3.right * 0.5f
        );


        Gizmos.DrawLine(
            rotationCenter +
            Vector3.down * 0.5f,

            rotationCenter +
            Vector3.up * 0.5f
        );
    }
}