using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RegionCheckPowerUp : MonoBehaviour
{
    [Header("Region Check Settings")]
    public int regionChecks = 2;
    public float highlightDuration = 3f;
    public Color validRegionColor = Color.green;
    public Color invalidRegionColor = Color.red;
    public Color neutralRegionColor = Color.yellow;
    
    [Header("Visual Effects")]
    public GameObject regionHighlight;
    public ParticleSystem checkParticles;
    public LineRenderer connectionLine;
    
    [Header("Check Types")]
    public bool showConflicts = true;
    public bool showValidRegions = true;
    public bool showEmptyRegions = true;
    
    private int currentRegionChecks;
    private bool isChecking = false;

    void Start()
    {
        currentRegionChecks = regionChecks;
    }

    public bool UseRegionCheck()
    {
        if (currentRegionChecks <= 0 || isChecking)
            return false;
            
        currentRegionChecks--;
        StartCoroutine(PerformRegionCheck());
        return true;
    }

    System.Collections.IEnumerator PerformRegionCheck()
    {
        isChecking = true;
        
        var puzzle = GameManager.Instance.currentPuzzle;
        var placedCats = GameManager.Instance.playerPlacedCats;
        var grid = GameManager.Instance.gridManager.Cells;
        
        // Analyze all regions
        Dictionary<int, RegionStatus> regionStatuses = AnalyzeRegions(puzzle, placedCats, grid);
        
        // Highlight regions based on their status
        foreach (var kvp in regionStatuses)
        {
            int zoneId = kvp.Key;
            RegionStatus status = kvp.Value;
            
            Color highlightColor = GetColorForStatus(status);
            StartCoroutine(HighlightRegion(zoneId, highlightColor));
        }
        
        // Show connection lines between conflicting cats
        if (showConflicts)
        {
            ShowConflictingCats(placedCats, grid);
        }
        
        // Wait for highlight duration
        yield return new WaitForSeconds(highlightDuration);
        
        // Clear highlights
        ClearRegionHighlights();
        
        isChecking = false;
    }

    Dictionary<int, RegionStatus> AnalyzeRegions(PuzzleData puzzle, HashSet<Vector2Int> placedCats, Cell[,] grid)
    {
        Dictionary<int, RegionStatus> statuses = new Dictionary<int, RegionStatus>();
        
        // Initialize all regions
        for (int zoneId = 0; zoneId < puzzle.gridSize; zoneId++)
        {
            statuses[zoneId] = RegionStatus.Empty;
        }
        
        // Check each placed cat
        foreach (var catPos in placedCats)
        {
            int zoneId = puzzle.zoneMap[catPos.x, catPos.y];
            
            if (statuses[zoneId] == RegionStatus.Empty)
            {
                statuses[zoneId] = RegionStatus.Valid;
            }
            else if (statuses[zoneId] == RegionStatus.Valid)
            {
                statuses[zoneId] = RegionStatus.Invalid; // Multiple cats in same zone
            }
        }
        
        // Check for conflicts (row, column, diagonal)
        foreach (var catPos in placedCats)
        {
            int zoneId = puzzle.zoneMap[catPos.x, catPos.y];
            
            if (HasConflicts(catPos, placedCats))
            {
                statuses[zoneId] = RegionStatus.Invalid;
            }
        }
        
        return statuses;
    }

    bool HasConflicts(Vector2Int catPos, HashSet<Vector2Int> placedCats)
    {
        foreach (var otherCat in placedCats)
        {
            if (otherCat == catPos) continue;
            
            // Same row
            if (otherCat.x == catPos.x) return true;
            
            // Same column
            if (otherCat.y == catPos.y) return true;
            
            // Diagonal adjacency
            if (Mathf.Abs(otherCat.x - catPos.x) == 1 && Mathf.Abs(otherCat.y - catPos.y) == 1)
                return true;
        }
        
        return false;
    }

    Color GetColorForStatus(RegionStatus status)
    {
        switch (status)
        {
            case RegionStatus.Valid:
                return validRegionColor;
            case RegionStatus.Invalid:
                return invalidRegionColor;
            case RegionStatus.Empty:
                return neutralRegionColor;
            default:
                return Color.white;
        }
    }

    System.Collections.IEnumerator HighlightRegion(int zoneId, Color color)
    {
        var puzzle = GameManager.Instance.currentPuzzle;
        var grid = GameManager.Instance.gridManager.Cells;
        
        // Find all cells in this zone
        List<Cell> zoneCells = new List<Cell>();
        for (int x = 0; x < puzzle.gridSize; x++)
        {
            for (int y = 0; y < puzzle.gridSize; y++)
            {
                if (puzzle.zoneMap[x, y] == zoneId)
                {
                    zoneCells.Add(grid[x, y]);
                }
            }
        }
        
        // Create highlight effect
        foreach (var cell in zoneCells)
        {
            StartCoroutine(HighlightCell(cell, color));
        }
        
        yield return null;
    }

    System.Collections.IEnumerator HighlightCell(Cell cell, Color color)
    {
        Color originalColor = cell.zoneOverlay.color;
        float elapsed = 0f;
        
        while (elapsed < highlightDuration)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f;
            cell.zoneOverlay.color = Color.Lerp(originalColor, color, pulse);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cell.zoneOverlay.color = originalColor;
    }

    void ShowConflictingCats(HashSet<Vector2Int> placedCats, Cell[,] grid)
    {
        List<Vector2Int> conflictCats = new List<Vector2Int>();
        
        foreach (var catPos in placedCats)
        {
            if (HasConflicts(catPos, placedCats))
            {
                conflictCats.Add(catPos);
            }
        }
        
        // Draw connection lines between conflicting cats
        if (connectionLine != null && conflictCats.Count > 1)
        {
            connectionLine.gameObject.SetActive(true);
            connectionLine.positionCount = conflictCats.Count;
            
            for (int i = 0; i < conflictCats.Count; i++)
            {
                Vector3 worldPos = grid[conflictCats[i].x, conflictCats[i].y].transform.position;
                connectionLine.SetPosition(i, worldPos);
            }
            
            connectionLine.startColor = Color.red;
            connectionLine.endColor = Color.red;
        }
    }

    void ClearRegionHighlights()
    {
        if (connectionLine != null)
        {
            connectionLine.gameObject.SetActive(false);
        }
    }

    public int GetRemainingRegionChecks()
    {
        return currentRegionChecks;
    }

    public void AddRegionChecks(int amount)
    {
        currentRegionChecks += amount;
    }

    public void ResetRegionChecks()
    {
        currentRegionChecks = regionChecks;
    }
}

public enum RegionStatus
{
    Empty,
    Valid,
    Invalid
}
