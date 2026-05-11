using System.Collections.Generic;
using UnityEngine;

public static class GameRules
{
    public static bool IsPlacementValid(Vector2Int pos, HashSet<Vector2Int> allCats, int[,] zoneMap)
    {
        // Check row
        foreach (var cat in allCats)
            if (cat.x == pos.x) return false;
        // Check column
        foreach (var cat in allCats)
            if (cat.y == pos.y) return false;
        // Check diagonal adjacency
        foreach (var cat in allCats)
            if (Mathf.Abs(cat.x - pos.x) == 1 && Mathf.Abs(cat.y - pos.y) == 1)
                return false;
        // Check zone count (one cat per zone)
        int zoneID = zoneMap[pos.x, pos.y];
        foreach (var cat in allCats)
            if (zoneMap[cat.x, cat.y] == zoneID) return false;

        return true;
    }
}
