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

        if (linkIndex == lastLinkIndex)
        {
            return;
        }

        lastLinkIndex =
            linkIndex;

        // Mouse is no longer over a link.
        if (linkIndex == -1)
        {
            ClearAbilityHover();
            return;
        }

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

        if (!linkId.StartsWith(
                "ability_"))
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

        if (abilityIndex == lastHoveredAbilityIndex)
        {
            return;
        }

        lastHoveredAbilityIndex =
            abilityIndex;

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

    private void ShowAbilityRange(
        int abilityIndex)
    {
        if (gridManager == null ||
            highlightManager == null)
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

        if (abilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilities.Count)
        {
            ClearAbilityHighlights();
            return;
        }

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

        Vector2Int unitPosition =
            gridManager.WorldToGridPosition(
                selectedObject.transform.position
            );

        // ==================================================
        // YELLOW = ACTUAL ABILITY TARGET RANGE
        // ==================================================

        List<Vector2Int> hitbox =
            ability.GetHitboxTiles(
                gridManager,
                selectedObject
            );

        if (hitbox == null ||
            hitbox.Count == 0)
        {
            ClearAbilityHighlights();
            return;
        }

        Vector2Int facingDirection =
            GetFacingDirection(
                selectedObject
            );

        List<Vector2Int> orientedOffsets =
            new List<Vector2Int>(
                hitbox.Count
            );

        foreach (Vector2Int position in hitbox)
        {
            Vector2Int defaultOffset =
                position - unitPosition;

            Vector2Int rotatedOffset =
                RotateOffset(
                    defaultOffset,
                    facingDirection
                );

            orientedOffsets.Add(
                rotatedOffset
            );
        }

        highlightManager.ShowAbilityCells(
            unitPosition,
            orientedOffsets
        );
    }

    // ==================================================
    // FACING
    // ==================================================

    private Vector2Int GetFacingDirection(
        GameObject unit)
    {
        if (unit == null)
        {
            return Vector2Int.up;
        }

        Vector3 forward =
            unit.transform.up;

        if (Mathf.Abs(forward.x) >
            Mathf.Abs(forward.y))
        {
            return forward.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        return forward.y > 0
            ? Vector2Int.up
            : Vector2Int.down;
    }

    // ==================================================
    // ROTATE
    // ==================================================

    private Vector2Int RotateOffset(
        Vector2Int offset,
        Vector2Int direction)
    {
        if (direction ==
            Vector2Int.right)
        {
            return new Vector2Int(
                offset.y,
                -offset.x
            );
        }

        if (direction ==
            Vector2Int.left)
        {
            return new Vector2Int(
                -offset.y,
                offset.x
            );
        }

        if (direction ==
            Vector2Int.down)
        {
            return new Vector2Int(
                -offset.x,
                -offset.y
            );
        }

        return offset;
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