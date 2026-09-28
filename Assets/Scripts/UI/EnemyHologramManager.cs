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

    private readonly List<GameObject> hologramPool = new List<GameObject>();
    private readonly List<SpriteRenderer> hologramSpriteRenderers = new List<SpriteRenderer>();

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

    [SerializeField]
    private bool requireMovementAction = true;


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
        HoverInfoTrigger.SelectionChanged += OnSelectionChanged;
    }


    private void OnDisable()
    {
        HoverInfoTrigger.SelectionChanged -= OnSelectionChanged;
        ClearSelection();
    }


    private void Update()
    {
        if (!showHologram || selectedEnemy == null || selectedMoveBrain == null)
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
    // LINE RENDERER
    // ============================================================

    private void FindLineRenderer()
    {
        if (pathLineRenderer != null) return;

        if (lineRendererManagerObject != null)
        {
            pathLineRenderer = lineRendererManagerObject.GetComponent<LineRenderer>();
            if (pathLineRenderer == null)
            {
                pathLineRenderer = lineRendererManagerObject.GetComponentInChildren<LineRenderer>();
            }
        }

        if (pathLineRenderer == null)
        {
            pathLineRenderer = GetComponentInChildren<LineRenderer>();
        }
    }


    // ============================================================
    // CREATE POOL
    // ============================================================

    private void CreateHologramPool()
    {
        if (hologramPrefab == null) return;

        poolSize = Mathf.Max(1, poolSize);
        hologramPool.Clear();
        hologramSpriteRenderers.Clear();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject hologram = Instantiate(hologramPrefab, transform);
            hologram.name = "Enemy Movement Hologram " + i;
            hologram.SetActive(false);

            SpriteRenderer spriteRenderer = hologram.GetComponentInChildren<SpriteRenderer>();
            hologramPool.Add(hologram);
            hologramSpriteRenderers.Add(spriteRenderer);
        }

        if (hologramPool.Count > 0)
        {
            hologramInstance = hologramPool[0];
            hologramSpriteRenderer = hologramSpriteRenderers[0];

            if (hologramInstance != null)
            {
                hologramBaseScale = hologramInstance.transform.localScale;
            }
        }
    }


    // ============================================================
    // GET INSTANCE
    // ============================================================

    private void EnsureHologramInstance()
    {
        if (hologramInstance != null && hologramSpriteRenderer != null) return;

        if (hologramPool.Count == 0)
        {
            CreateHologramPool();
        }

        if (hologramPool.Count == 0) return;

        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram = hologramPool[i];
            if (hologram == null) continue;

            if (!hologram.activeSelf)
            {
                hologramInstance = hologram;
                hologramSpriteRenderer = hologramSpriteRenderers[i];
                hologramBaseScale = hologramInstance.transform.localScale;
                return;
            }
        }

        hologramInstance = hologramPool[0];
        hologramSpriteRenderer = hologramSpriteRenderers[0];

        if (hologramInstance != null)
        {
            hologramBaseScale = hologramInstance.transform.localScale;
        }
    }


    // ============================================================
    // SELECTION
    // ============================================================

    private void OnSelectionChanged(HoverInfoTrigger trigger, bool selected)
    {
        if (trigger == null) return;

        AttackUnit unit = trigger.GetAttackUnit();

        if (!selected)
        {
            if (unit == selectedEnemy)
            {
                ClearSelection();
            }
            return;
        }

        if (unit == null || unit.IsDead())
        {
            ClearSelection();
            return;
        }

        Team team = unit.GetTeam();
        if (team == Team.Player || team == Team.Ally)
        {
            ClearSelection();
            return;
        }

        selectedEnemy = unit;
        cachedEnemySpriteRenderer = selectedEnemy.GetComponentInChildren<SpriteRenderer>();
        selectedMoveBrain = selectedEnemy.GetComponent<UnitMoveBrain>();

        if (selectedMoveBrain == null)
        {
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
        if (selectedEnemy == null || selectedMoveBrain == null)
        {
            HideHologram();
            return;
        }

        if (requireMovementAction && selectedMoveBrain.GetMoveActionsRemaining() <= 0)
        {
            HideHologram();
            return;
        }

        if (!selectedMoveBrain.TryGetPredictedMoveTile(out Vector2Int predictedTile))
        {
            HideHologram();
            return;
        }

        GridManager gridManager = selectedMoveBrain.GetGridManager();
        if (gridManager == null)
        {
            HideHologram();
            return;
        }

        ShowHologramAt(gridManager, predictedTile);
    }


    // ============================================================
    // UPDATE SPRITE
    // ============================================================

    private void UpdateHologramSprite()
    {
        if (selectedEnemy == null) return;

        EnsureHologramInstance();
        if (hologramSpriteRenderer == null) return;

        if (cachedEnemySpriteRenderer == null)
        {
            cachedEnemySpriteRenderer = selectedEnemy.GetComponentInChildren<SpriteRenderer>();
            if (cachedEnemySpriteRenderer == null)
            {
                HideHologram();
                return;
            }
        }

        hologramSpriteRenderer.sprite = cachedEnemySpriteRenderer.sprite;
        hologramSpriteRenderer.flipX = cachedEnemySpriteRenderer.flipX;
        hologramSpriteRenderer.flipY = cachedEnemySpriteRenderer.flipY;
        hologramSpriteRenderer.sortingLayerID = cachedEnemySpriteRenderer.sortingLayerID;
        hologramSpriteRenderer.sortingOrder = cachedEnemySpriteRenderer.sortingOrder + 1;
    }


    // ============================================================
    // SHOW HOLOGRAM
    // ============================================================

    private void ShowHologramAt(GridManager gridManager, Vector2Int tile)
    {
        if (gridManager == null) return;

        EnsureHologramInstance();
        if (hologramInstance == null || hologramSpriteRenderer == null) return;

        Vector3 worldPosition = gridManager.GridToWorldPosition(tile);

        hologramInstance.transform.position = worldPosition + hologramOffset;
        hologramInstance.transform.localScale = hologramBaseScale;

        SetHologramAlpha(maxAlpha);

        if (!hologramInstance.activeSelf)
        {
            hologramInstance.SetActive(true);
        }

        UpdatePathLine(worldPosition);
    }


    // ============================================================
    // PATH LINE
    // ============================================================

    private void UpdatePathLine(Vector3 destination)
    {
        if (selectedEnemy == null) return;

        if (pathLineRenderer == null)
        {
            FindLineRenderer();
        }

        if (pathLineRenderer == null) return;

        pathLineRenderer.positionCount = 2;
        pathLineRenderer.SetPosition(0, selectedEnemy.transform.position + lineOffset);
        pathLineRenderer.SetPosition(1, destination + lineOffset);
        pathLineRenderer.enabled = true;
    }


    // ============================================================
    // EFFECTS
    // ============================================================

    private void UpdateHologramEffects()
    {
        if (hologramInstance == null || hologramSpriteRenderer == null || !hologramInstance.activeSelf) return;

        if (pulseHologram)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            hologramInstance.transform.localScale = hologramBaseScale * pulse;
        }

        float pingPong = Mathf.PingPong(Time.time * transparencySpeed, 1f);
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, pingPong);
        SetHologramAlpha(alpha);
    }


    // ============================================================
    // ALPHA
    // ============================================================

    private void SetHologramAlpha(float alpha)
    {
        if (hologramSpriteRenderer == null) return;

        Color color = hologramSpriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        hologramSpriteRenderer.color = color;
    }


    // ============================================================
    // HIDE
    // ============================================================

    private void HideHologram()
    {
        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram = hologramPool[i];
            if (hologram != null)
            {
                hologram.SetActive(false);
            }
        }

        if (hologramInstance != null)
        {
            hologramInstance.transform.localScale = hologramBaseScale;
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
        return hologramInstance != null && hologramInstance.activeSelf;
    }


    public void SetHologramVisible(bool visible)
    {
        showHologram = visible;

        if (!visible)
        {
            HideHologram();
        }
    }
}