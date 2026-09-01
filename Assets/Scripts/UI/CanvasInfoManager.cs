using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CanvasInfoManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Graphic secondPulsingGraphic;

    [Header("Background Pulse Settings")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minPulseScale = 0.98f;
    [SerializeField] private float maxPulseScale = 1.02f;
    [SerializeField] private float selectionPulseMultiplier = 1.5f;

    [Header("Selection Opacity Flash")]
    [SerializeField] private float flashTargetOpacity = 0.8f;
    [SerializeField] private float flashDuration = 0.4f;

    [Header("Selected Ability")]
    [SerializeField] private Color selectedAbilityColor = Color.cyan;

    [Header("Tooltips & Highlights")]
    [SerializeField] private tooltipManager tooltipManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;

    private int lastHoveredAbilityIndex = -1;
    private int lastLinkIndex = -1;

    private int selectedAbilityIndex = -1;
    private AbilitySO selectedAbility;

    private Camera eventCamera;
    private Canvas cachedCanvas;

    private readonly StringBuilder textBuilder = new StringBuilder();

    private Vector3 initialBackgroundScale = Vector3.one;
    private Vector3 initialSecondScale = Vector3.one;

    private float defaultOpacity = 1f;
    private float defaultSecondOpacity = 1f;

    private float currentPulseMultiplier = 1f;

    private Coroutine flashCoroutine;
    private Coroutine pulseLerpCoroutine;

    private void Awake()
    {
        SetupReferences();

        if (backgroundImage != null)
        {
            initialBackgroundScale = backgroundImage.rectTransform.localScale;
            defaultOpacity = backgroundImage.color.a;
        }

        if (secondPulsingGraphic != null)
        {
            initialSecondScale = secondPulsingGraphic.rectTransform.localScale;
            defaultSecondOpacity = secondPulsingGraphic.color.a;
        }
    }

    private void OnEnable()
    {
        AttackUnit.OnAbilityUsed += HandleAbilityUsed;
        HealthManager.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        AttackUnit.OnAbilityUsed -= HandleAbilityUsed;
        HealthManager.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        CheckAbilityHover();
        AnimateBackgroundPulse();

        if (CombatUtility.IsPlayerInputLocked() && selectedAbility != null)
        {
            ClearSelectedAbilityForEnemyTurn();
        }
    }

    private void AnimateBackgroundPulse()
    {
        if (!enablePulse) return;

        float sineProgress = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float smoothProgress = Mathf.SmoothStep(0f, 1f, sineProgress);
        float targetScale = Mathf.Lerp(minPulseScale, maxPulseScale, smoothProgress);
        float activeScaleOffset = (targetScale - 1f) * currentPulseMultiplier;
        Vector3 scaleVector = Vector3.one * (1f + activeScaleOffset);

        if (backgroundImage != null)
        {
            backgroundImage.rectTransform.localScale = Vector3.Scale(initialBackgroundScale, scaleVector);
        }

        if (secondPulsingGraphic != null)
        {
            secondPulsingGraphic.rectTransform.localScale = Vector3.Scale(initialSecondScale, scaleVector);
        }
    }

    private void TriggerOpacityFlash()
    {
        if (backgroundImage == null && secondPulsingGraphic == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(OpacityFlashRoutine());
    }

    private IEnumerator OpacityFlashRoutine()
    {
        Color bgCol = backgroundImage != null ? backgroundImage.color : Color.white;
        Color secCol = secondPulsingGraphic != null ? secondPulsingGraphic.color : Color.white;

        float halfDuration = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (backgroundImage != null)
            {
                bgCol.a = Mathf.Lerp(defaultOpacity, flashTargetOpacity, easedProgress);
                backgroundImage.color = bgCol;
            }

            if (secondPulsingGraphic != null)
            {
                secCol.a = Mathf.Lerp(defaultSecondOpacity, flashTargetOpacity, easedProgress);
                secondPulsingGraphic.color = secCol;
            }

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (backgroundImage != null)
            {
                bgCol.a = Mathf.Lerp(flashTargetOpacity, defaultOpacity, easedProgress);
                backgroundImage.color = bgCol;
            }

            if (secondPulsingGraphic != null)
            {
                secCol.a = Mathf.Lerp(flashTargetOpacity, defaultSecondOpacity, easedProgress);
                secondPulsingGraphic.color = secCol;
            }

            yield return null;
        }

        if (backgroundImage != null)
        {
            bgCol.a = defaultOpacity;
            backgroundImage.color = bgCol;
        }

        if (secondPulsingGraphic != null)
        {
            secCol.a = defaultSecondOpacity;
            secondPulsingGraphic.color = secCol;
        }
    }

    private void TriggerPulseBurst()
    {
        if (pulseLerpCoroutine != null) StopCoroutine(pulseLerpCoroutine);
        pulseLerpCoroutine = StartCoroutine(PulseBurstRoutine());
    }

    private IEnumerator PulseBurstRoutine()
    {
        float halfDuration = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            currentPulseMultiplier = Mathf.Lerp(1f, selectionPulseMultiplier, easedProgress);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            currentPulseMultiplier = Mathf.Lerp(selectionPulseMultiplier, 1f, easedProgress);
            yield return null;
        }

        currentPulseMultiplier = 1f;
    }

    private void HandleHealthChanged(HealthManager changedHealthManager)
    {
        if (changedHealthManager == null || UIManager.CurrentSelection == null) return;

        AttackUnit selectedAttackUnit = UIManager.CurrentSelection.GetAttackUnit();
        if (selectedAttackUnit == null) return;

        HealthManager selectedHealthManager = selectedAttackUnit.GetComponent<HealthManager>();
        if (selectedHealthManager != changedHealthManager) return;

        CharacterSO character = selectedAttackUnit.GetCharacterData();
        if (character != null) RefreshCharacter(character);
    }

    private void HandleAbilityUsed(AttackUnit attackUnit, AbilitySO ability)
    {
        if (attackUnit == null || UIManager.CurrentSelection == null) return;

        AttackUnit selectedAttackUnit = UIManager.CurrentSelection.GetAttackUnit();
        if (selectedAttackUnit != attackUnit) return;

        CharacterSO character = attackUnit.GetCharacterData();
        if (character != null) RefreshCharacter(character);
    }

    private void SetupReferences()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (highlightManager == null && gridManager != null) highlightManager = gridManager.GetHighlightManager();
        if (highlightManager == null) highlightManager = FindFirstObjectByType<GridHighlightManager>();
        if (tooltipManager == null) tooltipManager = FindFirstObjectByType<tooltipManager>();
        if (infoText != null) cachedCanvas = infoText.canvas;
    }

    public void RefreshCurrentSelection()
    {
        if (UIManager.CurrentSelection == null) return;

        AttackUnit attackUnit = UIManager.CurrentSelection.GetAttackUnit();
        if (attackUnit == null) return;

        CharacterSO character = attackUnit.GetCharacterData();
        if (character != null) RefreshCharacter(character);
    }

    public void ShowCharacter(ICharacterHolder characterHolder)
    {
        if (characterHolder != null) ShowCharacter(characterHolder.GetCharacterData());
    }

    public void ShowCharacter(CharacterSO character)
    {
        if (character == null) return;

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        HideStatusTooltip();
        RefreshCharacter(character);
    }

    private void RefreshCharacter(CharacterSO character)
    {
        if (character == null) return;

        textBuilder.Clear();
        textBuilder.AppendLine($"<b>{character.characterName}</b>\n");
        textBuilder.AppendLine($"Team: {character.team}");

        int currentHealth = character.maxHealth;
        int maxHealth = character.maxHealth;

        AttackUnit selectedAttackUnit = UIManager.CurrentSelection?.GetAttackUnit();
        HealthManager selectedHealthManager = selectedAttackUnit?.GetComponent<HealthManager>();

        if (selectedHealthManager == null || selectedAttackUnit == null || selectedAttackUnit.GetCharacterData() != character)
        {
            AttackUnit[] attackUnits = FindObjectsByType<AttackUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (AttackUnit attackUnit in attackUnits)
            {
                if (attackUnit != null && attackUnit.GetCharacterData() == character)
                {
                    selectedHealthManager = attackUnit.GetComponent<HealthManager>();
                    break;
                }
            }
        }

        if (selectedHealthManager != null)
        {
            currentHealth = selectedHealthManager.GetHealth();
            maxHealth = selectedHealthManager.GetMaxHealth();
        }

        textBuilder.AppendLine($"Health: {currentHealth}/{maxHealth}\n");

        List<AbilitySO> abilities = character.GetAbilities();

        if (abilities != null && abilities.Count > 0)
        {
            textBuilder.AppendLine("<b>Abilities</b>\n");

            AttackUnit activeUnit = UIManager.CurrentSelection?.GetAttackUnit();
            bool canSelectAbilities = !CombatUtility.IsPlayerInputLocked() && activeUnit != null &&
                                      (activeUnit.GetTeam() == Team.Player || activeUnit.GetTeam() == Team.Ally);

            for (int i = 0; i < abilities.Count; i++)
            {
                AbilitySO ability = abilities[i];
                if (ability == null) continue;

                string selectedText = string.Empty;

                if (canSelectAbilities && selectedAbilityIndex == i && selectedAbility != null && selectedAbility == ability)
                {
                    string selectedColor = ColorUtility.ToHtmlStringRGB(selectedAbilityColor);
                    selectedText = $" <color=#{selectedColor}><b>(Selected)</b></color>";
                }

                string abilityName = ability.GetAbilityName();
                string abilityLink = $"<link=\"ability_{i}\"><color=yellow><u><b>{abilityName}</b></u></color></link>";

                textBuilder.AppendLine(abilityLink + selectedText);

                string description = ability.GetDescription();
                if (!string.IsNullOrEmpty(description)) textBuilder.AppendLine(description);

                if (ability is HealAbilitySO healAbility)
                {
                    textBuilder.AppendLine($"Heal: {healAbility.GetHealAmount()} | Range: {ability.GetRange()}");
                }
                else
                {
                    textBuilder.AppendLine($"Damage: {ability.GetDamage()} | Range: {ability.GetRange()}");
                }

                int currentCooldown = activeUnit != null ? activeUnit.GetAbilityCooldown(ability) : ability.GetCooldown();
                int usesPerTurn = ability.GetUsesPerTurn();
                string usesText;

                if (usesPerTurn <= 0)
                {
                    usesText = "Unlimited";
                }
                else
                {
                    int remainingUses = activeUnit != null ? activeUnit.GetAbilityUsesRemaining(ability) : usesPerTurn;
                    usesText = $"{remainingUses}/{usesPerTurn}";
                }

                textBuilder.AppendLine($"Cooldown: {currentCooldown} | Uses: {usesText}\n");
            }
        }
        else
        {
            textBuilder.Append("<b>No Abilities</b>");
        }

        if (infoText != null)
        {
            infoText.text = textBuilder.ToString();
            infoText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(infoText.rectTransform);
        }

        if (characterIcon != null)
        {
            characterIcon.sprite = character.icon;
            characterIcon.enabled = character.icon != null;
        }
    }

    private void CheckAbilityHover()
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            HideStatusTooltip();
            ClearAbilityHighlights();
            return;
        }

        if (infoText == null || !infoText.gameObject.activeInHierarchy || Mouse.current == null)
        {
            HideStatusTooltip();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(infoText, mousePosition, GetEventCamera());

        if (linkIndex == -1)
        {
            if (lastLinkIndex != -1)
            {
                lastLinkIndex = -1;
                HideStatusTooltip();
                ClearAbilityHover();
            }
            return;
        }

        TMP_TextInfo textInfo = infoText.textInfo;

        if (linkIndex < 0 || linkIndex >= textInfo.linkCount)
        {
            HideStatusTooltip();
            return;
        }

        string linkId = textInfo.linkInfo[linkIndex].GetLinkID();

        if (linkId.StartsWith("status_"))
        {
            if (linkIndex != lastLinkIndex)
            {
                lastLinkIndex = linkIndex;
                ShowStatusTooltip(linkId.Substring("status_".Length));
            }
            return;
        }

        if (linkId.StartsWith("ability_"))
        {
            HideStatusTooltip();

            if (linkIndex == lastLinkIndex) return;

            lastLinkIndex = linkIndex;
            string indexString = linkId.Substring("ability_".Length);

            if (int.TryParse(indexString, out int abilityIndex))
            {
                if (abilityIndex != lastHoveredAbilityIndex)
                {
                    lastHoveredAbilityIndex = abilityIndex;
                    ShowAbilityRange(abilityIndex);
                }
            }
            else
            {
                ClearAbilityHover();
            }

            return;
        }

        HideStatusTooltip();
        ClearAbilityHover();
    }

    public bool IsPointerOverAbilityLink() => CheckLinkPrefix("ability_");

    public bool IsPointerOverStatusLink() => CheckLinkPrefix("status_");

    private bool CheckLinkPrefix(string prefix)
    {
        if (CombatUtility.IsPlayerInputLocked()) return false;
        if (infoText == null || !infoText.gameObject.activeInHierarchy || Mouse.current == null) return false;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(infoText, Mouse.current.position.ReadValue(), GetEventCamera());

        if (linkIndex < 0 || linkIndex >= infoText.textInfo.linkCount) return false;

        return infoText.textInfo.linkInfo[linkIndex].GetLinkID().StartsWith(prefix);
    }

    public bool TrySelectAbilityUnderMouse()
    {
        if (CombatUtility.IsPlayerInputLocked()) return false;
        if (infoText == null || !infoText.gameObject.activeInHierarchy || Mouse.current == null) return false;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(infoText, Mouse.current.position.ReadValue(), GetEventCamera());

        if (linkIndex < 0 || linkIndex >= infoText.textInfo.linkCount) return false;

        string linkId = infoText.textInfo.linkInfo[linkIndex].GetLinkID();
        if (!linkId.StartsWith("ability_")) return false;

        string indexString = linkId.Substring("ability_".Length);

        if (int.TryParse(indexString, out int abilityIndex))
        {
            SelectAbility(abilityIndex);
            return true;
        }

        return false;
    }

    private bool CanSelectAbilitiesForCurrentUnit()
    {
        if (CombatUtility.IsPlayerInputLocked()) return false;
        if (UIManager.CurrentSelection == null) return false;

        AttackUnit attackUnit = UIManager.CurrentSelection.GetAttackUnit();
        if (attackUnit == null) return false;

        Team team = attackUnit.GetTeam();
        return team == Team.Player || team == Team.Ally;
    }

    private bool CanUseSelectedAbility(AbilitySO ability)
    {
        if (ability == null || UIManager.CurrentSelection == null) return false;
        if (CombatUtility.IsPlayerInputLocked()) return false;

        AttackUnit attackUnit = UIManager.CurrentSelection.GetAttackUnit();
        if (attackUnit == null) return false;

        GameObject selectedObject = attackUnit.gameObject;

        if (!ability.CanUseAfterMovement(selectedObject)) return false;
        if (attackUnit.GetAbilityCooldown(ability) > 0) return false;

        int usesPerTurn = ability.GetUsesPerTurn();
        if (usesPerTurn > 0 && attackUnit.GetAbilityUsesRemaining(ability) <= 0) return false;

        return true;
    }

    private void SelectAbility(int abilityIndex)
    {
        if (CombatUtility.IsPlayerInputLocked()) return;
        if (UIManager.CurrentSelection == null) return;
        if (!CanSelectAbilitiesForCurrentUnit()) return;

        CharacterSO character = UIManager.CurrentSelection.GetCharacterData();
        if (character == null) return;

        List<AbilitySO> abilities = character.GetAbilities();
        if (abilities == null || abilityIndex < 0 || abilityIndex >= abilities.Count) return;

        AbilitySO ability = abilities[abilityIndex];
        if (ability == null) return;

        if (!CanUseSelectedAbility(ability))
        {
            ClearAbilityHighlights();
            return;
        }

        selectedAbilityIndex = abilityIndex;
        selectedAbility = ability;

        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance.PlayAbilitySelect();
        }

        TriggerOpacityFlash();
        TriggerPulseBurst();
        RefreshCurrentSelection();
        ShowAbilityRange(abilityIndex);
    }

    private void ShowAbilityRange(int abilityIndex)
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            ClearAbilityHighlights();
            return;
        }

        if (gridManager == null || highlightManager == null || UIManager.CurrentSelection == null)
        {
            ClearAbilityHighlights();
            return;
        }

        CharacterSO character = UIManager.CurrentSelection.GetCharacterData();
        List<AbilitySO> abilities = character?.GetAbilities();

        if (abilities == null || abilityIndex < 0 || abilityIndex >= abilities.Count)
        {
            ClearAbilityHighlights();
            return;
        }

        AbilitySO ability = abilities[abilityIndex];
        GameObject selectedObject = UIManager.CurrentSelection.gameObject;

        if (ability == null || selectedObject == null)
        {
            ClearAbilityHighlights();
            return;
        }

        bool isPlayerControlled = CanSelectAbilitiesForCurrentUnit();

        if (isPlayerControlled && !CanUseSelectedAbility(ability))
        {
            ClearAbilityHighlights();
            return;
        }

        List<Vector2Int> rangeTiles = ability.GetRangeTiles(gridManager, selectedObject);

        if (rangeTiles == null || rangeTiles.Count == 0)
        {
            ClearAbilityHighlights();
            return;
        }

        if (ability is HealAbilitySO)
        {
            highlightManager.ShowHealTiles(rangeTiles, selectedObject);
        }
        else
        {
            highlightManager.ShowAbilityTiles(rangeTiles, selectedObject);
        }
    }

    public AbilitySO GetSelectedAbility() => selectedAbility;

    public int GetSelectedAbilityIndex() => selectedAbilityIndex;

    public bool HasSelectedAbility() => selectedAbility != null;

    private Camera GetEventCamera()
    {
        if (infoText == null) return null;

        if (cachedCanvas == null) cachedCanvas = infoText.canvas;

        if (cachedCanvas == null || cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

        if (eventCamera == null) eventCamera = cachedCanvas.worldCamera;

        return eventCamera;
    }

    private void ShowStatusTooltip(string statusId) => tooltipManager?.ShowStatusTooltip(statusId);

    private void HideStatusTooltip() => tooltipManager?.HideTooltip();

    private void ClearAbilityHover()
    {
        lastHoveredAbilityIndex = -1;

        if (selectedAbility != null && CanSelectAbilitiesForCurrentUnit())
        {
            ShowAbilityRange(selectedAbilityIndex);
        }
        else
        {
            ClearAbilityHighlights();
        }
    }

    private void ClearAbilityHighlights() => highlightManager?.ClearAbilityRange();

    public void ClearSelectedAbilityForEnemyTurn()
    {
        selectedAbilityIndex = -1;
        selectedAbility = null;
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        ClearAbilityHighlights();
        HideStatusTooltip();
        RefreshCurrentSelection();
    }

    public void ClearInfo()
    {
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;
        selectedAbilityIndex = -1;
        selectedAbility = null;

        ClearAbilityHighlights();
        HideStatusTooltip();

        if (infoText != null) infoText.text = string.Empty;

        if (characterIcon != null)
        {
            characterIcon.sprite = null;
            characterIcon.enabled = false;
        }
    }

    public void ClearSelectedAbility()
    {
        selectedAbilityIndex = -1;
        selectedAbility = null;
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        ClearAbilityHighlights();
        HideStatusTooltip();
        RefreshCurrentSelection();
    }
}