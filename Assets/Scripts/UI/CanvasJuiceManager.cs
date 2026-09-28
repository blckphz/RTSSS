using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CanvasJuiceManager : MonoBehaviour
{
    public static CanvasJuiceManager Instance;

    [Header("Canvas / Info Box")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("The info box GameObject. Its Scene position is used as the final position.")]
    [SerializeField] private GameObject infoBox;

    [SerializeField] private RectTransform tooltip;
    [SerializeField] private Camera uiCamera;

    [Header("Info Box Animation")]
    [SerializeField] private float hoverTransparency = 1f;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float infoBoxMoveDuration = 0.3f;

    [Tooltip("World-space offset from the cached Scene position when hidden.")]
    [SerializeField] private Vector3 hiddenOffset = new Vector3(-5f, 0f, 0f);

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
    // INFO BOX POSITION
    // =========================================================

    private Vector3 cachedInfoBoxPosition;
    private bool hasCachedInfoBoxPosition = false;

    // =========================================================
    // COROUTINES
    // =========================================================

    private Coroutine fadeCoroutine;
    private Coroutine infoBoxMoveCoroutine;
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

        CacheInfoBoxPosition();

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
    // INFO BOX POSITION
    // =========================================================

    private void CacheInfoBoxPosition()
    {
        if (infoBox == null)
            return;

        // Cache the exact world position set in the Scene.
        cachedInfoBoxPosition =
            infoBox.transform.position;

        hasCachedInfoBoxPosition = true;

        // Immediately move the object to its hidden position.
        infoBox.transform.position =
            cachedInfoBoxPosition + hiddenOffset;
    }

    private Vector3 GetHiddenInfoBoxPosition()
    {
        return cachedInfoBoxPosition + hiddenOffset;
    }

    // =========================================================
    // SHOW INFO BOX
    // =========================================================

    private void ShowInfoBox()
    {
        if (infoBox == null || !hasCachedInfoBoxPosition)
        {
            FadeCanvasTo(hoverTransparency);
            return;
        }

        if (infoBoxMoveCoroutine != null)
        {
            StopCoroutine(infoBoxMoveCoroutine);
        }

        infoBoxMoveCoroutine =
            StartCoroutine(
                ShowInfoBoxCoroutine()
            );
    }

    private IEnumerator ShowInfoBoxCoroutine()
    {
        Vector3 startPosition =
            infoBox.transform.position;

        Vector3 targetPosition =
            cachedInfoBoxPosition;

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f;

        float elapsed = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        while (elapsed < infoBoxMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / infoBoxMoveDuration
                );

            // Smooth ease-in/ease-out.
            float eased =
                t * t * (3f - 2f * t);

            if (infoBox != null)
            {
                infoBox.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        eased
                    );
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        hoverTransparency,
                        eased
                    );
            }

            yield return null;
        }

        // Make absolutely sure it ends at the exact Scene position.
        if (infoBox != null)
        {
            infoBox.transform.position =
                cachedInfoBoxPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                hoverTransparency;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        infoBoxMoveCoroutine = null;
    }

    // =========================================================
    // HIDE INFO BOX
    // =========================================================

    private void HideInfoBox()
    {
        if (infoBox == null || !hasCachedInfoBoxPosition)
        {
            FadeCanvasTo(0f);
            return;
        }

        if (infoBoxMoveCoroutine != null)
        {
            StopCoroutine(infoBoxMoveCoroutine);
        }

        infoBoxMoveCoroutine =
            StartCoroutine(
                HideInfoBoxCoroutine()
            );
    }

    private IEnumerator HideInfoBoxCoroutine()
    {
        Vector3 startPosition =
            infoBox.transform.position;

        Vector3 targetPosition =
            GetHiddenInfoBoxPosition();

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : hoverTransparency;

        float elapsed = 0f;

        while (elapsed < infoBoxMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / infoBoxMoveDuration
                );

            float eased =
                t * t * (3f - 2f * t);

            if (infoBox != null)
            {
                infoBox.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        eased
                    );
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        eased
                    );
            }

            yield return null;
        }

        if (infoBox != null)
        {
            infoBox.transform.position =
                targetPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        infoBoxMoveCoroutine = null;
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
            return;

        currentSelectedUnit = unit;

        ShowInfoBox();

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
            return;

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
            return;

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
        HideInfoBox();

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
            yield break;

        Vector3 startPosition =
            cameraTarget.position;

        float elapsed = 0f;

        while (
            elapsed <
            cameraMoveDuration
        )
        {
            elapsed += Time.deltaTime;

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
            return;

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
            elapsed += Time.deltaTime;

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