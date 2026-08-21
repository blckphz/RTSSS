using System.Collections;
using UnityEngine;

public class GridHighlightVisuals : MonoBehaviour
{
    // ============================================================
    // PLACEMENT
    // ============================================================

    [Header("Placement")]
    [SerializeField]
    private Color placementColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float placementAlpha = 0.5f;


    // ============================================================
    // ABILITY RANGE
    // ============================================================

    [Header("Ability Range")]
    [SerializeField]
    private Color abilityRangeColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float abilityRangeAlpha = 0.35f;


    // ============================================================
    // ENEMY TILE
    // ============================================================

    [Header("Enemy Tile")]
    [SerializeField]
    private Color enemyTileColor = Color.red;

    [SerializeField, Range(0f, 1f)]
    private float enemyTileAlpha = 0.85f;


    // ============================================================
    // HEAL TILE
    // ============================================================

    [Header("Heal Tile")]
    [SerializeField]
    private Color healTileColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float healTileAlpha = 0.85f;


    // ============================================================
    // ANIMATIONS
    // ============================================================

    [Header("Animations")]

    [SerializeField]
    private bool pulseAbility = true;

    [SerializeField, Min(0f)]
    private float pulseSpeed = 5f;

    [SerializeField, Range(0f, 1f)]
    private float pulseAmount = 0.2f;


    [SerializeField]
    private bool animateTileScale = true;

    [SerializeField, Min(0f)]
    private float scaleAmount = 0.06f;

    [SerializeField, Min(0f)]
    private float scaleSpeed = 5f;


    // ============================================================
    // EXPLOSION
    // ============================================================

    [Header("Explosion")]

    [SerializeField]
    private Color explosionColor = Color.red;

    [SerializeField, Min(0f)]
    private float explosionPulseAmount = 0.12f;

    [SerializeField, Min(0.01f)]
    private float explosionPulseDuration = 0.12f;

    [SerializeField]
    private AnimationCurve explosionPulseCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ============================================================
    // REFERENCES
    // ============================================================

    private GameObject tileObject;
    private SpriteRenderer spriteRenderer;
    private Transform tileTransform;

    private Vector3 originalScale;
    private Color originalColor;

    private bool initialized;


    // ============================================================
    // STATE
    // ============================================================

    private enum VisualState
    {
        Default,
        Placement,
        Movement,
        Ability,
        Enemy,
        Heal
    }

    private VisualState currentState =
        VisualState.Default;

    private Coroutine explosionCoroutine;


    // ============================================================
    // INITIALIZATION
    // ============================================================

    public void Initialize(
        GameObject tile
    )
    {
        if (initialized)
        {
            return;
        }

        tileObject =
            tile != null
                ? tile
                : gameObject;

        tileTransform =
            tileObject.transform;

        spriteRenderer =
            tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                tileObject.GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }

        originalScale =
            tileTransform.localScale;

        initialized = true;
    }


    private void Awake()
    {
        Initialize(gameObject);
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        UpdateAnimations();
    }


    private void UpdateAnimations()
    {
        UpdateScaleAnimation();

        if (currentState ==
            VisualState.Ability)
        {
            UpdateAbilityPulse();
        }
    }


    // ============================================================
    // ABILITY PULSE
    // ============================================================

    private void UpdateAbilityPulse()
    {
        if (!pulseAbility ||
            spriteRenderer == null)
        {
            return;
        }

        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    pulseSpeed
                ) + 1f
            ) * 0.5f;

        float alphaMultiplier =
            1f +
            pulse *
            pulseAmount;

        Color color =
            abilityRangeColor;

        color.a =
            Mathf.Clamp01(
                abilityRangeAlpha *
                alphaMultiplier
            );

        spriteRenderer.color =
            color;
    }


    // ============================================================
    // SCALE
    // ============================================================

    private void UpdateScaleAnimation()
    {
        if (!animateTileScale ||
            tileTransform == null)
        {
            return;
        }

        if (currentState ==
            VisualState.Default)
        {
            ResetScale();

            return;
        }

        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    scaleSpeed
                ) + 1f
            ) * 0.5f;

        float multiplier =
            1f +
            pulse *
            scaleAmount;

        tileTransform.localScale =
            originalScale *
            multiplier;
    }


    private void ResetScale()
    {
        if (tileTransform != null)
        {
            tileTransform.localScale =
                originalScale;
        }
    }


    // ============================================================
    // STATES
    // ============================================================

    public void ShowPlacement()
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Placement;

        ApplyColor(
            placementColor,
            placementAlpha
        );
    }


    // ============================================================
    // MOVEMENT
    // ============================================================

    public void ShowMovement(
        Color color,
        float alpha)
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Movement;

        ApplyColor(
            color,
            alpha
        );
    }


    // ============================================================
    // ABILITY
    // ============================================================

    public void ShowAbility()
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Ability;

        ApplyColor(
            abilityRangeColor,
            abilityRangeAlpha
        );
    }


    // ============================================================
    // ENEMY
    // ============================================================

    public void ShowEnemy()
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Enemy;

        ApplyColor(
            enemyTileColor,
            enemyTileAlpha
        );
    }


    // ============================================================
    // HEAL
    // ============================================================

    public void ShowHeal()
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Heal;

        ApplyColor(
            healTileColor,
            healTileAlpha
        );
    }


    // ============================================================
    // COLOR
    // ============================================================

    private void ApplyColor(
        Color color,
        float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        color.a =
            Mathf.Clamp01(alpha);

        spriteRenderer.color =
            color;
    }


    // ============================================================
    // RESET
    // ============================================================

    public void Reset()
    {
        StopExplosionAnimation();

        currentState =
            VisualState.Default;

        ResetScale();

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }
    }


    // ============================================================
    // EXPLOSION
    // ============================================================

    public void PlayExplosion()
    {
        if (!initialized)
        {
            Initialize(gameObject);
        }

        StopExplosionAnimation();

        explosionCoroutine =
            StartCoroutine(
                ExplosionRoutine()
            );
    }


    private IEnumerator ExplosionRoutine()
    {
        if (spriteRenderer == null ||
            tileTransform == null)
        {
            yield break;
        }

        Color startingColor =
            spriteRenderer.color;

        Vector3 startingScale =
            originalScale;

        float elapsed = 0f;

        while (
            elapsed <
            explosionPulseDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    explosionPulseDuration
                );

            float curveValue =
                explosionPulseCurve.Evaluate(
                    normalizedTime
                );

            float pulse =
                Mathf.Sin(
                    curveValue *
                    Mathf.PI
                );

            tileTransform.localScale =
                startingScale *
                (
                    1f +
                    pulse *
                    explosionPulseAmount
                );

            Color color =
                Color.Lerp(
                    startingColor,
                    explosionColor,
                    pulse
                );

            color.a =
                Mathf.Lerp(
                    startingColor.a,
                    explosionColor.a,
                    pulse
                );

            spriteRenderer.color =
                color;

            yield return null;
        }

        tileTransform.localScale =
            startingScale;

        spriteRenderer.color =
            startingColor;

        explosionCoroutine =
            null;
    }


    private void StopExplosionAnimation()
    {
        if (explosionCoroutine != null)
        {
            StopCoroutine(
                explosionCoroutine
            );

            explosionCoroutine =
                null;
        }

        ResetScale();
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public Color GetPlacementColor()
    {
        return placementColor;
    }

    public Color GetAbilityColor()
    {
        return abilityRangeColor;
    }

    public Color GetEnemyColor()
    {
        return enemyTileColor;
    }

    public Color GetHealColor()
    {
        return healTileColor;
    }
}