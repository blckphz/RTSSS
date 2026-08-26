using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// ============================================================
// LEVEL NODE
// ============================================================

public class LevelNode
{
    public Transform Transform;

    public Vector2 Position;

    public int Row;

    public int Column;

    public EncounterDefinition Encounter;

    public List<LevelNode> NextNodes =
        new List<LevelNode>();

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


// ============================================================
// LEVEL MAP MANAGER
// ============================================================

public class LevelMapManager : MonoBehaviour
{
    // ============================================================
    // MAP PARENTS
    // ============================================================

    [Header("Map Parents")]

    [SerializeField]
    private Transform iconParent;


    [SerializeField]
    private Transform lineParent;


    // ============================================================
    // MAP CONTROLLER
    // ============================================================

    [Header("Map Controller")]

    [Tooltip(
        "Controls whether the map is visible during encounters."
    )]
    [SerializeField]
    private MapController mapController;


    // ============================================================
    // PREFABS
    // ============================================================

    [Header("Prefabs")]

    [SerializeField]
    private GameObject iconPrefab;


    [Tooltip(
        "UI Image prefab used to draw connections between map nodes."
    )]
    [SerializeField]
    private GameObject linePrefab;


    // ============================================================
    // ENCOUNTERS
    // ============================================================

    [Header("Start")]

    [SerializeField]
    private EncounterDefinition startEncounter;


    [Header("Normal Encounters")]

    [SerializeField]
    private List<EncounterDefinition> normalEncounters =
        new List<EncounterDefinition>();


    [Header("Elite Encounters")]

    [SerializeField]
    private List<EncounterDefinition> eliteEncounters =
        new List<EncounterDefinition>();


    [Header("Boss")]

    [SerializeField]
    private EncounterDefinition bossEncounter;


    // ============================================================
    // ENCOUNTER SETTINGS
    // ============================================================

    [Header("Encounter Selection")]

    [Range(0f, 1f)]
    [SerializeField]
    private float eliteChance = 0.2f;


    [SerializeField]
    private bool allowEncounterRepeats = true;


    // ============================================================
    // MAP SETTINGS
    // ============================================================

    [Header("Map Settings")]

    [SerializeField]
    private int rows = 8;


    [SerializeField]
    private int minNodesPerRow = 2;


    [SerializeField]
    private int maxNodesPerRow = 4;


    [SerializeField]
    private float maxRandomOffset = 0.4f;


    // ============================================================
    // WORLD SPACING
    // ============================================================

    [Header("2D World Spacing")]

    [SerializeField]
    private float horizontalSpacing = 2.5f;


    [SerializeField]
    private float verticalSpacing = 2.0f;


    // ============================================================
    // IMAGE CONNECTION SETTINGS
    // ============================================================

    [Header("Image Connection Settings")]

    [Tooltip(
        "Width of each connection Image."
    )]
    [SerializeField]
    private float lineWidth = 0.08f;


    [Tooltip(
        "Offset applied to the beginning and end of each connection."
    )]
    [SerializeField]
    private float lineEndPadding = 0.15f;


    [Tooltip(
        "If enabled, the Image is centered between the two nodes."
    )]
    [SerializeField]
    private bool centerLine = true;


    // ============================================================
    // ICON SORTING
    // ============================================================

    [Header("Icon Sorting")]

    [SerializeField]
    private string iconSortingLayerName = "Default";


    [SerializeField]
    private int lockedIconSortingOrder = 1;


    [SerializeField]
    private int unlockedIconSortingOrder = 2;


    [SerializeField]
    private int completedIconSortingOrder = 1;


    // ============================================================
    // INTERNAL
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

    private void Awake()
    {
        FindMapController();
    }


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
    // FIND MAP CONTROLLER
    // ============================================================

    private void FindMapController()
    {
        if (mapController == null)
        {
            mapController =
                FindFirstObjectByType<MapController>();
        }


        if (mapController == null)
        {
            Debug.LogWarning(
                "[LevelMapManager] MapController was not found.",
                this
            );
        }
    }


    // ============================================================
    // GENERATE MAP
    // ============================================================

    [ContextMenu("Regenerate Map")]
    public void GenerateMap()
    {
        ClearMap();


        if (!ValidateMapSettings())
        {
            return;
        }


        SpawnNodes();


        GeneratePaths();


        DrawLines2D();


        UnlockStartNode();


        mapGenerated = true;


        Debug.Log(
            "[LevelMapManager] Roguelike map generated.",
            this
        );
    }


    // ============================================================
    // VALIDATE MAP
    // ============================================================

    private bool ValidateMapSettings()
    {
        bool valid = true;


        if (iconPrefab == null)
        {
            Debug.LogError(
                "[LevelMapManager] Icon Prefab is missing!",
                this
            );

            valid = false;
        }


        if (linePrefab == null)
        {
            Debug.LogError(
                "[LevelMapManager] Line Image Prefab is missing!",
                this
            );

            valid = false;
        }


        if (iconParent == null)
        {
            Debug.LogError(
                "[LevelMapManager] Icon Parent is missing!",
                this
            );

            valid = false;
        }


        if (lineParent == null)
        {
            Debug.LogError(
                "[LevelMapManager] Line Parent is missing!",
                this
            );

            valid = false;
        }


        if (startEncounter == null)
        {
            Debug.LogWarning(
                "[LevelMapManager] Start Encounter is not assigned.",
                this
            );
        }


        if (bossEncounter == null)
        {
            Debug.LogWarning(
                "[LevelMapManager] Boss Encounter is not assigned.",
                this
            );
        }


        if (
            normalEncounters == null ||
            normalEncounters.Count == 0
        )
        {
            Debug.LogWarning(
                "[LevelMapManager] No Normal Encounters assigned.",
                this
            );
        }


        if (
            eliteEncounters == null ||
            eliteEncounters.Count == 0
        )
        {
            Debug.LogWarning(
                "[LevelMapManager] No Elite Encounters assigned.",
                this
            );
        }


        rows =
            Mathf.Max(
                2,
                rows
            );


        minNodesPerRow =
            Mathf.Max(
                1,
                minNodesPerRow
            );


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


        for (
            int row = 0;
            row < rows;
            row++
        )
        {
            List<LevelNode> currentRow =
                new List<LevelNode>();


            int countInRow;


            if (row == 0)
            {
                countInRow = 1;
            }
            else if (row == rows - 1)
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
                column++
            )
            {
                float x =
                    (
                        column -
                        (countInRow - 1) / 2f
                    ) *
                    horizontalSpacing;


                float y =
                    row *
                    verticalSpacing;


                if (
                    row > 0 &&
                    row < rows - 1
                )
                {
                    x +=
                        Random.Range(
                            -maxRandomOffset,
                            maxRandomOffset
                        );


                    y +=
                        Random.Range(
                            -maxRandomOffset / 2f,
                            maxRandomOffset / 2f
                        );
                }


                Vector2 position =
                    baseOrigin +
                    new Vector2(
                        x,
                        y
                    );


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


                IconBehav iconBehaviour =
                    icon.GetComponent<IconBehav>();


                if (iconBehaviour == null)
                {
                    iconBehaviour =
                        icon.AddComponent<IconBehav>();
                }


                iconBehaviour.SetMapManager(
                    this
                );


                iconBehaviour.SetEncounter(
                    encounter
                );


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


                currentRow.Add(
                    node
                );
            }


            mapNodes.Add(
                currentRow
            );
        }
    }


    // ============================================================
    // GET ENCOUNTER FOR NODE
    // ============================================================

    private EncounterDefinition GetEncounterForNode(
        int row,
        int column)
    {
        if (row == 0)
        {
            return startEncounter;
        }


        if (row == rows - 1)
        {
            return bossEncounter;
        }


        bool chooseElite =
            Random.value <
            eliteChance;


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


    // ============================================================
    // RANDOM ENCOUNTER
    // ============================================================

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
            int index =
                Random.Range(
                    0,
                    pool.Count
                );


            return pool[index];
        }


        List<EncounterDefinition> unused =
            new List<EncounterDefinition>();


        foreach (
            EncounterDefinition candidate
            in pool
        )
        {
            if (candidate == null)
            {
                continue;
            }


            if (!IsEncounterAlreadyUsed(candidate))
            {
                unused.Add(
                    candidate
                );
            }
        }


        if (unused.Count > 0)
        {
            int index =
                Random.Range(
                    0,
                    unused.Count
                );


            return unused[index];
        }


        int fallbackIndex =
            Random.Range(
                0,
                pool.Count
            );


        return pool[fallbackIndex];
    }


    // ============================================================
    // ENCOUNTER ALREADY USED
    // ============================================================

    private bool IsEncounterAlreadyUsed(
        EncounterDefinition encounter)
    {
        foreach (
            List<LevelNode> row
            in mapNodes
        )
        {
            foreach (
                LevelNode node
                in row
            )
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


    // ============================================================
    // NODE NAME
    // ============================================================

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
            $"LevelNode_Row{row}_Column{column}_" +
            encounter.encounterName;
    }


    // ============================================================
    // GENERATE PATHS
    // ============================================================

    private void GeneratePaths()
    {
        if (mapNodes.Count < 2)
        {
            return;
        }


        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++
        )
        {
            List<LevelNode> current =
                mapNodes[row];


            List<LevelNode> next =
                mapNodes[row + 1];


            for (
                int i = 0;
                i < current.Count;
                i++
            )
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


        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++
        )
        {
            List<LevelNode> current =
                mapNodes[row];


            List<LevelNode> next =
                mapNodes[row + 1];


            for (
                int i = 0;
                i < current.Count;
                i++
            )
            {
                LevelNode node =
                    current[i];


                if (Random.value > 0.5f)
                {
                    continue;
                }


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
                    mainTarget +
                    direction;


                if (
                    extraIndex < 0 ||
                    extraIndex >= next.Count
                )
                {
                    extraIndex =
                        mainTarget -
                        direction;
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


                bool causesCross =
                    false;


                if (i > 0)
                {
                    LevelNode previous =
                        current[i - 1];


                    foreach (
                        LevelNode previousTarget
                        in previous.NextNodes
                    )
                    {
                        if (
                            previousTarget.Column >
                            extraTarget.Column
                        )
                        {
                            causesCross =
                                true;

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


        for (
            int row = 0;
            row < mapNodes.Count - 1;
            row++
        )
        {
            List<LevelNode> current =
                mapNodes[row];


            List<LevelNode> next =
                mapNodes[row + 1];


            foreach (
                LevelNode nextNode
                in next
            )
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


    // ============================================================
    // PRIMARY TARGET
    // ============================================================

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


    // ============================================================
    // ADD CONNECTION
    // ============================================================

    private void AddConnection(
        LevelNode from,
        LevelNode to)
    {
        if (
            from == null ||
            to == null
        )
        {
            return;
        }


        if (
            from.NextNodes.Contains(
                to
            )
        )
        {
            return;
        }


        from.NextNodes.Add(
            to
        );
    }


    // ============================================================
    // INCOMING CONNECTION
    // ============================================================

    private bool HasIncomingConnection(
        LevelNode target,
        List<LevelNode> previousRow)
    {
        foreach (
            LevelNode node
            in previousRow
        )
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
    // DRAW IMAGE CONNECTIONS
    // ============================================================

    private void DrawLines2D()
    {
        foreach (
            Image line
            in linePool
        )
        {
            if (line != null)
            {
                line.gameObject.SetActive(
                    false
                );
            }
        }


        int lineIndex = 0;


        foreach (
            List<LevelNode> row
            in mapNodes
        )
        {
            foreach (
                LevelNode node
                in row
            )
            {
                foreach (
                    LevelNode nextNode
                    in node.NextNodes
                )
                {
                    Image line =
                        GetOrCreateLine(
                            lineIndex++
                        );


                    if (line == null)
                    {
                        continue;
                    }


                    SetupLineImage(
                        line,
                        node.Position,
                        nextNode.Position
                    );


                    line.gameObject.SetActive(
                        true
                    );
                }
            }
        }
    }


    // ============================================================
    // SETUP IMAGE LINE
    // ============================================================

    private void SetupLineImage(
        Image line,
        Vector2 start,
        Vector2 end)
    {
        if (line == null)
        {
            return;
        }


        RectTransform rect =
            line.rectTransform;


        if (rect == null)
        {
            return;
        }


        Vector2 direction =
            end -
            start;


        float distance =
            direction.magnitude;


        if (distance <= 0.001f)
        {
            line.gameObject.SetActive(
                false
            );

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
            paddedEnd -
            paddedStart;


        float finalDistance =
            finalDirection.magnitude;


        if (finalDistance <= 0.001f)
        {
            line.gameObject.SetActive(
                false
            );

            return;
        }


        Vector2 finalPosition;


        if (centerLine)
        {
            finalPosition =
                (
                    paddedStart +
                    paddedEnd
                ) *
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


        line.gameObject.SetActive(
            true
        );
    }


    // ============================================================
    // GET / CREATE IMAGE LINE
    // ============================================================

    private Image GetOrCreateLine(
        int index)
    {
        if (
            index <
            linePool.Count
        )
        {
            return linePool[index];
        }


        GameObject obj =
            Instantiate(
                linePrefab,
                lineParent
            );


        if (obj == null)
        {
            Debug.LogError(
                "[LevelMapManager] Failed to create line Image.",
                this
            );

            return null;
        }


        Image image =
            obj.GetComponent<Image>();


        if (image == null)
        {
            Debug.LogError(
                "[LevelMapManager] Line Prefab requires " +
                "an Image component!",
                obj
            );


            Destroy(obj);


            return null;
        }


        image.raycastTarget =
            false;


        linePool.Add(
            image
        );


        return image;
    }


    // ============================================================
    // UNLOCK START
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


        UnlockNode(
            startNode
        );
    }


    // ============================================================
    // UNLOCK NODE
    // ============================================================

    private void UnlockNode(
        LevelNode node)
    {
        if (node == null)
        {
            return;
        }


        node.IsUnlocked =
            true;


        UpdateIconState(
            node
        );
    }


    // ============================================================
    // HANDLE ICON CLICK
    // ============================================================

    public void HandleNodeClicked(
        IconBehav icon)
    {
        if (icon == null)
        {
            return;
        }


        LevelNode node =
            FindNode(
                icon.transform
            );


        if (node == null)
        {
            Debug.LogWarning(
                "[LevelMapManager] Could not find node " +
                "for clicked icon.",
                this
            );

            return;
        }


        if (!node.IsUnlocked)
        {
            Debug.Log(
                "[LevelMapManager] Node is locked: " +
                node.Transform.name,
                this
            );

            return;
        }


        if (node.IsCompleted)
        {
            Debug.Log(
                "[LevelMapManager] Node already completed: " +
                node.Transform.name,
                this
            );

            return;
        }


        if (node.Encounter == null)
        {
            Debug.LogError(
                "[LevelMapManager] Node has no EncounterDefinition!",
                node.Transform
            );

            return;
        }


        EncounterManager encounterManager =
            FindFirstObjectByType<EncounterManager>();


        if (encounterManager == null)
        {
            Debug.LogError(
                "[LevelMapManager] EncounterManager not found!",
                this
            );

            return;
        }


        if (
            encounterManager.IsEncounterRunning()
        )
        {
            Debug.LogWarning(
                "[LevelMapManager] An encounter is already running.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // SET CURRENT NODE
        // --------------------------------------------------------

        currentNode =
            node;


        // --------------------------------------------------------
        // ASSIGN ENCOUNTER
        // --------------------------------------------------------

        encounterManager.SetCurrentEncounter(
            node.Encounter
        );


        // --------------------------------------------------------
        // HIDE MAP
        // --------------------------------------------------------

        if (mapController != null)
        {
            mapController.HideMap();
        }
        else
        {
            Debug.LogWarning(
                "[LevelMapManager] MapController is not assigned.",
                this
            );
        }


        // --------------------------------------------------------
        // START ENCOUNTER
        // --------------------------------------------------------

        encounterManager.StartEncounter();


        Debug.Log(
            "[LevelMapManager] Starting node: " +
            node.Transform.name +
            "\nEncounter: " +
            node.Encounter.encounterName,
            this
        );
    }


    // ============================================================
    // ENCOUNTER VICTORY
    // ============================================================

    private void HandleEncounterVictory(
        EncounterDefinition completedEncounter)
    {
        if (currentNode == null)
        {
            Debug.LogWarning(
                "[LevelMapManager] Encounter victory received " +
                "but no current map node exists.",
                this
            );


            // ----------------------------------------------------
            // STILL SHOW MAP
            // ----------------------------------------------------

            if (mapController != null)
            {
                mapController.ShowMap();
            }


            return;
        }


        if (
            completedEncounter != null &&
            currentNode.Encounter != completedEncounter
        )
        {
            Debug.LogWarning(
                "[LevelMapManager] Victory encounter does not " +
                "match current map node.",
                this
            );
        }


        // --------------------------------------------------------
        // COMPLETE CURRENT NODE
        // --------------------------------------------------------

        CompleteCurrentNode();


        // --------------------------------------------------------
        // SHOW MAP AGAIN
        // --------------------------------------------------------

        if (mapController != null)
        {
            mapController.ShowMap();
        }
        else
        {
            Debug.LogWarning(
                "[LevelMapManager] MapController is not assigned. " +
                "Cannot show map after victory.",
                this
            );
        }
    }


    // ============================================================
    // COMPLETE CURRENT NODE
    // ============================================================

    private void CompleteCurrentNode()
    {
        if (currentNode == null)
        {
            return;
        }


        currentNode.IsCompleted =
            true;


        currentNode.IsUnlocked =
            false;


        UpdateIconState(
            currentNode
        );


        // --------------------------------------------------------
        // UNLOCK NEXT NODES
        // --------------------------------------------------------

        foreach (
            LevelNode nextNode
            in currentNode.NextNodes
        )
        {
            if (nextNode == null)
            {
                continue;
            }


            UnlockNode(
                nextNode
            );
        }


        Debug.Log(
            "[LevelMapManager] Node completed: " +
            currentNode.Transform.name +
            "\nUnlocked next nodes: " +
            currentNode.NextNodes.Count,
            this
        );


        // --------------------------------------------------------
        // BOSS COMPLETE
        // --------------------------------------------------------

        if (
            currentNode.Row ==
            rows - 1
        )
        {
            Debug.Log(
                "[LevelMapManager] ========================================",
                this
            );


            Debug.Log(
                "[LevelMapManager] BOSS NODE COMPLETED!",
                this
            );


            Debug.Log(
                "[LevelMapManager] MAP COMPLETE!",
                this
            );


            Debug.Log(
                "[LevelMapManager] ========================================",
                this
            );
        }


        currentNode =
            null;
    }


    // ============================================================
    // FIND NODE
    // ============================================================

    private LevelNode FindNode(
        Transform target)
    {
        if (target == null)
        {
            return null;
        }


        foreach (
            List<LevelNode> row
            in mapNodes
        )
        {
            foreach (
                LevelNode node
                in row
            )
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


        if (icon == null)
        {
            return;
        }


        icon.SetNodeState(
            node.IsUnlocked,
            node.IsCompleted
        );


        // --------------------------------------------------------
        // SORTING
        // --------------------------------------------------------

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
    }


    // ============================================================
    // GET CLOSEST NODE
    // ============================================================

    private LevelNode GetClosestNode(
        LevelNode target,
        List<LevelNode> pool)
    {
        LevelNode closest =
            null;


        float minDistance =
            float.MaxValue;


        foreach (
            LevelNode candidate
            in pool
        )
        {
            if (candidate == null)
            {
                continue;
            }


            float distance =
                Vector2.Distance(
                    target.Position,
                    candidate.Position
                );


            if (
                distance <
                minDistance
            )
            {
                minDistance =
                    distance;


                closest =
                    candidate;
            }
        }


        return closest;
    }


    // ============================================================
    // CLEAR MAP
    // ============================================================

    private void ClearMap()
    {
        // --------------------------------------------------------
        // ICONS
        // --------------------------------------------------------

        if (iconParent != null)
        {
            for (
                int i = iconParent.childCount - 1;
                i >= 0;
                i--
            )
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


        // --------------------------------------------------------
        // CONNECTION IMAGES
        // --------------------------------------------------------

        foreach (
            Image line
            in linePool
        )
        {
            if (line != null)
            {
                line.gameObject.SetActive(
                    false
                );
            }
        }


        // --------------------------------------------------------
        // RESET
        // --------------------------------------------------------

        mapNodes.Clear();


        currentNode =
            null;


        mapGenerated =
            false;
    }


    // ============================================================
    // PUBLIC ACCESSORS
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


        return mapNodes[row][column].IsUnlocked;
    }


    public void RegenerateMap()
    {
        GenerateMap();
    }
}