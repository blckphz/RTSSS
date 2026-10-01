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
    // MOVEMENT / ROUND ROTATION LOCK
    // ============================================================

    [Header("Movement / Round Rotation Lock")]
    [SerializeField]
    private bool preventRotationWhileUnitsMove = true;

    [SerializeField]
    private bool preventRotationDuringEnemyTurn = true;

    [SerializeField]
    private RoundManager roundManager;


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
        "If enabled, highlight visuals are hidden while the board rotates. " +
        "The selected unit/highlight state itself is NOT destroyed."
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

    private bool restoreAbilityHighlightAfterRotation;

    private readonly List<Transform> externalUnits =
        new List<Transform>();


    // ============================================================
    // SELECTED UNIT RESTORATION STATE
    // ============================================================

    private GameObject selectedUnitBeforeRotation;

    private bool restoreUnitRangeAfterRotation;


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


        if (roundManager == null)
        {
            roundManager =
                FindFirstObjectByType<RoundManager>();
        }


        currentRotation =
            NormalizeRotation(
                startingRotation
            );


        CalculateGridCenter();

        FindExternalUnits();


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
    // GLOBAL ROTATION LOCK
    // ============================================================

    private bool IsRotationBlocked()
    {
        if (
            preventRotationWhileUnitsMove &&
            UnitMoveBrain.IsAnyUnitMoving
        )
        {
            return true;
        }


        if (
            preventRotationDuringEnemyTurn &&
            roundManager != null &&
            roundManager.IsEnemyTurn()
        )
        {
            return true;
        }


        return false;
    }


    // ============================================================
    // INPUT
    // ============================================================

    private void OnRotateLeft(
        InputAction.CallbackContext context
    )
    {
        if (!context.performed)
        {
            return;
        }


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


        if (IsRotationBlocked())
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "LEFT rotation blocked. " +
                    "A unit is moving or the enemy turn is active.",
                    this
                );
            }

            return;
        }


        RotateLeft();
    }


    private void OnRotateRight(
        InputAction.CallbackContext context
    )
    {
        if (!context.performed)
        {
            return;
        }


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


        if (IsRotationBlocked())
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "RIGHT rotation blocked. " +
                    "A unit is moving or the enemy turn is active.",
                    this
                );
            }

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
        int targetRotation
    )
    {
        if (boardTransform == null)
        {
            return;
        }


        if (IsRotationBlocked())
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] Rotation request rejected.\n" +
                    "Units Moving: " +
                    UnitMoveBrain.IsAnyUnitMoving +
                    "\nMoving Unit Count: " +
                    UnitMoveBrain.GetMovingUnitCount() +
                    "\nEnemy Turn: " +
                    (
                        roundManager != null &&
                        roundManager.IsEnemyTurn()
                    ),
                    this
                );
            }

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
        // SAVE CURRENT SELECTION / ABILITY STATE.
        // --------------------------------------------------------

        CaptureSelectionStateBeforeRotation();


        // --------------------------------------------------------
        // HIDE VISUALS ONLY.
        //
        // DO NOT call ClearAllHighlights().
        //
        // The GridHighlightBrain must retain its cached movement
        // state so it can rebuild the range after rotation.
        // --------------------------------------------------------

        if (deselectUnitWhenRotating)
        {
            HideHighlightVisualsBeforeRotation();
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
    // CAPTURE SELECTION STATE
    // ============================================================

    private void CaptureSelectionStateBeforeRotation()
    {
        restoreAbilityHighlightAfterRotation = false;

        restoreUnitRangeAfterRotation = false;

        selectedUnitBeforeRotation = null;


        // --------------------------------------------------------
        // SELECTED UNIT
        // --------------------------------------------------------

        if (UIManager.CurrentSelection != null)
        {
            selectedUnitBeforeRotation =
                UIManager.CurrentSelection.gameObject;

            restoreUnitRangeAfterRotation = true;


            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "Selected unit saved before rotation.\n" +
                    "Unit: " +
                    selectedUnitBeforeRotation.name,
                    this
                );
            }
        }


        // --------------------------------------------------------
        // ABILITY
        // --------------------------------------------------------

        CanvasInfoManager canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();


        if (canvasInfoManager == null)
        {
            return;
        }


        if (!canvasInfoManager.HasSelectedAbility())
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "No ability selected before rotation.",
                    this
                );
            }

            return;
        }


        if (selectedUnitBeforeRotation == null)
        {
            return;
        }


        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();


        if (ability == null)
        {
            return;
        }


        restoreAbilityHighlightAfterRotation = true;


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Ability highlight marked for restoration.\n" +
                "Ability: " +
                ability.name +
                "\nUnit: " +
                selectedUnitBeforeRotation.name,
                this
            );
        }
    }


    // ============================================================
    // HIDE HIGHLIGHT VISUALS
    // ============================================================

    private void HideHighlightVisualsBeforeRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();


        if (highlightManager == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "GridHighlightManager not found while hiding " +
                    "highlight visuals.",
                    this
                );
            }

            return;
        }


        // IMPORTANT:
        //
        // This method must NOT destroy:
        //
        // movementCells
        // abilityCells
        // currentRangeUser
        // currentAbility
        //
        // Those are needed after rotation.

        highlightManager.ClearVisualsForBoardRotation();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Highlight VISUALS hidden before board rotation. " +
                "Cached highlight state preserved.",
                this
            );
        }
    }


    // ============================================================
    // ROTATION COROUTINE
    // ============================================================

    private IEnumerator RotateBoardCoroutine(
        int targetRotation
    )
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


            boardTransform.RotateAround(
                rotationCenter,
                Vector3.forward,
                frameAngle
            );


            if (rotateUnits)
            {
                RotateExternalUnits(
                    frameAngle
                );
            }


            yield return null;
        }


        // ========================================================
        // FINAL ROTATION CORRECTION
        // ========================================================

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


        // --------------------------------------------------------
        // Rotation is now physically complete.
        // --------------------------------------------------------

        isRotating = false;

        rotationCoroutine = null;


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


        // ========================================================
        // REFRESH GRID VISUAL CACHE
        // ========================================================

        yield return StartCoroutine(
            RefreshHighlightsAfterRotation()
        );


        // ========================================================
        // RESTORE CACHED MOVEMENT RANGE
        // ========================================================

        if (restoreUnitRangeAfterRotation)
        {
            yield return StartCoroutine(
                RestoreSelectedUnitRangeAfterRotation()
            );
        }


        // ========================================================
        // RESTORE ABILITY
        // ========================================================

        if (restoreAbilityHighlightAfterRotation)
        {
            yield return null;

            yield return new WaitForEndOfFrame();

            RestoreAbilityHighlightAfterRotation();
        }


        // ========================================================
        // CLEANUP
        // ========================================================

        restoreAbilityHighlightAfterRotation = false;

        restoreUnitRangeAfterRotation = false;

        selectedUnitBeforeRotation = null;
    }


    // ============================================================
    // REFRESH HIGHLIGHTS AFTER ROTATION
    // ============================================================

    private IEnumerator RefreshHighlightsAfterRotation()
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

            yield break;
        }


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Refreshing highlights after rotation.\n" +
                "Current Rotation: " +
                currentRotation,
                this
            );
        }


        // IMPORTANT:
        //
        // Wait for the manager to actually rebuild its tile cache.
        //

        yield return StartCoroutine(
            highlightManager.RefreshAfterBoardRotation()
        );


        yield return null;

        yield return new WaitForEndOfFrame();
    }


    // ============================================================
    // RESTORE SELECTED UNIT RANGE
    // ============================================================

    private IEnumerator RestoreSelectedUnitRangeAfterRotation()
    {
        if (selectedUnitBeforeRotation == null)
        {
            yield break;
        }


        if (UIManager.CurrentSelection == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "Selected unit no longer exists after rotation.",
                    this
                );
            }

            yield break;
        }


        GameObject selectedUnit =
            UIManager.CurrentSelection.gameObject;


        if (
            selectedUnit !=
            selectedUnitBeforeRotation
        )
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "Selected unit changed during rotation. " +
                    "Range restoration cancelled.",
                    this
                );
            }

            yield break;
        }


        // ========================================================
        // MOST IMPORTANT PART
        //
        // Tell GridHighlightBrain to rebuild its cached active
        // highlight.
        // ========================================================

        GridHighlightBrain highlightBrain =
            FindFirstObjectByType<GridHighlightBrain>();


        if (highlightBrain != null)
        {
            highlightBrain.RefreshActiveHighlights();


            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "GridHighlightBrain.RefreshActiveHighlights() " +
                    "called after rotation.\n" +
                    "Unit: " +
                    selectedUnit.name +
                    "\nRotation: " +
                    currentRotation,
                    this
                );
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "GridHighlightBrain not found after rotation.",
                    this
                );
            }
        }


        yield return null;

        yield return new WaitForEndOfFrame();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Selected player's grid range refreshed after rotation.\n" +
                "Unit: " +
                selectedUnit.name +
                "\nRotation: " +
                currentRotation,
                this
            );
        }
    }


    // ============================================================
    // RESTORE ABILITY HIGHLIGHT
    // ============================================================

    private void RestoreAbilityHighlightAfterRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();


        if (highlightManager == null)
        {
            return;
        }


        if (UIManager.CurrentSelection == null)
        {
            return;
        }


        CanvasInfoManager canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();


        if (canvasInfoManager == null)
        {
            return;
        }


        if (!canvasInfoManager.HasSelectedAbility())
        {
            return;
        }


        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();


        if (ability == null)
        {
            return;
        }


        GameObject selectedUnit =
            UIManager.CurrentSelection.gameObject;


        if (selectedUnit == null)
        {
            return;
        }


        GridManager currentGridManager =
            gridManager;


        if (currentGridManager == null)
        {
            currentGridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (currentGridManager == null)
        {
            return;
        }


        List<Vector2Int> abilityTiles =
            ability.GetRangeTiles(
                currentGridManager,
                selectedUnit
            );


        if (abilityTiles == null)
        {
            return;
        }


        highlightManager.SetCurrentAbility(
            ability
        );


        highlightManager.ShowAbilityTiles(
            abilityTiles,
            selectedUnit,
            false
        );


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "ABILITY GRID RESTORED AFTER ROTATION.\n" +
                "Ability: " +
                ability.name +
                "\nSelected Unit: " +
                selectedUnit.name +
                "\nAbility Tiles: " +
                abilityTiles.Count +
                "\nRotation: " +
                currentRotation,
                this
            );
        }
    }


    // ============================================================
    // EXTERNAL UNITS
    // ============================================================

    private void RotateExternalUnits(
        float angle
    )
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
    }


    // ============================================================
    // INSTANT ROTATION
    // ============================================================

    private void RotateEverythingInstant(
        int rotation
    )
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
        int rotation
    )
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


    public bool IsRotationCurrentlyBlocked()
    {
        return IsRotationBlocked();
    }


    // ============================================================
    // DEBUG
    // ============================================================

    [ContextMenu("Debug Board State")]
    private void DebugBoardState()
    {
        CalculateGridCenter();


        bool enemyTurn =
            roundManager != null &&
            roundManager.IsEnemyTurn();


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
            "\nUnits Moving: " +
            UnitMoveBrain.IsAnyUnitMoving +
            "\nMoving Unit Count: " +
            UnitMoveBrain.GetMovingUnitCount() +
            "\nEnemy Turn: " +
            enemyTurn +
            "\nRotation Blocked: " +
            IsRotationBlocked() +
            "\nCenter: " +
            rotationCenter +
            "\nExternal Units: " +
            externalUnits.Count +
            "\nSelected Unit Before Rotation: " +
            (
                selectedUnitBeforeRotation != null
                    ? selectedUnitBeforeRotation.name
                    : "NULL"
            ) +
            "\nRestore Unit Range: " +
            restoreUnitRangeAfterRotation +
            "\nRestore Ability Highlight: " +
            restoreAbilityHighlightAfterRotation,
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