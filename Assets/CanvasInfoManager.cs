using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CanvasInfoManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text infoText;

    [SerializeField]
    private Image characterIcon;


    // ==================================================
    // HOVER STATE
    // ==================================================

    private int lastHoveredAbilityIndex = -1;

    private int lastLinkIndex = -1;


    // ==================================================
    // SHOW CHARACTER VIA INTERFACE
    // ==================================================

    public void ShowCharacter(
        ICharacterHolder characterHolder
    )
    {
        if (characterHolder == null)
        {
            Debug.Log(
                "[CanvasInfoManager] CharacterHolder is NULL."
            );

            ClearInfo();

            return;
        }

        Debug.Log(
            "[CanvasInfoManager] " +
            "Showing character from CharacterHolder."
        );

        ShowCharacter(
            characterHolder.GetCharacterData()
        );
    }


    // ==================================================
    // SHOW CHARACTER
    // ==================================================

    public void ShowCharacter(
        CharacterSO character
    )
    {
        if (character == null)
        {
            Debug.Log(
                "[CanvasInfoManager] Character is NULL."
            );

            ClearInfo();

            return;
        }


        // Reset hover state
        lastHoveredAbilityIndex = -1;
        lastLinkIndex = -1;


        string text = "";


        // ==================================================
        // CHARACTER INFO
        // ==================================================

        text +=
            $"<b>{character.characterName}</b>\n\n";

        text +=
            $"Team: {character.team}\n";

        text +=
            $"Health: {character.maxHealth}\n\n";


        // ==================================================
        // ABILITIES
        // ==================================================

        List<AbilitySO> abilities =
            character.GetAbilities();


        if (
            abilities != null &&
            abilities.Count > 0
        )
        {
            text +=
                "<b>Abilities</b>\n\n";


            for (
                int i = 0;
                i < abilities.Count;
                i++
            )
            {
                AbilitySO ability =
                    abilities[i];


                // ==================================================
                // NULL ABILITY
                // ==================================================

                if (ability == null)
                {
                    Debug.LogWarning(
                        $"[CanvasInfoManager] " +
                        $"Ability {i} is NULL."
                    );

                    continue;
                }


                // ==================================================
                // ABILITY NAME
                // ==================================================

                string abilityName =
                    ability.GetAbilityName();


                // ==================================================
                // CREATE TMP LINK
                // ==================================================

                text +=
                    $"<link=\"ability_{i}\">" +
                    $"<color=yellow>" +
                    $"<u>" +
                    $"<b>{abilityName}</b>" +
                    $"</u>" +
                    $"</color>" +
                    $"</link>\n";


                // ==================================================
                // DESCRIPTION
                // ==================================================

                text +=
                    $"{ability.GetDescription()}\n";


                // ==================================================
                // DAMAGE
                // ==================================================

                text +=
                    $"Damage: {ability.GetDamage()}\n";


                // ==================================================
                // RANGE
                // ==================================================

                text +=
                    $"Range: {ability.GetRange()}\n";


                // ==================================================
                // COOLDOWN
                // ==================================================

                text +=
                    $"Cooldown: {ability.GetCooldown()}\n\n";


                Debug.Log(
                    "[CanvasInfoManager] " +
                    $"Created link: ability_{i} " +
                    $"= {abilityName}"
                );
            }
        }
        else
        {
            text +=
                "<b>No Abilities</b>";
        }


        // ==================================================
        // APPLY TEXT
        // ==================================================

        if (infoText != null)
        {
            infoText.text = text;


            // Force TMP to rebuild
            infoText.ForceMeshUpdate();


            Debug.Log(
                "[CanvasInfoManager] " +
                $"TMP Link Count: " +
                $"{infoText.textInfo.linkCount}"
            );


            // ==================================================
            // DEBUG LINKS
            // ==================================================

            for (
                int i = 0;
                i < infoText.textInfo.linkCount;
                i++
            )
            {
                TMP_LinkInfo link =
                    infoText.textInfo.linkInfo[i];


                Debug.Log(
                    "[CanvasInfoManager] " +
                    $"TMP Link {i}: " +
                    $"ID='{link.GetLinkID()}' " +
                    $"Text='{link.GetLinkText()}'"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[CanvasInfoManager] " +
                "infoText is NULL."
            );
        }


        // ==================================================
        // CHARACTER ICON
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
        // --------------------------------------------------
        // Make sure text exists
        // --------------------------------------------------

        if (infoText == null)
        {
            return;
        }


        // --------------------------------------------------
        // Make sure text is visible
        // --------------------------------------------------

        if (!infoText.gameObject.activeInHierarchy)
        {
            return;
        }


        // --------------------------------------------------
        // Make sure mouse exists
        // --------------------------------------------------

        if (Mouse.current == null)
        {
            return;
        }


        // ==================================================
        // GET MOUSE POSITION
        // ==================================================

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // ==================================================
        // GET EVENT CAMERA
        // ==================================================

        Camera eventCamera = null;


        if (infoText.canvas != null)
        {
            Canvas canvas =
                infoText.canvas;


            // Screen Space Overlay does NOT use a camera
            if (
                canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
            )
            {
                eventCamera = null;
            }
            else
            {
                eventCamera =
                    canvas.worldCamera;
            }
        }


        // ==================================================
        // FIND LINK
        // ==================================================

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                infoText,
                mousePosition,
                eventCamera
            );


        // ==================================================
        // LINK CHANGED
        // ==================================================

        if (linkIndex != lastLinkIndex)
        {
            lastLinkIndex =
                linkIndex;


            // --------------------------------------------------
            // LEFT ALL LINKS
            // --------------------------------------------------

            if (linkIndex == -1)
            {
                if (
                    lastHoveredAbilityIndex != -1
                )
                {
                    Debug.Log(
                        "[CanvasInfoManager] " +
                        "MOUSE LEFT ABILITY."
                    );

                    lastHoveredAbilityIndex =
                        -1;
                }

                return;
            }


            // --------------------------------------------------
            // LINK FOUND
            // --------------------------------------------------

            if (
                linkIndex >=
                infoText.textInfo.linkCount
            )
            {
                return;
            }


            TMP_LinkInfo link =
                infoText.textInfo.linkInfo[
                    linkIndex
                ];


            string linkId =
                link.GetLinkID();


            string linkText =
                link.GetLinkText();


            Debug.Log(
                "[CanvasInfoManager] " +
                $"TMP LINK DETECTED: {linkIndex}"
            );


            Debug.Log(
                "[CanvasInfoManager] " +
                $"Link ID: {linkId}"
            );


            Debug.Log(
                "[CanvasInfoManager] " +
                $"Link Text: {linkText}"
            );


            // ==================================================
            // CHECK ABILITY LINK
            // ==================================================

            if (
                !linkId.StartsWith(
                    "ability_"
                )
            )
            {
                return;
            }


            // ==================================================
            // GET ABILITY INDEX
            // ==================================================

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
                return;
            }


            // ==================================================
            // NEW ABILITY
            // ==================================================

            if (
                abilityIndex !=
                lastHoveredAbilityIndex
            )
            {
                lastHoveredAbilityIndex =
                    abilityIndex;


                Debug.Log(
                    "========================================"
                );


                Debug.Log(
                    "[CanvasInfoManager] " +
                    "HOVERING ABILITY!"
                );


                Debug.Log(
                    "[CanvasInfoManager] " +
                    $"Ability Index: " +
                    $"{abilityIndex}"
                );


                Debug.Log(
                    "[CanvasInfoManager] " +
                    $"Ability Name: " +
                    $"{linkText}"
                );


                Debug.Log(
                    "========================================"
                );
            }
        }
    }


    // ==================================================
    // CLEAR INFO
    // ==================================================

    public void ClearInfo()
    {
        lastHoveredAbilityIndex = -1;

        lastLinkIndex = -1;


        // ==================================================
        // CLEAR TEXT
        // ==================================================

        if (infoText != null)
        {
            infoText.text = "";
        }


        // ==================================================
        // CLEAR ICON
        // ==================================================

        if (characterIcon != null)
        {
            characterIcon.sprite = null;

            characterIcon.enabled = false;
        }


        Debug.Log(
            "[CanvasInfoManager] " +
            "Info cleared."
        );
    }
}