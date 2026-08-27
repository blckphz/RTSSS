using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class LineWalkPreview : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private Camera previewCamera;

    [SerializeField]
    private CanvasInfoManager canvasInfoManager;


    // ============================================================
    // MOVEMENT PREVIEW
    // ============================================================

    [Header("Movement Preview")]

    [Tooltip(
        "Only show the preview when a unit is currently selected."
    )]
    [SerializeField]
    private bool requireSelectedUnit = true;

    [Tooltip(
        "Only show movement previews for Player units."
    )]
    [SerializeField]
    private bool onlyShowForPlayerUnits = true;

    [Tooltip(
        "Hide the preview after the unit has consumed its movement."
    )]
    [SerializeField]
    private bool hideWhenNoMovementRemaining = true;

    [Tooltip(
        "Hide the movement path while an ability is selected."
    )]
    [SerializeField]
    private bool hideWhenAbilitySelected = true;

    [Tooltip(
        "If the mouse is outside movement range, draw the path only up to the furthest reachable point."
    )]
    [SerializeField]
    private bool clampPathToMoveRange = true;


    // ============================================================
    // RANGE
    // ============================================================

    [Header("Range")]

    [Tooltip(
        "Use Manhattan movement range. Range forms a diamond around the unit."
    )]
    [SerializeField]
    private bool useManhattanRange = true;


    // ============================================================
    // LINE
    // ============================================================

    [Header("Line")]

    [SerializeField]
    private float lineHeight = 0.05f;

    // DO NOT CHANGE THESE AT RUNTIME.
    // Width remains controlled by the Inspector.
    [SerializeField]
    private float startWidth = 0.12f;

    [SerializeField]
    private float endWidth = 0.12f;

    [SerializeField]
    private bool useWorldSpace = true;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugPreview = false;


    // ============================================================
    // PATHFINDING CACHE
    // ============================================================

    private readonly List<Vector2Int> path =
        new List<Vector2Int>(64);

    private readonly List<Vector2Int> previewPath =
        new List<Vector2Int>(64);

    private readonly List<Vector2Int> openList =
        new List<Vector2Int>(128);

    private readonly HashSet<Vector2Int> closedSet =
        new HashSet<Vector2Int>();

    private readonly Dictionary<Vector2Int, Vector2Int> cameFrom =
        new Dictionary<Vector2Int, Vector2Int>();

    private readonly Dictionary<Vector2Int, int> gScore =
        new Dictionary<Vector2Int, int>();


    // ============================================================
    // STATE
    // ============================================================

    private HoverInfoTrigger currentSelectedUnit;

    private Vector2Int currentHoveredCell;

    private bool hasHoveredCell;


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

        if (previewCamera == null)
        {
            previewCamera =
                Camera.main;
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }

        if (canvasInfoManager == null)
        {
            canvasInfoManager =
                FindFirstObjectByType<CanvasInfoManager>();
        }

        ConfigureLineRenderer();

        HideLine();
    }


    private void Update()
    {
        // --------------------------------------------------------
        // UPDATE CURRENT SELECTION
        // --------------------------------------------------------

        UpdateSelectedUnit();


        // --------------------------------------------------------
        // ABILITY SELECTED
        // --------------------------------------------------------

        if (hideWhenAbilitySelected)
        {
            if (canvasInfoManager == null)
            {
                canvasInfoManager =
                    FindFirstObjectByType<CanvasInfoManager>();
            }

            if (
                canvasInfoManager != null &&
                canvasInfoManager.HasSelectedAbility()
            )
            {
                HideLine();

                return;
            }
        }


        // --------------------------------------------------------
        // NO SELECTED UNIT
        // --------------------------------------------------------

        if (
            requireSelectedUnit &&
            currentSelectedUnit == null
        )
        {
            HideLine();
            return;
        }

        if (currentSelectedUnit == null)
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // PLAYER ONLY
        // --------------------------------------------------------

        if (onlyShowForPlayerUnits)
        {
            AttackUnit attackUnit =
                currentSelectedUnit.GetAttackUnit();

            if (
                attackUnit == null ||
                attackUnit.GetTeam() != Team.Player
            )
            {
                HideLine();
                return;
            }
        }


        // --------------------------------------------------------
        // MOVEMENT BRAIN
        // --------------------------------------------------------

        UnitMoveBrain moveBrain =
            currentSelectedUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // MOVEMENT USED
        // --------------------------------------------------------

        if (
            hideWhenNoMovementRemaining &&
            !moveBrain.CanMoveThisTurn()
        )
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // UPDATE MOUSE
        // --------------------------------------------------------

        UpdateHoveredCell();
    }


    // ============================================================
    // LINE SETUP
    // ============================================================

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace =
            useWorldSpace;

        // IMPORTANT:
        // We intentionally do NOT set startWidth/endWidth here.
        // Your Inspector values remain untouched.

        lineRenderer.positionCount =
            0;

        lineRenderer.alignment =
            LineAlignment.View;

        lineRenderer.textureMode =
            LineTextureMode.Stretch;

        lineRenderer.enabled =
            false;
    }


    // ============================================================
    // SELECTED UNIT
    // ============================================================

    private void UpdateSelectedUnit()
    {
        HoverInfoTrigger selected =
            UIManager.CurrentSelection;

        if (selected == currentSelectedUnit)
        {
            return;
        }

        currentSelectedUnit =
            selected;

        hasHoveredCell =
            false;

        path.Clear();

        previewPath.Clear();

        HideLine();

        if (debugPreview)
        {
            if (currentSelectedUnit != null)
            {
                Debug.Log(
                    "[LineWalkPreview] Selected unit: " +
                    currentSelectedUnit.gameObject.name,
                    currentSelectedUnit.gameObject
                );
            }
            else
            {
                Debug.Log(
                    "[LineWalkPreview] No selected unit."
                );
            }
        }
    }


    // ============================================================
    // MOUSE / TILE HOVER
    // ============================================================

    private void UpdateHoveredCell()
    {
        if (Mouse.current == null)
        {
            HideLine();
            return;
        }

        if (previewCamera == null)
        {
            previewCamera =
                Camera.main;
        }

        if (previewCamera == null)
        {
            HideLine();
            return;
        }

        if (gridManager == null)
        {
            HideLine();
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition =
            previewCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    Mathf.Abs(
                        previewCamera.transform.position.z
                    )
                )
            );

        Vector2Int hoveredCell =
            gridManager.WorldToGridPosition(
                worldPosition
            );


        // --------------------------------------------------------
        // OUTSIDE GRID
        // --------------------------------------------------------

        if (!gridManager.IsInsideGrid(
                hoveredCell))
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // SAME CELL AS LAST FRAME
        // --------------------------------------------------------

        if (
            hasHoveredCell &&
            hoveredCell == currentHoveredCell
        )
        {
            return;
        }

        currentHoveredCell =
            hoveredCell;

        hasHoveredCell =
            true;

        BuildPreview(
            hoveredCell
        );
    }


    // ============================================================
    // BUILD PREVIEW
    // ============================================================

    private void BuildPreview(
        Vector2Int destination
    )
    {
        // --------------------------------------------------------
        // ABILITY SELECTED
        // --------------------------------------------------------

        if (
            hideWhenAbilitySelected &&
            canvasInfoManager != null &&
            canvasInfoManager.HasSelectedAbility()
        )
        {
            HideLine();
            return;
        }


        if (
            gridManager == null ||
            currentSelectedUnit == null
        )
        {
            HideLine();
            return;
        }

        UnitMoveBrain moveBrain =
            currentSelectedUnit.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            HideLine();
            return;
        }

        if (!moveBrain.CanMoveThisTurn())
        {
            HideLine();
            return;
        }

        GameObject selectedObject =
            currentSelectedUnit.gameObject;

        Vector2Int start =
            gridManager.GetUnitGridPosition(
                selectedObject
            );


        // --------------------------------------------------------
        // SAME CELL
        // --------------------------------------------------------

        if (start == destination)
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // INVALID CELL
        // --------------------------------------------------------

        if (!gridManager.IsInsideGrid(
                destination))
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // OCCUPIED DESTINATION
        // --------------------------------------------------------

        if (gridManager.IsCellOccupied(
                destination))
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // GET MOVE RANGE
        // --------------------------------------------------------

        int moveRange =
            moveBrain.GetMoveRange();

        if (moveRange <= 0)
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // MANHATTAN RANGE
        // --------------------------------------------------------

        int manhattanDistance =
            GetManhattanDistance(
                start,
                destination
            );


        if (
            useManhattanRange &&
            manhattanDistance > moveRange &&
            !clampPathToMoveRange
        )
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // FIND PATH
        // --------------------------------------------------------

        bool foundPath =
            FindPath(
                selectedObject,
                start,
                destination,
                path
            );

        if (!foundPath)
        {
            HideLine();
            return;
        }

        if (path.Count < 2)
        {
            HideLine();
            return;
        }


        // --------------------------------------------------------
        // CREATE PREVIEW PATH
        // --------------------------------------------------------

        previewPath.Clear();


        int movementCostUsed = 0;

        previewPath.Add(start);


        for (
            int i = 1;
            i < path.Count;
            i++
        )
        {
            Vector2Int previous =
                path[i - 1];

            Vector2Int current =
                path[i];

            int stepCost =
                GetMovementCost(
                    previous,
                    current
                );

            if (
                movementCostUsed +
                stepCost >
                moveRange
            )
            {
                break;
            }

            movementCostUsed +=
                stepCost;

            previewPath.Add(
                current
            );


            if (current == destination)
            {
                break;
            }
        }


        // --------------------------------------------------------
        // DESTINATION OUTSIDE RANGE
        // --------------------------------------------------------

        if (
            path.Count > 1 &&
            path[path.Count - 1] == destination &&
            destination != previewPath[previewPath.Count - 1]
        )
        {
            if (!clampPathToMoveRange)
            {
                HideLine();
                return;
            }
        }


        // --------------------------------------------------------
        // DRAW
        // --------------------------------------------------------

        if (previewPath.Count < 2)
        {
            HideLine();
            return;
        }

        DrawPath(
            previewPath
        );
    }


    // ============================================================
    // MOVEMENT COST
    // ============================================================

    private int GetMovementCost(
        Vector2Int from,
        Vector2Int to
    )
    {
        int dx =
            Mathf.Abs(
                to.x - from.x
            );

        int dy =
            Mathf.Abs(
                to.y - from.y
            );

        if (dx == 1 && dy == 1)
        {
            return 2;
        }

        return 1;
    }


    // ============================================================
    // MANHATTAN DISTANCE
    // ============================================================

    private int GetManhattanDistance(
        Vector2Int a,
        Vector2Int b
    )
    {
        return
            Mathf.Abs(
                a.x - b.x
            ) +
            Mathf.Abs(
                a.y - b.y
            );
    }


    // ============================================================
    // A* PATHFINDING
    // ============================================================

    private bool FindPath(
        GameObject unit,
        Vector2Int start,
        Vector2Int destination,
        List<Vector2Int> result
    )
    {
        result.Clear();

        openList.Clear();
        closedSet.Clear();
        cameFrom.Clear();
        gScore.Clear();


        if (start == destination)
        {
            result.Add(start);
            return true;
        }


        openList.Add(start);

        gScore[start] =
            0;


        while (openList.Count > 0)
        {
            Vector2Int current =
                GetBestOpenNode(
                    destination
                );


            if (current == destination)
            {
                ReconstructPath(
                    start,
                    destination,
                    result
                );

                return result.Count > 0;
            }


            openList.Remove(
                current
            );

            closedSet.Add(
                current
            );


            if (!gScore.TryGetValue(
                    current,
                    out int currentG
                ))
            {
                continue;
            }


            foreach (
                Vector2Int neighbour
                in GetNeighbours(
                    current,
                    unit
                )
            )
            {
                if (closedSet.Contains(
                        neighbour))
                {
                    continue;
                }

                if (!gridManager.IsInsideGrid(
                        neighbour))
                {
                    continue;
                }


                // ------------------------------------------------
                // OCCUPIED CELL
                // ------------------------------------------------

                if (
                    neighbour != destination &&
                    gridManager.IsCellOccupied(
                        neighbour
                    )
                )
                {
                    continue;
                }


                // ------------------------------------------------
                // MOVEMENT COST
                // ------------------------------------------------

                int movementCost =
                    GetMovementCost(
                        current,
                        neighbour
                    );

                int tentativeG =
                    currentG +
                    movementCost;


                if (
                    !gScore.TryGetValue(
                        neighbour,
                        out int existingG
                    ) ||
                    tentativeG < existingG
                )
                {
                    cameFrom[neighbour] =
                        current;

                    gScore[neighbour] =
                        tentativeG;


                    if (!openList.Contains(
                            neighbour))
                    {
                        openList.Add(
                            neighbour
                        );
                    }
                }
            }
        }


        return false;
    }


    // ============================================================
    // BEST OPEN NODE
    // ============================================================

    private Vector2Int GetBestOpenNode(
        Vector2Int destination
    )
    {
        Vector2Int best =
            openList[0];

        int bestScore =
            GetFScore(
                best,
                destination
            );


        for (
            int i = 1;
            i < openList.Count;
            i++
        )
        {
            Vector2Int candidate =
                openList[i];

            int candidateScore =
                GetFScore(
                    candidate,
                    destination
                );


            if (candidateScore < bestScore)
            {
                best =
                    candidate;

                bestScore =
                    candidateScore;
            }
        }


        return best;
    }


    // ============================================================
    // F SCORE
    // ============================================================

    private int GetFScore(
        Vector2Int position,
        Vector2Int destination
    )
    {
        int g =
            gScore.TryGetValue(
                position,
                out int value
            )
                ? value
                : int.MaxValue;


        int h =
            GetManhattanDistance(
                position,
                destination
            );


        return g + h;
    }


    // ============================================================
    // NEIGHBOURS
    // ============================================================

    private IEnumerable<Vector2Int> GetNeighbours(
        Vector2Int current,
        GameObject unit
    )
    {
        UnitMoveBrain moveBrain =
            unit != null
                ? unit.GetComponent<UnitMoveBrain>()
                : null;

        bool diagonal =
            moveBrain != null &&
            CanWalkDiagonally(
                moveBrain
            );


        // ========================================================
        // CARDINAL
        // ========================================================

        Vector2Int[] cardinal =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        for (
            int i = 0;
            i < cardinal.Length;
            i++
        )
        {
            Vector2Int next =
                current +
                cardinal[i];


            if (gridManager.IsInsideGrid(
                    next))
            {
                yield return next;
            }
        }


        // ========================================================
        // DIAGONAL
        // ========================================================

        if (!diagonal)
        {
            yield break;
        }


        Vector2Int[] diagonals =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };


        for (
            int i = 0;
            i < diagonals.Length;
            i++
        )
        {
            Vector2Int direction =
                diagonals[i];

            Vector2Int next =
                current +
                direction;


            if (!gridManager.IsInsideGrid(
                    next))
            {
                continue;
            }


            // ----------------------------------------------------
            // PREVENT CORNER CUTTING
            // ----------------------------------------------------

            Vector2Int horizontal =
                current +
                new Vector2Int(
                    direction.x,
                    0
                );

            Vector2Int vertical =
                current +
                new Vector2Int(
                    0,
                    direction.y
                );


            if (
                gridManager.IsCellOccupied(
                    horizontal
                ) ||
                gridManager.IsCellOccupied(
                    vertical
                )
            )
            {
                continue;
            }


            yield return next;
        }
    }


    // ============================================================
    // DIAGONAL MOVEMENT
    // ============================================================

    private bool CanWalkDiagonally(
        UnitMoveBrain moveBrain
    )
    {
        if (moveBrain == null)
        {
            return false;
        }


        AttackUnit attackUnit =
            moveBrain.GetAttackUnit();

        if (attackUnit == null)
        {
            return false;
        }


        CharacterSO characterData =
            attackUnit.GetCharacterData();


        return
            characterData != null &&
            characterData.canwalkdiagonally;
    }


    // ============================================================
    // RECONSTRUCT PATH
    // ============================================================

    private void ReconstructPath(
        Vector2Int start,
        Vector2Int destination,
        List<Vector2Int> result
    )
    {
        result.Clear();


        Vector2Int current =
            destination;


        result.Add(
            current
        );


        while (current != start)
        {
            if (!cameFrom.TryGetValue(
                    current,
                    out Vector2Int previous
                ))
            {
                result.Clear();
                return;
            }


            current =
                previous;


            result.Add(
                current
            );
        }


        result.Reverse();
    }


    // ============================================================
    // DRAW PATH
    // ============================================================

    private void DrawPath(
        List<Vector2Int> pathToDraw
    )
    {
        if (
            lineRenderer == null ||
            pathToDraw == null ||
            pathToDraw.Count < 2
        )
        {
            HideLine();
            return;
        }


        lineRenderer.positionCount =
            pathToDraw.Count;


        for (
            int i = 0;
            i < pathToDraw.Count;
            i++
        )
        {
            Vector3 position =
                gridManager.GridToWorldPosition(
                    pathToDraw[i]
                );


            position.z =
                lineHeight;


            lineRenderer.SetPosition(
                i,
                position
            );
        }


        lineRenderer.enabled =
            true;


        if (debugPreview)
        {
            Debug.Log(
                "[LineWalkPreview] Drawing " +
                (pathToDraw.Count - 1) +
                " tile(s). " +
                "Move range = " +
                GetCurrentMoveRange(),
                currentSelectedUnit
            );
        }
    }


    // ============================================================
    // CURRENT MOVE RANGE
    // ============================================================

    private int GetCurrentMoveRange()
    {
        if (currentSelectedUnit == null)
        {
            return 0;
        }


        UnitMoveBrain moveBrain =
            currentSelectedUnit.GetComponent<UnitMoveBrain>();


        if (moveBrain == null)
        {
            return 0;
        }


        return moveBrain.GetMoveRange();
    }


    // ============================================================
    // HIDE LINE
    // ============================================================

    private void HideLine()
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


    // ============================================================
    // PUBLIC API
    // ============================================================

    public void ClearPreview()
    {
        hasHoveredCell =
            false;

        path.Clear();

        previewPath.Clear();

        HideLine();
    }


    public bool IsShowingPreview()
    {
        return
            lineRenderer != null &&
            lineRenderer.enabled &&
            lineRenderer.positionCount > 1;
    }


    public HoverInfoTrigger GetCurrentSelectedUnit()
    {
        return currentSelectedUnit;
    }
}