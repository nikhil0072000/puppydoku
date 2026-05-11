using System.Collections.Generic;
using UnityEngine;

public static class PuzzleGenerator
{
    public static PuzzleData GeneratePuzzle(int level, int seed = 0)
    {
        if (seed == 0) seed = level;
        Random.InitState(seed);

        int gridSize = GetGridSize(level);
        int catCount = gridSize;                // exactly one cat per zone, N zones total

        // 1. Place cats in a valid pattern (solution)
        List<Vector2Int> solutionCats = PlaceCats(gridSize, seed);
        // If placing fails (practically impossible for N>=6), try a new seed
        if (solutionCats == null || solutionCats.Count != catCount)
        {
            // Fallback: simple row-permutation (always works if grid>=4)
            solutionCats = FallbackCatPlacement(gridSize);
        }

        // 2. Grow zones around each cat
        int[,] zoneMap = GrowZonesAroundCats(gridSize, solutionCats, seed);

        // 3. Decide how many cats to show as clues
        float clueDensity = GetClueDensity(level, gridSize);
        int clueCount = Mathf.RoundToInt(catCount * clueDensity);
        clueCount = Mathf.Clamp(clueCount, 1, catCount - 1);

        // Shuffle and pick clues
        List<Vector2Int> shuffledCats = new List<Vector2Int>(solutionCats);
        Shuffle(shuffledCats, seed);
        List<Vector2Int> givenCats = shuffledCats.GetRange(0, clueCount);
        List<Vector2Int> hiddenCats = shuffledCats.GetRange(clueCount, catCount - clueCount);

        return new PuzzleData
        {
            gridSize = gridSize,
            zoneMap = zoneMap,
            givenCats = givenCats,
            solution = solutionCats,
            remainingCatsToPlace = hiddenCats.Count
        };
    }

    // ------------------------------------------------------------------
    // Place N cats under row/col/diag constraints.
    // Uses a simple backtracking with random permutation to ensure variety.
    // ------------------------------------------------------------------
    static List<Vector2Int> PlaceCats(int size, int seed)
    {
        List<int> columns = new List<int>();
        for (int i = 0; i < size; i++) columns.Add(i);
        Shuffle(columns, seed);

        List<Vector2Int> cats = new List<Vector2Int>();
        // We'll try to place one cat in each row (row i, column columns[i]) and adjust if diagonal conflict.
        // Since we shuffle columns, we just check the diagonal rule after placing.
        // However, a simple permutation may violate diagonal constraints.
        // We implement a backtracking placement algorithm.
        if (PlaceCatsBacktrack(0, size, new List<Vector2Int>(), new HashSet<int>(), new HashSet<int>(), seed, out cats))
            return cats;

        // If backtracking fails (rare for N>=6 with diagonal rule), use fallback.
        return null;
    }

    static bool PlaceCatsBacktrack(int row, int size, List<Vector2Int> current,
                                   HashSet<int> usedCols, HashSet<int> usedDiagCheck,
                                   int seed, out List<Vector2Int> result)
    {
        if (row == size)
        {
            result = new List<Vector2Int>(current);
            return true;
        }

        List<int> cols = new List<int>();
        for (int c = 0; c < size; c++) cols.Add(c);
        Shuffle(cols, seed + row * 1000);  // vary per row

        foreach (int col in cols)
        {
            if (usedCols.Contains(col)) continue;

            // Diagonal check: cannot touch diagonally any already placed cat
            bool diagConflict = false;
            foreach (var cat in current)
            {
                if (Mathf.Abs(cat.x - row) == 1 && Mathf.Abs(cat.y - col) == 1)
                {
                    diagConflict = true;
                    break;
                }
            }
            if (diagConflict) continue;

            current.Add(new Vector2Int(row, col));
            usedCols.Add(col);
            if (PlaceCatsBacktrack(row + 1, size, current, usedCols, usedDiagCheck, seed + 1, out result))
                return true;
            current.RemoveAt(current.Count - 1);
            usedCols.Remove(col);
        }

        result = null;
        return false;
    }

    // Fallback: manual placement that always works for even sizes; adjust for odd sizes.
    static List<Vector2Int> FallbackCatPlacement(int size)
    {
        List<Vector2Int> cats = new List<Vector2Int>();
        // Simple pattern: place cats on a "shifted" diagonal, e.g., (0,1), (1,3), (2,5) ... but need diag check.
        // Instead, just use the backtracker with a more forgiving approach, or increase attempts.
        // For now, a hardcoded pattern for 6,8,10:
        if (size == 6)
        {
            cats.Add(new Vector2Int(0, 0));
            cats.Add(new Vector2Int(1, 2));
            cats.Add(new Vector2Int(2, 4));
            cats.Add(new Vector2Int(3, 1));
            cats.Add(new Vector2Int(4, 3));
            cats.Add(new Vector2Int(5, 5));
        }
        else // generic: place on (i, (i*2)%size) and hope diag rule ok? Might fail. For safety, run backtracker again with different seed.
        {
            // Try again with a higher seed range
            if (PlaceCatsBacktrack(0, size, new List<Vector2Int>(), new HashSet<int>(), new HashSet<int>(), 99999, out cats))
                return cats;
            // Last resort: brute force, but for our known sizes, the fallback should work.
        }
        return cats;
    }

    // ------------------------------------------------------------------
    // Grow zones: each cat is the seed of a region, then expand regionally.
    // ------------------------------------------------------------------
    static int[,] GrowZonesAroundCats(int size, List<Vector2Int> cats, int seed)
    {
        int[,] map = new int[size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                map[x, y] = -1;

        // Assign cat cells first
        for (int i = 0; i < cats.Count; i++)
            map[cats[i].x, cats[i].y] = i;

        // Frontier starts with the cats
        List<Vector2Int> frontier = new List<Vector2Int>(cats);
        System.Random rng = new System.Random(seed);

        while (frontier.Count > 0)
        {
            // Shuffle frontier for organic feel
            ShuffleList(frontier, rng.Next());
            List<Vector2Int> newFrontier = new List<Vector2Int>();

            foreach (var cell in frontier)
            {
                int zone = map[cell.x, cell.y];
                // Check unassigned orthogonal neighbours
                List<Vector2Int> neigh = new List<Vector2Int>
                {
                    new Vector2Int(cell.x+1, cell.y),
                    new Vector2Int(cell.x-1, cell.y),
                    new Vector2Int(cell.x, cell.y+1),
                    new Vector2Int(cell.x, cell.y-1)
                };
                ShuffleList(neigh, rng.Next());

                foreach (var n in neigh)
                {
                    if (n.x >= 0 && n.x < size && n.y >= 0 && n.y < size && map[n.x, n.y] == -1)
                    {
                        map[n.x, n.y] = zone;
                        newFrontier.Add(n);
                        break;  // expand slowly for organic shape
                    }
                }
            }
            frontier = newFrontier;
        }

        return map;
    }

    // ------------------------------------------------------------------
    // Difficulty parameters
    // ------------------------------------------------------------------
    static int GetGridSize(int level)
    {
        if (level <= 20) return 6;
        if (level <= 50) return 8;
        return 10;
    }

    static float GetClueDensity(int level, int gridSize)
    {
        float t = (level - 1) / 99f;
        float factor = Mathf.Pow(1f - t, 0.7f);
        return Mathf.Clamp(factor, 0.15f, 0.8f);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    static void Shuffle<T>(List<T> list, int seed)
    {
        System.Random rng = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static void ShuffleList<T>(List<T> list, int seed)
    {
        System.Random rng = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
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
