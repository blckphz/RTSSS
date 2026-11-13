
using System.Collections;
using UnityEngine;

public class NextRoundButtonManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Transform buttonTransform;

    [Header("Activation Animation")]
    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float pulseDuration = 0.6f;

    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.15f;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;
    private bool isActive;

    private void Awake()
    {
        if (buttonTransform == null)
        {
            Debug.LogError("[NextRoundButtonManager] Button Transform is missing!");
            return;
        }

        originalScale = buttonTransform.localScale;
        ResetNextRoundButton();
    }

    public void ResetNextRoundButton()
    {
        isActive = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(false);
            buttonTransform.localScale = originalScale;
        }
    }

    public void EnableNextRoundButton()
    {
        if (buttonTransform == null)
            return;

        if (isActive)
            return;

        isActive = true;

        buttonTransform.gameObject.SetActive(true);
        buttonTransform.localScale = originalScale;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ActivationAnimation());
    }

    private IEnumerator ActivationAnimation()
    {
        // Pop up animation
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / popDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            buttonTransform.localScale = Vector3.Lerp(
                originalScale * 0.5f,
                originalScale * popScale,
                easedT
            );

            yield return null;
        }

        // Bounce back to normal size
        elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / popDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 2f);

            buttonTransform.localScale = Vector3.Lerp(
                originalScale * popScale,
                originalScale,
                easedT
            );

            yield return null;
        }

        // Gentle repeating pulse
        while (isActive)
        {
            elapsed = 0f;

            while (elapsed < pulseDuration && isActive)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / pulseDuration);
                float scale = Mathf.Lerp(
                    1f,
                    pulseScale,
                    Mathf.Sin(t * Mathf.PI)
                );

                buttonTransform.localScale = originalScale * scale;

                yield return null;
            }
        }

        buttonTransform.localScale = originalScale;
        animationCoroutine = null;
    }

    public void OnButtonHover()
    {
        if (!isActive || buttonTransform == null)
            return;

        StopAllCoroutines();
        animationCoroutine = StartCoroutine(ScaleButton(originalScale * hoverScale));
    }

    public void OnButtonExit()
    {
        if (!isActive || buttonTransform == null)
            return;

        StopAllCoroutines();
        animationCoroutine = StartCoroutine(ScaleButton(originalScale));
    }

    private IEnumerator ScaleButton(Vector3 targetScale)
    {
        Vector3 startScale = buttonTransform.localScale;
        float elapsed = 0f;

        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / hoverDuration);
            buttonTransform.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                t
            );

            yield return null;
        }

        buttonTransform.localScale = targetScale;
        animationCoroutine = null;
    }
}
