using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool LevelComplete = false;
    public static bool LevelFailed = false;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject puppyPrefab;
    [SerializeField] private HUDManager hudManager;
    [SerializeField] private GridAnimator gridAnimator;

    [Header("Level Data")]
    // Removed ScriptableObject level array. Levels are now loaded via LevelLoader (JSON).
    // CurrentLevelData holds the loaded JSON data.
    // private LevelData[] allLevels; // no longer used
    // private int startLevelIndex = 0; // no longer used

    // Level data
    // No longer using ScriptableObject LevelData
    // private LevelData currentLevel;
    private int[,] zoneMap;
    private int[] zoneToColorIndex;          // NEW - mapping zoneID → colour index
    private int totalColorCount;             // NEW - number of unique colours (win target)

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

        // Load level from LevelLoader singleton (JSON based)
        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelData != null)
        {
            StartCoroutine(LoadLevelWithAnimation(LevelLoader.Instance.CurrentLevelData));
        }
        else
        {
            Debug.LogError("LevelLoader or CurrentLevelData not initialized!");
        }
    }

    void OnDestroy()
    {
        Cell.OnCellDoubleTapped -= OnCellDoubleTapped;
    }

    /// <summary>
    /// Load level data from the JSON based LevelDataModel.
    /// </summary>
    // public void LoadLevelFromJson(LevelDataModel model)
    // {
    //     // Convert safe data to internal structures
    //     int size = model.GetSafeGridSize();
    //     zoneMap = model.GetSafeColorData();

    //     // Determine the highest zone ID used in the map
    //     int maxZoneId = -1;
    //     for (int y = 0; y < size; y++)
    //         for (int x = 0; x < size; x++)
    //             if (zoneMap[x, y] > maxZoneId) maxZoneId = zoneMap[x, y];

    //     int colourCount = maxZoneId + 1;
    //     Color[] colors = new Color[colourCount];

    //     // Load colours from the GridConfig asset (instead of random HSV)
    //     GridConfig config = Resources.Load<GridConfig>("GridConfig");
    //     if (config == null)
    //     {
    //         Debug.LogError("GridConfig asset missing from Resources! Using white fallback.");
    //     }

    //     for (int i = 0; i < colourCount; i++)
    //     {
    //         if (config != null)
    //             colors[i] = config.GetColor((ColorID)i); // cast int to ColorID enum
    //         else
    //             colors[i] = Color.white; // fallback
    //     }

    //     // In this representation, each zone ID directly maps to a colour index.
    //     zoneToColorIndex = new int[colourCount];
    //     for (int i = 0; i < colourCount; i++) zoneToColorIndex[i] = i;
    //     totalColorCount = colourCount;

    //     // Generate grid with colours
    //     gridManager.GenerateGrid(size, zoneMap, colors);

    //     placedPuppies.Clear();
    //     lives = MaxLives;
    //     gameOver = false;
    //     LevelComplete = false;
    //     LevelFailed = false;

    //     // Place pre‑placed puppies from JSON
    //     foreach (Vector2Int pos in model.GetSafePrePlaced())
    //     {
    //         Cell cell = gridManager.GetCell(pos.x, pos.y);
    //         if (cell != null)
    //         {
    //             PuzzleObject pup = cell.PlacePuppy(puppyPrefab);
    //             if (pup != null)
    //             {
    //                 cell.isGiven = true;
    //                 placedPuppies.Add(pos);
    //             }
    //         }
    //     }
    // }
    public void LoadLevelFromJson(LevelDataModel model)
    {
        StartCoroutine(LoadLevelWithAnimation(model));
    }

    /// <summary>
    /// Builds the grid from JSON, plays the staggered intro animation, then
    /// pops in any pre-placed puppies and refreshes the HUD.
    /// </summary>
    private IEnumerator LoadLevelWithAnimation(LevelDataModel model)
    {
        int size = model.GetSafeGridSize();
        int[,] map = model.GetSafeColorData();   // now contains ColorID values directly
        zoneMap = map;

        // Count unique colour IDs in the grid
        HashSet<int> uniqueColors = new HashSet<int>();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                uniqueColors.Add(map[x, y]);

        totalColorCount = model.GetSafeWinCondition();

        // Each zone ID already equals its colour group index
        int maxZone = 0;
        foreach (int id in uniqueColors) if (id > maxZone) maxZone = id;
        zoneToColorIndex = new int[maxZone + 1];
        for (int i = 0; i <= maxZone; i++) zoneToColorIndex[i] = i;

        // Build visual colours array
        GridConfig config = Resources.Load<GridConfig>("GridConfig");
        if (config == null) Debug.LogError("GridConfig missing!");

        Color[] zoneColors = new Color[maxZone + 1];
        for (int i = 0; i <= maxZone; i++)
        {
            zoneColors[i] = (config != null) ? config.GetColor((ColorID)i) : Color.white;
        }

        // Generate grid (cells will be scaled to zero by the animator before showing)
        gridManager.GenerateGrid(size, zoneMap, zoneColors);

        // Reset state
        placedPuppies.Clear();
        lives = MaxLives;
        gameOver = false;
        LevelComplete = false;
        LevelFailed = false;

        // Play the staggered diagonal intro
        if (gridAnimator != null)
        {
            List<Cell> ordered = gridManager.GetCellsInDiagonalOrder();
            bool animationDone = false;
            yield return StartCoroutine(gridAnimator.AnimateGrid(ordered, () => animationDone = true));
            while (!animationDone) yield return null;
        }

        // Place pre‑placed puppies after the grid is fully visible.
        // Cell.PlacePuppy plays its own pop-in animation, so no extra call needed here.
        foreach (Vector2Int pos in model.GetSafePrePlaced())
        {
            Cell cell = gridManager.GetCell(pos.x, pos.y);
            if (cell == null) continue;

            PuzzleObject pup = cell.PlacePuppy(puppyPrefab);
            if (pup == null) continue;

            cell.isGiven = true;
            placedPuppies.Add(pos);
        }

        if (hudManager != null)
        {
            hudManager.UpdateHearts(lives);
            hudManager.UpdateProgress(placedPuppies.Count, totalColorCount);
        }

        Debug.Log($"Level loaded: {size}x{size}, unique colours: {totalColorCount}, pre-placed: {placedPuppies.Count}");
    }

    // Called when a cell is double-tapped
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

        // Use colour-based rule check
        if (GameRules.IsPlacementValid(pos, placedPuppies, zoneMap, zoneToColorIndex))
        {
            // Valid placement
            PuzzleObject pup = cell.PlacePuppy(puppyPrefab);
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
            // Invalid placement → permanent red cross (cell drives its own
            // heart-break effect internally) + lose a life on the HUD.
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
        LevelComplete = true;
        Debug.Log("🎉 Level Complete!");
        if (PopupManager.Instance != null)
            PopupManager.Instance.ShowWin();
    }

    private void Lose()
    {
        gameOver = true;
        LevelFailed = true;
        Debug.Log("💀 Game Over - out of lives.");
        if (PopupManager.Instance != null)
            PopupManager.Instance.ShowLose();
    }

    // // Helper to convert hex to Color
    // private Color HexToColor(string hex)
    // {
    //     ColorUtility.TryParseHtmlString(hex, out Color col);
    //     return col;
    // }
}
