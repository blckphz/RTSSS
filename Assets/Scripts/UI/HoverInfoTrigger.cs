using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoverInfoTrigger : MonoBehaviour, ICharacterHolder
{
    // ============================================================
    // TOOLTIP CONTENT
    // ============================================================

    [Header("Tooltip Content")]
    [TextArea]
    [SerializeField]
    private string hoverMessage = "Default Prefab Info";

    public string HoverMessage => hoverMessage;


    // ============================================================
    // CLICK FEEDBACK
    // ============================================================

    [Header("Click Feedback")]
    [SerializeField]
    private float selectedScale = 1.1f;

    [SerializeField]
    private float scaleSpeed = 10f;


    // ============================================================
    // SCREEN SHAKE
    // ============================================================

    [Header("Screen Shake")]
    [SerializeField]
    private bool enableScreenShake = true;

    [SerializeField]
    private float screenShakeDuration = 0.15f;

    [SerializeField]
    private float screenShakeMagnitude = 0.5f;


    // ============================================================
    // HOVER SCREEN SHAKE
    // ============================================================

    [Header("Hover Screen Shake")]
    [SerializeField]
    private bool enableHoverScreenShake = true;

    [SerializeField]
    private float hoverScreenShakeDuration = 0.05f;

    [SerializeField]
    private float hoverScreenShakeMagnitude = 0.08f;


    // ============================================================
    // NORMAL HOVER SCALE
    // ============================================================

    [Header("Normal Hover Scale")]
    [SerializeField]
    private bool enableHoverScale = true;

    [SerializeField, Range(0f, 0.2f)]
    private float hoverScaleAmount = 0.05f;

    [SerializeField, Min(0.01f)]
    private float hoverScaleSmoothTime = 0.08f;


    // ============================================================
    // ABILITY TARGET SCALE
    // ============================================================

    [Header("Ability Target Scale")]
    [SerializeField]
    private bool enableAbilityTargetScale = true;

    [SerializeField, Range(0f, 0.25f)]
    private float abilityTargetScaleAmount = 0.06f;

    [SerializeField, Min(0.01f)]
    private float abilityTargetScaleSmoothTime = 0.08f;


    // ============================================================
    // HOVER OUTLINE
    // ============================================================

    [Header("Hover Outline")]
    [SerializeField]
    private SpriteRenderer hoverShaderSprite;

    [SerializeField]
    private Color hoverOutlineColor = Color.white;

    [SerializeField]
    private string hoverShaderObjectName = "HoverShaderSprite";

    [SerializeField]
    private bool enableHoverOutline = true;


    // ============================================================
    // SELECTED VISUAL
    // ============================================================

    [Header("Selected Visual")]
    [SerializeField]
    private SpriteRenderer selectedChildSprite;


    // ============================================================
    // REFERENCES
    // ============================================================

    private Vector3 originalScale;

    private bool isSelected;
    private bool isHovered;
    private bool isAbilityTargetHovered;

    private AttackUnit attackUnit;

    private CanvasInfoManager canvasInfoManager;
    private GridHighlightManager gridHighlightManager;
    private AudioFXManager audioFXManager;


    // ============================================================
    // SCALE STATE
    // ============================================================

    private float currentHoverScale;
    private float hoverScaleVelocity;

    private float currentAbilityTargetScale;
    private float abilityTargetScaleVelocity;


    // ============================================================
    // HOVER OUTLINE STATE
    // ============================================================

    private MaterialPropertyBlock hoverPropertyBlock;

    private static readonly int OutlineColorID =
        Shader.PropertyToID("_OutlineColor");


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        attackUnit = GetComponent<AttackUnit>();

        originalScale =
            transform.localScale;

        audioFXManager =
            AudioFXManager.Instance;

        hoverPropertyBlock =
            new MaterialPropertyBlock();

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = false;
        }

        SetupHoverOutline();

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
    // HOVER OUTLINE SETUP
    // ============================================================

    private void SetupHoverOutline()
    {
        if (hoverShaderSprite == null)
        {
            Transform hoverShader =
                transform.Find(hoverShaderObjectName);

            if (hoverShader != null)
            {
                hoverShaderSprite =
                    hoverShader.GetComponent<SpriteRenderer>();
            }
        }

        if (hoverShaderSprite == null)
        {
            return;
        }

        hoverShaderSprite.enabled = false;

        hoverShaderSprite.GetPropertyBlock(
            hoverPropertyBlock
        );

        hoverPropertyBlock.SetColor(
            OutlineColorID,
            hoverOutlineColor
        );

        hoverShaderSprite.SetPropertyBlock(
            hoverPropertyBlock
        );
    }


    // ============================================================
    // HOVER OUTLINE
    // ============================================================

    private void UpdateHoverOutline()
    {
        if (
            hoverShaderSprite == null ||
            !enableHoverOutline
        )
        {
            return;
        }

        hoverShaderSprite.enabled =
            isHovered || isSelected;
    }


    // ============================================================
    // HOVER MANAGER CALLBACK
    // ============================================================

    public void SetHoveredFromManager(bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        UpdateHoverOutline();

        if (hovered)
        {
            OnHoverEnter();
        }
        else
        {
            OnHoverExit();
        }
    }


    // ============================================================
    // HOVER ENTER
    // ============================================================

    private void OnHoverEnter()
    {
        if (audioFXManager == null)
        {
            audioFXManager =
                AudioFXManager.Instance;
        }

        if (audioFXManager != null)
        {
            audioFXManager.PlayUnitHover();
        }

        if (
            enableHoverScreenShake &&
            ScreenShaker.Instance != null
        )
        {
            ScreenShaker.Instance.Shake(
                hoverScreenShakeMagnitude,
                hoverScreenShakeDuration
            );
        }

        if (canvasInfoManager != null)
        {
            canvasInfoManager.ShowCharacter(this);
        }

        UpdateAbilityTargetHover();
    }


    // ============================================================
    // HOVER EXIT
    // ============================================================

    private void OnHoverExit()
    {
        isAbilityTargetHovered = false;

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

        if (
            !isSelected &&
            canvasInfoManager != null
        )
        {
            canvasInfoManager.ClearInfo();
        }
    }


    // ============================================================
    // SCALE
    // ============================================================

    private void UpdateScale()
    {
        float baseScale =
            isSelected
                ? selectedScale
                : 1f;

        float targetHoverScale =
            isHovered && enableHoverScale
                ? hoverScaleAmount
                : 0f;

        currentHoverScale =
            Mathf.SmoothDamp(
                currentHoverScale,
                targetHoverScale,
                ref hoverScaleVelocity,
                hoverScaleSmoothTime
            );

        float targetAbilityScale =
            isAbilityTargetHovered &&
            enableAbilityTargetScale
                ? abilityTargetScaleAmount
                : 0f;

        currentAbilityTargetScale =
            Mathf.SmoothDamp(
                currentAbilityTargetScale,
                targetAbilityScale,
                ref abilityTargetScaleVelocity,
                abilityTargetScaleSmoothTime
            );

        float finalScale =
            baseScale +
            currentHoverScale +
            currentAbilityTargetScale;

        Vector3 targetScale =
            originalScale * finalScale;

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );
    }


    // ============================================================
    // ABILITY TARGET HOVER
    // ============================================================

    private void UpdateAbilityTargetHover()
    {
        isAbilityTargetHovered = false;

        if (!enableAbilityTargetScale)
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

        isAbilityTargetHovered =
            gridHighlightManager.IsValidCurrentAbilityTarget(
                gameObject
            );
    }


    // ============================================================
    // SELECTION
    // ============================================================

    public bool IsSelected()
    {
        return isSelected;
    }


    public void SetSelected(bool selected)
    {
        bool becomingSelected =
            selected && !isSelected;

        bool becomingDeselected =
            !selected && isSelected;

        if (
            audioFXManager == null &&
            (becomingSelected || becomingDeselected)
        )
        {
            audioFXManager =
                AudioFXManager.Instance;
        }

        if (becomingSelected)
        {
            audioFXManager?.PlayUnitClick();

            if (
                enableScreenShake &&
                ScreenShaker.Instance != null
            )
            {
                ScreenShaker.Instance.Shake(
                    screenShakeMagnitude,
                    screenShakeDuration
                );
            }
        }

        if (becomingDeselected)
        {
            audioFXManager?.PlayUnitDeselect();
        }

        isSelected = selected;

        UpdateHoverOutline();

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = selected;
        }

        if (canvasInfoManager != null)
        {
            if (selected)
            {
                canvasInfoManager.ShowCharacter(this);
            }
            else
            {
                canvasInfoManager.ClearInfo();
            }
        }
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        isSelected = false;
        isHovered = false;
        isAbilityTargetHovered = false;

        currentHoverScale = 0f;
        hoverScaleVelocity = 0f;

        currentAbilityTargetScale = 0f;
        abilityTargetScaleVelocity = 0f;

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = false;
        }

        if (hoverShaderSprite != null)
        {
            hoverShaderSprite.enabled = false;
        }

        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(this);
        }
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(this);
        }
    }


    // ============================================================
    // CHARACTER DATA
    // ============================================================

    public CharacterSO GetCharacterData()
    {
        return attackUnit != null
            ? attackUnit.GetCharacterData()
            : null;
    }


    // ============================================================
    // ATTACK UNIT
    // ============================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }


    // ============================================================
    // ABILITIES
    // ============================================================

    public int GetAbilityCooldown(AbilitySO ability)
    {
        return attackUnit != null
            ? attackUnit.GetAbilityCooldown(ability)
            : -1;
    }


    public bool IsAbilityOnCooldown(AbilitySO ability)
    {
        return attackUnit != null &&
               attackUnit.IsAbilityOnCooldown(ability);
    }


    public int GetAbilityUsesRemaining(AbilitySO ability)
    {
        return attackUnit != null
            ? attackUnit.GetAbilityUsesRemaining(ability)
            : -1;
    }


    public bool IsAbilityReady(AbilitySO ability)
    {
        return attackUnit != null &&
               attackUnit.IsAbilityReady(ability);
    }
}
