
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DamageNumber : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField] private TextMeshProUGUI damageText;


    // ==================================================
    // NUMBER TYPE
    // ==================================================

    [Header("Number Type")]
    [SerializeField] private bool isHealNumber = false;


    // ==================================================
    // COLORS
    // ==================================================

    [Header("Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;


    // ==================================================
    // JUICY POP
    // ==================================================

    [Header("Juicy Pop")]
    [Tooltip("How long the entrance animation takes.")]
    [SerializeField] private float popDuration = 0.24f;

    [Tooltip("Maximum scale reached during the pop.")]
    [SerializeField] private float popOvershoot = 1.22f;

    [Tooltip("Subtle squash and stretch intensity.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float squashAmount = 0.04f;

    [Tooltip("Small random rotation that smoothly settles.")]
    [SerializeField] private float popRotation = 4f;


    // ==================================================
    // PAUSE
    // ==================================================

    [Header("Pause")]
    [SerializeField] private float pauseDuration = 1f;


    // ==================================================
    // DAMAGE FLOAT
    // ==================================================

    [Header("Damage Float")]
    [SerializeField] private float damageRiseDistance = 0.85f;

    [SerializeField] private float damageSidewaysDistance = 0.35f;

    [Tooltip("Subtle floating movement.")]
    [SerializeField] private float floatWobbleDistance = 0.035f;

    [SerializeField] private float floatWobbleCycles = 1.25f;


    // ==================================================
    // HEAL FLOAT
    // ==================================================

    [Header("Heal Float")]
    [SerializeField] private float healHorizontalDistance = 0.25f;

    [SerializeField] private float healHorizontalSpeed = 3f;

    [SerializeField] private float healVerticalSpeed = 1.5f;


    // ==================================================
    // MOVEMENT TIMING
    // ==================================================

    [Header("Movement Timing")]
    [SerializeField] private float movementDuration = 0.85f;


    // ==================================================
    // FADE
    // ==================================================

    [Header("Fade")]
    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f, 1f,
            1f, 0f
        );


    // ==================================================
    // PRIVATE
    // ==================================================

    private Rigidbody2D rb;

    private float timer;
    private float movementTimer;

    private float randomHorizontal;
    private float randomRotation;
    private float healPingPongOffset;

    private Vector3 startPosition;
    private Vector3 originalScale;

    private Quaternion originalRotation;

    private bool movementStarted;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (damageText == null)
        {
            damageText =
                GetComponentInChildren<TextMeshProUGUI>();
        }

        if (damageText != null)
        {
            originalScale =
                damageText.transform.localScale;

            originalRotation =
                damageText.transform.localRotation;
        }
    }


    // ==================================================
    // SETUP
    // ==================================================

    public void Setup(int amount)
    {
        if (damageText == null)
        {
            damageText =
                GetComponentInChildren<TextMeshProUGUI>();
        }

        if (damageText != null)
        {
            damageText.text = isHealNumber
                ? "+" + amount
                : "-" + amount;

            damageText.color = isHealNumber
                ? healColor
                : damageColor;

            Color color = damageText.color;
            color.a = 1f;
            damageText.color = color;

            originalScale =
                damageText.transform.localScale;

            originalRotation =
                damageText.transform.localRotation;

            // Start invisible in scale.
            damageText.transform.localScale =
                Vector3.zero;

            randomRotation =
                Random.Range(-popRotation, popRotation);

            damageText.transform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, randomRotation);
        }

        timer = 0f;
        movementTimer = 0f;

        movementStarted = false;

        startPosition = transform.position;

        randomHorizontal =
            Random.Range(-1f, 1f);

        healPingPongOffset =
            Random.Range(0f, Mathf.PI * 2f);

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        timer += Time.deltaTime;


        // ==================================================
        // JUICY POP
        // ==================================================

        if (timer < popDuration)
        {
            AnimatePop();
            return;
        }

        // Ensure the final pop state is exact.
        if (damageText != null)
        {
            damageText.transform.localScale =
                originalScale;

            damageText.transform.localRotation =
                originalRotation;
        }


        // ==================================================
        // STATIONARY PAUSE
        // ==================================================

        if (timer < popDuration + pauseDuration)
        {
            transform.position = startPosition;
            return;
        }


        // ==================================================
        // START FLOATING
        // ==================================================

        if (!movementStarted)
        {
            movementStarted = true;
            movementTimer = 0f;
        }

        movementTimer += Time.deltaTime;

        float t = movementDuration <= 0f
            ? 1f
            : Mathf.Clamp01(
                movementTimer / movementDuration
            );

        // Smooth deceleration.
        float easedT = EaseOutCubic(t);


        // ==================================================
        // DAMAGE MOVEMENT
        // ==================================================

        if (!isHealNumber)
        {
            float horizontal =
                randomHorizontal *
                damageSidewaysDistance *
                easedT;

            float wobble =
                Mathf.Sin(
                    t * Mathf.PI * 2f * floatWobbleCycles
                ) *
                floatWobbleDistance *
                (1f - t);

            float vertical =
                damageRiseDistance * easedT;

            transform.position =
                startPosition +
                new Vector3(
                    horizontal + wobble,
                    vertical,
                    0f
                );
        }


        // ==================================================
        // HEAL MOVEMENT
        // ==================================================

        else
        {
            float elapsed =
                movementTimer;

            float vertical =
                healVerticalSpeed * elapsed;

            float currentWave =
                Mathf.Sin(
                    healPingPongOffset +
                    elapsed * healHorizontalSpeed
                );

            float startingWave =
                Mathf.Sin(healPingPongOffset);

            float horizontal =
                (currentWave - startingWave) *
                healHorizontalDistance;

            transform.position =
                startPosition +
                new Vector3(
                    horizontal,
                    vertical,
                    0f
                );
        }


        // ==================================================
        // FADE
        // ==================================================

        if (damageText != null)
        {
            Color color = damageText.color;

            color.a = fadeCurve.Evaluate(t);

            damageText.color = color;
        }


        // ==================================================
        // DESTROY
        // ==================================================

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }


    // ==================================================
    // POP ANIMATION
    // ==================================================

    private void AnimatePop()
    {
        if (damageText == null)
            return;

        float t = popDuration <= 0f
            ? 1f
            : Mathf.Clamp01(timer / popDuration);

        float scale;

        // First phase: quickly grow beyond normal size.
        if (t < 0.68f)
        {
            float phase = t / 0.68f;

            float eased = EaseOutCubic(phase);

            scale = Mathf.Lerp(
                0f,
                popOvershoot,
                eased
            );
        }

        // Second phase: smoothly settle to normal size.
        else
        {
            float phase =
                (t - 0.68f) / 0.32f;

            float eased = Mathf.SmoothStep(
                0f,
                1f,
                phase
            );

            scale = Mathf.Lerp(
                popOvershoot,
                1f,
                eased
            );
        }

        // Gentle squash and stretch.
        float squash =
            Mathf.Sin(t * Mathf.PI) *
            squashAmount;

        float scaleX =
            scale * (1f - squash);

        float scaleY =
            scale * (1f + squash);

        damageText.transform.localScale =
            Vector3.Scale(
                originalScale,
                new Vector3(scaleX, scaleY, 1f)
            );

        // Smoothly remove the initial rotation.
        float rotationAmount =
            1f - EaseOutCubic(t);

        damageText.transform.localRotation =
            originalRotation *
            Quaternion.Euler(
                0f,
                0f,
                randomRotation * rotationAmount
            );
    }


    // ==================================================
    // EASING
    // ==================================================

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
