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
    // INTERNAL
    // ============================================================

    private RectTransform hoverRectTransform;

    private Coroutine fadeCoroutine;
    private Coroutine cameraCoroutine;


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


    public void MoveCameraToUnit(Transform unit)
    {
        if (unit == null)
            return;

        if (cameraTarget == null)
            return;

        Vector3 targetPosition =
            unit.position +
            unitCameraOffset;

        Quaternion targetRotation =
            cameraTarget.rotation;

        MoveCameraTargetTo(
            targetPosition,
            targetRotation
        );
    }


    // ============================================================
    // ABILITY CAMERA
    // ============================================================

    public void MoveCameraToAbilityPosition()
    {
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