
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardManager : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField] private GridManager gridManager;


    // ==================================================
    // CARD SETTINGS
    // ==================================================

    [Header("Card Settings")]
    [SerializeField] private GameObject cardPrefab;


    // ==================================================
    // HAND SETTINGS
    // ==================================================

    [Header("Hand Settings")]
    [SerializeField] private Transform handArea;

    [SerializeField] private float cardSpacing = 120f;


    // ==================================================
    // GRID PLACEMENT
    // ==================================================

    [Header("Grid Placement")]
    [SerializeField] private Camera mainCamera;


    // ==================================================
    // RUNTIME CARD TEST
    // ==================================================

    [Header("Runtime Card Test")]
    [SerializeField] private CharacterSO testCharacter;


    // ==================================================
    // INPUT
    // ==================================================

    [Header("Input")]
    [SerializeField] private InputActionReference spawnCardAction;


    // ==================================================
    // HAND RIPPLE ANIMATION
    // ==================================================

    [Header("Hand Ripple Animation")]

    [SerializeField] private float rippleDistance = 25f;

    [SerializeField] private float rippleDuration = 0.3f;

    [SerializeField] private float rippleDelay = 0.06f;

    [SerializeField] private float rippleCooldown = 1.5f;

    [SerializeField] private float rippleStartDelay = 2f;

    [SerializeField] private bool continuousRipple = true;


    // ==================================================
    // RUNTIME
    // ==================================================

    private readonly List<CardUI> hand = new List<CardUI>();

    private Coroutine rippleCoroutine;

    private bool cardBeingDragged;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        Debug.Log("[CardManager] Awake.");

        InitializeReferences();
        ValidateReferences();
    }

    private void Start()
    {
        Debug.Log("[CardManager] Start.");
    }

    private void OnEnable()
    {
        Debug.Log("[CardManager] OnEnable.");

        if (spawnCardAction != null)
        {
            spawnCardAction.action.performed += OnSpawnCard;
            spawnCardAction.action.Enable();

            Debug.Log("[CardManager] Spawn input enabled.");
        }
        else
        {
            Debug.LogWarning("[CardManager] Spawn Card Input Action is missing.");
        }
    }

    private void OnDisable()
    {
        Debug.Log("[CardManager] OnDisable.");

        if (spawnCardAction != null)
        {
            spawnCardAction.action.performed -= OnSpawnCard;
            spawnCardAction.action.Disable();
        }

        StopHandRipple();
    }


    // ==================================================
    // INPUT
    // ==================================================

    private void OnSpawnCard(InputAction.CallbackContext context)
    {
        Debug.Log("[CardManager] Spawn input received.");

        if (testCharacter == null)
        {
            Debug.LogWarning("[CardManager] Test Character is not assigned.");
            return;
        }

        CreateCard(testCharacter);

        PlayHandRipple();
    }


    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void InitializeReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void ValidateReferences()
    {
        if (gridManager == null)
            Debug.LogError("[CardManager] GridManager missing.", this);

        if (mainCamera == null)
            Debug.LogError("[CardManager] Main Camera missing.", this);

        if (cardPrefab == null)
            Debug.LogError("[CardManager] Card prefab missing.", this);

        if (handArea == null)
            Debug.LogError("[CardManager] Hand Area missing.", this);

        if (spawnCardAction == null)
            Debug.LogWarning("[CardManager] Spawn Card Input Action is not assigned.", this);
    }


    // ==================================================
    // START NEW ENCOUNTER HAND
    // ==================================================

    public void StartNewEncounterHand()
    {
        Debug.Log("[CardManager] Starting new encounter hand.");

        cardBeingDragged = false;

        StopHandRipple();

        ClearHand();

        CreateSquadHand();

        ArrangeHand();

        PlayHandRipple();
    }


    // ==================================================
    // CLEAR HAND
    // ==================================================

    private void ClearHand()
    {
        Debug.Log("[CardManager] Clearing hand. Cards: " + hand.Count);

        for (int i = hand.Count - 1; i >= 0; i--)
        {
            CardUI card = hand[i];

            if (card != null)
                Destroy(card.gameObject);
        }

        hand.Clear();

        Debug.Log("[CardManager] Hand cleared.");
    }


    // ==================================================
    // CREATE SQUAD HAND
    // ==================================================

    private void CreateSquadHand()
    {
        Debug.Log("[CardManager] Creating squad hand.");

        if (GameSession.Instance == null)
        {
            Debug.LogError("[CardManager] GameSession does not exist!", this);
            return;
        }

        SquadSO selectedSquad =
            GameSession.Instance.GetSelectedSquad();

        if (selectedSquad == null)
        {
            Debug.LogError("[CardManager] No squad selected!", this);
            return;
        }

        if (selectedSquad.squadMembers == null ||
            selectedSquad.squadMembers.Length == 0)
        {
            Debug.LogError("[CardManager] Selected squad has no members!", this);
            return;
        }

        foreach (CharacterSO character in selectedSquad.squadMembers)
        {
            if (character == null)
            {
                Debug.LogWarning("[CardManager] Squad contains a null character.");
                continue;
            }

            CreateCard(character);
        }

        Debug.Log("[CardManager] Squad hand created. Total cards: " + hand.Count);
    }


    // ==================================================
    // CREATE CARD
    // ==================================================

    public void CreateCard(CharacterSO character)
    {
        Debug.Log("[CardManager] CreateCard() called.");

        if (cardPrefab == null || handArea == null || character == null)
        {
            Debug.LogError("[CardManager] Cannot create card. Missing reference or character.");
            return;
        }

        GameObject cardObject = Instantiate(
            cardPrefab,
            handArea,
            false
        );

        cardObject.name = character.characterName + "_Card";

        RectTransform rect = cardObject.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogError("[CardManager] Card prefab has no RectTransform.");
            Destroy(cardObject);
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (!cardObject.TryGetComponent<CardUI>(out CardUI card))
        {
            Debug.LogError("[CardManager] Card prefab is missing CardUI.", cardObject);
            Destroy(cardObject);
            return;
        }

        card.Setup(this, character);

        hand.Add(card);

        Debug.Log("[CardManager] Card registered: " + card.name +
                  " | Total tracked cards: " + hand.Count);

        ArrangeHand();
    }


    // ==================================================
    // REMOVE CARD
    // ==================================================

    public void RemoveCard(CardUI card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardManager] Tried to remove a null card.");
            return;
        }

        if (!hand.Contains(card))
        {
            Debug.LogWarning("[CardManager] Card was not tracked: " + card.name);
            return;
        }

        Debug.Log("[CardManager] Removing card: " + card.name);

        StopHandRipple();

        hand.Remove(card);

        ArrangeHand();

        Debug.Log("[CardManager] Card removed: " + card.name);

        if (hand.Count > 0)
            PlayHandRipple();
    }


    // ==================================================
    // RETURN CARD TO HAND
    // ==================================================

    public void ReturnCardToHand(CardUI card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardManager] Cannot return null card.");
            return;
        }

        if (hand.Contains(card))
        {
            Debug.Log("[CardManager] Card already belongs to hand: " + card.name);
            return;
        }

        StopHandRipple();

        card.transform.SetParent(handArea, false);

        hand.Add(card);

        Debug.Log("[CardManager] Card returned to hand: " + card.name);

        ArrangeHand();

        PlayHandRipple();
    }


    // ==================================================
    // ARRANGE HAND
    // ==================================================

    public void ArrangeHand()
    {
        hand.RemoveAll(card => card == null);

        int count = hand.Count;

        if (count == 0)
            return;

        float startY = 0f;

        for (int i = 0; i < count; i++)
        {
            CardUI card = hand[i];

            if (card == null)
                continue;

            RectTransform rect = card.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            float yPosition = startY - (i * cardSpacing);

            Vector2 newPosition = new Vector2(0f, yPosition);

            rect.anchoredPosition = newPosition;

            rect.SetSiblingIndex(i);

            card.UpdateHandPosition();
        }

        Debug.Log("[CardManager] Hand arranged from top to bottom. Cards: " + hand.Count);
    }


    // ==================================================
    // CARD DRAG STARTED
    // ==================================================

    public void OnCardDragStarted()
    {
        cardBeingDragged = true;

        Debug.Log("[CardManager] Card drag started. Stopping ripple.");

        StopHandRipple();
    }


    // ==================================================
    // CARD DRAG ENDED
    // ==================================================

    public void OnCardDragEnded(bool returnedToHand)
    {
        cardBeingDragged = false;

        Debug.Log("[CardManager] Card drag ended.");

        if (returnedToHand && hand.Count > 0)
        {
            ArrangeHand();

            PlayHandRipple();
        }
    }


    // ==================================================
    // PLAY HAND RIPPLE
    // ==================================================

    public void PlayHandRipple()
    {
        if (!continuousRipple)
            return;

        if (cardBeingDragged)
            return;

        if (rippleCoroutine != null)
            return;

        if (hand.Count == 0)
            return;

        Debug.Log("[CardManager] Starting continuous hand ripple.");

        rippleCoroutine = StartCoroutine(HandRippleRoutine());
    }


    // ==================================================
    // STOP HAND RIPPLE
    // ==================================================

    public void StopHandRipple()
    {
        if (rippleCoroutine != null)
        {
            StopCoroutine(rippleCoroutine);
            rippleCoroutine = null;
        }

        StopAllCoroutines();

        foreach (CardUI card in hand)
        {
            if (card == null)
                continue;

            RectTransform rect = card.GetComponent<RectTransform>();

            if (rect != null)
                rect.anchoredPosition = card.GetHandPosition();
        }

        Debug.Log("[CardManager] Ripple stopped.");
    }


    // ==================================================
    // CONTINUOUS HAND RIPPLE
    // ==================================================

    private IEnumerator HandRippleRoutine()
    {
        Debug.Log("[CardManager] Continuous ripple routine started.");

        // WAIT BEFORE THE FIRST RIPPLE
        if (rippleStartDelay > 0f)
            yield return new WaitForSeconds(rippleStartDelay);

        while (continuousRipple && hand.Count > 0 && !cardBeingDragged)
        {
            hand.RemoveAll(card => card == null);

            if (hand.Count == 0 || cardBeingDragged)
                break;

            List<RectTransform> cards = new List<RectTransform>();

            List<Vector2> originalPositions = new List<Vector2>();

            foreach (CardUI card in hand)
            {
                if (card == null)
                    continue;

                RectTransform rect = card.GetComponent<RectTransform>();

                if (rect == null)
                    continue;

                cards.Add(rect);

                originalPositions.Add(card.GetHandPosition());
            }

            if (cards.Count == 0)
                break;

            // START EACH CARD INDEPENDENTLY

            for (int i = 0; i < cards.Count; i++)
            {
                if (cardBeingDragged)
                    break;

                if (cards[i] == null)
                    continue;

                Vector2 original = originalPositions[i];

                Vector2 leftPosition =
                    original + Vector2.left * rippleDistance;

                StartCoroutine(
                    AnimateCardRipple(
                        cards[i],
                        original,
                        leftPosition
                    )
                );

                if (rippleDelay > 0f)
                    yield return new WaitForSeconds(rippleDelay);
            }

            if (cardBeingDragged)
                break;

            // WAIT FOR ENTIRE WAVE

            float waveDuration =
                rippleDuration * 2f +
                Mathf.Max(0, cards.Count - 1) * rippleDelay;

            yield return new WaitForSeconds(waveDuration);

            if (cardBeingDragged)
                break;

            // COOLDOWN

            if (rippleCooldown > 0f)
                yield return new WaitForSeconds(rippleCooldown);
        }

        Debug.Log("[CardManager] Continuous ripple routine finished.");

        rippleCoroutine = null;
    }


    // ==================================================
    // INDEPENDENT CARD RIPPLE ANIMATION
    // ==================================================

    private IEnumerator AnimateCardRipple(
        RectTransform rect,
        Vector2 original,
        Vector2 leftPosition)
    {
        if (rect == null || cardBeingDragged)
            yield break;

        float duration = Mathf.Max(0.001f, rippleDuration);

        // MOVE LEFT

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rect == null || cardBeingDragged)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition =
                Vector2.Lerp(original, leftPosition, t);

            yield return null;
        }

        if (rect == null || cardBeingDragged)
            yield break;

        rect.anchoredPosition = leftPosition;


        // MOVE BACK

        elapsed = 0f;

        while (elapsed < duration)
        {
            if (rect == null || cardBeingDragged)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition =
                Vector2.Lerp(leftPosition, original, t);

            yield return null;
        }

        if (rect != null && !cardBeingDragged)
            rect.anchoredPosition = original;
    }


    // ==================================================
    // GETTERS
    // ==================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public Camera GetCamera()
    {
        return mainCamera;
    }

    public IReadOnlyList<CardUI> GetHandCards()
    {
        return hand;
    }

    public int GetHandCount()
    {
        return hand.Count;
    }

    public bool IsCardTracked(CardUI card)
    {
        return card != null && hand.Contains(card);
    }
}
