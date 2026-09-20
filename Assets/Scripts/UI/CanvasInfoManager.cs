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

    [Header("Input")]
    [SerializeField] private InputActionReference ability1Action;
    [SerializeField] private InputActionReference ability2Action;
    [SerializeField] private InputActionReference ability3Action;
    [SerializeField] private InputActionReference ability4Action;
    [SerializeField] private InputActionReference ability5Action;
    [SerializeField] private InputActionReference ability6Action;
    [SerializeField] private InputActionReference ability7Action;
    [SerializeField] private InputActionReference ability8Action;
    [SerializeField] private InputActionReference ability9Action;

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

    [Header("Icon Pop Juice")]
    [SerializeField] private bool enableIconPop = true;
    [SerializeField] private float iconPopDuration = 0.2f;
    [SerializeField] private float iconPopScale = 1.25f;

    [SerializeField]
    private AnimationCurve iconPopCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.55f, 0.65f),
            new Keyframe(0.8f, 0.25f),
            new Keyframe(1f, 0f)
        );

    [Header("Tooltips & Highlights")]
    [SerializeField] private tooltipManager tooltipManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;

    [Header("Debug")]
    [SerializeField] private bool debugAbilitySelection = true;

    private int lastHoveredAbilityIndex = -1;
    private int lastLinkIndex = -1;

    private int selectedAbilityIndex = -1;
    private AbilitySO selectedAbility;

    private Sprite currentCharacterIcon;
    private CharacterSO displayedCharacter;

    private Camera eventCamera;
    private Canvas cachedCanvas;

    private readonly StringBuilder textBuilder =
        new StringBuilder();

    private Vector3 initialBackgroundScale = Vector3.one;
    private Vector3 initialSecondScale = Vector3.one;
    private Vector3 initialCharacterIconScale = Vector3.one;

    private float defaultOpacity = 1f;
    private float defaultSecondOpacity = 1f;

    private float currentPulseMultiplier = 1f;

    private Coroutine flashCoroutine;
    private Coroutine pulseLerpCoroutine;
    private Coroutine iconPopCoroutine;


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugAbility(string message)
    {
        if (!debugAbilitySelection)
            return;

        Debug.Log(
            "[CanvasInfoManager Ability] " +
            message,
            this
        );
    }


    private void DebugAbilityWarning(string message)
    {
        if (!debugAbilitySelection)
            return;

        Debug.LogWarning(
            "[CanvasInfoManager Ability] " +
            message,
            this
        );
    }


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        SetupReferences();

        DebugAbility(
            "CanvasInfoManager initialized. " +
            "Instance ID = " +
            GetInstanceID()
        );

        if (backgroundImage != null)
        {
            initialBackgroundScale =
                backgroundImage.rectTransform.localScale;

            defaultOpacity =
                backgroundImage.color.a;
        }

        if (secondPulsingGraphic != null)
        {
            initialSecondScale =
                secondPulsingGraphic.rectTransform.localScale;

            defaultSecondOpacity =
                secondPulsingGraphic.color.a;
        }

        if (characterIcon != null)
        {
            initialCharacterIconScale =
                characterIcon.rectTransform.localScale;
        }
    }


    private void OnEnable()
    {
        AttackUnit.OnAbilityUsed += HandleAbilityUsed;

        RegisterInputAction(
            ability1Action,
            OnAbility1,
            "Ability 1"
        );

        RegisterInputAction(
            ability2Action,
            OnAbility2,
            "Ability 2"
        );

        RegisterInputAction(
            ability3Action,
            OnAbility3,
            "Ability 3"
        );

        RegisterInputAction(
            ability4Action,
            OnAbility4,
            "Ability 4"
        );

        RegisterInputAction(
            ability5Action,
            OnAbility5,
            "Ability 5"
        );

        RegisterInputAction(
            ability6Action,
            OnAbility6,
            "Ability 6"
        );

        RegisterInputAction(
            ability7Action,
            OnAbility7,
            "Ability 7"
        );

        RegisterInputAction(
            ability8Action,
            OnAbility8,
            "Ability 8"
        );

        RegisterInputAction(
            ability9Action,
            OnAbility9,
            "Ability 9"
        );
    }


    private void OnDisable()
    {
        AttackUnit.OnAbilityUsed -= HandleAbilityUsed;

        UnregisterInputAction(
            ability1Action,
            OnAbility1,
            "Ability 1"
        );

        UnregisterInputAction(
            ability2Action,
            OnAbility2,
            "Ability 2"
        );

        UnregisterInputAction(
            ability3Action,
            OnAbility3,
            "Ability 3"
        );

        UnregisterInputAction(
            ability4Action,
            OnAbility4,
            "Ability 4"
        );

        UnregisterInputAction(
            ability5Action,
            OnAbility5,
            "Ability 5"
        );

        UnregisterInputAction(
            ability6Action,
            OnAbility6,
            "Ability 6"
        );

        UnregisterInputAction(
            ability7Action,
            OnAbility7,
            "Ability 7"
        );

        UnregisterInputAction(
            ability8Action,
            OnAbility8,
            "Ability 8"
        );

        UnregisterInputAction(
            ability9Action,
            OnAbility9,
            "Ability 9"
        );
    }


    private void Update()
    {
        CheckAbilityHover();

        AnimateBackgroundPulse();

        if (
            CombatUtility.IsPlayerInputLocked() &&
            selectedAbility != null
        )
        {
            DebugAbility(
                "Input became locked. " +
                "Clearing selected ability."
            );

            ClearSelectedAbilityForEnemyTurn();
        }
    }


    // ============================================================
    // INPUT
    // ============================================================

    private void RegisterInputAction(
        InputActionReference actionReference,
        System.Action<InputAction.CallbackContext> callback,
        string debugName)
    {
        if (actionReference == null)
            return;

        if (actionReference.action == null)
            return;

        actionReference.action.performed += callback;
        actionReference.action.Enable();
    }


    private void UnregisterInputAction(
        InputActionReference actionReference,
        System.Action<InputAction.CallbackContext> callback,
        string debugName)
    {
        if (
            actionReference == null ||
            actionReference.action == null
        )
        {
            return;
        }

        actionReference.action.performed -= callback;
        actionReference.action.Disable();
    }


    private void OnAbility1(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(0);
    }


    private void OnAbility2(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(1);
    }


    private void OnAbility3(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(2);
    }


    private void OnAbility4(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(3);
    }


    private void OnAbility5(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(4);
    }


    private void OnAbility6(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(5);
    }


    private void OnAbility7(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(6);
    }


    private void OnAbility8(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(7);
    }


    private void OnAbility9(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SelectAbility(8);
    }


    // ============================================================
    // VISUAL JUICE
    // ============================================================

    private void AnimateBackgroundPulse()
    {
        if (!enablePulse)
            return;

        float sineProgress =
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float smoothProgress =
            Mathf.SmoothStep(
                0f,
                1f,
                sineProgress
            );

        float targetScale =
            Mathf.Lerp(
                minPulseScale,
                maxPulseScale,
                smoothProgress
            );

        float activeScaleOffset =
            (targetScale - 1f) *
            currentPulseMultiplier;

        Vector3 scaleVector =
            Vector3.one *
            (1f + activeScaleOffset);

        if (backgroundImage != null)
        {
            backgroundImage.rectTransform.localScale =
                Vector3.Scale(
                    initialBackgroundScale,
                    scaleVector
                );
        }

        if (secondPulsingGraphic != null)
        {
            secondPulsingGraphic.rectTransform.localScale =
                Vector3.Scale(
                    initialSecondScale,
                    scaleVector
                );
        }
    }


    private void TriggerIconPop()
    {
        if (
            !enableIconPop ||
            characterIcon == null
        )
        {
            return;
        }

        if (iconPopCoroutine != null)
        {
            StopCoroutine(iconPopCoroutine);
        }

        iconPopCoroutine =
            StartCoroutine(
                IconPopRoutine()
            );
    }


    private IEnumerator IconPopRoutine()
    {
        if (characterIcon == null)
            yield break;

        RectTransform iconTransform =
            characterIcon.rectTransform;

        float elapsed = 0f;

        while (elapsed < iconPopDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / iconPopDuration
                );

            float curveValue =
                iconPopCurve.Evaluate(
                    progress
                );

            float scaleMultiplier =
                Mathf.Lerp(
                    1f,
                    iconPopScale,
                    curveValue
                );

            iconTransform.localScale =
                initialCharacterIconScale *
                scaleMultiplier;

            yield return null;
        }

        iconTransform.localScale =
            initialCharacterIconScale;

        iconPopCoroutine = null;
    }


    private void TriggerOpacityFlash()
    {
        if (
            backgroundImage == null &&
            secondPulsingGraphic == null
        )
        {
            return;
        }

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine =
            StartCoroutine(
                OpacityFlashRoutine()
            );
    }


    private IEnumerator OpacityFlashRoutine()
    {
        Color bgCol =
            backgroundImage != null
                ? backgroundImage.color
                : Color.white;

        Color secCol =
            secondPulsingGraphic != null
                ? secondPulsingGraphic.color
                : Color.white;

        float halfDuration =
            flashDuration * 0.5f;

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (backgroundImage != null)
            {
                bgCol.a =
                    Mathf.Lerp(
                        defaultOpacity,
                        flashTargetOpacity,
                        easedProgress
                    );

                backgroundImage.color =
                    bgCol;
            }

            if (secondPulsingGraphic != null)
            {
                secCol.a =
                    Mathf.Lerp(
                        defaultSecondOpacity,
                        flashTargetOpacity,
                        easedProgress
                    );

                secondPulsingGraphic.color =
                    secCol;
            }

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (backgroundImage != null)
            {
                bgCol.a =
                    Mathf.Lerp(
                        flashTargetOpacity,
                        defaultOpacity,
                        easedProgress
                    );

                backgroundImage.color =
                    bgCol;
            }

            if (secondPulsingGraphic != null)
            {
                secCol.a =
                    Mathf.Lerp(
                        flashTargetOpacity,
                        defaultSecondOpacity,
                        easedProgress
                    );

                secondPulsingGraphic.color =
                    secCol;
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

        flashCoroutine = null;
    }


    private void TriggerPulseBurst()
    {
        if (pulseLerpCoroutine != null)
            StopCoroutine(pulseLerpCoroutine);

        pulseLerpCoroutine =
            StartCoroutine(
                PulseBurstRoutine()
            );
    }


    private IEnumerator PulseBurstRoutine()
    {
        float halfDuration =
            flashDuration * 0.5f;

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            currentPulseMultiplier =
                Mathf.Lerp(
                    1f,
                    selectionPulseMultiplier,
                    easedProgress
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            currentPulseMultiplier =
                Mathf.Lerp(
                    selectionPulseMultiplier,
                    1f,
                    easedProgress
                );

            yield return null;
        }

        currentPulseMultiplier = 1f;
        pulseLerpCoroutine = null;
    }


    // ============================================================
    // ABILITY USED
    // ============================================================

    private void HandleAbilityUsed(
        AttackUnit attackUnit,
        AbilitySO ability)
    {
        if (attackUnit == null)
            return;

        if (UIManager.CurrentSelection == null)
            return;

        AttackUnit selectedAttackUnit =
            UIManager.CurrentSelection
                .GetAttackUnit();

        if (selectedAttackUnit != attackUnit)
            return;

        CharacterSO character =
            attackUnit.GetCharacterData();

        if (character != null)
        {
            RefreshCharacter(character);
        }
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void SetupReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (
            highlightManager == null &&
            gridManager != null
        )
        {
            highlightManager =
                gridManager.GetHighlightManager();
        }

        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }

        if (tooltipManager == null)
        {
            tooltipManager =
                FindFirstObjectByType<tooltipManager>();
        }

        if (infoText != null)
        {
            cachedCanvas =
                infoText.canvas;
        }
    }


    // ============================================================
    // CHARACTER DISPLAY
    // ============================================================

    public void RefreshCurrentSelection()
    {
        if (UIManager.CurrentSelection == null)
            return;

        AttackUnit attackUnit =
            UIManager.CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
            return;

        CharacterSO character =
            attackUnit.GetCharacterData();

        if (character != null)
        {
            RefreshCharacter(character);
        }
    }


    public void ShowCharacter(
        ICharacterHolder characterHolder)
    {
        if (characterHolder != null)
        {
            ShowCharacter(
                characterHolder.GetCharacterData()
            );
        }
    }


    public void ShowCharacter(
        CharacterSO character)
    {
        if (character == null)
            return;

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        HideStatusTooltip();

        RefreshCharacter(character);
    }


    private void RefreshCharacter(
        CharacterSO character)
    {
        if (character == null)
            return;

        displayedCharacter =
            character;

        currentCharacterIcon =
            character.icon;

        textBuilder.Clear();

        textBuilder.AppendLine(
            $"<b>{character.characterName}</b>\n"
        );

        textBuilder.AppendLine(
            $"Team: {character.team}\n"
        );

        List<AbilitySO> abilities =
            character.GetAbilities();

        if (
            abilities != null &&
            abilities.Count > 0
        )
        {
            textBuilder.AppendLine(
                "<b>Abilities</b>\n"
            );

            AttackUnit activeUnit =
                UIManager.CurrentSelection
                    ?.GetAttackUnit();

            bool canSelectAbilities =
                !CombatUtility.IsPlayerInputLocked() &&
                activeUnit != null &&
                (
                    activeUnit.GetTeam() == Team.Player ||
                    activeUnit.GetTeam() == Team.Ally
                );

            for (
                int i = 0;
                i < abilities.Count;
                i++
            )
            {
                AbilitySO ability =
                    abilities[i];

                if (ability == null)
                    continue;

                string selectedText =
                    string.Empty;

                if (
                    canSelectAbilities &&
                    selectedAbilityIndex == i &&
                    selectedAbility != null &&
                    selectedAbility == ability
                )
                {
                    string selectedColor =
                        ColorUtility.ToHtmlStringRGB(
                            selectedAbilityColor
                        );

                    selectedText =
                        $" <color=#{selectedColor}><b>(Selected)</b></color>";
                }

                string abilityName =
                    ability.GetAbilityName();

                string abilityLink =
                    $"<link=\"ability_{i}\"><color=yellow><u><b>{abilityName}</b></u></color></link>";

                textBuilder.AppendLine(
                    abilityLink +
                    selectedText
                );

                string description =
                    ability.GetDescription();

                description =
                    AddAbilityIndexToStatusLinks(
                        description,
                        i
                    );

                if (
                    !string.IsNullOrEmpty(
                        description
                    )
                )
                {
                    textBuilder.AppendLine(
                        description
                    );
                }

                // =================================================
                // EFFECTIVE DAMAGE / RANGE
                // =================================================
                //
                // IMPORTANT:
                //
                // We use the active unit here instead of directly
                // accessing UpgradeableCombatUnit.
                //
                // That keeps this UI generic for all units.
                //
                // For a normal unit:
                //     effective damage = base damage
                //
                // For an upgraded Fortress/turret:
                //     effective damage = base + bonus
                //
                // AbilitySO.GetEffectiveDamage() also safely falls
                // back to base damage if the unit has no upgradeable
                // combat component.
                // =================================================

                if (
                    ability is HealAbilitySO healAbility
                )
                {
                    textBuilder.AppendLine(
                        $"Heal: {healAbility.GetHealAmount()} | Range: {ability.GetRange()}"
                    );
                }
                else
                {
                    int damage =
                        ability.GetDamage();

                    int range =
                        ability.GetRange();

                    if (activeUnit != null)
                    {
                        damage =
                            activeUnit.GetEffectiveDamage(
                                ability
                            );

                        range =
                            activeUnit.GetEffectiveRange(
                                ability
                            );
                    }

                    textBuilder.AppendLine(
                        $"Damage: {damage} | Range: {range}"
                    );
                }

                int usesPerTurn =
                    ability.GetUsesPerTurn();

                string usesText;

                if (usesPerTurn <= 0)
                {
                    usesText = "Unlimited";
                }
                else
                {
                    int remainingUses =
                        activeUnit != null
                            ? activeUnit.GetAbilityUsesRemaining(
                                ability
                            )
                            : usesPerTurn;

                    usesText =
                        $"{remainingUses}/{usesPerTurn}";
                }

                string cooldownText;

                if (usesPerTurn <= 0)
                {
                    cooldownText = "None";
                }
                else
                {
                    int currentCooldown =
                        activeUnit != null
                            ? activeUnit.GetAbilityCooldown(
                                ability
                            )
                            : 0;

                    if (
                        activeUnit == null ||
                        usesText ==
                        $"{usesPerTurn}/{usesPerTurn}"
                    )
                    {
                        cooldownText = "Ready";
                    }
                    else if (currentCooldown > 0)
                    {
                        cooldownText =
                            $"Next: {currentCooldown}";
                    }
                    else
                    {
                        cooldownText = "Ready";
                    }
                }

                textBuilder.AppendLine(
                    $"Cooldown: {cooldownText} | Uses: {usesText}\n"
                );
            }
        }
        else
        {
            textBuilder.Append(
                "<b>No Abilities</b>"
            );
        }

        if (infoText != null)
        {
            infoText.text =
                textBuilder.ToString();

            infoText.ForceMeshUpdate();

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                infoText.rectTransform
            );
        }

        UpdateCharacterIcon();
    }


    private string AddAbilityIndexToStatusLinks(
        string description,
        int abilityIndex)
    {
        if (
            string.IsNullOrEmpty(
                description
            )
        )
        {
            return description;
        }

        return description.Replace(
            "<link=\"status_stun\">",
            $"<link=\"status_stun_{abilityIndex}\">"
        );
    }


    private void UpdateCharacterIcon()
    {
        if (characterIcon == null)
            return;

        Sprite newSprite =
            currentCharacterIcon;

        if (selectedAbility != null)
        {
            Sprite abilityIcon =
                selectedAbility.GetAbilityIcon();

            if (abilityIcon != null)
            {
                newSprite =
                    abilityIcon;
            }
        }

        bool spriteChanged =
            characterIcon.sprite != newSprite;

        characterIcon.sprite =
            newSprite;

        characterIcon.enabled =
            newSprite != null;

        if (
            spriteChanged &&
            newSprite != null
        )
        {
            TriggerIconPop();
        }
    }


    // ============================================================
    // ABILITY HOVER
    // ============================================================

    private void CheckAbilityHover()
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            HideStatusTooltip();
            ClearAbilityHighlights();
            return;
        }

        if (
            infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null
        )
        {
            HideStatusTooltip();
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                GetEventCamera()
            );

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

        TMP_TextInfo textInfo =
            infoText.textInfo;

        if (
            linkIndex < 0 ||
            linkIndex >= textInfo.linkCount
        )
        {
            HideStatusTooltip();
            return;
        }

        string linkId =
            textInfo.linkInfo[linkIndex]
                .GetLinkID();

        if (
            linkId.StartsWith(
                "status_"
            )
        )
        {
            if (linkIndex != lastLinkIndex)
            {
                lastLinkIndex =
                    linkIndex;

                ShowStatusTooltip(
                    linkId.Substring(
                        "status_".Length
                    )
                );
            }

            return;
        }

        if (
            linkId.StartsWith(
                "ability_"
            )
        )
        {
            HideStatusTooltip();

            if (
                linkIndex ==
                lastLinkIndex
            )
            {
                return;
            }

            lastLinkIndex =
                linkIndex;

            string indexString =
                linkId.Substring(
                    "ability_".Length
                );

            if (
                int.TryParse(
                    indexString,
                    out int abilityIndex
                )
            )
            {
                if (
                    abilityIndex !=
                    lastHoveredAbilityIndex
                )
                {
                    lastHoveredAbilityIndex =
                        abilityIndex;

                    ShowAbilityRange(
                        abilityIndex
                    );
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


    public bool IsPointerOverAbilityLink() =>
        CheckLinkPrefix("ability_");


    public bool IsPointerOverStatusLink() =>
        CheckLinkPrefix("status_");


    private bool CheckLinkPrefix(
        string prefix)
    {
        if (CombatUtility.IsPlayerInputLocked())
            return false;

        if (
            infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null
        )
        {
            return false;
        }

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                Mouse.current.position.ReadValue(),
                GetEventCamera()
            );

        if (
            linkIndex < 0 ||
            linkIndex >= infoText.textInfo.linkCount
        )
        {
            return false;
        }

        return infoText.textInfo
            .linkInfo[linkIndex]
            .GetLinkID()
            .StartsWith(prefix);
    }


    // ============================================================
    // SELECT ABILITY UNDER MOUSE
    // ============================================================

    public bool TrySelectAbilityUnderMouse()
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            DebugAbilityWarning(
                "TrySelectAbilityUnderMouse BLOCKED: " +
                "player input is locked."
            );

            return false;
        }

        if (infoText == null)
        {
            DebugAbilityWarning(
                "TrySelectAbilityUnderMouse BLOCKED: " +
                "infoText is NULL."
            );

            return false;
        }

        if (!infoText.gameObject.activeInHierarchy)
        {
            DebugAbilityWarning(
                "TrySelectAbilityUnderMouse BLOCKED: " +
                "infoText is inactive."
            );

            return false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                GetEventCamera()
            );

        if (
            linkIndex < 0 ||
            linkIndex >= infoText.textInfo.linkCount
        )
        {
            return false;
        }

        string linkId =
            infoText.textInfo
                .linkInfo[linkIndex]
                .GetLinkID();

        DebugAbility(
            "Clicked TMP link = " +
            linkId
        );

        if (
            !linkId.StartsWith(
                "ability_"
            )
        )
        {
            return false;
        }

        string indexString =
            linkId.Substring(
                "ability_".Length
            );

        if (
            !int.TryParse(
                indexString,
                out int abilityIndex
            )
        )
        {
            DebugAbilityWarning(
                "Could not parse ability index from " +
                linkId
            );

            return true;
        }

        DebugAbility(
            "Ability link clicked. " +
            "Index = " +
            abilityIndex
        );

        bool selected =
            SelectAbility(
                abilityIndex
            );

        DebugAbility(
            "SelectAbility(" +
            abilityIndex +
            ") returned " +
            selected +
            " | selectedAbility = " +
            (
                selectedAbility != null
                    ? selectedAbility.GetAbilityName()
                    : "NULL"
            )
        );

        return true;
    }


    // ============================================================
    // CAN SELECT ABILITIES
    // ============================================================

    private bool CanSelectAbilitiesForCurrentUnit()
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            DebugAbility(
                "CanSelectAbilitiesForCurrentUnit = FALSE " +
                "(input locked)"
            );

            return false;
        }

        if (UIManager.CurrentSelection == null)
        {
            DebugAbility(
                "CanSelectAbilitiesForCurrentUnit = FALSE " +
                "(no current selection)"
            );

            return false;
        }

        AttackUnit attackUnit =
            UIManager.CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            DebugAbility(
                "CanSelectAbilitiesForCurrentUnit = FALSE " +
                "(AttackUnit NULL)"
            );

            return false;
        }

        Team team =
            attackUnit.GetTeam();

        bool result =
            team == Team.Player ||
            team == Team.Ally;

        DebugAbility(
            "CanSelectAbilitiesForCurrentUnit = " +
            result +
            " | Unit=" +
            attackUnit.name +
            " | Team=" +
            team
        );

        return result;
    }


    // ============================================================
    // CAN USE ABILITY
    // ============================================================

    private bool CanUseSelectedAbility(
        AbilitySO ability)
    {
        if (ability == null)
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: ability NULL."
            );

            return false;
        }

        if (UIManager.CurrentSelection == null)
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: " +
                "CurrentSelection NULL."
            );

            return false;
        }

        if (CombatUtility.IsPlayerInputLocked())
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: " +
                "input locked."
            );

            return false;
        }

        AttackUnit attackUnit =
            UIManager.CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: " +
                "AttackUnit NULL."
            );

            return false;
        }

        GameObject selectedObject =
            attackUnit.gameObject;

        bool canUseAfterMovement =
            ability.CanUseAfterMovement(
                selectedObject
            );

        int usesPerTurn =
            ability.GetUsesPerTurn();

        int remainingUses =
            attackUnit.GetAbilityUsesRemaining(
                ability
            );

        int cooldown =
            attackUnit.GetAbilityCooldown(
                ability
            );

        DebugAbility(
            "CanUseSelectedAbility | " +
            "Ability=" +
            ability.GetAbilityName() +
            " | UsesPerTurn=" +
            usesPerTurn +
            " | UsesRemaining=" +
            remainingUses +
            " | Cooldown=" +
            cooldown +
            " | CanUseAfterMovement=" +
            canUseAfterMovement
        );

        // --------------------------------------------------------
        // Movement restriction
        // --------------------------------------------------------

        if (!canUseAfterMovement)
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: " +
                "ability cannot be used after movement."
            );

            return false;
        }

        // --------------------------------------------------------
        // Unlimited
        // --------------------------------------------------------

        if (usesPerTurn <= 0)
        {
            DebugAbility(
                "CanUseSelectedAbility TRUE: unlimited uses."
            );

            return true;
        }

        // --------------------------------------------------------
        // Per-charge uses
        // --------------------------------------------------------

        if (remainingUses <= 0)
        {
            DebugAbilityWarning(
                "CanUseSelectedAbility FALSE: " +
                "no ready charges remain."
            );

            return false;
        }

        DebugAbility(
            "CanUseSelectedAbility TRUE."
        );

        return true;
    }


    // ============================================================
    // SELECT ABILITY
    // ============================================================

    private bool SelectAbility(
        int abilityIndex)
    {
        DebugAbility(
            "================================================"
        );

        DebugAbility(
            "SelectAbility(" +
            abilityIndex +
            ") START"
        );

        DebugAbility(
            "Current CanvasInfoManager instance ID = " +
            GetInstanceID()
        );

        // --------------------------------------------------------
        // INPUT
        // --------------------------------------------------------

        if (CombatUtility.IsPlayerInputLocked())
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "player input is locked."
            );

            return false;
        }

        // --------------------------------------------------------
        // SELECTION
        // --------------------------------------------------------

        if (UIManager.CurrentSelection == null)
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "CurrentSelection is NULL."
            );

            return false;
        }

        DebugAbility(
            "Current selection = " +
            UIManager.CurrentSelection.name
        );

        // --------------------------------------------------------
        // UNIT
        // --------------------------------------------------------

        AttackUnit attackUnit =
            UIManager.CurrentSelection
                .GetAttackUnit();

        if (attackUnit == null)
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "AttackUnit is NULL."
            );

            return false;
        }

        Team team =
            attackUnit.GetTeam();

        DebugAbility(
            "Selected AttackUnit = " +
            attackUnit.name +
            " | Team = " +
            team
        );

        if (
            team != Team.Player &&
            team != Team.Ally
        )
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "unit is not Player or Ally."
            );

            return false;
        }

        // --------------------------------------------------------
        // CHARACTER
        // --------------------------------------------------------

        CharacterSO character =
            attackUnit.GetCharacterData();

        if (character == null)
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "CharacterSO is NULL."
            );

            return false;
        }

        // --------------------------------------------------------
        // ABILITIES
        // --------------------------------------------------------

        List<AbilitySO> abilities =
            character.GetAbilities();

        if (abilities == null)
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "Character abilities list is NULL."
            );

            return false;
        }

        DebugAbility(
            "Character ability count = " +
            abilities.Count
        );

        if (
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count
        )
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "ability index " +
                abilityIndex +
                " is outside list."
            );

            return false;
        }

        AbilitySO ability =
            abilities[abilityIndex];

        if (ability == null)
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "AbilitySO at index " +
                abilityIndex +
                " is NULL."
            );

            return false;
        }

        DebugAbility(
            "Ability = " +
            ability.GetAbilityName()
        );

        // --------------------------------------------------------
        // ABILITY STATE
        // --------------------------------------------------------

        int usesPerTurn =
            ability.GetUsesPerTurn();

        int usesRemaining =
            attackUnit.GetAbilityUsesRemaining(
                ability
            );

        int cooldown =
            attackUnit.GetAbilityCooldown(
                ability
            );

        bool canUseAfterMovement =
            ability.CanUseAfterMovement(
                attackUnit.gameObject
            );

        DebugAbility(
            "Ability state | " +
            "UsesPerTurn=" +
            usesPerTurn +
            " | UsesRemaining=" +
            usesRemaining +
            " | Cooldown=" +
            cooldown +
            " | CanUseAfterMovement=" +
            canUseAfterMovement
        );

        // --------------------------------------------------------
        // USE CHECK
        // --------------------------------------------------------

        if (
            !CanUseSelectedAbility(
                ability
            )
        )
        {
            DebugAbilityWarning(
                "SelectAbility FAILED: " +
                "CanUseSelectedAbility returned FALSE."
            );

            return false;
        }

        // --------------------------------------------------------
        // ACTUALLY SELECT
        // --------------------------------------------------------

        selectedAbilityIndex =
            abilityIndex;

        selectedAbility =
            ability;

        DebugAbility(
            "SELECTED ABILITY = " +
            selectedAbility.GetAbilityName()
        );

        DebugAbility(
            "selectedAbilityIndex = " +
            selectedAbilityIndex
        );

        // --------------------------------------------------------
        // VISUALS
        // --------------------------------------------------------

        UpdateCharacterIcon();

        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance
                .PlayAbilitySelect();
        }

        TriggerOpacityFlash();
        TriggerPulseBurst();

        // --------------------------------------------------------
        // REFRESH UI
        // --------------------------------------------------------

        RefreshCurrentSelection();

        // --------------------------------------------------------
        // RANGE
        // --------------------------------------------------------

        ShowAbilityRange(
            abilityIndex
        );

        // --------------------------------------------------------
        // FINAL VERIFICATION
        // --------------------------------------------------------

        DebugAbility(
            "SelectAbility COMPLETE | " +
            "selectedAbility = " +
            (
                selectedAbility != null
                    ? selectedAbility.GetAbilityName()
                    : "NULL"
            ) +
            " | index = " +
            selectedAbilityIndex
        );

        DebugAbility(
            "================================================"
        );

        return selectedAbility != null;
    }


    // ============================================================
    // SHOW ABILITY RANGE
    // ============================================================

    private void ShowAbilityRange(
        int abilityIndex)
    {
        if (CombatUtility.IsPlayerInputLocked())
        {
            ClearAbilityHighlights();
            return;
        }

        if (
            gridManager == null ||
            highlightManager == null ||
            UIManager.CurrentSelection == null
        )
        {
            ClearAbilityHighlights();
            return;
        }

        CharacterSO character =
            UIManager.CurrentSelection
                .GetCharacterData();

        List<AbilitySO> abilities =
            character?.GetAbilities();

        if (
            abilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count
        )
        {
            ClearAbilityHighlights();
            return;
        }

        AbilitySO ability =
            abilities[abilityIndex];

        GameObject selectedObject =
            UIManager.CurrentSelection.gameObject;

        if (
            ability == null ||
            selectedObject == null
        )
        {
            ClearAbilityHighlights();
            return;
        }

        bool isPlayerControlled =
            CanSelectAbilitiesForCurrentUnit();

        if (
            isPlayerControlled &&
            !CanUseSelectedAbility(
                ability
            )
        )
        {
            DebugAbility(
                "ShowAbilityRange: " +
                "ability cannot currently be used."
            );

            ClearAbilityHighlights();
            return;
        }

        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                selectedObject
            );

        if (
            rangeTiles == null ||
            rangeTiles.Count == 0
        )
        {
            ClearAbilityHighlights();
            return;
        }

        if (ability is HealAbilitySO)
        {
            highlightManager.ShowHealTiles(
                rangeTiles,
                selectedObject
            );
        }
        else
        {
            highlightManager.ShowAbilityTiles(
                rangeTiles,
                selectedObject
            );
        }
    }


    // ============================================================
    // SELECTED ABILITY ACCESS
    // ============================================================

    public AbilitySO GetSelectedAbility()
    {
        return selectedAbility;
    }


    public int GetSelectedAbilityIndex()
    {
        return selectedAbilityIndex;
    }


    public bool HasSelectedAbility()
    {
        return selectedAbility != null;
    }


    // ============================================================
    // EVENT CAMERA
    // ============================================================

    private Camera GetEventCamera()
    {
        if (infoText == null)
            return null;

        if (cachedCanvas == null)
        {
            cachedCanvas =
                infoText.canvas;
        }

        if (
            cachedCanvas == null ||
            cachedCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            return null;
        }

        if (eventCamera == null)
        {
            eventCamera =
                cachedCanvas.worldCamera;
        }

        return eventCamera;
    }


    // ============================================================
    // STATUS TOOLTIP
    // ============================================================

    private void ShowStatusTooltip(
        string statusData)
    {
        if (tooltipManager == null)
            return;

        string[] parts =
            statusData.Split('_');

        if (parts.Length < 2)
        {
            tooltipManager.ShowStatusTooltip(
                statusData
            );

            return;
        }

        string statusId =
            parts[0];

        string abilityIndexString =
            parts[parts.Length - 1];

        if (
            !int.TryParse(
                abilityIndexString,
                out int abilityIndex
            )
        )
        {
            tooltipManager.ShowStatusTooltip(
                statusId
            );

            return;
        }

        if (displayedCharacter == null)
        {
            tooltipManager.ShowStatusTooltip(
                statusId
            );

            return;
        }

        List<AbilitySO> abilities =
            displayedCharacter.GetAbilities();

        if (
            abilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count
        )
        {
            tooltipManager.ShowStatusTooltip(
                statusId
            );

            return;
        }

        AbilitySO ability =
            abilities[abilityIndex];

        if (ability == null)
        {
            tooltipManager.ShowStatusTooltip(
                statusId
            );

            return;
        }

        float stunChance = 0f;
        int stunDuration = 1;

        if (
            statusId.Equals(
                "stun",
                System.StringComparison.OrdinalIgnoreCase
            ) &&
            ability is ChainLightning chainLightning
        )
        {
            stunChance =
                chainLightning.GetStunPercentage();

            stunDuration =
                chainLightning.GetStunDuration();
        }

        tooltipManager.ShowStatusTooltip(
            statusId,
            stunChance,
            stunDuration
        );
    }


    private void HideStatusTooltip()
    {
        tooltipManager?.HideTooltip();
    }


    // ============================================================
    // ABILITY HOVER CLEAR
    // ============================================================

    private void ClearAbilityHover()
    {
        lastHoveredAbilityIndex = -1;

        if (
            selectedAbility != null &&
            CanSelectAbilitiesForCurrentUnit()
        )
        {
            ShowAbilityRange(
                selectedAbilityIndex
            );
        }
        else
        {
            ClearAbilityHighlights();
        }
    }


    private void ClearAbilityHighlights()
    {
        highlightManager?.ClearAbilityRange();
    }


    // ============================================================
    // CLEAR SELECTED ABILITY
    // ============================================================

    public void ClearSelectedAbilityForEnemyTurn()
    {
        DebugAbility(
            "ClearSelectedAbilityForEnemyTurn()"
        );

        selectedAbilityIndex = -1;
        selectedAbility = null;

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        ClearAbilityHighlights();
        HideStatusTooltip();

        UpdateCharacterIcon();

        RefreshCurrentSelection();
    }


    public void ClearSelectedAbility()
    {
        DebugAbility(
            "ClearSelectedAbility()"
        );

        selectedAbilityIndex = -1;
        selectedAbility = null;

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        ClearAbilityHighlights();
        HideStatusTooltip();

        UpdateCharacterIcon();

        RefreshCurrentSelection();
    }


    // ============================================================
    // CLEAR INFO
    // ============================================================

    public void ClearInfo()
    {
        DebugAbility(
            "ClearInfo()"
        );

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        selectedAbilityIndex = -1;
        selectedAbility = null;

        currentCharacterIcon = null;
        displayedCharacter = null;

        if (iconPopCoroutine != null)
        {
            StopCoroutine(iconPopCoroutine);

            iconPopCoroutine = null;
        }

        if (characterIcon != null)
        {
            characterIcon.sprite = null;
            characterIcon.enabled = false;

            characterIcon.rectTransform.localScale =
                initialCharacterIconScale;
        }

        ClearAbilityHighlights();
        HideStatusTooltip();

        if (infoText != null)
        {
            infoText.text = string.Empty;
        }
    }
}