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

    private void Awake()
    {
        ApplyTheme();
    }

    /// <summary>
    /// Applies theme-driven sprites at startup. Called from Awake so theme is
    /// applied before any hearts are updated.
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
        dailyTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetDailyTimerVisible(bool visible)
    {
        if (dailyTimerText != null)
            dailyTimerText.gameObject.SetActive(visible);
    }
}
