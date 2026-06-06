using System;

/// <summary>
/// UTC-based daily-reset time helpers. The daily challenge "day" is the number of
/// whole days since the Unix epoch in UTC; it ticks over at UTC midnight, every
/// 24 hours, identically for all players — no local-date parsing or storage.
/// </summary>
public static class TimerUtils
{
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Whole days elapsed since the Unix epoch (UTC). Increments at UTC midnight.</summary>
    public static int CurrentUtcDay => (int)(DateTime.UtcNow - Epoch).TotalDays;

    /// <summary>Seconds remaining until the next UTC midnight (the next daily reset).</summary>
    public static float SecondsUntilUtcReset
    {
        get
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextMidnight = now.Date.AddDays(1);
            double seconds = (nextMidnight - now).TotalSeconds;
            return seconds > 0d ? (float)seconds : 0f;
        }
    }

    /// <summary>Formats a duration as a countdown, e.g. "04h22m09sec".</summary>
    public static string FormatCountdown(double totalSeconds)
    {
        if (totalSeconds < 0d) totalSeconds = 0d;
        int hours = (int)(totalSeconds / 3600d);
        int minutes = (int)((totalSeconds % 3600d) / 60d);
        int seconds = (int)(totalSeconds % 60d);
        return string.Format("{0:00}h{1:00}m{2:00}sec", hours, minutes, seconds);
    }
}
