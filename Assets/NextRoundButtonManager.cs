using UnityEngine;
using UnityEngine.EventSystems;

public class NextRoundButtonManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover GameObject")]
    [SerializeField] private GameObject hoverObject;

    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 10f;

    [Header("Button Transform")]
    [SerializeField] private RectTransform buttonTransform;

    [Header("Breath Pulse")]
    [SerializeField] private float idlePulseScale = 1.015f;
    [SerializeField] private float idlePulseSpeed = 1.8f;

    [SerializeField] private float hoverPulseScale = 1.04f;
    [SerializeField] private float hoverPulseSpeed = 2.5f;

    [Header("Sprite Movement")]
    [SerializeField] private RectTransform leftSprite;
    [SerializeField] private RectTransform rightSprite;

    [SerializeField] private float moveX = 20f;
    [SerializeField] private float moveY = 15f;
    [SerializeField] private float moveSpeed = 8f;

    private Vector3 originalScale;

    private Vector2 leftOriginalPosition;
    private Vector2 rightOriginalPosition;

    private float originalAlpha;

    // 0 = normal position
    // 1 = fully hovered position
    private float hoverProgress;

    private bool isHovering;

    private void Awake()
    {
        if (buttonTransform == null)
            buttonTransform = transform as RectTransform;

        if (buttonTransform != null)
            originalScale = buttonTransform.localScale;

        // Store initial sprite positions
        if (leftSprite != null)
            leftOriginalPosition = leftSprite.anchoredPosition;

        if (rightSprite != null)
            rightOriginalPosition = rightSprite.anchoredPosition;

        // Store initial CanvasGroup alpha
        if (canvasGroup != null)
            originalAlpha = canvasGroup.alpha;

        if (hoverObject != null)
            hoverObject.SetActive(false);
    }

    private void Update()
    {
        UpdateHoverProgress();
        UpdateButtonPulse();
        UpdateSpriteMovement();
        UpdateCanvasGroup();
    }

    private void UpdateHoverProgress()
    {
        float targetProgress = isHovering ? 1f : 0f;

        hoverProgress = Mathf.MoveTowards(
            hoverProgress,
            targetProgress,
            Time.deltaTime * moveSpeed
        );
    }

    private void UpdateButtonPulse()
    {
        if (buttonTransform == null)
            return;

        float pulseScale = isHovering
            ? hoverPulseScale
            : idlePulseScale;

        float pulseSpeed = isHovering
            ? hoverPulseSpeed
            : idlePulseSpeed;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float scale = Mathf.Lerp(
            1f,
            pulseScale,
            pulse
        );

        Vector3 targetScale = originalScale * scale;

        buttonTransform.localScale = Vector3.Lerp(
            buttonTransform.localScale,
            targetScale,
            Time.deltaTime * 10f
        );
    }

    private void UpdateSpriteMovement()
    {
        // Left sprite:
        // Initial position -> LEFT + UP
        if (leftSprite != null)
        {
            Vector2 leftTarget = leftOriginalPosition +
                                 new Vector2(-moveX, moveY) * hoverProgress;

            leftSprite.anchoredPosition = Vector2.Lerp(
                leftOriginalPosition,
                leftTarget,
                1f
            );
        }

        // Right sprite:
        // Initial position -> RIGHT + UP
        if (rightSprite != null)
        {
            Vector2 rightTarget = rightOriginalPosition +
                                  new Vector2(moveX, moveY) * hoverProgress;

            rightSprite.anchoredPosition = Vector2.Lerp(
                rightOriginalPosition,
                rightTarget,
                1f
            );
        }
    }

    private void UpdateCanvasGroup()
    {
        if (canvasGroup == null)
            return;

        // Original alpha -> 1 on hover
        // 1 -> original alpha on exit
        float targetAlpha = Mathf.Lerp(
            originalAlpha,
            1f,
            hoverProgress
        );

        canvasGroup.alpha = targetAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        if (hoverObject != null)
            hoverObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // Do NOT immediately disable the hover object.
        // This allows the reverse animation to finish.
        if (hoverObject != null)
            StartCoroutine(DisableHoverObjectAfterAnimation());
    }

    private System.Collections.IEnumerator DisableHoverObjectAfterAnimation()
    {
        // Wait until the reverse animation reaches the beginning
        while (hoverProgress > 0f)
        {
            yield return null;
        }

        if (hoverObject != null && !isHovering)
            hoverObject.SetActive(false);
    }
}
