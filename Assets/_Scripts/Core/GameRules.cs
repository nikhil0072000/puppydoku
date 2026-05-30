using System.Collections.Generic;
using UnityEngine;

public static class GameRules
{
    /// <summary>
    /// Checks if placing a puppy at 'pos' is valid.
    /// 'zoneToColorIndex' maps each zone ID to its colour index.
    /// Only one puppy per colour index is allowed.
    /// </summary>
    public static bool IsPlacementValid(Vector2Int pos,
                                        HashSet<Vector2Int> occupiedPositions,
                                        int[,] zoneMap,
                                        int[] zoneToColorIndex,
                                        bool verbose = true)
    {
        int targetZone = zoneMap[pos.x, pos.y];
        int targetColor = zoneToColorIndex[targetZone];

        foreach (var occupied in occupiedPositions)
        {
            int occZone = zoneMap[occupied.x, occupied.y];
            int occColor = zoneToColorIndex[occZone];

            // 1. Same colour → fail (global colour limit)
            if (occColor == targetColor)
            {
                if (verbose) Debug.Log($"Invalid: colour already used at {occupied}");
                return false;
            }

            // 2. Row constraint
            if (occupied.x == pos.x)
            {
                if (verbose) Debug.Log($"Invalid: same row as {occupied}");
                return false;
            }

            // 3. Column constraint
            if (occupied.y == pos.y)
            {
                if (verbose) Debug.Log($"Invalid: same column as {occupied}");
                return false;
            }

            // 4. Diagonal touch constraint
            if (Mathf.Abs(occupied.x - pos.x) == 1 && Mathf.Abs(occupied.y - pos.y) == 1)
            {
                if (verbose) Debug.Log($"Invalid: diagonal touch with {occupied}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the grid positions a puppy at <paramref name="focus"/> forbids by the
    /// row, column, and diagonal-touch rules: its entire row, its entire column, and
    /// its four diagonal neighbours (clamped to the board, excluding the focus cell
    /// itself). These are the cells the Bulb hint marks with an X — every one is
    /// guaranteed to be an illegal placement while that puppy stands.
    /// Same-colour-zone cells are intentionally NOT included (see plan decision).
    /// </summary>
    public static List<Vector2Int> GetRuleRestrictedCells(Vector2Int focus, int gridWidth, int gridHeight)
    {
        var result = new List<Vector2Int>();

        // Full row and column (excluding the focus cell).
        for (int x = 0; x < gridWidth; x++)
            if (x != focus.x) result.Add(new Vector2Int(x, focus.y));
        for (int y = 0; y < gridHeight; y++)
            if (y != focus.y) result.Add(new Vector2Int(focus.x, y));

        // Four diagonal touches.
        AddIfInBounds(result, focus.x - 1, focus.y - 1, gridWidth, gridHeight);
        AddIfInBounds(result, focus.x - 1, focus.y + 1, gridWidth, gridHeight);
        AddIfInBounds(result, focus.x + 1, focus.y - 1, gridWidth, gridHeight);
        AddIfInBounds(result, focus.x + 1, focus.y + 1, gridWidth, gridHeight);

        return result;
    }

    private static void AddIfInBounds(List<Vector2Int> list, int x, int y, int gridWidth, int gridHeight)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            list.Add(new Vector2Int(x, y));
    }
}
