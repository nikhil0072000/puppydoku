using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Hearts")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    [Header("Progress")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Level")]
    [SerializeField] private TextMeshProUGUI levelLabelText;

    [Header("Daily Timer")]
    [SerializeField] private TextMeshProUGUI dailyTimerText;

    // Last MM:SS pushed to the timer label, so we only format + reassign on a real change
    // (UpdateDailyTimer is called every frame from GameManager.Update).
    private int _lastTimerMinutes = -1;
    private int _lastTimerSeconds = -1;

    // Last lives count, so a runtime theme swap can re-apply heart sprites correctly.
    private int _lastLives;

    private void Awake()
    {
        ApplyTheme();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged()
    {
        ApplyTheme();
        UpdateHearts(_lastLives);
    }

    /// <summary>
    /// Applies theme-driven sprites. Called from Awake and whenever the theme is swapped
    /// at runtime so the HUD reflects the active theme without a scene reload.
    /// </summary>
    private void ApplyTheme()
    {
        ThemeData theme = ThemeManager.Current;
        if (theme == null) return;

        if (theme.hudHeartFullSprite != null)
            fullHeart = theme.hudHeartFullSprite;
        if (theme.hudHeartEmptySprite != null)
            emptyHeart = theme.hudHeartEmptySprite;
    }

    public void UpdateHearts(int currentLives)
    {
        _lastLives = currentLives;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = i < currentLives ? fullHeart : emptyHeart;
        }
    }

    public void UpdateProgress(int placed, int total)
    {
        if (progressText != null)
            progressText.text = $"{placed}/{total}";
    }

    public void SetLevelLabel(string label)
    {
        if (levelLabelText != null)
            levelLabelText.text = label;
    }

    public void UpdateDailyTimer(float elapsedSeconds)
    {
        if (dailyTimerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);

        // Skip the string allocation + TMP mesh rebuild when the displayed value is unchanged.
        if (minutes == _lastTimerMinutes && seconds == _lastTimerSeconds)
            return;

        _lastTimerMinutes = minutes;
        _lastTimerSeconds = seconds;
        dailyTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetDailyTimerVisible(bool visible)
    {
        if (dailyTimerText != null)
            dailyTimerText.gameObject.SetActive(visible);
    }
}
