
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NextRoundButtonManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover GameObject")]
    [SerializeField] private GameObject hoverObject;

    [Header("Button References")]
    [SerializeField] private RectTransform buttonTransform;

    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 10f;

    [Header("Activation Juice")]
    [SerializeField] private float activationStartScale = 0.7f;
    [SerializeField] private float activationOvershoot = 1.15f;
    [SerializeField] private float activationSettleScale = 0.95f;
    [SerializeField] private float activationDuration = 0.4f;
    [SerializeField] private float activationRotation = 8f;

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
    [SerializeField] private float idleLiftDuration = 0.25f;
    [SerializeField] private float idleLiftInterval = 0.5f;

    [Header("Idle Side Movement")]
    [SerializeField] private float idleSideAmount = 3f;

    [Range(0f, 0.5f)]
    [SerializeField] private float idleSecondSpriteDelay = 0.1f;

    [Header("Hover Sound")]
    [SerializeField] private AudioSource hoverAudioSource;

    [Header("Press Sound")]
    [SerializeField] private AudioSource pressAudioSource;

    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.15f;

    private Vector3 originalScale;
    private Quaternion originalRotation;

    private Vector2 leftOriginalPosition;
    private Vector2 rightOriginalPosition;

    private float originalAlpha;

    private float hoverProgress;
    private float secondHoverProgress;

    private bool isHovering;
    private bool leftSpriteGoesFirst = true;
    private bool idleLeftSpriteGoesFirst = true;

    private float punchTimer;
    private float hoverSizeTimer;
    private bool hoverSizeAnimationActive;

    private float idleTimer;
    private bool idleAnimationRunning;

    private bool isActive;

    private Coroutine activationCoroutine;
    private bool activationAnimationPlaying;

    private void Awake()
    {
        if (buttonTransform == null)
            buttonTransform = transform as RectTransform;

        if (buttonTransform != null)
        {
            originalScale = buttonTransform.localScale;
            originalRotation = buttonTransform.localRotation;
        }

        if (leftSprite != null)
            leftOriginalPosition = leftSprite.anchoredPosition;

        if (rightSprite != null)
            rightOriginalPosition = rightSprite.anchoredPosition;

        if (canvasGroup != null)
            originalAlpha = canvasGroup.alpha;

        if (hoverObject != null)
            hoverObject.SetActive(false);

        ResetNextRoundButton();
    }

    private void Update()
    {
        UpdateHoverProgress();
        UpdateButtonPulse();
        UpdateSpriteMovement();
        UpdateCanvasGroup();

        if (punchTimer > 0f)
            punchTimer -= Time.deltaTime;

        if (!isHovering &&
            hoverProgress <= 0f &&
            secondHoverProgress <= 0f &&
            hoverObject != null &&
            hoverObject.activeSelf)
        {
            hoverObject.SetActive(false);
        }
    }

    // =========================================================
    // RESET BUTTON
    // =========================================================

    public void ResetNextRoundButton()
    {
        isActive = false;
        isHovering = false;

        activationAnimationPlaying = false;

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        hoverProgress = 0f;
        secondHoverProgress = 0f;

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = false;

        punchTimer = 0f;

        idleTimer = 0f;
        idleAnimationRunning = false;

        if (buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(false);
            buttonTransform.localScale = originalScale;
            buttonTransform.localRotation = originalRotation;
        }

        if (leftSprite != null)
            leftSprite.anchoredPosition = leftOriginalPosition;

        if (rightSprite != null)
            rightSprite.anchoredPosition = rightOriginalPosition;

        if (hoverObject != null)
            hoverObject.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = originalAlpha;
    }

    // =========================================================
    // ENABLE BUTTON
    // =========================================================

    public void EnableNextRoundButton()
    {
        if (buttonTransform == null || isActive)
            return;

        isActive = true;

        buttonTransform.gameObject.SetActive(true);

        buttonTransform.localScale =
            originalScale * activationStartScale;

        buttonTransform.localRotation = originalRotation;

        activationAnimationPlaying = true;

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine =
            StartCoroutine(ActivationJuiceAnimation());
    }

    // =========================================================
    // ACTIVATION JUICE ANIMATION
    // =========================================================

    private IEnumerator ActivationJuiceAnimation()
    {
        float halfDuration = activationDuration * 0.45f;
        float elapsed = 0f;

        // PHASE 1: Shoot past normal size

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float t = halfDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / halfDuration);

            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            float scale = Mathf.Lerp(
                activationStartScale,
                activationOvershoot,
                easedT
            );

            float rotation = Mathf.Lerp(
                activationRotation,
                -activationRotation * 0.5f,
                easedT
            );

            buttonTransform.localScale = originalScale * scale;

            buttonTransform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        // PHASE 2: Squash back slightly

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float t = halfDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / halfDuration);

            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            float scale = Mathf.Lerp(
                activationOvershoot,
                activationSettleScale,
                easedT
            );

            float rotation = Mathf.Lerp(
                -activationRotation * 0.5f,
                0f,
                easedT
            );

            buttonTransform.localScale = originalScale * scale;

            buttonTransform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        // PHASE 3: Final bounce to normal size

        elapsed = 0f;

        float finalDuration = activationDuration * 0.1f;

        while (elapsed < finalDuration)
        {
            elapsed += Time.deltaTime;

            float t = finalDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / finalDuration);

            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            float scale = Mathf.Lerp(
                activationSettleScale,
                1f,
                easedT
            );

            buttonTransform.localScale = originalScale * scale;

            yield return null;
        }

        buttonTransform.localScale = originalScale;
        buttonTransform.localRotation = originalRotation;

        activationAnimationPlaying = false;
        activationCoroutine = null;
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
    // BUTTON PULSE AND HOVER SCALE
    // =========================================================

    private void UpdateButtonPulse()
    {
        if (activationAnimationPlaying)
            return;

        if (!isActive || buttonTransform == null)
            return;

        if (hoverSizeAnimationActive)
        {
            hoverSizeTimer += Time.deltaTime;

            float t = hoverGrowDuration <= 0f
                ? 1f
                : Mathf.Clamp01(hoverSizeTimer / hoverGrowDuration);

            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            float currentScale = Mathf.Lerp(
                hoverStartScale,
                1f,
                easedT
            );

            buttonTransform.localScale = originalScale * currentScale;

            if (t >= 1f)
                hoverSizeAnimationActive = false;

            return;
        }

        if (punchTimer > 0f)
        {
            float punchProgress = punchDuration <= 0f
                ? 1f
                : 1f - (punchTimer / punchDuration);

            float currentPunchScale = Mathf.Lerp(
                entryPunchScale,
                hoverPulseScale,
                punchProgress
            );

            buttonTransform.localScale = originalScale * currentPunchScale;
            return;
        }

        float targetPulseScale = Mathf.Lerp(
            idlePulseScale,
            hoverPulseScale,
            hoverProgress
        );

        float targetPulseSpeed = Mathf.Lerp(
            idlePulseSpeed,
            hoverPulseSpeed,
            hoverProgress
        );

        float pulse = (Mathf.Sin(Time.time * targetPulseSpeed) + 1f) * 0.5f;

        float scale = Mathf.Lerp(
            1f,
            targetPulseScale,
            pulse
        );

        Vector3 targetScale = originalScale * scale;

        buttonTransform.localScale = Vector3.Lerp(
            buttonTransform.localScale,
            targetScale,
            Time.deltaTime * 12f
        );
    }

    // =========================================================
    // SPRITE OVERSHOOT
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

        float firstProgress = isHovering
            ? CalculateOvershoot(hoverProgress)
            : hoverProgress;

        float secondProgress = isHovering
            ? CalculateOvershoot(secondHoverProgress)
            : secondHoverProgress;

        Vector2 firstTarget =
            firstOriginalPos + firstMoveDir * firstProgress;

        Vector2 secondTarget =
            secondOriginalPos + secondMoveDir * secondProgress;

        if (isHovering && hoverProgress >= 0.99f)
        {
            UpdateIdleLift();

            Vector2 firstIdleOffset = GetIdleOffset(
                idleLeftSpriteGoesFirst,
                true,
                firstSprite
            );

            Vector2 secondIdleOffset = GetIdleOffset(
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
        if (idleLiftInterval <= 0f)
            return;

        idleTimer += Time.deltaTime;

        if (idleTimer >= idleLiftInterval)
        {
            idleTimer -= idleLiftInterval;

            idleLeftSpriteGoesFirst = Random.value > 0.5f;
            idleAnimationRunning = true;
        }
    }

    // =========================================================
    // IDLE LIFT AND SIDEWAYS MOVEMENT
    // =========================================================

    private Vector2 GetIdleOffset(
        bool leftGoesFirst,
        bool isFirstSprite,
        RectTransform sprite)
    {
        if (!idleAnimationRunning || sprite == null)
            return Vector2.zero;

        bool thisSpriteGoesFirst = isFirstSprite == leftGoesFirst;

        float spriteDelay = thisSpriteGoesFirst
            ? 0f
            : idleSecondSpriteDelay;

        if (idleLiftDuration <= 0f)
            return Vector2.zero;

        float t = (idleTimer - spriteDelay) / idleLiftDuration;

        if (t <= 0f || t >= 1f)
            return Vector2.zero;

        float movement = Mathf.Sin(t * Mathf.PI);

        float sideDirection = sprite == leftSprite ? -1f : 1f;

        float sideMovement =
            movement * idleSideAmount * sideDirection;

        float verticalMovement =
            movement * idleLiftAmount;

        return new Vector2(sideMovement, verticalMovement);
    }

    // =========================================================
    // CANVAS GROUP FADE
    // =========================================================

    private void UpdateCanvasGroup()
    {
        if (canvasGroup == null)
            return;

        float targetAlpha = isActive
            ? Mathf.Lerp(originalAlpha, 1f, hoverProgress)
            : originalAlpha;

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );
    }

    // =========================================================
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActive)
            return;

        isHovering = true;

        if (hoverAudioSource != null)
            hoverAudioSource.Play();

        leftSpriteGoesFirst = Random.value > 0.5f;
        idleLeftSpriteGoesFirst = Random.value > 0.5f;

        idleTimer = 0f;
        idleAnimationRunning = false;

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = true;

        punchTimer = punchDuration;

        if (buttonTransform != null && !activationAnimationPlaying)
            buttonTransform.localScale = originalScale * hoverStartScale;

        if (hoverObject != null)
            hoverObject.SetActive(true);
    }

    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverExit();
    }

    private void SetHoverExit()
    {
        isHovering = false;

        idleTimer = 0f;
        idleAnimationRunning = false;

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = false;
        punchTimer = 0f;
    }

    // =========================================================
    // PUBLIC HOVER METHODS
    // =========================================================

    public void OnButtonHover()
    {
        if (!isActive || buttonTransform == null)
            return;

        if (!isHovering)
        {
            isHovering = true;

            leftSpriteGoesFirst = Random.value > 0.5f;
            idleLeftSpriteGoesFirst = Random.value > 0.5f;

            idleTimer = 0f;
            idleAnimationRunning = false;

            if (hoverAudioSource != null)
                hoverAudioSource.Play();

            if (hoverObject != null)
                hoverObject.SetActive(true);
        }

        if (activationAnimationPlaying)
            return;

        hoverSizeTimer = 0f;
        hoverSizeAnimationActive = true;
        punchTimer = punchDuration;

        buttonTransform.localScale = originalScale * hoverStartScale;
    }

    public void OnButtonExit()
    {
        if (!isActive || buttonTransform == null)
            return;

        SetHoverExit();
    }

    // =========================================================
    // BUTTON PRESSED SOUND
    // =========================================================

    public void OnButtonPressed()
    {
        if (pressAudioSource != null)
            pressAudioSource.Play();
    }
}
