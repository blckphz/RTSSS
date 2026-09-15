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
    [Tooltip(
        "Prevents board rotation while any UnitMoveBrain is moving."
    )]
    [SerializeField]
    private bool preventRotationWhileUnitsMove = true;

    [Tooltip(
        "Prevents board rotation during the entire enemy turn."
    )]
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

    private bool restoreAbilityHighlightAfterRotation;

    private readonly List<Transform> externalUnits =
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
        // --------------------------------------------------------
        // UNIT MOVEMENT LOCK
        // --------------------------------------------------------

        if (
            preventRotationWhileUnitsMove &&
            UnitMoveBrain.IsAnyUnitMoving
        )
        {
            return true;
        }


        // --------------------------------------------------------
        // ENEMY TURN LOCK
        // --------------------------------------------------------

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


        // ========================================================
        // FINAL ROTATION SAFETY LOCK
        // ========================================================

        if (IsRotationBlocked())
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "Rotation request rejected.\n" +
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


        CaptureAbilityStateBeforeRotation();


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
    // ABILITY STATE BEFORE ROTATION
    // ============================================================

    private void CaptureAbilityStateBeforeRotation()
    {
        restoreAbilityHighlightAfterRotation = false;


        CanvasInfoManager canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();


        if (canvasInfoManager == null)
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[BoardViewController] " +
                    "CanvasInfoManager not found while capturing " +
                    "ability state before rotation.",
                    this
                );
            }

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


        if (UIManager.CurrentSelection == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "Ability is selected, but no unit is currently selected. " +
                    "Ability grid will not be restored.",
                    this
                );
            }

            return;
        }


        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();


        if (ability == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[BoardViewController] " +
                    "CanvasInfoManager reports a selected ability, " +
                    "but GetSelectedAbility() returned NULL.",
                    this
                );
            }

            return;
        }


        restoreAbilityHighlightAfterRotation = true;


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Ability highlight marked for restoration after rotation.\n" +
                "Ability: " +
                ability.name +
                "\nUnit: " +
                UIManager.CurrentSelection.name,
                this
            );
        }
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


        highlightManager.ClearAllHighlights();


        if (debugLogs)
        {
            Debug.Log(
                "[BoardViewController] " +
                "Unit highlights cleared before board rotation.",
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


        RefreshHighlightsAfterRotation();


        if (restoreAbilityHighlightAfterRotation)
        {
            yield return null;

            yield return new WaitForEndOfFrame();

            RestoreAbilityHighlightAfterRotation();
        }


        restoreAbilityHighlightAfterRotation = false;
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


        highlightManager.SetCurrentAbility(
            ability
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