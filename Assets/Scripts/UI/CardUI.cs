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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private CardManager cardManager;

    private Camera mainCamera;

    private GridManager gridManager;

    private Canvas canvas;

    private GameObject ghostObject;

    private Vector2 originalAnchoredPosition;

    private Transform originalParent;

    private Vector3 originalScale;

    private bool dragging;

    private bool hovering;

    private bool validPlacement;

    private Vector2Int currentGridPosition;


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


        if (character == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: CharacterSO is missing."
            );

            return;
        }


        if (character.prefabToSpawn == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: " +
                $"{character.name} has no prefabToSpawn."
            );
        }
    }


    // ==================================================
    // POINTER ENTER
    // ==================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        hovering = true;


        if (dragging)
            return;


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
            return;


        transform.localScale =
            originalScale;
    }


    // ==================================================
    // BEGIN DRAG
    // ==================================================

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (cardManager == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: CardManager missing."
            );

            return;
        }


        if (gridManager == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: GridManager missing."
            );

            return;
        }


        if (mainCamera == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: Camera missing."
            );

            return;
        }


        if (character == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: CharacterSO missing."
            );

            return;
        }


        if (character.prefabToSpawn == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: " +
                $"{character.name} has no prefabToSpawn."
            );

            return;
        }


        // ==================================================
        // START DRAG
        // ==================================================

        dragging = true;

        validPlacement = false;


        // ==================================================
        // SAVE POSITION
        // ==================================================

        RectTransform rect =
            GetComponent<RectTransform>();


        if (rect != null)
        {
            originalAnchoredPosition =
                rect.anchoredPosition;
        }


        originalParent =
            transform.parent;


        // ==================================================
        // DRAG SCALE
        // ==================================================

        transform.localScale =
            originalScale *
            draggedScale;


        // ==================================================
        // DISABLE CARD UI IMAGE
        // ==================================================

        if (cardImage != null)
        {
            cardImage.enabled = false;
        }


        // ==================================================
        // CREATE GHOST
        // ==================================================

        CreateGhost();


        // ==================================================
        // INITIAL GHOST UPDATE
        // ==================================================

        UpdateGhost(
            eventData.position
        );


        DebugLog(
            $"{name} drag started."
        );
    }


    // ==================================================
    // DRAG
    // ==================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging)
            return;


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
            return;


        dragging = false;


        // ==================================================
        // CLEAR GRID HOVER
        // ==================================================

        if (gridManager != null)
        {
            gridManager.ClearHoveredTile();
        }


        // ==================================================
        // PLACE
        // ==================================================

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
    // FOLLOW MOUSE
    // ==================================================

    private void FollowMouse(
        Vector2 screenPosition)
    {
        if (canvas == null)
            return;


        RectTransform rect =
            GetComponent<RectTransform>();


        if (rect == null)
            return;


        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();


        if (canvasRect == null)
            return;


        Camera eventCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;


        Vector2 localPosition;


        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                eventCamera,
                out localPosition))
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
        GameObject prefabToUse;


        if (ghostPrefab != null)
        {
            prefabToUse =
                ghostPrefab;
        }
        else
        {
            prefabToUse =
                character.prefabToSpawn;
        }


        if (prefabToUse == null)
        {
            Debug.LogWarning(
                $"[CardUI] {name}: No ghost prefab available."
            );

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


        DebugLog(
            $"Ghost created: {ghostObject.name}"
        );
    }


    // ==================================================
    // DISABLE GHOST BEHAVIOUR
    // ==================================================

    private void DisableGhostBehaviour()
    {
        if (ghostObject == null)
            return;


        AttackUnit attackUnit =
            ghostObject.GetComponent<
                AttackUnit>();


        if (attackUnit != null)
        {
            attackUnit.enabled = false;
        }


        HealthManager healthManager =
            ghostObject.GetComponent<
                HealthManager>();


        if (healthManager != null)
        {
            healthManager.enabled = false;
        }


        Collider2D[] colliders =
            ghostObject.GetComponentsInChildren<
                Collider2D>(
                    true
                );


        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }


        Graphic[] graphics =
            ghostObject.GetComponentsInChildren<
                Graphic>(
                    true
                );


        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }


    // ==================================================
    // UPDATE GHOST
    // ==================================================

    private void UpdateGhost(
        Vector2 screenPosition)
    {
        if (ghostObject == null)
            return;


        if (gridManager == null)
            return;


        if (mainCamera == null)
            return;


        // ==================================================
        // SCREEN -> WORLD
        // ==================================================

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );


        Plane gameplayPlane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );


        float distance;


        if (!gameplayPlane.Raycast(
                ray,
                out distance))
        {
            validPlacement = false;

            gridManager.ClearHoveredTile();

            return;
        }


        Vector3 worldPosition =
            ray.GetPoint(
                distance
            );


        worldPosition.z =
            0f;


        // ==================================================
        // WORLD -> GRID
        // ==================================================

        currentGridPosition =
            gridManager.WorldToGridPosition(
                worldPosition
            );


        // ==================================================
        // OUTSIDE GRID
        // ==================================================

        if (!gridManager.IsInsideGrid(
                currentGridPosition))
        {
            validPlacement = false;

            gridManager.ClearHoveredTile();

            // Keep ghost following mouse outside grid.
            ghostObject.transform.position =
                worldPosition;

            ghostObject.transform.rotation =
                Quaternion.identity;

            return;
        }


        // ==================================================
        // HOVER TILE
        // ==================================================

        gridManager.SetHoveredTile(
            currentGridPosition
        );


        // ==================================================
        // OCCUPIED
        // ==================================================

        if (gridManager.IsCellOccupied(
                currentGridPosition))
        {
            validPlacement = false;

            Vector3 occupiedPosition =
                gridManager.GridToWorldPosition(
                    currentGridPosition
                );


            ghostObject.transform.position =
                occupiedPosition;


            ghostObject.transform.rotation =
                Quaternion.identity;


            return;
        }


        // ==================================================
        // VALID
        // ==================================================

        validPlacement = true;


        Vector3 gridWorldPosition =
            gridManager.GridToWorldPosition(
                currentGridPosition
            );


        ghostObject.transform.position =
            gridWorldPosition;


        ghostObject.transform.rotation =
            Quaternion.identity;
    }


    // ==================================================
    // PLACE CARD
    // ==================================================

    private void PlaceCard()
    {
        if (!validPlacement)
            return;


        if (gridManager == null)
            return;


        if (character == null)
        {
            DestroyGhost();

            ReturnCardToHand();

            return;
        }


        if (character.prefabToSpawn == null)
        {
            DestroyGhost();

            ReturnCardToHand();

            return;
        }


        // ==================================================
        // SPAWN POSITION
        // ==================================================

        Vector3 spawnPosition =
            gridManager.GridToWorldPosition(
                currentGridPosition
            );


        // ==================================================
        // DESTROY GHOST
        // ==================================================

        DestroyGhost();


        // ==================================================
        // CREATE REAL UNIT
        // ==================================================

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


        // ==================================================
        // HEALTH
        // ==================================================

        HealthManager healthManager =
            placedObject.GetComponent<
                HealthManager>();


        if (healthManager == null)
        {
            Debug.LogError(
                $"[CardUI] {placedObject.name} " +
                "is missing HealthManager."
            );


            Destroy(
                placedObject
            );


            ReturnCardToHand();

            return;
        }


        healthManager.Initialize(
            character
        );


        // ==================================================
        // ATTACK
        // ==================================================

        AttackUnit attackUnit =
            placedObject.GetComponent<
                AttackUnit>();


        if (attackUnit == null)
        {
            Debug.LogError(
                $"[CardUI] {placedObject.name} " +
                "is missing AttackUnit."
            );


            Destroy(
                placedObject
            );


            ReturnCardToHand();

            return;
        }


        attackUnit.Initialize(
            character
        );


        // ==================================================
        // REGISTER ON GRID
        // ==================================================

        bool placed =
            gridManager.PlaceUnit(
                placedObject,
                currentGridPosition
            );


        if (!placed)
        {
            Debug.LogWarning(
                $"[CardUI] Failed to place " +
                $"{placedObject.name}."
            );


            Destroy(
                placedObject
            );


            ReturnCardToHand();

            return;
        }


        // ==================================================
        // REMOVE CARD FROM HAND
        // ==================================================

        if (cardManager != null)
        {
            cardManager.RemoveCard(
                this
            );
        }


        Destroy(
            gameObject
        );
    }


    // ==================================================
    // RETURN CARD TO HAND
    // ==================================================

    private void ReturnCardToHand()
    {
        // ==================================================
        // RESTORE IMAGE
        // ==================================================

        if (cardImage != null)
        {
            cardImage.enabled = true;
        }


        // ==================================================
        // RESTORE PARENT
        // ==================================================

        transform.SetParent(
            originalParent,
            false
        );


        // ==================================================
        // RESTORE POSITION
        // ==================================================

        RectTransform rect =
            GetComponent<RectTransform>();


        if (rect != null)
        {
            rect.anchoredPosition =
                originalAnchoredPosition;
        }


        // ==================================================
        // RESTORE SCALE
        // ==================================================

        transform.localScale =
            originalScale;


        // ==================================================
        // ARRANGE HAND
        // ==================================================

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
            return;


        Destroy(
            ghostObject
        );


        ghostObject = null;
    }


    // ==================================================
    // PUBLIC GETTERS
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


    // ==================================================
    // DEBUG
    // ==================================================

    private void DebugLog(
        string message)
    {
        if (!enableDebugLogs)
            return;


        Debug.Log(
            $"[CardUI] {message}"
        );
    }
}