using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class tooltipManager : MonoBehaviour
{
    [Header("Tooltip References")]
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private GameObject tooltipObject;
    [SerializeField] private RectTransform tooltipBackground;
    [SerializeField] private CanvasGroup canvasGroup;

[Header("Tooltip Background Padding")]
    [SerializeField] private float backgroundPaddingX = 30f;
    [SerializeField] private float backgroundPaddingY = 20f;

    [Header("Tooltip Position")]
    [SerializeField]
    private Vector2 mouseOffset =
        new Vector2(15f, -15f);

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 10f;
    [SerializeField] private float popInScale = 1f;

    private RectTransform tooltipRect;
    private Canvas canvas;

    private bool isShowing;

    private void Awake()
    {
        if (tooltipObject != null)
        {
            tooltipRect =
                tooltipObject.GetComponent<RectTransform>();
        }

        if (canvasGroup == null && tooltipObject != null)
        {
            canvasGroup =
                tooltipObject.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null && tooltipObject != null)
        {
            canvasGroup =
                tooltipObject.AddComponent<CanvasGroup>();
        }

        canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            canvas =
                FindFirstObjectByType<Canvas>();
        }

        HideTooltipImmediate();
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        FollowMouse();

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                Mathf.MoveTowards(
                    canvasGroup.alpha,
                    1f,
                    fadeSpeed * Time.unscaledDeltaTime
                );
        }

        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.Lerp(
                    tooltipRect.localScale,
                    Vector3.one * popInScale,
                    fadeSpeed * Time.unscaledDeltaTime
                );
        }
    }

    // =========================================================
    // SHOW TOOLTIP
    // =========================================================

    public void ShowStatusTooltip(
        string statusId,
        float stunChance = 0f,
        int stunDuration = 1
    )
    {
        if (tooltipText == null)
        {
            return;
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }

        isShowing = true;

        switch (statusId.ToLower())
        {
            case "stun":

                tooltipText.text =
                    "<b>STUN</b>\n" +
                    "Cannot move or attack.\n" +
                    $"Chance: {stunChance:0}%\n" +
                    $"Duration: {stunDuration} turn" +
                    (stunDuration == 1
                        ? ""
                        : "s");

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

                tooltipText.text = "";

                break;
        }

        tooltipText.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();

        UpdateTooltipSize();

        Canvas.ForceUpdateCanvases();

        FollowMouse();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.zero;
        }
    }

    // =========================================================
    // HIDE TOOLTIP
    // =========================================================

    public void HideTooltip()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    private void HideTooltipImmediate()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (tooltipRect != null)
        {
            tooltipRect.localScale =
                Vector3.zero;
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    // =========================================================
    // TOOLTIP SIZE
    // =========================================================

    private void UpdateTooltipSize()
    {
        if (tooltipText == null)
        {
            return;
        }

        if (tooltipBackground == null)
        {
            return;
        }

        tooltipText.ForceMeshUpdate();

        Vector2 preferredSize =
            tooltipText.GetPreferredValues();

        float width =
            preferredSize.x +
            backgroundPaddingX;

        float height =
            preferredSize.y +
            backgroundPaddingY;

        tooltipBackground.sizeDelta =
            new Vector2(
                width,
                height
            );
    }

    // =========================================================
    // FOLLOW MOUSE
    // =========================================================

    private void FollowMouse()
    {
        if (tooltipRect == null)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;

        if (canvas == null)
        {
            tooltipRect.position =
                mousePosition + mouseOffset;

            return;
        }

        if (
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            tooltipRect.position =
                mousePosition + mouseOffset;

            return;
        }

        Camera canvasCamera =
            canvas.worldCamera;

        if (canvasCamera == null)
        {
            canvasCamera =
                Camera.main;
        }

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        Vector2 localPoint;

        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mousePosition,
                canvasCamera,
                out localPoint
            )
        )
        {
            tooltipRect.localPosition =
                localPoint + mouseOffset;
        }
    }

}
