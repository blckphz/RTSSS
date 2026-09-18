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
    private Vector3 hologramOffset =
        new Vector3(0f, 0.05f, 0f);


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

    [SerializeField]
    private bool requireMovementAction = true;

    [SerializeField]
    private bool showDebugLogs = true;


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


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        ValidateSetup();

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

        if (selectedEnemy == null)
        {
            HideHologram();
            return;
        }

        if (selectedMoveBrain == null)
        {
            HideHologram();
            return;
        }

        if (selectedEnemy.IsDead())
        {
            ClearSelection();
            return;
        }

        UpdateHologramSprite();

        UpdateHologram();

        UpdateHologramEffects();
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private void ValidateSetup()
    {
        if (hologramPrefab == null)
        {
            Debug.LogError(
                "[EnemyHologram] HOLOGRAM PREFAB IS NOT ASSIGNED.",
                this
            );

            return;
        }

        SpriteRenderer prefabRenderer =
            hologramPrefab.GetComponentInChildren<SpriteRenderer>();

        if (prefabRenderer == null)
        {
            Debug.LogError(
                "[EnemyHologram] Hologram prefab does not contain a SpriteRenderer.",
                hologramPrefab
            );
        }
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
                lineRendererManagerObject.GetComponent<LineRenderer>();

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
                "[EnemyHologram] No LineRenderer found. " +
                "The hologram will still work without the line.",
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
                Debug.LogError(
                    "[EnemyHologram] Hologram instance has no SpriteRenderer.",
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

        if (showDebugLogs)
        {
            Debug.Log(
                "[EnemyHologram] Hologram pool created. " +
                "Count: " + hologramPool.Count,
                this
            );
        }
    }


    // ============================================================
    // GET INSTANCE
    // ============================================================

    private void EnsureHologramInstance()
    {
        if (hologramInstance != null &&
            hologramSpriteRenderer != null)
        {
            return;
        }

        if (hologramPool.Count == 0)
        {
            CreateHologramPool();
        }

        if (hologramPool.Count == 0)
        {
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
        bool selected)
    {
        if (trigger == null)
        {
            return;
        }

        AttackUnit unit =
            trigger.GetAttackUnit();

        if (!selected)
        {
            if (unit == selectedEnemy)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        "[EnemyHologram] Enemy deselected.",
                        this
                    );
                }

                ClearSelection();
            }

            return;
        }

        if (unit == null)
        {
            Debug.LogWarning(
                "[EnemyHologram] Selection event fired, " +
                "but HoverInfoTrigger returned no AttackUnit.",
                trigger
            );

            ClearSelection();

            return;
        }

        Team team =
            unit.GetTeam();

        if (team == Team.Player ||
            team == Team.Ally)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[EnemyHologram] Selected unit is not an enemy.",
                    unit
                );
            }

            ClearSelection();

            return;
        }

        if (unit.IsDead())
        {
            ClearSelection();

            return;
        }

        selectedEnemy =
            unit;

        selectedMoveBrain =
            selectedEnemy.GetComponent<UnitMoveBrain>();

        if (selectedMoveBrain == null)
        {
            Debug.LogError(
                "[EnemyHologram] Enemy has AttackUnit but NO UnitMoveBrain.",
                selectedEnemy
            );

            ClearSelection();

            return;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[EnemyHologram] Selected enemy: " +
                selectedEnemy.name +
                " | Move actions: " +
                selectedMoveBrain.GetMoveActionsRemaining() +
                "/" +
                selectedMoveBrain.GetMoveActionsPerTurn() +
                " | Move range: " +
                selectedMoveBrain.GetMoveRange(),
                selectedEnemy
            );
        }

        UpdateHologramSprite();

        UpdateHologram();
    }


    // ============================================================
    // UPDATE HOLOGRAM
    // ============================================================

    private void UpdateHologram()
    {
        if (selectedEnemy == null)
        {
            HideHologram();
            return;
        }

        if (selectedMoveBrain == null)
        {
            HideHologram();
            return;
        }

        // --------------------------------------------------------
        // MOVEMENT ACTION CHECK
        // --------------------------------------------------------

        if (requireMovementAction)
        {
            int remaining =
                selectedMoveBrain.GetMoveActionsRemaining();

            if (remaining <= 0)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        "[EnemyHologram] No movement actions remaining.",
                        selectedEnemy
                    );
                }

                HideHologram();

                return;
            }
        }


        // --------------------------------------------------------
        // PREDICT
        // --------------------------------------------------------

        bool hasPrediction =
            selectedMoveBrain.TryGetPredictedMoveTile(
                out Vector2Int predictedTile
            );

        if (!hasPrediction)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[EnemyHologram] Could not predict movement for: " +
                    selectedEnemy.name +
                    " | Actions: " +
                    selectedMoveBrain.GetMoveActionsRemaining() +
                    " | Move range: " +
                    selectedMoveBrain.GetMoveRange(),
                    selectedEnemy
                );
            }

            HideHologram();

            return;
        }


        // --------------------------------------------------------
        // GRID
        // --------------------------------------------------------

        GridManager gridManager =
            selectedMoveBrain.GetGridManager();

        if (gridManager == null)
        {
            Debug.LogError(
                "[EnemyHologram] No GridManager found.",
                selectedEnemy
            );

            HideHologram();

            return;
        }


        // --------------------------------------------------------
        // SHOW
        // --------------------------------------------------------

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
            Debug.LogError(
                "[EnemyHologram] Hologram SpriteRenderer is missing.",
                this
            );

            return;
        }

        SpriteRenderer enemySpriteRenderer =
            selectedEnemy.GetComponentInChildren<SpriteRenderer>();

        if (enemySpriteRenderer == null)
        {
            Debug.LogError(
                "[EnemyHologram] Selected enemy has no SpriteRenderer.",
                selectedEnemy
            );

            HideHologram();

            return;
        }

        hologramSpriteRenderer.sprite =
            enemySpriteRenderer.sprite;

        hologramSpriteRenderer.flipX =
            enemySpriteRenderer.flipX;

        hologramSpriteRenderer.flipY =
            enemySpriteRenderer.flipY;

        hologramSpriteRenderer.sortingLayerID =
            enemySpriteRenderer.sortingLayerID;

        hologramSpriteRenderer.sortingOrder =
            enemySpriteRenderer.sortingOrder + 1;
    }


    // ============================================================
    // SHOW HOLOGRAM
    // ============================================================

    private void ShowHologramAt(
        GridManager gridManager,
        Vector2Int tile)
    {
        if (gridManager == null)
        {
            return;
        }

        EnsureHologramInstance();

        if (hologramInstance == null)
        {
            Debug.LogError(
                "[EnemyHologram] Could not create hologram instance.",
                this
            );

            return;
        }

        if (hologramSpriteRenderer == null)
        {
            Debug.LogError(
                "[EnemyHologram] Hologram SpriteRenderer is null.",
                hologramInstance
            );

            return;
        }

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(
                tile
            );


        // --------------------------------------------------------
        // POSITION
        // --------------------------------------------------------

        hologramInstance.transform.position =
            worldPosition +
            hologramOffset;


        // --------------------------------------------------------
        // SCALE
        // --------------------------------------------------------

        hologramInstance.transform.localScale =
            hologramBaseScale;


        // --------------------------------------------------------
        // ALPHA
        // --------------------------------------------------------

        SetHologramAlpha(
            maxAlpha
        );


        // --------------------------------------------------------
        // ENABLE
        // --------------------------------------------------------

        if (!hologramInstance.activeSelf)
        {
            hologramInstance.SetActive(true);
        }


        // --------------------------------------------------------
        // LINE
        // --------------------------------------------------------

        UpdatePathLine(
            worldPosition
        );


        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                "[EnemyHologram] SHOWING at tile " +
                tile +
                " | World position: " +
                worldPosition,
                hologramInstance
            );
        }
    }


    // ============================================================
    // PATH LINE
    // ============================================================

    private void UpdatePathLine(
        Vector3 destination)
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

        Vector3 startPosition =
            selectedEnemy.transform.position +
            lineOffset;

        Vector3 endPosition =
            destination +
            lineOffset;

        pathLineRenderer.positionCount = 2;

        pathLineRenderer.SetPosition(
            0,
            startPosition
        );

        pathLineRenderer.SetPosition(
            1,
            endPosition
        );

        pathLineRenderer.enabled = true;
    }


    // ============================================================
    // EFFECTS
    // ============================================================

    private void UpdateHologramEffects()
    {
        if (hologramInstance == null)
        {
            return;
        }

        if (hologramSpriteRenderer == null)
        {
            return;
        }

        if (!hologramInstance.activeSelf)
        {
            return;
        }


        // --------------------------------------------------------
        // SCALE
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // ALPHA
        // --------------------------------------------------------

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
        float alpha)
    {
        if (hologramSpriteRenderer == null)
        {
            return;
        }

        Color color =
            hologramSpriteRenderer.color;

        color.a =
            Mathf.Clamp01(alpha);

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
            pathLineRenderer.positionCount = 0;

            pathLineRenderer.enabled = false;
        }
    }


    // ============================================================
    // CLEAR
    // ============================================================

    private void ClearSelection()
    {
        selectedEnemy = null;

        selectedMoveBrain = null;

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
        bool visible)
    {
        showHologram =
            visible;

        if (!visible)
        {
            HideHologram();
        }
    }
}
