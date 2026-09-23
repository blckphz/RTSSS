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
    //
    // When the player turns LOCK ON, we save the exact camera
    // position from free mode.
    //
    // When LOCK is turned OFF, the camera returns to this
    // position instead of normalPosition.
    //

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

        // Start in the correct camera mode.
        if (inspectMode)
        {
            DisableFreeCamera();
        }
        else
        {
            EnableFreeCamera();
        }
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
        ToggleInspectMode();
    }

    public void ToggleInspectMode()
    {
        SetInspectMode(!inspectMode);
    }

    public void SetInspectMode(bool enabled)
    {
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

            // Return to the exact free-camera position that
            // existed when lock was activated.
            MoveCameraBackToSavedFreePosition();

            return;
        }

        // =====================================================
        // TURNING LOCK ON
        // =====================================================

        // IMPORTANT:
        //
        // Save the CURRENT free camera position BEFORE moving
        // to the selected unit or ability.
        //
        // This only happens when going from FREE -> LOCK.
        //
        if (
            cameraTarget != null &&
            !inspectMode
        )
        {
            savedFreeCameraPosition =
                cameraTarget.position;

            hasSavedFreeCameraPosition = true;
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
            MoveCameraToAbilityPosition();
            return;
        }

        // =====================================================
        // UNIT SECOND
        // =====================================================

        if (currentSelectedUnit != null)
        {
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
        return
            canvasInfoManager != null &&
            canvasInfoManager.HasSelectedAbility();
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

        FadeCanvasTo(hoverTransparency);

        // =====================================================
        // FREE MODE
        // =====================================================
        //
        // Selecting a unit MUST NOT move the camera.
        //

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        // =====================================================
        // LOCKED MODE
        // =====================================================

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

        // =====================================================
        // FREE MODE
        // =====================================================
        //
        // Never move camera because of unit selection while
        // lock is OFF.
        //

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        // =====================================================
        // ABILITY HAS PRIORITY
        // =====================================================

        if (HasSelectedAbility())
        {
            MoveCameraToAbilityPosition();
            return;
        }

        // =====================================================
        // FOLLOW UNIT
        // =====================================================

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
        // IMPORTANT:
        //
        // Do NOT clear currentSelectedUnit here.
        //
        // currentSelectedUnit = what is selected.
        // currentFollowTarget = what camera is following.
        //
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
        // =====================================================
        // FREE MODE PROTECTION
        // =====================================================
        //
        // Selecting an ability while free MUST NOT move camera.
        //

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        if (
            abilityCameraPosition == null ||
            cameraTarget == null
        )
        {
            return;
        }

        // Ability camera takes priority over unit following.
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

        // =====================================================
        // USE SAVED FREE POSITION
        // =====================================================

        if (hasSavedFreeCameraPosition)
        {
            targetPosition =
                savedFreeCameraPosition;

            // Preserve the current camera Z.
            targetPosition.z =
                cameraTarget.position.z;
        }

        // =====================================================
        // FALLBACK
        // =====================================================

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

        // =====================================================
        // MOVE TO SAVED POSITION
        // =====================================================

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
    //
    // Kept as a public method in case another system needs to
    // explicitly move to normalPosition.
    //
    // Camera LOCK OFF uses MoveCameraBackToSavedFreePosition()
    // instead.
    //

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
        FadeCanvasTo(0f);

        currentSelectedUnit = null;

        StopFollowingUnit();

        // =====================================================
        // FREE MODE
        // =====================================================
        //
        // Deselecting while free does NOT move camera.
        //

        if (!inspectMode)
        {
            EnableFreeCamera();
            return;
        }

        // =====================================================
        // LOCKED MODE
        // =====================================================
        //
        // Deselecting while locked exits lock and returns to
        // the saved free position.
        //

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

            // SmoothStep easing.
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