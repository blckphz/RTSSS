
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class UnitSelectorManager : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference upAction;
    [SerializeField] private InputActionReference downAction;
    [SerializeField] private InputActionReference leftAction;
    [SerializeField] private InputActionReference rightAction;

    [Header("Navigation")]
    [SerializeField, Min(0f)]
    private float directionWeight = 1000f;

    [SerializeField, Range(0f, 89f)]
    private float maximumNavigationAngle = 85f;

    [SerializeField]
    private bool preventSelectionWhileMoving = true;

    [SerializeField]
    private bool preventSelectionWhileRotating = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<HoverInfoTrigger> selectableObjects = new();

    private void OnEnable()
    {
        EnableAction(upAction);
        EnableAction(downAction);
        EnableAction(leftAction);
        EnableAction(rightAction);
    }

    private void OnDisable()
    {
        DisableAction(upAction);
        DisableAction(downAction);
        DisableAction(leftAction);
        DisableAction(rightAction);
    }

    private void Update()
    {
        Vector2Int direction;
        string inputName;

        if (WasPressed(upAction))
        {
            direction = Vector2Int.up;
            inputName = "UP";
        }
        else if (WasPressed(downAction))
        {
            direction = Vector2Int.down;
            inputName = "DOWN";
        }
        else if (WasPressed(leftAction))
        {
            direction = Vector2Int.left;
            inputName = "LEFT";
        }
        else if (WasPressed(rightAction))
        {
            direction = Vector2Int.right;
            inputName = "RIGHT";
        }
        else
        {
            return;
        }

        if (UIManager.CurrentSelection == null)
        {
            Log("No current selection. Navigation ignored.");
            return;
        }

        if (IsNavigationBlocked())
        {
            Log("Navigation blocked by movement or rotation.");
            return;
        }

        SelectInDirection(direction, inputName);
    }

    // ------------------------------------------------------------
    // INPUT
    // ------------------------------------------------------------

    private void EnableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Enable();
    }

    private void DisableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Disable();
    }

    private bool WasPressed(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.WasPressedThisFrame();
    }

    // ------------------------------------------------------------
    // LOCKS
    // ------------------------------------------------------------

    private bool IsNavigationBlocked()
    {
        if (preventSelectionWhileMoving)
        {
            if (UnitMoveBrain.IsAnyUnitMoving)
                return true;

            if (UnitMoveBrainManager.IsUnitMoving)
                return true;
        }

        if (preventSelectionWhileRotating &&
            BoardViewController.Instance != null &&
            BoardViewController.Instance.IsRotating())
            return true;

        return false;
    }

    // ------------------------------------------------------------
    // DIRECTIONAL SELECTION
    // ------------------------------------------------------------

    private void SelectInDirection(
        Vector2Int inputDirection,
        string inputName)
    {
        HoverInfoTrigger currentTrigger = UIManager.CurrentSelection;

        if (currentTrigger == null)
            return;

        Vector2 currentPosition =
            GetSelectablePosition(currentTrigger);

        Vector2Int desiredDirection =
            RotateDirectionForBoard(inputDirection);

        HoverInfoTrigger[] allTriggers =
            FindObjectsByType<HoverInfoTrigger>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        selectableObjects.Clear();

        foreach (HoverInfoTrigger trigger in allTriggers)
        {
            if (IsValidSelectable(trigger))
                selectableObjects.Add(trigger);
        }

        Log(
            "INPUT: " + inputName +
            " | Current: " + currentTrigger.name +
            " | Position: " + currentPosition +
            " | Rotation: " +
            (BoardViewController.Instance != null
                ? BoardViewController.Instance.GetCurrentRotation()
                : 0) +
            " | Direction: " + desiredDirection +
            " | Selectable objects: " + selectableObjects.Count
        );

        HoverInfoTrigger bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (HoverInfoTrigger candidate in selectableObjects)
        {
            if (candidate == currentTrigger)
                continue;

            Vector2 candidatePosition =
                GetSelectablePosition(candidate);

            Vector2 difference =
                candidatePosition - currentPosition;

            if (difference.sqrMagnitude < 0.001f)
                continue;

            Vector2 normalizedDifference =
                difference.normalized;

            float dot = Vector2.Dot(
                normalizedDifference,
                (Vector2)desiredDirection
            );

            if (dot <= 0f)
            {
                Log(
                    "Rejected " + candidate.name +
                    ": behind requested direction."
                );
                continue;
            }

            float angle =
                Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) *
                Mathf.Rad2Deg;

            if (angle > maximumNavigationAngle)
            {
                Log(
                    "Rejected " + candidate.name +
                    ": angle " + angle.ToString("F1") +
                    " exceeds limit."
                );
                continue;
            }

            float distance = difference.magnitude;

            float score =
                angle * directionWeight + distance;

            Log(
                "Candidate: " + candidate.name +
                " | Position: " + candidatePosition +
                " | Angle: " + angle.ToString("F1") +
                " | Distance: " + distance.ToString("F1") +
                " | Score: " + score.ToString("F1")
            );

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        if (bestTarget == null)
        {
            Log("No selectable object found in direction: " + inputName);
            return;
        }

        Log("SELECTING: " + bestTarget.name);

        UIManager.SelectObject(bestTarget);
    }

    // ------------------------------------------------------------
    // POSITION
    // ------------------------------------------------------------

    private Vector2 GetSelectablePosition(HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return Vector2.zero;

        AttackUnit unit = trigger.GetAttackUnit();

        // Characters use their logical grid position.
        if (unit != null)
        {
            Vector2Int gridPosition =
                unit.GetLogicalGridPosition();

            return gridPosition;
        }

        // Obstacles without AttackUnit use their world position.
        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            Vector3 worldPosition =
                trigger.transform.position;

            Vector2Int gridPosition =
                gridManager.WorldToGridPosition(worldPosition);

            return gridPosition;
        }

        Log(
            "WARNING: No GridManager found. Using world position for " +
            trigger.name
        );

        Vector3 position = trigger.transform.position;

        return new Vector2(position.x, position.y);
    }

    // ------------------------------------------------------------
    // BOARD ROTATION
    // ------------------------------------------------------------

    private Vector2Int RotateDirectionForBoard(Vector2Int direction)
    {
        if (BoardViewController.Instance == null)
            return direction;

        int rotation =
            BoardViewController.Instance.GetCurrentRotation();

        rotation = ((rotation % 360) + 360) % 360;

        switch (rotation)
        {
            case 90:
                return new Vector2Int(
                    direction.y,
                    -direction.x
                );

            case 180:
                return new Vector2Int(
                    -direction.x,
                    -direction.y
                );

            case 270:
                return new Vector2Int(
                    -direction.y,
                    direction.x
                );

            default:
                return direction;
        }
    }

    // ------------------------------------------------------------
    // VALIDATION
    // ------------------------------------------------------------

    private bool IsValidSelectable(HoverInfoTrigger trigger)
    {
        if (trigger == null)
            return false;

        if (!trigger.gameObject.activeInHierarchy)
            return false;

        // Obstacles do not need AttackUnit or CharacterData.
        // Any active object with HoverInfoTrigger is selectable.
        return true;
    }

    // ------------------------------------------------------------
    // DEBUG
    // ------------------------------------------------------------

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log("[UnitSelector] " + message, this);
    }
}
