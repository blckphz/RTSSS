using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CanvasInfoManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image characterIcon;

    [Header("Grid Highlighting")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridHighlightManager highlightManager;

    private int lastHoveredAbilityIndex = -1;
    private int lastLinkIndex = -1;

    private readonly StringBuilder textBuilder =
        new StringBuilder();


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        SetupReferences();
    }

    private void Update()
    {
        CheckAbilityHover();
    }


    // ==================================================
    // SETUP
    // ==================================================

    private void SetupReferences()
    {
        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (highlightManager == null &&
            gridManager != null)
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


    // ==================================================
    // SHOW CHARACTER
    // ==================================================

    public void ShowCharacter(
        ICharacterHolder characterHolder)
    {
        if (characterHolder == null)
        {
            ClearInfo();
            return;
        }

        ShowCharacter(
            characterHolder.GetCharacterData()
        );
    }


    // ==================================================

    public void ShowCharacter(
        CharacterSO character)
    {
        if (character == null)
        {
            ClearInfo();
            return;
        }

        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

        ClearAbilityHighlights();

        textBuilder.Clear();


        // ==================================================
        // CHARACTER
        // ==================================================

        textBuilder.AppendLine(
            $"<b>{character.characterName}</b>\n"
        );

        textBuilder.AppendLine(
            $"Team: {character.team}"
        );

        textBuilder.AppendLine(
            $"Health: {character.maxHealth}\n"
        );


        // ==================================================
        // ABILITIES
        // ==================================================

        List<AbilitySO> abilities =
            character.GetAbilities();

        if (abilities != null &&
            abilities.Count > 0)
        {
            textBuilder.AppendLine(
                "<b>Abilities</b>\n"
            );

            for (int i = 0;
                 i < abilities.Count;
                 i++)
            {
                AbilitySO ability =
                    abilities[i];

                if (ability == null)
                {
                    continue;
                }

                string abilityName =
                    ability.GetAbilityName();

                textBuilder.AppendLine(
                    $"<link=\"ability_{i}\">" +
                    $"<color=yellow>" +
                    $"<u><b>{abilityName}</b></u>" +
                    $"</color></link>"
                );

                textBuilder.AppendLine(
                    ability.GetDescription()
                );

                textBuilder.AppendLine(
                    $"Damage: {ability.GetDamage()}"
                );

                textBuilder.AppendLine(
                    $"Range: {ability.GetRange()}"
                );

                textBuilder.AppendLine(
                    $"Shape: {ability.GetRangeShape()}"
                );

                textBuilder.AppendLine(
                    $"Cooldown: {ability.GetCooldown()}\n"
                );
            }
        }
        else
        {
            textBuilder.Append(
                "<b>No Abilities</b>"
            );
        }


        // ==================================================
        // TEXT
        // ==================================================

        if (infoText != null)
        {
            infoText.text =
                textBuilder.ToString();

            infoText.ForceMeshUpdate();
        }


        // ==================================================
        // ICON
        // ==================================================

        if (characterIcon != null)
        {
            characterIcon.sprite =
                character.icon;

            characterIcon.enabled =
                character.icon != null;
        }
    }


    // ==================================================
    // ABILITY HOVER
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

        Camera eventCamera =
            GetEventCamera();

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                eventCamera
            );


        // ==================================================
        // NOTHING CHANGED
        // ==================================================

        if (linkIndex == lastLinkIndex)
        {
            return;
        }

        lastLinkIndex =
            linkIndex;


        // ==================================================
        // MOUSE IS NO LONGER OVER A LINK
        // ==================================================

        if (linkIndex == -1)
        {
            ClearAbilityHover();
            return;
        }


        // ==================================================
        // VALIDATE LINK
        // ==================================================

        TMP_TextInfo textInfo =
            infoText.textInfo;

        if (linkIndex < 0 ||
            linkIndex >= textInfo.linkCount)
        {
            ClearAbilityHover();
            return;
        }

        TMP_LinkInfo link =
            textInfo.linkInfo[linkIndex];

        string linkId =
            link.GetLinkID();


        // ==================================================
        // ONLY ABILITY LINKS
        // ==================================================

        if (!linkId.StartsWith(
                "ability_"))
        {
            ClearAbilityHover();
            return;
        }


        // ==================================================
        // GET ABILITY INDEX
        // ==================================================

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


        // ==================================================
        // SAME ABILITY
        // ==================================================

        if (abilityIndex == lastHoveredAbilityIndex)
        {
            return;
        }

        lastHoveredAbilityIndex =
            abilityIndex;


        // ==================================================
        // SHOW RANGE
        // ==================================================

        ShowAbilityRange(
            abilityIndex
        );
    }


    // ==================================================
    // EVENT CAMERA
    // ==================================================

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

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }


    // ==================================================
    // CLEAR HOVER
    // ==================================================

    private void ClearAbilityHover()
    {
        lastHoveredAbilityIndex = -1;

        ClearAbilityHighlights();
    }


    // ==================================================
    // SHOW ABILITY RANGE
    // ==================================================
    //
    // This is the TARGETING RANGE preview.
    //
    // It intentionally uses:
    //
    //     GetRangeTiles()
    //
    // NOT:
    //
    //     GetHitboxTiles()
    //
    // Therefore FrontAttack displays its FULL BOX.
    //
    // Range 1:
    //
    // XXX
    // XOX
    // XXX
    //
    // Range 2:
    //
    // XXXXX
    // XXXXX
    // XXOXX
    // XXXXX
    // XXXXX
    //
    // Range 4:
    //
    // XXXXXXXXX
    // XXXXXXXXX
    // XXXXXXXXX
    // XXXXXXXXX
    // XXXXOXXXX
    // XXXXXXXXX
    // XXXXXXXXX
    // XXXXXXXXX
    // XXXXXXXXX
    //
    // The character's facing direction does NOT affect
    // this preview.
    //
    // The actual FrontAttack hitbox is handled separately
    // by FrontAttack.GetHitboxTiles().
    // ==================================================

    private void ShowAbilityRange(
        int abilityIndex)
    {
        if (gridManager == null ||
            highlightManager == null)
        {
            return;
        }


        // ==================================================
        // GET CURRENTLY SELECTED CHARACTER
        // ==================================================

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


        // ==================================================
        // GET ABILITIES
        // ==================================================

        List<AbilitySO> abilities =
            character.GetAbilities();

        if (abilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count)
        {
            ClearAbilityHighlights();
            return;
        }


        // ==================================================
        // GET ABILITY
        // ==================================================

        AbilitySO ability =
            abilities[abilityIndex];

        GameObject selectedObject =
            UIManager.CurrentSelection.gameObject;

        if (ability == null ||
            selectedObject == null)
        {
            ClearAbilityHighlights();
            return;
        }


        // ==================================================
        // GET TARGETING RANGE
        // ==================================================
        //
        // GetRangeTiles() returns the actual targeting
        // range defined by the AbilitySO.
        //
        // FrontAttack overrides this and returns a FULL BOX.
        //
        // We do NOT call GetHitboxTiles() here.
        // ==================================================

        List<Vector2Int> rangeTiles =
            ability.GetRangeTiles(
                gridManager,
                selectedObject
            );

        if (rangeTiles == null ||
            rangeTiles.Count == 0)
        {
            ClearAbilityHighlights();
            return;
        }


        // ==================================================
        // SHOW ABSOLUTE GRID POSITIONS
        // ==================================================
        //
        // GetRangeTiles() already returns absolute grid
        // positions, so there is no need to rotate or
        // offset anything.
        //
        // IMPORTANT:
        //
        // Pass selectedObject as the USER/CASTER.
        //
        // GridHighlightManager needs this object to
        // compare the caster's team against the target's
        // team.
        // ==================================================

        highlightManager.ShowAbilityTiles(
            rangeTiles,
            selectedObject
        );
    }


    // ==================================================
    // CLEAR HIGHLIGHTS
    // ==================================================

    private void ClearAbilityHighlights()
    {
        if (highlightManager == null)
        {
            return;
        }

        highlightManager.ClearAbilityRange();
    }


    // ==================================================
    // CLEAR INFO
    // ==================================================

    public void ClearInfo()
    {
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;

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
}