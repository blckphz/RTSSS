using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CanvasInfoManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text infoText;

    [SerializeField]
    private Image characterIcon;


    [Header("Grid Highlighting")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GridHighlightManager highlightManager;


    private int lastHoveredAbilityIndex = -1;
    private int lastLinkIndex = -1;

    private int selectedAbilityIndex = -1;

    private AbilitySO selectedAbility;

    private readonly StringBuilder textBuilder =
        new StringBuilder();


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        SetupReferences();
    }


    private void OnEnable()
    {
        AttackUnit.OnAbilityUsed +=
            HandleAbilityUsed;

        HealthManager.OnHealthChanged +=
            HandleHealthChanged;
    }


    private void OnDisable()
    {
        AttackUnit.OnAbilityUsed -=
            HandleAbilityUsed;

        HealthManager.OnHealthChanged -=
            HandleHealthChanged;
    }


    private void Update()
    {
        CheckAbilityHover();
    }


    // ============================================================
    // HEALTH CHANGED EVENT
    // ============================================================

    private void HandleHealthChanged(
        HealthManager healthManager)
    {
        if (healthManager == null)
        {
            return;
        }


        if (UIManager.CurrentSelection == null)
        {
            return;
        }


        AttackUnit selectedAttackUnit =
            UIManager.CurrentSelection.GetAttackUnit();


        if (selectedAttackUnit == null)
        {
            return;
        }


        HealthManager selectedHealthManager =
            selectedAttackUnit.GetComponent<HealthManager>();


        if (selectedHealthManager != healthManager)
        {
            return;
        }


        CharacterSO character =
            selectedAttackUnit.GetCharacterData();


        if (character == null)
        {
            return;
        }


        RefreshCharacter(
            character
        );
    }


    // ============================================================
    // ABILITY USED EVENT
    // ============================================================

    private void HandleAbilityUsed(
        AttackUnit attackUnit,
        AbilitySO ability)
    {
        if (attackUnit == null)
        {
            return;
        }


        if (UIManager.CurrentSelection == null)
        {
            return;
        }


        AttackUnit selectedAttackUnit =
            UIManager.CurrentSelection.GetAttackUnit();


        if (selectedAttackUnit != attackUnit)
        {
            return;
        }


        CharacterSO character =
            attackUnit.GetCharacterData();


        if (character == null)
        {
            return;
        }


        RefreshCharacter(
            character
        );
    }


    // ============================================================
    // REFRESH CURRENT SELECTION
    // ============================================================

    public void RefreshCurrentSelection()
    {
        if (UIManager.CurrentSelection == null)
        {
            return;
        }


        AttackUnit attackUnit =
            UIManager.CurrentSelection.GetAttackUnit();


        if (attackUnit == null)
        {
            return;
        }


        CharacterSO character =
            attackUnit.GetCharacterData();


        if (character == null)
        {
            return;
        }


        RefreshCharacter(
            character
        );
    }


    // ============================================================
    // SETUP
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
    }


    // ============================================================
    // SHOW CHARACTER
    // ============================================================

    public void ShowCharacter(
        ICharacterHolder characterHolder)
    {
        if (characterHolder == null)
        {
            return;
        }


        ShowCharacter(
            characterHolder.GetCharacterData()
        );
    }


    // ============================================================
    // SHOW CHARACTER
    // ============================================================

    public void ShowCharacter(
        CharacterSO character)
    {
        if (character == null)
        {
            return;
        }


        /*
         * IMPORTANT:
         *
         * This method is used for HOVER INFORMATION.
         *
         * Hovering another unit must NOT cancel the currently
         * selected ability.
         *
         * Therefore we intentionally DO NOT do:
         *
         * selectedAbilityIndex = -1;
         * selectedAbility = null;
         * ClearAbilityHighlights();
         *
         * The selected attack belongs to the currently selected
         * AttackUnit and must remain active while hovering.
         */


        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;


        RefreshCharacter(
            character
        );
    }


    // ============================================================
    // REFRESH CHARACTER
    // ============================================================

    private void RefreshCharacter(
        CharacterSO character)
    {
        if (character == null)
        {
            return;
        }


        textBuilder.Clear();


        // ========================================================
        // CHARACTER
        // ========================================================

        textBuilder.AppendLine(
            $"<b>{character.characterName}</b>\n"
        );


        textBuilder.AppendLine(
            $"Team: {character.team}"
        );


        // ========================================================
        // HEALTH
        // ========================================================

        int currentHealth =
            character.maxHealth;

        int maxHealth =
            character.maxHealth;


        AttackUnit selectedAttackUnit = null;


        if (UIManager.CurrentSelection != null)
        {
            selectedAttackUnit =
                UIManager.CurrentSelection.GetAttackUnit();
        }


        /*
         * We first try to get the HealthManager from the
         * currently selected AttackUnit.
         *
         * If the UI is showing another hovered character,
         * we also try to find that character's HealthManager.
         */

        HealthManager healthManager = null;


        if (selectedAttackUnit != null)
        {
            healthManager =
                selectedAttackUnit.GetComponent<HealthManager>();
        }


        /*
         * If the displayed character is not the selected unit,
         * try to find the corresponding unit from the scene.
         */

        if (
            healthManager == null ||
            selectedAttackUnit == null ||
            selectedAttackUnit.GetCharacterData() != character
        )
        {
            AttackUnit[] attackUnits =
                FindObjectsByType<AttackUnit>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < attackUnits.Length;
                i++
            )
            {
                AttackUnit attackUnit =
                    attackUnits[i];


                if (attackUnit == null)
                {
                    continue;
                }


                CharacterSO unitCharacter =
                    attackUnit.GetCharacterData();


                if (unitCharacter != character)
                {
                    continue;
                }


                healthManager =
                    attackUnit.GetComponent<HealthManager>();


                break;
            }
        }


        if (healthManager != null)
        {
            currentHealth =
                healthManager.GetHealth();

            maxHealth =
                healthManager.GetMaxHealth();
        }


        textBuilder.AppendLine(
            $"Health: {currentHealth}/{maxHealth}\n"
        );


        // ========================================================
        // ABILITIES
        // ========================================================

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


            AttackUnit attackUnit = null;


            /*
             * IMPORTANT:
             *
             * Ability cooldowns and uses are always taken from
             * the actual selected AttackUnit.
             *
             * Hovering another unit does NOT change this.
             */

            if (UIManager.CurrentSelection != null)
            {
                attackUnit =
                    UIManager.CurrentSelection
                        .GetAttackUnit();
            }


            for (
                int i = 0;
                i < abilities.Count;
                i++
            )
            {
                AbilitySO ability =
                    abilities[i];


                if (ability == null)
                {
                    continue;
                }


                string abilityName =
                    ability.GetAbilityName();


                // ------------------------------------------------
                // ABILITY NAME
                // ------------------------------------------------

                textBuilder.AppendLine(
                    $"<link=\"ability_{i}\">" +
                    $"<color=yellow>" +
                    $"<u><b>{abilityName}</b></u>" +
                    $"</color></link>"
                );


                // ------------------------------------------------
                // DESCRIPTION
                // ------------------------------------------------

                textBuilder.AppendLine(
                    ability.GetDescription()
                );


                // ------------------------------------------------
                // DAMAGE / HEAL
                // ------------------------------------------------

                if (ability is HealAbilitySO healAbility)
                {
                    textBuilder.AppendLine(
                        $"Heal: " +
                        $"{healAbility.GetHealAmount()}"
                    );
                }
                else
                {
                    textBuilder.AppendLine(
                        $"Damage: " +
                        $"{ability.GetDamage()}"
                    );
                }


                // ------------------------------------------------
                // RANGE
                // ------------------------------------------------

                textBuilder.AppendLine(
                    $"Range: " +
                    $"{ability.GetRange()}"
                );


                // ------------------------------------------------
                // SHAPE
                // ------------------------------------------------

                textBuilder.AppendLine(
                    $"Shape: " +
                    $"{ability.GetRangeShape()}"
                );


                // ------------------------------------------------
                // COOLDOWN
                // ------------------------------------------------

                int currentCooldown =
                    ability.GetCooldown();


                if (attackUnit != null)
                {
                    currentCooldown =
                        attackUnit.GetAbilityCooldown(
                            ability
                        );
                }


                textBuilder.AppendLine(
                    $"Cooldown: " +
                    $"{currentCooldown}"
                );


                // ------------------------------------------------
                // USES PER TURN
                // ------------------------------------------------

                int usesPerTurn =
                    ability.GetUsesPerTurn();


                if (usesPerTurn <= 0)
                {
                    textBuilder.AppendLine(
                        "Uses: Unlimited"
                    );
                }
                else
                {
                    int usesRemaining =
                        usesPerTurn;


                    if (attackUnit != null)
                    {
                        usesRemaining =
                            attackUnit
                                .GetAbilityUsesRemaining(
                                    ability
                                );
                    }


                    textBuilder.AppendLine(
                        $"Uses: " +
                        $"{usesRemaining}" +
                        $"/{usesPerTurn}"
                    );
                }


                textBuilder.AppendLine();
            }
        }
        else
        {
            textBuilder.Append(
                "<b>No Abilities</b>"
            );
        }


        // ========================================================
        // TEXT
        // ========================================================

        if (infoText != null)
        {
            infoText.text =
                textBuilder.ToString();


            infoText.ForceMeshUpdate();
        }


        // ========================================================
        // ICON
        // ========================================================

        if (characterIcon != null)
        {
            characterIcon.sprite =
                character.icon;


            characterIcon.enabled =
                character.icon != null;
        }
    }


    // ============================================================
    // HOVER
    // ============================================================

    private void CheckAbilityHover()
    {
        if (
            infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null
        )
        {
            return;
        }


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        Camera eventCamera =
            GetEventCamera();


        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                eventCamera
            );


        if (linkIndex == lastLinkIndex)
        {
            return;
        }


        lastLinkIndex =
            linkIndex;


        if (linkIndex == -1)
        {
            ClearAbilityHover();
            return;
        }


        TMP_TextInfo textInfo =
            infoText.textInfo;


        if (
            linkIndex < 0 ||
            linkIndex >= textInfo.linkCount
        )
        {
            ClearAbilityHover();
            return;
        }


        TMP_LinkInfo link =
            textInfo.linkInfo[linkIndex];


        string linkId =
            link.GetLinkID();


        if (!linkId.StartsWith("ability_"))
        {
            ClearAbilityHover();
            return;
        }


        string indexString =
            linkId.Substring(
                "ability_".Length
            );


        if (!int.TryParse(
                indexString,
                out int abilityIndex))
        {
            ClearAbilityHover();
            return;
        }


        if (
            abilityIndex ==
            lastHoveredAbilityIndex
        )
        {
            return;
        }


        lastHoveredAbilityIndex =
            abilityIndex;


        // ========================================================
        // ONLY SHOW RANGE IF ABILITY CAN ACTUALLY BE USED
        // ========================================================

        ShowAbilityRange(
            abilityIndex
        );
    }


    // ============================================================
    // POINTER OVER ABILITY
    // ============================================================

    public bool IsPointerOverAbilityLink()
    {
        if (
            infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null
        )
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


        if (linkIndex < 0)
        {
            return false;
        }


        TMP_TextInfo textInfo =
            infoText.textInfo;


        if (linkIndex >= textInfo.linkCount)
        {
            return false;
        }


        TMP_LinkInfo link =
            textInfo.linkInfo[linkIndex];


        return link.GetLinkID()
            .StartsWith("ability_");
    }


    // ============================================================
    // SELECT ABILITY UNDER MOUSE
    // ============================================================

    public bool TrySelectAbilityUnderMouse()
    {
        if (
            infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null
        )
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


        if (linkIndex < 0)
        {
            return false;
        }


        TMP_TextInfo textInfo =
            infoText.textInfo;


        if (linkIndex >= textInfo.linkCount)
        {
            return false;
        }


        TMP_LinkInfo link =
            textInfo.linkInfo[linkIndex];


        string linkId =
            link.GetLinkID();


        if (!linkId.StartsWith("ability_"))
        {
            return false;
        }


        string indexString =
            linkId.Substring(
                "ability_".Length
            );


        if (!int.TryParse(
                indexString,
                out int abilityIndex))
        {
            return false;
        }


        SelectAbility(
            abilityIndex
        );


        return true;
    }


    // ============================================================
    // CHECK ABILITY CAN BE USED
    // ============================================================

    private bool CanUseSelectedAbility(
        AbilitySO ability)
    {
        if (ability == null)
        {
            return false;
        }


        if (UIManager.CurrentSelection == null)
        {
            return false;
        }


        AttackUnit attackUnit =
            UIManager.CurrentSelection.GetAttackUnit();


        if (attackUnit == null)
        {
            return false;
        }


        GameObject selectedObject =
            attackUnit.gameObject;


        // --------------------------------------------------------
        // MOVEMENT RESTRICTION
        // --------------------------------------------------------

        if (!ability.CanUseAfterMovement(
                selectedObject))
        {
            return false;
        }


        // --------------------------------------------------------
        // COOLDOWN
        // --------------------------------------------------------

        if (
            attackUnit.GetAbilityCooldown(
                ability
            ) > 0
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // USES PER TURN
        // --------------------------------------------------------

        int usesPerTurn =
            ability.GetUsesPerTurn();


        // 0 means unlimited.
        if (usesPerTurn > 0)
        {
            int usesRemaining =
                attackUnit.GetAbilityUsesRemaining(
                    ability
                );


            if (usesRemaining <= 0)
            {
                return false;
            }
        }


        return true;
    }


    // ============================================================
    // SELECT ABILITY
    // ============================================================

    private void SelectAbility(
        int abilityIndex)
    {
        if (UIManager.CurrentSelection == null)
        {
            return;
        }


        CharacterSO character =
            UIManager.CurrentSelection
                .GetCharacterData();


        if (character == null)
        {
            return;
        }


        List<AbilitySO> abilities =
            character.GetAbilities();


        if (
            abilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count
        )
        {
            return;
        }


        AbilitySO ability =
            abilities[abilityIndex];


        if (ability == null)
        {
            return;
        }


        // ========================================================
        // DO NOT SELECT AN UNUSABLE ABILITY
        // ========================================================

        if (!CanUseSelectedAbility(
                ability))
        {
            ClearAbilityHighlights();
            return;
        }


        selectedAbilityIndex =
            abilityIndex;


        selectedAbility =
            ability;


        ShowAbilityRange(
            abilityIndex
        );
    }


    // ============================================================
    // GET SELECTED
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
        {
            return null;
        }


        Canvas canvas =
            infoText.canvas;


        if (canvas == null)
        {
            return null;
        }


        if (
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            return null;
        }


        return canvas.worldCamera;
    }


    // ============================================================
    // CLEAR HOVER
    // ============================================================

    private void ClearAbilityHover()
    {
        lastHoveredAbilityIndex = -1;


        /*
         * IMPORTANT:
         *
         * If an ability is selected, hovering away from an
         * ability link must NOT clear its range.
         *
         * However, ShowAbilityRange() now checks whether the
         * selected ability is still usable.
         */

        if (selectedAbility == null)
        {
            ClearAbilityHighlights();
        }
        else
        {
            ShowAbilityRange(
                selectedAbilityIndex
            );
        }
    }


    // ============================================================
    // SHOW ABILITY RANGE
    // ============================================================

    private void ShowAbilityRange(
        int abilityIndex)
    {
        if (
            gridManager == null ||
            highlightManager == null
        )
        {
            return;
        }


        if (UIManager.CurrentSelection == null)
        {
            ClearAbilityHighlights();
            return;
        }


        CharacterSO character =
            UIManager.CurrentSelection
                .GetCharacterData();


        if (character == null)
        {
            ClearAbilityHighlights();
            return;
        }


        List<AbilitySO> abilities =
            character.GetAbilities();


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


        // ========================================================
        // ABILITY CANNOT BE USED
        // ========================================================

        if (!CanUseSelectedAbility(
                ability))
        {
            ClearAbilityHighlights();
            return;
        }


        // ========================================================
        // GET RANGE
        // ========================================================

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


        // ========================================================
        // HEAL ABILITY
        // ========================================================

        if (ability is HealAbilitySO)
        {
            highlightManager.ShowHealTiles(
                rangeTiles,
                selectedObject
            );


            return;
        }


        // ========================================================
        // NORMAL ABILITY
        // ========================================================

        highlightManager.ShowAbilityTiles(
            rangeTiles,
            selectedObject
        );
    }


    // ============================================================
    // CLEAR HIGHLIGHTS
    // ============================================================

    private void ClearAbilityHighlights()
    {
        if (highlightManager == null)
        {
            return;
        }


        highlightManager.ClearAbilityRange();
    }


    // ============================================================
    // CLEAR INFO
    // ============================================================

    public void ClearInfo()
    {
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;


        /*
         * IMPORTANT:
         *
         * ClearInfo is a true reset.
         *
         * This DOES cancel the selected ability.
         */

        selectedAbilityIndex = -1;
        selectedAbility = null;


        ClearAbilityHighlights();


        if (infoText != null)
        {
            infoText.text =
                string.Empty;
        }


        if (characterIcon != null)
        {
            characterIcon.sprite = null;
            characterIcon.enabled = false;
        }
    }


    // ============================================================
    // CLEAR SELECTED ABILITY
    // ============================================================

    public void ClearSelectedAbility()
    {
        selectedAbilityIndex = -1;
        selectedAbility = null;


        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;


        ClearAbilityHighlights();
    }
}