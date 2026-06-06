using PuppyPuzzle.Economy;
using UnityEngine;

/// <summary>
/// Daily challenge state: which sequence number is live today, whether today's
/// challenge is done, and the in-run timer. "Today" is a UTC day number from
/// <see cref="TimerUtils"/> — resets at UTC midnight every 24h, no date strings.
///
/// Progression: the level number advances by one the first time a new day starts
/// after a completed challenge (numbers wrap via <see cref="LevelLoader.GetDailyLevel"/>).
/// Completing a daily grants its gemReward through <see cref="EconomyHandler"/>.
/// </summary>
public class DailyChallengeManager : MonoBehaviour
{
    public static DailyChallengeManager Instance { get; private set; }

    private const string PrefStateDay = "DailyChallenge.StateDay";          // int UTC day number
    private const string PrefLevelNumber = "DailyChallenge.LevelNumber";    // int, 1-based
    private const string PrefCompleted = "DailyChallenge.Completed";
    private const string PrefCompletionTime = "DailyChallenge.CompletionTime";
    private const string PrefPercentile = "DailyChallenge.Percentile";
    private const string PrefElapsedSeconds = "DailyChallenge.ElapsedSeconds";

    /// <summary>1-based sequence number of today's daily level.</summary>
    public int CurrentDailyLevelNumber { get; private set; } = 1;

    public bool HasCompletedToday { get; private set; }
    public float CompletedTimeSeconds { get; private set; }
    public int CompletedPercentile { get; private set; }
    public float ElapsedSeconds { get; private set; }

    public bool IsDailySessionRunning => timerRunning && !HasCompletedToday;
    public bool DailyAvailable => !HasCompletedToday;

    /// <summary>PawGems today's level pays out (0 if no level data is available).</summary>
    public int CurrentGemReward
    {
        get
        {
            DailyChallengeLevelData level = LevelLoader.Instance != null
                ? LevelLoader.Instance.GetDailyLevel(CurrentDailyLevelNumber)
                : null;
            return level != null ? level.gemReward : 0;
        }
    }

    private bool timerRunning;
    private int stateDay = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!timerRunning)
            return;

        // Accumulate in memory only. Persisting every second would flush PlayerPrefs to disk
        // (a synchronous, main-thread I/O) once per second — we save on pause/focus-loss/quit
        // and on completion instead (see OnApplicationPause/OnApplicationFocus/OnDestroy).
        ElapsedSeconds += Time.unscaledDeltaTime;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && timerRunning)
            SaveState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && timerRunning)
            SaveState();
    }

    private void OnDestroy()
    {
        if (timerRunning)
            SaveState();
    }

    // ---- State ----

    private void LoadState()
    {
        stateDay = PlayerPrefs.GetInt(PrefStateDay, -1);
        CurrentDailyLevelNumber = Mathf.Max(1, PlayerPrefs.GetInt(PrefLevelNumber, 1));
        HasCompletedToday = PlayerPrefs.GetInt(PrefCompleted, 0) == 1;
        CompletedTimeSeconds = PlayerPrefs.GetFloat(PrefCompletionTime, 0f);
        CompletedPercentile = PlayerPrefs.GetInt(PrefPercentile, 0);
        ElapsedSeconds = PlayerPrefs.GetFloat(PrefElapsedSeconds, 0f);

        EnsureDailyState();
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PrefStateDay, stateDay);
        PlayerPrefs.SetInt(PrefLevelNumber, CurrentDailyLevelNumber);
        PlayerPrefs.SetInt(PrefCompleted, HasCompletedToday ? 1 : 0);
        PlayerPrefs.SetFloat(PrefCompletionTime, CompletedTimeSeconds);
        PlayerPrefs.SetInt(PrefPercentile, CompletedPercentile);
        PlayerPrefs.SetFloat(PrefElapsedSeconds, ElapsedSeconds);
        PlayerPrefs.Save();
    }

    /// <summary>Rolls the state over if a new UTC day has started. Safe to call any time.</summary>
    public void EnsureDailyState()
    {
        int today = TimerUtils.CurrentUtcDay;
        if (stateDay == today)
            return;

        // New day. If the previous day's challenge was completed, advance the sequence.
        if (stateDay >= 0 && HasCompletedToday)
            CurrentDailyLevelNumber++;

        stateDay = today;
        HasCompletedToday = false;
        CompletedTimeSeconds = 0f;
        CompletedPercentile = 0;
        ElapsedSeconds = 0f;
        timerRunning = false;
        SaveState();
    }

    // ---- Run timer ----

    public void StartDailyTimer()
    {
        if (HasCompletedToday)
            return;

        timerRunning = true;
    }

    public void StopDailyTimer()
    {
        timerRunning = false;
    }

    public void ResetDailyTimer()
    {
        ElapsedSeconds = 0f;
    }

    // ---- Completion ----

    public void RecordDailyCompletion(float elapsedSeconds)
    {
        EnsureDailyState();
        if (HasCompletedToday)
            return; // already rewarded today

        HasCompletedToday = true;
        CompletedTimeSeconds = elapsedSeconds;
        CompletedPercentile = CalculatePercentile(elapsedSeconds);
        timerRunning = false;

        int reward = CurrentGemReward;
        if (reward > 0)
            EconomyHandler.AddPawGems(reward);

        SaveState();
    }

    /// <summary>Seconds until the next UTC-midnight reset.</summary>
    public float GetSecondsUntilReset() => TimerUtils.SecondsUntilUtcReset;

    /// <summary>Game-scene header label for a daily session, e.g. "Daily 3".</summary>
    public string GetChallengeDateText() => $"Daily {CurrentDailyLevelNumber}";

    public string GetDailyPercentText()
    {
        // Shown once today's challenge is finished, e.g. "Top 18%".
        // CompletedPercentile is "faster than X% of players" (higher = better), so the
        // rank shown is its complement: beating 82% of players => "Top 18%".
        return HasCompletedToday ? $"Top {Mathf.Clamp(100 - CompletedPercentile, 1, 100)}%" : string.Empty;
    }

    private int CalculatePercentile(float elapsedSeconds)
    {
        float benchmark = 90f;
        float normalized = Mathf.Clamp01(elapsedSeconds / benchmark);
        int percentile = Mathf.RoundToInt(99f - normalized * 70f);
        return Mathf.Clamp(percentile, 10, 99);
    }
}
