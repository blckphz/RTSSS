
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardViewController : MonoBehaviour
{
    public static BoardViewController Instance { get; private set; }

    [Header("Board")]
    [SerializeField] private Transform boardTransform;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private int gridWidth = 11;
    [SerializeField] private int gridHeight = 11;
    [SerializeField] private float cellSize = 1f;

    [Header("Input")]
    [SerializeField] private InputActionReference rotateLeftAction;
    [SerializeField] private InputActionReference rotateRightAction;

    [Header("Rotation")]
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private bool blockInputDuringRotation = true;
    [SerializeField] private bool rotateUnits = true;

    [Header("Movement / Round Rotation Lock")]
    [SerializeField] private bool preventRotationWhileUnitsMove = true;
    [SerializeField] private bool preventRotationDuringEnemyTurn = true;
    [SerializeField] private RoundManager roundManager;

    [Header("Start")]
    [SerializeField] private int startingRotation = 0;

    [Header("Rotation Selection")]
    [SerializeField] private bool deselectUnitWhenRotating = true;

    [Header("Selection Debug")]
    [SerializeField] private bool selectionDebugLogs = true;

    private int currentRotation;
    private bool isRotating;
    private Coroutine rotationCoroutine;
    private Vector3 rotationCenter;

    private bool restoreAbilityHighlightAfterRotation;

    private readonly List<Transform> externalUnits =
        new List<Transform>();

    private GameObject selectedUnitBeforeRotation;
    private bool restoreUnitRangeAfterRotation;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
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
            gridManager = FindFirstObjectByType<GridManager>();

        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>();

        currentRotation = NormalizeRotation(startingRotation);

        CalculateGridCenter();
        FindExternalUnits();

        if (currentRotation != 0)
            RotateEverythingInstant(currentRotation);
    }

    private void OnEnable()
    {
        if (rotateLeftAction != null)
        {
            rotateLeftAction.action.performed += OnRotateLeft;
            rotateLeftAction.action.Enable();
        }

        if (rotateRightAction != null)
        {
            rotateRightAction.action.performed += OnRotateRight;
            rotateRightAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (rotateLeftAction != null)
        {
            rotateLeftAction.action.performed -= OnRotateLeft;
            rotateLeftAction.action.Disable();
        }

        if (rotateRightAction != null)
        {
            rotateRightAction.action.performed -= OnRotateRight;
            rotateRightAction.action.Disable();
        }
    }

    // ============================================================
    // ROTATION LOCK
    // ============================================================

    private bool IsRotationBlocked()
    {
        if (preventRotationWhileUnitsMove &&
            UnitMoveBrain.IsAnyUnitMoving)
            return true;

        if (preventRotationDuringEnemyTurn &&
            roundManager != null &&
            roundManager.IsEnemyTurn())
            return true;

        return false;
    }

    // ============================================================
    // INPUT
    // ============================================================

    private void OnRotateLeft(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (blockInputDuringRotation && isRotating)
            return;

        if (IsRotationBlocked())
            return;

        RotateLeft();
    }

    private void OnRotateRight(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (blockInputDuringRotation && isRotating)
            return;

        if (IsRotationBlocked())
            return;

        RotateRight();
    }

    // ============================================================
    // PUBLIC ROTATION
    // ============================================================

    public void RotateLeft()
    {
        RotateTo(currentRotation + 90);
    }

    public void RotateRight()
    {
        RotateTo(currentRotation - 90);
    }

    public void RotateTo(int targetRotation)
    {
        if (boardTransform == null)
            return;

        if (IsRotationBlocked())
            return;

        if (blockInputDuringRotation && isRotating)
            return;

        targetRotation = NormalizeRotation(targetRotation);

        if (targetRotation == currentRotation)
            return;

        CaptureSelectionStateBeforeRotation();

        if (deselectUnitWhenRotating)
            HideHighlightVisualsBeforeRotation();

        if (rotationCoroutine != null)
            StopCoroutine(rotationCoroutine);

        rotationCoroutine =
            StartCoroutine(RotateBoardCoroutine(targetRotation));
    }

    // ============================================================
    // HOLOGRAM ROTATION
    // ============================================================

    private void SetEnemyHologramRotationState(bool rotating)
    {
        HologramManager hologramManager =
            FindFirstObjectByType<HologramManager>();

        if (hologramManager == null)
            return;

        if (rotating)
            hologramManager.OnBoardRotationStarted();
        else
            hologramManager.OnBoardRotationFinished();
    }

    // ============================================================
    // CAPTURE SELECTION
    // ============================================================

    private void CaptureSelectionStateBeforeRotation()
    {
        restoreAbilityHighlightAfterRotation = false;
        restoreUnitRangeAfterRotation = false;
        selectedUnitBeforeRotation = null;

        HoverInfoTrigger selection = UIManager.CurrentSelection;

        if (selection == null)
        {
            if (selectionDebugLogs)
            {
                Debug.Log(
                    "[Selection Debug] No object selected before rotation."
                );
            }

            return;
        }

        AttackUnit attackUnit = selection.GetAttackUnit();

        bool isValidCharacter =
            attackUnit != null &&
            !attackUnit.IsDead() &&
            selection.GetCharacterData() != null;

        if (selectionDebugLogs)
        {
            Debug.Log(
                "[Selection Debug] Selection before rotation: " +
                selection.gameObject.name +
                "\nHas AttackUnit: " + (attackUnit != null) +
                "\nHas CharacterData: " +
                (selection.GetCharacterData() != null) +
                "\nIs Dead: " +
                (attackUnit != null && attackUnit.IsDead()) +
                "\nValid Character: " + isValidCharacter,
                selection
            );
        }

        if (!isValidCharacter)
        {
            if (selectionDebugLogs)
            {
                Debug.LogWarning(
                    "[Selection Debug] Selection is NOT a valid character. " +
                    "Restoration will be skipped.",
                    selection
                );
            }

            return;
        }

        selectedUnitBeforeRotation = selection.gameObject;
        restoreUnitRangeAfterRotation = true;

        CanvasInfoManager canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();

        if (canvasInfoManager == null)
            return;

        if (!canvasInfoManager.HasSelectedAbility())
            return;

        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();

        if (ability == null)
            return;

        restoreAbilityHighlightAfterRotation = true;
    }

    // ============================================================
    // HIDE HIGHLIGHTS
    // ============================================================

    private void HideHighlightVisualsBeforeRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();

        if (highlightManager == null)
            return;

        highlightManager.ClearVisualsForBoardRotation();
    }

    // ============================================================
    // ROTATION COROUTINE
    // ============================================================

    private IEnumerator RotateBoardCoroutine(int targetRotation)
    {
        isRotating = true;

        SetEnemyHologramRotationState(true);

        CalculateGridCenter();
        FindExternalUnits();

        float totalAngle =
            Mathf.DeltaAngle(currentRotation, targetRotation);

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / rotationDuration);

            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            float currentAngle =
                totalAngle * smoothProgress;

            float previousProgress =
                Mathf.Clamp01(
                    (elapsed - Time.deltaTime) / rotationDuration
                );

            float previousSmoothProgress =
                Mathf.SmoothStep(0f, 1f, previousProgress);

            float previousAngle =
                totalAngle * previousSmoothProgress;

            float frameAngle =
                currentAngle - previousAngle;

            boardTransform.RotateAround(
                rotationCenter,
                Vector3.forward,
                frameAngle
            );

            if (rotateUnits)
                RotateExternalUnits(frameAngle);

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
                RotateExternalUnits(finalCorrection);
        }

        currentRotation = targetRotation;

        isRotating = false;
        rotationCoroutine = null;

        yield return null;
        yield return new WaitForEndOfFrame();

        Physics.SyncTransforms();

        yield return StartCoroutine(
            RefreshHighlightsAfterRotation()
        );

        if (restoreUnitRangeAfterRotation)
        {
            yield return StartCoroutine(
                RestoreSelectedUnitRangeAfterRotation()
            );
        }

        if (restoreAbilityHighlightAfterRotation)
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            RestoreAbilityHighlightAfterRotation();
        }

        restoreAbilityHighlightAfterRotation = false;
        restoreUnitRangeAfterRotation = false;
        selectedUnitBeforeRotation = null;

        SetEnemyHologramRotationState(false);
    }

    // ============================================================
    // REFRESH HIGHLIGHTS
    // ============================================================

    private IEnumerator RefreshHighlightsAfterRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();

        if (highlightManager == null)
            yield break;

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
            if (selectionDebugLogs)
            {
                Debug.LogWarning(
                    "[Selection Debug] No saved character. " +
                    "Movement range restoration skipped."
                );
            }

            yield break;
        }

        HoverInfoTrigger trigger =
            selectedUnitBeforeRotation.GetComponent<HoverInfoTrigger>();

        if (trigger == null ||
            trigger.GetAttackUnit() == null ||
            trigger.GetCharacterData() == null ||
            trigger.GetAttackUnit().IsDead())
        {
            if (selectionDebugLogs)
            {
                Debug.LogWarning(
                    "[Selection Debug] Saved object is no longer a " +
                    "valid character. Restoration cancelled."
                );
            }

            yield break;
        }

        if (UIManager.CurrentSelection == null)
        {
            if (selectionDebugLogs)
            {
                Debug.LogWarning(
                    "[Selection Debug] Current selection is NULL after " +
                    "rotation. Movement range restoration cancelled."
                );
            }

            yield break;
        }

        GameObject currentSelectedObject =
            UIManager.CurrentSelection.gameObject;

        if (currentSelectedObject != selectedUnitBeforeRotation)
        {
            if (selectionDebugLogs)
            {
                Debug.LogWarning(
                    "[Selection Debug] Selection changed during rotation." +
                    "\nSaved: " + selectedUnitBeforeRotation.name +
                    "\nCurrent: " + currentSelectedObject.name +
                    "\nMovement range restoration cancelled."
                );
            }

            yield break;
        }

        GridHighlightBrain highlightBrain =
            FindFirstObjectByType<GridHighlightBrain>();

        if (highlightBrain != null)
        {
            highlightBrain.RefreshActiveHighlights();

            if (selectionDebugLogs)
            {
                Debug.Log(
                    "[Selection Debug] SUCCESS: Movement range restored " +
                    "for " + currentSelectedObject.name + "."
                );
            }
        }
        else if (selectionDebugLogs)
        {
            Debug.LogWarning(
                "[Selection Debug] GridHighlightBrain not found. " +
                "Movement range could not be restored."
            );
        }

        yield return null;
        yield return new WaitForEndOfFrame();
    }

    // ============================================================
    // RESTORE ABILITY HIGHLIGHT
    // ============================================================

    private void RestoreAbilityHighlightAfterRotation()
    {
        GridHighlightManager highlightManager =
            FindFirstObjectByType<GridHighlightManager>();

        if (highlightManager == null)
            return;

        if (UIManager.CurrentSelection == null)
            return;

        CanvasInfoManager canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();

        if (canvasInfoManager == null ||
            !canvasInfoManager.HasSelectedAbility())
            return;

        AbilitySO ability =
            canvasInfoManager.GetSelectedAbility();

        if (ability == null)
            return;

        GameObject selectedUnit =
            UIManager.CurrentSelection.gameObject;

        if (selectedUnit == null)
            return;

        GridManager currentGridManager = gridManager;

        if (currentGridManager == null)
            currentGridManager =
                FindFirstObjectByType<GridManager>();

        if (currentGridManager == null)
            return;

        List<Vector2Int> abilityTiles =
            ability.GetRangeTiles(
                currentGridManager,
                selectedUnit
            );

        if (abilityTiles == null)
            return;

        highlightManager.SetCurrentAbility(ability);

        highlightManager.ShowAbilityTiles(
            abilityTiles,
            selectedUnit,
            false
        );
    }

    // ============================================================
    // EXTERNAL UNITS
    // ============================================================

    private void RotateExternalUnits(float angle)
    {
        for (int i = externalUnits.Count - 1; i >= 0; i--)
        {
            Transform unit = externalUnits[i];

            if (unit == null)
            {
                externalUnits.RemoveAt(i);
                continue;
            }

            if (unit.IsChildOf(boardTransform))
                continue;

            Vector3 offset =
                unit.position - rotationCenter;

            Quaternion rotation =
                Quaternion.AngleAxis(angle, Vector3.forward);

            offset = rotation * offset;

            unit.position = rotationCenter + offset;
        }
    }

    // ============================================================
    // FIND UNITS
    // ============================================================

    private void FindExternalUnits()
    {
        externalUnits.Clear();

        if (!rotateUnits)
            return;

        AttackUnit[] units =
            FindObjectsByType<AttackUnit>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null)
                continue;

            Transform unit = units[i].transform;

            if (unit.IsChildOf(boardTransform))
                continue;

            externalUnits.Add(unit);
        }
    }

    // ============================================================
    // INSTANT ROTATION
    // ============================================================

    private void RotateEverythingInstant(int rotation)
    {
        if (rotation == 0)
            return;

        CalculateGridCenter();

        float angle = Mathf.DeltaAngle(0f, rotation);

        boardTransform.RotateAround(
            rotationCenter,
            Vector3.forward,
            angle
        );

        if (rotateUnits)
        {
            FindExternalUnits();

            for (int i = 0; i < externalUnits.Count; i++)
            {
                Transform unit = externalUnits[i];

                if (unit == null)
                    continue;

                if (unit.IsChildOf(boardTransform))
                    continue;

                Vector3 offset =
                    unit.position - rotationCenter;

                offset =
                    Quaternion.AngleAxis(
                        angle,
                        Vector3.forward
                    ) * offset;

                unit.position = rotationCenter + offset;
            }
        }
    }

    // ============================================================
    // GRID CENTER
    // ============================================================

    private void CalculateGridCenter()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            Grid grid = gridManager.GetGrid();

            if (grid != null)
            {
                rotationCenter =
                    gridManager.GridToWorldPosition(Vector2Int.zero);

                return;
            }
        }

        float centerX =
            (gridWidth - 1) * cellSize * 0.5f;

        float centerY =
            (gridHeight - 1) * cellSize * 0.5f;

        Vector3 localCenter =
            new Vector3(centerX, centerY, 0f);

        rotationCenter =
            boardTransform.TransformPoint(localCenter);
    }

    // ============================================================
    // NORMALIZE
    // ============================================================

    private int NormalizeRotation(int rotation)
    {
        rotation %= 360;

        if (rotation < 0)
            rotation += 360;

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
    // GIZMOS
    // ============================================================

    private void OnDrawGizmos()
    {
        if (boardTransform == null)
            return;

        CalculateGridCenter();

        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(rotationCenter, 0.2f);

        Gizmos.DrawLine(
            rotationCenter + Vector3.left * 0.5f,
            rotationCenter + Vector3.right * 0.5f
        );

        Gizmos.DrawLine(
            rotationCenter + Vector3.down * 0.5f,
            rotationCenter + Vector3.up * 0.5f
        );
    }
}
