using System.Collections.Generic;
using UnityEngine;

public class LEVELMAPMANAGER : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private LineRenderer linePrefab;

    [Header("Map Settings")]
    [SerializeField] private int numberOfRows = 8;
    [SerializeField] private int nodesPerRow = 3;

    [SerializeField] private float horizontalSpacing = 3f;
    [SerializeField] private float verticalSpacing = 2.5f;

    private List<List<LevelNode>> map = new List<List<LevelNode>>();

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        // Create every row
        for (int row = 0; row < numberOfRows; row++)
        {
            List<LevelNode> currentRow = new List<LevelNode>();

            for (int i = 0; i < nodesPerRow; i++)
            {
                float x = (i - (nodesPerRow - 1) / 2f) * horizontalSpacing;
                float y = row * verticalSpacing;

                Vector3 position = new Vector3(x, y, 0);

                GameObject icon = Instantiate(
                    iconPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                LevelNode node = new LevelNode(icon, position);
                currentRow.Add(node);
            }

            map.Add(currentRow);
        }

        // Connect rows together
        for (int row = 0; row < map.Count - 1; row++)
        {
            ConnectRows(map[row], map[row + 1]);
        }
    }

    void ConnectRows(List<LevelNode> currentRow, List<LevelNode> nextRow)
    {
        foreach (LevelNode node in currentRow)
        {
            // Find closest node in next row
            LevelNode closest = null;
            float closestDistance = Mathf.Infinity;

            foreach (LevelNode nextNode in nextRow)
            {
                float distance = Vector3.Distance(
                    node.position,
                    nextNode.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = nextNode;
                }
            }

            if (closest != null)
            {
                CreateLine(node.position, closest.position);
            }
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        LineRenderer line = Instantiate(linePrefab, transform);

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}