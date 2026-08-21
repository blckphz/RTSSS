using UnityEngine;
using UnityEngine.InputSystem;

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
    // NORMAL HOVER SCALE
    // ============================================================

    [Header("Normal Hover Scale")]
    [Tooltip(
        "Enables the small scale increase while the mouse is hovering over the unit."
    )]
    [SerializeField]
    private bool enableHoverScale = true;

    [Tooltip(
        "Additional scale applied while the mouse is hovering over the unit."
    )]
    [SerializeField, Range(0f, 0.2f)]
    private float hoverScaleAmount = 0.05f;

    [Tooltip(
        "How quickly the normal hover scale turns on and off."
    )]
    [SerializeField, Min(0.01f)]
    private float hoverScaleSmoothTime = 0.08f;


    // ============================================================
    // ABILITY TARGET SCALE
    // ============================================================

    [Header("Ability Target Scale")]
    [Tooltip(
        "Enables the slightly larger scale when this unit is a valid target of the selected ability."
    )]
    [SerializeField]
    private bool enableAbilityTargetScale = true;

    [Tooltip(
        "Additional scale applied to a valid ability target."
    )]
    [SerializeField, Range(0f, 0.25f)]
    private float abilityTargetScaleAmount = 0.06f;

    [Tooltip(
        "How quickly the ability target scale turns on and off."
    )]
    [SerializeField, Min(0.01f)]
    private float abilityTargetScaleSmoothTime = 0.08f;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugHover = true;


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

    private bool isHovered;

    private bool isAbilityTargetHovered;

    private SpriteRenderer spriteRenderer;

    private Collider2D collider2D;

    private AttackUnit attackUnit;

    private CanvasInfoManager canvasInfoManager;

    private GridHighlightManager gridHighlightManager;

    private Camera hoverCamera;


    // ============================================================
    // SCALE STATE
    // ============================================================

    private float currentHoverScale;

    private float hoverScaleVelocity;

    private float currentAbilityTargetScale;

    private float abilityTargetScaleVelocity;


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


        hoverCamera =
            Camera.main;


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


        if (hoverCamera == null)
        {
            Debug.LogWarning(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} could not find Camera.main.",
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
        CheckMouseHover();

        UpdateScale();
    }


    // ============================================================
    // NEW INPUT SYSTEM HOVER
    // ============================================================

    private void CheckMouseHover()
    {
        if (Mouse.current == null)
        {
            if (isHovered)
            {
                SetHovered(false);
            }

            return;
        }


        if (hoverCamera == null)
        {
            hoverCamera =
                Camera.main;
        }


        if (hoverCamera == null)
        {
            return;
        }


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        Ray ray =
            hoverCamera.ScreenPointToRay(
                mousePosition
            );


        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity
            );


        bool mouseIsOverThisUnit =
            false;


        if (hit.collider != null)
        {
            HoverInfoTrigger trigger =
                hit.collider.GetComponentInParent<
                    HoverInfoTrigger
                >();


            if (trigger == this)
            {
                mouseIsOverThisUnit =
                    true;
            }
        }


        if (mouseIsOverThisUnit != isHovered)
        {
            SetHovered(
                mouseIsOverThisUnit
            );
        }
    }


    // ============================================================
    // SET HOVERED
    // ============================================================

    private void SetHovered(
        bool hovered)
    {
        isHovered =
            hovered;


        if (debugHover)
        {
            Debug.Log(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} hover = {hovered}",
                this
            );
        }


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

    private void OnHoverExit()
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
        // --------------------------------------------------------
        // SELECTED SCALE
        // --------------------------------------------------------

        float baseScale =
            isSelected
                ? selectedScale
                : 1f;


        // --------------------------------------------------------
        // NORMAL HOVER SCALE
        // --------------------------------------------------------

        float targetHoverScale =
            0f;


        if (
            isHovered &&
            enableHoverScale
        )
        {
            targetHoverScale =
                hoverScaleAmount;
        }


        currentHoverScale =
            Mathf.SmoothDamp(
                currentHoverScale,
                targetHoverScale,
                ref hoverScaleVelocity,
                hoverScaleSmoothTime
            );


        // --------------------------------------------------------
        // ABILITY TARGET SCALE
        // --------------------------------------------------------

        float targetAbilityScale =
            0f;


        if (
            isAbilityTargetHovered &&
            enableAbilityTargetScale
        )
        {
            targetAbilityScale =
                abilityTargetScaleAmount;
        }


        currentAbilityTargetScale =
            Mathf.SmoothDamp(
                currentAbilityTargetScale,
                targetAbilityScale,
                ref abilityTargetScaleVelocity,
                abilityTargetScaleSmoothTime
            );


        // --------------------------------------------------------
        // FINAL SCALE
        // --------------------------------------------------------

        float finalScale =
            baseScale +
            currentHoverScale +
            currentAbilityTargetScale;


        Vector3 targetScale =
            originalScale *
            finalScale;


        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime *
                scaleSpeed
            );
    }


    // ============================================================
    // ABILITY TARGET HOVER
    // ============================================================

    private void UpdateAbilityTargetHover()
    {
        isAbilityTargetHovered =
            false;


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
                FindFirstObjectByType<
                    GridHighlightManager>();
        }


        if (gridHighlightManager == null)
        {
            return;
        }


        if (
            gridHighlightManager
                .IsValidCurrentAbilityTarget(
                    gameObject
                )
        )
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
        isSelected =
            false;


        isHovered =
            false;


        isAbilityTargetHovered =
            false;


        currentHoverScale =
            0f;


        hoverScaleVelocity =
            0f;


        currentAbilityTargetScale =
            0f;


        abilityTargetScaleVelocity =
            0f;


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
    // ABILITIES
    // ============================================================

    /// <summary>
    /// Returns the current cooldown of the specified ability.
    ///
    /// 0 = ready.
    /// Greater than 0 = cooldown turns remaining.
    /// </summary>
    public int GetAbilityCooldown(
        AbilitySO ability)
    {
        if (attackUnit == null)
        {
            return -1;
        }


        return attackUnit.GetAbilityCooldown(
            ability
        );
    }


    /// <summary>
    /// Returns true if the ability currently has
    /// a cooldown remaining.
    /// </summary>
    public bool IsAbilityOnCooldown(
        AbilitySO ability)
    {
        if (attackUnit == null)
        {
            return false;
        }


        return attackUnit.IsAbilityOnCooldown(
            ability
        );
    }


    /// <summary>
    /// Returns the number of uses remaining
    /// for this turn.
    ///
    /// For unlimited abilities:
    /// returns 0.
    /// </summary>
    public int GetAbilityUsesRemaining(
        AbilitySO ability)
    {
        if (attackUnit == null)
        {
            return -1;
        }


        return attackUnit.GetAbilityUsesRemaining(
            ability
        );
    }


    /// <summary>
    /// Returns true if this ability can currently
    /// be used by this unit.
    /// </summary>
    public bool IsAbilityReady(
        AbilitySO ability)
    {
        if (attackUnit == null)
        {
            return false;
        }


        return attackUnit.IsAbilityReady(
            ability
        );
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