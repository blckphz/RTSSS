using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DamageNumber : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField]
    private TextMeshProUGUI damageText;


    // ==================================================
    // NUMBER TYPE
    // ==================================================

    [Header("Number Type")]
    [Tooltip(
        "OFF = Damage Number\n" +
        "ON = Heal Number"
    )]
    [SerializeField]
    private bool isHealNumber = false;


    // ==================================================
    // COLORS
    // ==================================================

    [Header("Colors")]
    [SerializeField]
    private Color damageColor = Color.red;

    [SerializeField]
    private Color healColor = Color.green;


    // ==================================================
    // DAMAGE MOVEMENT
    // ==================================================

    [Header("Damage Movement")]
    [SerializeField]
    private float upwardForce = 3f;

    [SerializeField]
    private float sidewaysForce = 1f;


    // ==================================================
    // RANDOM DAMAGE TRAJECTORY
    // ==================================================

    [Header("Random Damage Trajectory")]
    [SerializeField]
    private float minHorizontalForce = -1f;

    [SerializeField]
    private float maxHorizontalForce = 1f;


    // ==================================================
    // HEAL MOVEMENT
    // ==================================================

    [Header("Heal Movement")]
    [Tooltip(
        "How far the heal number moves left and right."
    )]
    [SerializeField]
    private float healHorizontalDistance = 0.25f;

    [Tooltip(
        "How quickly the heal number moves left and right."
    )]
    [SerializeField]
    private float healHorizontalSpeed = 3f;

    [Tooltip(
        "How quickly the heal number moves upward."
    )]
    [SerializeField]
    private float healVerticalSpeed = 1.5f;


    // ==================================================
    // LIFETIME
    // ==================================================

    [Header("Lifetime")]
    [SerializeField]
    private float duration = 0.8f;


    // ==================================================
    // FADE
    // ==================================================

    [Header("Fade")]
    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f,
            1f,
            1f,
            0f
        );


    // ==================================================
    // PRIVATE
    // ==================================================

    private Rigidbody2D rb;

    private float timer;

    private Vector3 healStartPosition;

    private float healPingPongOffset;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        // Prevent physics from rotating
        // the number.
        rb.freezeRotation = true;

        // Disable gravity.
        rb.gravityScale = 0f;
    }


    // ==================================================
    // SETUP
    // ==================================================

    public void Setup(
        int amount)
    {
        if (damageText == null)
        {
            damageText =
                GetComponentInChildren<
                    TextMeshProUGUI
                >();
        }

        if (damageText != null)
        {
            // ==================================================
            // HEAL NUMBER
            // ==================================================

            if (isHealNumber)
            {
                damageText.text =
                    "+" +
                    amount;

                damageText.color =
                    healColor;
            }

            // ==================================================
            // DAMAGE NUMBER
            // ==================================================

            else
            {
                damageText.text =
                    "-" +
                    amount;

                damageText.color =
                    damageColor;
            }

            Color color =
                damageText.color;

            color.a = 1f;

            damageText.color =
                color;
        }

        timer = 0f;

        // Stop previous movement.
        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;


        // ==================================================
        // HEAL MOVEMENT
        // ==================================================

        if (isHealNumber)
        {
            healStartPosition =
                transform.position;

            healPingPongOffset =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

            // Heal movement is handled manually.
            rb.bodyType =
                RigidbodyType2D.Kinematic;

            rb.linearVelocity =
                Vector2.zero;

            return;
        }


        // ==================================================
        // DAMAGE MOVEMENT
        // ==================================================

        rb.bodyType =
            RigidbodyType2D.Dynamic;

        float randomHorizontal =
            UnityEngine.Random.Range(
                minHorizontalForce,
                maxHorizontalForce
            );

        Vector2 force =
            new Vector2(
                randomHorizontal *
                sidewaysForce,
                upwardForce
            );

        rb.AddForce(
            force,
            ForceMode2D.Impulse
        );
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        timer +=
            Time.deltaTime;

        float t =
            duration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    timer /
                    duration
                );


        // ==================================================
        // HEAL MOVEMENT
        // ==================================================

        if (isHealNumber)
        {
            // Move straight upward.
            float verticalMovement =
                healVerticalSpeed *
                timer;

            // Gentle left/right ping-pong.
            float horizontalMovement =
                Mathf.Sin(
                    (
                        timer *
                        healHorizontalSpeed
                    ) +
                    healPingPongOffset
                ) *
                healHorizontalDistance;

            transform.position =
                healStartPosition +
                new Vector3(
                    horizontalMovement,
                    verticalMovement,
                    0f
                );
        }


        // ==================================================
        // FADE
        // ==================================================

        if (damageText != null)
        {
            Color color =
                damageText.color;

            color.a =
                fadeCurve.Evaluate(
                    t
                );

            damageText.color =
                color;
        }


        // ==================================================
        // DESTROY
        // ==================================================

        if (t >= 1f)
        {
            Destroy(
                gameObject
            );
        }
    }
}