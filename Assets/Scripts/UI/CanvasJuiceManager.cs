using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasJuiceManager : MonoBehaviour
{
    public static CanvasJuiceManager Instance;


    // ============================================================
    // UI REFERENCES
    // ============================================================

    [Header("UI References")]
    [SerializeField] private CanvasGroup hoverInfoCanvas;


    // ============================================================
    // TOOLTIP SETTINGS
    // ============================================================

    [Header("Tooltip Settings")]
    [SerializeField] private float fadeDuration = 0.15f;

    [SerializeField] private bool followMouse = true;

    [SerializeField]
    private Vector3 mouseOffset =
        new Vector3(15f, -15f, 0f);


    // ============================================================
    // TRANSPARENCY
    // ============================================================

    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)]
    private float hoverTransparency = 1f;


    // ============================================================
    // CAMERA TARGET
    // ============================================================

    [Header("Camera Target")]
    [Tooltip(
        "The GameObject that your Cinemachine Camera follows."
    )]
    [SerializeField] private Transform cameraTarget;

    [Tooltip(
        "Position the camera target returns to when no unit is selected."
    )]
    [SerializeField] private Transform normalPosition;

    [Tooltip(
        "Position used when an ability is selected."
    )]
    [SerializeField] private Transform abilityCameraPosition;

    [Tooltip(
        "Optional offset from the selected unit."
    )]
    [SerializeField]
    private Vector3 unitCameraOffset =
        Vector3.zero;

    [SerializeField]
    private float cameraMoveDuration = 0.25f;


    // ============================================================
    // FREE CAMERA
    // ============================================================

    [Header("Free Camera")]
    [Tooltip(
        "Camera movement script responsible for mouse edge scrolling."
    )]
    [SerializeField] private cameraMoveScript freeCamera;


    // ============================================================
    // UNIT CAMERA FOLLOW
    // ============================================================

    [Header("Unit Camera Follow")]
    [Tooltip(
        "How quickly the camera follows a moving unit."
    )]
    [SerializeField]
    private float unitFollowSpeed = 10f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private RectTransform hoverRectTransform;

    private Coroutine fadeCoroutine;
    private Coroutine cameraCoroutine;
    private Coroutine followCoroutine;

    private Transform currentFollowTarget;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // SINGLETON
        // --------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        // --------------------------------------------------------
        // CANVAS
        // --------------------------------------------------------

        if (hoverInfoCanvas == null)
        {
            Debug.LogWarning(
                "CanvasJuiceManager: Hover Info Canvas is not assigned."
            );
        }
        else
        {
            hoverRectTransform =
                hoverInfoCanvas.GetComponent<RectTransform>();

            hoverInfoCanvas.alpha = 0f;

            hoverInfoCanvas.interactable = false;

            hoverInfoCanvas.blocksRaycasts = false;
        }


        // --------------------------------------------------------
        // CAMERA
        // --------------------------------------------------------

        if (cameraTarget == null)
        {
            Debug.LogWarning(
                "CanvasJuiceManager: Camera Target is not assigned."
            );
        }

        if (normalPosition == null)
        {
            Debug.LogWarning(
                "CanvasJuiceManager: Normal Position is not assigned."
            );
        }

        if (abilityCameraPosition == null)
        {
            Debug.LogWarning(
                "CanvasJuiceManager: Ability Camera Position is not assigned."
            );
        }

        if (freeCamera == null)
        {
            Debug.LogWarning(
                "CanvasJuiceManager: Free Camera is not assigned."
            );
        }


        // --------------------------------------------------------
        // START IN FREE CAMERA MODE
        // --------------------------------------------------------

        if (freeCamera != null)
        {
            freeCamera.EnableFreeMode();
        }
    }


    private void Update()
    {
        UpdateTooltipPosition();
    }


    // ============================================================
    // TOOLTIP
    // ============================================================

    private void UpdateTooltipPosition()
    {
        if (!followMouse)
            return;

        if (hoverInfoCanvas == null)
            return;

        if (hoverInfoCanvas.alpha <= 0.01f)
            return;

        if (hoverRectTransform == null)
            return;


        Vector2 mousePosition =
            Input.mousePosition;


        hoverRectTransform.position =
            mousePosition +
            (Vector2)mouseOffset;
    }


    // ============================================================
    // UNIT SELECTION
    // ============================================================

    public void ShowUnitInfo(Transform unit)
    {
        if (unit == null)
            return;


        // Disable free camera while a unit is selected.
        DisableFreeCamera();


        FadeCanvasTo(
            hoverTransparency
        );


        MoveCameraToUnit(unit);
    }


    // ============================================================
    // MOVE CAMERA TO UNIT
    // ============================================================

    public void MoveCameraToUnit(Transform unit)
    {
        if (unit == null)
            return;

        if (cameraTarget == null)
            return;


        // --------------------------------------------------------
        // DISABLE FREE CAMERA
        // --------------------------------------------------------

        DisableFreeCamera();


        // --------------------------------------------------------
        // STOP FOLLOWING PREVIOUS UNIT
        // --------------------------------------------------------

        StopFollowingUnit();


        // --------------------------------------------------------
        // SET NEW FOLLOW TARGET
        // --------------------------------------------------------

        currentFollowTarget = unit;


        // --------------------------------------------------------
        // CALCULATE TARGET POSITION
        // --------------------------------------------------------

        Vector3 targetPosition =
            unit.position +
            unitCameraOffset;


        Quaternion targetRotation =
            cameraTarget.rotation;


        // --------------------------------------------------------
        // STOP PREVIOUS CAMERA MOVEMENT
        // --------------------------------------------------------

        if (cameraCoroutine != null)
        {
            StopCoroutine(
                cameraCoroutine
            );

            cameraCoroutine = null;
        }


        // --------------------------------------------------------
        // IMMEDIATE MOVE IF OBJECT IS INACTIVE
        // --------------------------------------------------------

        if (!gameObject.activeInHierarchy)
        {
            cameraTarget.position =
                targetPosition;

            cameraTarget.rotation =
                targetRotation;

            return;
        }


        // --------------------------------------------------------
        // SMOOTH MOVE TO UNIT
        // --------------------------------------------------------

        cameraCoroutine =
            StartCoroutine(
                MoveCameraTargetCoroutine(
                    targetPosition,
                    targetRotation
                )
            );


        // --------------------------------------------------------
        // START CONTINUOUS FOLLOW
        // --------------------------------------------------------

        if (gameObject.activeInHierarchy)
        {
            followCoroutine =
                StartCoroutine(
                    FollowUnitCoroutine(unit)
                );
        }
    }


    // ============================================================
    // FOLLOW UNIT
    // ============================================================

    private IEnumerator FollowUnitCoroutine(
        Transform unit
    )
    {
        if (unit == null)
            yield break;


        // --------------------------------------------------------
        // WAIT FOR INITIAL CAMERA MOVEMENT
        // --------------------------------------------------------

        if (cameraCoroutine != null)
        {
            yield return cameraCoroutine;
        }


        cameraCoroutine = null;


        // --------------------------------------------------------
        // CONTINUOUS FOLLOW
        // --------------------------------------------------------

        while (
            currentFollowTarget == unit &&
            unit != null &&
            cameraTarget != null
        )
        {
            Vector3 desiredPosition =
                unit.position +
                unitCameraOffset;


            float smoothAmount =
                1f -
                Mathf.Exp(
                    -unitFollowSpeed *
                    Time.deltaTime
                );


            cameraTarget.position =
                Vector3.Lerp(
                    cameraTarget.position,
                    desiredPosition,
                    smoothAmount
                );


            yield return null;
        }


        followCoroutine = null;
    }


    // ============================================================
    // STOP FOLLOWING
    // ============================================================

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


    // ============================================================
    // ABILITY CAMERA
    // ============================================================

    public void MoveCameraToAbilityPosition()
    {
        // --------------------------------------------------------
        // STOP UNIT FOLLOW
        // --------------------------------------------------------

        StopFollowingUnit();


        // --------------------------------------------------------
        // DISABLE FREE CAMERA
        // --------------------------------------------------------

        DisableFreeCamera();


        // --------------------------------------------------------
        // VALIDATE
        // --------------------------------------------------------

        if (cameraTarget == null)
            return;

        if (abilityCameraPosition == null)
            return;


        // --------------------------------------------------------
        // MOVE TO ABILITY POSITION
        // --------------------------------------------------------

        MoveCameraTargetTo(
            abilityCameraPosition.position,
            abilityCameraPosition.rotation
        );
    }


    // ============================================================
    // NORMAL CAMERA
    // ============================================================

    public void MoveCameraToNormalPosition()
    {
        // --------------------------------------------------------
        // STOP UNIT FOLLOW
        // --------------------------------------------------------

        StopFollowingUnit();


        if (cameraTarget == null)
            return;

        if (normalPosition == null)
            return;


        // --------------------------------------------------------
        // MOVE TO NORMAL POSITION
        // --------------------------------------------------------

        MoveCameraTargetTo(
            normalPosition.position,
            normalPosition.rotation
        );


        // --------------------------------------------------------
        // ENABLE FREE CAMERA
        // --------------------------------------------------------

        EnableFreeCamera();
    }


    // ============================================================
    // ENABLE FREE CAMERA
    // ============================================================

    public void EnableFreeCamera()
    {
        StopFollowingUnit();


        if (freeCamera != null)
        {
            freeCamera.EnableFreeMode();
        }
    }


    // ============================================================
    // DISABLE FREE CAMERA
    // ============================================================

    public void DisableFreeCamera()
    {
        if (freeCamera != null)
        {
            freeCamera.DisableFreeMode();
        }
    }


    // ============================================================
    // HIDE / DESELECT
    // ============================================================

    public void HideHoverInfo()
    {
        FadeCanvasTo(0f);


        // --------------------------------------------------------
        // Return to normal position and then free camera.
        // --------------------------------------------------------

        MoveCameraToNormalPosition();
    }


    // ============================================================
    // CAMERA MOVEMENT
    // ============================================================

    private void MoveCameraTargetTo(
        Vector3 targetPosition,
        Quaternion targetRotation
    )
    {
        if (cameraTarget == null)
            return;


        // --------------------------------------------------------
        // STOP PREVIOUS MOVEMENT
        // --------------------------------------------------------

        if (cameraCoroutine != null)
        {
            StopCoroutine(
                cameraCoroutine
            );

            cameraCoroutine = null;
        }


        // --------------------------------------------------------
        // IMMEDIATE MOVE IF INACTIVE
        // --------------------------------------------------------

        if (!gameObject.activeInHierarchy)
        {
            cameraTarget.position =
                targetPosition;

            cameraTarget.rotation =
                targetRotation;

            cameraCoroutine = null;

            return;
        }


        // --------------------------------------------------------
        // SMOOTH MOVEMENT
        // --------------------------------------------------------

        cameraCoroutine =
            StartCoroutine(
                MoveCameraTargetCoroutine(
                    targetPosition,
                    targetRotation
                )
            );
    }


    private IEnumerator MoveCameraTargetCoroutine(
        Vector3 targetPosition,
        Quaternion targetRotation
    )
    {
        if (cameraTarget == null)
            yield break;


        Vector3 startPosition =
            cameraTarget.position;

        Quaternion startRotation =
            cameraTarget.rotation;

        float elapsed = 0f;


        // --------------------------------------------------------
        // INSTANT MOVEMENT
        // --------------------------------------------------------

        if (cameraMoveDuration <= 0f)
        {
            cameraTarget.position =
                targetPosition;

            cameraTarget.rotation =
                targetRotation;

            cameraCoroutine = null;

            yield break;
        }


        // --------------------------------------------------------
        // SMOOTH MOVEMENT
        // --------------------------------------------------------

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    cameraMoveDuration
                );


            // SmoothStep
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


            cameraTarget.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );


            yield return null;
        }


        // --------------------------------------------------------
        // FINAL POSITION
        // --------------------------------------------------------

        cameraTarget.position =
            targetPosition;

        cameraTarget.rotation =
            targetRotation;

        cameraCoroutine = null;
    }


    // ============================================================
    // CANVAS FADE
    // ============================================================

    private void FadeCanvasTo(
        float targetAlpha
    )
    {
        if (hoverInfoCanvas == null)
            return;


        // --------------------------------------------------------
        // STOP PREVIOUS FADE
        // --------------------------------------------------------

        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }


        // --------------------------------------------------------
        // INSTANT FADE IF INACTIVE
        // --------------------------------------------------------

        if (!gameObject.activeInHierarchy)
        {
            hoverInfoCanvas.alpha =
                targetAlpha;

            fadeCoroutine = null;

            return;
        }


        // --------------------------------------------------------
        // START FADE
        // --------------------------------------------------------

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
            hoverInfoCanvas.alpha;

        float elapsed = 0f;


        // --------------------------------------------------------
        // INSTANT FADE
        // --------------------------------------------------------

        if (fadeDuration <= 0f)
        {
            hoverInfoCanvas.alpha =
                targetAlpha;

            fadeCoroutine = null;

            yield break;
        }


        // --------------------------------------------------------
        // SMOOTH FADE
        // --------------------------------------------------------

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );


            hoverInfoCanvas.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );


            yield return null;
        }


        // --------------------------------------------------------
        // FINAL ALPHA
        // --------------------------------------------------------

        hoverInfoCanvas.alpha =
            targetAlpha;

        fadeCoroutine = null;
    }
}