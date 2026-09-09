using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeChoiceUI : MonoBehaviour
{
    [Header("Update Manager")]
    [SerializeField]
    private UpdateManager updateManager;

    [Header("Victory Manager")]
    [SerializeField]
    private VictoryManager victoryManager;

    [Header("Upgrade Canvas")]
    [SerializeField]
    private GameObject upgradeCanvas;

    [Header("Buttons")]
    [SerializeField]
    private Button[] upgradeButtons;

    [Header("Button Text")]
    [SerializeField]
    private TMP_Text[] upgradeTexts;

    private UpgradeSO[] currentChoices;

    private const int CHOICE_COUNT = 3;

    private void Awake()
    {
        // Start with the upgrade canvas hidden.
        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Upgrade Canvas is not assigned."
            );
        }

        // Validate buttons.
        if (upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] You need exactly 3 upgrade buttons."
            );

            return;
        }

        // Validate text.
        if (upgradeTexts == null ||
            upgradeTexts.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] You need exactly 3 TMP text components."
            );

            return;
        }

        // Create choice array.
        currentChoices = new UpgradeSO[CHOICE_COUNT];

        // Find VictoryManager if it hasn't been assigned.
        if (victoryManager == null)
        {
            victoryManager = FindFirstObjectByType<VictoryManager>();
        }

        // Add button listeners.
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            int buttonIndex = i;

            upgradeButtons[i].onClick.AddListener(
                () => SelectUpgrade(buttonIndex)
            );
        }
    }

    public void ShowUpgradeChoices()
    {
        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] UpdateManager is not assigned."
            );

            return;
        }

        if (upgradeCanvas == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Upgrade Canvas is not assigned."
            );

            return;
        }

        CharacterSO character = updateManager.GetCurrentCharacter();

        if (character == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] There is no current character."
            );

            return;
        }

        UpgradeSO[] availableUpgrades =
            updateManager.GetCurrentCharacterUpgrades();

        if (availableUpgrades == null ||
            availableUpgrades.Length == 0)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                character.characterName +
                " has no available upgrades."
            );

            if (victoryManager != null)
            {
                victoryManager.OnUpgradeSelected();
            }

            return;
        }

        Debug.Log(
            "[UpgradeChoiceUI] Showing upgrades for " +
            character.characterName
        );

        // Reset choices.
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            currentChoices[i] = null;
        }

        // Generate random unique choices.
        GenerateChoices(availableUpgrades);

        // Display choices.
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (currentChoices[i] == null)
            {
                upgradeButtons[i].gameObject.SetActive(false);
                continue;
            }

            upgradeButtons[i].gameObject.SetActive(true);

            upgradeTexts[i].text = currentChoices[i].name;
        }

        // Turn the upgrade canvas ON.
        upgradeCanvas.SetActive(true);
    }

    private void GenerateChoices(UpgradeSO[] availableUpgrades)
    {
        int availableCount = availableUpgrades.Length;

        if (availableCount < CHOICE_COUNT)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] " +
                "Character has fewer than 3 upgrades. " +
                "Showing all available upgrades."
            );
        }

        // Make a copy so we don't modify the original array.
        UpgradeSO[] pool = new UpgradeSO[availableCount];

        for (int i = 0; i < availableCount; i++)
        {
            pool[i] = availableUpgrades[i];
        }

        // Shuffle the pool.
        for (int i = 0; i < pool.Length; i++)
        {
            int randomIndex = Random.Range(i, pool.Length);

            UpgradeSO temp = pool[i];

            pool[i] = pool[randomIndex];
            pool[randomIndex] = temp;
        }

        // Take the first 3 upgrades.
        int choiceCount = Mathf.Min(
            CHOICE_COUNT,
            pool.Length
        );

        for (int i = 0; i < choiceCount; i++)
        {
            currentChoices[i] = pool[i];
        }
    }

    private void SelectUpgrade(int index)
    {
        if (currentChoices == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Current choices array is null."
            );

            return;
        }

        if (index < 0 ||
            index >= currentChoices.Length)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Invalid upgrade index."
            );

            return;
        }

        UpgradeSO selectedUpgrade = currentChoices[index];

        if (selectedUpgrade == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Selected upgrade is null."
            );

            return;
        }

        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] UpdateManager is not assigned."
            );

            return;
        }

        CharacterSO character =
            updateManager.GetCurrentCharacter();

        if (character == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Current character is null."
            );

            return;
        }

        Debug.Log(
            "[UpgradeChoiceUI] Player selected " +
            selectedUpgrade.name +
            " for " +
            character.characterName
        );

        // Apply the selected upgrade.
        updateManager.ApplyUpgrade(selectedUpgrade);

        // Hide the upgrade canvas.
        HideUpgradeChoices();

        // Tell VictoryManager that an upgrade was selected.
        if (victoryManager != null)
        {
            victoryManager.OnUpgradeSelected();
        }
        else
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] VictoryManager is not assigned."
            );
        }
    }

    public void HideUpgradeChoices()
    {
        if (upgradeCanvas == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Upgrade Canvas is not assigned."
            );

            return;
        }

        // Turn the upgrade canvas OFF.
        upgradeCanvas.SetActive(false);
    }
}