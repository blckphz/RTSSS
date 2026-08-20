using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoverInfoTrigger : MonoBehaviour, ICharacterHolder
{
    [Header("Tooltip Content")]
    [TextArea]
    [SerializeField]
    private string hoverMessage =
        "Default Prefab Info";

    public string HoverMessage =>
        hoverMessage;

    [Header("Click Feedback")]
    [SerializeField]
    private float selectedScale = 1.1f;

    [SerializeField]
    private float scaleSpeed = 10f;

    [Header("Selected Visual")]
    [Tooltip(
        "SpriteRenderer on the child object that appears when selected."
    )]
    [SerializeField]
    private SpriteRenderer selectedChildSprite;

    private Vector3 originalScale;

    private bool isSelected;

    private SpriteRenderer spriteRenderer;
    private Collider2D collider2D;
    private AttackUnit attackUnit;
    private CanvasInfoManager canvasInfoManager;

    // =============================================================
    // UNITY LIFECYCLE
    // =============================================================

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

        // =========================================================
        // VALIDATION
        // =========================================================

        if (collider2D == null)
        {
            Debug.LogError(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} is missing " +
                "a required Collider2D!",
                this
            );
        }

        if (attackUnit == null)
        {
            Debug.LogError(
                $"[HoverInfoTrigger] " +
                $"{gameObject.name} is missing " +
                "an AttackUnit component!",
                this
            );
        }

        // =========================================================
        // SELECTED VISUAL
        // =========================================================

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled =
                false;
        }

        // =========================================================
        // CANVAS INFO
        // =========================================================

        canvasInfoManager =
            FindFirstObjectByType<CanvasInfoManager>();
    }

    private void Update()
    {
        Vector3 targetScale =
            isSelected
                ? originalScale * selectedScale
                : originalScale;

        if (
            (transform.localScale - targetScale)
            .sqrMagnitude > 0.0001f
        )
        {
            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    targetScale,
                    Time.deltaTime *
                    scaleSpeed
                );
        }
    }

    // =============================================================
    // HOVER DETECTION
    // =============================================================

    private void OnMouseEnter()
    {
        if (canvasInfoManager != null)
        {
            canvasInfoManager.ShowCharacter(
                this
            );
        }
    }

    private void OnMouseExit()
    {
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

    // =============================================================
    // DISABLE
    // =============================================================

    private void OnDisable()
    {
        isSelected =
            false;

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

    // =============================================================
    // DESTROY
    // =============================================================

    private void OnDestroy()
    {
        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(
                this
            );
        }
    }

    // =============================================================
    // CHARACTER DATA
    // =============================================================

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

    // =============================================================
    // ATTACK UNIT
    // =============================================================

    public AttackUnit GetAttackUnit()
    {
        return attackUnit;
    }

    // =============================================================
    // SELECTION
    // =============================================================

    public bool IsSelected()
    {
        return isSelected;
    }

    public void SetSelected(
        bool selected)
    {
        isSelected =
            selected;

        // =========================================================
        // SELECTED VISUAL
        // =========================================================

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled =
                selected;
        }

        // =========================================================
        // CANVAS
        // =========================================================

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