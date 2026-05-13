using System.Collections;
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

    public LevelDataModel CurrentLevelData { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;
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

        // Start loading Level 1 as soon as the Loading scene appears
        StartCoroutine(LoadLevelRoutine(1));
    }

    private IEnumerator LoadLevelRoutine(int levelIndex)
    {
        if (progressSlider != null) progressSlider.value = 0f;
        if (progressText != null) progressText.text = "0%";

        // --- Simulate initial work ---
        for (float i = 0f; i <= 0.3f; i += Time.deltaTime)
        {
            if (progressSlider != null) progressSlider.value = i / 0.3f;
            if (progressText != null) progressText.text = Mathf.RoundToInt((i / 0.3f) * 100) + "%";
            yield return null;
        }

        // --- Actual JSON loading (fast, but we'll pad it) ---
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

        Debug.Log("Level loaded successfully!");
        // After loading, switch to the Home scene where the player can press Play
        SceneManager.LoadScene(homeSceneName);
    }

    // Called from Home scene Play button
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
