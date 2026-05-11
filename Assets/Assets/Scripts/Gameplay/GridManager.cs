using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform gridParent;
    public Cell[,] Cells { get; private set; }

    public void GenerateGrid(PuzzleData puzzle)
    {
        int size = puzzle.gridSize;
        Cells = new Cell[size, size];
        // Clear existing
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        float cellSize = 1f; // adjust based on screen
        Vector2 origin = new Vector2(-(size - 1) * cellSize / 2f, -(size - 1) * cellSize / 2f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridParent);
                cellObj.transform.localPosition = new Vector3(origin.x + x * cellSize, origin.y + y * cellSize, 0);
                Cell cell = cellObj.GetComponent<Cell>();
                cell.Init(new Vector2Int(x, y), puzzle.zoneMap[x, y]);
                Cells[x, y] = cell;
            }
        }
    }
}
