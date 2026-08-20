using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Card")]
    [SerializeField] private Image cardImage;

    [Header("Character")]
    [SerializeField] private CharacterSO character;

    [Header("Ghost")]
    [SerializeField] private GameObject ghostPrefab;

    [Header("Drag")]
    [SerializeField] private float draggedScale = 1.1f;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.15f;

    // ==================================================
    // REFERENCES
    // ==================================================

    private CardManager cardManager;
    private Camera mainCamera;
    private GridManager gridManager;
    private GridHighlightManager highlightManager;
    private Canvas canvas;

    // ==================================================
    // DRAG STATE
    // ==================================================

    private GameObject ghostObject;

    private Vector2 originalAnchoredPosition;

    private Transform originalParent;

    private Vector3 originalScale;

    private Vector2Int currentGridPosition;

    private bool dragging;

    private bool hovering;

    private bool validPlacement;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (cardImage == null)
        {
            cardImage =
                GetComponent<Image>();
        }

        originalScale =
            transform.localScale;
    }

    // ==================================================
    // SETUP
    // ==================================================

    public void Setup(
        CardManager manager)
    {
        cardManager =
            manager;

        if (cardManager != null)
        {
            gridManager =
                cardManager.GetGridManager();

            mainCamera =
                cardManager.GetCamera();
        }

        if (gridManager != null)
        {
            highlightManager =
                gridManager.GetHighlightManager();
        }

        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }

        canvas =
            GetComponentInParent<Canvas>();

        originalParent =
            transform.parent;

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            originalAnchoredPosition =
                rect.anchoredPosition;
        }

        originalScale =
            transform.localScale;
    }

    // ==================================================
    // POINTER ENTER
    // ==================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        hovering = true;

        if (dragging)
        {
            return;
        }

        transform.localScale =
            originalScale *
            hoverScale;
    }

    // ==================================================
    // POINTER EXIT
    // ==================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        hovering = false;

        if (dragging)
        {
            return;
        }

        transform.localScale =
            originalScale;
    }

    // ==================================================
    // BEGIN DRAG
    // ==================================================

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            return;
        }

        dragging = true;
        validPlacement = false;

        SaveCardPosition();

        transform.localScale =
            originalScale *
            draggedScale;

        if (cardImage != null)
        {
            cardImage.enabled = false;
        }

        CreateGhost();

        UpdateGhost(
            eventData.position
        );
    }

    // ==================================================
    // DRAG
    // ==================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        FollowMouse(
            eventData.position
        );

        UpdateGhost(
            eventData.position
        );
    }

    // ==================================================
    // END DRAG
    // ==================================================

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;

        if (highlightManager != null)
        {
            highlightManager.ClearPlacementTile();
        }

        if (validPlacement)
        {
            PlaceCard();
        }
        else
        {
            DestroyGhost();
            ReturnCardToHand();
        }

        validPlacement = false;
    }

    // ==================================================
    // VALIDATE DRAG
    // ==================================================

    private bool CanStartDrag()
    {
        if (cardManager == null)
        {
            return false;
        }

        if (gridManager == null)
        {
            return false;
        }

        if (mainCamera == null)
        {
            return false;
        }

        if (highlightManager == null)
        {
            return false;
        }

        if (character == null)
        {
            return false;
        }

        if (character.prefabToSpawn == null)
        {
            return false;
        }

        return true;
    }

    // ==================================================
    // SAVE POSITION
    // ==================================================

    private void SaveCardPosition()
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            originalAnchoredPosition =
                rect.anchoredPosition;
        }

        originalParent =
            transform.parent;
    }

    // ==================================================
    // FOLLOW MOUSE
    // ==================================================

    private void FollowMouse(
        Vector2 screenPosition)
    {
        if (canvas == null)
        {
            return;
        }

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            return;
        }

        Camera eventCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        if (
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition
                ))
        {
            rect.localPosition =
                localPosition;
        }
    }

    // ==================================================
    // CREATE GHOST
    // ==================================================

    private void CreateGhost()
    {
        GameObject prefabToUse =
            ghostPrefab != null
                ? ghostPrefab
                : character.prefabToSpawn;

        if (prefabToUse == null)
        {
            return;
        }

        ghostObject =
            Instantiate(
                prefabToUse
            );

        ghostObject.name =
            $"{character.characterName}_Ghost";

        DisableGhostBehaviour();

        ghostObject.SetActive(true);
    }

    // ==================================================
    // DISABLE GHOST BEHAVIOUR
    // ==================================================

    private void DisableGhostBehaviour()
    {
        if (ghostObject == null)
        {
            return;
        }

        AttackUnit attackUnit =
            ghostObject.GetComponent<AttackUnit>();

        if (attackUnit != null)
        {
            attackUnit.enabled = false;
        }

        HealthManager healthManager =
            ghostObject.GetComponent<HealthManager>();

        if (healthManager != null)
        {
            healthManager.enabled = false;
        }

        Collider2D[] colliders =
            ghostObject.GetComponentsInChildren<Collider2D>(
                true
            );

        foreach (Collider2D collider
                 in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Graphic[] graphics =
            ghostObject.GetComponentsInChildren<Graphic>(
                true
            );

        foreach (Graphic graphic
                 in graphics)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    // ==================================================
    // UPDATE GHOST
    // ==================================================

    private void UpdateGhost(
        Vector2 screenPosition)
    {
        if (ghostObject == null ||
            gridManager == null ||
            mainCamera == null)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        Plane gameplayPlane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );

        if (!gameplayPlane.Raycast(
                ray,
                out float distance))
        {
            SetInvalidPlacement();
            return;
        }

        Vector3 worldPosition =
            ray.GetPoint(distance);

        worldPosition.z = 0f;

        currentGridPosition =
            gridManager.WorldToGridPosition(
                worldPosition
            );

        if (!gridManager.IsInsideGrid(
                currentGridPosition))
        {
            SetInvalidPlacement();

            ghostObject.transform.position =
                worldPosition;

            ghostObject.transform.rotation =
                Quaternion.identity;

            return;
        }

        if (highlightManager != null)
        {
            highlightManager.SetPlacementTile(
                currentGridPosition
            );
        }

        Vector3 gridWorldPosition =
            gridManager.GridToWorldPosition(
                currentGridPosition
            );

        ghostObject.transform.position =
            gridWorldPosition;

        ghostObject.transform.rotation =
            Quaternion.identity;

        if (gridManager.IsCellOccupied(
                currentGridPosition))
        {
            validPlacement = false;
            return;
        }

        validPlacement = true;
    }

    // ==================================================
    // INVALID PLACEMENT
    // ==================================================

    private void SetInvalidPlacement()
    {
        validPlacement = false;

        if (highlightManager != null)
        {
            highlightManager.ClearPlacementTile();
        }
    }

    // ==================================================
    // PLACE CARD
    // ==================================================

    private void PlaceCard()
    {
        if (!validPlacement ||
            gridManager == null ||
            character == null ||
            character.prefabToSpawn == null)
        {
            DestroyGhost();
            ReturnCardToHand();
            return;
        }

        Vector3 spawnPosition =
            gridManager.GridToWorldPosition(
                currentGridPosition
            );

        DestroyGhost();

        GameObject placedObject =
            Instantiate(
                character.prefabToSpawn,
                spawnPosition,
                Quaternion.identity
            );

        if (placedObject == null)
        {
            ReturnCardToHand();
            return;
        }

        placedObject.name =
            character.characterName;

        HealthManager healthManager =
            placedObject.GetComponent<HealthManager>();

        if (healthManager == null)
        {
            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        healthManager.Initialize(
            character
        );

        AttackUnit attackUnit =
            placedObject.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        attackUnit.Initialize(
            character
        );

        bool placed =
            gridManager.PlaceUnit(
                placedObject,
                currentGridPosition
            );

        if (!placed)
        {
            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        if (cardManager != null)
        {
            cardManager.RemoveCard(
                this
            );
        }

        Destroy(gameObject);
    }

    // ==================================================
    // RETURN CARD
    // ==================================================

    private void ReturnCardToHand()
    {
        if (cardImage != null)
        {
            cardImage.enabled = true;
        }

        transform.SetParent(
            originalParent,
            false
        );

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition =
                originalAnchoredPosition;
        }

        transform.localScale =
            originalScale;

        if (cardManager != null)
        {
            cardManager.ArrangeHand();
        }
    }

    // ==================================================
    // DESTROY GHOST
    // ==================================================

    private void DestroyGhost()
    {
        if (ghostObject == null)
        {
            return;
        }

        Destroy(
            ghostObject
        );

        ghostObject = null;
    }

    // ==================================================
    // GETTERS
    // ==================================================

    public CharacterSO GetCharacter()
    {
        return character;
    }

    public Team GetCharacterTeam()
    {
        if (character == null)
        {
            return Team.Ally;
        }

        return character.team;
    }

    public bool IsDragging()
    {
        return dragging;
    }

    public bool IsHovering()
    {
        return hovering;
    }

    public bool IsValidPlacement()
    {
        return validPlacement;
    }

    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }
}