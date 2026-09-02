using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class IconBehav :
    MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // =========================================================
    // ENCOUNTER
    // =========================================================

    [Header("Encounter")]
    [SerializeField]
    private EncounterDefinition encounter;

    [SerializeField]
    private EncounterManager encounterManager;


    // =========================================================
    // MAP
    // =========================================================

    [Header("Map")]
    [SerializeField]
    private LevelMapManager mapManager;


    // =========================================================
    // NODE STATE
    // =========================================================

    [Header("Node State")]
    [SerializeField]
    private bool isUnlocked;

    [SerializeField]
    private bool isCompleted;


    // =========================================================
    // VISUALS
    // =========================================================

    [Header("Visuals")]
    [SerializeField]
    private GameObject lockedVisual;

    [SerializeField]
    private GameObject unlockedVisual;

    [SerializeField]
    private GameObject completedVisual;


    // =========================================================
    // HOVER INFO
    // =========================================================

    [Header("Hover Info")]
    private TMP_Text hoverInfoText;


    // =========================================================
    // HOVER ANIMATION
    // =========================================================

    [Header("Hover Animation")]
    [SerializeField]
    private float hoverScale = 1.15f;

    [SerializeField]
    private float hoverSpeed = 8f;


    // =========================================================
    // CLICK ANIMATION
    // =========================================================

    [Header("Click Animation")]
    [SerializeField]
    private float clickScale = 1.3f;

    [SerializeField]
    private float clickDuration = 0.12f;


    // =========================================================
    // TRANSITION
    // =========================================================

    [Header("Transition")]
    [SerializeField]
    private transitionGameManager transitionManager;


    // =========================================================
    // INTERNAL
    // =========================================================

    private bool clickedThisFrame;

    private Vector3 originalScale;

    private Coroutine scaleCoroutine;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Save the original scale of the icon.
        originalScale = transform.localScale;


        // -----------------------------------------------------
        // FIND MANAGERS
        // -----------------------------------------------------

        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        // -----------------------------------------------------
        // FIND LEVEL DESCRIPTION
        // -----------------------------------------------------

        FindLevelDescription();


        // -----------------------------------------------------
        // REFRESH NODE VISUAL
        // -----------------------------------------------------

        RefreshVisuals();


        // -----------------------------------------------------
        // HIDE DESCRIPTION
        // -----------------------------------------------------

        HideHoverInfo();
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        clickedThisFrame = false;
    }


    // =========================================================
    // POINTER CLICK
    // =========================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        StartLevel();
    }


    // =========================================================
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show encounter information.
        ShowHoverInfo();


        // Scale icon up.
        StartScaleAnimation(
            originalScale * hoverScale
        );


        // Play map node hover sound.
        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance.PlayMapNodeHover();
        }
    }


    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide encounter information.
        HideHoverInfo();


        // Return icon to normal size.
        StartScaleAnimation(
            originalScale
        );
    }


    // =========================================================
    // FIND LEVEL DESCRIPTION
    // =========================================================

    private void FindLevelDescription()
    {
        GameObject levelDesc =
            GameObject.Find("LevelDesc");


        if (levelDesc == null)
        {
            Debug.LogError(
                "[IconBehav] Could not find GameObject named 'LevelDesc'."
            );

            return;
        }


        // Try to find TMP directly on LevelDesc.
        hoverInfoText =
            levelDesc.GetComponent<TMP_Text>();


        // If it isn't directly on LevelDesc,
        // search its children.
        if (hoverInfoText == null)
        {
            hoverInfoText =
                levelDesc.GetComponentInChildren<TMP_Text>(true);
        }


        if (hoverInfoText == null)
        {
            Debug.LogError(
                "[IconBehav] Could not find a TMP_Text component " +
                "on or inside LevelDesc.",
                levelDesc
            );
        }
    }


    // =========================================================
    // SHOW HOVER INFO
    // =========================================================

    private void ShowHoverInfo()
    {
        if (hoverInfoText == null)
        {
            return;
        }


        if (encounter == null)
        {
            return;
        }


        string objective =
            GetObjectiveText();


        hoverInfoText.text =
            $"<b>{encounter.encounterName}</b>\n\n" +
            $"{encounter.description}\n\n" +
            $"<b>Objective:</b> {objective}";


        // Enable TMP instead of disabling LevelDesc.
        //
        // This is important because GameObject.Find()
        // only finds active GameObjects.
        hoverInfoText.enabled = true;
    }


    // =========================================================
    // HIDE HOVER INFO
    // =========================================================

    private void HideHoverInfo()
    {
        if (hoverInfoText != null)
        {
            // Do NOT disable the LevelDesc GameObject.
            // Only disable the TMP component.
            hoverInfoText.enabled = false;
        }
    }


    // =========================================================
    // OBJECTIVE TEXT
    // =========================================================

    private string GetObjectiveText()
    {
        if (encounter == null)
        {
            return string.Empty;
        }


        switch (encounter.victoryCondition)
        {
            case VictoryCondition.DefeatAllEnemies:

                return "Defeat all enemies.";


            case VictoryCondition.SurviveRounds:

                return
                    $"Survive {encounter.roundsToSurvive} rounds.";


            case VictoryCondition.DefeatSpecificEnemy:

                if (string.IsNullOrWhiteSpace(
                    encounter.targetEnemyId))
                {
                    return "Defeat the target enemy.";
                }


                return
                    $"Defeat {encounter.targetEnemyId}.";


            default:

                return "Unknown objective.";
        }
    }


    // =========================================================
    // SCALE ANIMATION
    // =========================================================

    private void StartScaleAnimation(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }


        scaleCoroutine =
            StartCoroutine(
                ScaleTo(targetScale)
            );
    }


    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        Vector3 startScale =
            transform.localScale;


        float time = 0f;


        while (time < 1f)
        {
            time +=
                Time.unscaledDeltaTime *
                hoverSpeed;


            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    time
                );


            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    t
                );


            yield return null;
        }


        transform.localScale =
            targetScale;


        scaleCoroutine = null;
    }


    // =========================================================
    // CLICK POP
    // =========================================================

    private IEnumerator ClickPop()
    {
        // Stop hover animation.
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }


        Vector3 startScale =
            transform.localScale;


        Vector3 bigScale =
            originalScale * clickScale;


        Vector3 smallScale =
            originalScale * 0.9f;


        // -----------------------------------------------------
        // GROW
        // -----------------------------------------------------

        float time = 0f;


        while (time < 1f)
        {
            time +=
                Time.unscaledDeltaTime /
                clickDuration;


            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    time
                );


            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    bigScale,
                    t
                );


            yield return null;
        }


        // -----------------------------------------------------
        // SHRINK
        // -----------------------------------------------------

        time = 0f;


        while (time < 1f)
        {
            time +=
                Time.unscaledDeltaTime /
                clickDuration;


            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    time
                );


            transform.localScale =
                Vector3.Lerp(
                    bigScale,
                    smallScale,
                    t
                );


            yield return null;
        }


        // -----------------------------------------------------
        // RETURN TO NORMAL
        // -----------------------------------------------------

        time = 0f;


        while (time < 1f)
        {
            time +=
                Time.unscaledDeltaTime /
                clickDuration;


            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    time
                );


            transform.localScale =
                Vector3.Lerp(
                    smallScale,
                    originalScale,
                    t
                );


            yield return null;
        }


        transform.localScale =
            originalScale;
    }


    // =========================================================
    // MOUSE DOWN
    // =========================================================

    private void OnMouseDown()
    {
        StartLevel();
    }


    // =========================================================
    // START LEVEL
    // =========================================================

    public void StartLevel()
    {
        if (clickedThisFrame)
        {
            return;
        }


        clickedThisFrame = true;


        // -----------------------------------------------------
        // LOCKED CHECK
        // -----------------------------------------------------

        if (!isUnlocked)
        {
            Debug.Log(
                "[IconBehav] Node is locked.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // COMPLETED CHECK
        // -----------------------------------------------------

        if (isCompleted)
        {
            Debug.Log(
                "[IconBehav] Node is already completed.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // ENCOUNTER CHECK
        // -----------------------------------------------------

        if (encounter == null)
        {
            Debug.LogError(
                "[IconBehav] No EncounterDefinition assigned!",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // CLICK POP
        // -----------------------------------------------------

        StartCoroutine(
            ClickPop()
        );


        // -----------------------------------------------------
        // LEVEL ENTER SOUND
        // -----------------------------------------------------

        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance.PlayLevelEnter();
        }


        // -----------------------------------------------------
        // ENCOUNTER MANAGER
        // -----------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (encounterManager == null)
        {
            Debug.LogError(
                "[IconBehav] EncounterManager was not found in the scene!",
                this
            );

            return;
        }


        // Set current encounter.
        encounterManager.SetCurrentEncounter(
            encounter
        );


        // -----------------------------------------------------
        // TRANSITION MANAGER
        // -----------------------------------------------------

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        if (transitionManager == null)
        {
            Debug.LogError(
                "[IconBehav] transitionGameManager was not found in the scene!",
                this
            );

            return;
        }


        // Start level transition.
        transitionManager.TransitionPeakProcess();
    }


    // =========================================================
    // SET ENCOUNTER
    // =========================================================

    public void SetEncounter(
        EncounterDefinition newEncounter)
    {
        encounter = newEncounter;
    }


    // =========================================================
    // GET ENCOUNTER
    // =========================================================

    public EncounterDefinition GetEncounter()
    {
        return encounter;
    }


    // =========================================================
    // SET ENCOUNTER MANAGER
    // =========================================================

    public void SetEncounterManager(
        EncounterManager newEncounterManager)
    {
        encounterManager =
            newEncounterManager;
    }


    // =========================================================
    // SET MAP MANAGER
    // =========================================================

    public void SetMapManager(
        LevelMapManager newMapManager)
    {
        mapManager =
            newMapManager;
    }


    // =========================================================
    // SET NODE STATE
    // =========================================================

    public void SetNodeState(
        bool unlocked,
        bool completed)
    {
        isUnlocked =
            unlocked;

        isCompleted =
            completed;


        RefreshVisuals();
    }


    // =========================================================
    // REFRESH VISUALS
    // =========================================================

    private void RefreshVisuals()
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(
                !isUnlocked &&
                !isCompleted
            );
        }


        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(
                isUnlocked &&
                !isCompleted
            );
        }


        if (completedVisual != null)
        {
            completedVisual.SetActive(
                isCompleted
            );
        }
    }


    // =========================================================
    // STATE GETTERS
    // =========================================================

    public bool IsUnlocked()
    {
        return isUnlocked;
    }


    public bool IsCompleted()
    {
        return isCompleted;
    }
}