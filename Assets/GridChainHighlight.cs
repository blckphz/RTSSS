using System.Collections.Generic;
using UnityEngine;

public class GridChainHighlight : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private LineRenderer lineRenderer;


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


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer =
                GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;

            lineRenderer.startWidth =
                lineWidth;

            lineRenderer.endWidth =
                lineWidth;

            lineRenderer.useWorldSpace =
                true;

            lineRenderer.enabled =
                false;
        }

        gridManager =
            FindFirstObjectByType<GridManager>();
    }


    private void Update()
    {
        UpdateSelectedTarget();
    }


    // ============================================================
    // BEGIN PREVIEW
    // ============================================================

    public void BeginPreview(
        GameObject user,
        ChainLightning ability)
    {
        this.user =
            user;

        this.chainLightning =
            ability;

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        UpdateSelectedTarget();
    }


    // ============================================================
    // END PREVIEW
    // ============================================================

    public void EndPreview()
    {
        user = null;
        chainLightning = null;

        Hide();
    }


    // ============================================================
    // SELECTED TARGET
    // ============================================================

    private void UpdateSelectedTarget()
    {
        if (
            user == null ||
            chainLightning == null
        )
        {
            Hide();
            return;
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET CURRENTLY SELECTED UNIT
        // --------------------------------------------------------

        HoverInfoTrigger selected =
            UIManager.CurrentSelection;


        if (selected == null)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET TARGET GAMEOBJECT
        // --------------------------------------------------------

        AttackUnit selectedAttackUnit =
            selected.GetAttackUnit();


        if (selectedAttackUnit == null)
        {
            Hide();
            return;
        }


        GameObject selectedTarget =
            selectedAttackUnit.gameObject;


        // --------------------------------------------------------
        // DON'T TARGET USER
        // --------------------------------------------------------

        if (selectedTarget == user)
        {
            Hide();
            return;
        }


        // --------------------------------------------------------
        // GET CHAIN
        // --------------------------------------------------------

        List<GameObject> chain =
            chainLightning.GetChainPreview(
                user,
                selectedTarget,
                gridManager
            );


        // --------------------------------------------------------
        // NOTHING TO SHOW
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
        // DRAW
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
            return;
        }

        lineRenderer.positionCount =
            chain.Count;


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

            position.y +=
                lineHeight;

            lineRenderer.SetPosition(
                i,
                position
            );
        }

        lineRenderer.enabled =
            true;
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

        lineRenderer.positionCount =
            0;

        lineRenderer.enabled =
            false;
    }
}