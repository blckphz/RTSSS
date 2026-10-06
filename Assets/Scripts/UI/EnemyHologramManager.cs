using System.Collections;
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

    private Vector3 hologramBaseScale = Vector3.one;


    // ============================================================
    // LINE
    // ============================================================

    [Header("Path Line Renderer")]
    [SerializeField]
    private GameObject lineRendererManagerObject;

    [SerializeField]
    private Vector3 lineOffset = Vector3.zero;

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

    private Coroutine effectsCoroutine;

    private bool isShowing;


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
        HoverInfoTrigger.SelectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        HoverInfoTrigger.SelectionChanged -= OnSelectionChanged;

        StopEffectsCoroutine();
        ClearSelection();
    }


    // ============================================================
    // LINE RENDERER
    // ============================================================

    private void FindLineRenderer()
    {
        if (pathLineRenderer != null)
            return;

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
                "[Hologram] Hologram Prefab is unassigned!",
                this
            );

            return;
        }

        poolSize = Mathf.Max(1, poolSize);

        hologramPool.Clear();
        hologramSpriteRenderers.Clear();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject hologram =
                Instantiate(
                    hologramPrefab,
                    transform
                );

            hologram.name =
                $"Enemy Movement Hologram {i}";

            hologram.SetActive(false);

            SpriteRenderer spriteRenderer =
                hologram.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                Debug.LogWarning(
                    "[Hologram] No SpriteRenderer found on hologram prefab!",
                    hologram
                );
            }

            hologramPool.Add(hologram);
            hologramSpriteRenderers.Add(spriteRenderer);
        }

        if (hologramPool.Count > 0)
        {
            hologramInstance = hologramPool[0];
            hologramSpriteRenderer = hologramSpriteRenderers[0];

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
            return;

        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram = hologramPool[i];

            if (hologram == null)
                continue;

            if (!hologram.activeSelf)
            {
                hologramInstance = hologram;
                hologramSpriteRenderer = hologramSpriteRenderers[i];

                hologramBaseScale =
                    hologramInstance.transform.localScale;

                return;
            }
        }

        // Fallback
        hologramInstance = hologramPool[0];
        hologramSpriteRenderer = hologramSpriteRenderers[0];

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
            return;

        AttackUnit unit =
            trigger.GetAttackUnit();

        // --------------------------------------------------------
        // DESELECT
        // --------------------------------------------------------

        if (!selected)
        {
            if (unit == selectedEnemy)
            {
                ClearSelection();
            }

            return;
        }

        // --------------------------------------------------------
        // INVALID
        // --------------------------------------------------------

        if (
            unit == null ||
            unit.IsDead()
        )
        {
            ClearSelection();
            return;
        }

        // --------------------------------------------------------
        // TEAM
        // --------------------------------------------------------

        Team team = unit.GetTeam();

        if (
            team == Team.Player ||
            team == Team.Ally
        )
        {
            ClearSelection();
            return;
        }

        // --------------------------------------------------------
        // SELECT
        // --------------------------------------------------------

        selectedEnemy = unit;

        selectedMoveBrain =
            selectedEnemy.GetComponent<UnitMoveBrain>();

        if (selectedMoveBrain == null)
        {
            Debug.LogError(
                "[Hologram] Selected enemy has no UnitMoveBrain!",
                selectedEnemy
            );

            ClearSelection();
            return;
        }

        cachedEnemySpriteRenderer =
            selectedEnemy.GetComponentInChildren<SpriteRenderer>();

        if (cachedEnemySpriteRenderer == null)
        {
            Debug.LogWarning(
                "[Hologram] Enemy has no SpriteRenderer!",
                selectedEnemy
            );

            ClearSelection();
            return;
        }

        UpdateHologramSprite();

        RefreshHologram();

        StartEffectsCoroutine();
    }


    // ============================================================
    // REFRESH
    // ============================================================

    /// <summary>
    /// Recalculates the predicted destination and moves the hologram.
    /// Call this whenever the enemy's movement/path state changes.
    /// </summary>
    public void RefreshHologram()
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
            ClearSelection();
            return;
        }

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
            HideHologram();
            return;
        }

        ShowHologramAt(
            gridManager,
            predictedTile
        );
    }


    // ============================================================
    // SPRITE
    // ============================================================

    private void UpdateHologramSprite()
    {
        if (cachedEnemySpriteRenderer == null)
            return;

        EnsureHologramInstance();

        if (hologramSpriteRenderer == null)
            return;

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
    // SHOW
    // ============================================================

    private void ShowHologramAt(
        GridManager gridManager,
        Vector2Int tile
    )
    {
        EnsureHologramInstance();

        if (
            hologramInstance == null ||
            hologramSpriteRenderer == null
        )
        {
            return;
        }

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(tile);

        hologramInstance.transform.position =
            worldPosition + hologramOffset;

        hologramInstance.transform.localScale =
            hologramBaseScale;

        if (!hologramInstance.activeSelf)
        {
            hologramInstance.SetActive(true);
        }

        isShowing = true;

        UpdatePathLine(worldPosition);
    }


    // ============================================================
    // PATH LINE
    // ============================================================

    private void UpdatePathLine(Vector3 destination)
    {
        if (selectedEnemy == null)
            return;

        if (pathLineRenderer == null)
            FindLineRenderer();

        if (pathLineRenderer == null)
            return;

        pathLineRenderer.positionCount = 2;

        pathLineRenderer.SetPosition(
            0,
            selectedEnemy.transform.position + lineOffset
        );

        pathLineRenderer.SetPosition(
            1,
            destination + lineOffset
        );

        pathLineRenderer.enabled = true;
    }


    // ============================================================
    // EFFECT COROUTINE
    // ============================================================

    private void StartEffectsCoroutine()
    {
        StopEffectsCoroutine();

        if (!pulseHologram && transparencySpeed <= 0f)
            return;

        effectsCoroutine =
            StartCoroutine(HologramEffectsRoutine());
    }


    private void StopEffectsCoroutine()
    {
        if (effectsCoroutine == null)
            return;

        StopCoroutine(effectsCoroutine);
        effectsCoroutine = null;
    }


    private IEnumerator HologramEffectsRoutine()
    {
        while (isShowing && hologramInstance != null)
        {
            if (!hologramInstance.activeSelf)
                break;

            float time = Time.time;

            // ----------------------------------------------------
            // PULSE
            // ----------------------------------------------------

            if (pulseHologram)
            {
                float pulse =
                    1f +
                    Mathf.Sin(time * pulseSpeed) *
                    pulseAmount;

                hologramInstance.transform.localScale =
                    hologramBaseScale * pulse;
            }

            // ----------------------------------------------------
            // TRANSPARENCY
            // ----------------------------------------------------

            if (transparencySpeed > 0f)
            {
                float pingPong =
                    Mathf.PingPong(
                        time * transparencySpeed,
                        1f
                    );

                float alpha =
                    Mathf.Lerp(
                        maxAlpha,
                        minAlpha,
                        pingPong
                    );

                SetHologramAlpha(alpha);
            }

            yield return null;
        }

        effectsCoroutine = null;
    }


    // ============================================================
    // ALPHA
    // ============================================================

    private void SetHologramAlpha(float alpha)
    {
        if (hologramSpriteRenderer == null)
            return;

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
        isShowing = false;

        StopEffectsCoroutine();

        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram =
                hologramPool[i];

            if (hologram != null)
                hologram.SetActive(false);
        }

        if (hologramInstance != null)
        {
            hologramInstance.transform.localScale =
                hologramBaseScale;
        }

        if (hologramSpriteRenderer != null)
        {
            SetHologramAlpha(maxAlpha);
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
        return isShowing;
    }


    public void SetHologramVisible(bool visible)
    {
        showHologram = visible;

        if (!visible)
        {
            HideHologram();
            return;
        }

        if (selectedEnemy != null)
        {
            RefreshHologram();
            StartEffectsCoroutine();
        }
    }


    /// <summary>
    /// Call this when the selected enemy's movement/path changes.
    /// </summary>
    public void Refresh()
    {
        RefreshHologram();
    }
}