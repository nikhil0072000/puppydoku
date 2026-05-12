using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject puppyPrefab;
    [SerializeField] private HUDManager hudManager;

    [Header("Level Data")]
    [SerializeField] private LevelData[] allLevels;     // assign all your LevelData assets here
    [SerializeField] private int startLevelIndex = 0;

    // Level data
    private LevelData currentLevel;
    private int[,] zoneMap;
    private int[] zoneToColorIndex;          // NEW – mapping zoneID → colour index
    private int totalColorCount;             // NEW – number of unique colours (win target)

    private HashSet<Vector2Int> placedPuppies = new HashSet<Vector2Int>();
    private int lives;
    private bool gameOver = false;

    private const int MaxLives = 3;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Cell.OnCellDoubleTapped += OnCellDoubleTapped;

        if (allLevels.Length > 0)
            LoadLevel(allLevels[Mathf.Min(startLevelIndex, allLevels.Length - 1)]);
        else
            Debug.LogError("No level data assigned!");
    }

    void OnDestroy()
    {
        Cell.OnCellDoubleTapped -= OnCellDoubleTapped;
    }

    public void LoadLevel(LevelData level)
    {
        currentLevel = level;
        zoneMap = currentLevel.GetZoneMap();
        int size = currentLevel.gridSize;

        // ---- Build colour index mapping ----
        // zoneColors has one entry per zone ID. We group identical colours.
        Color[] colors = currentLevel.zoneColors;
        Dictionary<Color, int> colorToIndex = new Dictionary<Color, int>();
        zoneToColorIndex = new int[colors.Length];
        int nextIndex = 0;

        for (int i = 0; i < colors.Length; i++)
        {
            if (!colorToIndex.TryGetValue(colors[i], out int idx))
            {
                idx = nextIndex++;
                colorToIndex[colors[i]] = idx;
            }
            zoneToColorIndex[i] = idx;
        }

        totalColorCount = nextIndex;   // number of unique colours

        // Generate grid with original colours for visuals
        gridManager.GenerateGrid(size, zoneMap, colors);

        placedPuppies.Clear();
        lives = MaxLives;
        gameOver = false;

        // Place pre‑placed puppies
        foreach (Vector2Int pos in currentLevel.prePlacedPuppies)
        {
            Cell cell = gridManager.GetCell(pos.x, pos.y);
            if (cell != null)
            {
                Puppy pup = cell.PlacePuppy(puppyPrefab);
                if (pup != null)
                {
                    cell.isGiven = true;
                    placedPuppies.Add(pos);
                }
            }
        }

        if (hudManager != null)
        {
            hudManager.UpdateHearts(lives);
            hudManager.UpdateProgress(placedPuppies.Count, totalColorCount);
        }

        Debug.Log($"Level loaded: {size}x{size}, unique colours: {totalColorCount}, pre-placed: {placedPuppies.Count}");
    }

    // Called when a cell is double‑tapped
    private void OnCellDoubleTapped(Vector2Int pos)
    {
        if (gameOver) return;
        TryPlacePuppy(pos);
    }

    public void TryPlacePuppy(Vector2Int pos)
    {
        if (gameOver) return;

        if (pos.x < 0 || pos.x >= zoneMap.GetLength(0) || pos.y < 0 || pos.y >= zoneMap.GetLength(1))
            return;

        Cell cell = gridManager.GetCell(pos.x, pos.y);
        if (cell == null) return;

        // Already a puppy or given?
        if (cell.GetPuppy() != null)
        {
            Debug.Log("Cell already occupied.");
            return;
        }

        // Use colour‑based rule check
        if (GameRules.IsPlacementValid(pos, placedPuppies, zoneMap, zoneToColorIndex))
        {
            // Valid placement
            Puppy pup = cell.PlacePuppy(puppyPrefab);
            if (pup != null)
            {
                placedPuppies.Add(pos);
                Debug.Log($"Puppy placed at {pos}");

                // Update UI
                if (hudManager != null)
                    hudManager.UpdateProgress(placedPuppies.Count, totalColorCount);

                // Win check
                if (placedPuppies.Count == totalColorCount)
                {
                    Win();
                }
            }
        }
        else
        {
            // Invalid placement → permanent red cross, lose a life
            cell.ShowPermanentRedCross();
            lives--;
            Debug.Log($"Invalid placement at {pos}. Lives left: {lives}");

            if (hudManager != null)
                hudManager.UpdateHearts(lives);

            if (lives <= 0)
            {
                Lose();
            }
        }
    }

    private void Win()
    {
        gameOver = true;
        Debug.Log("🎉 Level Complete!");
        // Later we'll show a win popup
    }

    private void Lose()
    {
        gameOver = true;
        Debug.Log("💀 Game Over – out of lives.");
        // Later we'll show a retry popup
    }

    // Helper to convert hex to Color
    private Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color col);
        return col;
    }
}
