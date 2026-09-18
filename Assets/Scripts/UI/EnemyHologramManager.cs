using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    [Tooltip("Number of holograms created when the manager starts.")]
    [SerializeField]
    private int poolSize = 1;

    private readonly List<GameObject> hologramPool =
        new List<GameObject>();

    private readonly List<SpriteRenderer> hologramSpriteRenderers =
        new List<SpriteRenderer>();

    private GameObject hologramInstance;

    private SpriteRenderer hologramSpriteRenderer;


    // ============================================================
    // LINE RENDERER
    // ============================================================

    [Header("Path Line Renderer")]

    [Tooltip("GameObject containing the LineRenderer.")]
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
    // HOLOGRAM PULSE
    // ============================================================

    [Header("Hologram Pulse")]
    [SerializeField]
    private bool pulseHologram = true;

    [Tooltip("Speed of the hologram scale pulse.")]
    [SerializeField]
    private float pulseSpeed = 1f;

    [Tooltip("Amount of scale variation.")]
    [SerializeField]
    private float pulseAmount = 0.08f;


    // ============================================================
    // HOLOGRAM TRANSPARENCY
    // ============================================================

    [Header("Hologram Transparency")]
    [Tooltip("Speed of the transparency ping-pong.")]
    [SerializeField]
    private float transparencySpeed = 3f;

    [Tooltip("Maximum hologram alpha.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float maxAlpha = 0.7f;

    [Tooltip("Minimum hologram alpha.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float minAlpha = 0.5f;


    // ============================================================
    // STATE
    // ============================================================

    private AttackUnit selectedEnemy;

    private UnitMoveBrain selectedMoveBrain;

    private Vector3 hologramBaseScale = Vector3.one;


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

        if (selectedEnemy == null ||
            selectedMoveBrain == null)
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
    // FIND LINE RENDERER
    // ============================================================

    private void FindLineRenderer()
    {
        // If manually assigned, use that.
        if (pathLineRenderer != null)
            return;

        // Try the assigned manager GameObject.
        if (lineRendererManagerObject != null)
        {
            pathLineRenderer =
                lineRendererManagerObject.GetComponent<LineRenderer>();

            if (pathLineRenderer == null)
            {
                pathLineRenderer =
                    lineRendererManagerObject.GetComponentInChildren<LineRenderer>();
            }
        }

        if (pathLineRenderer == null)
        {
            Debug.LogWarning(
                "EnemyHologramManager: Could not find LineRenderer. " +
                "Assign the GameObject containing the LineRenderer " +
                "to 'Line Renderer Manager Object'.",
                this
            );
        }
    }


    // ============================================================
    // CREATE HOLOGRAM POOL
    // ============================================================

    private void CreateHologramPool()
    {
        if (hologramPrefab == null)
        {
            Debug.LogWarning(
                "EnemyHologramManager: Hologram Prefab is not assigned.",
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
                "Enemy Movement Hologram " + i;

            hologram.SetActive(false);

            SpriteRenderer spriteRenderer =
                hologram.GetComponentInChildren<SpriteRenderer>();

            hologramPool.Add(hologram);

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
    // GET HOLOGRAM FROM POOL
    // ============================================================

    private void EnsureHologramInstance()
    {
        if (hologramInstance != null)
            return;

        if (hologramPool.Count == 0)
        {
            CreateHologramPool();
        }

        if (hologramPool.Count == 0)
            return;

        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram =
                hologramPool[i];

            if (hologram == null)
                continue;

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

        // If all pooled holograms are active,
        // reuse the first one.
        hologramInstance =
            hologramPool[0];

        hologramSpriteRenderer =
            hologramSpriteRenderers[0];

        hologramBaseScale =
            hologramInstance.transform.localScale;
    }


    // ============================================================
    // SELECTION EVENT
    // ============================================================

    private void OnSelectionChanged(
        HoverInfoTrigger trigger,
        bool selected)
    {
        if (trigger == null)
            return;

        AttackUnit unit =
            trigger.GetAttackUnit();

        if (!selected)
        {
            if (unit == selectedEnemy)
            {
                ClearSelection();
            }

            return;
        }

        // Only allow enemy units.
        if (unit == null ||
            unit.GetTeam() == Team.Player ||
            unit.GetTeam() == Team.Ally ||
            unit.IsDead())
        {
            ClearSelection();
            return;
        }

        selectedEnemy = unit;

        selectedMoveBrain =
            selectedEnemy.GetComponent<UnitMoveBrain>();

        if (selectedMoveBrain == null)
        {
            Debug.LogWarning(
                "EnemyHologramManager: Selected enemy does not have a UnitMoveBrain.",
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
        if (selectedMoveBrain == null)
        {
            HideHologram();
            return;
        }

        bool hasPrediction =
            selectedMoveBrain.TryGetPredictedMoveTile(
                out Vector2Int predictedTile
            );

        if (!hasPrediction)
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
    // UPDATE HOLOGRAM SPRITE
    // ============================================================

    private void UpdateHologramSprite()
    {
        if (selectedEnemy == null)
            return;

        EnsureHologramInstance();

        if (hologramSpriteRenderer == null)
            return;

        SpriteRenderer enemySpriteRenderer =
            selectedEnemy.GetComponentInChildren<SpriteRenderer>();

        if (enemySpriteRenderer == null)
        {
            HideHologram();
            return;
        }

        hologramSpriteRenderer.sprite =
            enemySpriteRenderer.sprite;

        hologramSpriteRenderer.flipX =
            enemySpriteRenderer.flipX;

        hologramSpriteRenderer.flipY =
            enemySpriteRenderer.flipY;
    }


    // ============================================================
    // SHOW HOLOGRAM & LINE
    // ============================================================

    private void ShowHologramAt(
        GridManager gridManager,
        Vector2Int tile)
    {
        if (hologramPrefab == null)
            return;

        EnsureHologramInstance();

        if (hologramInstance == null ||
            hologramSpriteRenderer == null)
            return;

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(tile);

        // --------------------------------------------------------
        // HOLOGRAM POSITION
        // --------------------------------------------------------

        hologramInstance.transform.position =
            worldPosition +
            hologramOffset;

        // Reset scale.
        hologramInstance.transform.localScale =
            hologramBaseScale;

        // Start at 50% transparency.
        SetHologramAlpha(maxAlpha);

        if (!hologramInstance.activeSelf)
        {
            hologramInstance.SetActive(true);
        }


        // --------------------------------------------------------
        // LINE RENDERER
        // --------------------------------------------------------

        if (pathLineRenderer == null)
        {
            FindLineRenderer();
        }

        if (pathLineRenderer != null)
        {
            Vector3 startPos =
                selectedEnemy.transform.position +
                lineOffset;

            Vector3 endPos =
                worldPosition +
                lineOffset;

            pathLineRenderer.positionCount = 2;

            pathLineRenderer.SetPosition(
                0,
                startPos
            );

            pathLineRenderer.SetPosition(
                1,
                endPos
            );

            pathLineRenderer.enabled = true;
        }
    }


    // ============================================================
    // HOLOGRAM EFFECTS
    // ============================================================

    private void UpdateHologramEffects()
    {
        if (hologramInstance == null ||
            hologramSpriteRenderer == null ||
            !hologramInstance.activeSelf)
            return;


        // --------------------------------------------------------
        // SCALE PULSE
        // --------------------------------------------------------

        if (pulseHologram)
        {
            float pulse =
                1f +
                Mathf.Sin(Time.time * pulseSpeed) *
                pulseAmount;

            hologramInstance.transform.localScale =
                hologramBaseScale * pulse;
        }


        // --------------------------------------------------------
        // TRANSPARENCY PING-PONG
        // 50% -> 30% -> 50%
        // --------------------------------------------------------

        float transparencyPingPong =
            Mathf.PingPong(
                Time.time * transparencySpeed,
                1f
            );

        float alpha =
            Mathf.Lerp(
                maxAlpha,
                minAlpha,
                transparencyPingPong
            );

        SetHologramAlpha(alpha);
    }


    // ============================================================
    // SET HOLOGRAM ALPHA
    // ============================================================

    private void SetHologramAlpha(float alpha)
    {
        if (hologramSpriteRenderer == null)
            return;

        Color color =
            hologramSpriteRenderer.color;

        color.a = alpha;

        hologramSpriteRenderer.color =
            color;
    }


    // ============================================================
    // HIDE
    // ============================================================

    private void HideHologram()
    {
        // Disable all pooled holograms.
        for (int i = 0; i < hologramPool.Count; i++)
        {
            GameObject hologram =
                hologramPool[i];

            if (hologram != null &&
                hologram.activeSelf)
            {
                hologram.SetActive(false);
            }
        }

        // Reset scale.
        if (hologramInstance != null)
        {
            hologramInstance.transform.localScale =
                hologramBaseScale;
        }

        // Reset transparency.
        if (hologramSpriteRenderer != null)
        {
            SetHologramAlpha(maxAlpha);
        }

        // Disable standalone LineRenderer.
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;

            pathLineRenderer.enabled = false;
        }
    }


    // ============================================================
    // CLEAR SELECTION
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
}
