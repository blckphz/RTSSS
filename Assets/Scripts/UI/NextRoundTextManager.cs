using System.Collections;
using TMPro;
using UnityEngine;

public class NextRoundTextManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform animationTarget;

    [SerializeField]
    private TMP_Text turnText;


    [Header("Cinematic Bars")]
    [SerializeField]
    private RectTransform topBar;

    [SerializeField]
    private RectTransform bottomBar;


    [Header("Text")]
    [SerializeField]
    private string enemyTurnText = "ENEMY TURN";

    [SerializeField]
    private string playerTurnText = "PLAYER TURN";


    [Header("Text Colors")]
    [SerializeField]
    private Color playerTurnColor = Color.white;

    [SerializeField]
    private Color enemyTurnColor = Color.red;


    [Header("Round Start Audio")]
    [SerializeField]
    private AudioSource roundAudioSource;

    [SerializeField]
    private AudioClip enemyRoundStartClip;

    [SerializeField, Range(0f, 1f)]
    private float enemyRoundStartVolume = 0.8f;

    [SerializeField]
    private AudioClip playerRoundStartClip;

    [SerializeField, Range(0f, 1f)]
    private float playerRoundStartVolume = 0.8f;


    [Header("Timing")]
    [SerializeField]
    private float fadeInDuration = 0.18f;

    [SerializeField]
    private float visibleDuration = 0.25f;

    [SerializeField]
    private float fadeOutDuration = 0.22f;


    [Header("Movement")]
    [SerializeField]
    private Vector2 startOffset = new Vector2(0f, -40f);

    [SerializeField]
    private Vector2 endOffset = new Vector2(0f, 40f);


    [Header("Scale")]
    [SerializeField]
    private float startScale = 0.85f;

    [SerializeField]
    private float endScale = 0.95f;


    [Header("Tilt")]
    [SerializeField]
    private float tiltAngle = 2f;


    [Header("Cinematic Bar Settings")]
    [SerializeField]
    private float barMoveDistance = 120f;

    [SerializeField]
    private float barMoveDuration = 0.25f;

    [SerializeField]
    [Range(0f, 0.3f)]
    private float barOvershootAmount = 0.08f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private Coroutine animationCoroutine;
    private Coroutine barCoroutine;

    private Vector3 originalPosition;
    private Vector3 originalScale;

    private Vector3 originalTopBarPosition;
    private Vector3 originalBottomBarPosition;

    private RoundManager roundManager;


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsAnimating { get; private set; }


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (animationTarget == null)
        {
            animationTarget = GetComponent<RectTransform>();
        }

        if (roundAudioSource == null)
        {
            roundAudioSource =
                GetComponent<AudioSource>();
        }

        if (roundAudioSource == null)
        {
            roundAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        roundAudioSource.playOnAwake = false;

        if (animationTarget != null)
        {
            originalPosition =
                animationTarget.localPosition;

            originalScale =
                animationTarget.localScale;
        }

        if (topBar != null)
        {
            originalTopBarPosition =
                topBar.localPosition;
        }

        if (bottomBar != null)
        {
            originalBottomBarPosition =
                bottomBar.localPosition;
        }

        if (turnText != null)
        {
            turnText.text =
                enemyTurnText;

            turnText.color =
                enemyTurnColor;
        }

        HideImmediate();

        roundManager =
            FindFirstObjectByType<RoundManager>();

        if (roundManager != null)
        {
            roundManager.OnEnemyTurnStarted +=
                ShowEnemyTurn;
        }
        else
        {
            Debug.LogWarning(
                "NextRoundTextManager: RoundManager was not found in Awake()."
            );
        }
    }


    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnEnemyTurnStarted -=
                ShowEnemyTurn;
        }
    }


    // ============================================================
    // ENEMY TURN
    // ============================================================

    public void ShowEnemyTurn()
    {
        PlayEnemyRoundStartSound();

        ShowTurnBanner(
            enemyTurnText,
            true,
            enemyTurnColor
        );
    }


    // ============================================================
    // PLAYER TURN
    // ============================================================

    public void ShowPlayerTurn()
    {
        PlayPlayerRoundStartSound();

        ShowTurnBanner(
            playerTurnText,
            false,
            playerTurnColor
        );
    }


    // ============================================================
    // ROUND START AUDIO
    // ============================================================

    private void PlayEnemyRoundStartSound()
    {
        if (
            enemyRoundStartClip == null ||
            roundAudioSource == null
        )
        {
            return;
        }

        roundAudioSource.PlayOneShot(
            enemyRoundStartClip,
            enemyRoundStartVolume
        );
    }


    private void PlayPlayerRoundStartSound()
    {
        if (
            playerRoundStartClip == null ||
            roundAudioSource == null
        )
        {
            return;
        }

        roundAudioSource.PlayOneShot(
            playerRoundStartClip,
            playerRoundStartVolume
        );
    }


    // ============================================================
    // GENERIC BANNER CONTROLLER
    // ============================================================

    private void ShowTurnBanner(
        string textToDisplay,
        bool showCinematicBars,
        Color bannerColor
    )
    {
        IsAnimating = true;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (barCoroutine != null)
        {
            StopCoroutine(barCoroutine);
            barCoroutine = null;
        }

        if (showCinematicBars)
        {
            barCoroutine =
                StartCoroutine(
                    MoveCinematicBars(true)
                );
        }

        animationCoroutine =
            StartCoroutine(
                TurnBannerAnimation(
                    textToDisplay,
                    bannerColor
                )
            );
    }


    // ============================================================
    // TURN BANNER ANIMATION
    // ============================================================

    private IEnumerator TurnBannerAnimation(
        string textToDisplay,
        Color bannerColor
    )
    {
        if (turnText != null)
        {
            turnText.text =
                textToDisplay;

            turnText.color =
                bannerColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }


        // ========================================================
        // RANDOM TILT
        // ========================================================

        float tiltDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float startTilt =
            tiltAngle *
            tiltDirection;

        float endTilt =
            -startTilt;


        // ========================================================
        // START POSITION
        // ========================================================

        Vector3 startPosition =
            originalPosition +
            new Vector3(
                startOffset.x,
                startOffset.y,
                0f
            );


        // ========================================================
        // INITIAL STATE
        // ========================================================

        if (animationTarget != null)
        {
            animationTarget.localPosition =
                startPosition;

            animationTarget.localScale =
                originalScale *
                startScale;

            animationTarget.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    startTilt
                );
        }


        // ========================================================
        // FADE IN
        // ========================================================

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                fadeInDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        fadeInDuration
                    );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    smoothT;
            }

            if (animationTarget != null)
            {
                animationTarget.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        originalPosition,
                        smoothT
                    );

                animationTarget.localScale =
                    Vector3.Lerp(
                        originalScale * startScale,
                        originalScale,
                        smoothT
                    );

                animationTarget.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Lerp(
                            startTilt,
                            0f,
                            smoothT
                        )
                    );
            }

            yield return null;
        }


        // ========================================================
        // RESET TO PERFECT CENTER
        // ========================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (animationTarget != null)
        {
            animationTarget.localPosition =
                originalPosition;

            animationTarget.localScale =
                originalScale;

            animationTarget.localRotation =
                Quaternion.identity;
        }


        // ========================================================
        // VISIBLE
        // ========================================================

        if (visibleDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    visibleDuration
                );
        }


        // ========================================================
        // FADE OUT
        // ========================================================

        elapsed = 0f;

        Vector3 endPosition =
            originalPosition +
            new Vector3(
                endOffset.x,
                endOffset.y,
                0f
            );

        while (elapsed < fadeOutDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                fadeOutDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        fadeOutDuration
                    );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        1f,
                        0f,
                        smoothT
                    );
            }

            if (animationTarget != null)
            {
                animationTarget.localPosition =
                    Vector3.Lerp(
                        originalPosition,
                        endPosition,
                        smoothT
                    );

                animationTarget.localScale =
                    Vector3.Lerp(
                        originalScale,
                        originalScale * endScale,
                        smoothT
                    );

                animationTarget.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Lerp(
                            0f,
                            endTilt,
                            smoothT
                        )
                    );
            }

            yield return null;
        }


        // ========================================================
        // COMPLETE
        // ========================================================

        HideTextImmediate();

        IsAnimating = false;
        animationCoroutine = null;
    }


    // ============================================================
    // CINEMATIC BARS
    // ============================================================

    private IEnumerator MoveCinematicBars(bool show)
    {
        Vector3 topStart =
            topBar != null
                ? topBar.localPosition
                : Vector3.zero;

        Vector3 bottomStart =
            bottomBar != null
                ? bottomBar.localPosition
                : Vector3.zero;

        Vector3 normalTopTarget =
            originalTopBarPosition +
            Vector3.down *
            barMoveDistance;

        Vector3 normalBottomTarget =
            originalBottomBarPosition +
            Vector3.up *
            barMoveDistance;

        Vector3 overshootTopTarget =
            originalTopBarPosition +
            Vector3.down *
            (
                barMoveDistance *
                (1f + barOvershootAmount)
            );

        Vector3 overshootBottomTarget =
            originalBottomBarPosition +
            Vector3.up *
            (
                barMoveDistance *
                (1f + barOvershootAmount)
            );

        float elapsed = 0f;

        float halfDuration =
            barMoveDuration * 0.7f;

        float remainingDuration =
            barMoveDuration * 0.3f;


        // ========================================================
        // PHASE 1
        // ========================================================

        while (
            elapsed < halfDuration &&
            halfDuration > 0f
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    halfDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (topBar != null)
            {
                topBar.localPosition =
                    Vector3.Lerp(
                        topStart,
                        show
                            ? overshootTopTarget
                            : originalTopBarPosition,
                        smoothT
                    );
            }

            if (bottomBar != null)
            {
                bottomBar.localPosition =
                    Vector3.Lerp(
                        bottomStart,
                        show
                            ? overshootBottomTarget
                            : originalBottomBarPosition,
                        smoothT
                    );
            }

            yield return null;
        }


        // ========================================================
        // PHASE 2
        // ========================================================

        Vector3 currentTopOvershoot =
            topBar != null
                ? topBar.localPosition
                : normalTopTarget;

        Vector3 currentBottomOvershoot =
            bottomBar != null
                ? bottomBar.localPosition
                : normalBottomTarget;

        elapsed = 0f;

        while (
            elapsed < remainingDuration &&
            remainingDuration > 0f
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    remainingDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (topBar != null)
            {
                topBar.localPosition =
                    Vector3.Lerp(
                        currentTopOvershoot,
                        show
                            ? normalTopTarget
                            : originalTopBarPosition,
                        smoothT
                    );
            }

            if (bottomBar != null)
            {
                bottomBar.localPosition =
                    Vector3.Lerp(
                        currentBottomOvershoot,
                        show
                            ? normalBottomTarget
                            : originalBottomBarPosition,
                        smoothT
                    );
            }

            yield return null;
        }

        if (topBar != null)
        {
            topBar.localPosition =
                show
                    ? normalTopTarget
                    : originalTopBarPosition;
        }

        if (bottomBar != null)
        {
            bottomBar.localPosition =
                show
                    ? normalBottomTarget
                    : originalBottomBarPosition;
        }

        barCoroutine = null;
    }


    // ============================================================
    // HIDE CINEMATIC BARS
    // ============================================================

    public IEnumerator HideCinematicBars()
    {
        if (barCoroutine != null)
        {
            StopCoroutine(barCoroutine);
            barCoroutine = null;
        }

        yield return StartCoroutine(
            HideCinematicBarsCoroutine()
        );
    }


    // ============================================================
    // HIDE CINEMATIC BARS COROUTINE
    // ============================================================

    private IEnumerator HideCinematicBarsCoroutine()
    {
        Vector3 topStart =
            topBar != null
                ? topBar.localPosition
                : Vector3.zero;

        Vector3 bottomStart =
            bottomBar != null
                ? bottomBar.localPosition
                : Vector3.zero;

        float elapsed = 0f;

        while (elapsed < barMoveDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                barMoveDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        barMoveDuration
                    );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (topBar != null)
            {
                topBar.localPosition =
                    Vector3.Lerp(
                        topStart,
                        originalTopBarPosition,
                        smoothT
                    );
            }

            if (bottomBar != null)
            {
                bottomBar.localPosition =
                    Vector3.Lerp(
                        bottomStart,
                        originalBottomBarPosition,
                        smoothT
                    );
            }

            yield return null;
        }

        if (topBar != null)
        {
            topBar.localPosition =
                originalTopBarPosition;
        }

        if (bottomBar != null)
        {
            bottomBar.localPosition =
                originalBottomBarPosition;
        }

        barCoroutine = null;
    }


    // ============================================================
    // HIDE TEXT ONLY
    // ============================================================

    private void HideTextImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (animationTarget != null)
        {
            animationTarget.localPosition =
                originalPosition;

            animationTarget.localScale =
                originalScale;

            animationTarget.localRotation =
                Quaternion.identity;
        }
    }


    // ============================================================
    // HIDE EVERYTHING
    // ============================================================

    public void HideImmediate()
    {
        HideTextImmediate();

        if (topBar != null)
        {
            topBar.localPosition =
                originalTopBarPosition;
        }

        if (bottomBar != null)
        {
            bottomBar.localPosition =
                originalBottomBarPosition;
        }
    }


    // ============================================================
    // HIDE CINEMATIC + RETURN CAMERA
    // ============================================================

    public void HideCinematicBarsAndReturnCamera()
    {
        if (barCoroutine != null)
        {
            StopCoroutine(barCoroutine);
            barCoroutine = null;
        }

        barCoroutine =
            StartCoroutine(
                MoveCameraThenHideCinematicBarsSequence()
            );
    }


    private IEnumerator MoveCameraThenHideCinematicBarsSequence()
    {
        if (CanvasJuiceManager.Instance != null)
        {
            CanvasJuiceManager.Instance
                .MoveCameraToNormalPosition();
        }

        yield return
            new WaitForSecondsRealtime(0.25f);

        yield return StartCoroutine(
            HideCinematicBarsCoroutine()
        );

        barCoroutine = null;
    }
}