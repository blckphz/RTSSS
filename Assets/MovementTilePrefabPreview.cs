
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementTilePrefabPreview : MonoBehaviour
{
    [Header("Character Sprite")]
    public CharacterSO charsprite;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;
    [SerializeField] private LineWalkPreview lineWalkPreview;
    [SerializeField] private CanvasInfoManager canvasInfoManager;

    [Header("Preview Prefab")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private float previewZOffset = -0.1f;
    [SerializeField] private Transform previewParent;

    [Header("Preview Settings")]
    [SerializeField] private bool playerUnitsOnly = true;
    [SerializeField] private bool hideWhenAbilitySelected = true;
    [SerializeField] private bool hideWhenMovementIsUsed = true;

    [Header("Preview Pulse")]
    [SerializeField] private bool pulsePreview = true;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.08f;

    [Header("Preview Transparency")]
    [SerializeField] private float transparencySpeed = 3f;

    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.5f;

    private GameObject previewInstance;
    private SpriteRenderer previewSpriteRenderer;

    private Vector2Int lastHoveredCell;
    private bool hasLastHoveredCell;

    private Camera previewCamera;
    private GameObject lastSelectedObject;

    private Vector3 previewBaseScale;
    private Coroutine effectsCoroutine;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindReferences();
        CreatePreviewInstance();
        HidePreview();
    }

    private void Update()
    {
        if (!ReferencesAreValid())
        {
            HidePreview();
            return;
        }

        HoverInfoTrigger selectedUnit =
            lineWalkPreview.GetCurrentSelectedUnit();

        if (selectedUnit == null)
        {
            lastSelectedObject = null;
            HidePreview();
            return;
        }

        GameObject selectedObject = selectedUnit.gameObject;

        AttackUnit attackUnit =
            selectedObject.GetComponent<AttackUnit>();

        if (playerUnitsOnly)
        {
            if (attackUnit == null ||
                attackUnit.GetTeam() != Team.Player)
            {
                HidePreview();
                return;
            }
        }

        if (lastSelectedObject != selectedObject)
        {
            lastSelectedObject = selectedObject;
            UpdatePreviewSprite(selectedUnit);
        }

        if (highlightManager.GetCurrentRangeUser() != selectedObject)
        {
            HidePreview();
            return;
        }

        UnitMoveBrain moveBrain =
            selectedObject.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            HidePreview();
            return;
        }

        if (hideWhenMovementIsUsed &&
            !moveBrain.CanMoveThisTurn())
        {
            HidePreview();
            return;
        }

        if (hideWhenAbilitySelected &&
            canvasInfoManager != null &&
            canvasInfoManager.HasSelectedAbility())
        {
            HidePreview();
            return;
        }

        if (lineWalkPreview.IsPlayerWalkPreviewDisabled())
        {
            HidePreview();
            return;
        }

        if (!highlightManager.HasMovementRange() ||
            highlightManager.IsMovementHighlightSuppressed())
        {
            HidePreview();
            return;
        }

        UpdateMousePreview();
    }

    // ============================================================
    // REFERENCES
    // ============================================================

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (highlightManager == null)
            highlightManager = FindFirstObjectByType<GridHighlightManager>();

        if (lineWalkPreview == null)
            lineWalkPreview = FindFirstObjectByType<LineWalkPreview>();

        if (canvasInfoManager == null)
            canvasInfoManager = FindFirstObjectByType<CanvasInfoManager>();

        if (previewCamera == null)
            previewCamera = Camera.main;
    }

    private bool ReferencesAreValid()
    {
        if (gridManager == null ||
            highlightManager == null ||
            lineWalkPreview == null ||
            previewInstance == null)
        {
            FindReferences();
        }

        return gridManager != null &&
               highlightManager != null &&
               lineWalkPreview != null &&
               previewInstance != null;
    }

    // ============================================================
    // CREATE PREVIEW
    // ============================================================

    private void CreatePreviewInstance()
    {
        if (previewPrefab == null)
        {
            Debug.LogWarning(
                "[MovementTilePrefabPreview] Preview prefab is not assigned.",
                this
            );
            return;
        }

        previewInstance = Instantiate(previewPrefab, previewParent);
        previewInstance.name = previewPrefab.name + "_Preview";

        previewSpriteRenderer =
            previewInstance.GetComponentInChildren<SpriteRenderer>(true);

        if (previewSpriteRenderer == null)
        {
            Debug.LogWarning(
                "[MovementTilePrefabPreview] Preview prefab has no SpriteRenderer.",
                previewInstance
            );
            return;
        }

        previewBaseScale = previewInstance.transform.localScale;
        SetPreviewAlpha(maxAlpha);
    }

    // ============================================================
    // CHARACTER ICON
    // ============================================================

    private void UpdatePreviewSprite(HoverInfoTrigger selectedUnit)
    {
        if (previewSpriteRenderer == null ||
            selectedUnit == null)
            return;

        CharacterSO character = selectedUnit.GetCharacterData();

        if (character == null)
            character = charsprite;

        if (character == null)
        {
            Debug.LogWarning(
                "[MovementTilePrefabPreview] No CharacterSO found for selected unit.",
                selectedUnit
            );
            return;
        }

        if (character.icon == null)
        {
            Debug.LogWarning(
                $"[MovementTilePrefabPreview] {character.characterName} has no icon assigned.",
                selectedUnit
            );
            return;
        }

        previewSpriteRenderer.sprite = character.icon;
    }

    // ============================================================
    // MOUSE PREVIEW
    // ============================================================

    private void UpdateMousePreview()
    {
        if (Mouse.current == null)
        {
            HidePreview();
            return;
        }

        if (previewCamera == null)
            previewCamera = Camera.main;

        if (previewCamera == null)
        {
            HidePreview();
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            previewCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    Mathf.Abs(previewCamera.transform.position.z)
                )
            );

        Vector2Int hoveredCell =
            gridManager.WorldToGridPosition(mouseWorldPosition);

        if (!gridManager.IsInsideGrid(hoveredCell))
        {
            HidePreview();
            return;
        }

        if (!highlightManager.IsMovementCell(hoveredCell))
        {
            HidePreview();
            return;
        }

        if (gridManager.IsCellOccupied(hoveredCell))
        {
            HidePreview();
            return;
        }

        if (hasLastHoveredCell &&
            hoveredCell == lastHoveredCell &&
            previewInstance.activeSelf)
        {
            return;
        }

        lastHoveredCell = hoveredCell;
        hasLastHoveredCell = true;

        Vector3 previewPosition =
            gridManager.GridToWorldPosition(hoveredCell);

        previewPosition.z += previewZOffset;

        previewInstance.transform.position = previewPosition;

        if (!previewInstance.activeSelf)
        {
            previewInstance.SetActive(true);
            StartEffectsCoroutine();
        }
    }

    // ============================================================
    // PULSE AND TRANSPARENCY
    // ============================================================

    private void StartEffectsCoroutine()
    {
        StopEffectsCoroutine();

        if (!pulsePreview && transparencySpeed <= 0f)
            return;

        if (previewInstance == null ||
            !previewInstance.activeSelf)
            return;

        effectsCoroutine =
            StartCoroutine(PreviewEffectsRoutine());
    }

    private void StopEffectsCoroutine()
    {
        if (effectsCoroutine == null)
            return;

        StopCoroutine(effectsCoroutine);
        effectsCoroutine = null;
    }

    private IEnumerator PreviewEffectsRoutine()
    {
        while (
            previewInstance != null &&
            previewInstance.activeSelf
        )
        {
            float time = Time.time;

            // Pulsing scale.
            if (pulsePreview)
            {
                float pulse =
                    1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;

                previewInstance.transform.localScale =
                    previewBaseScale * pulse;
            }

            // Fading transparency.
            if (transparencySpeed > 0f)
            {
                float pingPong =
                    Mathf.PingPong(time * transparencySpeed, 1f);

                float alpha =
                    Mathf.Lerp(maxAlpha, minAlpha, pingPong);

                SetPreviewAlpha(alpha);
            }

            yield return null;
        }

        effectsCoroutine = null;
    }

    private void SetPreviewAlpha(float alpha)
    {
        if (previewSpriteRenderer == null)
            return;

        Color color = previewSpriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);

        previewSpriteRenderer.color = color;
    }

    // ============================================================
    // HIDE PREVIEW
    // ============================================================

    private void HidePreview()
    {
        hasLastHoveredCell = false;

        StopEffectsCoroutine();

        if (previewInstance != null)
        {
            previewInstance.transform.localScale = previewBaseScale;

            if (previewInstance.activeSelf)
                previewInstance.SetActive(false);
        }

        SetPreviewAlpha(maxAlpha);
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    public void ClearPreview()
    {
        HidePreview();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDisable()
    {
        HidePreview();
    }

    private void OnDestroy()
    {
        StopEffectsCoroutine();

        if (previewInstance != null)
            Destroy(previewInstance);
    }
}
