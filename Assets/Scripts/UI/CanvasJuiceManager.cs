using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CanvasJuiceManager : MonoBehaviour
{
    public static CanvasJuiceManager Instance;


    // =========================================================
    // CANVAS / INFO BOX
    // =========================================================

    [Header("Canvas / Info Box")]

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Tooltip("The info box GameObject. Its Scene position is used as the final position.")]
    [SerializeField]
    private GameObject infoBox;

    [SerializeField]
    private RectTransform tooltip;

    [SerializeField]
    private Camera uiCamera;


    // =========================================================
    // INFO BOX ANIMATION
    // =========================================================

    [Header("Info Box Animation")]

    [SerializeField]
    private float hoverTransparency = 1f;

    [SerializeField]
    private float fadeDuration = 0.15f;

    [SerializeField]
    private float infoBoxMoveDuration = 0.3f;

    [Tooltip("World-space offset from the cached Scene position when hidden.")]
    [SerializeField]
    private Vector3 hiddenOffset =
        new Vector3(-5f, 0f, 0f);

    [Tooltip("Scale of the info box while hidden. 0.85 = 85% of normal size.")]
    [SerializeField, Range(0.1f, 1f)]
    private float hiddenScale = 0.85f;

    [SerializeField]
    private float scaleAnimationDuration = 0.3f;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Camera")]

    [SerializeField]
    private Transform cameraTarget;

    [Tooltip("Position used as the fallback/default camera position.")]
    [SerializeField]
    private Transform normalPosition;

    [Tooltip("Position used while an ability is selected in inspect mode.")]
    [SerializeField]
    private Transform abilityCameraPosition;

    [Tooltip("Offset from the selected unit.")]
    [SerializeField]
    private Vector3 unitCameraOffset;

    [SerializeField]
    private float cameraMoveDuration = 0.5f;


    // =========================================================
    // CAMERA LOCK
    // =========================================================

    [Header("Camera Lock")]

    [SerializeField]
    private bool inspectMode = false;

    [Tooltip("Input used to toggle camera lock.")]
    [SerializeField]
    private InputActionReference cameraLockAction;


    // =========================================================
    // FREE CAMERA
    // =========================================================

    [Header("Free Camera")]

    [SerializeField]
    private cameraMoveScript freeCamera;


    // =========================================================
    // UNIT FOLLOWING
    // =========================================================

    [Header("Unit Following")]

    [SerializeField]
    private float unitFollowSpeed = 8f;


    // =========================================================
    // MANAGERS
    // =========================================================

    [Header("Managers")]

    [SerializeField]
    private CanvasInfoManager canvasInfoManager;


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
    // INFO BOX POSITION / SCALE
    // =========================================================

    private Vector3 cachedInfoBoxPosition;

    private Vector3 cachedInfoBoxScale;

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


        // -----------------------------------------------------
        // FIND CANVAS INFO MANAGER
        // -----------------------------------------------------

        if (canvasInfoManager == null)
        {
            canvasInfoManager =
                FindFirstObjectByType<CanvasInfoManager>();
        }


        // -----------------------------------------------------
        // CACHE INFO BOX
        // -----------------------------------------------------

        CacheInfoBoxPosition();


        // -----------------------------------------------------
        // START CANVAS HIDDEN
        // -----------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;

            canvasGroup.interactable = false;

            canvasGroup.blocksRaycasts = false;
        }


        // -----------------------------------------------------
        // FREE CAMERA
        // -----------------------------------------------------

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
    // INFO BOX POSITION / SCALE
    // =========================================================

    private void CacheInfoBoxPosition()
    {
        if (infoBox == null)
        {
            return;
        }


        // -----------------------------------------------------
        // CACHE SCENE POSITION
        // -----------------------------------------------------

        cachedInfoBoxPosition =
            infoBox.transform.position;


        // -----------------------------------------------------
        // CACHE SCENE SCALE
        // -----------------------------------------------------

        cachedInfoBoxScale =
            infoBox.transform.localScale;


        hasCachedInfoBoxPosition = true;


        // -----------------------------------------------------
        // START HIDDEN
        // -----------------------------------------------------

        infoBox.transform.position =
            cachedInfoBoxPosition +
            hiddenOffset;

        infoBox.transform.localScale =
            cachedInfoBoxScale *
            hiddenScale;
    }


    private Vector3 GetHiddenInfoBoxPosition()
    {
        return
            cachedInfoBoxPosition +
            hiddenOffset;
    }


    private Vector3 GetHiddenInfoBoxScale()
    {
        return
            cachedInfoBoxScale *
            hiddenScale;
    }


    // =========================================================
    // SHOW INFO BOX
    // =========================================================

    private void ShowInfoBox()
    {
        if (
            infoBox == null ||
            !hasCachedInfoBoxPosition
        )
        {
            FadeCanvasTo(
                hoverTransparency
            );

            return;
        }


        if (infoBoxMoveCoroutine != null)
        {
            StopCoroutine(
                infoBoxMoveCoroutine
            );
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


        Vector3 startScale =
            infoBox.transform.localScale;

        Vector3 targetScale =
            cachedInfoBoxScale;


        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f;


        float elapsed = 0f;


        // -----------------------------------------------------
        // ENABLE CANVAS INTERACTION
        // -----------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;

            canvasGroup.blocksRaycasts = true;
        }


        // -----------------------------------------------------
        // USE THE LONGEST ANIMATION TIME
        // -----------------------------------------------------

        float duration =
            Mathf.Max(
                infoBoxMoveDuration,
                scaleAnimationDuration,
                fadeDuration
            );


        // -----------------------------------------------------
        // ANIMATE
        // -----------------------------------------------------

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            // Smooth ease-in/ease-out.
            float eased =
                t *
                t *
                (3f - 2f * t);


            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            if (infoBox != null)
            {
                infoBox.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        eased
                    );


                // ---------------------------------------------
                // SCALE
                // ---------------------------------------------

                infoBox.transform.localScale =
                    Vector3.Lerp(
                        startScale,
                        targetScale,
                        eased
                    );
            }


            // -------------------------------------------------
            // FADE
            // -------------------------------------------------

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


        // -----------------------------------------------------
        // FORCE FINAL VALUES
        // -----------------------------------------------------

        if (infoBox != null)
        {
            infoBox.transform.position =
                cachedInfoBoxPosition;

            infoBox.transform.localScale =
                cachedInfoBoxScale;
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
        if (
            infoBox == null ||
            !hasCachedInfoBoxPosition
        )
        {
            FadeCanvasTo(0f);

            return;
        }


        if (infoBoxMoveCoroutine != null)
        {
            StopCoroutine(
                infoBoxMoveCoroutine
            );
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


        Vector3 startScale =
            infoBox.transform.localScale;

        Vector3 targetScale =
            GetHiddenInfoBoxScale();


        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : hoverTransparency;


        float elapsed = 0f;


        // -----------------------------------------------------
        // USE THE LONGEST ANIMATION TIME
        // -----------------------------------------------------

        float duration =
            Mathf.Max(
                infoBoxMoveDuration,
                scaleAnimationDuration,
                fadeDuration
            );


        // -----------------------------------------------------
        // ANIMATE
        // -----------------------------------------------------

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            float eased =
                t *
                t *
                (3f - 2f * t);


            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            if (infoBox != null)
            {
                infoBox.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        eased
                    );


                // ---------------------------------------------
                // SCALE
                // ---------------------------------------------

                infoBox.transform.localScale =
                    Vector3.Lerp(
                        startScale,
                        targetScale,
                        eased
                    );
            }


            // -------------------------------------------------
            // FADE
            // -------------------------------------------------

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


        // -----------------------------------------------------
        // FORCE FINAL VALUES
        // -----------------------------------------------------

        if (infoBox != null)
        {
            infoBox.transform.position =
                targetPosition;

            infoBox.transform.localScale =
                targetScale;
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
        SetInspectMode(
            !inspectMode
        );
    }


    public void SetInspectMode(
        bool enabled
    )
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
                StopCoroutine(
                    normalCameraCoroutine
                );

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
            StopCoroutine(
                normalCameraCoroutine
            );

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

    public void ShowUnitInfo(
        Transform unit
    )
    {
        if (unit == null)
        {
            return;
        }


        currentSelectedUnit = unit;


        // -----------------------------------------------------
        // SHOW INFO BOX
        // -----------------------------------------------------

        ShowInfoBox();


        // -----------------------------------------------------
        // NORMAL CAMERA
        // -----------------------------------------------------

        if (!inspectMode)
        {
            EnableFreeCamera();

            return;
        }


        // -----------------------------------------------------
        // ABILITY HAS PRIORITY
        // -----------------------------------------------------

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
        Transform unit
    )
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


        // -----------------------------------------------------
        // ABILITY HAS PRIORITY
        // -----------------------------------------------------

        if (HasSelectedAbility())
        {
            MoveCameraToAbilityPosition();

            return;
        }


        // -----------------------------------------------------
        // DISABLE FREE CAMERA
        // -----------------------------------------------------

        DisableFreeCamera();


        StopFollowingUnit();

        currentFollowTarget = unit;


        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(
                cameraMoveCoroutine
            );

            cameraMoveCoroutine = null;
        }


        // -----------------------------------------------------
        // CALCULATE CAMERA POSITION
        // -----------------------------------------------------

        Vector3 targetPosition =
            unit.position +
            unitCameraOffset;

        targetPosition.z =
            cameraTarget.position.z;


        // -----------------------------------------------------
        // MOVE CAMERA
        // -----------------------------------------------------

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraCoroutine(
                    targetPosition
                )
            );


        // -----------------------------------------------------
        // FOLLOW UNIT
        // -----------------------------------------------------

        followCoroutine =
            StartCoroutine(
                FollowUnitCoroutine(unit)
            );
    }


    private IEnumerator FollowUnitCoroutine(
        Transform unit
    )
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
            StopCoroutine(
                followCoroutine
            );

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
            StopCoroutine(
                cameraMoveCoroutine
            );

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
            StopCoroutine(
                cameraMoveCoroutine
            );

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
            StopCoroutine(
                normalCameraCoroutine
            );
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
            StopCoroutine(
                cameraMoveCoroutine
            );

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
            StopCoroutine(
                normalCameraCoroutine
            );
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
        // -----------------------------------------------------
        // HIDE INFO BOX
        // -----------------------------------------------------

        HideInfoBox();


        // -----------------------------------------------------
        // CLEAR UNIT
        // -----------------------------------------------------

        currentSelectedUnit = null;

        StopFollowingUnit();


        // =====================================================
        // KEEP INSPECT MODE ACTIVE
        // =====================================================

        if (inspectMode)
        {
            DisableFreeCamera();


            // -------------------------------------------------
            // IF ABILITY IS SELECTED
            // -------------------------------------------------

            if (HasSelectedAbility())
            {
                MoveCameraToAbilityPosition();
            }


            // -------------------------------------------------
            // OTHERWISE:
            // Stay exactly where the camera currently is.
            // Inspect mode remains active.
            // -------------------------------------------------

            return;
        }


        // =====================================================
        // NORMAL MODE
        // =====================================================

        EnableFreeCamera();
    }


    // =========================================================
    // CAMERA MOVEMENT
    // =========================================================

    private IEnumerator MoveCameraCoroutine(
        Vector3 targetPosition
    )
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
            elapsed += Time.deltaTime;


            float t =
                elapsed /
                cameraMoveDuration;


            // Smooth ease-in/ease-out.
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
        float targetAlpha
    )
    {
        if (canvasGroup == null)
        {
            return;
        }


        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }


        fadeCoroutine =
            StartCoroutine(
                FadeCanvasCoroutine(
                    targetAlpha
                )
            );
    }


    private IEnumerator FadeCanvasCoroutine(
        float targetAlpha
    )
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
