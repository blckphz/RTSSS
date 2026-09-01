using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]
    [SerializeField]
    private GridManager gridManager;


    // ==================================================
    // CARD SETTINGS
    // ==================================================

    [Header("Card Settings")]
    [SerializeField]
    private GameObject cardPrefab;


    // ==================================================
    // HAND SETTINGS
    // ==================================================

    [Header("Hand Settings")]
    [SerializeField]
    private Transform handArea;

    [SerializeField]
    private float cardSpacing = 120f;


    // ==================================================
    // GRID PLACEMENT
    // ==================================================

    [Header("Grid Placement")]
    [SerializeField]
    private Camera mainCamera;


    // ==================================================
    // HAND
    // ==================================================

    private readonly List<CardUI> hand =
        new List<CardUI>();


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        InitializeReferences();

        ValidateReferences();
    }


    private void Start()
    {
        // Do NOT create the hand here anymore.
        //
        // EncounterManager will create the hand
        // whenever a new encounter starts.
    }


    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void InitializeReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void ValidateReferences()
    {
        if (gridManager == null)
        {
            Debug.LogError(
                "[CardManager] GridManager missing.",
                this
            );
        }


        if (mainCamera == null)
        {
            Debug.LogError(
                "[CardManager] Main Camera missing.",
                this
            );
        }


        if (cardPrefab == null)
        {
            Debug.LogError(
                "[CardManager] Card prefab missing.",
                this
            );
        }


        if (handArea == null)
        {
            Debug.LogError(
                "[CardManager] Hand Area missing.",
                this
            );
        }
    }


    // ==================================================
    // START NEW ENCOUNTER HAND
    // ==================================================

    public void StartNewEncounterHand()
    {

        // Remove any cards left from the previous encounter.
        ClearHand();


        // Create fresh cards from the selected squad.
        CreateSquadHand();
    }


    // ==================================================
    // CLEAR HAND
    // ==================================================

    private void ClearHand()
    {
        for (
            int i = hand.Count - 1;
            i >= 0;
            i--
        )
        {
            CardUI card =
                hand[i];


            if (card != null)
            {
                Destroy(
                    card.gameObject
                );
            }
        }


        hand.Clear();
    }


    // ==================================================
    // CREATE SQUAD HAND
    // ==================================================

    private void CreateSquadHand()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "[CardManager] GameSession does not exist!",
                this
            );

            return;
        }


        SquadSO selectedSquad =
            GameSession.Instance.GetSelectedSquad();


        if (selectedSquad == null)
        {
            Debug.LogError(
                "[CardManager] No squad selected!",
                this
            );

            return;
        }


        if (
            selectedSquad.squadMembers == null ||
            selectedSquad.squadMembers.Length == 0
        )
        {
            Debug.LogError(
                "[CardManager] Selected squad has no members!",
                this
            );

            return;
        }


        foreach (
            CharacterSO character
            in selectedSquad.squadMembers
        )
        {
            if (character == null)
            {
                Debug.LogWarning(
                    "[CardManager] Squad contains a null character.",
                    this
                );

                continue;
            }


            CreateCard(
                character
            );
        }


        ArrangeHand();
    }


    // ==================================================
    // CREATE CARD
    // ==================================================

    public void CreateCard(
        CharacterSO character)
    {
        if (cardPrefab == null)
        {
            Debug.LogError(
                "[CardManager] Card prefab missing.",
                this
            );

            return;
        }


        if (handArea == null)
        {
            Debug.LogError(
                "[CardManager] Hand Area missing.",
                this
            );

            return;
        }


        if (character == null)
        {
            Debug.LogWarning(
                "[CardManager] Cannot create card. Character is null.",
                this
            );

            return;
        }


        GameObject cardObject =
            Instantiate(
                cardPrefab,
                handArea
            );


        cardObject.name =
            character.characterName +
            "_Card";


        if (
            !cardObject.TryGetComponent<CardUI>(
                out CardUI card
            )
        )
        {
            Debug.LogError(
                "[CardManager] Card prefab is missing a CardUI component.",
                cardObject
            );


            Destroy(
                cardObject
            );

            return;
        }


        card.Setup(
            this,
            character
        );


        hand.Add(
            card
        );
    }


    // ==================================================
    // REMOVE CARD
    // ==================================================

    public void RemoveCard(
        CardUI card)
    {
        if (card == null)
        {
            return;
        }


        if (!hand.Remove(card))
        {
            return;
        }


        ArrangeHand();
    }


    // ==================================================
    // RETURN CARD TO HAND
    // ==================================================

    public void ReturnCardToHand(
        CardUI card)
    {
        if (card == null)
        {
            return;
        }


        if (hand.Contains(card))
        {
            return;
        }


        card.transform.SetParent(
            handArea,
            false
        );


        hand.Add(
            card
        );


        ArrangeHand();
    }


    // ==================================================
    // ARRANGE HAND
    // ==================================================

    public void ArrangeHand()
    {
        hand.RemoveAll(
            card => card == null
        );


        int count =
            hand.Count;


        if (count == 0)
        {
            return;
        }


        float totalWidth =
            (count - 1) *
            cardSpacing;


        float startX =
            -totalWidth *
            0.5f;


        Vector2 cachedPosition =
            Vector2.zero;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            CardUI card =
                hand[i];


            if (card == null)
            {
                continue;
            }


            if (
                !card.TryGetComponent<RectTransform>(
                    out RectTransform rect
                )
            )
            {
                continue;
            }


            cachedPosition.x =
                startX +
                (
                    i *
                    cardSpacing
                );


            rect.anchoredPosition =
                cachedPosition;


            rect.SetSiblingIndex(
                i
            );
        }
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
}