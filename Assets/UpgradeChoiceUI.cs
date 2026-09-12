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
        // --------------------------------------------------------
        // FIND UPDATE MANAGER
        // --------------------------------------------------------

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        // --------------------------------------------------------
        // FIND VICTORY MANAGER
        // --------------------------------------------------------

        if (victoryManager == null)
        {
            victoryManager =
                FindFirstObjectByType<VictoryManager>();
        }


        // --------------------------------------------------------
        // CREATE CHOICE ARRAY
        // --------------------------------------------------------

        currentChoices =
            new UpgradeSO[CHOICE_COUNT];


        // --------------------------------------------------------
        // VALIDATE BUTTONS
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // VALIDATE TEXTS
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // SET BUTTON LISTENERS
        // --------------------------------------------------------

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


                upgradeButtons[i].onClick.RemoveAllListeners();


                upgradeButtons[i].onClick.AddListener(
                    () => SelectUpgrade(index)
                );
            }
        }


        // --------------------------------------------------------
        // HIDE CANVAS
        // --------------------------------------------------------

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
        // --------------------------------------------------------
        // SAVE CALLBACK
        // --------------------------------------------------------

        onUpgradeSelected =
            onSelected;


        // --------------------------------------------------------
        // CHECK UPDATE MANAGER
        // --------------------------------------------------------

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "UpdateManager is missing!",
                this
            );

            CompleteSelection();

            return;
        }


        // --------------------------------------------------------
        // GET CURRENT CHARACTER
        // --------------------------------------------------------

        CharacterSO character =
            updateManager.GetCurrentCharacter();


        if (character == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "Current character is missing!",
                this
            );

            CompleteSelection();

            return;
        }


        Debug.Log(
            "[UpgradeChoiceUI] Showing starting upgrades for " +
            character.characterName
        );


        // --------------------------------------------------------
        // GET CHARACTER UPGRADES
        // --------------------------------------------------------

        UpgradeSO[] availableUpgrades =
            updateManager.GetCurrentCharacterUpgrades();


        // --------------------------------------------------------
        // CHECK UPGRADES
        // --------------------------------------------------------

        if (
            availableUpgrades == null ||
            availableUpgrades.Length == 0
        )
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                "Current character has no upgrades.",
                this
            );

            CompleteSelection();

            return;
        }


        // --------------------------------------------------------
        // CLEAR OLD CHOICES
        // --------------------------------------------------------

        ClearCurrentChoices();


        // --------------------------------------------------------
        // GENERATE CHOICES
        // --------------------------------------------------------

        GenerateChoices(
            availableUpgrades
        );


        // --------------------------------------------------------
        // SET BUTTONS
        // --------------------------------------------------------

        SetupButtons();


        // --------------------------------------------------------
        // SHOW CANVAS
        // --------------------------------------------------------

        ShowUpgradeCanvas(
            "Starting upgrades"
        );
    }


    // ============================================================
    // VICTORY UPGRADE CHOICE
    // ============================================================

    public void ShowUpgradeChoices(
        Action onSelected = null
    )
    {
        // --------------------------------------------------------
        // SAVE CALLBACK
        // --------------------------------------------------------

        onUpgradeSelected =
            onSelected;


        // --------------------------------------------------------
        // CHECK UPDATE MANAGER
        // --------------------------------------------------------

        if (updateManager == null)
        {
            updateManager =
                FindFirstObjectByType<UpdateManager>();
        }


        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "UpdateManager is missing!",
                this
            );

            CompleteSelection();

            return;
        }


        // --------------------------------------------------------
        // GET CURRENT CHARACTER
        // --------------------------------------------------------

        CharacterSO character =
            updateManager.GetCurrentCharacter();


        if (character == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "Current character is missing!",
                this
            );

            CompleteSelection();

            return;
        }


        Debug.Log(
            "[UpgradeChoiceUI] Showing victory upgrades for " +
            character.characterName
        );


        // --------------------------------------------------------
        // GET CHARACTER UPGRADES
        // --------------------------------------------------------

        UpgradeSO[] availableUpgrades =
            updateManager.GetCurrentCharacterUpgrades();


        // --------------------------------------------------------
        // CHECK UPGRADES
        // --------------------------------------------------------

        if (
            availableUpgrades == null ||
            availableUpgrades.Length == 0
        )
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                "Current character has no upgrades.",
                this
            );

            CompleteSelection();

            return;
        }


        // --------------------------------------------------------
        // CLEAR OLD CHOICES
        // --------------------------------------------------------

        ClearCurrentChoices();


        // --------------------------------------------------------
        // GENERATE CHOICES
        // --------------------------------------------------------

        GenerateChoices(
            availableUpgrades
        );


        // --------------------------------------------------------
        // SET BUTTONS
        // --------------------------------------------------------

        SetupButtons();


        // --------------------------------------------------------
        // SHOW CANVAS
        // --------------------------------------------------------

        ShowUpgradeCanvas(
            "Victory upgrades"
        );
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


        // --------------------------------------------------------
        // SHUFFLE
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // SELECT THREE
        // --------------------------------------------------------

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


            // ----------------------------------------------------
            // EMPTY SLOT
            // ----------------------------------------------------

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


            // ----------------------------------------------------
            // ENABLE BUTTON
            // ----------------------------------------------------

            button.gameObject.SetActive(true);


            // ----------------------------------------------------
            // SET TEXT
            // ----------------------------------------------------

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

    private void ShowUpgradeCanvas(
        string context
    )
    {
        if (upgradeCanvas == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "Upgrade Canvas is not assigned!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // ENABLE CANVAS
        // --------------------------------------------------------

        upgradeCanvas.SetActive(true);


        // --------------------------------------------------------
        // CHECK CANVAS STATE
        // --------------------------------------------------------

        Debug.Log(
            "[UpgradeChoiceUI] " +
            context +
            " canvas state: activeSelf=" +
            upgradeCanvas.activeSelf +
            ", activeInHierarchy=" +
            upgradeCanvas.activeInHierarchy,
            upgradeCanvas
        );
    }


    // ============================================================
    // SELECT UPGRADE
    // ============================================================

    private void SelectUpgrade(
        int index
    )
    {
        // --------------------------------------------------------
        // CHECK INDEX
        // --------------------------------------------------------

        if (
            currentChoices == null ||
            index < 0 ||
            index >= currentChoices.Length
        )
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                "Invalid upgrade index."
            );

            return;
        }


        // --------------------------------------------------------
        // GET SELECTED UPGRADE
        // --------------------------------------------------------

        UpgradeSO selectedUpgrade =
            currentChoices[index];


        if (selectedUpgrade == null)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                "Selected upgrade is null."
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK UPDATE MANAGER
        // --------------------------------------------------------

        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] " +
                "UpdateManager is missing!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // APPLY UPGRADE
        // --------------------------------------------------------

        Debug.Log(
            "[UpgradeChoiceUI] Selected upgrade: " +
            selectedUpgrade.name
        );


        updateManager.ApplyUpgrade(
            selectedUpgrade
        );


        // --------------------------------------------------------
        // COMPLETE
        // --------------------------------------------------------

        CompleteSelection();
    }


    // ============================================================
    // COMPLETE SELECTION
    // ============================================================

    private void CompleteSelection()
    {
        // --------------------------------------------------------
        // HIDE UI
        // --------------------------------------------------------

        HideUpgradeChoices();


        // --------------------------------------------------------
        // SAVE CALLBACK
        // --------------------------------------------------------

        Action callback =
            onUpgradeSelected;


        onUpgradeSelected =
            null;


        // --------------------------------------------------------
        // INVOKE CALLBACK
        // --------------------------------------------------------

        if (callback != null)
        {
            callback();

            return;
        }


        // --------------------------------------------------------
        // DEFAULT VICTORY CALLBACK
        // --------------------------------------------------------

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
        // --------------------------------------------------------
        // CLEAR CALLBACK
        // --------------------------------------------------------

        onUpgradeSelected =
            null;


        // --------------------------------------------------------
        // HIDE CANVAS
        // --------------------------------------------------------

        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(false);
        }
    }
}