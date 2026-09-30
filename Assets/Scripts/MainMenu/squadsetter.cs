using UnityEngine;
using TMPro;
using System.Collections;

public class squadsetter : MonoBehaviour
{
    [Header("World Objects")]
    public GameObject PlayerWorld;
    public GameObject PetWorld;

    [Header("References")]
    public MainMenuClassSelector mmcs;

    [Header("Squads")]
    public SquadSO selectedSquad;
    public SquadSO[] availablesquads;

    [Header("Character UI")]
    public TMP_Text characterNameText;

    [Header("Pet Name Input")]
    [SerializeField] private TMP_InputField petNameInputField;

    [Header("Juice / Animation")]
    [SerializeField] private Transform popTargetTransform;
    [SerializeField] private float popScaleMultiplier = 1.15f;
    [SerializeField] private float popDuration = 0.15f;

    private int currentSquadIndex = 0;

    private SpriteRenderer playerSpriteRenderer;
    private SpriteRenderer petspriterenderer;

    private Vector3 originalContainerScale = Vector3.one;
    private Coroutine popCoroutine;


    void Start()
    {
        if (PlayerWorld != null)
        {
            playerSpriteRenderer =
                PlayerWorld.GetComponent<SpriteRenderer>();
        }

        if (PetWorld != null)
        {
            petspriterenderer =
                PetWorld.GetComponent<SpriteRenderer>();
        }

        if (popTargetTransform != null)
        {
            originalContainerScale =
                popTargetTransform.localScale;
        }

        // Listen for pet name changes
        if (petNameInputField != null)
        {
            petNameInputField.onValueChanged.AddListener(ChangePetName);
        }

        if (availablesquads != null &&
            availablesquads.Length > 0)
        {
            currentSquadIndex = 0;
            RefreshGrid();
        }
    }


    public void SetChar()
    {
        mmcs.selectedSquad = selectedSquad;
    }


    // ============================================================
    // CHANGE PET NAME
    // ============================================================

    private void ChangePetName(string newName)
    {
        if (selectedSquad == null)
            return;

        if (selectedSquad.squadMembers == null ||
            selectedSquad.squadMembers.Length < 2)
            return;

        CharacterSO pet = selectedSquad.squadMembers[1];

        if (pet == null)
            return;

        pet.characterName = newName;
    }


    // ============================================================
    // NEXT BUTTON
    // ============================================================

    public void onNextButtonPressed()
    {
        if (availablesquads == null ||
            availablesquads.Length == 0)
        {
            return;
        }

        currentSquadIndex =
            (currentSquadIndex + 1) %
            availablesquads.Length;

        RefreshGrid();

        ScreenShaker.ShakeScreen(0.5f, 0.1f);
        TriggerPopEffect();
    }


    // ============================================================
    // PREVIOUS BUTTON
    // ============================================================

    public void onPrevButtonPressed()
    {
        if (availablesquads == null ||
            availablesquads.Length == 0)
        {
            return;
        }

        currentSquadIndex =
            (currentSquadIndex - 1 +
             availablesquads.Length) %
            availablesquads.Length;

        RefreshGrid();

        ScreenShaker.ShakeScreen(0.3f, 0.1f);
        TriggerPopEffect();
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private void RefreshGrid()
    {
        selectedSquad =
            availablesquads[currentSquadIndex];

        if (selectedSquad == null)
        {
            Debug.LogWarning("Selected squad is NULL.");
            return;
        }

        if (selectedSquad.squadMembers == null ||
            selectedSquad.squadMembers.Length < 2)
        {
            Debug.LogWarning(
                "Selected squad needs at least 2 members."
            );

            return;
        }

        CharacterSO firstCharacter =
            selectedSquad.squadMembers[0];

        CharacterSO firstCharacterPet =
            selectedSquad.squadMembers[1];

        if (firstCharacter == null)
        {
            Debug.LogWarning(
                "First character in squad is NULL."
            );

            return;
        }

        if (firstCharacterPet == null)
        {
            Debug.LogWarning(
                "Pet in squad is NULL."
            );

            return;
        }


        // Player sprite
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sprite =
                firstCharacter.icon;
        }


        // Pet sprite
        if (petspriterenderer != null)
        {
            petspriterenderer.sprite =
                firstCharacterPet.icon;
        }


        // Player name
        if (characterNameText != null)
        {
            characterNameText.text =
                firstCharacter.characterName;
        }


        // Pet name input
        if (petNameInputField != null)
        {
            petNameInputField.SetTextWithoutNotify(
                firstCharacterPet.characterName
            );
        }
    }


    // ============================================================
    // POP ANIMATION
    // ============================================================

    private void TriggerPopEffect()
    {
        if (popTargetTransform == null)
            return;

        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine =
            StartCoroutine(PopRoutine());
    }


    private IEnumerator PopRoutine()
    {
        float elapsed = 0f;
        float halfDuration = popDuration * 0.5f;

        Vector3 peakScale =
            originalContainerScale *
            popScaleMultiplier;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                elapsed / halfDuration;

            popTargetTransform.localScale =
                Vector3.Lerp(
                    originalContainerScale,
                    peakScale,
                    t
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                elapsed / halfDuration;

            popTargetTransform.localScale =
                Vector3.Lerp(
                    peakScale,
                    originalContainerScale,
                    t
                );

            yield return null;
        }

        popTargetTransform.localScale =
            originalContainerScale;

        popCoroutine = null;
    }
}