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
                                        int[] zoneToColorIndex)
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
                Debug.Log($"Invalid: colour already used at {occupied}");
                return false;
            }

            // 2. Row constraint
            if (occupied.x == pos.x)
            {
                Debug.Log($"Invalid: same row as {occupied}");
                return false;
            }

            // 3. Column constraint
            if (occupied.y == pos.y)
            {
                Debug.Log($"Invalid: same column as {occupied}");
                return false;
            }

            // 4. Diagonal touch constraint
            if (Mathf.Abs(occupied.x - pos.x) == 1 && Mathf.Abs(occupied.y - pos.y) == 1)
            {
                Debug.Log($"Invalid: diagonal touch with {occupied}");
                return false;
            }
        }

        return true;
    }
}
