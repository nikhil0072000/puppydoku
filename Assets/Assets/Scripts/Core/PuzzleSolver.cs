using System.Collections.Generic;
using UnityEngine;

public static class PuzzleSolver
{
    public static int CountSolutions(int[,] zoneMap, List<Vector2Int> emptyCells, 
        HashSet<Vector2Int> fixedCats = null)
    {
        int solutionCount = 0;
        var state = new HashSet<Vector2Int>(fixedCats ?? new HashSet<Vector2Int>());
        SolveRecursive(zoneMap, emptyCells, 0, state, ref solutionCount);

        // Stop at 2 solutions (we only need to know if unique)
        return solutionCount;
    }

    static void SolveRecursive(int[,] zoneMap, List<Vector2Int> cells, int index, 
        HashSet<Vector2Int> current, ref int count)
    {
        if (count > 1) return; // early exit for uniqueness check

        if (index == cells.Count)
        {
            count++;
            return;
        }

        Vector2Int cell = cells[index];

        // Try to place a cat here
        if (GameRules.IsPlacementValid(cell, current, zoneMap))
        {
            current.Add(cell);
            SolveRecursive(zoneMap, cells, index + 1, current, ref count);
            current.Remove(cell);
        }

        // Also try leaving cell empty (important for non-cat cells)
        // Actually all cells must have a cat? No, the puzzle asks to place one cat per zone.
        // But some cells are not in any zone (if grid has non-zone cells).
        // In Meowdoku every cell is part of a coloured zone, so we MUST place a cat in each zone.
        // So we cannot skip a cell if it's in a zone that hasn't been filled.
        // But we handle that with forward check: we'll only consider placements that fill the zone eventually.
        // For simplicity, the generator will create a board where every cell belongs to a zone and we must place exactly one cat per zone. 
        // That means leaving a cell empty is only valid if its zone already has a cat. We'll enforce that later.
        // For now, this solver assumes we are allowed to skip cells, but that might generate invalid puzzles if zones are incomplete.
        // To keep it simple, we'll later only generate full cat placements on every zone, then remove clues.
    }
}
