using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class LevelLoader : MonoBehaviour
{
    private const string TutorialCompleteKey = "TutorialCompleted";
    private const string SavedLevelPositionKey = "SavedLevelPosition";

    public static LevelLoader Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string gameSceneName = "Game";

    [Header("Loading UI (assign in Loading scene)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Levels")]
    [SerializeField] private string levelsFolder = "Levels";

    private readonly List<LevelInfo> availableLevels = new();
    private int currentListPosition = -1;
    private LevelInfo currentLevelInfo;

    public LevelDataModel CurrentLevelData { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;
    public bool IsDailySession { get; private set; }
    public LevelType CurrentLevelType
    {
        get
        {
            if (IsDailySession)
                return LevelType.DailyChallenge;
            return currentLevelInfo != null ? currentLevelInfo.Type : LevelType.Normal;
        }
    }
    public bool HasNextLevel => currentListPosition >= 0 && currentListPosition < availableLevels.Count - 1;
    public bool HasTutorialLevel => availableLevels.Any(level => level.Type == LevelType.Tutorial);
    public bool IsTutorialComplete => PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1;
    public string CurrentLevelButtonLabel => currentLevelInfo != null ? currentLevelInfo.GetButtonLabel() : "Level 1";
    public string NextLevelButtonLabel => HasNextLevel ? availableLevels[currentListPosition + 1].GetButtonLabel() : "";

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
            Debug.LogError("No level files found in StreamingAssets/Levels! Creating fallback data.");
            availableLevels.Add(new LevelInfo
            {
                FileName = "Level_1",
                FilePath = string.Empty,
                FileKey = "1",
                Type = LevelType.Normal
            });
        }

        if (!IsTutorialComplete && TryGetTutorialLevel(out LevelInfo tutorialLevel))
        {
            StartCoroutine(LoadLevelRoutine(tutorialLevel, gameSceneName));
            return;
        }

        SetSavedLevelPosition();
        StartCoroutine(LoadLevelRoutine(availableLevels[currentListPosition], homeSceneName));
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

        // Level type is derived from the filename prefix: Level_* (Normal), Daily_* (DailyChallenge), Tutorial_* (Tutorial).
        string[] files = Directory.GetFiles(folder, "*.json");
        foreach (string filePath in files)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            if (!LevelDataModel.TryParseFileName(name, out LevelType type, out string keyPart))
                continue;

            availableLevels.Add(new LevelInfo
            {
                FileName = name,
                FilePath = filePath,
                FileKey = keyPart,
                Type = type
            });
        }

        availableLevels.Sort(CompareLevels);
        Debug.Log($"Found {availableLevels.Count} level(s): {string.Join(", ", availableLevels.Select(level => level.GetButtonLabel()))}");

    }

    private static int CompareLevels(LevelInfo a, LevelInfo b)
    {
        if (a.Type != b.Type)
            return a.Type == LevelType.Tutorial ? -1 : b.Type == LevelType.Tutorial ? 1 : 0;

        if (int.TryParse(a.FileKey, out int aKey) && int.TryParse(b.FileKey, out int bKey))
            return aKey.CompareTo(bKey);

        return string.Compare(a.FileName, b.FileName, System.StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerator LoadLevelRoutine(LevelInfo levelInfo, string targetScene)
    {
        currentListPosition = availableLevels.IndexOf(levelInfo);
        if (currentListPosition < 0)
        {
            Debug.LogError($"Level '{levelInfo.FileName}' not in available list; defaulting to first entry.");
            currentListPosition = 0;
            levelInfo = availableLevels[0];
        }

        currentLevelInfo = levelInfo;

        if (progressSlider != null) progressSlider.value = 0f;
        if (progressText != null) progressText.text = "0%";

        for (float i = 0f; i <= 0.3f; i += Time.deltaTime)
        {
            if (progressSlider != null) progressSlider.value = i;
            if (progressText != null) progressText.text = Mathf.RoundToInt(i * 100) + "%";
            yield return null;
        }

        if (string.IsNullOrEmpty(levelInfo.FilePath) || !File.Exists(levelInfo.FilePath))
        {
            Debug.LogError($"Level file not found: {levelInfo.FilePath}. Creating fallback.");
            CurrentLevelData = CreateFallbackLevel(4);
            CurrentDifficulty = Difficulty.Easy;
        }
        else
        {
            string json = File.ReadAllText(levelInfo.FilePath);
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

        if (levelInfo.Type == LevelType.Normal && !IsDailySession)
            SaveCurrentLevelPosition();

        for (float i = 0.3f; i <= 1f; i += Time.deltaTime * 1.5f)
        {
            if (progressSlider != null) progressSlider.value = i;
            if (progressText != null) progressText.text = Mathf.RoundToInt(i * 100) + "%";
            yield return null;
        }

        if (progressSlider != null) progressSlider.value = 1f;
        if (progressText != null) progressText.text = "100%";
        levelLoaded = true;

        Debug.Log($"Level '{levelInfo.GetButtonLabel()}' loaded. Switching to '{targetScene}'.");
        SceneManager.LoadScene(targetScene);
    }

    public void LoadCurrentLevel()
    {
        if (availableLevels.Count == 0)
        {
            Debug.LogError("No levels available to load!");
            return;
        }

        if (currentListPosition < 0 || currentListPosition >= availableLevels.Count)
            SetSavedLevelPosition();

        EndDailySession();
        StartCoroutine(LoadLevelRoutine(availableLevels[currentListPosition], gameSceneName));
    }

    public void LoadFirstAvailableLevel()
    {
        EndDailySession();
        LoadCurrentLevel();
    }

    public void LoadFirstNormalLevel()
    {
        LevelInfo firstNormal = availableLevels.FirstOrDefault(level => level.Type != LevelType.Tutorial);
        if (firstNormal == null)
        {
            Debug.LogError("No normal levels available to start the game.");
            return;
        }

        EndDailySession();
        currentListPosition = availableLevels.IndexOf(firstNormal);
        SaveCurrentLevelPosition();
        StartCoroutine(LoadLevelRoutine(firstNormal, gameSceneName));
    }

    public void LoadNextLevel()
    {
        if (!HasNextLevel)
        {
            Debug.Log("No next level available.");
            return;
        }

        EndDailySession();
        LevelInfo nextLevel = availableLevels[currentListPosition + 1];
        StartCoroutine(LoadLevelRoutine(nextLevel, gameSceneName));
    }

    public void LoadNextNormalLevel()
    {
        if (availableLevels.Count == 0)
        {
            Debug.LogError("No levels available to load.");
            return;
        }

        int startIndex = currentListPosition + 1;
        LevelInfo nextNormal = null;

        for (int i = startIndex; i < availableLevels.Count; i++)
        {
            if (availableLevels[i].Type == LevelType.Normal)
            {
                nextNormal = availableLevels[i];
                break;
            }
        }

        if (nextNormal == null)
            nextNormal = availableLevels.FirstOrDefault(level => level.Type == LevelType.Normal);

        if (nextNormal == null)
        {
            Debug.LogError("No normal levels available to continue to.");
            return;
        }

        EndDailySession();
        currentListPosition = availableLevels.IndexOf(nextNormal);
        SaveCurrentLevelPosition();
        StartCoroutine(LoadLevelRoutine(nextNormal, gameSceneName));
    }

    public void LoadDailyChallenge()
    {
        if (DailyChallengeManager.Instance == null)
        {
            Debug.LogError("DailyChallengeManager is not present in the scene.");
            return;
        }

        DailyChallengeManager.Instance.EnsureDailyState();
        string selectedKey = DailyChallengeManager.Instance.SelectedLevelKey;
        LevelInfo dailyLevel = null;

        if (!string.IsNullOrEmpty(selectedKey))
            dailyLevel = availableLevels.FirstOrDefault(level => level.FileKey == selectedKey && level.Type == LevelType.DailyChallenge);

        if (dailyLevel == null)
            dailyLevel = ChooseDailyLevelForDate(DailyChallengeManager.Instance.ChallengeDate);

        if (dailyLevel == null)
        {
            Debug.LogError("No daily challenge level available to load.");
            return;
        }

        DailyChallengeManager.Instance.SetSelectedDailyLevelKey(dailyLevel.FileKey);
        IsDailySession = true;
        StartCoroutine(LoadLevelRoutine(dailyLevel, gameSceneName));
    }

    private LevelInfo ChooseDailyLevelForDate(DateTime date)
    {
        List<LevelInfo> dailyLevels = availableLevels.Where(level => level.Type == LevelType.DailyChallenge).ToList();
        if (dailyLevels.Count == 0)
            dailyLevels = availableLevels.Where(level => level.Type == LevelType.Normal).ToList();

        if (dailyLevels.Count == 0)
            return null;

        int seed = date.Year * 1000 + date.DayOfYear;
        int index = Mathf.Abs(seed) % dailyLevels.Count;
        return dailyLevels[index];
    }

    public void MarkTutorialCompleted()
    {
        if (IsTutorialComplete) return;

        PlayerPrefs.SetInt(TutorialCompleteKey, 1);
        int normalIndex = availableLevels.FindIndex(level => level.Type != LevelType.Tutorial);
        currentListPosition = normalIndex >= 0 ? normalIndex : 0;
        SaveCurrentLevelPosition();
        PlayerPrefs.Save();
    }

    private void SetSavedLevelPosition()
    {
        int savedPosition = PlayerPrefs.GetInt(SavedLevelPositionKey, 0);
        if (savedPosition < 0 || savedPosition >= availableLevels.Count)
            savedPosition = 0;

        // The home progression only tracks Normal levels — never snap to a Tutorial/Daily/Event entry.
        if (savedPosition < 0 || savedPosition >= availableLevels.Count || availableLevels[savedPosition].Type != LevelType.Normal)
        {
            int normalIndex = availableLevels.FindIndex(level => level.Type == LevelType.Normal);
            savedPosition = normalIndex >= 0 ? normalIndex : Mathf.Clamp(savedPosition, 0, availableLevels.Count - 1);
        }

        currentListPosition = savedPosition;
    }

    private void EndDailySession()
    {
        if (!IsDailySession)
            return;

        IsDailySession = false;
        DailyChallengeManager.Instance?.StopDailyTimer();
    }

    private void SaveCurrentLevelPosition()
    {
        PlayerPrefs.SetInt(SavedLevelPositionKey, currentListPosition);
        PlayerPrefs.Save();
    }

    private bool TryGetTutorialLevel(out LevelInfo tutorialLevel)
    {
        tutorialLevel = availableLevels.FirstOrDefault(level => level.Type == LevelType.Tutorial);
        return tutorialLevel != null;
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

    private sealed class LevelInfo
    {
        public string FileName;
        public string FilePath;
        public string FileKey;
        public LevelType Type = LevelType.Normal;

        public string GetButtonLabel()
        {
            if (Type == LevelType.Tutorial)
                return "Tutorial";

            // Predefined label from the level type + number, e.g. "Level 1" / "Daily 3".
            string word = Type == LevelType.DailyChallenge ? "Daily" : "Level";

            if (int.TryParse(FileKey, out int parsed))
                return $"{word} {parsed}";

            if (!string.IsNullOrWhiteSpace(FileKey))
                return $"{word} {FileKey}";

            return word;
        }
    }
}
