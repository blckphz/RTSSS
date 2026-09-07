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
    // ============================================================
    // ENCOUNTER
    // ============================================================

    [Header("Encounter")]
    [SerializeField]
    private EncounterDefinition encounter;

    [SerializeField]
    private EncounterManager encounterManager;


    // ============================================================
    // MAP
    // ============================================================

    [Header("Map")]
    [SerializeField]
    private LevelMapManager mapManager;


    // ============================================================
    // NODE STATE
    // ============================================================

    [Header("Node State")]
    [SerializeField]
    private bool isUnlocked;

    [SerializeField]
    private bool isCompleted;


    // ============================================================
    // VISUALS
    // ============================================================

    [Header("Visuals")]
    [SerializeField]
    private GameObject lockedVisual;

    [SerializeField]
    private GameObject unlockedVisual;

    [SerializeField]
    private GameObject completedVisual;


    // ============================================================
    // HOVER INFO
    // ============================================================

    [Header("Hover Info")]
    private TMP_Text hoverInfoText;


    // ============================================================
    // HOVER ANIMATION
    // ============================================================

    [Header("Hover Animation")]
    [SerializeField]
    private float hoverScale = 1.15f;

    [SerializeField]
    private float hoverSpeed = 8f;


    // ============================================================
    // CLICK ANIMATION
    // ============================================================

    [Header("Click Animation")]
    [SerializeField]
    private float clickScale = 1.3f;

    [SerializeField]
    private float clickDuration = 0.12f;


    // ============================================================
    // TRANSITION
    // ============================================================

    [Header("Transition")]
    [SerializeField]
    private transitionGameManager transitionManager;


    // ============================================================
    // AUDIO
    // ============================================================

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip clickSound;


    // ============================================================
    // INTERNAL
    // ============================================================

    private bool clickedThisFrame;

    private Vector3 originalScale;

    private Coroutine scaleCoroutine;

    private bool isHovering;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // Save the original icon scale.
        originalScale = transform.localScale;


        // --------------------------------------------------------
        // FIND ENCOUNTER MANAGER
        // --------------------------------------------------------

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        // --------------------------------------------------------
        // FIND MAP MANAGER
        // --------------------------------------------------------

        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        // --------------------------------------------------------
        // FIND TRANSITION MANAGER
        // --------------------------------------------------------

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        // --------------------------------------------------------
        // FIND AUDIO SOURCE
        // --------------------------------------------------------

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // Managers may have been spawned after Awake,
        // so try again if necessary.

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        // Find the LevelDesc TMP.
        FindLevelDescription();


        // Update locked/unlocked/completed visuals.
        RefreshVisuals();


        // Hide hover description at startup.
        HideHoverInfo();
    }


    // ============================================================
    // POINTER CLICK
    // ============================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        StartLevel();
    }


    // ============================================================
    // MOUSE CLICK
    // ============================================================

    private void OnMouseDown()
    {
        StartLevel();
    }


    // ============================================================
    // POINTER ENTER
    // ============================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovering = true;


        // --------------------------------------------------------
        // SHOW DESCRIPTION
        // --------------------------------------------------------

        ShowHoverInfo();


        // --------------------------------------------------------
        // HOVER SCALE
        // --------------------------------------------------------

        StartScaleAnimation(
            originalScale * hoverScale
        );


        // --------------------------------------------------------
        // HOVER SOUND
        // --------------------------------------------------------

        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance.PlayMapNodeHover();
        }
    }


    // ============================================================
    // POINTER EXIT
    // ============================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovering = false;


        // --------------------------------------------------------
        // HIDE DESCRIPTION
        // --------------------------------------------------------

        HideHoverInfo();


        // --------------------------------------------------------
        // RETURN TO NORMAL SCALE
        // --------------------------------------------------------

        StartScaleAnimation(
            originalScale
        );
    }


    // ============================================================
    // START LEVEL
    // ============================================================

    public void StartLevel()
    {
        // Prevent OnPointerClick + OnMouseDown
        // from triggering twice.

        if (clickedThisFrame)
        {
            return;
        }


        clickedThisFrame = true;


        CancelInvoke(
            nameof(ResetClickGuard)
        );


        Invoke(
            nameof(ResetClickGuard),
            0.15f
        );


        // ========================================================
        // CHECK UNLOCKED
        // ========================================================

        if (!isUnlocked)
        {
            Debug.Log(
                "[IconBehav] Node is locked.",
                this
            );

            return;
        }


        // ========================================================
        // CHECK COMPLETED
        // ========================================================

        if (isCompleted)
        {
            Debug.Log(
                "[IconBehav] Node is already completed.",
                this
            );

            return;
        }


        // ========================================================
        // CHECK ENCOUNTER
        // ========================================================

        if (encounter == null)
        {
            Debug.LogError(
                "[IconBehav] Encounter is missing!",
                this
            );

            return;
        }


        // ========================================================
        // FIND ENCOUNTER MANAGER
        // ========================================================

        if (encounterManager == null)
        {
            encounterManager =
                FindFirstObjectByType<EncounterManager>();
        }


        if (encounterManager == null)
        {
            Debug.LogError(
                "[IconBehav] EncounterManager was not found!",
                this
            );

            return;
        }


        // ========================================================
        // MAKE SURE NO ENCOUNTER IS RUNNING
        // ========================================================

        if (encounterManager.IsEncounterRunning())
        {
            Debug.LogWarning(
                "[IconBehav] An encounter is already running.",
                this
            );

            return;
        }


        // ========================================================
        // FIND MAP MANAGER
        // ========================================================

        if (mapManager == null)
        {
            mapManager =
                FindFirstObjectByType<LevelMapManager>();
        }


        if (mapManager == null)
        {
            Debug.LogError(
                "[IconBehav] LevelMapManager was not found!",
                this
            );

            return;
        }


        // ========================================================
        // SELECT NODE
        // ========================================================
        //
        // IMPORTANT:
        //
        // SetCurrentNode() is VOID in your current
        // LevelMapManager.
        //
        // It also calls SelectRoute().
        //
        // So if we have:
        //
        //        B       C
        //         \     /
        //           A
        //
        // and choose B:
        //
        //        B       C
        //        🔓      🔒
        //
        // C becomes locked.
        //
        // ========================================================

        mapManager.SetCurrentNode(this);


        // ========================================================
        // SET CURRENT ENCOUNTER
        // ========================================================

        encounterManager.SetCurrentEncounter(
            encounter
        );


        // ========================================================
        // CLICK SOUND
        // ========================================================

        PlayClickSound();


        // ========================================================
        // CLICK POP
        // ========================================================

        StartCoroutine(
            ClickPop()
        );


        // ========================================================
        // FIND TRANSITION MANAGER
        // ========================================================

        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }


        if (transitionManager == null)
        {
            Debug.LogError(
                "[IconBehav] transitionGameManager was not found!",
                this
            );

            return;
        }


        // ========================================================
        // LEVEL ENTER SOUND
        // ========================================================

        if (AudioFXManager.Instance != null)
        {
            AudioFXManager.Instance.PlayLevelEnter();
        }


        // ========================================================
        // DEBUG
        // ========================================================

        Debug.Log(
            $"[IconBehav] Starting encounter: {encounter.encounterName}",
            this
        );


        // ========================================================
        // START TRANSITION
        // ========================================================

        transitionManager.TransitionPeakProcess();
    }


    // ============================================================
    // CLICK GUARD RESET
    // ============================================================

    private void ResetClickGuard()
    {
        clickedThisFrame = false;
    }


    // ============================================================
    // HOVER SCALE ANIMATION
    // ============================================================

    private void StartScaleAnimation(
        Vector3 targetScale)
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


    // ============================================================
    // SCALE TO TARGET
    // ============================================================

    private IEnumerator ScaleTo(
        Vector3 targetScale)
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


    // ============================================================
    // CLICK POP ANIMATION
    // ============================================================

    private IEnumerator ClickPop()
    {
        // Stop hover animation.
        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        // --------------------------------------------------------
        // START SCALE
        // --------------------------------------------------------

        Vector3 startScale =
            transform.localScale;


        // --------------------------------------------------------
        // BIG SCALE
        // --------------------------------------------------------

        Vector3 bigScale =
            originalScale *
            clickScale;


        // --------------------------------------------------------
        // SMALL SCALE
        // --------------------------------------------------------

        Vector3 smallScale =
            originalScale *
            0.9f;


        // ========================================================
        // GROW
        // ========================================================

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


        // ========================================================
        // SHRINK
        // ========================================================

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


        // ========================================================
        // RETURN TO NORMAL
        // ========================================================

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


        // --------------------------------------------------------
        // FINAL SCALE
        // --------------------------------------------------------

        transform.localScale =
            originalScale;
    }


    // ============================================================
    // FIND LEVEL DESCRIPTION
    // ============================================================

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


        // --------------------------------------------------------
        // LOOK FOR TMP DIRECTLY
        // --------------------------------------------------------

        hoverInfoText =
            levelDesc.GetComponent<TMP_Text>();


        // --------------------------------------------------------
        // OTHERWISE SEARCH CHILDREN
        // --------------------------------------------------------

        if (hoverInfoText == null)
        {
            hoverInfoText =
                levelDesc.GetComponentInChildren<TMP_Text>(
                    true
                );
        }


        // --------------------------------------------------------
        // FAILED
        // --------------------------------------------------------

        if (hoverInfoText == null)
        {
            Debug.LogError(
                "[IconBehav] Could not find a TMP_Text component " +
                "on or inside LevelDesc.",
                levelDesc
            );
        }
    }


    // ============================================================
    // SHOW HOVER INFO
    // ============================================================

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


        // Enable TMP only.
        //
        // Do NOT disable the LevelDesc GameObject.
        //
        // GameObject.Find() only finds active objects.

        hoverInfoText.enabled = true;
    }


    // ============================================================
    // HIDE HOVER INFO
    // ============================================================

    private void HideHoverInfo()
    {
        if (hoverInfoText != null)
        {
            hoverInfoText.enabled = false;
        }
    }


    // ============================================================
    // OBJECTIVE TEXT
    // ============================================================

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

                if (
                    string.IsNullOrWhiteSpace(
                        encounter.targetEnemyId
                    )
                )
                {
                    return "Defeat the target enemy.";
                }


                return
                    $"Defeat {encounter.targetEnemyId}.";


            default:

                return "Unknown objective.";
        }
    }


    // ============================================================
    // SET ENCOUNTER
    // ============================================================

    public void SetEncounter(
        EncounterDefinition newEncounter)
    {
        encounter =
            newEncounter;


        FindLevelDescription();
    }


    // ============================================================
    // GET ENCOUNTER
    // ============================================================

    public EncounterDefinition GetEncounter()
    {
        return encounter;
    }


    // ============================================================
    // SET ENCOUNTER MANAGER
    // ============================================================

    public void SetEncounterManager(
        EncounterManager manager)
    {
        encounterManager =
            manager;
    }


    // ============================================================
    // SET MAP MANAGER
    // ============================================================

    public void SetMapManager(
        LevelMapManager manager)
    {
        mapManager =
            manager;
    }


    // ============================================================
    // SET TRANSITION MANAGER
    // ============================================================

    public void SetTransitionManager(
        transitionGameManager manager)
    {
        transitionManager =
            manager;
    }


    // ============================================================
    // SET NODE STATE
    // ============================================================

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


    // ============================================================
    // REFRESH VISUALS
    // ============================================================

    private void RefreshVisuals()
    {
        // --------------------------------------------------------
        // LOCKED
        // --------------------------------------------------------

        if (lockedVisual != null)
        {
            lockedVisual.SetActive(
                !isUnlocked &&
                !isCompleted
            );
        }


        // --------------------------------------------------------
        // UNLOCKED
        // --------------------------------------------------------

        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(
                isUnlocked &&
                !isCompleted
            );
        }


        // --------------------------------------------------------
        // COMPLETED
        // --------------------------------------------------------

        if (completedVisual != null)
        {
            completedVisual.SetActive(
                isCompleted
            );
        }
    }


    // ============================================================
    // STATE GETTERS
    // ============================================================

    public bool IsUnlocked()
    {
        return isUnlocked;
    }


    public bool IsCompleted()
    {
        return isCompleted;
    }


    // ============================================================
    // CLICK SOUND
    // ============================================================

    private void PlayClickSound()
    {
        if (
            audioSource == null ||
            clickSound == null
        )
        {
            return;
        }


        audioSource.PlayOneShot(
            clickSound
        );
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        CancelInvoke(
            nameof(ResetClickGuard)
        );


        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        StopAllCoroutines();


        transform.localScale =
            originalScale;


        isHovering = false;
    }
}