using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoverInfoTrigger : MonoBehaviour, ICharacterHolder
{
    [Header("Tooltip Content")]
    [TextArea]
    [SerializeField] private string hoverMessage = "Default Prefab Info";

    public string HoverMessage => hoverMessage;

    [Header("Click Feedback")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Selected Visual")]
    [Tooltip("SpriteRenderer on the child object that appears when selected.")]
    [SerializeField] private SpriteRenderer selectedChildSprite;

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
        collider2D = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        attackUnit = GetComponent<AttackUnit>();

        originalScale = transform.localScale;

        if (collider2D == null)
        {
            Debug.LogError($"[HoverInfoTrigger] {gameObject.name} is missing a required Collider2D!", this);
        }

        if (attackUnit == null)
        {
            Debug.LogError($"[HoverInfoTrigger] {gameObject.name} is missing an AttackUnit component!", this);
        }

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = false;
        }

        // Cache CanvasInfoManager reference
        canvasInfoManager = FindObjectOfType<CanvasInfoManager>();
    }

    private void Update()
    {
        Vector3 targetScale = isSelected ? originalScale * selectedScale : originalScale;

        // Skip Lerp if already at or very close to target scale
        if ((transform.localScale - targetScale).sqrMagnitude > 0.0001f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
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
            canvasInfoManager.ShowCharacter(this);
        }
    }

    private void OnMouseExit()
    {
        // Check if there is an active global selection in UIManager
        if (UIManager.CurrentSelection != null)
        {
            // If another unit is selected, show its info upon hover-off
            if (canvasInfoManager != null)
            {
                canvasInfoManager.ShowCharacter(UIManager.CurrentSelection);
            }
        }
        else if (!isSelected && canvasInfoManager != null)
        {
            // If nothing is selected and this isn't selected, clear the canvas
            canvasInfoManager.ClearInfo();
        }
    }

    private void OnDisable()
    {
        isSelected = false;

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = false;
        }

        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(this);
        }

        if (canvasInfoManager != null)
        {
            canvasInfoManager.ClearInfo();
        }
    }

    private void OnDestroy()
    {
        if (UIManager.CurrentSelection == this)
        {
            UIManager.ClearSelection(this);
        }
    }

    // =============================================================
    // INTERFACE & GETTERS / SETTERS
    // =============================================================

    public CharacterSO GetCharacterData()
    {
        if (attackUnit == null) return null;

        CharacterSO characterData = attackUnit.GetCharacterData();
        if (characterData == null)
        {
            Debug.LogError($"[HoverInfoTrigger] AttackUnit on {gameObject.name} has no CharacterSO assigned!", this);
        }

        return characterData;
    }

    public AttackUnit GetAttackUnit() => attackUnit;

    public bool IsSelected() => isSelected;

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedChildSprite != null)
        {
            selectedChildSprite.enabled = selected;
        }

        if (canvasInfoManager != null)
        {
            if (selected)
            {
                // Lock info on canvas
                canvasInfoManager.ShowCharacter(this);
            }
            else
            {
                // Clear info on deselection
                canvasInfoManager.ClearInfo();
            }
        }
    }
}