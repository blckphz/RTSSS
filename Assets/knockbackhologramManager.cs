
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class KnockbackHologramManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridHighlightManager highlightManager;
    [SerializeField] private GridManager gridManager;

    [Header("Hologram")]
    [SerializeField] private GameObject hologramPrefab;
    [SerializeField]
    private Vector3 hologramOffset =
        new Vector3(0f, 0.05f, 0f);

    [Header("Appearance")]
    [SerializeField] private bool copyPlayerSprite = true;

    [SerializeField, Range(0f, 1f)]
    private float hologramAlpha = 0.65f;

    [Header("Pulse")]
    [SerializeField] private bool pulseHologram = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.08f;

    private GameObject hologramInstance;
    private SpriteRenderer hologramRenderer;

    private Vector3 originalScale;

    private Vector2Int lastBombTile;
    private Vector2Int lastLandingTile;

    private bool hasLastBombTile;
    private bool isShowing;

    private void Awake()
    {
        FindReferences();
        CreateHologram();
        Hide();
    }

    private void Update()
    {
        if (highlightManager == null || gridManager == null)
        {
            Hide();
            return;
        }

        if (hologramInstance == null)
            return;

        if (isShowing)
            UpdatePulse();

        // Only show the preview for BombAttack.
        BombAttack bomb =
            highlightManager.GetCurrentAbility() as BombAttack;

        if (bomb == null)
        {
            Hide();
            return;
        }

        GameObject owner =
            highlightManager.GetCurrentRangeUser();

        if (owner == null)
        {
            Hide();
            return;
        }

        if (Mouse.current == null || Camera.main == null)
        {
            Hide();
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Camera cam = Camera.main;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(
            new Vector3(
                mousePosition.x,
                mousePosition.y,
                Mathf.Abs(cam.transform.position.z)
            )
        );

        Vector2Int bombTile =
            gridManager.WorldToGridPosition(mouseWorld);

        if (!highlightManager.IsAbilityCell(bombTile))
        {
            Hide();
            return;
        }

        if (hasLastBombTile &&
            lastBombTile == bombTile &&
            isShowing)
        {
            return;
        }

        lastBombTile = bombTile;
        hasLastBombTile = true;

        CalculateLandingPosition(
            bomb,
            owner,
            bombTile
        );
    }

    private void FindReferences()
    {
        if (highlightManager == null)
        {
            highlightManager =
                FindFirstObjectByType<GridHighlightManager>();
        }

        if (gridManager == null && highlightManager != null)
        {
            gridManager =
                highlightManager.GetGridManager();
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }
    }

    // ============================================================
    // CREATE HOLOGRAM
    // ============================================================

    private void CreateHologram()
    {
        if (hologramPrefab == null)
        {
            Debug.LogError(
                "[KnockbackHologramManager] Hologram prefab missing.",
                this
            );

            return;
        }

        hologramInstance =
            Instantiate(hologramPrefab, transform);

        hologramRenderer =
            hologramInstance.GetComponentInChildren<SpriteRenderer>();

        if (hologramRenderer == null)
        {
            Debug.LogError(
                "[KnockbackHologramManager] Prefab has no SpriteRenderer.",
                hologramInstance
            );

            Destroy(hologramInstance);
            hologramInstance = null;

            return;
        }

        originalScale =
            hologramInstance.transform.localScale;

        SetAlpha();

        hologramInstance.SetActive(false);
    }

    // ============================================================
    // CALCULATE LANDING TILE
    // ============================================================

    private void CalculateLandingPosition(
        BombAttack bomb,
        GameObject owner,
        Vector2Int bombTile)
    {
        if (bomb == null || owner == null)
        {
            Hide();
            return;
        }

        GameObject explosionPrefab =
            bomb.GetExplosionPrefab();

        if (explosionPrefab == null)
        {
            Hide();
            return;
        }

        ExplosionAttack explosion =
            explosionPrefab.GetComponent<ExplosionAttack>();

        if (explosion == null)
        {
            Hide();
            return;
        }

        int effectiveRadius =
            bomb.GetEffectiveExplosionRadius(owner);

        HealthManager[] players =
            FindObjectsByType<HealthManager>(
                FindObjectsSortMode.None
            );

        foreach (HealthManager health in players)
        {
            if (health == null ||
                !health.gameObject.activeInHierarchy ||
                !health.IsPlayerCharacter() ||
                health.IsDead())
            {
                continue;
            }

            GameObject player = health.gameObject;

            bool predicted =
                explosion.TryGetPlayerKnockbackLandingTile(
                    owner,
                    player,
                    bombTile,
                    effectiveRadius,
                    out Vector2Int landingTile
                );

            if (!predicted)
                continue;

            ShowLandingPosition(
                player,
                landingTile
            );

            return;
        }

        Hide();
    }

    // ============================================================
    // SHOW HOLOGRAM
    // ============================================================

    private void ShowLandingPosition(
        GameObject player,
        Vector2Int landingTile)
    {
        if (player == null ||
            hologramInstance == null ||
            hologramRenderer == null)
        {
            Hide();
            return;
        }

        if (copyPlayerSprite)
        {
            SpriteRenderer playerRenderer =
                player.GetComponentInChildren<SpriteRenderer>();

            if (playerRenderer != null)
            {
                hologramRenderer.sprite =
                    playerRenderer.sprite;

                hologramRenderer.flipX =
                    playerRenderer.flipX;

                hologramRenderer.flipY =
                    playerRenderer.flipY;

                hologramRenderer.sortingLayerID =
                    playerRenderer.sortingLayerID;

                hologramRenderer.sortingOrder =
                    playerRenderer.sortingOrder;
            }
        }

        Vector3 worldPosition =
            gridManager.GridToWorldPosition(landingTile);

        hologramInstance.transform.position =
            worldPosition + hologramOffset;

        lastLandingTile = landingTile;

        if (!hologramInstance.activeSelf)
            hologramInstance.SetActive(true);

        isShowing = true;

        UpdatePulse();
    }

    // ============================================================
    // PULSE
    // ============================================================

    private void UpdatePulse()
    {
        if (!pulseHologram || hologramInstance == null)
            return;

        float pulse =
            1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        hologramInstance.transform.localScale =
            originalScale * pulse;
    }

    // ============================================================
    // ALPHA
    // ============================================================

    private void SetAlpha()
    {
        if (hologramRenderer == null)
            return;

        Color color = hologramRenderer.color;
        color.a = hologramAlpha;

        hologramRenderer.color = color;
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void Hide()
    {
        isShowing = false;
        hasLastBombTile = false;

        if (hologramInstance != null)
        {
            hologramInstance.SetActive(false);

            hologramInstance.transform.localScale =
                originalScale;
        }
    }

    // ============================================================
    // PUBLIC ACCESS
    // ============================================================

    public bool IsShowing()
    {
        return isShowing;
    }

    public Vector2Int GetPredictedLandingTile()
    {
        return lastLandingTile;
    }
}
