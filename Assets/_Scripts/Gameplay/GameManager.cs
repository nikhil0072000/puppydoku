using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PuppyPuzzle.PowerUps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool LevelComplete = false;
    public static bool LevelFailed = false;

    /// <summary>
    /// While true, grid input is suppressed (e.g. the Bulb hint overlay is open).
    /// Checked by <see cref="InputManager"/>. Reset on level (re)load.
    /// </summary>
    public static bool InputLocked = false;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    /// <summary>Cached puppy prefab. Can be overridden by the active theme.</summary>
    [SerializeField] private GameObject puppyPrefab;
    private GameObject ResolvedPuppyPrefab => ThemeManager.Current?.puppyPrefab != null
        ? ThemeManager.Current.puppyPrefab
        : puppyPrefab;
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
    private Vector2Int[] hiddenSolution;      // Optional authored solution loaded from level JSON
    private HashSet<Vector2Int> hiddenSolutionSet;

    private HashSet<Vector2Int> placedPuppies = new HashSet<Vector2Int>();
    private int lives;
    private bool gameOver = false;
    private bool revivedThisLevel = false;
    private bool isDailyMode = false;

    // Most recently placed puppy — used by the Bulb hint as its focus cell.
    private Vector2Int? lastPlacedCell;

    // Every puppy in placement order: givens first (initial), then player/reveal
    // placements appended. The Bulb hint walks this in reverse (recent → initial).
    private readonly List<Vector2Int> placementOrder = new List<Vector2Int>();

    private const int MaxLives = 3;

    // ---- Read-only state exposed to the power-up system ----
    public bool IsGameOver => gameOver;
    public bool CanRevive => !revivedThisLevel;

    /// <summary>Current remaining lives. Read-only for UI (LifePanel/HUD).</summary>
    public int CurrentLives => lives;

    /// <summary>Maximum lives a level starts with.</summary>
    public int MaxLifeCount => MaxLives;
    public GridManager Grid => gridManager;
    public int GridWidth => zoneMap != null ? zoneMap.GetLength(0) : 0;
    public int GridHeight => zoneMap != null ? zoneMap.GetLength(1) : 0;

    /// <summary>Placed puppies in placement order (givens first, newest last).</summary>
    public IReadOnlyList<Vector2Int> PlacementOrder => placementOrder;
    public bool HasHiddenSolution => hiddenSolutionSet != null && hiddenSolutionSet.Count > 0;
    public bool IsHiddenSolutionCell(Vector2Int pos) => hiddenSolutionSet != null && hiddenSolutionSet.Contains(pos);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

    void Update()
    {
        if (isDailyMode && DailyChallengeManager.Instance != null && hudManager != null)
        {
            hudManager.UpdateDailyTimer(DailyChallengeManager.Instance.ElapsedSeconds);
        }
    }

    void OnDestroy()
    {
        Cell.OnCellDoubleTapped -= OnCellDoubleTapped;
        if (Instance == this)
            Instance = null;
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
    //             PuzzleObject pup = cell.PlacePuppy(ResolvedPuppyPrefab);
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
        hiddenSolution = model.GetSafeSolution();
        hiddenSolutionSet = (hiddenSolution != null) ? new HashSet<Vector2Int>(hiddenSolution) : null;

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
        placementOrder.Clear();
        lastPlacedCell = null;
        lives = MaxLives;
        gameOver = false;
        revivedThisLevel = false;
        LevelComplete = false;
        LevelFailed = false;
        InputLocked = false;

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

            PuzzleObject pup = cell.PlacePuppy(ResolvedPuppyPrefab);
            if (pup == null) continue;

            cell.isGiven = true;
            placedPuppies.Add(pos);
            placementOrder.Add(pos);
        }

        if (hudManager != null)
        {
            hudManager.UpdateHearts(lives);
            hudManager.UpdateProgress(placedPuppies.Count, totalColorCount);
        }

        isDailyMode = LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.DailyChallenge;

        if (hudManager != null)
        {
            string label = "Level";
            if (isDailyMode)
            {
                label = DailyChallengeManager.Instance != null ? DailyChallengeManager.Instance.GetChallengeDateText() : "Daily";
            }
            else if (LevelLoader.Instance != null)
            {
                label = LevelLoader.Instance.CurrentLevelButtonLabel;
            }

            hudManager.SetLevelLabel(label);
        }

        if (isDailyMode)
        {
            DailyChallengeManager.Instance?.EnsureDailyState();
            DailyChallengeManager.Instance?.StartDailyTimer();
            if (hudManager != null)
                hudManager.SetDailyTimerVisible(true);
        }
        else
        {
            if (hudManager != null)
                hudManager.SetDailyTimerVisible(false);
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

        // Guard against taps arriving before the level finished loading.
        if (zoneMap == null) return;

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

        if (HasHiddenSolution && !IsHiddenSolutionCell(pos))
        {
            cell.ShowPermanentRedCross();
            PuppyRegistry.PlaySadOnAll();
            lives--;
            Debug.Log($"Invalid placement at {pos} (not part of hidden solution). Lives left: {lives}");

            if (hudManager != null)
                hudManager.UpdateHearts(lives);

            if (lives <= 0)
                Lose();
            return;
        }

        // Use colour-based rule check
        if (GameRules.IsPlacementValid(pos, placedPuppies, zoneMap, zoneToColorIndex))
        {
            PlacePuppyAt(pos);
        }
        else
        {
            // Invalid placement → permanent red cross (cell drives its own
            // heart-break effect internally) + lose a life on the HUD.
            cell.ShowPermanentRedCross();
            PuppyRegistry.PlaySadOnAll();
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

    /// <summary>
    /// Places a puppy at <paramref name="pos"/> without re-checking rules (the caller
    /// must have validated). Handles the pop-in + wink, placement bookkeeping, HUD
    /// refresh and win check. Shared by manual placement and the Puppy power-up.
    /// </summary>
    private bool PlacePuppyAt(Vector2Int pos)
    {
        Cell cell = gridManager.GetCell(pos.x, pos.y);
        if (cell == null) return false;

        PuzzleObject pup = cell.PlacePuppy(ResolvedPuppyPrefab);
        if (pup == null) return false;

        placedPuppies.Add(pos);
        placementOrder.Add(pos);
        lastPlacedCell = pos;
        pup.PlayWink();
        Debug.Log($"Puppy placed at {pos}");

        if (hudManager != null)
            hudManager.UpdateProgress(placedPuppies.Count, totalColorCount);

        if (placedPuppies.Count == totalColorCount)
            Win();

        return true;
    }

    /// <summary>
    /// Puppy power-up: solves the board from the current state and auto-places one
    /// guaranteed-correct puppy. Returns false (place nothing) if the game is over,
    /// the board can't be completed from here, or every solution cell is already filled.
    /// </summary>
    public bool RevealCorrectPuppy()
    {
        if (gameOver) return false;

        if (!TryGetNextSolutionCell(out Vector2Int pos))
        {
            Debug.LogWarning("RevealCorrectPuppy: no empty solution cell available from the current board state.");
            return false;
        }

        return PlacePuppyAt(pos);
    }

    /// <summary>
    /// Places a puppy at <paramref name="pos"/> only if it's empty and a legal move.
    /// Used by the Bulb hint's deduction step (Apply on the highlighted cell). Returns
    /// false if the game is over, the cell is taken, or the move would break the rules.
    /// </summary>
    public bool PlacePuppyAtIfValid(Vector2Int pos)
    {
        if (gameOver) return false;
        if (zoneMap == null) return false;

        Cell cell = gridManager.GetCell(pos.x, pos.y);
        if (cell == null || cell.GetPuppy() != null) return false;

        if (HasHiddenSolution && !IsHiddenSolutionCell(pos))
            return false;

        if (!GameRules.IsPlacementValid(pos, placedPuppies, zoneMap, zoneToColorIndex))
            return false;

        return PlacePuppyAt(pos);
    }

    /// <summary>
    /// Solves the board from the current state and returns the first solution cell
    /// that is still empty. Used by the Puppy power-up (to place it) and by the Bulb
    /// hint's final "a puppy goes here" step (to highlight it). False if no completion
    /// exists or every solution cell is already filled.
    /// </summary>
    public bool TryGetNextSolutionCell(out Vector2Int cell)
    {
        cell = default;

        if (HasHiddenSolution)
        {
            bool allPlacedAreValid = true;
            foreach (Vector2Int pos in placedPuppies)
            {
                if (!IsHiddenSolutionCell(pos))
                {
                    allPlacedAreValid = false;
                    break;
                }
            }

            if (allPlacedAreValid)
            {
                foreach (Vector2Int pos in hiddenSolution)
                {
                    if (placedPuppies.Contains(pos)) continue;

                    Cell c = gridManager.GetCell(pos.x, pos.y);
                    if (c == null || c.GetPuppy() != null) continue;

                    cell = pos;
                    return true;
                }
            }
        }

        if (!PuzzleSolver.TrySolve(zoneMap, zoneToColorIndex, totalColorCount, placedPuppies, out var solution))
            return false;

        foreach (Vector2Int pos in solution)
        {
            if (placedPuppies.Contains(pos)) continue;

            Cell c = gridManager.GetCell(pos.x, pos.y);
            if (c == null || c.GetPuppy() != null) continue;

            cell = pos;
            return true;
        }

        return false;
    }

    /// <summary>
    /// The cell the Bulb hint should focus on: the most recently placed puppy, or
    /// any pre-placed/given puppy as a fallback on a fresh level. Null if the board
    /// has no puppies at all.
    /// </summary>
    public Vector2Int? GetHintFocusCell()
    {
        if (lastPlacedCell.HasValue) return lastPlacedCell;

        foreach (Vector2Int pos in placedPuppies)
            return pos; // any puppy will do as a fallback

        return null;
    }

    private void Win()
    {
        gameOver = true;
        LevelComplete = true;
        Debug.Log("🎉 Level Complete!");

        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.Tutorial)
        {
            LevelLoader.Instance.MarkTutorialCompleted();
            if (PopupManager.Instance != null)
                PopupManager.Instance.ShowTutorialComplete();
            return;
        }

        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.DailyChallenge)
        {
            float elapsed = DailyChallengeManager.Instance != null ? DailyChallengeManager.Instance.ElapsedSeconds : 0f;
            DailyChallengeManager.Instance?.StopDailyTimer();
            DailyChallengeManager.Instance?.RecordDailyCompletion(elapsed);
            if (PopupManager.Instance != null)
                PopupManager.Instance.ShowDailyResult(elapsed, DailyChallengeManager.Instance.CompletedPercentile);
            return;
        }

        if (PopupManager.Instance != null)
            PopupManager.Instance.ShowWin();
    }

    public void ReviveAfterAd()
    {
        if (!gameOver || revivedThisLevel)
            return;

        revivedThisLevel = true;
        gameOver = false;
        LevelFailed = false;
        lives = 1;
        InputLocked = false;

        if (hudManager != null)
            hudManager.UpdateHearts(lives);

        Debug.Log("✨ Revive granted: one life restored. Resume play.");
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
