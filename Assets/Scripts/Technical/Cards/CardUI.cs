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
    [SerializeField]
    private Image cardImage;

    private CharacterSO character;

    [Header("Ghost")]
    [SerializeField]
    private GameObject ghostPrefab;

    [Header("Drag")]
    [SerializeField]
    private float draggedScale = 1.1f;

    [Header("Hover")]
    [SerializeField]
    private float hoverScale = 1.15f;

    private CardManager cardManager;
    private Camera mainCamera;
    private GridManager gridManager;
    private GridHighlightManager highlightManager;
    private EncounterManager encounterManager;
    private Canvas canvas;

    private GameObject ghostObject;
    private Vector2 originalAnchoredPosition;
    private Transform originalParent;
    private Vector3 originalScale;
    private Vector2Int currentGridPosition;
    private bool dragging;
    private bool hovering;
    private bool validPlacement;

    private void Awake()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (cardImage == null)
        {
            cardImage = GetComponentInChildren<Image>(true);
        }

        if (cardImage == null)
        {
            Debug.LogError(
                "[CardUI] No Image component found on the card or its children.",
                this
            );
        }

        originalScale = transform.localScale;
    }

    public void Setup(
        CardManager manager,
        CharacterSO characterData)
    {
        cardManager = manager;
        character = characterData;

        if (character == null)
        {
            Debug.LogError(
                "[CardUI] Character data is null.",
                this
            );

            return;
        }

        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (cardImage == null)
        {
            cardImage = GetComponentInChildren<Image>(true);
        }

        if (cardImage == null)
        {
            Debug.LogError(
                "[CardUI] Card Image could not be found.",
                this
            );
        }
        else
        {
            cardImage.color = Color.white;

            if (character.icon == null)
            {
                Debug.LogWarning(
                    "[CardUI] Character has no icon assigned: " +
                    character.characterName,
                    this
                );
            }
            else
            {
                cardImage.sprite = character.icon;
                cardImage.enabled = true;
                cardImage.type = Image.Type.Simple;
            }
        }

        if (cardManager != null)
        {
            gridManager = cardManager.GetGridManager();
            mainCamera = cardManager.GetCamera();
        }

        encounterManager =
            FindFirstObjectByType<EncounterManager>();

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

        canvas = GetComponentInParent<Canvas>();

        originalParent = transform.parent;

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            originalAnchoredPosition =
                rect.anchoredPosition;
        }

        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;

        if (dragging)
        {
            return;
        }

        transform.localScale =
            originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;

        if (dragging)
        {
            return;
        }

        transform.localScale =
            originalScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            return;
        }

        dragging = true;
        validPlacement = false;

        SaveCardPosition();

        transform.localScale =
            originalScale * draggedScale;

        if (cardImage != null)
        {
            cardImage.enabled = false;
        }

        CreateGhost();

        UpdateGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        FollowMouse(eventData.position);
        UpdateGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
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

        if (encounterManager != null)
        {
            if (!encounterManager.IsEncounterRunning())
            {
                return false;
            }

            if (!encounterManager.IsPreparing())
            {
                return false;
            }
        }

        return true;
    }

    private void SaveCardPosition()
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            originalAnchoredPosition =
                rect.anchoredPosition;
        }

        originalParent = transform.parent;
    }

    private void FollowMouse(Vector2 screenPosition)
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
                )
        )
        {
            rect.localPosition = localPosition;
        }
    }

    private void CreateGhost()
    {
        GameObject prefabToUse =
            ghostPrefab != null
                ? ghostPrefab
                : character.prefabToSpawn;

        if (prefabToUse == null)
        {
            Debug.LogError(
                "[CardUI] Cannot create ghost. " +
                "No ghost prefab or character prefab assigned.",
                this
            );

            return;
        }

        ghostObject =
            Instantiate(prefabToUse);

        ghostObject.name =
            character.characterName +
            "_Ghost";

        ApplyCharacterIconToGhost();

        DisableGhostBehaviour();

        ghostObject.SetActive(true);
    }

    private void ApplyCharacterIconToGhost()
    {
        if (ghostObject == null)
        {
            return;
        }

        if (character == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer =
            ghostObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                ghostObject.GetComponentInChildren<
                    SpriteRenderer
                >(true);
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[CardUI] Ghost prefab has no SpriteRenderer.\n" +
                "Character: " +
                character.characterName,
                ghostObject
            );

            return;
        }

        if (character.icon == null)
        {
            Debug.LogWarning(
                "[CardUI] Character has no icon assigned.\n" +
                "Character: " +
                character.characterName,
                this
            );

            return;
        }

        spriteRenderer.sprite =
            character.icon;

        spriteRenderer.color =
            Color.white;
    }

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
            ghostObject.GetComponentsInChildren<
                Collider2D
            >(true);

        foreach (
            Collider2D collider
            in colliders
        )
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Graphic[] graphics =
            ghostObject.GetComponentsInChildren<
                Graphic
            >(true);

        foreach (
            Graphic graphic
            in graphics
        )
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private void UpdateGhost(Vector2 screenPosition)
    {
        if (
            ghostObject == null ||
            gridManager == null ||
            mainCamera == null
        )
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

        if (
            !gameplayPlane.Raycast(
                ray,
                out float distance
            )
        )
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

        if (
            !gridManager.IsInsideGrid(
                currentGridPosition
            )
        )
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

        if (
            gridManager.IsCellOccupied(
                currentGridPosition
            )
        )
        {
            validPlacement = false;
            return;
        }

        validPlacement = true;
    }

    private void SetInvalidPlacement()
    {
        validPlacement = false;

        if (highlightManager != null)
        {
            highlightManager.ClearPlacementTile();
        }
    }

    private void PlaceCard()
    {
        if (
            !validPlacement ||
            gridManager == null ||
            character == null ||
            character.prefabToSpawn == null
        )
        {
            DestroyGhost();
            ReturnCardToHand();
            return;
        }

        if (
            encounterManager != null &&
            !encounterManager.IsPreparing()
        )
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

        // --------------------------------------------------
        // APPLY CHARACTER ICON TO THE PLACED UNIT
        // --------------------------------------------------

        SpriteRenderer placedSprite =
            placedObject.GetComponent<SpriteRenderer>();

        if (placedSprite == null)
        {
            placedSprite =
                placedObject.GetComponentInChildren<
                    SpriteRenderer
                >(true);
        }

        if (placedSprite != null)
        {
            if (character.icon != null)
            {
                placedSprite.sprite =
                    character.icon;
            }

            placedSprite.color =
                Color.white;
        }
        else
        {
            Debug.LogWarning(
                "[CardUI] Placed character has no SpriteRenderer.\n" +
                "Character: " +
                character.characterName,
                placedObject
            );
        }

        // --------------------------------------------------
        // UNIT DATA
        // --------------------------------------------------

        UnitData unitData =
            placedObject.GetComponent<UnitData>();

        if (unitData == null)
        {
            unitData =
                placedObject.AddComponent<UnitData>();
        }

        unitData.Initialize(character);

        // --------------------------------------------------
        // HEALTH
        // --------------------------------------------------

        HealthManager healthManager =
            placedObject.GetComponent<HealthManager>();

        if (healthManager == null)
        {
            Debug.LogError(
                "[CardUI] Placed character prefab " +
                "is missing HealthManager.\n" +
                "Character: " +
                character.characterName,
                placedObject
            );

            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        // --------------------------------------------------
        // ATTACK
        // --------------------------------------------------

        AttackUnit attackUnit =
            placedObject.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            Debug.LogError(
                "[CardUI] Placed character prefab " +
                "is missing AttackUnit.\n" +
                "Character: " +
                character.characterName,
                placedObject
            );

            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        healthManager.Initialize(character);

        attackUnit.Initialize(character);

        // --------------------------------------------------
        // FORTRESS TARGET
        // --------------------------------------------------

        UpgradeableCombatUnit upgradeableCombatUnit =
            placedObject.GetComponent<
                UpgradeableCombatUnit
            >();

        if (upgradeableCombatUnit != null)
        {
            upgradeableCombatUnit.RegisterAsFortressTarget();
        }

        // --------------------------------------------------
        // GRID PLACEMENT
        // --------------------------------------------------

        bool placed =
            gridManager.PlaceUnit(
                placedObject,
                currentGridPosition
            );

        if (!placed)
        {
            Debug.LogWarning(
                "[CardUI] Failed to place unit on grid.\n" +
                "Character: " +
                character.characterName +
                "\nGrid Position: " +
                currentGridPosition,
                this
            );

            Destroy(placedObject);
            ReturnCardToHand();
            return;
        }

        attackUnit.SetLogicalGridPosition(
            currentGridPosition
        );

        // --------------------------------------------------
        // REMOVE CARD
        // --------------------------------------------------

        if (cardManager != null)
        {
            cardManager.RemoveCard(this);
        }

        Destroy(gameObject);
    }

    private void ReturnCardToHand()
    {
        if (cardImage != null)
        {
            cardImage.color = Color.white;

            if (
                character != null &&
                character.icon != null
            )
            {
                cardImage.sprite =
                    character.icon;
            }

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

    private void DestroyGhost()
    {
        if (ghostObject == null)
        {
            return;
        }

        Destroy(ghostObject);

        ghostObject = null;
    }

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
