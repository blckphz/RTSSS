using System.Collections;
using TMPro;
using UnityEngine;

public class RoundUIManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RoundManager roundManager;

    [SerializeField]
    private TextMeshProUGUI currentRoundText;


    [Header("Display")]

    [SerializeField]
    private string roundPrefix = "Round ";


    [Header("Fantasy Juice")]

    [Tooltip("Maximum scale during the round announcement.")]
    [SerializeField]
    private float popScale = 1.16f;

    [Tooltip("How far the text rises during the announcement.")]
    [SerializeField]
    private float floatHeight = 6f;

    [Tooltip("Small rotation during the impact.")]
    [SerializeField]
    private float rotationAmount = 1.5f;

    [Tooltip("Time spent expanding into the announcement.")]
    [SerializeField]
    private float impactDuration = 0.12f;

    [Tooltip("Time spent settling back to normal.")]
    [SerializeField]
    private float settleDuration = 0.28f;

    [Tooltip("How quickly the magical color fades.")]
    [SerializeField]
    private float flashDuration = 0.20f;

    [Tooltip("Color used for the magical flash.")]
    [SerializeField]
    private Color flashColor = new Color(1f, 0.75f, 0.25f, 1f);


    [Header("Optional Glow")]

    [Tooltip("Adds a subtle TMP glow during the announcement.")]
    [SerializeField]
    private bool useGlow = true;

    [Tooltip("Strength of the temporary TMP glow.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float glowStrength = 0.75f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private Color originalColor;

    private float originalGlow;

    private Coroutine juiceCoroutine;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>();
        }

        if (currentRoundText != null)
        {
            Transform textTransform =
                currentRoundText.transform;

            originalScale =
                textTransform.localScale;

            originalPosition =
                textTransform.localPosition;

            originalRotation =
                textTransform.localRotation;

            originalColor =
                currentRoundText.color;

            originalGlow =
                currentRoundText.fontMaterial.GetFloat(
                    ShaderUtilities.ID_GlowPower
                );
        }
    }


    private void OnEnable()
    {
        if (roundManager == null)
        {
            return;
        }

        roundManager.OnRoundChanged += UpdateRoundText;
    }


    private void Start()
    {
        if (roundManager == null)
        {
            UpdateRoundText(1);
            return;
        }

        UpdateRoundText(
            roundManager.GetCurrentRound()
        );
    }


    private void OnDisable()
    {
        if (roundManager == null)
        {
            return;
        }

        roundManager.OnRoundChanged -= UpdateRoundText;
    }


    // ============================================================
    // ROUND TEXT
    // ============================================================

    private void UpdateRoundText(int round)
    {
        if (currentRoundText == null)
        {
            return;
        }

        currentRoundText.text =
            roundPrefix + round;

        // Do not play the effect while Unity
        // is simply initializing the UI.
        if (!Application.isPlaying)
        {
            return;
        }

        PlayRoundChangeJuice();
    }


    // ============================================================
    // JUICE
    // ============================================================

    private void PlayRoundChangeJuice()
    {
        if (juiceCoroutine != null)
        {
            StopCoroutine(juiceCoroutine);
        }

        juiceCoroutine =
            StartCoroutine(RoundChangeAnimation());
    }


    private IEnumerator RoundChangeAnimation()
    {
        Transform textTransform =
            currentRoundText.transform;


        // --------------------------------------------------------
        // RESET
        // --------------------------------------------------------

        textTransform.localScale =
            originalScale;

        textTransform.localPosition =
            originalPosition;

        textTransform.localRotation =
            originalRotation;

        currentRoundText.color =
            originalColor;

        SetGlow(originalGlow);


        // --------------------------------------------------------
        // IMPACT
        // --------------------------------------------------------

        float elapsed = 0f;

        while (elapsed < impactDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / impactDuration
                );


            // Fast magical "arrival".
            float eased =
                1f - Mathf.Pow(1f - t, 3f);


            // SCALE
            textTransform.localScale =
                Vector3.Lerp(
                    originalScale,
                    originalScale * popScale,
                    eased
                );


            // FLOAT UP
            textTransform.localPosition =
                Vector3.Lerp(
                    originalPosition,
                    originalPosition +
                    Vector3.up * floatHeight,
                    eased
                );


            // SLIGHT ROTATION
            textTransform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(
                        0f,
                        rotationAmount,
                        eased
                    )
                ) *
                originalRotation;


            // GOLDEN COLOR
            float colorT =
                Mathf.Clamp01(
                    elapsed / flashDuration
                );

            currentRoundText.color =
                Color.Lerp(
                    originalColor,
                    flashColor,
                    colorT
                );


            // GLOW
            if (useGlow)
            {
                float glowT =
                    Mathf.Sin(
                        t * Mathf.PI
                    );

                SetGlow(
                    Mathf.Lerp(
                        originalGlow,
                        glowStrength,
                        glowT
                    )
                );
            }


            yield return null;
        }


        // --------------------------------------------------------
        // SETTLE
        // --------------------------------------------------------

        elapsed = 0f;

        Vector3 peakScale =
            originalScale * popScale;

        Vector3 peakPosition =
            originalPosition +
            Vector3.up * floatHeight;


        Quaternion peakRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotationAmount
            ) *
            originalRotation;


        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / settleDuration
                );


            // Smooth, graceful settle.
            float eased =
                Mathf.Sin(
                    t * Mathf.PI * 0.5f
                );


            // SCALE
            textTransform.localScale =
                Vector3.Lerp(
                    peakScale,
                    originalScale,
                    eased
                );


            // FLOAT BACK DOWN
            textTransform.localPosition =
                Vector3.Lerp(
                    peakPosition,
                    originalPosition,
                    eased
                );


            // ROTATION BACK
            textTransform.localRotation =
                Quaternion.Lerp(
                    peakRotation,
                    originalRotation,
                    eased
                );


            // COLOR BACK TO NORMAL
            currentRoundText.color =
                Color.Lerp(
                    flashColor,
                    originalColor,
                    Mathf.Clamp01(
                        t * 1.25f
                    )
                );


            // GLOW FADES OUT
            if (useGlow)
            {
                float glowT =
                    1f - t;

                SetGlow(
                    Mathf.Lerp(
                        originalGlow,
                        glowStrength,
                        glowT
                    )
                );
            }


            yield return null;
        }


        // --------------------------------------------------------
        // FINAL RESET
        // --------------------------------------------------------

        textTransform.localScale =
            originalScale;

        textTransform.localPosition =
            originalPosition;

        textTransform.localRotation =
            originalRotation;

        currentRoundText.color =
            originalColor;

        SetGlow(originalGlow);

        juiceCoroutine = null;
    }


    // ============================================================
    // GLOW
    // ============================================================

    private void SetGlow(float value)
    {
        if (!useGlow ||
            currentRoundText == null ||
            currentRoundText.fontMaterial == null)
        {
            return;
        }

        currentRoundText.fontMaterial.SetFloat(
            ShaderUtilities.ID_GlowPower,
            value
        );
    }
}
