using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform gridParent;

    private int gridSize;
    private Cell[,] cells;
    private int[,] zoneMap;

    public int GridSize => gridSize;

    public void GenerateGrid(int size, int[,] map, Color[] zoneColors)
    {
        gridSize = size;
        zoneMap = map;

        // Clear old children
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        cells = new Cell[gridSize, gridSize];

        SpriteRenderer prefabRenderer = cellPrefab.GetComponent<SpriteRenderer>();
        float cellSize = prefabRenderer.bounds.size.x;

        float startX = -(gridSize - 1) * cellSize / 2f;
        float startY = (gridSize - 1) * cellSize / 2f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int zone = map[x, y];
                Vector3 pos = new Vector3(startX + x * cellSize, startY - y * cellSize, 0);
                GameObject obj = Instantiate(cellPrefab, pos, Quaternion.identity, gridParent);
                Cell cell = obj.GetComponent<Cell>();
                if (cell != null)
                {
                    Color col = (zone >= 0 && zone < zoneColors.Length) ? zoneColors[zone] : Color.white;
                    cell.Init(x, y, zone, col);
                    cells[x, y] = cell;
                }
            }
        }
    }

    public Cell GetCell(int x, int y) => cells[x, y];
    public int[,] GetZoneMap() => zoneMap;
}