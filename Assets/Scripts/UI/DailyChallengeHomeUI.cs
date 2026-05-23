using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyChallengeHomeUI : MonoBehaviour
{
    [SerializeField] private Button dailyButton;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("Separate label that shows the result rank, e.g. \"Top 18%\". Hidden until today's challenge is finished.")]
    [SerializeField] private TextMeshProUGUI percentText;

    [Tooltip("How often (seconds) the reset countdown subtitle ticks while the challenge is still available.")]
    [SerializeField] private float subtitleRefreshInterval = 1f;

    private TextMeshProUGUI buttonLabel;
    private float refreshTimer;

    private void Awake()
    {
        if (dailyButton != null)
            buttonLabel = dailyButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        // Only the reset countdown needs to tick; once completed the labels are static.
        if (DailyChallengeManager.Instance == null || DailyChallengeManager.Instance.HasCompletedToday)
            return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < subtitleRefreshInterval)
            return;

        refreshTimer = 0f;
        RefreshSubtitle();
    }

    public void Refresh()
    {
        if (DailyChallengeManager.Instance == null)
            return;

        DailyChallengeManager.Instance.EnsureDailyState();

        bool completed = DailyChallengeManager.Instance.HasCompletedToday;

        // Button label stays "Daily Challenge" regardless of state.
        if (buttonLabel != null)
            buttonLabel.text = DailyChallengeManager.Instance.GetDailyButtonText();

        // Result rank lives in its own label, only visible once finished.
        if (percentText != null)
        {
            percentText.text = DailyChallengeManager.Instance.GetDailyPercentText();
            percentText.gameObject.SetActive(completed);
        }

        RefreshSubtitle();

        // Once finished, the button is disabled (and shows its disabled color) until the daily resets.
        if (dailyButton != null)
            dailyButton.interactable = !completed;
    }

    private void RefreshSubtitle()
    {
        if (subtitleText != null)
            subtitleText.text = DailyChallengeManager.Instance.GetDailySubtitleText();
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
