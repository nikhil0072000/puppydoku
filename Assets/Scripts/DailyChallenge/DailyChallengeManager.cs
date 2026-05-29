using System;
using UnityEngine;

public class DailyChallengeManager : MonoBehaviour
{
    public static DailyChallengeManager Instance { get; private set; }

    private const string PrefDailyDate = "DailyChallenge.Date";
    private const string PrefCompleted = "DailyChallenge.Completed";
    private const string PrefCompletionTime = "DailyChallenge.CompletionTime";
    private const string PrefPercentile = "DailyChallenge.Percentile";
    private const string PrefSelectedLevelKey = "DailyChallenge.LevelKey";
    private const string PrefElapsedSeconds = "DailyChallenge.ElapsedSeconds";

    public DateTime ChallengeDate { get; private set; }
    public bool HasCompletedToday { get; private set; }
    public float CompletedTimeSeconds { get; private set; }
    public int CompletedPercentile { get; private set; }
    public float ElapsedSeconds { get; private set; }

    public bool IsDailySessionRunning => timerRunning && !HasCompletedToday;
    public bool DailyAvailable => !HasCompletedToday;
    public string SelectedLevelKey => selectedLevelKey;

    private bool timerRunning;
    private string selectedLevelKey;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDailyState();
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
            SaveDailyState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && timerRunning)
            SaveDailyState();
    }

    private void OnDestroy()
    {
        if (timerRunning)
            SaveDailyState();
    }

    private void LoadDailyState()
    {
        string rawDate = PlayerPrefs.GetString(PrefDailyDate, string.Empty);
        DateTime savedDate = ParseStoredDate(rawDate);
        ChallengeDate = savedDate != DateTime.MinValue ? savedDate : DateTime.Now.Date;

        if (ChallengeDate.Date != DateTime.Now.Date)
        {
            ResetDailyState();
            return;
        }

        HasCompletedToday = PlayerPrefs.GetInt(PrefCompleted, 0) == 1;
        CompletedTimeSeconds = PlayerPrefs.GetFloat(PrefCompletionTime, 0f);
        CompletedPercentile = PlayerPrefs.GetInt(PrefPercentile, 0);
        selectedLevelKey = PlayerPrefs.GetString(PrefSelectedLevelKey, string.Empty);
        ElapsedSeconds = PlayerPrefs.GetFloat(PrefElapsedSeconds, 0f);
    }

    private void SaveDailyState()
    {
        PlayerPrefs.SetString(PrefDailyDate, ChallengeDate.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt(PrefCompleted, HasCompletedToday ? 1 : 0);
        PlayerPrefs.SetFloat(PrefCompletionTime, CompletedTimeSeconds);
        PlayerPrefs.SetInt(PrefPercentile, CompletedPercentile);
        PlayerPrefs.SetString(PrefSelectedLevelKey, selectedLevelKey ?? string.Empty);
        PlayerPrefs.SetFloat(PrefElapsedSeconds, ElapsedSeconds);
        PlayerPrefs.Save();
    }

    private void ResetDailyState()
    {
        ChallengeDate = DateTime.Now.Date;
        HasCompletedToday = false;
        CompletedTimeSeconds = 0f;
        CompletedPercentile = 0;
        selectedLevelKey = string.Empty;
        ElapsedSeconds = 0f;
        timerRunning = false;
        SaveDailyState();
    }

    private DateTime ParseStoredDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DateTime.MinValue;

        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime result))
            return result;

        return DateTime.MinValue;
    }

    public void EnsureDailyState()
    {
        if (ChallengeDate.Date != DateTime.Now.Date)
            ResetDailyState();
    }

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

    public void RecordDailyCompletion(float elapsedSeconds)
    {
        HasCompletedToday = true;
        CompletedTimeSeconds = elapsedSeconds;
        CompletedPercentile = CalculatePercentile(elapsedSeconds);
        timerRunning = false;
        SaveDailyState();
    }

    public void SetSelectedDailyLevelKey(string levelKey)
    {
        selectedLevelKey = levelKey ?? string.Empty;
        SaveDailyState();
    }

    public float GetSecondsUntilReset()
    {
        DateTime nextReset = ChallengeDate.AddDays(1);
        return Mathf.Max(0f, (float)(nextReset - DateTime.Now).TotalSeconds);
    }

    public string GetDailyButtonText()
    {
        // The button label is always the mode name; it stays fixed even after completion.
        return "Daily Challenge";
    }

    public string GetDailyPercentText()
    {
        // Shown in a separate label once today's challenge is finished, e.g. "Top 18%".
        // CompletedPercentile is "faster than X% of players" (higher = better), so the
        // rank shown is its complement: beating 82% of players => "Top 18%".
        return HasCompletedToday ? $"Top {Mathf.Clamp(100 - CompletedPercentile, 1, 100)}%" : string.Empty;
    }

    public string GetDailySubtitleText()
    {
        if (HasCompletedToday)
            return $"Time {FormatTime(CompletedTimeSeconds)}";

        float secondsUntilReset = GetSecondsUntilReset();
        if (secondsUntilReset <= 0f)
            return "New challenge available soon";

        return $"Resets in {FormatCountdown(secondsUntilReset)}";
    }

    public string GetChallengeDateText()
    {
        // Game-scene header format, e.g. "May.22".
        return ChallengeDate.ToString("MMM.dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private int CalculatePercentile(float elapsedSeconds)
    {
        float benchmark = 90f;
        float normalized = Mathf.Clamp01(elapsedSeconds / benchmark);
        int percentile = Mathf.RoundToInt(99f - normalized * 70f);
        return Mathf.Clamp(percentile, 10, 99);
    }

    private string FormatTime(float elapsedSeconds)
    {
        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private string FormatCountdown(float totalSeconds)
    {
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        if (hours > 0)
            return string.Format("{0}h {1:00}m {2:00}s", hours, minutes, seconds);
        return string.Format("{0}m {1:00}s", minutes, seconds);
    }
}
