using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 6;           // change this to scale the grid
    [SerializeField] private GameObject cellPrefab;      // the Cell prefab
    [SerializeField] private Transform gridParent;       // where to parent the cells (can be this transform)

    private Cell[,] cells;                               // store references (optional)

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        // Clear any old cells
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        if (cellPrefab == null)
        {
            Debug.LogError("GridManager: cellPrefab is not assigned!");
            return;
        }

        // Calculate cell size based on the sprite's bounds (assume all cells same size)
        SpriteRenderer prefabRenderer = cellPrefab.GetComponent<SpriteRenderer>();
        if (prefabRenderer == null)
        {
            Debug.LogError("GridManager: cellPrefab must have a SpriteRenderer!");
            return;
        }
        float cellSize = prefabRenderer.bounds.size.x;   // assume square sprite

        // Starting position (top‑left corner)
        float startX = -(gridSize - 1) * cellSize / 2f;
        float startY = (gridSize - 1) * cellSize / 2f;

        cells = new Cell[gridSize, gridSize];

        // ---- Dummy Zone Data (Module 1 only) ----
        // We'll create a simple zone map: each cell's zone is based on (x + y) % gridSize,
        // just so we can see different colours.
        // In later modules, this will be replaced by the real puzzle data.
        int[,] zoneMap = new int[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                zoneMap[x, y] = (x + y) % gridSize;     // dummy zone

        // Spawn cells
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 position = new Vector3(startX + x * cellSize, startY - y * cellSize, 0);
                GameObject cellObj = Instantiate(cellPrefab, position, Quaternion.identity, gridParent);
                Cell cellScript = cellObj.GetComponent<Cell>();
                if (cellScript != null)
                {
                    cellScript.Init(x, y, zoneMap[x, y]);
                    cells[x, y] = cellScript;
                }
                else
                {
                    Debug.LogError("GridManager: Cell prefab is missing Cell script!");
                }
            }
        }

        Debug.Log($"Grid generated: {gridSize}x{gridSize}");
    }
}