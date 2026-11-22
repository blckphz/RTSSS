
using System;
using System.Collections;
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

    [Tooltip("Assign the ROOT parent GameObject of each entire upgrade card.")]
    [SerializeField] private Transform[] upgradeCardParents;

    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeTexts;
    [SerializeField] private Image[] upgradeIcons;
    [SerializeField] private TMP_Text[] upgradeDescriptions;
    [SerializeField] private TMP_Text[] upgradeRarities;
    [SerializeField] private Image[] Backgrounds;

    [Header("Rarity Colors")]
    [SerializeField]
    private Color normalColor =
        new Color(0.85f, 0.85f, 0.85f);

    [SerializeField]
    private Color epicColor =
        new Color(0.65f, 0.25f, 1f);

    [SerializeField]
    private Color artifactColor =
        new Color(1f, 0.72f, 0.1f);

    [Header("Card Animation")]
    [SerializeField] private float animationDuration = 0.45f;
    [SerializeField] private float startScale = 0.75f;
    [SerializeField] private float overshootScale = 1.08f;

    private const int CHOICE_COUNT = 3;

    private UpgradeSO[] currentChoices =
        new UpgradeSO[CHOICE_COUNT];

    private Vector3[] originalScales =
        new Vector3[CHOICE_COUNT];

    private Action onUpgradeSelected;
    private Coroutine animationCoroutine;

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

        if (!ValidateReferences())
            return;

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            int index = i;

            originalScales[i] =
                upgradeCardParents[i].localScale;

            upgradeButtons[i].onClick.RemoveAllListeners();

            upgradeButtons[i].onClick.AddListener(
                () => SelectUpgrade(index)
            );
        }
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private bool ValidateReferences()
    {
        bool valid = true;

        if (upgradeCardParents == null ||
            upgradeCardParents.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 card parent transforms.");
            valid = false;
        }

        if (upgradeButtons == null ||
            upgradeButtons.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 buttons.");
            valid = false;
        }

        if (upgradeTexts == null ||
            upgradeTexts.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 name texts.");
            valid = false;
        }

        if (upgradeIcons == null ||
            upgradeIcons.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 icons.");
            valid = false;
        }

        if (upgradeDescriptions == null ||
            upgradeDescriptions.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 description texts.");
            valid = false;
        }

        if (upgradeRarities == null ||
            upgradeRarities.Length != CHOICE_COUNT)
        {
            Debug.LogError("Assign exactly 3 rarity texts.");
            valid = false;
        }

        if (!valid)
            return false;

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (upgradeCardParents[i] == null ||
                upgradeButtons[i] == null ||
                upgradeTexts[i] == null ||
                upgradeIcons[i] == null ||
                upgradeDescriptions[i] == null ||
                upgradeRarities[i] == null)
            {
                Debug.LogError(
                    "One or more upgrade card references are missing. " +
                    "Check card " + (i + 1)
                );

                valid = false;
            }
        }

        return valid;
    }

    // =====================================================
    // STARTING UPGRADE
    // =====================================================

    public void ShowStartingUpgradeChoice(Action onSelected = null)
    {
        ShowStartingArtifactChoice(onSelected);
    }

    public void ShowStartingArtifactChoice(Action onSelected = null)
    {
        ShowArtifactSelection(() =>
        {
            ShowStartingRingChoice(onSelected);
        });
    }

    // =====================================================
    // STARTING RING
    // =====================================================

    public void ShowStartingRingChoice(Action onSelected = null)
    {
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
    // BOSS ARTIFACT REWARD
    // =====================================================

    public void ShowArtifactChoices(Action onSelected = null)
    {
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
            Debug.LogError("UpdateManager missing!");
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
    // SHOW CHOICES
    // =====================================================

    private void ShowChoices(
        List<UpgradeSO> availableUpgrades,
        Action onSelected)
    {
        if (selectionInProgress)
            return;

        if (!ValidateReferences())
        {
            onSelected?.Invoke();
            return;
        }

        selectionInProgress = true;
        onUpgradeSelected = onSelected;

        ClearCurrentChoices();

        if (availableUpgrades == null ||
            availableUpgrades.Count == 0)
        {
            Debug.LogWarning("No available upgrades.");
            CompleteSelection();
            return;
        }

        List<UpgradeSO> shuffled =
            new List<UpgradeSO>(availableUpgrades);

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
            currentChoices[i] = shuffled[i];

        SetupButtons();

        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(true);
            PlayChoiceAnimation();
        }
        else
        {
            Debug.LogError("Upgrade Canvas is missing!");
            CompleteSelection();
        }
    }

    // =====================================================
    // CLEAR CHOICES
    // =====================================================

    private void ClearCurrentChoices()
    {
        for (int i = 0; i < CHOICE_COUNT; i++)
            currentChoices[i] = null;
    }

    // =====================================================
    // SETUP BUTTONS
    // =====================================================

    private void SetupButtons()
    {
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            UpgradeSO upgrade = currentChoices[i];

            if (upgrade == null)
            {
                upgradeCardParents[i].gameObject.SetActive(false);
                continue;
            }

            upgradeCardParents[i].gameObject.SetActive(true);

            upgradeButtons[i].interactable = true;

            upgradeTexts[i].text = upgrade.UpgradeName;

            upgradeIcons[i].sprite = upgrade.Icon;
            upgradeIcons[i].enabled = upgrade.Icon != null;

            upgradeDescriptions[i].text =
                upgrade.Description;

            UpgradeSO.Rarity rarity = upgrade.GetRarity();

            upgradeRarities[i].text = rarity.ToString();
            upgradeRarities[i].color = GetRarityColor(rarity);
        }
    }

    // =====================================================
    // RARITY COLORS
    // =====================================================

    private Color GetRarityColor(UpgradeSO.Rarity rarity)
    {
        switch (rarity)
        {
            case UpgradeSO.Rarity.Normal:
                return normalColor;

            case UpgradeSO.Rarity.Epic:
                return epicColor;

            case UpgradeSO.Rarity.Artifact:
                return artifactColor;

            default:
                return Color.white;
        }
    }

    // =====================================================
    // CARD ANIMATION
    // =====================================================

    private void PlayChoiceAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateChoices());
    }

    private IEnumerator AnimateChoices()
    {
        // Start all cards small.
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (currentChoices[i] == null)
                continue;

            upgradeCardParents[i].localScale =
                originalScales[i] * startScale;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        // Smoothly animate all cards together.
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth ease-out with a gentle overshoot.
            float scale = CalculateOvershootScale(t);

            for (int i = 0; i < CHOICE_COUNT; i++)
            {
                if (currentChoices[i] == null)
                    continue;

                upgradeCardParents[i].localScale =
                    originalScales[i] * scale;
            }

            yield return null;
        }

        // Ensure exact final scale.
        ResetChoiceScales();

        animationCoroutine = null;
    }

    private float CalculateOvershootScale(float t)
    {
        // First 75%: grow from starting scale to overshoot.
        // Final 25%: gently settle back to normal scale.

        if (t < 0.75f)
        {
            float growT = t / 0.75f;

            // Smooth ease-out.
            float eased = 1f - Mathf.Pow(1f - growT, 3f);

            return Mathf.Lerp(
                startScale,
                overshootScale,
                eased
            );
        }
        else
        {
            float settleT = (t - 0.75f) / 0.25f;

            // Smooth settle.
            float eased = settleT * settleT *
                          (3f - 2f * settleT);

            return Mathf.Lerp(
                overshootScale,
                1f,
                eased
            );
        }
    }

    // =====================================================
    // SELECT UPGRADE
    // =====================================================

    private void SelectUpgrade(int index)
    {
        if (!selectionInProgress)
            return;

        if (index < 0 || index >= CHOICE_COUNT)
            return;

        UpgradeSO selectedUpgrade = currentChoices[index];

        if (selectedUpgrade == null)
            return;

        if (!GetUpdateManager())
            return;

        foreach (Button button in upgradeButtons)
        {
            if (button != null)
                button.interactable = false;
        }

        updateManager.ApplyUpgrade(selectedUpgrade);

        Debug.Log(
            "Selected upgrade: " + selectedUpgrade.UpgradeName
        );

        CompleteSelection();
    }

    // =====================================================
    // COMPLETE SELECTION
    // =====================================================

    private void CompleteSelection()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetChoiceScales();

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        selectionInProgress = false;

        Action callback = onUpgradeSelected;
        onUpgradeSelected = null;

        ClearCurrentChoices();

        callback?.Invoke();
    }

    // =====================================================
    // RESET SCALES
    // =====================================================

    private void ResetChoiceScales()
    {
        if (upgradeCardParents == null)
            return;

        for (int i = 0; i < upgradeCardParents.Length &&
                        i < CHOICE_COUNT; i++)
        {
            if (upgradeCardParents[i] != null)
            {
                upgradeCardParents[i].localScale =
                    originalScales[i];
            }
        }
    }

    // =====================================================
    // HIDE
    // =====================================================

    public void HideUpgradeChoices()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetChoiceScales();

        selectionInProgress = false;
        onUpgradeSelected = null;

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        ClearCurrentChoices();
    }
}
