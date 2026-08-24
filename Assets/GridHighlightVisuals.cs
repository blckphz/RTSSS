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
    // MOVEMENT
    // ============================================================

    [Header("Movement")]
    [SerializeField]
    private Color movementColor = Color.cyan;

    [SerializeField, Range(0f, 1f)]
    private float movementAlpha = 0.35f;


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
            /*
             * The board can be rotated or rebuilt.
             * Make sure references are still valid.
             */

            if (spriteRenderer == null)
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


        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
        else
        {
            originalColor =
                Color.white;
        }


        initialized = true;
    }


    private void Awake()
    {
        Initialize(gameObject);
    }


    // ============================================================
    // FIND RENDERER
    // ============================================================

    private void FindRenderer()
    {
        if (tileObject == null)
        {
            tileObject = gameObject;
        }


        /*
         * First try the tile itself.
         */

        spriteRenderer =
            tileObject.GetComponent<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            return;
        }


        /*
         * Then search children, including inactive children.
         */

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


        /*
         * Prefer a child named Highlight.
         */

        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            if (
                renderers[i] != null &&
                (
                    renderers[i].name.Contains(
                        "Highlight"
                    ) ||
                    renderers[i].name.Contains(
                        "highlight"
                    )
                )
            )
            {
                spriteRenderer =
                    renderers[i];

                return;
            }
        }


        /*
         * Otherwise use the first SpriteRenderer.
         */

        spriteRenderer =
            renderers[0];
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (!initialized)
        {
            Initialize(gameObject);
        }


        if (!initialized)
        {
            return;
        }


        UpdateAnimations();
    }


    // ============================================================
    // ANIMATIONS
    // ============================================================

    private void UpdateAnimations()
    {
        UpdateScaleAnimation();


        if (
            currentState ==
            VisualState.Ability
        )
        {
            UpdateAbilityPulse();
        }
    }


    // ============================================================
    // ABILITY PULSE
    // ============================================================

    private void UpdateAbilityPulse()
    {
        if (
            !pulseAbility ||
            spriteRenderer == null
        )
        {
            return;
        }


        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    pulseSpeed
                ) +
                1f
            ) *
            0.5f;


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
        if (
            !animateTileScale ||
            tileTransform == null
        )
        {
            return;
        }


        if (
            currentState ==
            VisualState.Default
        )
        {
            ResetScale();

            return;
        }


        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    scaleSpeed
                ) +
                1f
            ) *
            0.5f;


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
    // PLACEMENT
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

    public void ShowMovement()
    {
        ShowMovement(
            movementColor,
            movementAlpha
        );
    }


    public void ShowMovement(
        Color color,
        float alpha
    )
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
        float alpha
    )
    {
        if (spriteRenderer == null)
        {
            FindRenderer();
        }


        if (spriteRenderer == null)
        {
            return;
        }


        /*
         * Every highlight state explicitly enables
         * the renderer.
         */

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


        if (spriteRenderer == null)
        {
            FindRenderer();
        }


        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;


            /*
             * Do not disable the renderer.
             * Resetting the color is enough.
             */

            spriteRenderer.enabled = true;
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
        if (
            explosionCoroutine != null
        )
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
        if (spriteRenderer == null)
        {
            FindRenderer();
        }


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