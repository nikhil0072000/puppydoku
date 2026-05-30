using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Level_", menuName = "PuppyPuzzle/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridSize = 4;

    [Header("Zone Map")]
    public ZoneRow[] rows;          // each row is an array of zone IDs

    [Header("Zone Colors")]
    public Color[] zoneColors;      // matching the zone IDs used in rows

    [Header("Pre-placed Puppies")]
    public Vector2Int[] prePlacedPuppies;

    // Helper to build a 2D int array
    public int[,] GetZoneMap()
    {
        int[,] map = new int[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                map[x, y] = rows[y].columns[x];   // rows[y][x] – watch X/Y order
            }
        }
        return map;
    }

    public int TotalZones
    {
        get
        {
            // Count unique zone IDs present in the map
            HashSet<int> unique = new HashSet<int>();
            for (int y = 0; y < gridSize; y++)
                for (int x = 0; x < gridSize; x++)
                    unique.Add(rows[y].columns[x]);
            return unique.Count;
        }
    }
}

[System.Serializable]
public class ZoneRow
{
    public int[] columns;   // zone IDs for this row
}
