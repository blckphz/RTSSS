using UnityEngine;
using UnityEngine.InputSystem;

public class HoverManager : MonoBehaviour
{
    private Camera hoverCamera;
    private HoverInfoTrigger currentHoveredTrigger;

    private void Awake()
    {
        hoverCamera = Camera.main;
    }

    private void Update()
    {
        UpdateHoveredTrigger();
    }

    private void UpdateHoveredTrigger()
    {
        if (Mouse.current == null)
        {
            ClearHoveredTrigger();
            return;
        }

        if (hoverCamera == null)
        {
            hoverCamera = Camera.main;
        }

        if (hoverCamera == null)
        {
            ClearHoveredTrigger();
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            hoverCamera.ScreenPointToRay(mousePosition);

        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity
            );

        HoverInfoTrigger newHoveredTrigger = null;

        if (hit.collider != null)
        {
            newHoveredTrigger =
                hit.collider.GetComponentInParent<HoverInfoTrigger>();
        }

        if (newHoveredTrigger == currentHoveredTrigger)
        {
            return;
        }

        if (currentHoveredTrigger != null)
        {
            currentHoveredTrigger.SetHoveredFromManager(false);
        }

        currentHoveredTrigger = newHoveredTrigger;

        if (currentHoveredTrigger != null)
        {
            currentHoveredTrigger.SetHoveredFromManager(true);
        }
    }

    private void ClearHoveredTrigger()
    {
        if (currentHoveredTrigger == null)
        {
            return;
        }

        currentHoveredTrigger.SetHoveredFromManager(false);
        currentHoveredTrigger = null;
    }

    private void OnDisable()
    {
        ClearHoveredTrigger();
    }
}
