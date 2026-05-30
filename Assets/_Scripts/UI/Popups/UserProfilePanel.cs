using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User profile popup. There is no structured profile in the project yet, so this
/// shell aggregates the scattered PlayerPrefs (level progress, tutorial flag, daily
/// status) into a read-only display. Extend as a real profile model is added.
/// </summary>
public class UserProfilePanel : Popup
{
    [Header("User Profile Panel")]
    [SerializeField] private TextMeshProUGUI levelProgressText;
    [SerializeField] private TextMeshProUGUI tutorialStatusText;
    [SerializeField] private TextMeshProUGUI dailyStatusText;
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    protected override void OnOpened()
    {
        RefreshProfile();
    }

    private void RefreshProfile()
    {
        if (levelProgressText != null)
        {
            string savedLevel = PlayerPrefs.GetString("SavedLevelKey", "None");
            levelProgressText.text = $"Level: {savedLevel}";
        }

        if (tutorialStatusText != null)
        {
            bool tutorialDone = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            tutorialStatusText.text = tutorialDone ? "Tutorial: Complete" : "Tutorial: Incomplete";
        }

        if (dailyStatusText != null)
        {
            bool dailyDone = PlayerPrefs.GetInt("DailyChallenge.Completed", 0) == 1;
            dailyStatusText.text = dailyDone ? "Daily: Done today" : "Daily: Available";
        }
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }
}
