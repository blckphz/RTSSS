using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHologramManager : MonoBehaviour
{
    // ============================================================
    // HOLOGRAM
    // ============================================================

    [Header("Hologram")]
    [SerializeField]
    private GameObject hologramPrefab;

    [SerializeField]
    private Vector3 hologramOffset = new Vector3(0f, 0.05f, 0f);


    // ============================================================
    // HOLOGRAM POOL
    // ============================================================

    [Header("Hologram Pool")]
    [SerializeField]
    [Min(1)]
    private int poolSize = 1;

    private readonly List<GameObject> hologramPool =
        new List<GameObject>();

    private readonly List<SpriteRenderer> hologramSpriteRenderers =
        new List<SpriteRenderer>();

    private GameObject hologramInstance;

    private SpriteRenderer hologramSpriteRenderer;

    private Vector3 hologramBaseScale =
        Vector3.one;


    // ============================================================
    // LINE
    // ============================================================

    [Header("Path Line Renderer")]
    [SerializeField]
    private GameObject lineRendererManagerObject;

    [SerializeField]
    private Vector3 lineOffset =
        Vector3.zero;

    private LineRenderer pathLineRenderer;


    // ============================================================
    // SETTINGS
    // ============================================================

    [Header("Settings")]
    [SerializeField]
    private bool showHologram = true;


    // ============================================================
    // PULSE
    // ============================================================

    [Header("Hologram Pulse")]
    [SerializeField]
    private bool pulseHologram = true;

    [SerializeField]
    private float pulseSpeed = 1f;

    [SerializeField]
    private float pulseAmount = 0.08f;


    // ============================================================
    // TRANSPARENCY
    // ============================================================

    [Header("Hologram Transparency")]
    [SerializeField]
    private float transparencySpeed = 3f;

    [Range(0f, 1f)]
    [SerializeField]
    private float maxAlpha = 0.7f;

    [Range(0f, 1f)]
    [SerializeField]
    private float minAlpha = 0.5f;


    // ============================================================
    // STATE
    // ============================================================

    private AttackUnit selectedEnemy;

    private UnitMoveBrain selectedMoveBrain;

    private SpriteRenderer cachedEnemySpriteRenderer;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindLineRenderer();

        CreateHologramPool();

        HideHologram();
    }


    private void OnEnable()
    {
        HoverInfoTrigger.SelectionChanged +=
            OnSelectionChanged;
    }


    private void OnDisable()
    {
        HoverInfoTrigger.SelectionChanged -=
            OnSelectionChanged;

        ClearSelection();
    }


    private void Update()
    {
        if (!showHologram)
        {
            HideHologram();
            return;
        }


        if (
            selectedEnemy == null ||
            selectedMoveBrain == null
        )
        {
            HideHologram();
            return;
        }


        if (selectedEnemy.IsDead())
        {
            Debug.Log(
                "[Hologram] Selected enemy is dead. Clearing selection.",
                selectedEnemy
            );

            ClearSelection();

            return;
        }


        UpdateHologramSprite();

        UpdateHologram();

        UpdateHologramEffects();
    }


    // ============================================================
    // LINE RENDERER
    // ============================================================

    private void FindLineRenderer()
    {
        if (pathLineRenderer != null)
        {
            return;
        }


        if (lineRendererManagerObject != null)
        {
            pathLineRenderer =
                lineRendererManagerObject
                    .GetComponent<LineRenderer>();


            if (pathLineRenderer == null)
            {
                pathLineRenderer =
                    lineRendererManagerObject
                        .GetComponentInChildren<LineRenderer>();
            }
        }


        if (pathLineRenderer == null)
        {
            pathLineRenderer =
                GetComponentInChildren<LineRenderer>();
        }


        if (pathLineRenderer == null)
        {
            Debug.LogWarning(
                "[Hologram] Path LineRenderer could not be found!",
                this
            );
        }
    }


    // ============================================================
    // CREATE POOL
    // ============================================================

    private void CreateHologramPool()
    {
        if (hologramPrefab == null)
        {
            Debug.LogError(
                "[Hologram] Hologram Prefab is unassigned in the inspector!",
                this
            );

            return;
        }


        poolSize =
            Mathf.Max(
                1,
                poolSize
            );


        hologramPool.Clear();

        hologramSpriteRenderers.Clear();


        for (
            int i = 0;
            i < poolSize;
            i++
        )
        {
            GameObject hologram =
                Instantiate(
                    hologramPrefab,
                    transform
                );


            hologram.name =
                "Enemy Movement Hologram " + i;


            hologram.SetActive(false);


            SpriteRenderer spriteRenderer =
                hologram.GetComponentInChildren<SpriteRenderer>();


            if (spriteRenderer == null)
            {
                Debug.LogWarning(
                    "[Hologram] No SpriteRenderer found on hologram prefab children!",
                    hologram
                );
            }


            hologramPool.Add(
                hologram
            );


            hologramSpriteRenderers.Add(
                spriteRenderer
            );
        }


        if (hologramPool.Count > 0)
        {
            hologramInstance =
                hologramPool[0];


            hologramSpriteRenderer =
                hologramSpriteRenderers[0];


            if (hologramInstance != null)
            {
                hologramBaseScale =
                    hologramInstance.transform.localScale;
            }
        }
    }


    // ============================================================
    // GET INSTANCE
    // ============================================================

    private void EnsureHologramInstance()
    {
        if (
            hologramInstance != null &&
            hologramSpriteRenderer != null
        )
        {
            return;
        }


        if (hologramPool.Count == 0)
        {
            CreateHologramPool();
        }


        if (hologramPool.Count == 0)
        {
            Debug.LogError(
                "[Hologram] Hologram pool count is 0 after creation attempt!",
                this
            );

            return;
        }


        for (
            int i = 0;
            i < hologramPool.Count;
            i++
        )
        {
            GameObject hologram =
                hologramPool[i];


            if (hologram == null)
            {
                continue;
            }


            if (!hologram.activeSelf)
            {
                hologramInstance =
                    hologram;


                hologramSpriteRenderer =
                    hologramSpriteRenderers[i];


                hologramBaseScale =
                    hologramInstance.transform.localScale;


                return;
            }
        }


        hologramInstance =
            hologramPool[0];


        hologramSpriteRenderer =
            hologramSpriteRenderers[0];


        if (hologramInstance != null)
        {
            hologramBaseScale =
                hologramInstance.transform.localScale;
        }
    }


    // ============================================================
    // SELECTION
    // ============================================================

    private void OnSelectionChanged(
        HoverInfoTrigger trigger,
        bool selected
    )
    {
        if (trigger == null)
        {
            Debug.Log(
                "[Hologram] SelectionChanged triggered with a null trigger."
            );

            return;
        }


        AttackUnit unit =
            trigger.GetAttackUnit();


        // --------------------------------------------------------
        // DESELECT
        // --------------------------------------------------------

        if (!selected)
        {
            if (unit == selectedEnemy)
            {
                Debug.Log(
                    "[Hologram] Deselected current enemy unit.",
                    unit
                );

                ClearSelection();
            }

            return;
        }


        // --------------------------------------------------------
        // INVALID UNIT
        // --------------------------------------------------------

        if (
            unit == null ||
            unit.IsDead()
        )
        {
            Debug.Log(
                "[Hologram] Selected unit is null or dead.",
                unit
            );

            ClearSelection();

            return;
        }


        // --------------------------------------------------------
        // TEAM CHECK
        // --------------------------------------------------------

        Team team =
            unit.GetTeam();


        if (
            team == Team.Player ||
            team == Team.Ally
        )
        {
            Debug.Log(
                $"[Hologram] Ignored selection because unit team is {team} (not an enemy).",
                unit
            );

            ClearSelection();

            return;
        }


        // --------------------------------------------------------
        // SELECT ENEMY
        // --------------------------------------------------------

        Debug.Log(
            "[Hologram] Successfully selected enemy unit: " +
            unit.name,
            unit
        );


        selectedEnemy =
            unit;


        cachedEnemySpriteRenderer =
            selectedEnemy
                .GetComponentInChildren<SpriteRenderer>();


        selectedMoveBrain =
            selectedEnemy
                .GetComponent<UnitMoveBrain>();


        if (selectedMoveBrain == null)
        {
            Debug.LogError(
                "[Hologram] Selected enemy is missing a UnitMoveBrain component!",
                selectedEnemy
            );

            ClearSelection();

            return;
        }


        UpdateHologramSprite();

        UpdateHologram();
    }


    // ============================================================
    // UPDATE HOLOGRAM
    // ============================================================

    private void UpdateHologram()
    {
        if (
            selectedEnemy == null ||
            selectedMoveBrain == null
        )
        {
            HideHologram();

            return;
        }


        // ========================================================
        // IMPORTANT
        //
        // TRUE means:
        //
        // "Ignore how many movement steps the enemy has already
        // spent this turn and show the movement it WOULD make
        // using its full movement range."
        //
        // This does NOT consume movement.
        // This does NOT change stepsRemaining.
        // ========================================================

        if (
            !selectedMoveBrain.TryGetPredictedMoveTile(
                out Vector2Int predictedTile,
                true
            )
        )
        {
            HideHologram();

            return;
        }


        GridManager gridManager =
            selectedMoveBrain.GetGridManager();


        if (gridManager == null)
        {
            Debug.LogError(
                "[Hologram] Hidden: GridManager returned null from selectedMoveBrain.",
                selectedEnemy
            );

            HideHologram();

            return;
        }


        ShowHologramAt(
            gridManager,
            predictedTile
        );
    }


    // ============================================================
    // UPDATE SPRITE
    // ============================================================

    private void UpdateHologramSprite()
    {
        if (selectedEnemy == null)
        {
            return;
        }


        EnsureHologramInstance();


        if (hologramSpriteRenderer == null)
        {
            return;
        }


        if (cachedEnemySpriteRenderer == null)
        {
            cachedEnemySpriteRenderer =
                selectedEnemy
                    .GetComponentInChildren<SpriteRenderer>();


            if (cachedEnemySpriteRenderer == null)
            {
                Debug.LogWarning(
                    "[Hologram] Cached enemy SpriteRenderer is missing.",
                    selectedEnemy
                );

                HideHologram();

                return;
            }
        }


        hologramSpriteRenderer.sprite =
            cachedEnemySpriteRenderer.sprite;


        hologramSpriteRenderer.flipX =
            cachedEnemySpriteRenderer.flipX;


        hologramSpriteRenderer.flipY =
            cachedEnemySpriteRenderer.flipY;


        hologramSpriteRenderer.sortingLayerID =
            cachedEnemySpriteRenderer.sortingLayerID;


        hologramSpriteRenderer.sortingOrder =
            cachedEnemySpriteRenderer.sortingOrder + 1;
    }


    // ============================================================
    // SHOW HOLOGRAM
    // ============================================================

    private void ShowHologramAt(
        GridManager gridManager,
        Vector2Int tile
    )
    {
        if (gridManager == null)
        {
            return;
        }


        EnsureHologramInstance();


        if (
            hologramInstance == null ||
            hologramSpriteRenderer == null
        )
        {
            Debug.LogError(
                "[Hologram] Cannot show hologram: hologramInstance or hologramSpriteRenderer is null."
            );

            return;
        }


        Vector3 worldPosition =
            gridManager.GridToWorldPosition(
                tile
            );


        hologramInstance.transform.position =
            worldPosition +
            hologramOffset;


        hologramInstance.transform.localScale =
            hologramBaseScale;


        SetHologramAlpha(
            maxAlpha
        );


        if (!hologramInstance.activeSelf)
        {
            Debug.Log(
                "[Hologram] Enabling hologram GameObject at tile: " +
                tile,
                hologramInstance
            );


            hologramInstance.SetActive(true);
        }


        UpdatePathLine(
            worldPosition
        );
    }


    // ============================================================
    // PATH LINE
    // ============================================================

    private void UpdatePathLine(
        Vector3 destination
    )
    {
        if (selectedEnemy == null)
        {
            return;
        }


        if (pathLineRenderer == null)
        {
            FindLineRenderer();
        }


        if (pathLineRenderer == null)
        {
            return;
        }


        pathLineRenderer.positionCount =
            2;


        pathLineRenderer.SetPosition(
            0,
            selectedEnemy.transform.position +
            lineOffset
        );


        pathLineRenderer.SetPosition(
            1,
            destination +
            lineOffset
        );


        pathLineRenderer.enabled =
            true;
    }


    // ============================================================
    // EFFECTS
    // ============================================================

    private void UpdateHologramEffects()
    {
        if (
            hologramInstance == null ||
            hologramSpriteRenderer == null ||
            !hologramInstance.activeSelf
        )
        {
            return;
        }


        if (pulseHologram)
        {
            float pulse =
                1f +
                Mathf.Sin(
                    Time.time *
                    pulseSpeed
                ) *
                pulseAmount;


            hologramInstance.transform.localScale =
                hologramBaseScale *
                pulse;
        }


        float pingPong =
            Mathf.PingPong(
                Time.time *
                transparencySpeed,
                1f
            );


        float alpha =
            Mathf.Lerp(
                maxAlpha,
                minAlpha,
                pingPong
            );


        SetHologramAlpha(
            alpha
        );
    }


    // ============================================================
    // ALPHA
    // ============================================================

    private void SetHologramAlpha(
        float alpha
    )
    {
        if (hologramSpriteRenderer == null)
        {
            return;
        }


        Color color =
            hologramSpriteRenderer.color;


        color.a =
            Mathf.Clamp01(
                alpha
            );


        hologramSpriteRenderer.color =
            color;
    }


    // ============================================================
    // HIDE
    // ============================================================

    private void HideHologram()
    {
        for (
            int i = 0;
            i < hologramPool.Count;
            i++
        )
        {
            GameObject hologram =
                hologramPool[i];


            if (hologram != null)
            {
                hologram.SetActive(false);
            }
        }


        if (hologramInstance != null)
        {
            hologramInstance.transform.localScale =
                hologramBaseScale;
        }


        if (hologramSpriteRenderer != null)
        {
            SetHologramAlpha(
                maxAlpha
            );
        }


        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount =
                0;


            pathLineRenderer.enabled =
                false;
        }
    }


    // ============================================================
    // CLEAR
    // ============================================================

    private void ClearSelection()
    {
        selectedEnemy = null;

        selectedMoveBrain = null;

        cachedEnemySpriteRenderer = null;

        HideHologram();
    }


    // ============================================================
    // PUBLIC API
    // ============================================================

    public void Hide()
    {
        ClearSelection();
    }


    public AttackUnit GetSelectedEnemy()
    {
        return selectedEnemy;
    }


    public UnitMoveBrain GetSelectedMoveBrain()
    {
        return selectedMoveBrain;
    }


    public bool IsShowingHologram()
    {
        return
            hologramInstance != null &&
            hologramInstance.activeSelf;
    }


    public void SetHologramVisible(
        bool visible
    )
    {
        showHologram =
            visible;


        if (!visible)
        {
            HideHologram();
        }
    }
}