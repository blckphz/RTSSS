using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Card Settings")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Hand Settings")]
    [SerializeField] private Transform handArea;
    [SerializeField] private int startingHandSize = 5;
    [SerializeField] private float cardSpacing = 120f;

    [Header("Grid Placement")]
    [SerializeField] private Camera mainCamera;

    private readonly List<CardUI> hand = new List<CardUI>();

    // ==================================================
    // UNITY EVENTS
    // ==================================================

    private void Awake()
    {
        InitializeReferences();
        ValidateReferences();
    }

    private void Start()
    {
        CreateStartingHand();
    }

    // ==================================================
    // INITIALIZATION & VALIDATION
    // ==================================================

    private void InitializeReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void ValidateReferences()
    {
        if (gridManager == null) Debug.LogError("[CardManager] GridManager missing.", this);
        if (mainCamera == null) Debug.LogError("[CardManager] Main Camera missing.", this);
        if (cardPrefab == null) Debug.LogError("[CardManager] Card prefab missing.", this);
        if (handArea == null) Debug.LogError("[CardManager] Hand Area missing.", this);
    }

    // ==================================================
    // HAND MANAGEMENT
    // ==================================================

    private void CreateStartingHand()
    {
        for (int i = 0; i < startingHandSize; i++)
        {
            CreateCard();
        }

        ArrangeHand();
    }

    public void CreateCard()
    {
        if (cardPrefab == null || handArea == null) return;

        GameObject cardObject = Instantiate(cardPrefab, handArea);
        cardObject.name = $"Card_{hand.Count + 1}";

        if (!cardObject.TryGetComponent<CardUI>(out var card))
        {
            Debug.LogError("[CardManager] Card prefab is missing a CardUI component.", cardObject);
            Destroy(cardObject);
            return;
        }

        card.Setup(this);
        hand.Add(card);
    }

    public void RemoveCard(CardUI card)
    {
        if (card == null || !hand.Remove(card))
            return;

        ArrangeHand();
    }

    public void ReturnCardToHand(CardUI card)
    {
        if (card == null || hand.Contains(card))
            return;

        card.transform.SetParent(handArea, false);
        hand.Add(card);

        ArrangeHand();
    }

    public void ArrangeHand()
    {
        // Clean out any destroyed or missing cards first
        hand.RemoveAll(c => c == null);

        int count = hand.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;

        Vector2 cachedPosition = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            CardUI card = hand[i];

            // TryGetComponent is faster and safer than GetComponent
            if (!card.TryGetComponent<RectTransform>(out var rect))
                continue;

            cachedPosition.x = startX + (i * cardSpacing);
            rect.anchoredPosition = cachedPosition;
            rect.SetSiblingIndex(i);
        }
    }

    // ==================================================
    // GETTERS
    // ==================================================

    public GridManager GetGridManager() => gridManager;
    public Camera GetCamera() => mainCamera;
}