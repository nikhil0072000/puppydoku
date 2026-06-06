using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// Loads level content from a <see cref="LevelConfigSO"/> (two JSON TextAssets:
/// Levels + DailyChallengeLevels) and drives the Loading-scene progress flow.
/// Replaces the old StreamingAssets/manifest file scanning — all content ships
/// inside the build as TextAssets, parsed once at boot.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    private const string TutorialCompleteKey = "TutorialCompleted";
    // Stores the FileKey of the player's current Normal level (stable across content
    // updates that add/remove/reorder levels — unlike a raw list index).
    private const string SavedLevelKey = "SavedLevelKey";

    public static LevelLoader Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string gameSceneName = "Game";

    [Header("Loading UI (assign in Loading scene)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [Tooltip("Displays the build version (Project Settings > Player > Version) as \"Ver.<version>\".")]
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("Levels")]
    [Tooltip("ScriptableObject holding the Levels and DailyChallengeLevels JSON TextAssets.")]
    [SerializeField] private LevelConfigSO levelConfig;

    private readonly List<LevelInfo> availableLevels = new();
    private readonly List<DailyChallengeLevelData> dailyLevels = new();
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

    /// <summary>How many daily challenge levels exist in the config.</summary>
    public int DailyLevelCount => dailyLevels.Count;

    private bool levelLoaded = false;
    private int lastShownPercent = -1;

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

        if (versionText != null)
            versionText.text = $"Ver.{Application.version}";

        ParseLevelConfig();
        StartCoroutine(InitializeRoutine());
    }

    // ---- Content parsing (from LevelConfigSO) ----

    private void ParseLevelConfig()
    {
        availableLevels.Clear();
        dailyLevels.Clear();

        if (levelConfig == null)
        {
            Debug.LogError("LevelLoader has no LevelConfigSO assigned!");
            return;
        }

        // Tutorial / Normal levels.
        LevelCollection collection = ParseJson<LevelCollection>(levelConfig.Levels, "Levels");
        if (collection?.levels != null)
        {
            foreach (LevelDataModel data in collection.levels)
            {
                if (data == null)
                    continue;
                availableLevels.Add(LevelInfo.FromData(data));
            }
        }

        // Daily challenge levels (kept separate — they never enter the Normal progression).
        DailyChallengeLevelCollection dailyCollection =
            ParseJson<DailyChallengeLevelCollection>(levelConfig.DailyChallengeLevels, "DailyChallengeLevels");
        if (dailyCollection?.levels != null)
        {
            dailyLevels.AddRange(dailyCollection.levels.Where(level => level != null));
            dailyLevels.Sort((a, b) => a.dailyChallengeLevelNumber.CompareTo(b.dailyChallengeLevelNumber));
        }

        availableLevels.Sort(CompareLevels);
        Debug.Log($"LevelConfig parsed: {availableLevels.Count} level(s), {dailyLevels.Count} daily level(s).");
    }

    private static T ParseJson<T>(TextAsset asset, string label) where T : class
    {
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
        {
            Debug.LogError($"LevelConfig: '{label}' TextAsset is missing or empty.");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(asset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"LevelConfig: failed to parse '{label}': {e.Message}");
            return null;
        }
    }

    private IEnumerator InitializeRoutine()
    {
        if (availableLevels.Count == 0)
        {
            Debug.LogError("No levels found in LevelConfig! Creating fallback data.");
            availableLevels.Add(LevelInfo.FromData(CreateFallbackLevel(4)));
        }

        if (!IsTutorialComplete && TryGetTutorialLevel(out LevelInfo tutorialLevel))
        {
            yield return StartCoroutine(LoadLevelRoutine(tutorialLevel, gameSceneName));
            yield break;
        }

        SetSavedLevelPosition();
        yield return StartCoroutine(LoadLevelRoutine(availableLevels[currentListPosition], homeSceneName));
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
        int index = availableLevels.IndexOf(levelInfo);
        if (index >= 0)
        {
            currentListPosition = index;
        }
        else if (levelInfo.Type != LevelType.DailyChallenge)
        {
            // Non-daily levels must come from the list; daily levels are transient.
            Debug.LogError($"Level '{levelInfo.FileName}' not in available list; defaulting to first entry.");
            currentListPosition = 0;
            levelInfo = availableLevels[0];
        }

        currentLevelInfo = levelInfo;

        SetLoadingProgress(0f);

        for (float i = 0f; i <= 0.3f; i += Time.deltaTime)
        {
            SetLoadingProgress(i);
            yield return null;
        }

        if (levelInfo.Data != null)
        {
            CurrentLevelData = levelInfo.Data;
            CurrentDifficulty = ParseDifficulty(CurrentLevelData.difficulty);
        }
        else
        {
            Debug.LogError($"Level '{levelInfo.FileName}' has no data. Using fallback.");
            CurrentLevelData = CreateFallbackLevel(4);
            CurrentDifficulty = Difficulty.Easy;
        }

        if (levelInfo.Type == LevelType.Normal && !IsDailySession)
            SaveCurrentLevelPosition();

        for (float i = 0.3f; i <= 1f; i += Time.deltaTime * 1.5f)
        {
            SetLoadingProgress(i);
            yield return null;
        }

        SetLoadingProgress(1f);
        levelLoaded = true;

        Debug.Log($"Level '{levelInfo.GetButtonLabel()}' loaded. Switching to '{targetScene}'.");
        SceneManager.LoadScene(targetScene);
    }

    private void SetLoadingProgress(float normalized)
    {
        if (progressSlider != null) progressSlider.value = normalized;

        if (progressText == null) return;

        // Only build/assign the string when the displayed integer percent actually
        // changes, so the per-frame loading loops don't allocate every frame.
        int percent = Mathf.RoundToInt(normalized * 100);
        if (percent == lastShownPercent) return;

        lastShownPercent = percent;
        progressText.text = $"Loading...{percent}%";
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

    // ---- Daily challenge ----

    /// <summary>
    /// The daily level for a given 1-based sequence number. Exact match first;
    /// numbers beyond the last authored level wrap around so the rotation never runs dry.
    /// </summary>
    public DailyChallengeLevelData GetDailyLevel(int levelNumber)
    {
        if (dailyLevels.Count == 0)
            return null;

        DailyChallengeLevelData exact = dailyLevels.FirstOrDefault(level => level.dailyChallengeLevelNumber == levelNumber);
        if (exact != null)
            return exact;

        int index = Mathf.Abs(levelNumber - 1) % dailyLevels.Count;
        return dailyLevels[index];
    }

    public void LoadDailyChallenge()
    {
        if (DailyChallengeManager.Instance == null)
        {
            Debug.LogError("DailyChallengeManager is not present in the scene.");
            return;
        }

        DailyChallengeManager.Instance.EnsureDailyState();
        DailyChallengeLevelData dailyLevel = GetDailyLevel(DailyChallengeManager.Instance.CurrentDailyLevelNumber);

        if (dailyLevel == null)
        {
            Debug.LogError("No daily challenge level available to load (DailyChallengeLevels JSON empty?).");
            return;
        }

        IsDailySession = true;
        StartCoroutine(LoadLevelRoutine(LevelInfo.FromDaily(dailyLevel), gameSceneName));
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
        // Progress is keyed by the level's stable FileKey, not its position in the parsed
        // list, so adding/removing/reordering levels won't shift a returning player.
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
            levelId = "Level_Fallback",
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
        public string FileKey;
        public LevelType Type = LevelType.Normal;
        public LevelDataModel Data;

        /// <summary>Builds an entry from a parsed level; type/key derive from levelId
        /// prefix ("Level_3" → Normal/"3"), with the levelType field as fallback.</summary>
        public static LevelInfo FromData(LevelDataModel data)
        {
            LevelType type;
            string key;
            if (!LevelDataModel.TryParseFileName(data.levelId, out type, out key))
            {
                type = data.GetParsedLevelType();
                key = data.levelId ?? string.Empty;
            }

            return new LevelInfo
            {
                FileName = data.levelId,
                FileKey = key,
                Type = type,
                Data = data
            };
        }

        /// <summary>Transient entry for a daily session — never added to availableLevels.</summary>
        public static LevelInfo FromDaily(DailyChallengeLevelData daily)
        {
            return new LevelInfo
            {
                FileName = daily.levelId,
                FileKey = daily.dailyChallengeLevelNumber.ToString(),
                Type = LevelType.DailyChallenge,
                Data = daily
            };
        }

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
