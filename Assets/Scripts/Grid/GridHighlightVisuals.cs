using System.Collections;
using UnityEngine;

public class GridHighlightVisuals : MonoBehaviour
{
    // ============================================================
    // PLACEMENT
    // ============================================================

    [Header("Placement")]
    [SerializeField] private Color placementColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float placementAlpha = 0.5f;


    // ============================================================
    // PLACEMENT PULSE
    // ============================================================

    [Header("Placement Pulse")]
    [SerializeField] private bool pulsePlacement = true;

    // Makes the placement tile permanently larger than its
    // original size.
    [SerializeField, Min(0f)]
    private float placementBaseScale = 1.15f;

    // Additional scale added during the pulse.
    [SerializeField, Min(0f)]
    private float placementPulseAmount = 0.07f;

    // Pulse speed.
    [SerializeField, Min(0.01f)]
    private float placementPulseSpeed = 5f;


    // ============================================================
    // MOVEMENT
    // ============================================================

    [Header("Movement")]
    [SerializeField] private Color movementColor = Color.cyan;

    [SerializeField, Range(0f, 1f)]
    private float movementAlpha = 0.35f;


    // ============================================================
    // ABILITY RANGE
    // ============================================================

    [Header("Ability Range")]
    [SerializeField] private Color abilityRangeColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float abilityRangeAlpha = 0.35f;


    // ============================================================
    // ENEMY TILE
    // ============================================================

    [Header("Enemy Tile")]
    [SerializeField] private Color enemyTileColor = Color.red;

    [SerializeField, Range(0f, 1f)]
    private float enemyTileAlpha = 0.85f;


    // ============================================================
    // HEAL TILE
    // ============================================================

    [Header("Heal Tile")]
    [SerializeField] private Color healTileColor = Color.green;

    [SerializeField, Range(0f, 1f)]
    private float healTileAlpha = 0.85f;


    // ============================================================
    // ANIMATIONS
    // ============================================================

    [Header("Animations")]
    [SerializeField] private bool pulseAbility = true;

    [SerializeField, Min(0f)]
    private float pulseSpeed = 5f;

    [SerializeField, Range(0f, 1f)]
    private float pulseAmount = 0.2f;

    [SerializeField] private bool animateTileScale = true;

    [SerializeField, Min(0f)]
    private float scaleAmount = 0.06f;

    [SerializeField, Min(0f)]
    private float scaleSpeed = 5f;


    // ============================================================
    // EXPLOSION
    // ============================================================

    [Header("Explosion")]
    [SerializeField] private Color explosionColor = Color.red;

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
    // REFERENCES & STATE
    // ============================================================

    private GameObject tileObject;
    private SpriteRenderer spriteRenderer;
    private Transform tileTransform;

    private Vector3 originalScale;
    private Color originalColor;

    private bool initialized;
    private bool searchAttempted;


    // ============================================================
    // VISUAL STATE
    // ============================================================

    public enum VisualState
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
    // INITIALIZATION & LIFECYCLE
    // ============================================================

    private void Awake()
    {
        Initialize(gameObject);
    }

    private void OnDisable()
    {
        StopExplosionAnimation();
    }

    public void Initialize(GameObject tile)
    {
        if (initialized)
        {
            if (
                spriteRenderer == null &&
                !searchAttempted
            )
            {
                FindRenderer();
            }

            if (tileTransform == null)
            {
                tileTransform =
                    tileObject != null
                        ? tileObject.transform
                        : transform;
            }

            return;
        }

        tileObject =
            tile != null
                ? tile
                : gameObject;

        tileTransform =
            tileObject.transform;

        FindRenderer();

        if (tileTransform != null)
        {
            originalScale =
                tileTransform.localScale;
        }

        originalColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : Color.white;

        initialized = true;
    }


    // ============================================================
    // FIND RENDERER
    // ============================================================

    private void FindRenderer()
    {
        searchAttempted = true;

        if (tileObject == null)
        {
            tileObject = gameObject;
        }

        // Try direct component first.
        spriteRenderer =
            tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return;
        }

        // Search children.
        SpriteRenderer[] renderers =
            tileObject.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (
            renderers == null ||
            renderers.Length == 0
        )
        {
            spriteRenderer = null;
            return;
        }

        // Search for a renderer containing "highlight".
        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            if (
                renderers[i] != null &&
                renderers[i].name
                    .ToLower()
                    .Contains("highlight")
            )
            {
                spriteRenderer =
                    renderers[i];

                return;
            }
        }

        // Fallback.
        spriteRenderer =
            renderers[0];
    }


    // ============================================================
    // UPDATE & ANIMATIONS
    // ============================================================

    private void Update()
    {
        if (!initialized)
        {
            Initialize(gameObject);
        }

        if (
            !initialized ||
            currentState == VisualState.Default
        )
        {
            return;
        }

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (tileTransform == null)
        {
            return;
        }


        // ========================================================
        // PLACEMENT PULSE
        // ========================================================

        if (
            currentState ==
            VisualState.Placement
        )
        {
            if (pulsePlacement)
            {
                float pulse =
                    (
                        Mathf.Sin(
                            Time.time *
                            placementPulseSpeed
                        ) + 1f
                    ) * 0.5f;

                float multiplier =
                    placementBaseScale +
                    pulse *
                    placementPulseAmount;

                tileTransform.localScale =
                    originalScale *
                    multiplier;
            }
            else
            {
                tileTransform.localScale =
                    originalScale *
                    placementBaseScale;
            }

            return;
        }


        // ========================================================
        // NORMAL TILE SCALE ANIMATION
        // ========================================================

        if (animateTileScale)
        {
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


        // ========================================================
        // ABILITY COLOR PULSE
        // ========================================================

        if (
            currentState ==
            VisualState.Ability &&
            pulseAbility &&
            spriteRenderer != null
        )
        {
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
    }


    // ============================================================
    // RESET SCALE
    // ============================================================

    private void ResetScale()
    {
        if (tileTransform != null)
        {
            tileTransform.localScale =
                originalScale;
        }
    }


    // ============================================================
    // STATE HIGHLIGHTS
    // ============================================================

    public void ShowPlacement()
    {
        SetState(
            VisualState.Placement,
            placementColor,
            placementAlpha
        );
    }

    public void ShowMovement()
    {
        ShowMovement(
            movementColor,
            movementAlpha
        );
    }

    public void ShowMovement(
        Color color,
        float alpha)
    {
        SetState(
            VisualState.Movement,
            color,
            alpha
        );
    }

    public void ShowAbility()
    {
        SetState(
            VisualState.Ability,
            abilityRangeColor,
            abilityRangeAlpha
        );
    }

    public void ShowEnemy()
    {
        SetState(
            VisualState.Enemy,
            enemyTileColor,
            enemyTileAlpha
        );
    }

    public void ShowHeal()
    {
        SetState(
            VisualState.Heal,
            healTileColor,
            healTileAlpha
        );
    }


    // ============================================================
    // SET STATE
    // ============================================================

    private void SetState(
        VisualState state,
        Color color,
        float alpha)
    {
        StopExplosionAnimation();

        currentState =
            state;

        ApplyColor(
            color,
            alpha
        );
    }


    // ============================================================
    // APPLY COLOR
    // ============================================================

    private void ApplyColor(
        Color color,
        float alpha)
    {
        if (
            spriteRenderer == null &&
            !searchAttempted
        )
        {
            FindRenderer();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = true;

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

        if (
            spriteRenderer == null &&
            !searchAttempted
        )
        {
            FindRenderer();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;

            spriteRenderer.enabled = true;
        }
    }


    // ============================================================
    // EXPLOSION
    // ============================================================

    public void PlayExplosion()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

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
        if (
            spriteRenderer == null ||
            tileTransform == null
        )
        {
            yield break;
        }

        spriteRenderer.enabled = true;

        Color startingColor =
            spriteRenderer.color;

        Vector3 startingScale =
            tileTransform.localScale;

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

        explosionCoroutine = null;
    }


    // ============================================================
    // STOP EXPLOSION
    // ============================================================

    private void StopExplosionAnimation()
    {
        if (explosionCoroutine != null)
        {
            StopCoroutine(
                explosionCoroutine
            );

            explosionCoroutine = null;
        }
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public Color GetPlacementColor()
    {
        return placementColor;
    }

    public Color GetMovementColor()
    {
        return movementColor;
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

    public bool IsInitialized()
    {
        return initialized;
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        return spriteRenderer;
    }

    public Transform GetTileTransform()
    {
        return tileTransform;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }
}
