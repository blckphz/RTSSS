using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoverInfoTrigger :
    MonoBehaviour,
    ICharacterHolder
{
    [Header("Tooltip Content")]
    [TextArea]
    [SerializeField]
    private string hoverMessage =
        "Default Prefab Info";

    public string HoverMessage =>
        hoverMessage;


    // ============================================================
    // CLICK FEEDBACK
    // ============================================================

    [Header("Click Feedback")]
    [SerializeField]
    private float selectedScale = 1.1f;

    [SerializeField]
    private float scaleSpeed = 10f;


    // ============================================================
    // ABILITY TARGET PULSE
    // ============================================================

    [Header("Ability Target Pulse")]
    [Tooltip(
        "Enables the subtle scale pulse when hovering a valid target of the selected ability."
    )]
    [SerializeField]
    private bool enableAbilityTargetPulse = true;

    [Tooltip(
        "Maximum additional scale during the pulse."
    )]
    [SerializeField, Range(0f, 0.15f)]
    private float abilityTargetPulseAmount = 0.035f;

    [Tooltip(
        "Speed of the pulse."
    )]
    [SerializeField, Min(0f)]
    private float abilityTargetPulseSpeed = 5f;

    [Tooltip(
        "How smoothly the pulse turns on and off."
    )]
    [SerializeField, Min(0.01f)]
    private float abilityTargetPulseSmoothTime = 0.08f;


    // ============================================================
    // SELECTED VISUAL
    // ============================================================

    [Header("Selected Visual")]
    [Tooltip(
        "SpriteRenderer on the child object that appears when selected."
    )]
    [SerializeField]
    private SpriteRenderer selectedChildSprite;


    // ============================================================
    // REFERENCES
    // ============================================================

    private Vector3 originalScale;

    private bool isSelected;

    private bool isAbilityTargetHovered;

    private SpriteRenderer spriteRenderer;
    private Collider2D collider2D;

    private AttackUnit attackUnit;

    private CanvasInfoManager canvasInfoManager;
    private GridHighlightManager gridHighlightManager;


    // ============================================================
    // PULSE STATE
    // ============================================================

    private float currentPulseScale;
    private float pulseVelocity;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        collider2D =
            GetComponent<Collider2D>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        attackUnit =
            GetComponent<AttackUnit>();

        originalScale =
            transform.localScale;


        if (collider2D == null)
        {
            Debug.LogError(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} is missing Collider2D!",
                this
            );
        }


        if (attackUnit == null)
        {
            Debug.LogError(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} is missing AttackUnit!",
                this
            );
        }


        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled =
                false;
        }


        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();

        gridHighlightManager =
            FindFirstObjectByType<GridHighlightManager>();
    }


    private void Update()
    {
        UpdateScale();
    }


    // ============================================================
    // SCALE
    // ============================================================

    private void UpdateScale()
    {
        // --------------------------------------------------------
        // SELECTED SCALE
        // --------------------------------------------------------

        float baseScale =
            isSelected
                ? selectedScale
                : 1f;


        // --------------------------------------------------------
        // TARGET PULSE
        // --------------------------------------------------------

        float targetPulse =
            0f;

        if (isAbilityTargetHovered &&
            enableAbilityTargetPulse)
        {
            float pulse =
                (Mathf.Sin(
                    Time.time *
                    abilityTargetPulseSpeed
                ) + 1f) * 0.5f;

            targetPulse =
                pulse *
                abilityTargetPulseAmount;
        }


        // --------------------------------------------------------
        // SMOOTH PULSE
        // --------------------------------------------------------

        currentPulseScale =
            Mathf.SmoothDamp(
                currentPulseScale,
                targetPulse,
                ref pulseVelocity,
                abilityTargetPulseSmoothTime
            );


        // --------------------------------------------------------
        // FINAL SCALE
        // --------------------------------------------------------

        float finalScale =
            baseScale +
            currentPulseScale;


        Vector3 targetScale =
            originalScale *
            finalScale;


        // --------------------------------------------------------
        // SMOOTH SCALE
        // --------------------------------------------------------

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime *
                scaleSpeed
            );
    }


    // ============================================================
    // HOVER ENTER
    // ============================================================

    private void OnMouseEnter()
    {
        if (canvasInfoManager != null)
        {
            canvasInfoManager.ShowCharacter(
                this
            );
        }


        UpdateAbilityTargetHover();
    }


    // ============================================================
    // HOVER EXIT
    // ============================================================

    private void OnMouseExit()
    {
        isAbilityTargetHovered =
            false;


        if (UIManager.CurrentSelection != null)
        {
            if (canvasInfoManager != null)
            {
                canvasInfoManager.ShowCharacter(
                    UIManager.CurrentSelection
                );
            }

            return;
        }


        if (!isSelected &&
            canvasInfoManager != null)
        {
            canvasInfoManager.ClearInfo();
        }
    }


    // ============================================================
    // ABILITY TARGET HOVER
    // ============================================================

    private void UpdateAbilityTargetHover()
    {
        isAbilityTargetHovered =
            false;


        if (!enableAbilityTargetPulse)
        {
            return;
        }


        if (canvasInfoManager == null)
        {
            return;
        }


        if (!canvasInfoManager.HasSelectedAbility())
        {
            return;
        }


        if (gridHighlightManager == null)
        {
            gridHighlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }


        if (gridHighlightManager == null)
        {
            return;
        }


        if (gridHighlightManager.IsValidCurrentAbilityTarget(
                gameObject))
        {
            isAbilityTargetHovered =
                true;
        }
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        isSelected = false;

        isAbilityTargetHovered = false;

        currentPulseScale = 0f;
        pulseVelocity = 0f;


        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled =
                false;
        }


        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(
                this
            );
        }
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(
                this
            );
        }
    }


    // ============================================================
    // CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null)
        {
            return null;
        }


        CharacterSO characterData =
            attackUnit.GetCharacterData();


        if (characterData == null)
        {
            Debug.LogError(
                $"[HoverInfoTrigger] " +
                $"AttackUnit on {gameObject.name} " +
                "has no CharacterSO assigned!",
                this
            );
        }


        return characterData;
    }


    // ============================================================
    // ATTACK UNIT
    // ============================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }


    // ============================================================
    // SELECTION
    // ============================================================

    public bool IsSelected()
    {
        return isSelected;
    }


    public void SetSelected(
        bool selected)
    {
        isSelected =
            selected;


        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled =
                selected;
        }


        if (canvasInfoManager != null)
        {
            if (selected)
            {
                canvasInfoManager.ShowCharacter(
                    this
                );
            }
            else
            {
                canvasInfoManager.ClearInfo();
            }
        }
    }
}