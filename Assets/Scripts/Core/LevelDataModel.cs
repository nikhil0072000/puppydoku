using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelDataModel
{
    public string levelId = "Unknown";
    public string difficulty = "Easy";
    public int gridSize = 4;
    public int[][] colorData;            // jagged array for JSON
    public PrePlacedData[] prePlaced;    // array of {x, y}

    // ----- Safe accessors with fallbacks -----
    public int GetSafeGridSize() => gridSize > 0 ? gridSize : 4;

    public int[,] GetSafeColorData()
    {
        int size = GetSafeGridSize();
        int[,] map = new int[size, size];

        if (colorData == null || colorData.Length < size)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    map[x, y] = 0;
            return map;
        }

        for (int y = 0; y < size; y++)
        {
            int[] row = colorData[y];
            if (row == null || row.Length < size)
            {
                for (int x = 0; x < size; x++)
                    map[x, y] = 0;
                continue;
            }
            for (int x = 0; x < size; x++)
                map[x, y] = row[x];
        }
        return map;
    }

    public Vector2Int[] GetSafePrePlaced()
    {
        if (prePlaced == null) return new Vector2Int[0];
        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var pp in prePlaced)
            if (pp != null)
                result.Add(new Vector2Int(pp.x, pp.y));
        return result.ToArray();
    }
}

[Serializable]
public class PrePlacedData
{
    public int x;
    public int y;
}
