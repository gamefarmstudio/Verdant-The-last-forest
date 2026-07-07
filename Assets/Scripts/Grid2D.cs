using System.Collections.Generic;
using UnityEngine;

public class Grid2D : MonoBehaviour
{
    [Header("Grid Size & Setup")]
    [Tooltip("The total width and height of your level's pathfinding area.")]
    public Vector2 gridWorldSize = new Vector2(20, 10);
    public float nodeRadius = 0.15f;

    [Header("Path Setup")]
    [Tooltip("Make sure all your dirt path sprites have this exact Tag!")]
    public string pathTag = "Path";

    private Node2D[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;
    private Vector3 bottomLeft;

    void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        // Calculate the bottom-left corner based on the center of the GameObject
        bottomLeft = transform.position - Vector3.right * (gridWorldSize.x / 2) - Vector3.up * (gridWorldSize.y / 2);

        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node2D[gridSizeX, gridSizeY];

        // Find all dirt paths
        GameObject[] pathObjects = GameObject.FindGameObjectsWithTag(pathTag);
        List<SpriteRenderer> actualPathSprites = new List<SpriteRenderer>();

        foreach (GameObject obj in pathObjects)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) actualPathSprites.Add(sr);
        }

        // Generate the nodes
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = bottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                bool walkable = false;

                foreach (SpriteRenderer path in actualPathSprites)
                {
                    if (path.bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, path.transform.position.z)))
                    {
                        walkable = true;
                        break;
                    }
                }

                grid[x, y] = new Node2D(walkable, worldPoint, x, y);
            }
        }
    }

    public Node2D NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x - bottomLeft.x) / gridWorldSize.x;
        float percentY = (worldPosition.y - bottomLeft.y) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public List<Node2D> GetNeighbors(Node2D node)
    {
        List<Node2D> neighbors = new List<Node2D>();

        // 4-Way Movement only
        Vector2Int[] directions = {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            int checkX = node.gridX + dir.x;
            int checkY = node.gridY + dir.y;

            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
                neighbors.Add(grid[checkX, checkY]);
            }
        }
        return neighbors;
    }

    void OnDrawGizmos()
    {
        // Draw a giant yellow outline showing exactly how big your grid is!
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

        if (grid != null)
        {
            foreach (Node2D n in grid)
            {
                if (n.walkable)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.4f);
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.02f));
                }
            }
        }
    }
}