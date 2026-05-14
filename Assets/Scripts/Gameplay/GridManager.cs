using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab; // the Cell prefab
    [SerializeField] private Transform gridParent; // where to spawn cells

    private int gridSize;
    private Cell[,] cells;
    private int[,] zoneMap; // stored for later rule checks

    public int GridSize => gridSize;

    public void GenerateGrid(int size, int[,] zoneMapData, Color[] zoneColors)
    {
        gridSize = size;
        zoneMap = zoneMapData;

        // Clear old children
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        cells = new Cell[gridSize, gridSize];

        SpriteRenderer prefabRenderer = cellPrefab.GetComponent<SpriteRenderer>();
        if (prefabRenderer == null)
        {
            Debug.LogError("Cell prefab missing SpriteRenderer!");
            return;
        }
        float cellSize = prefabRenderer.bounds.size.x;

        float startX = -(gridSize - 1) * cellSize / 2f;
        float startY = (gridSize - 1) * cellSize / 2f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int zoneID = zoneMap[x, y];
                Vector3 pos = new Vector3(startX + x * cellSize, startY - y * cellSize, 0);
                GameObject obj = Instantiate(cellPrefab, pos, Quaternion.identity, gridParent);
                Cell cell = obj.GetComponent<Cell>();
                if (cell != null)
                {
                    // Pass the actual colour for this zone
                    Color col = (zoneID >= 0 && zoneID < zoneColors.Length) ? zoneColors[zoneID] : Color.white;
                    cell.Init(x, y, zoneID, col);
                    cells[x, y] = cell;
                }
                else
                {
                    Debug.LogError("Cell prefab does not contain Cell script!");
                }
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (cells != null && x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            return cells[x, y];
        return null;
    }

    public int[,] GetZoneMap() => zoneMap;

    /// <summary>
    /// Returns all cells ordered by diagonal distance (x+y), then by x, so an
    /// intro animation steps through the grid in a wave from the top-left.
    /// One-shot helper used at level load — not a hot path.
    /// </summary>
    public List<Cell> GetCellsInDiagonalOrder()
    {
        List<Cell> ordered = new List<Cell>(gridSize * gridSize);
        if (cells == null) return ordered;

        int maxDiagonal = (gridSize - 1) * 2;
        for (int d = 0; d <= maxDiagonal; d++)
        {
            int xStart = Mathf.Max(0, d - (gridSize - 1));
            int xEnd = Mathf.Min(d, gridSize - 1);
            for (int x = xStart; x <= xEnd; x++)
            {
                int y = d - x;
                Cell cell = cells[x, y];
                if (cell != null) ordered.Add(cell);
            }
        }
        return ordered;
    }
}