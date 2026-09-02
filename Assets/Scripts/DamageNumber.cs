using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TextMeshProUGUI damageText;

    [Header("Animation")]
    [SerializeField]
    private float moveDistance = 1f;

    [SerializeField]
    private float duration = 0.6f;

    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);


    private Vector3 startPosition;

    private float timer;


    public void Setup(
        int damage)
    {
        if (damageText == null)
        {
            damageText =
                GetComponentInChildren<TextMeshProUGUI>();
        }


        if (damageText != null)
        {
            damageText.text =
                damage.ToString();

            Color color =
                damageText.color;

            color.a = 1f;

            damageText.color =
                color;
        }


        startPosition =
            transform.position;

        timer = 0f;
    }


    private void Update()
    {
        timer +=
            Time.deltaTime;


        float t =
            duration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    timer / duration
                );


        // --------------------------------------------------
        // MOVE UP
        // --------------------------------------------------

        float movement =
            movementCurve.Evaluate(t)
            * moveDistance;


        transform.position =
            startPosition
            + Vector3.up * movement;


        // --------------------------------------------------
        // FADE
        // --------------------------------------------------

        if (damageText != null)
        {
            Color color =
                damageText.color;

            color.a =
                fadeCurve.Evaluate(t);

            damageText.color =
                color;
        }


        // --------------------------------------------------
        // DESTROY
        // --------------------------------------------------

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}