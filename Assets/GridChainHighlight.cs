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

        gridManager =
            FindFirstObjectByType<GridManager>();
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
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        Debug.Log(
            "[GridChainHighlight] BEGIN PREVIEW -> " +
            "User: " +
            (user == null ? "NULL" : user.name) +
            " | Ability: " +
            (ability == null
                ? "NULL"
                : ability.GetAbilityName()) +
            " | Spawn Point: " +
            (abilitySpawnPoint == null
                ? "NOT FOUND"
                : abilitySpawnPoint.name)
        );

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

        Transform[] children =
            user.GetComponentsInChildren<Transform>(
                true
            );

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

        Debug.LogWarning(
            "[GridChainHighlight] Could not find child '" +
            abilitySpawnPointName +
            "' on " +
            user.name
        );
    }


    // ============================================================
    // END PREVIEW
    // ============================================================

    public void EndPreview()
    {
        Debug.Log(
            "[GridChainHighlight] END PREVIEW"
        );

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
        // --------------------------------------------------------
        // NO ACTIVE PREVIEW
        // --------------------------------------------------------

        if (
            user == null ||
            chainLightning == null
        )
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // SPAWN POINT
        // --------------------------------------------------------

        if (abilitySpawnPoint == null)
        {
            FindAbilitySpawnPoint();
        }


        // --------------------------------------------------------
        // CAMERA
        // --------------------------------------------------------

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }


        // --------------------------------------------------------
        // GRID
        // --------------------------------------------------------

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (
            mainCamera == null ||
            gridManager == null
        )
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // MOUSE
        // --------------------------------------------------------

        if (Mouse.current == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // MOUSE POSITION
        // --------------------------------------------------------

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // --------------------------------------------------------
        // SCREEN -> WORLD RAY
        // --------------------------------------------------------

        Ray ray =
            mainCamera.ScreenPointToRay(
                mousePosition
            );


        // --------------------------------------------------------
        // 2D RAYCAST
        // --------------------------------------------------------

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity
            );


        if (hit.collider == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // FIND UNIT
        // --------------------------------------------------------

        HoverInfoTrigger trigger =
            hit.collider.GetComponentInParent<
                HoverInfoTrigger
            >();


        if (trigger == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET TARGET UNIT
        // --------------------------------------------------------

        AttackUnit targetUnit =
            trigger.GetAttackUnit();


        if (targetUnit == null)
        {
            Hide();
            return;
        }


        GameObject target =
            targetUnit.gameObject;


        // --------------------------------------------------------
        // DON'T TARGET USER
        // --------------------------------------------------------

        if (target == user)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET USER UNIT
        // --------------------------------------------------------

        AttackUnit userUnit =
            user.GetComponent<AttackUnit>();


        if (userUnit == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // TARGET MUST BE ENEMY
        // --------------------------------------------------------

        if (
            targetUnit.GetTeam() ==
            userUnit.GetTeam()
        )
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET CHAIN PREVIEW
        // --------------------------------------------------------

        List<GameObject> chain =
            chainLightning.GetChainPreview(
                user,
                target,
                gridManager
            );


        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        Debug.Log(
            "[GridChainHighlight] Hovered target: " +
            target.name +
            " | Chain count: " +
            (chain == null ? 0 : chain.Count)
        );


        // --------------------------------------------------------
        // NOTHING TO DRAW
        // --------------------------------------------------------

        if (
            chain == null ||
            chain.Count == 0
        )
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // DRAW CHAIN
        // --------------------------------------------------------

        DrawChain(chain);
    }


    // ============================================================
    // DRAW CHAIN
    // ============================================================

    private void DrawChain(
        List<GameObject> chain)
    {
        if (lineRenderer == null)
        {
            Debug.LogWarning(
                "[GridChainHighlight] No LineRenderer!"
            );

            return;
        }


        if (
            user == null ||
            chain == null ||
            chain.Count == 0
        )
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // MAKE SURE SPAWN POINT EXISTS
        // --------------------------------------------------------

        if (abilitySpawnPoint == null)
        {
            FindAbilitySpawnPoint();
        }


        if (abilitySpawnPoint == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // POINT COUNT
        //
        // 1 point = AbilitySpawnPoint
        // + 1 point for every chain target
        // --------------------------------------------------------

        lineRenderer.positionCount =
            chain.Count + 1;


        // --------------------------------------------------------
        // FIRST POINT = ABILITY SPAWN POINT
        // --------------------------------------------------------

        Vector3 spawnPosition =
            abilitySpawnPoint.position;

        spawnPosition.y += lineHeight;

        lineRenderer.SetPosition(
            0,
            spawnPosition
        );


        // --------------------------------------------------------
        // REST OF POINTS = CHAIN TARGETS
        // --------------------------------------------------------

        for (
            int i = 0;
            i < chain.Count;
            i++
        )
        {
            GameObject target =
                chain[i];

            if (target == null)
            {
                continue;
            }


            Vector3 position =
                target.transform.position;

            position.y += lineHeight;


            lineRenderer.SetPosition(
                i + 1,
                position
            );
        }


        // --------------------------------------------------------
        // SHOW LINE
        // --------------------------------------------------------

        lineRenderer.enabled = true;


        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        Debug.Log(
            "[GridChainHighlight] DRAWING LINE -> " +
            "AbilitySpawnPoint + " +
            chain.Count +
            " targets = " +
            (chain.Count + 1) +
            " points."
        );
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