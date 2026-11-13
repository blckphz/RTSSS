
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeChoiceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpdateManager updateManager;
    [SerializeField] private VictoryManager victoryManager;

    [Header("UI")]
    [SerializeField] private GameObject upgradeCanvas;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeTexts;

    private UpgradeSO[] currentChoices;
    private Action onUpgradeSelected;

    private const int CHOICE_COUNT = 3;

    private void Awake()
    {
        Debug.Log("[UpgradeChoiceUI] Awake started.", this);

        if (updateManager == null)
            updateManager = FindFirstObjectByType<UpdateManager>();

        if (victoryManager == null)
            victoryManager = FindFirstObjectByType<VictoryManager>();

        Debug.Log(
            "[UpgradeChoiceUI] UpdateManager: " +
            (updateManager != null ? "FOUND" : "MISSING"),
            this
        );

        currentChoices = new UpgradeSO[CHOICE_COUNT];

        if (upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Exactly 3 buttons are required.",
                this
            );
        }

        if (upgradeTexts == null ||
            upgradeTexts.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Exactly 3 text references are required.",
                this
            );
        }

        if (upgradeButtons != null)
        {
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int index = i;

                if (upgradeButtons[i] == null)
                {
                    Debug.LogWarning(
                        "[UpgradeChoiceUI] Button " + i + " is NULL.",
                        this
                    );
                    continue;
                }

                upgradeButtons[i].onClick.RemoveAllListeners();

                upgradeButtons[i].onClick.AddListener(
                    () => SelectUpgrade(index)
                );

                Debug.Log(
                    "[UpgradeChoiceUI] Connected button " + i,
                    this
                );
            }
        }

        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(false);

            Debug.Log(
                "[UpgradeChoiceUI] Upgrade canvas hidden on Awake.",
                this
            );
        }
        else
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Upgrade Canvas is NULL.",
                this
            );
        }

        Debug.Log("[UpgradeChoiceUI] Awake completed.", this);
    }

    public void ShowStartingUpgradeChoice(Action onSelected = null)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Starting upgrade choice requested.",
            this
        );

        ShowChoices(onSelected);
    }

    public void ShowUpgradeChoices(Action onSelected = null)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Victory upgrade choice requested.",
            this
        );

        ShowChoices(onSelected);
    }

    private void ShowChoices(Action onSelected)
    {
        Debug.Log("[UpgradeChoiceUI] ShowChoices started.", this);

        onUpgradeSelected = onSelected;

        if (updateManager == null)
            updateManager = FindFirstObjectByType<UpdateManager>();

        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] UpdateManager not found.",
                this
            );

            CompleteSelection();
            return;
        }

        CharacterSO character = updateManager.GetCurrentCharacter();

        if (character == null)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] Current character is NULL.",
                this
            );

            CompleteSelection();
            return;
        }

        Debug.Log(
            "[UpgradeChoiceUI] Current character: " + character.name,
            this
        );

        List<UpgradeSO> availableUpgrades =
            updateManager.GetCurrentCharacterUpgrades();

        Debug.Log(
            "[UpgradeChoiceUI] Available upgrade count: " +
            (availableUpgrades != null ? availableUpgrades.Count : 0),
            this
        );

        if (availableUpgrades == null ||
            availableUpgrades.Count == 0)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] No available upgrades to display.",
                this
            );

            CompleteSelection();
            return;
        }

        ClearCurrentChoices();

        GenerateChoices(availableUpgrades);

        SetupButtons();

        ShowUpgradeCanvas();

        Debug.Log("[UpgradeChoiceUI] ShowChoices completed.", this);
    }

    private void ClearCurrentChoices()
    {
        Debug.Log(
            "[UpgradeChoiceUI] Clearing previous choices.",
            this
        );

        if (currentChoices == null)
            currentChoices = new UpgradeSO[CHOICE_COUNT];

        for (int i = 0; i < currentChoices.Length; i++)
            currentChoices[i] = null;
    }

    private void GenerateChoices(List<UpgradeSO> availableUpgrades)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Generating random choices.",
            this
        );

        List<UpgradeSO> normalPool = new List<UpgradeSO>();
        List<UpgradeSO> artifactPool = new List<UpgradeSO>();

        // Separate upgrades into the two pools.
        foreach (UpgradeSO upgrade in availableUpgrades)
        {
            if (upgrade == null)
            {
                Debug.LogWarning(
                    "[UpgradeChoiceUI] Null upgrade skipped.",
                    this
                );

                continue;
            }

            if (upgrade.GetRarity() == UpgradeSO.Rarity.Artifact)
            {
                artifactPool.Add(upgrade);
            }
            else
            {
                // Normal and Epic upgrades share this pool.
                normalPool.Add(upgrade);
            }
        }

        Debug.Log(
            "[UpgradeChoiceUI] Normal/Epic pool size: " +
            normalPool.Count,
            this
        );

        Debug.Log(
            "[UpgradeChoiceUI] Artifact pool size: " +
            artifactPool.Count,
            this
        );

        // Select one pool. Never combine the two pools.
        List<UpgradeSO> selectedPool = null;
        string selectedPoolName = "";

        if (normalPool.Count == 0 && artifactPool.Count == 0)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] Both upgrade pools are empty.",
                this
            );

            return;
        }
        else if (normalPool.Count == 0)
        {
            selectedPool = artifactPool;
            selectedPoolName = "Artifact";
        }
        else if (artifactPool.Count == 0)
        {
            selectedPool = normalPool;
            selectedPoolName = "Normal/Epic";
        }
        else
        {
            bool chooseArtifact = UnityEngine.Random.Range(0, 2) == 0;

            if (chooseArtifact)
            {
                selectedPool = artifactPool;
                selectedPoolName = "Artifact";
            }
            else
            {
                selectedPool = normalPool;
                selectedPoolName = "Normal/Epic";
            }
        }

        Debug.Log(
            "[UpgradeChoiceUI] Selected upgrade pool: " +
            selectedPoolName,
            this
        );

        // Shuffle only the selected pool.
        List<UpgradeSO> shuffled = new List<UpgradeSO>(selectedPool);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            UpgradeSO temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        // Show at most three choices.
        // Do not fill missing choices from the other pool.
        int count = Mathf.Min(CHOICE_COUNT, shuffled.Count);

        for (int i = 0; i < count; i++)
        {
            currentChoices[i] = shuffled[i];

            Debug.Log(
                "[UpgradeChoiceUI] Choice " + (i + 1) +
                ": " + currentChoices[i].UpgradeName +
                " (" + currentChoices[i].GetRarity() + ")",
                this
            );
        }

        Debug.Log(
            "[UpgradeChoiceUI] Generated " + count +
            " choices from the " + selectedPoolName + " pool.",
            this
        );
    }

    private void SetupButtons()
    {
        Debug.Log(
            "[UpgradeChoiceUI] Setting up buttons.",
            this
        );

        if (upgradeButtons == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Button array is NULL.",
                this
            );

            return;
        }

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            Button button = upgradeButtons[i];

            if (button == null)
            {
                Debug.LogWarning(
                    "[UpgradeChoiceUI] Button " + i + " is NULL.",
                    this
                );

                continue;
            }

            UpgradeSO upgrade = currentChoices[i];

            if (upgrade == null)
            {
                button.gameObject.SetActive(false);

                Debug.Log(
                    "[UpgradeChoiceUI] Hiding empty button " + i,
                    this
                );

                if (upgradeTexts != null &&
                    i < upgradeTexts.Length &&
                    upgradeTexts[i] != null)
                {
                    upgradeTexts[i].text = "";
                }

                continue;
            }

            button.gameObject.SetActive(true);
            button.interactable = true;

            if (upgradeTexts != null &&
                i < upgradeTexts.Length &&
                upgradeTexts[i] != null)
            {
                upgradeTexts[i].text = upgrade.UpgradeName;

                Debug.Log(
                    "[UpgradeChoiceUI] Button " + i +
                    " text set to: " + upgrade.UpgradeName,
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    "[UpgradeChoiceUI] Text reference missing for button " + i,
                    this
                );
            }
        }
    }

    private void ShowUpgradeCanvas()
    {
        if (upgradeCanvas == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Cannot show canvas: reference is NULL.",
                this
            );

            return;
        }

        upgradeCanvas.SetActive(true);

        Debug.Log(
            "[UpgradeChoiceUI] Upgrade canvas activated.",
            this
        );
    }

    private void SelectUpgrade(int index)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Button clicked: " + index,
            this
        );

        if (currentChoices == null ||
            index < 0 ||
            index >= currentChoices.Length)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] Invalid choice index.",
                this
            );

            return;
        }

        UpgradeSO selectedUpgrade = currentChoices[index];

        if (selectedUpgrade == null)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] Selected upgrade is NULL.",
                this
            );

            return;
        }

        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] UpdateManager is NULL.",
                this
            );

            return;
        }

        Debug.Log(
            "[UpgradeChoiceUI] Selected upgrade: " +
            selectedUpgrade.UpgradeName,
            this
        );

        updateManager.ApplyUpgrade(selectedUpgrade);

        CompleteSelection();
    }

    private void CompleteSelection()
    {
        Debug.Log(
            "[UpgradeChoiceUI] Completing selection.",
            this
        );

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        Action callback = onUpgradeSelected;
        onUpgradeSelected = null;

        if (callback != null)
        {
            Debug.Log(
                "[UpgradeChoiceUI] Executing selection callback.",
                this
            );

            callback();
        }
        else
        {
            Debug.Log(
                "[UpgradeChoiceUI] No selection callback assigned.",
                this
            );
        }
    }

    public void HideUpgradeChoices()
    {
        Debug.Log(
            "[UpgradeChoiceUI] HideUpgradeChoices called.",
            this
        );

        onUpgradeSelected = null;

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);
    }
}
