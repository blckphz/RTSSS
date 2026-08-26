using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridChainHighlight : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private string abilitySpawnPointName = "AbilitySpawnPoint";

    // ============================================================
    // VISUAL SETTINGS
    // ============================================================

    [Header("Visual")]
    [SerializeField]
    private float lineHeight = 0.15f;

    [SerializeField]
    private float lineWidth = 0.08f;

    // ============================================================
    // STATE
    // ============================================================

    private GameObject user;
    private ChainLightning chainLightning;
    private GridManager gridManager;
    private Transform abilitySpawnPoint;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
        }

        gridManager = FindFirstObjectByType<GridManager>();
    }

    private void Update()
    {
        UpdatePreview();
    }

    // ============================================================
    // BEGIN PREVIEW
    // ============================================================

    public void BeginPreview(
        GameObject user,
        ChainLightning ability)
    {
        this.user = user;
        this.chainLightning = ability;

        FindAbilitySpawnPoint();

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        UpdatePreview();
    }

    // ============================================================
    // FIND ABILITY SPAWN POINT
    // ============================================================

    private void FindAbilitySpawnPoint()
    {
        abilitySpawnPoint = null;

        if (user == null)
        {
            return;
        }

        Transform[] children = user.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null)
            {
                continue;
            }

            if (child.name == abilitySpawnPointName)
            {
                abilitySpawnPoint = child;
                return;
            }
        }
    }

    // ============================================================
    // END PREVIEW
    // ============================================================

    public void EndPreview()
    {
        user = null;
        chainLightning = null;
        abilitySpawnPoint = null;

        Hide();
    }

    // ============================================================
    // UPDATE PREVIEW
    // ============================================================

    private void UpdatePreview()
    {
        if (user == null || chainLightning == null)
        {
            Hide();
            return;
        }

        if (abilitySpawnPoint == null)
        {
            FindAbilitySpawnPoint();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (mainCamera == null || gridManager == null)
        {
            Hide();
            return;
        }

        if (Mouse.current == null)
        {
            Hide();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        RaycastHit2D hit = Physics2D.GetRayIntersection(
            ray,
            Mathf.Infinity
        );

        if (hit.collider == null)
        {
            Hide();
            return;
        }

        HoverInfoTrigger trigger = hit.collider.GetComponentInParent<HoverInfoTrigger>();

        if (trigger == null)
        {
            Hide();
            return;
        }

        AttackUnit targetUnit = trigger.GetAttackUnit();

        if (targetUnit == null)
        {
            Hide();
            return;
        }

        GameObject target = targetUnit.gameObject;

        if (target == user)
        {
            Hide();
            return;
        }

        AttackUnit userUnit = user.GetComponent<AttackUnit>();

        if (userUnit == null)
        {
            Hide();
            return;
        }

        if (targetUnit.GetTeam() == userUnit.GetTeam())
        {
            Hide();
            return;
        }

        List<GameObject> chain = chainLightning.GetChainPreview(
            user,
            target,
            gridManager
        );

        if (chain == null || chain.Count == 0)
        {
            Hide();
            return;
        }

        DrawChain(chain);
    }

    // ============================================================
    // DRAW CHAIN
    // ============================================================

    private void DrawChain(List<GameObject> chain)
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (user == null || chain == null || chain.Count == 0)
        {
            Hide();
            return;
        }

        if (abilitySpawnPoint == null)
        {
            FindAbilitySpawnPoint();
        }

        if (abilitySpawnPoint == null)
        {
            Hide();
            return;
        }

        lineRenderer.positionCount = chain.Count + 1;

        Vector3 spawnPosition = abilitySpawnPoint.position;
        spawnPosition.y += lineHeight;

        lineRenderer.SetPosition(0, spawnPosition);

        for (int i = 0; i < chain.Count; i++)
        {
            GameObject target = chain[i];

            if (target == null)
            {
                continue;
            }

            Vector3 position = target.transform.position;
            position.y += lineHeight;

            lineRenderer.SetPosition(i + 1, position);
        }

        lineRenderer.enabled = true;
    }

    // ============================================================
    // HIDE
    // ============================================================

    private void Hide()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }
}