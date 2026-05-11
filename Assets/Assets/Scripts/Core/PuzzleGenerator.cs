using System.Collections.Generic;
using UnityEngine;

public class PuzzleGenerator : MonoBehaviour
{
    public static PuzzleData GeneratePuzzle(int level, int seed = 0)
    {
        if (seed == 0) seed = level;
        Random.InitState(seed);

        int gridSize = GetGridSize(level);
        int zoneCount = gridSize; // enough zones to cover grid?
        // Actually zone count = number of cats to place, which = number of zones.
        // In a 6x6 grid we need 6 zones (since each zone gets one cat, rows/cols constraints imply gridSize x gridSize? 
        // Let's think: If we place one cat per row and per column (Sudoku-like), we'd need gridSize cats. 
        // Meowdoku is not full Sudoku (all cells filled), it's about placing cats in some cells, one per coloured zone. 
        // The zones cover the whole grid. Number of zones = number of cats to place = gridSize? 
        // Actually from description: "place one cat in each region". Regions are colored zones that partition the board. 
        // The number of regions = number of cats = gridSize (since no two cats share row/col). So each row and column will have exactly one cat? 
        // The rules do NOT enforce one per row/column, they say "cannot be in same row or column". That means at most one per row/col. 
        // So we could have fewer cats than gridSize? But each region must have one cat, so number of regions = number of cats. 
        // Typically in such puzzles, number of cats = gridSize (making it a Latin square of cats), but not necessarily. 
        // Let's assume for simplicity: the puzzle is an N x N grid, with N coloured regions, each region having N cells? 
        // No, that would make each region a full row? Not. It's a partition into N contiguous shapes, each must contain exactly one cat. 
        // That implies the cat count = N. For 6x6, we'll have 6 cats exactly. So yes, it becomes a "queen's placement" puzzle. 

        int size = gridSize;
        int catCount = size;

        // Step 1: generate random partition into catCount regions
        int[,] zoneMap = GenerateZones(size, catCount, seed);

        // Step 2: find a valid placement of cats (full solution)
        List<Vector2Int> solutionCats = GenerateFullSolution(size, zoneMap, seed);

        // Step 3: determine clues (some cats given, others hidden)
        float clueDensity = GetClueDensity(level, size);
        int clueCount = Mathf.RoundToInt(catCount * clueDensity);
        // We will randomly select clueCount cats to be "shown" initially.
        // The rest become hidden (player must deduce where they go).
        // However, the game might allow empty cells that are not part of any cat placement? 
        // In Meowdoku you only place a cat where you think it belongs; non-cat cells are just empty. 
        // So the initial board shows some cats already placed, and the player must place the remaining cats in the correct cells.

        List<Vector2Int> givenCats = new List<Vector2Int>();
        List<Vector2Int> remainingCats = new List<Vector2Int>(solutionCats);
        // Shuffle solutionCats and pick first clueCount as given
        Shuffle(remainingCats, seed);
        for (int i = 0; i < clueCount; i++)
        {
            givenCats.Add(remainingCats[i]);
        }
        remainingCats.RemoveRange(0, clueCount);

        // Now we need to ensure the reduced puzzle has exactly one solution.
        // We'll iteratively remove more clues while maintaining uniqueness.
        // Actually we already set clueCount based on density; we can test and adjust if needed.
        // This is a quick generator; for robustness we'd do a proper uniqueness check while removing.

        return new PuzzleData
        {
            gridSize = size,
            zoneMap = zoneMap,
            givenCats = givenCats,
            solution = solutionCats,
            remainingCatsToPlace = remainingCats.Count
        };
    }

    static int[,] GenerateZones(int size, int zoneCount, int seed)
    {
        // Simple approach: use Voronoi or flood fill from random seeds.
        // For brevity, we'll just create a checkerboard style partition.
        // In a real game you'd want organic shapes. We'll do a simple region growing.
        // (Omitted for brevity, but you can implement flood fill from random points)
        // Returning a dummy assignment where each row is a zone (bad for gameplay, but placeholder)
        int[,] map = new int[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                map[x, y] = y; // each row one zone, will work but too easy.
        return map;
    }

    static List<Vector2Int> GenerateFullSolution(int size, int[,] zoneMap, int seed)
    {
        // Use backtracking to place exactly one cat in each zone, meeting row/col/diag constraints.
        // This is a classic N-queens problem adapted for zones.
        // We'll implement a simple backtracker with zone constraint.
        // (Omitted for brevity, but you can use a recursive placement algorithm.)
        // For demo: return a manual placement.
        return new List<Vector2Int> { /* ... */ };
    }

    // Difficulty parameters
    static int GetGridSize(int level)
    {
        if (level <= 20) return 6;
        if (level <= 50) return 8;
        return 10;
    }

    static float GetClueDensity(int level, int gridSize)
    {
        float maxClues = gridSize; // all cats given = trivial
        float factor = Mathf.Pow(1f - (level / 100f), 0.7f);
        return Mathf.Clamp(factor, 0.15f, 0.8f);
    }

    static void Shuffle<T>(List<T> list, int seed)
    {
        System.Random rng = new System.Random(seed);
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public struct PuzzleData
{
    public int gridSize;
    public int[,] zoneMap;
    public List<Vector2Int> givenCats;
    public List<Vector2Int> solution;
    public int remainingCatsToPlace;
}
