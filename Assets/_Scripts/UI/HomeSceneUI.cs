using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Home scene UI: the main Play button and the Daily Challenge button,
/// plus their associated labels (current level, daily subtitle/countdown, daily result rank).
/// Merges the former HomeUI and DailyChallengeHomeUI behaviours into one component.
/// </summary>
public class HomeSceneUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button playDailyChallengeButton;

    [Header("Labels")]
    [Tooltip("Shows the current Normal level the Play button will load, e.g. \"Level 3\".")]
    [SerializeField] private TextMeshProUGUI levelLabelText;

    [Tooltip("Daily Challenge subtitle: the reset countdown while available, or completion text once finished.")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("Separate label that shows the daily result rank, e.g. \"Top 18%\". Hidden until today's challenge is finished.")]
    [SerializeField] private TextMeshProUGUI percentText;

    [Tooltip("How often (seconds) the reset countdown subtitle ticks while the challenge is still available.")]
    [SerializeField] private float subtitleRefreshInterval = 1f;

    private TextMeshProUGUI dailyButtonLabel;
    private float refreshTimer;
    private string lastSubtitle;

    private void Awake()
    {
        if (playDailyChallengeButton != null)
            dailyButtonLabel = playDailyChallengeButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (playDailyChallengeButton != null)
            playDailyChallengeButton.onClick.AddListener(OnDailyButtonClicked);

        RefreshAll();
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        if (playDailyChallengeButton != null)
            playDailyChallengeButton.onClick.RemoveListener(OnDailyButtonClicked);
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

    private void RefreshAll()
    {
        RefreshLevelLabel();
        RefreshDaily();
    }

    private void RefreshLevelLabel()
    {
        if (levelLabelText == null)
            return;

        levelLabelText.text = LevelLoader.Instance != null
            ? LevelLoader.Instance.CurrentLevelButtonLabel
            : "Level 1";
    }

    private void RefreshDaily()
    {
        if (DailyChallengeManager.Instance == null)
            return;

        DailyChallengeManager.Instance.EnsureDailyState();

        bool completed = DailyChallengeManager.Instance.HasCompletedToday;

        // Button label stays "Daily Challenge" regardless of state.
        if (dailyButtonLabel != null)
            dailyButtonLabel.text = DailyChallengeManager.Instance.GetDailyButtonText();

        // Result rank lives in its own label, only visible once finished.
        if (percentText != null)
        {
            percentText.text = DailyChallengeManager.Instance.GetDailyPercentText();
            percentText.gameObject.SetActive(completed);
        }

        RefreshSubtitle();

        // Once finished, the button is disabled (and shows its disabled color) until the daily resets.
        if (playDailyChallengeButton != null)
            playDailyChallengeButton.interactable = !completed;
    }

    private void RefreshSubtitle()
    {
        if (subtitleText == null || DailyChallengeManager.Instance == null)
            return;

        // Only reassign (and trigger a TMP rebuild) when the countdown string actually changes.
        string subtitle = DailyChallengeManager.Instance.GetDailySubtitleText();
        if (subtitle == lastSubtitle)
            return;

        lastSubtitle = subtitle;
        subtitleText.text = subtitle;
    }

    public void OnPlayClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadCurrentLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
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
