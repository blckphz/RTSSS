using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelNode
{
    public Transform Transform;
    public Vector2 Position;
    public int Row;
    public int Column;
    public EncounterDefinition Encounter;
    public List<LevelNode> NextNodes = new List<LevelNode>();
    public bool IsCompleted;
    public bool IsUnlocked;

    public LevelNode(
        Transform transform,
        Vector2 position,
        int row,
        int column,
        EncounterDefinition encounter)
    {
        Transform = transform;
        Position = position;
        Row = row;
        Column = column;
        Encounter = encounter;
        IsCompleted = false;
        IsUnlocked = false;
    }
}

public class LevelMapManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Map Parents")]
    [SerializeField] private Transform iconParent;
    [SerializeField] private Transform lineParent;

    [Header("Map Canvas")]
    [SerializeField] private GameObject mapCanvas;

    [Header("Prefabs")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private GameObject linePrefab;

    [Header("Default Node Icon")]
    [SerializeField] private Sprite defaultNodeIcon;

    [Header("Start")]
    [SerializeField] private EncounterDefinition startEncounter;

    [Header("Normal Encounters")]
    [SerializeField]
    private List<EncounterDefinition> normalEncounters =
        new List<EncounterDefinition>();

    [Header("Elite Encounters")]
    [SerializeField]
    private List<EncounterDefinition> eliteEncounters =
        new List<EncounterDefinition>();

    [Header("Boss")]
    [SerializeField] private EncounterDefinition bossEncounter;

    [Header("Encounter Selection")]
    [Range(0f, 1f)]
    [SerializeField] private float eliteChance = 0.2f;

    [SerializeField] private bool allowEncounterRepeats = true;

    [Header("Map Settings")]
    [SerializeField] private int rows = 8;
    [SerializeField] private int minNodesPerRow = 2;
    [SerializeField] private int maxNodesPerRow = 4;
    [SerializeField] private float maxRandomOffset = 0.4f;

    [Header("2D World Spacing")]
    [SerializeField] private float horizontalSpacing = 2.5f;
    [SerializeField] private float verticalSpacing = 2.0f;

    [Header("Image Connection Settings")]
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float lineEndPadding = 0.15f;
    [SerializeField] private bool centerLine = true;

    [Header("Icon Sorting")]
    [SerializeField] private string iconSortingLayerName = "Default";
    [SerializeField] private int lockedIconSortingOrder = 1;
    [SerializeField] private int unlockedIconSortingOrder = 2;
    [SerializeField] private int completedIconSortingOrder = 1;

    [Header("Map Visibility")]
    [SerializeField] private bool hideMapDuringEncounter = true;
    [SerializeField] private bool showMapAfterVictory = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // ============================================================
    // STATE
    // ============================================================

    private List<List<LevelNode>> mapNodes =
        new List<List<LevelNode>>();

    private List<Image> linePool =
        new List<Image>();

    private LevelNode currentNode;

    private bool mapGenerated;

    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        GenerateMap();
    }

    private void OnEnable()
    {
        EncounterManager.OnEncounterVictory +=
            HandleEncounterVictory;
    }

    private void OnDisable()
    {
        EncounterManager.OnEncounterVictory -=
            HandleEncounterVictory;
    }

    // ============================================================
    // DEBUG
    // ============================================================

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            "[LevelMap] " + message,
            this
        );
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning(
            "[LevelMap] " + message,
            this
        );
    }

    private void LogError(string message)
    {
        Debug.LogError(
            "[LevelMap] " + message,
            this
        );
    }

    // ============================================================
    // GENERATE MAP
    // ============================================================

    [ContextMenu("Regenerate Map")]
    public void GenerateMap()
    {
        ClearMap();

        if (!ValidateMapSettings())
            return;

        SpawnNodes();
        GeneratePaths();
        DrawLines2D();
        UnlockStartNode();

        mapGenerated = true;

        ShowMap();

        Log(
            "Map generated. Rows=" +
            rows +
            ", Nodes=" +
            CountNodes()
        );
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private bool ValidateMapSettings()
    {
        bool valid = true;

        if (iconPrefab == null)
        {
            LogError("Icon Prefab is missing.");
            valid = false;
        }

        if (linePrefab == null)
        {
            LogError("Line Prefab is missing.");
            valid = false;
        }

        if (iconParent == null)
        {
            LogError("Icon Parent is missing.");
            valid = false;
        }

        if (lineParent == null)
        {
            LogError("Line Parent is missing.");
            valid = false;
        }

        if (mapCanvas == null)
        {
            LogWarning(
                "Map Canvas is not assigned. " +
                "Using icon/line parents for visibility."
            );
        }

        if (startEncounter == null)
        {
            LogWarning("Start Encounter is not assigned.");
        }

        if (bossEncounter == null)
        {
            LogWarning("Boss Encounter is not assigned.");
        }

        if (
            normalEncounters == null ||
            normalEncounters.Count == 0
        )
        {
            LogWarning("No Normal Encounters assigned.");
        }

        if (
            eliteEncounters == null ||
            eliteEncounters.Count == 0
        )
        {
            LogWarning("No Elite Encounters assigned.");
        }

        rows = Mathf.Max(2, rows);

        minNodesPerRow =
            Mathf.Max(1, minNodesPerRow);

        maxNodesPerRow =
            Mathf.Max(
                minNodesPerRow,
                maxNodesPerRow
            );

        lineWidth =
            Mathf.Max(
                0.001f,
                lineWidth
            );

        lineEndPadding =
            Mathf.Max(
                0f,
                lineEndPadding
            );

        return valid;
    }

    // ============================================================
    // SPAWN NODES
    // ============================================================

    private void SpawnNodes()
    {
        Vector2 baseOrigin =
            transform.position;

        for (int row = 0; row < rows; row++)
        {
            List<LevelNode> currentRow =
                new List<LevelNode>();

            int countInRow;

            if (row == 0 || row == rows - 1)
            {
                countInRow = 1;
            }
            else
            {
                countInRow =
                    Random.Range(
                        minNodesPerRow,
                        maxNodesPerRow + 1
                    );
            }

            for (
                int column = 0;
                column < countInRow;
                column++)
            {
                float x =
                    (column -
                    (countInRow - 1) / 2f) *
                    horizontalSpacing;

                float y =
                    row *
                    verticalSpacing;

                if (row > 0 && row < rows - 1)
                {
                    x += Random.Range(
                        -maxRandomOffset,
                        maxRandomOffset
                    );

                    y += Random.Range(
                        -maxRandomOffset / 2f,
                        maxRandomOffset / 2f
                    );
                }

                Vector2 position =
                    baseOrigin +
                    new Vector2(x, y);

                EncounterDefinition encounter =
                    GetEncounterForNode(
                        row,
                        column
                    );

                GameObject icon =
                    Instantiate(
                        iconPrefab,
                        position,
                        Quaternion.identity,
                        iconParent
                    );

                icon.name =
                    GetNodeName(
                        row,
                        column,
                        encounter
                    );

                SetNodeIconSprite(
                    icon,
                    encounter
                );

                IconBehav iconBehaviour =
                    icon.GetComponent<IconBehav>();

                if (iconBehaviour == null)
                {
                    iconBehaviour =
                        icon.AddComponent<IconBehav>();
                }

                iconBehaviour.SetMapManager(this);
                iconBehaviour.SetEncounter(encounter);
                iconBehaviour.SetNodeState(
                    false,
                    false
                );

                LevelNode node =
                    new LevelNode(
                        icon.transform,
                        position,
                        row,
                        column,
                        encounter
                    );

                currentRow.Add(node);
            }

            mapNodes.Add(currentRow);
        }
    }

    // ============================================================
    // NODE ICON
    // ============================================================

    private void SetNodeIconSprite(
        GameObject iconObject,
        EncounterDefinition encounter)
    {
        if (iconObject == null)
            return;

        Image image =
            iconObject.GetComponent<Image>();

        if (image == null)
        {
            image =
                iconObject.GetComponentInChildren<Image>();
        }

        if (image == null)
        {
            LogWarning(
                "Icon prefab has no Image component."
            );

            return;
        }

        Sprite spriteToUse =
            defaultNodeIcon;

        if (
            encounter != null &&
            encounter.mapNodeIcon != null
        )
        {
            spriteToUse =
                encounter.mapNodeIcon;
        }

        if (spriteToUse != null)
        {
            image.sprite =
                spriteToUse;
        }
    }

    // ============================================================
    // ENCOUNTER SELECTION
    // ============================================================

    private EncounterDefinition GetEncounterForNode(
        int row,
        int column)
    {
        if (row == 0)
            return startEncounter;

        if (row == rows - 1)
            return bossEncounter;

        bool chooseElite =
            Random.value < eliteChance;

        if (
            chooseElite &&
            eliteEncounters != null &&
            eliteEncounters.Count > 0
        )
        {
            return GetRandomEncounter(
                eliteEncounters
            );
        }

        if (
            normalEncounters != null &&
            normalEncounters.Count > 0
        )
        {
            return GetRandomEncounter(
                normalEncounters
            );
        }

        if (
            eliteEncounters != null &&
            eliteEncounters.Count > 0
        )
        {
            return GetRandomEncounter(
                eliteEncounters
            );
        }

        return null;
    }

    private EncounterDefinition GetRandomEncounter(
        List<EncounterDefinition> pool)
    {
        if (
            pool == null ||
            pool.Count == 0
        )
        {
            return null;
        }

        if (allowEncounterRepeats)
        {
            return pool[
                Random.Range(
                    0,
                    pool.Count
                )
            ];
        }

        List<EncounterDefinition> unused =
            new List<EncounterDefinition>();

        foreach (
            EncounterDefinition candidate
            in pool)
        {
            if (candidate == null)
                continue;

            if (!IsEncounterAlreadyUsed(candidate))
            {
                unused.Add(candidate);
            }
        }

        if (unused.Count > 0)
        {
            return unused[
                Random.Range(
                    0,
                    unused.Count
                )
            ];
        }

        return pool[
            Random.Range(
                0,
                pool.Count
            )
        ];
    }

    private bool IsEncounterAlreadyUsed(
        EncounterDefinition encounter)
    {
        foreach (
            List<LevelNode> row
            in mapNodes)
        {
            foreach (
                LevelNode node
                in row)
            {
                if (
                    node != null &&
                    node.Encounter == encounter
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetNodeName(
        int row,
        int column,
        EncounterDefinition encounter)
    {
        if (encounter == null)
        {
            return
                $"LevelNode_Row{row}_Column{column}_NO_ENCOUNTER";
        }

        return
            $"LevelNode_Row{row}_Column{column}_{encounter.encounterName}";
    }

    // ============================================================
    // PATH GENERATION
    // ============================================================

    private void GeneratePaths()
    {
        if (mapNodes.Count < 2)
            return;

        // --------------------------------------------------------
        // GUARANTEE PRIMARY CONNECTION
        // --------------------------------------------------------

        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++)
        {
            List<LevelNode> current =
                mapNodes[row];

            List<LevelNode> next =
                mapNodes[row + 1];

            for (
                int i = 0;
                i < current.Count;
                i++)
            {
                LevelNode node =
                    current[i];

                float ratio =
                    (float)i /
                    Mathf.Max(
                        1,
                        current.Count - 1
                    );

                int targetIndex =
                    Mathf.RoundToInt(
                        ratio *
                        (next.Count - 1)
                    );

                targetIndex =
                    Mathf.Clamp(
                        targetIndex,
                        0,
                        next.Count - 1
                    );

                AddConnection(
                    node,
                    next[targetIndex]
                );
            }
        }

        // --------------------------------------------------------
        // OPTIONAL EXTRA CONNECTIONS
        // --------------------------------------------------------

        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++)
        {
            List<LevelNode> current =
                mapNodes[row];

            List<LevelNode> next =
                mapNodes[row + 1];

            for (
                int i = 0;
                i < current.Count;
                i++)
            {
                LevelNode node =
                    current[i];

                if (Random.value > 0.5f)
                    continue;

                int mainTarget =
                    GetPrimaryTargetIndex(
                        i,
                        current.Count,
                        next.Count
                    );

                int direction =
                    Random.value > 0.5f
                        ? 1
                        : -1;

                int extraIndex =
                    mainTarget + direction;

                if (
                    extraIndex < 0 ||
                    extraIndex >= next.Count
                )
                {
                    extraIndex =
                        mainTarget - direction;
                }

                if (
                    extraIndex < 0 ||
                    extraIndex >= next.Count
                )
                {
                    continue;
                }

                LevelNode extraTarget =
                    next[extraIndex];

                if (
                    node.NextNodes.Contains(
                        extraTarget
                    )
                )
                {
                    continue;
                }

                bool causesCross = false;

                if (i > 0)
                {
                    LevelNode previous =
                        current[i - 1];

                    foreach (
                        LevelNode previousTarget
                        in previous.NextNodes)
                    {
                        if (
                            previousTarget.Column >
                            extraTarget.Column
                        )
                        {
                            causesCross = true;
                            break;
                        }
                    }
                }

                if (!causesCross)
                {
                    AddConnection(
                        node,
                        extraTarget
                    );
                }
            }
        }

        // --------------------------------------------------------
        // GUARANTEE EVERY NODE HAS INCOMING CONNECTION
        // --------------------------------------------------------

        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++)
        {
            List<LevelNode> current =
                mapNodes[row];

            List<LevelNode> next =
                mapNodes[row + 1];

            foreach (
                LevelNode nextNode
                in next)
            {
                if (
                    HasIncomingConnection(
                        nextNode,
                        current
                    )
                )
                {
                    continue;
                }

                LevelNode closest =
                    GetClosestNode(
                        nextNode,
                        current
                    );

                if (closest != null)
                {
                    AddConnection(
                        closest,
                        nextNode
                    );
                }
            }
        }
    }

    private int GetPrimaryTargetIndex(
        int currentIndex,
        int currentCount,
        int nextCount)
    {
        float ratio =
            (float)currentIndex /
            Mathf.Max(
                1,
                currentCount - 1
            );

        int targetIndex =
            Mathf.RoundToInt(
                ratio *
                (nextCount - 1)
            );

        return Mathf.Clamp(
            targetIndex,
            0,
            nextCount - 1
        );
    }

    private void AddConnection(
        LevelNode from,
        LevelNode to)
    {
        if (from == null || to == null)
            return;

        if (from.NextNodes.Contains(to))
            return;

        from.NextNodes.Add(to);
    }

    private bool HasIncomingConnection(
        LevelNode target,
        List<LevelNode> previousRow)
    {
        foreach (
            LevelNode node
            in previousRow)
        {
            if (
                node.NextNodes.Contains(
                    target
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================
    // DRAW LINES
    // ============================================================

    private void DrawLines2D()
    {
        foreach (Image line in linePool)
        {
            if (line != null)
            {
                line.gameObject.SetActive(false);
            }
        }

        int lineIndex = 0;

        foreach (
            List<LevelNode> row
            in mapNodes)
        {
            foreach (
                LevelNode node
                in row)
            {
                foreach (
                    LevelNode nextNode
                    in node.NextNodes)
                {
                    Image line =
                        GetOrCreateLine(
                            lineIndex++
                        );

                    if (line == null)
                        continue;

                    SetupLineImage(
                        line,
                        node.Position,
                        nextNode.Position
                    );

                    line.gameObject.SetActive(true);
                }
            }
        }
    }

    private void SetupLineImage(
        Image line,
        Vector2 start,
        Vector2 end)
    {
        if (line == null)
            return;

        RectTransform rect =
            line.rectTransform;

        if (rect == null)
            return;

        Vector2 direction =
            end - start;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
        {
            line.gameObject.SetActive(false);
            return;
        }

        Vector2 normalizedDirection =
            direction.normalized;

        Vector2 paddedStart =
            start +
            normalizedDirection *
            lineEndPadding;

        Vector2 paddedEnd =
            end -
            normalizedDirection *
            lineEndPadding;

        Vector2 finalDirection =
            paddedEnd - paddedStart;

        float finalDistance =
            finalDirection.magnitude;

        if (finalDistance <= 0.001f)
        {
            line.gameObject.SetActive(false);
            return;
        }

        Vector2 finalPosition;

        if (centerLine)
        {
            finalPosition =
                (paddedStart + paddedEnd) *
                0.5f;
        }
        else
        {
            finalPosition =
                paddedStart;
        }

        rect.position =
            new Vector3(
                finalPosition.x,
                finalPosition.y,
                rect.position.z
            );

        rect.sizeDelta =
            new Vector2(
                finalDistance,
                lineWidth
            );

        float angle =
            Mathf.Atan2(
                finalDirection.y,
                finalDirection.x
            ) *
            Mathf.Rad2Deg;

        rect.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        line.gameObject.SetActive(true);
    }

    private Image GetOrCreateLine(
        int index)
    {
        if (index < linePool.Count)
            return linePool[index];

        GameObject obj =
            Instantiate(
                linePrefab,
                lineParent
            );

        if (obj == null)
        {
            LogError("Failed to create line.");
            return null;
        }

        Image image =
            obj.GetComponent<Image>();

        if (image == null)
        {
            LogError(
                "Line Prefab requires an Image component."
            );

            Destroy(obj);

            return null;
        }

        image.raycastTarget = false;

        linePool.Add(image);

        return image;
    }

    // ============================================================
    // START NODE
    // ============================================================

    private void UnlockStartNode()
    {
        if (
            mapNodes.Count == 0 ||
            mapNodes[0].Count == 0
        )
        {
            return;
        }

        LevelNode startNode =
            mapNodes[0][0];

        UnlockNode(startNode);

        Log(
            "Start node unlocked: " +
            startNode.Transform.name
        );
    }

    private void UnlockNode(
        LevelNode node)
    {
        if (node == null)
            return;

        node.IsUnlocked = true;

        UpdateIconState(node);
    }

    // ============================================================
    // NODE CLICK
    // ============================================================

    public void HandleNodeClicked(
        IconBehav icon)
    {
        if (icon == null)
            return;

        LevelNode node =
            FindNode(icon.transform);

        if (node == null)
        {
            LogWarning(
                "Clicked icon does not belong to map."
            );

            return;
        }

        if (!node.IsUnlocked)
        {
            Log(
                "Blocked locked node: " +
                node.Transform.name
            );

            return;
        }

        if (node.IsCompleted)
        {
            Log(
                "Blocked completed node: " +
                node.Transform.name
            );

            return;
        }

        if (node.Encounter == null)
        {
            LogError(
                "Node has no EncounterDefinition."
            );

            return;
        }

        EncounterManager encounterManager =
            FindFirstObjectByType<EncounterManager>();

        if (encounterManager == null)
        {
            LogError(
                "EncounterManager not found."
            );

            return;
        }

        if (encounterManager.IsEncounterRunning())
        {
            Log(
                "Encounter already running."
            );

            return;
        }

        // --------------------------------------------------------
        // SELECT THIS ROUTE
        // --------------------------------------------------------

        SelectRoute(node);

        currentNode = node;

        Log(
            "Selected node: " +
            node.Transform.name
        );

        if (hideMapDuringEncounter)
        {
            HideMap();
        }

        encounterManager.SetCurrentEncounter(
            node.Encounter
        );

        encounterManager.StartEncounter();
    }

    // ============================================================
    // ROUTE SELECTION
    // ============================================================

    private void SelectRoute(
        LevelNode selectedNode)
    {
        if (selectedNode == null)
            return;

        /*
         * The player has chosen this node.
         *
         * Every other currently unlocked node is locked.
         *
         * This prevents the player from clicking another
         * branch after choosing a route.
         */

        foreach (
            List<LevelNode> row
            in mapNodes)
        {
            foreach (
                LevelNode node
                in row)
            {
                if (node == null)
                    continue;

                if (node == selectedNode)
                    continue;

                node.IsUnlocked = false;

                UpdateIconState(node);
            }
        }

        // The selected node remains active.
        selectedNode.IsUnlocked = true;

        UpdateIconState(selectedNode);

        Log(
            "Route locked to: " +
            selectedNode.Transform.name
        );
    }

    // ============================================================
    // VICTORY
    // ============================================================

    private void HandleEncounterVictory(
        EncounterDefinition completedEncounter)
    {
        if (currentNode == null)
        {
            LogWarning(
                "Victory received with no current node."
            );

            return;
        }

        if (
            completedEncounter != null &&
            currentNode.Encounter !=
            completedEncounter
        )
        {
            LogWarning(
                "Victory encounter does not match current node."
            );
        }

        CompleteCurrentNode();

        if (showMapAfterVictory)
        {
            ShowMap();
        }
    }

    private void CompleteCurrentNode()
    {
        if (currentNode == null)
            return;

        LevelNode completedNode =
            currentNode;

        completedNode.IsCompleted = true;
        completedNode.IsUnlocked = false;

        UpdateIconState(
            completedNode
        );

        // --------------------------------------------------------
        // ONLY DIRECT CONNECTIONS BECOME AVAILABLE
        // --------------------------------------------------------

        foreach (
            LevelNode nextNode
            in completedNode.NextNodes)
        {
            if (nextNode == null)
                continue;

            UnlockNode(nextNode);
        }

        Log(
            "Completed: " +
            completedNode.Transform.name +
            ". Unlocked next nodes: " +
            completedNode.NextNodes.Count
        );

        if (
            completedNode.Row ==
            rows - 1
        )
        {
            Log(
                "Boss completed. Map complete."
            );
        }

        currentNode = null;
    }

    // ============================================================
    // MAP VISIBILITY
    // ============================================================

    public void HideMap()
    {
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
            return;
        }

        if (iconParent != null)
        {
            iconParent.gameObject.SetActive(false);
        }

        if (lineParent != null)
        {
            lineParent.gameObject.SetActive(false);
        }
    }

    public void ShowMap()
    {
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(true);
            return;
        }

        if (iconParent != null)
        {
            iconParent.gameObject.SetActive(true);
        }

        if (lineParent != null)
        {
            lineParent.gameObject.SetActive(true);
        }
    }

    public void ToggleMap()
    {
        if (mapCanvas != null)
        {
            if (mapCanvas.activeSelf)
                HideMap();
            else
                ShowMap();

            return;
        }

        bool currentlyVisible = true;

        if (iconParent != null)
        {
            currentlyVisible =
                iconParent.gameObject.activeSelf;
        }

        if (currentlyVisible)
            HideMap();
        else
            ShowMap();
    }

    // ============================================================
    // FIND NODE
    // ============================================================

    private LevelNode FindNode(
        Transform target)
    {
        if (target == null)
            return null;

        foreach (
            List<LevelNode> row
            in mapNodes)
        {
            foreach (
                LevelNode node
                in row)
            {
                if (
                    node != null &&
                    node.Transform == target
                )
                {
                    return node;
                }
            }
        }

        return null;
    }

    // ============================================================
    // UPDATE ICON
    // ============================================================

    private void UpdateIconState(
        LevelNode node)
    {
        if (
            node == null ||
            node.Transform == null
        )
        {
            return;
        }

        IconBehav icon =
            node.Transform.GetComponent<IconBehav>();

        if (icon != null)
        {
            icon.SetNodeState(
                node.IsUnlocked,
                node.IsCompleted
            );
        }

        SpriteRenderer sprite =
            node.Transform.GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            sprite.sortingLayerName =
                iconSortingLayerName;

            if (node.IsCompleted)
            {
                sprite.sortingOrder =
                    completedIconSortingOrder;
            }
            else if (node.IsUnlocked)
            {
                sprite.sortingOrder =
                    unlockedIconSortingOrder;
            }
            else
            {
                sprite.sortingOrder =
                    lockedIconSortingOrder;
            }
        }

        Image image =
            node.Transform.GetComponent<Image>();

        if (image == null)
        {
            image =
                node.Transform.GetComponentInChildren<Image>();
        }

        if (
            image != null &&
            node.Encounter != null &&
            node.Encounter.mapNodeIcon != null
        )
        {
            image.sprite =
                node.Encounter.mapNodeIcon;
        }
    }

    // ============================================================
    // CLOSEST NODE
    // ============================================================

    private LevelNode GetClosestNode(
        LevelNode target,
        List<LevelNode> pool)
    {
        LevelNode closest = null;

        float minDistance =
            float.MaxValue;

        foreach (
            LevelNode candidate
            in pool)
        {
            if (candidate == null)
                continue;

            float distance =
                Vector2.Distance(
                    target.Position,
                    candidate.Position
                );

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    // ============================================================
    // CLEAR MAP
    // ============================================================

    private void ClearMap()
    {
        if (iconParent != null)
        {
            for (
                int i =
                    iconParent.childCount - 1;
                i >= 0;
                i--)
            {
                Transform child =
                    iconParent.GetChild(i);

                if (child != null)
                {
                    Destroy(
                        child.gameObject
                    );
                }
            }
        }

        foreach (
            Image line
            in linePool)
        {
            if (line != null)
            {
                line.gameObject.SetActive(false);
            }
        }

        mapNodes.Clear();

        currentNode = null;

        mapGenerated = false;
    }

    // ============================================================
    // DEBUG / INFORMATION
    // ============================================================

    private int CountNodes()
    {
        int count = 0;

        foreach (
            List<LevelNode> row
            in mapNodes)
        {
            if (row != null)
            {
                count += row.Count;
            }
        }

        return count;
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    public List<List<LevelNode>> GetMapNodes()
    {
        return mapNodes;
    }

    public LevelNode GetCurrentNode()
    {
        return currentNode;
    }

    public bool IsMapGenerated()
    {
        return mapGenerated;
    }

    public bool IsNodeUnlocked(
        int row,
        int column)
    {
        if (
            row < 0 ||
            row >= mapNodes.Count
        )
        {
            return false;
        }

        if (
            column < 0 ||
            column >= mapNodes[row].Count
        )
        {
            return false;
        }

        return
            mapNodes[row][column].IsUnlocked;
    }

    public bool IsMapVisible()
    {
        if (mapCanvas != null)
        {
            return mapCanvas.activeSelf;
        }

        if (iconParent == null)
            return false;

        return
            iconParent.gameObject.activeSelf;
    }

    public void RegenerateMap()
    {
        GenerateMap();
    }
}