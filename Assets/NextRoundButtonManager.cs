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

    [Header("Button Hover Size")]
    [SerializeField] private float hoverStartScale = 0.85f;
    [SerializeField] private float hoverGrowDuration = 0.25f;

    [Header("Breath Pulse")]
    [SerializeField] private float idlePulseScale = 1.015f;
    [SerializeField] private float idlePulseSpeed = 1.8f;

    [SerializeField] private float hoverPulseScale = 1.04f;
    [SerializeField] private float hoverPulseSpeed = 2.5f;

    [Header("Juice / Pop Settings")]
    [SerializeField] private float entryPunchScale = 1.08f;
    [SerializeField] private float punchDuration = 0.15f;

    [Header("Sprite Movement & Overshoot")]
    [SerializeField] private RectTransform leftSprite;
    [SerializeField] private RectTransform rightSprite;

    [SerializeField] private float moveX = 20f;
    [SerializeField] private float moveY = 15f;
    [SerializeField] private float moveSpeed = 8f;

    [Range(1f, 1.8f)]
    [SerializeField] private float overshootMultiplier = 1.35f;

    [Header("Stagger / Delay")]
    [Range(0f, 0.8f)]
    [SerializeField] private float secondSpriteDelay = 0.15f;

    [Header("Idle Lift")]
    [SerializeField] private float idleLiftAmount = 4f;

    [Header("Idle Side Movement")]
    [SerializeField] private float idleSideAmount = 3f;

    [SerializeField] private float idleLiftDuration = 0.25f;
    [SerializeField] private float idleLiftInterval = 0.5f;

    [Range(0f, 0.5f)]
    [SerializeField] private float idleSecondSpriteDelay = 0.1f;

    private Vector3 originalScale;

    private Vector2 leftOriginalPosition;
    private Vector2 rightOriginalPosition;

    private float originalAlpha;

    private float hoverProgress;
    private float secondHoverProgress;

    private bool isHovering;
    private bool leftSpriteGoesFirst = true;

    private float punchTimer;

    // Button hover size animation
    private float hoverSizeTimer;
    private bool hoverSizeAnimationActive;

    // Idle animation
    private float idleTimer;
    private bool idleAnimationRunning;

    // Random idle order
    private bool idleLeftSpriteGoesFirst = true;


    private void Awake()
    {
        if (buttonTransform == null)
            buttonTransform = transform as RectTransform;

        // Fetch the initial/original size of the button
        if (buttonTransform != null)
            originalScale = buttonTransform.localScale;

        if (leftSprite != null)
            leftOriginalPosition = leftSprite.anchoredPosition;

        if (rightSprite != null)
            rightOriginalPosition = rightSprite.anchoredPosition;

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

        if (punchTimer > 0f)
            punchTimer -= Time.deltaTime;
    }


    // =========================================================
    // HOVER PROGRESS
    // =========================================================

    private void UpdateHoverProgress()
    {
        float targetProgress = isHovering ? 1f : 0f;

        hoverProgress = Mathf.MoveTowards(
            hoverProgress,
            targetProgress,
            Time.deltaTime * moveSpeed
        );

        if (isHovering)
        {
            float adjustedProgress = Mathf.InverseLerp(
                secondSpriteDelay,
                1f,
                hoverProgress
            );

            secondHoverProgress = Mathf.MoveTowards(
                secondHoverProgress,
                adjustedProgress,
                Time.deltaTime * moveSpeed
            );
        }
        else
        {
            secondHoverProgress = Mathf.MoveTowards(
                secondHoverProgress,
                0f,
                Time.deltaTime * moveSpeed
            );
        }
    }


    // =========================================================
    // BUTTON PULSE
    // =========================================================

    private void UpdateButtonPulse()
    {
        if (buttonTransform == null)
            return;

        // -------------------------------------------------
        // HOVER ENTRY SIZE ANIMATION
        // -------------------------------------------------

        if (hoverSizeAnimationActive)
        {
            hoverSizeTimer += Time.deltaTime;

            float t = Mathf.Clamp01(
                hoverSizeTimer / hoverGrowDuration
            );

            // Smooth ease-out
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            float currentScale = Mathf.Lerp(
                hoverStartScale,
                1f,
                easedT
            );

            buttonTransform.localScale =
                originalScale * currentScale;

            if (t >= 1f)
            {
                hoverSizeAnimationActive = false;
            }

            return;
        }

        // -------------------------------------------------
        // INITIAL PUNCH
        // -------------------------------------------------

        if (punchTimer > 0f)
        {
            float punchProgress =
                1f - (punchTimer / punchDuration);

            float currentPunchScale = Mathf.Lerp(
                entryPunchScale,
                hoverPulseScale,
                punchProgress
            );

            buttonTransform.localScale =
                originalScale * currentPunchScale;

            return;
        }

        // -------------------------------------------------
        // NORMAL SCALE PULSE
        // -------------------------------------------------

        float pulseScale = isHovering
            ? hoverPulseScale
            : idlePulseScale;

        float pulseSpeed = isHovering
            ? hoverPulseSpeed
            : idlePulseSpeed;

        float pulse =
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float scale =
            Mathf.Lerp(1f, pulseScale, pulse);

        Vector3 targetScale =
            originalScale * scale;

        buttonTransform.localScale =
            Vector3.Lerp(
                buttonTransform.localScale,
                targetScale,
                Time.deltaTime * 12f
            );
    }


    // =========================================================
    // OVERSHOOT
    // =========================================================

    private float CalculateOvershoot(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.95f)
               * overshootMultiplier
               * (1f - t)
               + Mathf.Sin(t * Mathf.PI * 0.45f);
    }


    // =========================================================
    // SPRITE MOVEMENT
    // =========================================================

    private void UpdateSpriteMovement()
    {
        RectTransform firstSprite =
            leftSpriteGoesFirst ? leftSprite : rightSprite;

        RectTransform secondSprite =
            leftSpriteGoesFirst ? rightSprite : leftSprite;

        Vector2 firstOriginalPos =
            leftSpriteGoesFirst
                ? leftOriginalPosition
                : rightOriginalPosition;

        Vector2 secondOriginalPos =
            leftSpriteGoesFirst
                ? rightOriginalPosition
                : leftOriginalPosition;

        Vector2 firstMoveDir =
            leftSpriteGoesFirst
                ? new Vector2(-moveX, moveY)
                : new Vector2(moveX, moveY);

        Vector2 secondMoveDir =
            leftSpriteGoesFirst
                ? new Vector2(moveX, moveY)
                : new Vector2(-moveX, moveY);


        // -------------------------------------------------
        // INITIAL HOVER ANIMATION
        // -------------------------------------------------

        float firstProgress = isHovering
            ? CalculateOvershoot(hoverProgress)
            : hoverProgress;

        float secondProgress = isHovering
            ? CalculateOvershoot(secondHoverProgress)
            : secondHoverProgress;

        Vector2 firstTarget =
            firstOriginalPos +
            firstMoveDir * firstProgress;

        Vector2 secondTarget =
            secondOriginalPos +
            secondMoveDir * secondProgress;


        // -------------------------------------------------
        // IDLE LIFT + SIDE MOVEMENT
        // -------------------------------------------------

        if (isHovering && hoverProgress >= 0.99f)
        {
            UpdateIdleLift();

            Vector2 firstIdleOffset =
                GetIdleOffset(
                    idleLeftSpriteGoesFirst,
                    true,
                    firstSprite
                );

            Vector2 secondIdleOffset =
                GetIdleOffset(
                    idleLeftSpriteGoesFirst,
                    false,
                    secondSprite
                );

            firstTarget += firstIdleOffset;
            secondTarget += secondIdleOffset;
        }
        else
        {
            idleTimer = 0f;
            idleAnimationRunning = false;
        }


        // -------------------------------------------------
        // APPLY POSITIONS
        // -------------------------------------------------

        if (firstSprite != null)
            firstSprite.anchoredPosition = firstTarget;

        if (secondSprite != null)
            secondSprite.anchoredPosition = secondTarget;
    }


    // =========================================================
    // START IDLE ANIMATION
    // =========================================================

    private void UpdateIdleLift()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= idleLiftInterval)
        {
            idleTimer -= idleLiftInterval;

            // 50/50 chance which sprite goes first
            idleLeftSpriteGoesFirst =
                Random.value > 0.5f;

            idleAnimationRunning = true;
        }
    }


    // =========================================================
    // IDLE LIFT + SIDEWAYS MOVEMENT
    // =========================================================

    private Vector2 GetIdleOffset(
        bool leftGoesFirst,
        bool isFirstSprite,
        RectTransform sprite)
    {
        if (!idleAnimationRunning)
            return Vector2.zero;


        // Determine whether this sprite is the first
        // or second sprite in the idle sequence
        bool thisSpriteGoesFirst =
            isFirstSprite == leftGoesFirst;


        float spriteDelay =
            thisSpriteGoesFirst
                ? 0f
                : idleSecondSpriteDelay;


        float t =
            (idleTimer - spriteDelay) /
            idleLiftDuration;


        // Not currently animating
        if (t <= 0f || t >= 1f)
            return Vector2.zero;


        // -------------------------------------------------
        // SMOOTH MOVEMENT
        // -------------------------------------------------

        // 0 -> 1 -> 0
        float movement =
            Mathf.Sin(t * Mathf.PI);


        // -------------------------------------------------
        // SIDEWAYS DIRECTION
        // -------------------------------------------------

        float sideDirection;

        if (sprite == leftSprite)
        {
            // Left sprite moves left
            sideDirection = -1f;
        }
        else
        {
            // Right sprite moves right
            sideDirection = 1f;
        }


        // -------------------------------------------------
        // CALCULATE MOVEMENT
        // -------------------------------------------------

        float sideMovement =
            movement *
            idleSideAmount *
            sideDirection;

        float verticalMovement =
            movement *
            idleLiftAmount;


        return new Vector2(
            sideMovement,
            verticalMovement
        );
    }


    // =========================================================
    // CANVAS GROUP
    // =========================================================

    private void UpdateCanvasGroup()
    {
        if (canvasGroup == null)
            return;

        float targetAlpha =
            Mathf.Lerp(
                originalAlpha,
                1f,
                hoverProgress
            );

        canvasGroup.alpha = targetAlpha;
    }


    // =========================================================
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovering = true;


        // -------------------------------------------------
        // RANDOMIZE INTRO SPRITE ORDER
        // -------------------------------------------------

        leftSpriteGoesFirst =
            Random.value > 0.5f;


        // -------------------------------------------------
        // RANDOMIZE IDLE SPRITE ORDER
        // -------------------------------------------------

        idleLeftSpriteGoesFirst =
            Random.value > 0.5f;


        // -------------------------------------------------
        // RESET IDLE ANIMATION
        // -------------------------------------------------

        idleTimer = 0f;
        idleAnimationRunning = false;


        // -------------------------------------------------
        // START BUTTON SIZE ANIMATION
        // -------------------------------------------------

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = true;


        // Start smaller immediately
        if (buttonTransform != null)
        {
            buttonTransform.localScale =
                originalScale * hoverStartScale;
        }


        // Trigger punch after size animation
        punchTimer = 0f;


        if (hoverObject != null)
            hoverObject.SetActive(true);
    }


    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovering = false;


        // -------------------------------------------------
        // RESET IDLE
        // -------------------------------------------------

        idleTimer = 0f;
        idleAnimationRunning = false;


        // -------------------------------------------------
        // RESET BUTTON SIZE ANIMATION
        // -------------------------------------------------

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = false;


        // Return to the original fetched size
        if (buttonTransform != null)
        {
            buttonTransform.localScale =
                originalScale;
        }


        // -------------------------------------------------
        // DISABLE HOVER OBJECT AFTER ANIMATION
        // -------------------------------------------------

        if (hoverObject != null)
        {
            StartCoroutine(
                DisableHoverObjectAfterAnimation()
            );
        }
    }


    // =========================================================
    // DISABLE HOVER OBJECT
    // =========================================================

    private System.Collections.IEnumerator
        DisableHoverObjectAfterAnimation()
    {
        while (
            hoverProgress > 0f ||
            secondHoverProgress > 0f
        )
        {
            yield return null;
        }


        if (hoverObject != null && !isHovering)
            hoverObject.SetActive(false);
    }
}