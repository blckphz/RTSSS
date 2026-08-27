using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class tooltipManager : MonoBehaviour
{
    [Header("Tooltip UI")]
    [SerializeField]
    private GameObject tooltipObject;

    [SerializeField]
    private TMP_Text tooltipText;


    [Header("Mouse")]
    [SerializeField]
    private Vector2 mouseOffset =
        new Vector2(20f, -20f);


    [Header("Tooltip Animation")]
    [SerializeField]
    private CanvasGroup tooltipCanvasGroup;

    [SerializeField]
    private float animationDuration = 0.18f;

    [SerializeField]
    private float startScale = 0.75f;

    [SerializeField]
    private float overshootScale = 1.08f;


    private RectTransform tooltipRect;

    private RectTransform canvasRect;

    private Canvas canvas;

    private Coroutine tooltipAnimation;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // GET CANVAS
        // --------------------------------------------------------

        canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "[TooltipManager] " +
                "Could not find Canvas on TooltipManager!"
            );

            return;
        }


        // --------------------------------------------------------
        // GET CANVAS RECT
        // --------------------------------------------------------

        canvasRect =
            canvas.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            Debug.LogError(
                "[TooltipManager] " +
                "Canvas does not have a RectTransform!"
            );

            return;
        }


        // --------------------------------------------------------
        // GET TOOLTIP RECT
        // --------------------------------------------------------

        if (tooltipObject != null)
        {
            tooltipRect =
                tooltipObject.GetComponent<RectTransform>();
        }


        if (tooltipRect == null)
        {
            Debug.LogError(
                "[TooltipManager] " +
                "Tooltip GameObject needs a RectTransform!"
            );
        }


        // --------------------------------------------------------
        // GET CANVAS GROUP
        // --------------------------------------------------------

        if (tooltipObject != null)
        {
            tooltipCanvasGroup =
                tooltipObject.GetComponent<CanvasGroup>();
        }


        if (tooltipCanvasGroup == null)
        {
            Debug.LogWarning(
                "[TooltipManager] " +
                "Tooltip GameObject does not have a CanvasGroup!"
            );
        }


        // --------------------------------------------------------
        // TEXT
        // --------------------------------------------------------

        if (tooltipText == null)
        {
            Debug.LogWarning(
                "[TooltipManager] " +
                "Tooltip Text is not assigned."
            );
        }


        // --------------------------------------------------------
        // INITIAL STATE
        // --------------------------------------------------------

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }


        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.one * startScale;
        }


        HideTooltipInstant();
    }


    private void Update()
    {
        FollowMouse();
    }


    // ============================================================
    // SHOW STATUS TOOLTIP
    // ============================================================

    public void ShowStatusTooltip(string statusId)
    {
        if (tooltipObject == null)
        {
            Debug.LogError(
                "[TooltipManager] " +
                "tooltipObject is NULL!"
            );

            return;
        }


        if (tooltipText == null)
        {
            Debug.LogError(
                "[TooltipManager] " +
                "tooltipText is NULL!"
            );

            return;
        }


        if (string.IsNullOrEmpty(statusId))
        {
            Debug.LogWarning(
                "[TooltipManager] " +
                "statusId is empty."
            );

            return;
        }


        // ========================================================
        // STATUS
        // ========================================================

        switch (statusId.ToLower())
        {
            case "stun":

                tooltipText.text =
                    "<b>STUN</b>\n" +
                    "Cannot move or attack.\n" +
                    "Duration: 1 turn";

                break;


            case "burn":

                tooltipText.text =
                    "<b>BURN</b>\n" +
                    "Takes damage over time.\n" +
                    "Duration: 2 turns";

                break;


            case "slow":

                tooltipText.text =
                    "<b>SLOW</b>\n" +
                    "Movement range is reduced.";

                break;


            case "poison":

                tooltipText.text =
                    "<b>POISON</b>\n" +
                    "Takes damage over time.";

                break;


            default:

                tooltipText.text =
                    $"<b>{statusId.ToUpper()}</b>";

                Debug.LogWarning(
                    "[TooltipManager] Unknown status: " +
                    statusId
                );

                break;
        }


        // ========================================================
        // STOP CURRENT ANIMATION
        // ========================================================

        if (tooltipAnimation != null)
        {
            StopCoroutine(tooltipAnimation);
            tooltipAnimation = null;
        }


        // ========================================================
        // ENABLE TOOLTIP
        // ========================================================

        tooltipObject.SetActive(true);


        // ========================================================
        // RESET ANIMATION
        // ========================================================

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }


        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.one * startScale;
        }


        // ========================================================
        // UPDATE LAYOUT
        // ========================================================

        tooltipText.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();


        // ========================================================
        // POSITION
        // ========================================================

        FollowMouse();


        // ========================================================
        // PLAY POP-IN
        // ========================================================

        tooltipAnimation =
            StartCoroutine(
                AnimateTooltipIn()
            );
    }


    // ============================================================
    // HIDE
    // ============================================================

    public void HideTooltip()
    {
        if (tooltipObject == null)
        {
            return;
        }


        if (!tooltipObject.activeSelf)
        {
            return;
        }


        // --------------------------------------------------------
        // STOP CURRENT ANIMATION
        // --------------------------------------------------------

        if (tooltipAnimation != null)
        {
            StopCoroutine(tooltipAnimation);
            tooltipAnimation = null;
        }


        // --------------------------------------------------------
        // PLAY POP-OUT
        // --------------------------------------------------------

        tooltipAnimation =
            StartCoroutine(
                AnimateTooltipOut()
            );
    }


    // ============================================================
    // HIDE INSTANTLY
    // ============================================================

    private void HideTooltipInstant()
    {
        if (tooltipObject == null)
        {
            return;
        }


        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }


        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.one * startScale;
        }


        tooltipObject.SetActive(false);
    }


    // ============================================================
    // POP IN
    // ============================================================

    private IEnumerator AnimateTooltipIn()
    {
        float elapsed = 0f;


        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;


            float t =
                animationDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        animationDuration
                    );


            // ====================================================
            // FADE
            // ====================================================

            if (tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );
            }


            // ====================================================
            // POP
            // ====================================================

            if (tooltipRect != null)
            {
                float scale;


                if (t < 0.7f)
                {
                    float popT =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            t / 0.7f
                        );


                    scale =
                        Mathf.Lerp(
                            startScale,
                            overshootScale,
                            popT
                        );
                }
                else
                {
                    float settleT =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            (t - 0.7f) / 0.3f
                        );


                    scale =
                        Mathf.Lerp(
                            overshootScale,
                            1f,
                            settleT
                        );
                }


                tooltipRect.localScale =
                    Vector3.one * scale;
            }


            yield return null;
        }


        // ========================================================
        // FINAL STATE
        // ========================================================

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 1f;
        }


        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.one;
        }


        tooltipAnimation = null;
    }


    // ============================================================
    // POP OUT
    // ============================================================

    private IEnumerator AnimateTooltipOut()
    {
        float elapsed = 0f;


        float startingAlpha =
            tooltipCanvasGroup != null
                ? tooltipCanvasGroup.alpha
                : 1f;


        Vector3 startingScale =
            tooltipRect != null
                ? tooltipRect.localScale
                : Vector3.one;


        while (elapsed < animationDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                animationDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        animationDuration
                    );


            float easedT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            // ====================================================
            // FADE OUT
            // ====================================================

            if (tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha =
                    Mathf.Lerp(
                        startingAlpha,
                        0f,
                        easedT
                    );
            }


            // ====================================================
            // SHRINK
            // ====================================================

            if (tooltipRect != null)
            {
                tooltipRect.localScale =
                    Vector3.Lerp(
                        startingScale,
                        Vector3.one * startScale,
                        easedT
                    );
            }


            yield return null;
        }


        // ========================================================
        // FINAL STATE
        // ========================================================

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }


        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.one * startScale;
        }


        tooltipObject.SetActive(false);

        tooltipAnimation = null;
    }


    // ============================================================
    // FOLLOW MOUSE
    // ============================================================

    private void FollowMouse()
    {
        if (tooltipObject == null)
        {
            return;
        }


        if (!tooltipObject.activeSelf)
        {
            return;
        }


        if (tooltipRect == null)
        {
            return;
        }


        if (canvas == null)
        {
            return;
        }


        // ========================================================
        // NEW INPUT SYSTEM
        // ========================================================

        if (Mouse.current == null)
        {
            return;
        }


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // ========================================================
        // SCREEN SPACE OVERLAY
        // ========================================================

        if (
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            Vector2 localPosition;


            bool success =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        mousePosition,
                        null,
                        out localPosition
                    );


            if (!success)
            {
                return;
            }


            localPosition += mouseOffset;

            tooltipRect.localPosition =
                localPosition;

            return;
        }


        // ========================================================
        // SCREEN SPACE CAMERA
        // ========================================================

        if (
            canvas.renderMode ==
            RenderMode.ScreenSpaceCamera
        )
        {
            Vector2 localPosition;


            bool success =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        mousePosition,
                        canvas.worldCamera,
                        out localPosition
                    );


            if (!success)
            {
                return;
            }


            localPosition += mouseOffset;

            tooltipRect.localPosition =
                localPosition;

            return;
        }


        // ========================================================
        // WORLD SPACE
        // ========================================================

        if (
            canvas.renderMode ==
            RenderMode.WorldSpace
        )
        {
            Camera cam =
                canvas.worldCamera;


            if (cam == null)
            {
                cam = Camera.main;
            }


            if (cam == null)
            {
                return;
            }


            Vector2 localPosition;


            bool success =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        mousePosition,
                        cam,
                        out localPosition
                    );


            if (!success)
            {
                return;
            }


            localPosition += mouseOffset;

            tooltipRect.localPosition =
                localPosition;
        }
    }
}