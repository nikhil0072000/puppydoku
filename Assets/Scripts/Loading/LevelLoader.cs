using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class LevelLoader : MonoBehaviour
{
    private const string TutorialCompleteKey = "TutorialCompleted";
    // Stores the FileKey of the player's current Normal level (stable across content
    // updates that add/remove/reorder level files — unlike a raw list index).
    private const string SavedLevelKey = "SavedLevelKey";
    // Shipped alongside the level JSONs so the runtime can enumerate them on platforms
    // (Android, WebGL) where StreamingAssets isn't a readable filesystem directory.
    private const string ManifestFileName = "levels_manifest.json";

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

        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        yield return StartCoroutine(ScanAvailableLevelsRoutine());

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
            yield return StartCoroutine(LoadLevelRoutine(tutorialLevel, gameSceneName));
            yield break;
        }

        SetSavedLevelPosition();
        yield return StartCoroutine(LoadLevelRoutine(availableLevels[currentListPosition], homeSceneName));
    }

    private IEnumerator ScanAvailableLevelsRoutine()
    {
        availableLevels.Clear();

        string folder = Path.Combine(Application.streamingAssetsPath, levelsFolder);

        // On desktop/iOS/Editor, StreamingAssets is a real directory we can enumerate.
        // On Android/WebGL it's packed inside the build, so we fall back to a shipped manifest.
        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*.json");
            foreach (string filePath in files)
            {
                string name = Path.GetFileNameWithoutExtension(filePath);
                if (name == Path.GetFileNameWithoutExtension(ManifestFileName))
                    continue;
                AddLevelFromName(name, filePath);
            }

#if UNITY_EDITOR
            WriteManifest(folder);
#endif
        }
        else
        {
            string manifestPath = Path.Combine(folder, ManifestFileName);
            string manifestJson = null;
            yield return StartCoroutine(ReadTextRoutine(manifestPath, text => manifestJson = text));

            if (string.IsNullOrEmpty(manifestJson))
            {
                Debug.LogWarning($"Levels manifest not found or unreadable at: {manifestPath}");
            }
            else
            {
                string[] names = null;
                try
                {
                    names = JsonConvert.DeserializeObject<string[]>(manifestJson);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse levels manifest: {e.Message}");
                }

                if (names != null)
                {
                    foreach (string name in names)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            continue;
                        string filePath = Path.Combine(folder, name + ".json");
                        AddLevelFromName(name, filePath);
                    }
                }
            }
        }

        availableLevels.Sort(CompareLevels);
        Debug.Log($"Found {availableLevels.Count} level(s): {string.Join(", ", availableLevels.Select(level => level.GetButtonLabel()))}");
    }

    private void AddLevelFromName(string name, string filePath)
    {
        // Level type is derived from the filename prefix: Level_* (Normal), Daily_* (DailyChallenge), Tutorial_* (Tutorial).
        if (!LevelDataModel.TryParseFileName(name, out LevelType type, out string keyPart))
            return;

        availableLevels.Add(new LevelInfo
        {
            FileName = name,
            FilePath = filePath,
            FileKey = keyPart,
            Type = type
        });
    }

#if UNITY_EDITOR
    private void WriteManifest(string folder)
    {
        try
        {
            string[] names = availableLevels.Select(level => level.FileName).ToArray();
            string json = JsonConvert.SerializeObject(names, Formatting.Indented);
            File.WriteAllText(Path.Combine(folder, ManifestFileName), json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not write levels manifest: {e.Message}");
        }
    }
#endif

    /// <summary>
    /// Reads a text file from a path that may be a plain filesystem path (desktop/iOS/Editor)
    /// or a packed StreamingAssets URL (Android/WebGL). Calls back with null on any failure.
    /// </summary>
    private IEnumerator ReadTextRoutine(string path, Action<string> onResult)
    {
        if (string.IsNullOrEmpty(path))
        {
            onResult(null);
            yield break;
        }

        // A URL-style path (jar:file://… on Android, http(s):// on WebGL) must go through UnityWebRequest.
        bool isUrl = path.Contains("://");
        if (!isUrl)
        {
            string text = null;
            try
            {
                if (File.Exists(path))
                    text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read level file '{path}': {e.Message}");
            }
            onResult(text);
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            onResult(request.downloadHandler.text);
        else
        {
            Debug.LogError($"Failed to fetch level file '{path}': {request.error}");
            onResult(null);
        }
    }

    private static int CompareLevels(LevelInfo a, LevelInfo b)
    {
        // Order by a stable type rank first, then by numeric key, then by name. This keeps the
        // comparator a strict weak ordering (returning 0 only for genuinely equal entries).
        int rankCompare = TypeRank(a.Type).CompareTo(TypeRank(b.Type));
        if (rankCompare != 0)
            return rankCompare;

        if (int.TryParse(a.FileKey, out int aKey) && int.TryParse(b.FileKey, out int bKey))
        {
            int keyCompare = aKey.CompareTo(bKey);
            if (keyCompare != 0)
                return keyCompare;
        }

        return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static int TypeRank(LevelType type)
    {
        switch (type)
        {
            case LevelType.Tutorial: return 0;
            case LevelType.Normal: return 1;
            case LevelType.DailyChallenge: return 2;
            case LevelType.Event: return 3;
            default: return 4;
        }
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

        bool loaded = false;
        if (!string.IsNullOrEmpty(levelInfo.FilePath))
        {
            string json = null;
            yield return StartCoroutine(ReadTextRoutine(levelInfo.FilePath, text => json = text));

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    CurrentLevelData = JsonConvert.DeserializeObject<LevelDataModel>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse level '{levelInfo.FileName}': {e.Message}. Using fallback.");
                    CurrentLevelData = null;
                }

                if (CurrentLevelData != null)
                {
                    CurrentDifficulty = ParseDifficulty(CurrentLevelData.difficulty);
                    loaded = true;
                }
            }
        }

        if (!loaded)
        {
            Debug.LogError($"Level '{levelInfo.FileName}' could not be loaded ({levelInfo.FilePath}). Using fallback.");
            CurrentLevelData = CreateFallbackLevel(4);
            CurrentDifficulty = Difficulty.Easy;
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
        // Progress is keyed by the level's stable FileKey, not its position in the scanned
        // list, so adding/removing/reordering level files won't shift a returning player.
        string savedKey = PlayerPrefs.GetString(SavedLevelKey, string.Empty);

        int index = -1;
        if (!string.IsNullOrEmpty(savedKey))
            index = availableLevels.FindIndex(level => level.Type == LevelType.Normal && level.FileKey == savedKey);

        if (index < 0)
            index = availableLevels.FindIndex(level => level.Type == LevelType.Normal);

        currentListPosition = index >= 0 ? index : 0;
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
        if (currentLevelInfo != null && currentLevelInfo.Type == LevelType.Normal)
        {
            PlayerPrefs.SetString(SavedLevelKey, currentLevelInfo.FileKey);
            PlayerPrefs.Save();
        }
        else if (currentListPosition >= 0 && currentListPosition < availableLevels.Count
                 && availableLevels[currentListPosition].Type == LevelType.Normal)
        {
            PlayerPrefs.SetString(SavedLevelKey, availableLevels[currentListPosition].FileKey);
            PlayerPrefs.Save();
        }
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
