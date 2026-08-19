using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasJuiceManager : MonoBehaviour
{
    public static CanvasJuiceManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup hoverInfoCanvas;

    [Header("Tooltip Settings")]
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private bool followMouse = true;
    [SerializeField] private Vector3 mouseOffset = new Vector3(15f, -15f, 0f);

    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)]
    private float hoverTransparency = 1f;

    [Header("Camera Target")]
    [Tooltip("The GameObject that your Cinemachine Camera follows.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Camera Positions")]
    [Tooltip("Position of the camera target when NOT hovering.")]
    [SerializeField] private Transform normalPosition;

    [Tooltip("Position of the camera target while hovering.")]
    [SerializeField] private Transform hoverPosition;

    [SerializeField] private float cameraMoveDuration = 0.25f;

    private Coroutine fadeCoroutine;
    private Coroutine cameraCoroutine;
    private RectTransform canvasRectTransform;

    // =============================================================
    // UNITY LIFECYCLE
    // =============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hoverInfoCanvas == null)
        {
            Debug.LogError("[CanvasJuiceManager] Hover Info Canvas is NOT assigned!", this);
            return;
        }

        canvasRectTransform = hoverInfoCanvas.GetComponent<RectTransform>();

        // Start completely transparent
        hoverInfoCanvas.alpha = 0f;

        ValidateReferences();
    }

    private void Update()
    {
        if (followMouse && hoverInfoCanvas != null && hoverInfoCanvas.alpha > 0.01f)
        {
            UpdateCanvasPosition();
        }
    }

    // =============================================================
    // PUBLIC CONTROL METHODS
    // =============================================================

    public void ShowHoverInfo()
    {
        if (hoverInfoCanvas == null) return;

        UpdateCanvasPosition();

        // Only transparency is changed here.
        // 1 = fully visible
        // 0.5 = 50% visible
        // 0 = invisible
        StartFade(hoverTransparency);

        MoveCameraTargetTo(hoverPosition);
    }

    public void HideHoverInfo()
    {
        if (hoverInfoCanvas == null) return;

        // Fade completely transparent.
        StartFade(0f);

        MoveCameraTargetTo(normalPosition);
    }

    // =============================================================
    // PRIVATE HELPERS
    // =============================================================

    private void UpdateCanvasPosition()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        //canvasRectTransform.position = mousePos + (Vector2)mouseOffset;
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCanvasCoroutine(targetAlpha));
    }

    private void MoveCameraTargetTo(Transform target)
    {
        if (cameraTarget == null || target == null) return;

        if (cameraCoroutine != null)
        {
            StopCoroutine(cameraCoroutine);
        }

        cameraCoroutine = StartCoroutine(MoveCameraTargetCoroutine(target));
    }

    // =============================================================
    // COROUTINES
    // =============================================================

    private IEnumerator FadeCanvasCoroutine(float targetAlpha)
    {
        float startAlpha = hoverInfoCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);

            hoverInfoCanvas.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            yield return null;
        }

        hoverInfoCanvas.alpha = targetAlpha;
    }

    private IEnumerator MoveCameraTargetCoroutine(Transform target)
    {
        Vector3 startPosition = cameraTarget.position;
        Quaternion startRotation = cameraTarget.rotation;

        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / cameraMoveDuration)
            );

            cameraTarget.position = Vector3.Lerp(
                startPosition,
                target.position,
                progress
            );

            cameraTarget.rotation = Quaternion.Slerp(
                startRotation,
                target.rotation,
                progress
            );

            yield return null;
        }

        cameraTarget.position = target.position;
        cameraTarget.rotation = target.rotation;
    }

    // =============================================================
    // VALIDATION
    // =============================================================

    private void ValidateReferences()
    {
        if (cameraTarget == null)
        {
            Debug.LogError(
                "[CanvasJuiceManager] Camera Target is NOT assigned!",
                this
            );
        }

        if (normalPosition == null)
        {
            Debug.LogError(
                "[CanvasJuiceManager] Normal Position is NOT assigned!",
                this
            );
        }

        if (hoverPosition == null)
        {
            Debug.LogError(
                "[CanvasJuiceManager] Hover Position is NOT assigned!",
                this
            );
        }
    }
}