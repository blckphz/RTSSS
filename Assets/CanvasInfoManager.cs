using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CanvasInfoManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image characterIcon;

    [Header("Ability Range")]
    [SerializeField] private GridManager gridManager;

    // Hover State
    private int lastHoveredAbilityIndex = -1;
    private int lastLinkIndex = -1;

    // Cached StringBuilder to prevent GC allocations on UI rebuilds
    private readonly StringBuilder textBuilder = new StringBuilder();

    // ==================================================
    // SHOW CHARACTER VIA INTERFACE
    // ==================================================

    public void ShowCharacter(ICharacterHolder characterHolder)
    {
        if (characterHolder == null)
        {
            ClearInfo();
            return;
        }

        ShowCharacter(characterHolder.GetCharacterData());
    }

    // ==================================================
    // SHOW CHARACTER
    // ==================================================

    public void ShowCharacter(CharacterSO character)
    {
        if (character == null)
        {
            ClearInfo();
            return;
        }

        // Reset hover state
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        // Clear any previous ability range
        if (gridManager != null)
        {
            gridManager.ClearAbilityRange();
        }

        textBuilder.Clear();

        // ==================================================
        // CHARACTER INFO
        // ==================================================

        textBuilder.AppendLine($"<b>{character.characterName}</b>\n");
        textBuilder.AppendLine($"Team: {character.team}");
        textBuilder.AppendLine($"Health: {character.maxHealth}\n");

        // ==================================================
        // ABILITIES
        // ==================================================

        List<AbilitySO> abilities = character.GetAbilities();

        if (abilities != null && abilities.Count > 0)
        {
            textBuilder.AppendLine("<b>Abilities</b>\n");

            for (int i = 0; i < abilities.Count; i++)
            {
                AbilitySO ability = abilities[i];

                if (ability == null)
                    continue;

                string abilityName = ability.GetAbilityName();

                // Ability clickable link
                textBuilder.AppendLine(
                    $"<link=\"ability_{i}\"><color=yellow><u><b>{abilityName}</b></u></color></link>"
                );

                textBuilder.AppendLine(
                    $"{ability.GetDescription()}"
                );

                textBuilder.AppendLine(
                    $"Damage: {ability.GetDamage()}"
                );

                textBuilder.AppendLine(
                    $"Range: {ability.GetRange()}"
                );

                textBuilder.AppendLine(
                    $"Cooldown: {ability.GetCooldown()}\n"
                );
            }
        }
        else
        {
            textBuilder.Append("<b>No Abilities</b>");
        }

        // ==================================================
        // APPLY TEXT
        // ==================================================

        if (infoText != null)
        {
            infoText.text = textBuilder.ToString();
            infoText.ForceMeshUpdate();
        }

        // ==================================================
        // CHARACTER ICON
        // ==================================================

        if (characterIcon != null)
        {
            characterIcon.sprite = character.icon;
            characterIcon.enabled = character.icon != null;
        }
    }

    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        CheckAbilityHover();
    }

    // ==================================================
    // CHECK ABILITY HOVER
    // ==================================================

    private void CheckAbilityHover()
    {
        if (infoText == null ||
            !infoText.gameObject.activeInHierarchy ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        // Determine camera based on Canvas Render Mode
        Camera eventCamera = null;

        Canvas canvas = infoText.canvas;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        // Find intersecting link
        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                eventCamera
            );

        // Nothing changed
        if (linkIndex == lastLinkIndex)
        {
            return;
        }

        lastLinkIndex = linkIndex;

        // ==================================================
        // LEFT ALL LINKS
        // ==================================================

        if (linkIndex == -1)
        {
            lastHoveredAbilityIndex = -1;

            ClearAbilityRange();

            return;
        }

        // ==================================================
        // VALIDATE LINK INDEX
        // ==================================================

        TMP_TextInfo textInfo = infoText.textInfo;

        if (linkIndex >= textInfo.linkCount)
        {
            ClearAbilityRange();
            return;
        }

        // ==================================================
        // GET LINK
        // ==================================================

        TMP_LinkInfo link =
            textInfo.linkInfo[linkIndex];

        string linkId =
            link.GetLinkID();

        // ==================================================
        // ONLY HANDLE ABILITY LINKS
        // ==================================================

        if (!linkId.StartsWith("ability_"))
        {
            lastHoveredAbilityIndex = -1;

            ClearAbilityRange();

            return;
        }

        // ==================================================
        // PARSE ABILITY INDEX
        // ==================================================

        if (int.TryParse(
            linkId.Substring(8),
            out int abilityIndex))
        {
            if (abilityIndex != lastHoveredAbilityIndex)
            {
                lastHoveredAbilityIndex = abilityIndex;

                ShowAbilityRange(abilityIndex);
            }
        }
    }

    // ==================================================
    // SHOW ABILITY RANGE
    // ==================================================

    private void ShowAbilityRange(int abilityIndex)
    {
        if (gridManager == null)
        {
            Debug.LogWarning(
                "[CanvasInfoManager] GridManager is not assigned!",
                this
            );

            return;
        }

        // Need a selected object
        if (UIManager.CurrentSelection == null)
        {
            gridManager.ClearAbilityRange();
            return;
        }

        // Get selected character data
        CharacterSO character =
            UIManager.CurrentSelection.GetCharacterData();

        if (character == null)
        {
            gridManager.ClearAbilityRange();
            return;
        }

        // Get abilities
        List<AbilitySO> abilities =
            character.GetAbilities();

        if (abilities == null)
        {
            gridManager.ClearAbilityRange();
            return;
        }

        // Validate index
        if (abilityIndex < 0 ||
            abilityIndex >= abilities.Count)
        {
            gridManager.ClearAbilityRange();
            return;
        }

        AbilitySO ability =
            abilities[abilityIndex];

        if (ability == null)
        {
            gridManager.ClearAbilityRange();
            return;
        }

        // ==================================================
        // GET UNIT GRID POSITION
        // ==================================================

        GameObject selectedObject =
            UIManager.CurrentSelection.gameObject;

        Vector2Int unitPosition =
            gridManager.WorldToGridPosition(
                selectedObject.transform.position
            );

        // ==================================================
        // GET ABILITY RANGE
        // ==================================================

        int range =
            ability.GetRange();

        // ==================================================
        // SHOW RANGE ON GRID
        // ==================================================

        gridManager.ShowAbilityRange(
            unitPosition,
            range
        );
    }

    // ==================================================
    // CLEAR ABILITY RANGE
    // ==================================================

    private void ClearAbilityRange()
    {
        if (gridManager != null)
        {
            gridManager.ClearAbilityRange();
        }
    }

    // ==================================================
    // CLEAR INFO
    // ==================================================

    public void ClearInfo()
    {
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        // Clear range
        if (gridManager != null)
        {
            gridManager.ClearAbilityRange();
        }

        // Clear text
        if (infoText != null)
        {
            infoText.text = string.Empty;
        }

        // Clear icon
        if (characterIcon != null)
        {
            characterIcon.sprite = null;
            characterIcon.enabled = false;
        }
    }
}