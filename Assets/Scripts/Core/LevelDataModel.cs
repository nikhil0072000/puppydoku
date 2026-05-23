using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelDataModel
{
    public string levelId = "Unknown";
    public string difficulty = "Easy";
    public int gridSize = 4;
    public string levelType = "Normal";
    public string displayName;
    public int[][] colorData;            // jagged array for JSON
    public PrePlacedData[] prePlaced;    // array of {x, y}
    public PrePlacedData[] solution;    // hidden solved puppy positions
    public int winCondition = -1;        // -1 means auto-calculate from colorData

    public LevelType GetParsedLevelType()
    {
        if (string.IsNullOrWhiteSpace(levelType))
            return LevelType.Normal;

        switch (levelType.Trim().ToLowerInvariant())
        {
            case "tutorial":
                return LevelType.Tutorial;
            case "dailychallenge":
            case "daily":
                return LevelType.DailyChallenge;
            case "event":
                return LevelType.Event;
            default:
                return LevelType.Normal;
        }
    }

    public string GetButtonLabel(string fileNamePart = null)
    {
        if (GetParsedLevelType() == LevelType.Tutorial)
            return "Tutorial";

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        int number = ParseNumberFromCandidate(fileNamePart) ?? ParseNumberFromCandidate(levelId) ?? -1;
        if (number > 0)
            return $"Level {number}";

        if (!string.IsNullOrWhiteSpace(levelId) && !string.Equals(levelId, "Unknown", StringComparison.OrdinalIgnoreCase))
            return levelId.Trim();

        if (!string.IsNullOrWhiteSpace(fileNamePart))
            return fileNamePart.Trim();

        return "Level";
    }

    private int? ParseNumberFromCandidate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        string text = candidate.Trim();
        if (text.StartsWith("Level_", StringComparison.OrdinalIgnoreCase))
            text = text["Level_".Length..].Trim();

        if (int.TryParse(text, out int result))
            return result;

        return null;
    }

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

    public int GetSafeWinCondition()
    {
        if (winCondition > 0)
            return winCondition;

        int[,] map = GetSafeColorData();
        HashSet<int> unique = new HashSet<int>();
        int size = GetSafeGridSize();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                unique.Add(map[x, y]);
        return unique.Count;
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

    public Vector2Int[] GetSafeSolution()
    {
        if (solution == null) return new Vector2Int[0];
        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var pp in solution)
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
