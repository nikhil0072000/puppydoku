using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public int gridSize;
    public int zoneCount;
    public float clueDensity;
    public int maxLives;
    public bool isUnlocked;
    public bool isCompleted;
    public int bestScore;
    public float bestTime;
    
    public LevelData(int level)
    {
        levelNumber = level;
        gridSize = GetGridSizeForLevel(level);
        zoneCount = gridSize;
        clueDensity = GetClueDensityForLevel(level);
        maxLives = 3;
        isUnlocked = level == 1; // Only first level unlocked by default
        isCompleted = false;
        bestScore = 0;
        bestTime = float.MaxValue;
    }
    
    private int GetGridSizeForLevel(int level)
    {
        if (level <= 20) return 6;
        if (level <= 50) return 8;
        return 10;
    }
    
    private float GetClueDensityForLevel(int level)
    {
        float factor = Mathf.Pow(1f - (level / 100f), 0.7f);
        return Mathf.Clamp(factor, 0.15f, 0.8f);
    }
}
