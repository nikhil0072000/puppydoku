using PuppyPuzzle.Ads;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;        // the root WinLosePopup
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject nextLevelButton;   // visible only on win and only if another level exists
    [SerializeField] private GameObject startGameButton;   // shown after tutorial completes
    [SerializeField] private GameObject reviveButton;      // shown only on lose if revive is available
    [SerializeField] private GameObject dailyResultPanel;
    [SerializeField] private TextMeshProUGUI dailyResultTitleText;
    [SerializeField] private TextMeshProUGUI dailyResultTimeText;
    [SerializeField] private TextMeshProUGUI dailyResultPercentileText;
    [SerializeField] private Button dailyResultContinueButton;
    [SerializeField] private TextMeshProUGUI nextLevelButtonText;
    [SerializeField] private TextMeshProUGUI startGameButtonText;

    [Header("Daily Lose")]
    [SerializeField] private GameObject dailyLosePanel;
    [SerializeField] private TextMeshProUGUI dailyLoseMessageText;
    [SerializeField] private GameObject dailyLoseRetryButton;
    [SerializeField] private GameObject dailyLoseHomeButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Ensure popup and daily result panel are hidden at start
        if (popupPanel != null) popupPanel.SetActive(false);
        if (dailyResultPanel != null) dailyResultPanel.SetActive(false);
        if (dailyLosePanel != null) dailyLosePanel.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(false);
        CacheButtonText();

        if (dailyResultContinueButton != null)
            dailyResultContinueButton.onClick.AddListener(OnDailyContinueClicked);
    }

    void OnDestroy()
    {
        if (dailyResultContinueButton != null)
            dailyResultContinueButton.onClick.RemoveListener(OnDailyContinueClicked);
    }

    private void CacheButtonText()
    {
        if (nextLevelButton != null && nextLevelButtonText == null)
            nextLevelButtonText = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
        if (startGameButton != null && startGameButtonText == null)
            startGameButtonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void SetButtonLabel(TextMeshProUGUI label, string text)
    {
        if (label != null)
            label.text = text;
    }

    public void ShowWin()
    {
        if (popupPanel != null) popupPanel.SetActive(true);

        bool hasNext = LevelLoader.Instance != null && LevelLoader.Instance.HasNextLevel;
        string nextLabel = LevelLoader.Instance != null ? LevelLoader.Instance.NextLevelButtonLabel : "Next Level";

        if (messageText != null)
            messageText.text = hasNext ? "🎉 Level Complete!" : "🏆 All Levels Complete!";

        if (retryButton != null) retryButton.SetActive(true);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(hasNext);
        if (startGameButton != null) startGameButton.SetActive(false);
        SetButtonLabel(nextLevelButtonText, nextLabel);
    }

    public void ShowTutorialComplete()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        if (messageText != null) messageText.text = "🎓 Tutorial Complete!";

        if (retryButton != null) retryButton.SetActive(false);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(true);
        SetButtonLabel(startGameButtonText, "Start Game");
    }

    public void ShowLose()
    {
        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.DailyChallenge)
        {
            ShowDailyLosePanel();
            return;
        }

        HideDailyLosePanel();
        if (popupPanel != null) popupPanel.SetActive(true);
        if (messageText != null) messageText.text = "💔 Out of Lives!";
        if (retryButton != null) retryButton.SetActive(true);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(false);
        if (reviveButton != null)
            reviveButton.SetActive(GameManager.Instance != null && GameManager.Instance.CanRevive);
    }

    public void OnReviveClicked()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (DummyRewardedAdService.Instance == null)
        {
            Debug.LogWarning("[PopupManager] No ad service available for revive.");
            if (popupPanel != null) popupPanel.SetActive(true);
            return;
        }

        Debug.Log("[PopupManager] Revive requested - playing ad.");
        DummyRewardedAdService.Instance.ShowRewardedAd(
            onReward: () =>
            {
                Debug.Log("[PopupManager] Revive ad complete. Granting one life.");
                GameManager.Instance?.ReviveAfterAd();
            },
            onSkip: () =>
            {
                Debug.Log("[PopupManager] Revive ad skipped.");
                ShowLose();
            });
    }

    public void ShowDailyResult(float elapsedSeconds, int percentile)
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (dailyResultPanel != null)
            dailyResultPanel.SetActive(true);

        if (dailyResultTitleText != null)
            dailyResultTitleText.text = "Challenge Cleared";

        if (dailyResultTimeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
            dailyResultTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        if (dailyResultPercentileText != null)
            dailyResultPercentileText.text = $"Beat {percentile}% of players";
    }

    public void OnDailyContinueClicked()
    {
        if (dailyResultPanel != null)
            dailyResultPanel.SetActive(false);

        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadNextNormalLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }

    private void HideDailyLosePanel()
    {
        if (dailyLosePanel != null)
            dailyLosePanel.SetActive(false);
    }

    private void ShowDailyLosePanel()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (dailyResultPanel != null) dailyResultPanel.SetActive(false);
        if (dailyLosePanel != null) dailyLosePanel.SetActive(true);

        if (dailyLoseMessageText != null)
            dailyLoseMessageText.text = "💔 Daily Challenge Failed";

        if (dailyLoseRetryButton != null) dailyLoseRetryButton.SetActive(true);
        if (dailyLoseHomeButton != null) dailyLoseHomeButton.SetActive(true);
    }

    public void ShowDailyAlreadyCompleted()
    {
        if (dailyResultPanel != null)
            dailyResultPanel.SetActive(false);

        if (popupPanel != null) popupPanel.SetActive(true);
        if (messageText != null) messageText.text = "You have completed Daily Challenge today.";

        if (retryButton != null) retryButton.SetActive(false);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(false);
        if (reviveButton != null) reviveButton.SetActive(false);
    }

    // Called by Retry button OnClick
    public void OnRetryClicked()
    {
        // Reload the Game scene (level data still in LevelLoader)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by Home button OnClick
    public void OnHomeClicked()
    {
        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.DailyChallenge)
        {
            DailyChallengeManager.Instance?.StopDailyTimer();
        }

        SceneManager.LoadScene("Home");
    }

    // Called by Next Level button OnClick
    public void OnNextLevelClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadNextLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }

    // Called by Start Game button OnClick
    public void OnStartGameClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadFirstNormalLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }
}
