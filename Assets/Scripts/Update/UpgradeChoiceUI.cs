
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeChoiceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpdateManager updateManager;

    [Header("UI")]
    [SerializeField] private GameObject upgradeCanvas;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeTexts;

    private UpgradeSO[] currentChoices = new UpgradeSO[3];
    private Action onUpgradeSelected;

    private const int CHOICE_COUNT = 3;

    private bool selectionInProgress;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (updateManager == null)
            updateManager = FindFirstObjectByType<UpdateManager>();

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);
        else
            Debug.LogError(
                "[UpgradeChoiceUI] Upgrade Canvas missing!",
                this
            );

        if (!ValidateButtonsAndTexts())
            return;

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            int index = i;

            if (upgradeButtons[i] == null)
                continue;

            upgradeButtons[i].onClick.RemoveAllListeners();

            upgradeButtons[i].onClick.AddListener(
                () => SelectUpgrade(index)
            );
        }
    }

    private bool ValidateButtonsAndTexts()
    {
        bool valid = true;

        if (upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Assign exactly 3 buttons.",
                this
            );

            valid = false;
        }

        if (upgradeTexts == null ||
            upgradeTexts.Length != CHOICE_COUNT)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Assign exactly 3 texts.",
                this
            );

            valid = false;
        }

        return valid;
    }

    // =====================================================
    // STARTING ARTIFACT
    // =====================================================

    public void ShowStartingUpgradeChoice(Action onSelected = null)
    {
        ShowStartingArtifactChoice(onSelected);
    }

    public void ShowStartingArtifactChoice(Action onSelected = null)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Starting Artifact selection."
        );

        ShowArtifactSelection(onSelected);
    }

    // =====================================================
    // BOSS ARTIFACT REWARD
    // =====================================================

    public void ShowArtifactChoices(Action onSelected = null)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Boss Artifact reward."
        );

        ShowArtifactSelection(onSelected);
    }

    private void ShowArtifactSelection(Action onSelected)
    {
        if (!GetUpdateManager())
        {
            onSelected?.Invoke();
            return;
        }

        ShowChoices(
            GetAvailableUpgrades(
                UpgradeSO.Rarity.Artifact,
                false
            ),
            onSelected
        );
    }

    // =====================================================
    // NORMAL / ELITE RING REWARD
    // =====================================================

    public void ShowRingChoices(Action onSelected = null)
    {
        Debug.Log(
            "[UpgradeChoiceUI] Ring reward."
        );

        if (!GetUpdateManager())
        {
            onSelected?.Invoke();
            return;
        }

        ShowChoices(
            GetAvailableUpgrades(default, true),
            onSelected
        );
    }

    // =====================================================
    // UPDATE MANAGER
    // =====================================================

    private bool GetUpdateManager()
    {
        if (updateManager == null)
            updateManager = FindFirstObjectByType<UpdateManager>();

        if (updateManager == null)
        {
            Debug.LogError(
                "[UpgradeChoiceUI] UpdateManager missing!",
                this
            );

            return false;
        }

        return true;
    }

    // =====================================================
    // GET AVAILABLE UPGRADES
    // =====================================================

    private List<UpgradeSO> GetAvailableUpgrades(
        UpgradeSO.Rarity rarity,
        bool ringsOnly)
    {
        List<UpgradeSO> result = new List<UpgradeSO>();

        if (updateManager == null)
            return result;

        List<UpgradeSO> available =
            updateManager.GetCurrentCharacterUpgrades();

        if (available == null)
            return result;

        foreach (UpgradeSO upgrade in available)
        {
            if (upgrade == null)
                continue;

            if (ringsOnly)
            {
                if (!(upgrade is RingUpgrade))
                    continue;
            }
            else
            {
                if (upgrade.GetRarity() != rarity)
                    continue;
            }

            result.Add(upgrade);
        }

        return result;
    }

    // =====================================================
    // DISPLAY CHOICES
    // =====================================================

    private void ShowChoices(
        List<UpgradeSO> availableUpgrades,
        Action onSelected)
    {
        if (selectionInProgress)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] A reward selection is already open.",
                this
            );

            return;
        }

        if (!ValidateButtonsAndTexts())
        {
            onSelected?.Invoke();
            return;
        }

        onUpgradeSelected = onSelected;
        selectionInProgress = true;

        ClearCurrentChoices();

        if (availableUpgrades == null ||
            availableUpgrades.Count == 0)
        {
            Debug.LogWarning(
                "[UpgradeChoiceUI] No available rewards."
            );

            CompleteSelection();
            return;
        }

        List<UpgradeSO> shuffled =
            new List<UpgradeSO>(availableUpgrades);

        // Shuffle available upgrades.
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            UpgradeSO temp = shuffled[i];

            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        int count = Mathf.Min(
            CHOICE_COUNT,
            shuffled.Count
        );

        for (int i = 0; i < count; i++)
        {
            currentChoices[i] = shuffled[i];
        }

        SetupButtons();

        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "[UpgradeChoiceUI] Canvas missing!",
                this
            );

            CompleteSelection();
        }
    }

    private void ClearCurrentChoices()
    {
        for (int i = 0; i < currentChoices.Length; i++)
        {
            currentChoices[i] = null;
        }
    }

    // =====================================================
    // SETUP BUTTONS
    // =====================================================

    private void SetupButtons()
    {
        if (upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT)
            return;

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            Button button = upgradeButtons[i];

            if (button == null)
                continue;

            UpgradeSO upgrade = currentChoices[i];

            if (upgrade == null)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);
            button.interactable = true;

            if (upgradeTexts != null &&
                i < upgradeTexts.Length &&
                upgradeTexts[i] != null)
            {
                upgradeTexts[i].text =
                    upgrade.UpgradeName;
            }
        }
    }

    // =====================================================
    // SELECT UPGRADE
    // =====================================================

    private void SelectUpgrade(int index)
    {
        if (!selectionInProgress)
            return;

        if (index < 0 ||
            index >= currentChoices.Length)
            return;

        UpgradeSO selectedUpgrade = currentChoices[index];

        if (selectedUpgrade == null)
            return;

        if (!GetUpdateManager())
            return;

        // Prevent multiple selections.
        if (upgradeButtons != null)
        {
            foreach (Button button in upgradeButtons)
            {
                if (button != null)
                    button.interactable = false;
            }
        }

        updateManager.ApplyUpgrade(selectedUpgrade);

        Debug.Log(
            "[UpgradeChoiceUI] Selected: " +
            selectedUpgrade.UpgradeName
        );

        CompleteSelection();
    }

    // =====================================================
    // COMPLETE SELECTION
    // =====================================================

    private void CompleteSelection()
    {
        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        selectionInProgress = false;

        Action callback = onUpgradeSelected;

        onUpgradeSelected = null;

        ClearCurrentChoices();

        callback?.Invoke();
    }

    // =====================================================
    // HIDE
    // =====================================================

    public void HideUpgradeChoices()
    {
        selectionInProgress = false;
        onUpgradeSelected = null;

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        ClearCurrentChoices();
    }
}
