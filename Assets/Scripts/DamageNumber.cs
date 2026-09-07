using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DamageNumber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Movement")]
    [SerializeField] private float upwardForce = 3f;
    [SerializeField] private float sidewaysForce = 1f;

    [Header("Random Trajectory")]
    [SerializeField] private float minHorizontalForce = -1f;
    [SerializeField] private float maxHorizontalForce = 1f;

    [Header("Lifetime")]
    [SerializeField] private float duration = 0.8f;

    [Header("Fade")]
    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Rigidbody2D rb;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Prevent physics from rotating the damage number.
        rb.freezeRotation = true;

        // Disable gravity for floating damage text.
        rb.gravityScale = 0f;
    }

    public void Setup(int damage)
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (damageText != null)
        {
            damageText.text = damage.ToString();

            Color color = damageText.color;
            color.a = 1f;
            damageText.color = color;
        }

        timer = 0f;

        // Stop previous movement.
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Random left/right direction.
        float randomHorizontal =
            Random.Range(minHorizontalForce, maxHorizontalForce);

        // Apply upward + sideways trajectory.
        Vector2 force = new Vector2(
            randomHorizontal * sidewaysForce,
            upwardForce
        );

        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = duration <= 0f
            ? 1f
            : Mathf.Clamp01(timer / duration);

        // Fade text.
        if (damageText != null)
        {
            Color color = damageText.color;
            color.a = fadeCurve.Evaluate(t);
            damageText.color = color;
        }

        // Destroy when finished.
        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}