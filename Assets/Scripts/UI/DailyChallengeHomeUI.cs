using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyChallengeHomeUI : MonoBehaviour
{
    [SerializeField] private Button dailyButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Color activeColor = new Color(0.6f, 0.2f, 0.9f);
    [SerializeField] private Color completedColor = new Color(0.35f, 0.18f, 0.45f);

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (DailyChallengeManager.Instance == null)
            return;

        DailyChallengeManager.Instance.EnsureDailyState();

        bool completed = DailyChallengeManager.Instance.HasCompletedToday;

        if (titleText != null)
            titleText.text = DailyChallengeManager.Instance.GetDailyTitleText();

        if (subtitleText != null)
            subtitleText.text = DailyChallengeManager.Instance.GetDailySubtitleText();

        if (dateText != null)
            dateText.text = DailyChallengeManager.Instance.GetChallengeDateText();

        if (dailyButton != null)
        {
            dailyButton.interactable = true;
            ColorBlock colors = dailyButton.colors;
            colors.normalColor = completed ? completedColor : activeColor;
            dailyButton.colors = colors;

            TextMeshProUGUI buttonLabel = dailyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
                buttonLabel.text = DailyChallengeManager.Instance.GetDailyButtonText();
        }
    }

    public void OnDailyButtonClicked()
    {
        if (DailyChallengeManager.Instance == null)
        {
            Debug.LogError("DailyChallengeManager missing.");
            return;
        }

        if (DailyChallengeManager.Instance.HasCompletedToday)
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.ShowDailyAlreadyCompleted();
            else
                Debug.Log("Daily Challenge already completed today.");
            return;
        }

        if (LevelLoader.Instance == null)
        {
            Debug.LogError("LevelLoader instance not found.");
            return;
        }

        LevelLoader.Instance.LoadDailyChallenge();
    }
}
