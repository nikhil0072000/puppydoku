using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Grid Scaling (Portrait 9:16 @ 1920x1080)")]
    [Tooltip("How tightly the grid fills the portrait width.\n" +
             "  0.48 = 4×4 looks the same as original (grid = 2.7 world units = 48% of portrait width).\n" +
             "  Higher = zoom in (cells get bigger); max 0.95.\n" +
             "  Larger grids (8×8, 12×12) auto-scale down to stay within the same portrait fraction.\n" +
             "  Tune this once in the Inspector — the formula handles all grid sizes uniformly.")]
    [Range(0.1f, 0.95f)]
    [SerializeField] private float screenCoverageFraction = 0.48f;
    [Tooltip("Fine-tune zoom on top of screenCoverageFraction: 1.0 = no change, 0.95 = 5% zoom in. " +
             "Stagger/intro animations always use the prefab's authored scale and are unaffected.")]
    [Range(0.5f, 2f)]
    [SerializeField] private float extraScaleMultiplier = 1f;

    [Header("Grid Spacing")]
    [Tooltip("Space between adjacent cells as a fraction of the fitted cell size. 0 = no gap.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float cellSpacingFraction = 0.05f;

    [Header("Cell Size Detection")]
    [Tooltip("Manually override the detected cell sprite size (world units). Use when the SpriteRenderer's bounds don't reflect the visible cell size. Set to 0 to use auto-detection.")]
    [SerializeField] private float manualCellSizeOverride = 0f;

    private int gridSize;
    private Cell[,] cells;
    private int[,] zoneMap;

    public int GridSize => gridSize;

    /// <summary>
    /// Screen-space fraction that the fitted grid will occupy on the shorter axis.
    /// Adjust in the Inspector to zoom the grid in or out without changing gameplay.
    /// </summary>
    public float ScreenCoverageFraction
    {
        get => screenCoverageFraction;
        set => screenCoverageFraction = Mathf.Clamp(value, 0.3f, 0.95f);
    }

    /// <summary>
    /// Extra multiplier on top of the fitted scale.
    /// Stagger animations use the prefab's authored scale and are unaffected by this.
    /// </summary>
    public float ExtraScaleMultiplier
    {
        get => extraScaleMultiplier;
        set => extraScaleMultiplier = Mathf.Clamp(value, 0.1f, 2f);
    }

    public void GenerateGrid(int size, int[,] zoneMapData, Color[] zoneColors)
    {
        gridSize = size;
        zoneMap = zoneMapData;

        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        cells = new Cell[gridSize, gridSize];

        float authoredCellWorldSize = GetAuthoredCellSize();
        float targetCellWorldSize = ComputeFittedCellSize(authoredCellWorldSize);
        float cellslocalScale = targetCellWorldSize / authoredCellWorldSize;
        float cellGapWorldSize = targetCellWorldSize * cellSpacingFraction;
        float step = targetCellWorldSize + cellGapWorldSize;

        float totalGridWorldWidth = gridSize * targetCellWorldSize + (gridSize - 1) * cellGapWorldSize;
        float startX = -totalGridWorldWidth / 2f + targetCellWorldSize / 2f;
        float totalGridWorldHeight = gridSize * targetCellWorldSize + (gridSize - 1) * cellGapWorldSize;
        float startY = totalGridWorldHeight / 2f - targetCellWorldSize / 2f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int zoneID = zoneMap[x, y];
                Vector3 pos = new Vector3(startX + x * step, startY - y * step, 0);
                GameObject obj = Instantiate(cellPrefab, pos, Quaternion.identity, gridParent);
                Cell cell = obj.GetComponent<Cell>();
                if (cell != null)
                {
                    Color col = (zoneID >= 0 && zoneID < zoneColors.Length) ? zoneColors[zoneID] : Color.white;
                    cell.Init(x, y, zoneID, col);
                    obj.transform.localScale = cell.RestingScale * cellslocalScale;
                    cells[x, y] = cell;
                }
                else
                {
                    Debug.LogError("Cell prefab does not contain Cell script!");
                }
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[GridManager] Grid {gridSize}x{gridSize} fitted to {targetCellWorldSize:F3} world units/cell " +
                   $"(authored={authoredCellWorldSize:F3}, scale={cellslocalScale:F3}x, coverage={screenCoverageFraction:P0}).");
#endif
    }

    /// <summary>
    /// Detects the cell's authored sprite size in world units by reading the
    /// SpriteRenderer's bounds on the prefab, then correcting for its localScale.
    /// Falls back to the Cell script's <see cref="Cell.restingScale"/> if no
    /// SpriteRenderer is found.
    /// </summary>
    private float GetAuthoredCellSize()
    {
        if (manualCellSizeOverride > 0f)
            return manualCellSizeOverride;

        SpriteRenderer sr = cellPrefab.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return sr.bounds.size.x;
        }

        Cell cellScript = cellPrefab.GetComponent<Cell>();
        if (cellScript != null)
        {
            float prefabScale = cellScript.RestingScale.x;
            if (prefabScale <= 0f) prefabScale = 0.3f;
            SpriteRenderer zoneOverlay = cellPrefab.transform.Find("ZoneOverlay")?.GetComponent<SpriteRenderer>();
            if (zoneOverlay != null && zoneOverlay.sprite != null)
                return zoneOverlay.sprite.bounds.size.x * prefabScale;
            return prefabScale * 2.25f;
        }

        Debug.LogWarning("[GridManager] Could not detect cell size — using default 0.675.");
        return 0.675f;
    }

    /// <summary>
    /// Computes the largest cell size that keeps the full grid within
    /// <see cref="screenCoverageFraction"/> of the portrait width (the short axis
    /// in both orientations), then applies <see cref="extraScaleMultiplier"/> on top.
    ///
    /// Formula (portrait = shortAxis, landscape = normalised to portraitWidth):
    ///   portraitWidth = orthoSize × 2 × aspect
    ///   fitSize = portraitWidth × coverage / gridSize
    ///   finalSize = fitSize × extraScaleMultiplier
    ///
    /// Uses the portrait width as the normalisation base in both orientations so
    /// the grid looks the same relative to the narrow dimension regardless of how
    /// the device is held.
    /// </summary>
    private float ComputeFittedCellSize(float authoredCellSize)
    {
        Camera cam = Camera.main;
        float orthoSize = (cam != null) ? cam.orthographicSize : 5f;
        float aspect = (Screen.width > 0 && Screen.height > 0)
            ? (float)Screen.width / Screen.height
            : 9f / 16f; // 9:16 portrait default

        float portraitWidth = orthoSize * 2f * aspect;
        float spanUnits = gridSize + (gridSize - 1) * cellSpacingFraction;

        // Boost larger grids so 12x12 is more readable on screen while still
        // fitting within the target coverage fraction.
        float gridSizeBoost = Mathf.Lerp(1f, 1.8f, Mathf.Clamp01((gridSize - 4f) / 8f));
        float fitCellWorldSize = (portraitWidth * screenCoverageFraction * extraScaleMultiplier * gridSizeBoost) / spanUnits;

        Debug.Log($"[GridManager] Screen={Screen.width}x{Screen.height}, aspect={aspect:F3}, " +
                   $"portraitWidth={portraitWidth:F3}, coverage={screenCoverageFraction:P0}, " +
                   $"gap={cellSpacingFraction:P0}, boost={gridSizeBoost:F3}, extra={extraScaleMultiplier:F2}, grid={gridSize}, cell={fitCellWorldSize:F4}");

        return fitCellWorldSize;
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