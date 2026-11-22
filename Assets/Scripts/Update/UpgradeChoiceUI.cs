
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
    [SerializeField] private Transform[] upgradeCardParents;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeTexts;
    [SerializeField] private Image[] upgradeIcons;
    [SerializeField] private TMP_Text[] upgradeDescriptions;
    [SerializeField] private TMP_Text[] upgradeRarities;
    [SerializeField] private Image[] Backgrounds;

    [Header("Dynamic Description Position")]
    [SerializeField] private float descriptionExtraSpacing = 5f;
    [SerializeField] private bool enableNameDebugLogs = true;

    [Header("Rarity Colors")]
    [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color epicColor = new Color(0.65f, 0.25f, 1f);
    [SerializeField] private Color artifactColor = new Color(1f, 0.72f, 0.1f);

    [Header("Card Animation")]
    [SerializeField] private float animationDuration = 0.45f;
    [SerializeField] private float startScale = 0.75f;
    [SerializeField] private float overshootScale = 1.08f;

    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverSpeed = 12f;

    private const int CHOICE_COUNT = 3;

    private UpgradeSO[] currentChoices = new UpgradeSO[CHOICE_COUNT];

    private Vector3[] originalScales = new Vector3[CHOICE_COUNT];

    private Vector2[] originalDescriptionPositions =
        new Vector2[CHOICE_COUNT];

    private bool[] isHovering = new bool[CHOICE_COUNT];

    private Action onUpgradeSelected;

    private Coroutine animationCoroutine;
    private Coroutine descriptionCoroutine;

    private bool selectionInProgress;
    private bool entranceAnimationFinished;

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

            originalScales[i] = upgradeCardParents[i].localScale;

            originalDescriptionPositions[i] =
                upgradeDescriptions[i].rectTransform.anchoredPosition;

            upgradeButtons[i].onClick.RemoveAllListeners();

            upgradeButtons[i].onClick.AddListener(
                () => SelectUpgrade(index)
            );
        }

        Debug.Log("[UpgradeChoiceUI] Initialized successfully.");
    }

    private void Update()
    {
        if (!selectionInProgress || !entranceAnimationFinished)
            return;

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (upgradeCardParents[i] == null ||
                currentChoices[i] == null)
                continue;

            float targetScale = isHovering[i] ? hoverScale : 1f;

            Vector3 target = originalScales[i] * targetScale;

            upgradeCardParents[i].localScale = Vector3.Lerp(
                upgradeCardParents[i].localScale,
                target,
                Time.unscaledDeltaTime * hoverSpeed
            );
        }
    }

    // =====================================================
    // HOVER
    // =====================================================

    public void OnCardHoverEnter(int index)
    {
        if (!selectionInProgress || !entranceAnimationFinished)
            return;

        if (index < 0 || index >= CHOICE_COUNT)
            return;

        if (currentChoices[index] == null)
            return;

        isHovering[index] = true;
    }

    public void OnCardHoverExit(int index)
    {
        if (index < 0 || index >= CHOICE_COUNT)
            return;

        isHovering[index] = false;
    }

    private void ResetHoverStates()
    {
        for (int i = 0; i < CHOICE_COUNT; i++)
            isHovering[i] = false;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private bool ValidateReferences()
    {
        bool valid = true;

        if (upgradeCardParents == null || upgradeCardParents.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 card parent transforms.");
            valid = false;
        }

        if (upgradeButtons == null || upgradeButtons.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 buttons.");
            valid = false;
        }

        if (upgradeTexts == null || upgradeTexts.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 name texts.");
            valid = false;
        }

        if (upgradeIcons == null || upgradeIcons.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 icons.");
            valid = false;
        }

        if (upgradeDescriptions == null || upgradeDescriptions.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 description texts.");
            valid = false;
        }

        if (upgradeRarities == null || upgradeRarities.Length != CHOICE_COUNT)
        {
            Debug.LogError("[UpgradeChoiceUI] Assign exactly 3 rarity texts.");
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
                    "[UpgradeChoiceUI] Missing reference on card " + (i + 1)
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

        ShowChoices(GetAvailableUpgrades(default, true), onSelected);
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
            GetAvailableUpgrades(UpgradeSO.Rarity.Artifact, false),
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

        ShowChoices(GetAvailableUpgrades(default, true), onSelected);
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
            Debug.LogError("[UpgradeChoiceUI] UpdateManager missing!");
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
        {
            Debug.LogWarning("[UpgradeChoiceUI] Available upgrades list is null.");
            return result;
        }

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

        Debug.Log(
            $"[UpgradeChoiceUI] Found {result.Count} upgrades. " +
            $"Rings only: {ringsOnly} | Rarity: {rarity}"
        );

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
        {
            Debug.LogWarning("[UpgradeChoiceUI] Selection already in progress.");
            return;
        }

        if (!ValidateReferences())
        {
            onSelected?.Invoke();
            return;
        }

        selectionInProgress = true;
        entranceAnimationFinished = false;
        onUpgradeSelected = onSelected;

        ResetHoverStates();
        ClearCurrentChoices();

        if (availableUpgrades == null || availableUpgrades.Count == 0)
        {
            Debug.LogWarning("[UpgradeChoiceUI] No available upgrades.");
            CompleteSelection();
            return;
        }

        List<UpgradeSO> shuffled = new List<UpgradeSO>(availableUpgrades);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            UpgradeSO temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        int count = Mathf.Min(CHOICE_COUNT, shuffled.Count);

        for (int i = 0; i < count; i++)
            currentChoices[i] = shuffled[i];

        SetupButtons();

        if (upgradeCanvas != null)
        {
            upgradeCanvas.SetActive(true);

            if (descriptionCoroutine != null)
                StopCoroutine(descriptionCoroutine);

            descriptionCoroutine =
                StartCoroutine(AdjustDescriptionsAfterLayout());

            PlayChoiceAnimation();
        }
        else
        {
            Debug.LogError("[UpgradeChoiceUI] Upgrade Canvas is missing!");
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

            isHovering[i] = false;

            if (upgrade == null)
            {
                upgradeCardParents[i].gameObject.SetActive(false);

                if (enableNameDebugLogs)
                    Debug.Log($"[UpgradeChoiceUI] Card {i + 1} hidden.");

                continue;
            }

            upgradeCardParents[i].gameObject.SetActive(true);

            upgradeButtons[i].interactable = true;

            upgradeTexts[i].text = upgrade.UpgradeName;

            upgradeIcons[i].sprite = upgrade.Icon;
            upgradeIcons[i].enabled = upgrade.Icon != null;

            upgradeDescriptions[i].text = upgrade.Description;

            UpgradeSO.Rarity rarity = upgrade.GetRarity();

            upgradeRarities[i].text = rarity.ToString();
            upgradeRarities[i].color = GetRarityColor(rarity);

            upgradeDescriptions[i].rectTransform.anchoredPosition =
                originalDescriptionPositions[i];

            if (enableNameDebugLogs)
            {
                Debug.Log(
                    $"[UpgradeChoiceUI] Card {i + 1} setup. " +
                    $"Name: {upgrade.UpgradeName} | Rarity: {rarity}"
                );
            }
        }
    }

    // =====================================================
    // DYNAMIC DESCRIPTION POSITION
    // =====================================================

    private IEnumerator AdjustDescriptionsAfterLayout()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (currentChoices[i] == null)
                continue;

            AdjustDescriptionPosition(i);
        }

        Canvas.ForceUpdateCanvases();

        Debug.Log("[UpgradeChoiceUI] All description positions updated.");

        descriptionCoroutine = null;
    }

    private void AdjustDescriptionPosition(int index)
    {
        TMP_Text nameText = upgradeTexts[index];
        TMP_Text descriptionText = upgradeDescriptions[index];

        if (nameText == null || descriptionText == null)
            return;

        RectTransform nameRect = nameText.rectTransform;
        RectTransform descriptionRect = descriptionText.rectTransform;

        descriptionRect.anchoredPosition =
            originalDescriptionPositions[index];

        Canvas.ForceUpdateCanvases();

        nameText.ForceMeshUpdate(true, true);

        float availableWidth = nameRect.rect.width;

        if (availableWidth <= 0f)
        {
            Debug.LogWarning(
                $"[UpgradeChoiceUI] Card {index + 1}: Name width is zero."
            );
            return;
        }

        Vector2 preferredSize = nameText.GetPreferredValues(
            nameText.text,
            availableWidth,
            0f
        );

        int lineCount = nameText.textInfo.lineCount;

        float renderedHeight = 0f;

        if (lineCount > 0)
        {
            TMP_LineInfo firstLine = nameText.textInfo.lineInfo[0];
            TMP_LineInfo lastLine = nameText.textInfo.lineInfo[lineCount - 1];

            renderedHeight = Mathf.Abs(
                firstLine.ascender - lastLine.descender
            );
        }

        if (lineCount <= 0)
        {
            float estimatedLineHeight = Mathf.Max(1f, nameText.fontSize);

            lineCount = Mathf.Max(
                1,
                Mathf.CeilToInt(preferredSize.y / estimatedLineHeight)
            );

            renderedHeight = preferredSize.y;
        }

        float singleLineHeight = nameText.fontSize;

        if (nameText.textInfo.lineCount > 0)
        {
            TMP_LineInfo firstLine = nameText.textInfo.lineInfo[0];

            singleLineHeight = Mathf.Abs(
                firstLine.ascender - firstLine.descender
            );
        }

        float extraHeight = Mathf.Max(
            0f,
            renderedHeight - singleLineHeight
        );

        Vector2 newPosition = originalDescriptionPositions[index];

        newPosition.y -= extraHeight;

        if (lineCount > 1)
            newPosition.y -= descriptionExtraSpacing;

        descriptionRect.anchoredPosition = newPosition;

        if (enableNameDebugLogs)
        {
            Debug.Log(
                $"[UpgradeChoiceUI] NAME LAYOUT | " +
                $"Card: {index + 1} | " +
                $"Name: \"{nameText.text}\" | " +
                $"Lines: {lineCount} | " +
                $"Width: {availableWidth:F2} | " +
                $"Preferred Height: {preferredSize.y:F2} | " +
                $"Rendered Height: {renderedHeight:F2} | " +
                $"Single Line Height: {singleLineHeight:F2} | " +
                $"Extra Height: {extraHeight:F2} | " +
                $"Original Y: {originalDescriptionPositions[index].y:F2} | " +
                $"New Y: {newPosition.y:F2}"
            );
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
        for (int i = 0; i < CHOICE_COUNT; i++)
        {
            if (currentChoices[i] == null)
                continue;

            upgradeCardParents[i].localScale =
                originalScales[i] * startScale;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

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

        ResetChoiceScales();

        entranceAnimationFinished = true;
        animationCoroutine = null;
    }

    private float CalculateOvershootScale(float t)
    {
        if (t < 0.75f)
        {
            float growT = t / 0.75f;

            float eased = 1f - Mathf.Pow(1f - growT, 3f);

            return Mathf.Lerp(startScale, overshootScale, eased);
        }
        else
        {
            float settleT = (t - 0.75f) / 0.25f;

            float eased = settleT * settleT * (3f - 2f * settleT);

            return Mathf.Lerp(overshootScale, 1f, eased);
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

        ResetHoverStates();

        Debug.Log(
            $"[UpgradeChoiceUI] APPLYING UPGRADE | " +
            $"Name: {selectedUpgrade.UpgradeName} | " +
            $"Rarity: {selectedUpgrade.GetRarity()} | " +
            $"Card: {index + 1}"
        );

        updateManager.ApplyUpgrade(selectedUpgrade);

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

        if (descriptionCoroutine != null)
        {
            StopCoroutine(descriptionCoroutine);
            descriptionCoroutine = null;
        }

        entranceAnimationFinished = false;

        ResetHoverStates();
        ResetChoiceScales();

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        selectionInProgress = false;

        Action callback = onUpgradeSelected;
        onUpgradeSelected = null;

        ClearCurrentChoices();

        Debug.Log("[UpgradeChoiceUI] Selection completed.");

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
                upgradeCardParents[i].localScale = originalScales[i];
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

        if (descriptionCoroutine != null)
        {
            StopCoroutine(descriptionCoroutine);
            descriptionCoroutine = null;
        }

        entranceAnimationFinished = false;

        ResetHoverStates();
        ResetChoiceScales();

        selectionInProgress = false;
        onUpgradeSelected = null;

        if (upgradeCanvas != null)
            upgradeCanvas.SetActive(false);

        ClearCurrentChoices();

        Debug.Log("[UpgradeChoiceUI] Upgrade choices hidden.");
    }
}
