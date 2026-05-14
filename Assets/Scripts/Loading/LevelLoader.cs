using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string gameSceneName = "Game";

    [Header("Loading UI (assign in Loading scene)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Levels")]
    [SerializeField] private string levelsFolder = "Levels";

    private readonly List<int> availableLevels = new();
    private int currentListPosition = -1;

    public LevelDataModel CurrentLevelData { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;
    public bool HasNextLevel => currentListPosition >= 0 && currentListPosition < availableLevels.Count - 1;

    private bool levelLoaded = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ScanAvailableLevels();

        if (availableLevels.Count == 0)
        {
            Debug.LogError("No level files found in StreamingAssets/Levels! Falling back to index 1.");
            availableLevels.Add(1);
        }

        // Initial load lands on Home so the player can press Play.
        StartCoroutine(LoadLevelRoutine(availableLevels[0], homeSceneName));
    }

    private void ScanAvailableLevels()
    {
        availableLevels.Clear();

        string folder = Path.Combine(Application.streamingAssetsPath, levelsFolder);
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning($"Levels folder not found: {folder}");
            return;
        }

        string[] files = Directory.GetFiles(folder, "Level_*.json");
        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileNameWithoutExtension(files[i]); // e.g., "Level_01"
            string numberPart = name["Level_".Length..];
            if (int.TryParse(numberPart, out int index))
                availableLevels.Add(index);
        }

        availableLevels.Sort();
        Debug.Log($"Found {availableLevels.Count} level(s): {string.Join(", ", availableLevels)}");
    }

    private IEnumerator LoadLevelRoutine(int levelIndex, string targetScene)
    {
        currentListPosition = availableLevels.IndexOf(levelIndex);
        if (currentListPosition < 0)
        {
            Debug.LogError($"Level index {levelIndex} not in available list; defaulting to first entry.");
            currentListPosition = 0;
            levelIndex = availableLevels[0];
        }

        if (progressSlider != null) progressSlider.value = 0f;
        if (progressText != null) progressText.text = "0%";

        // --- Simulate initial work (fills bar 0% -> 30%) ---
        for (float i = 0f; i <= 0.3f; i += Time.deltaTime)
        {
            if (progressSlider != null) progressSlider.value = i;
            if (progressText != null) progressText.text = Mathf.RoundToInt(i * 100) + "%";
            yield return null;
        }

        // --- Actual JSON loading ---
        string fileName = $"Level_{levelIndex:D2}";
        string filePath = Path.Combine(Application.streamingAssetsPath, levelsFolder, $"{fileName}.json");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Level file not found: {filePath}. Creating fallback.");
            CurrentLevelData = CreateFallbackLevel(4);
            CurrentDifficulty = Difficulty.Easy;
        }
        else
        {
            string json = File.ReadAllText(filePath);
            CurrentLevelData = JsonConvert.DeserializeObject<LevelDataModel>(json);

            if (CurrentLevelData == null)
            {
                Debug.LogError("Deserialization returned null, using fallback.");
                CurrentLevelData = CreateFallbackLevel(4);
                CurrentDifficulty = Difficulty.Easy;
            }
            else
            {
                CurrentDifficulty = ParseDifficulty(CurrentLevelData.difficulty);
            }
        }

        // --- Simulate finishing work ---
        for (float i = 0.3f; i <= 1f; i += Time.deltaTime * 1.5f)
        {
            if (progressSlider != null) progressSlider.value = i;
            if (progressText != null) progressText.text = Mathf.RoundToInt(i * 100) + "%";
            yield return null;
        }

        if (progressSlider != null) progressSlider.value = 1f;
        if (progressText != null) progressText.text = "100%";
        levelLoaded = true;

        Debug.Log($"Level {levelIndex} loaded. Switching to '{targetScene}'.");
        SceneManager.LoadScene(targetScene);
    }

    // Called from Home scene Play button — always starts from the first level.
    public void LoadFirstAvailableLevel()
    {
        if (availableLevels.Count == 0)
        {
            Debug.LogError("No levels available to load!");
            return;
        }
        StartCoroutine(LoadLevelRoutine(availableLevels[0], gameSceneName));
    }

    // Called from the win popup's Next Level button.
    public void LoadNextLevel()
    {
        if (!HasNextLevel)
        {
            Debug.Log("No next level available.");
            return;
        }

        int nextIndex = availableLevels[currentListPosition + 1];
        StartCoroutine(LoadLevelRoutine(nextIndex, gameSceneName));
    }

    // Legacy entry point — kept for any existing wiring that still calls it.
    public void GoToGameScene()
    {
        if (!levelLoaded)
        {
            Debug.LogError("Level not yet loaded!");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    private Difficulty ParseDifficulty(string diff)
    {
        if (string.IsNullOrEmpty(diff)) return Difficulty.Easy;
        switch (diff.Trim().ToLower())
        {
            case "easy": return Difficulty.Easy;
            case "medium": return Difficulty.Medium;
            case "hard": return Difficulty.Hard;
            default:
                Debug.LogWarning($"Unknown difficulty '{diff}', using Easy.");
                return Difficulty.Easy;
        }
    }

    private LevelDataModel CreateFallbackLevel(int size)
    {
        var fallback = new LevelDataModel
        {
            levelId = "Fallback",
            difficulty = "Easy",
            gridSize = size,
            colorData = new int[size][]
        };
        for (int i = 0; i < size; i++)
            fallback.colorData[i] = new int[size];
        return fallback;
    }
}
