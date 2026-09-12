using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeChoiceUI : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private UpdateManager updateManager;

    [SerializeField]
    private VictoryManager victoryManager;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField]
    private GameObject upgradeCanvas;

    [SerializeField]
    private Button[] upgradeButtons;

    [SerializeField]
    private TMP_Text[] upgradeTexts;


    // ============================================================
    // CURRENT CHOICES
    // ============================================================

    private UpgradeSO[] currentChoices;


    // ============================================================
    // CALLBACK
    // ============================================================

    private Action onUpgradeSelected;


    // ============================================================
    // CONSTANTS
    // ============================================================

    private const int CHOICE_COUNT = 3;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        if (victoryManager == null)
        {
            victoryManager =
                FindFirstObjectByType<VictoryManager>();
        }


        currentChoices =
            new UpgradeSO[CHOICE_COUNT];


        if (
            upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT
        )
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "Exactly 3 upgrade buttons are required.",
                this
            );
        }


        if (
            upgradeTexts == null ||
            upgradeTexts.Length != CHOICE_COUNT
        )
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "Exactly 3 upgrade texts are required.",
                this
            );
        }


        if (upgradeButtons != null)
        {
            for (
                int i = 0;
                i < upgradeButtons.Length;
                i++
            )
            {
                int index = i;


                if (upgradeButtons[i] == null)
                {
                    continue;
                }


                upgradeButtons[i]
                    .onClick
                    .RemoveAllListeners();


                upgradeButtons[i]
                    .onClick
                    .AddListener(
                        () => SelectUpgrade(index)
                    );
            }
        }


        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(false);
        }
    }


    // ============================================================
    // STARTING UPGRADE CHOICE
    // ============================================================

    public void ShowStartingUpgradeChoice(
        Action onSelected = null
    )
    {
        onUpgradeSelected =
            onSelected;


        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        if (updateManager == null)
        {
            CompleteSelection();

            return;
        }


        CharacterSO character =
            updateManager.GetCurrentCharacter();


        if (character == null)
        {
            CompleteSelection();

            return;
        }


        UpgradeSO[] availableUpgrades =
            updateManager
                .GetCurrentCharacterUpgrades();


        if (
            availableUpgrades == null ||
            availableUpgrades.Length == 0
        )
        {
            CompleteSelection();

            return;
        }


        ClearCurrentChoices();


        GenerateChoices(
            availableUpgrades
        );


        SetupButtons();


        ShowUpgradeCanvas();
    }


    // ============================================================
    // VICTORY UPGRADE CHOICE
    // ============================================================

    public void ShowUpgradeChoices(
        Action onSelected = null
    )
    {
        onUpgradeSelected =
            onSelected;


        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        if (updateManager == null)
        {
            CompleteSelection();

            return;
        }


        CharacterSO character =
            updateManager.GetCurrentCharacter();


        if (character == null)
        {
            CompleteSelection();

            return;
        }


        UpgradeSO[] availableUpgrades =
            updateManager
                .GetCurrentCharacterUpgrades();


        if (
            availableUpgrades == null ||
            availableUpgrades.Length == 0
        )
        {
            CompleteSelection();

            return;
        }


        ClearCurrentChoices();


        GenerateChoices(
            availableUpgrades
        );


        SetupButtons();


        ShowUpgradeCanvas();
    }


    // ============================================================
    // CLEAR CURRENT CHOICES
    // ============================================================

    private void ClearCurrentChoices()
    {
        if (currentChoices == null)
        {
            currentChoices =
                new UpgradeSO[CHOICE_COUNT];
        }


        for (
            int i = 0;
            i < currentChoices.Length;
            i++
        )
        {
            currentChoices[i] = null;
        }
    }


    // ============================================================
    // GENERATE CHOICES
    // ============================================================

    private void GenerateChoices(
        UpgradeSO[] availableUpgrades
    )
    {
        if (availableUpgrades == null)
        {
            return;
        }


        UpgradeSO[] shuffled =
            new UpgradeSO[
                availableUpgrades.Length
            ];


        Array.Copy(
            availableUpgrades,
            shuffled,
            availableUpgrades.Length
        );


        for (
            int i = shuffled.Length - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );


            UpgradeSO temp =
                shuffled[i];


            shuffled[i] =
                shuffled[randomIndex];


            shuffled[randomIndex] =
                temp;
        }


        int count =
            Mathf.Min(
                CHOICE_COUNT,
                shuffled.Length
            );


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            currentChoices[i] =
                shuffled[i];
        }
    }


    // ============================================================
    // SETUP BUTTONS
    // ============================================================

    private void SetupButtons()
    {
        if (upgradeButtons == null)
        {
            return;
        }


        for (
            int i = 0;
            i < upgradeButtons.Length;
            i++
        )
        {
            Button button =
                upgradeButtons[i];


            if (button == null)
            {
                continue;
            }


            UpgradeSO upgrade = null;


            if (
                currentChoices != null &&
                i < currentChoices.Length
            )
            {
                upgrade =
                    currentChoices[i];
            }


            if (upgrade == null)
            {
                button.gameObject.SetActive(false);


                if (
                    upgradeTexts != null &&
                    i < upgradeTexts.Length &&
                    upgradeTexts[i] != null
                )
                {
                    upgradeTexts[i].text =
                        string.Empty;
                }


                continue;
            }


            button.gameObject.SetActive(true);


            if (
                upgradeTexts != null &&
                i < upgradeTexts.Length &&
                upgradeTexts[i] != null
            )
            {
                upgradeTexts[i].text =
                    upgrade.name;
            }
        }
    }


    // ============================================================
    // SHOW UPGRADE CANVAS
    // ============================================================

    private void ShowUpgradeCanvas()
    {
        if (upgradeCanvas == null)
        {
            return;
        }


        upgradeCanvas.SetActive(true);
    }


    // ============================================================
    // SELECT UPGRADE
    // ============================================================

    private void SelectUpgrade(
        int index
    )
    {
        if (
            currentChoices == null ||
            index < 0 ||
            index >= currentChoices.Length
        )
        {
            return;
        }


        UpgradeSO selectedUpgrade =
            currentChoices[index];


        if (selectedUpgrade == null)
        {
            return;
        }


        if (updateManager == null)
        {
            return;
        }


        Debug.Log(
            "[UpgradeChoiceUI] " +
            "Selected upgrade: " +
            selectedUpgrade.name
        );


        // ========================================================
        // UPDATE MANAGER STORES THE UPGRADE
        // ========================================================

        updateManager.ApplyUpgrade(
            selectedUpgrade
        );


        CompleteSelection();
    }


    // ============================================================
    // COMPLETE SELECTION
    // ============================================================

    private void CompleteSelection()
    {
        HideUpgradeChoices();


        Action callback =
            onUpgradeSelected;


        onUpgradeSelected =
            null;


        if (callback != null)
        {
            callback();

            return;
        }


        if (victoryManager != null)
        {
            victoryManager.OnUpgradeSelected();
        }
    }


    // ============================================================
    // HIDE UPGRADE CHOICES
    // ============================================================

    public void HideUpgradeChoices()
    {
        onUpgradeSelected =
            null;


        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(false);
        }
    }
}