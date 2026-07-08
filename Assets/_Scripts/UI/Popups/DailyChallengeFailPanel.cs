using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Defeat popup for daily challenge levels (normal levels use
/// <see cref="DefeatPanel"/>). Retry restarts the challenge; no revive — the
/// daily run is timed.
/// </summary>
public class DailyChallengeFailPanel : Popup
{
    [Header("Daily Fail Panel")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Restarts the daily challenge (reloads the Game scene).")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
    }

    protected override void OnOpened()
    {
        if (titleText != null)
            titleText.text = "Daily Challenge Failed";
    }

    private void OnRetryClicked()
    {
        PlayClick();
        // The popup canvas persists across scenes — close before leaving.
        RequestClose();
        // Reload the active scene; level data is retained in LevelLoader.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnHomeClicked()
    {
        PlayClick();
        DailyChallengeManager.Instance?.StopDailyTimer();
        RequestClose();
        SceneManager.LoadScene("Home");
    }
}
