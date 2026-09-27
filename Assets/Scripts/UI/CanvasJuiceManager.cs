using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CanvasJuiceManager : MonoBehaviour
{
    public static CanvasJuiceManager Instance;

    [Header("Canvas / Tooltip")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform tooltip;
    [SerializeField] private Camera uiCamera;

    [Header("Tooltip Settings")]
    [SerializeField] private float hoverTransparency = 1f;
    [SerializeField] private float fadeDuration = 0.15f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTarget;

    [Tooltip("Position used as the fallback/default camera position.")]
    [SerializeField] private Transform normalPosition;

    [Tooltip("Position used while an ability is selected in inspect mode.")]
    [SerializeField] private Transform abilityCameraPosition;

    [Tooltip("Offset from the selected unit.")]
    [SerializeField] private Vector3 unitCameraOffset;

    [SerializeField] private float cameraMoveDuration = 0.5f;

    [Header("Camera Lock")]
    [SerializeField] private bool inspectMode = false;

    [Tooltip("Input used to toggle camera lock.")]
    [SerializeField] private InputActionReference cameraLockAction;

    [Header("Free Camera")]
    [SerializeField] private cameraMoveScript freeCamera;

    [Header("Unit Following")]
    [SerializeField] private float unitFollowSpeed = 8f;

    [Header("Managers")]
    [SerializeField] private CanvasInfoManager canvasInfoManager;

    // =========================================================
    // SELECTION
    // =========================================================

    private Transform currentSelectedUnit;
    private Transform currentFollowTarget;

    // =========================================================
    // SAVED FREE CAMERA POSITION
    // =========================================================

    private Vector3 savedFreeCameraPosition;
    private bool hasSavedFreeCameraPosition = false;

    // =========================================================
    // COROUTINES
    // =========================================================

    private Coroutine fadeCoroutine;
    private Coroutine cameraMoveCoroutine;
    private Coroutine followCoroutine;
    private Coroutine normalCameraCoroutine;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasInfoManager == null)
        {
            canvasInfoManager =
                FindFirstObjectByType<CanvasInfoManager>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (inspectMode)
        {
            DisableFreeCamera();
        }
        else
        {
            EnableFreeCamera();
        }

        Debug.Log(
            "[CanvasJuiceManager] Awake. Inspect mode = " +
            inspectMode
        );
    }

    private void OnEnable()
    {
        if (cameraLockAction != null)
        {
            cameraLockAction.action.performed +=
                OnCameraLockPerformed;

            cameraLockAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (cameraLockAction != null)
        {
            cameraLockAction.action.performed -=
                OnCameraLockPerformed;

            cameraLockAction.action.Disable();
        }
    }

    private void Update()
    {
        UpdateTooltipPosition();
    }

    // =========================================================
    // CAMERA LOCK INPUT
    // =========================================================

    private void OnCameraLockPerformed(
        InputAction.CallbackContext context)
    {
        Debug.Log(
            "[CanvasJuiceManager] Camera lock input."
        );

        ToggleInspectMode();
    }

    public void ToggleInspectMode()
    {
        Debug.Log(
            "[CanvasJuiceManager] ToggleInspectMode. " +
            "Current = " +
            inspectMode +
            " -> " +
            !inspectMode
        );

        SetInspectMode(!inspectMode);
    }

    public void SetInspectMode(bool enabled)
    {
        Debug.Log(
            "[CanvasJuiceManager] SetInspectMode(" +
            enabled +
            ")"
        );

        // =====================================================
        // TURNING LOCK OFF
        // =====================================================

        if (!enabled)
        {
            inspectMode = false;

            StopFollowingUnit();

            if (normalCameraCoroutine != null)
            {
                StopCoroutine(normalCameraCoroutine);
                normalCameraCoroutine = null;
            }

            MoveCameraBackToSavedFreePosition();

            return;
        }

        // =====================================================
        // TURNING LOCK ON
        // =====================================================

        if (
            cameraTarget != null &&
            !inspectMode
        )
        {
            savedFreeCameraPosition =
                cameraTarget.position;

            hasSavedFreeCameraPosition = true;

            Debug.Log(
                "[CanvasJuiceManager] Saved free camera position: " +
                savedFreeCameraPosition
            );
        }

        inspectMode = true;

        if (normalCameraCoroutine != null)
        {
            StopCoroutine(normalCameraCoroutine);
            normalCameraCoroutine = null;
        }

        // =====================================================
        // ABILITY HAS PRIORITY
        // =====================================================

        if (HasSelectedAbility())
        {
            Debug.Log(
                "[CanvasJuiceManager] Lock ON -> ability camera."
            );

            MoveCameraToAbilityPosition();
            return;
        }

        // =====================================================
        // UNIT SECOND
        // =====================================================

        if (currentSelectedUnit != null)
        {
            Debug.Log(
                "[CanvasJuiceManager] Lock ON -> selected unit."
            );

            MoveCameraToUnit(
                currentSelectedUnit
            );

            return;
        }

        // =====================================================
        // NOTHING SELECTED
        // =====================================================

        DisableFreeCamera();
    }

    public bool IsInspectMode()
    {
        return inspectMode;
    }

    // =========================================================
    // ABILITY CHECK
    // =========================================================

    private bool HasSelectedAbility()
    {
        bool selected =
            canvasInfoManager != null &&
            canvasInfoManager.HasSelectedAbility();

        return selected;
    }

    // =========================================================
    // UNIT SELECTION
    // =========================================================

    public void ShowUnitInfo(Transform unit)
    {
        if (unit == null)
        {
            return;
        }

        currentSelectedUnit = unit;

        Debug.Log(
            "[CanvasJuiceManager] ShowUnitInfo: " +
            unit.name +
            " | Inspect = " +
            inspectMode +
            " | Ability = " +
            HasSelectedAbility()
        );

        FadeCanvasTo(hoverTransparency);

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        if (HasSelectedAbility())
        {
            MoveCameraToAbilityPosition();
        }
        else
        {
            MoveCameraToUnit(unit);
        }
    }

    public void MoveCameraToUnit(
        Transform unit)
    {
        if (
            unit == null ||
            cameraTarget == null
        )
        {
            return;
        }

        currentSelectedUnit = unit;

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        if (HasSelectedAbility())
        {
            Debug.Log(
                "[CanvasJuiceManager] " +
                "MoveCameraToUnit blocked by ability camera."
            );

            MoveCameraToAbilityPosition();
            return;
        }

        DisableFreeCamera();

        StopFollowingUnit();

        currentFollowTarget = unit;

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        Vector3 targetPosition =
            unit.position +
            unitCameraOffset;

        targetPosition.z =
            cameraTarget.position.z;

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraCoroutine(
                    targetPosition
                )
            );

        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }

        followCoroutine =
            StartCoroutine(
                FollowUnitCoroutine(unit)
            );
    }

    private IEnumerator FollowUnitCoroutine(
        Transform unit)
    {
        while (
            currentFollowTarget == unit &&
            currentSelectedUnit == unit &&
            unit != null &&
            cameraTarget != null &&
            inspectMode &&
            !HasSelectedAbility()
        )
        {
            Vector3 targetPosition =
                unit.position +
                unitCameraOffset;

            targetPosition.z =
                cameraTarget.position.z;

            cameraTarget.position =
                Vector3.Lerp(
                    cameraTarget.position,
                    targetPosition,
                    unitFollowSpeed *
                    Time.deltaTime
                );

            yield return null;
        }

        followCoroutine = null;
    }

    // =========================================================
    // STOP FOLLOWING
    // =========================================================

    public void StopFollowingUnit()
    {
        currentFollowTarget = null;

        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }

    public void ClearSelectedUnit()
    {
        currentSelectedUnit = null;

        StopFollowingUnit();
    }

    // =========================================================
    // ABILITY CAMERA
    // =========================================================

    public void MoveCameraToAbilityPosition()
    {
        Debug.Log(
            "[CanvasJuiceManager] " +
            "MoveCameraToAbilityPosition()"
        );

        if (!inspectMode)
        {
            Debug.Log(
                "[CanvasJuiceManager] " +
                "Ability camera skipped: inspect mode OFF."
            );

            EnableFreeCamera();
            return;
        }

        if (
            abilityCameraPosition == null ||
            cameraTarget == null
        )
        {
            Debug.Log(
                "[CanvasJuiceManager] " +
                "Ability camera skipped: missing transform."
            );

            return;
        }

        StopFollowingUnit();

        DisableFreeCamera();

        Vector3 targetPosition =
            abilityCameraPosition.position;

        targetPosition.z =
            cameraTarget.position.z;

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraCoroutine(
                    targetPosition
                )
            );
    }

    // =========================================================
    // RETURN TO SAVED FREE CAMERA POSITION
    // =========================================================

    private void MoveCameraBackToSavedFreePosition()
    {
        if (cameraTarget == null)
        {
            return;
        }

        StopFollowingUnit();

        DisableFreeCamera();

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        Vector3 targetPosition;

        if (hasSavedFreeCameraPosition)
        {
            targetPosition =
                savedFreeCameraPosition;

            targetPosition.z =
                cameraTarget.position.z;
        }
        else if (normalPosition != null)
        {
            targetPosition =
                normalPosition.position;

            targetPosition.z =
                cameraTarget.position.z;
        }
        else
        {
            targetPosition =
                cameraTarget.position;
        }

        Debug.Log(
            "[CanvasJuiceManager] Returning camera to: " +
            targetPosition
        );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraCoroutine(
                    targetPosition
                )
            );

        if (normalCameraCoroutine != null)
        {
            StopCoroutine(normalCameraCoroutine);
        }

        normalCameraCoroutine =
            StartCoroutine(
                EnableFreeCameraAfterMove()
            );
    }

    // =========================================================
    // NORMAL CAMERA
    // =========================================================

    public void MoveCameraToNormalPosition()
    {
        if (cameraTarget == null)
        {
            return;
        }

        StopFollowingUnit();

        DisableFreeCamera();

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        Vector3 targetPosition;

        if (normalPosition != null)
        {
            targetPosition =
                normalPosition.position;

            targetPosition.z =
                cameraTarget.position.z;
        }
        else
        {
            targetPosition =
                cameraTarget.position;
        }

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraCoroutine(
                    targetPosition
                )
            );

        if (normalCameraCoroutine != null)
        {
            StopCoroutine(normalCameraCoroutine);
        }

        normalCameraCoroutine =
            StartCoroutine(
                EnableFreeCameraAfterMove()
            );
    }

    private IEnumerator EnableFreeCameraAfterMove()
    {
        yield return new WaitForSeconds(
            cameraMoveDuration
        );

        normalCameraCoroutine = null;

        if (!inspectMode)
        {
            EnableFreeCamera();
        }
    }

    // =========================================================
    // FREE CAMERA
    // =========================================================

    public void EnableFreeCamera()
    {
        if (freeCamera != null)
        {
            freeCamera.EnableFreeMode();
        }
    }

    public void DisableFreeCamera()
    {
        if (freeCamera != null)
        {
            freeCamera.DisableFreeMode();
        }
    }

    // =========================================================
    // DESELECT / HIDE
    // =========================================================

    public void HideHoverInfo()
    {
        Debug.Log(
            "[CanvasJuiceManager] HideHoverInfo()"
        );

        FadeCanvasTo(0f);

        currentSelectedUnit = null;

        StopFollowingUnit();

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        inspectMode = false;

        MoveCameraBackToSavedFreePosition();
    }

    // =========================================================
    // CAMERA MOVEMENT
    // =========================================================

    private IEnumerator MoveCameraCoroutine(
        Vector3 targetPosition)
    {
        if (cameraTarget == null)
        {
            yield break;
        }

        Vector3 startPosition =
            cameraTarget.position;

        float elapsed = 0f;

        while (
            elapsed <
            cameraMoveDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float t =
                elapsed /
                cameraMoveDuration;

            t =
                t *
                t *
                (3f - 2f * t);

            cameraTarget.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        cameraTarget.position =
            targetPosition;

        cameraMoveCoroutine = null;
    }

    // =========================================================
    // CANVAS FADE
    // =========================================================

    private void FadeCanvasTo(
        float targetAlpha)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine =
            StartCoroutine(
                FadeCanvasCoroutine(
                    targetAlpha
                )
            );
    }

    private IEnumerator FadeCanvasCoroutine(
        float targetAlpha)
    {
        float startAlpha =
            canvasGroup.alpha;

        float elapsed = 0f;

        while (
            elapsed <
            fadeDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float t =
                elapsed /
                fadeDuration;

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        canvasGroup.interactable =
            targetAlpha > 0f;

        canvasGroup.blocksRaycasts =
            targetAlpha > 0f;

        fadeCoroutine = null;
    }

    // =========================================================
    // TOOLTIP
    // =========================================================

    private void UpdateTooltipPosition()
    {
        if (
            tooltip == null ||
            Mouse.current == null
        )
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        tooltip.position =
            mousePosition;
    }
}