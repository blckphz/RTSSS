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
    // ENEMY FOLLOW
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
                hoverInfoCanvas.GetComponent<
                    RectTransform
                >();

            hoverInfoCanvas.alpha = 0f;

            hoverInfoCanvas.interactable =
                false;

            hoverInfoCanvas.blocksRaycasts =
                false;
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
        // Stop following the previous unit.
        // --------------------------------------------------------

        StopFollowingUnit();


        // --------------------------------------------------------
        // Set new follow target.
        // --------------------------------------------------------

        currentFollowTarget = unit;


        // --------------------------------------------------------
        // Calculate initial camera position.
        // --------------------------------------------------------

        Vector3 targetPosition =
            unit.position +
            unitCameraOffset;

        Quaternion targetRotation =
            cameraTarget.rotation;


        // --------------------------------------------------------
        // Smoothly move camera to the enemy first.
        // --------------------------------------------------------

        if (cameraCoroutine != null)
        {
            StopCoroutine(
                cameraCoroutine
            );
        }

        if (!gameObject.activeInHierarchy)
        {
            cameraTarget.position = targetPosition;
            cameraTarget.rotation = targetRotation;
            return;
        }

        cameraCoroutine =
            StartCoroutine(
                MoveCameraTargetCoroutine(
                    targetPosition,
                    targetRotation
                )
            );


        // --------------------------------------------------------
        // Start continuous follow.
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


        // Wait for the initial camera movement to finish.
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


            // Smooth camera movement.
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
        // Stop following the current unit.
        StopFollowingUnit();


        if (cameraTarget == null)
            return;

        if (abilityCameraPosition == null)
            return;


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
        // Stop following the current enemy.
        StopFollowingUnit();


        if (cameraTarget == null)
            return;

        if (normalPosition == null)
            return;


        MoveCameraTargetTo(
            normalPosition.position,
            normalPosition.rotation
        );
    }


    // ============================================================
    // HIDE / DESELECT
    // ============================================================

    public void HideHoverInfo()
    {
        FadeCanvasTo(0f);

        MoveCameraToNormalPosition();
    }


    // ============================================================
    // CAMERA MOVEMENT
    // ============================================================

    private void MoveCameraTargetTo(
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        if (cameraTarget == null)
            return;


        if (cameraCoroutine != null)
        {
            StopCoroutine(
                cameraCoroutine
            );
        }

        if (!gameObject.activeInHierarchy)
        {
            cameraTarget.position = targetPosition;
            cameraTarget.rotation = targetRotation;
            cameraCoroutine = null;
            return;
        }


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
        Quaternion targetRotation)
    {
        if (cameraTarget == null)
            yield break;


        Vector3 startPosition =
            cameraTarget.position;

        Quaternion startRotation =
            cameraTarget.rotation;

        float elapsed = 0f;


        // Prevent division by zero.
        if (cameraMoveDuration <= 0f)
        {
            cameraTarget.position =
                targetPosition;

            cameraTarget.rotation =
                targetRotation;

            cameraCoroutine = null;

            yield break;
        }


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
        float targetAlpha)
    {
        if (hoverInfoCanvas == null)
            return;


        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }

        if (!gameObject.activeInHierarchy)
        {
            hoverInfoCanvas.alpha = targetAlpha;
            fadeCoroutine = null;
            return;
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
            hoverInfoCanvas.alpha;

        float elapsed = 0f;


        if (fadeDuration <= 0f)
        {
            hoverInfoCanvas.alpha =
                targetAlpha;

            fadeCoroutine = null;

            yield break;
        }


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


        hoverInfoCanvas.alpha =
            targetAlpha;

        fadeCoroutine = null;
    }
}