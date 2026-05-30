using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Victory popup. Next / Retry / Home. Reads <see cref="LevelLoader"/> for next-level
/// availability and label. Standalone for now — not yet called from GameManager.Win().
/// </summary>
public class VictoryPanel : Popup
{
    [Header("Victory Panel")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonLabel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;

    protected override void Awake()
    {
        base.Awake();
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
    }

    protected override void OnOpened()
    {
        bool hasNext = LevelLoader.Instance != null && LevelLoader.Instance.HasNextLevel;

        if (titleText != null)
            titleText.text = hasNext ? "Level Complete!" : "All Levels Complete!";

        if (nextButton != null)
            nextButton.gameObject.SetActive(hasNext);

        if (nextButtonLabel != null && LevelLoader.Instance != null)
            nextButtonLabel.text = LevelLoader.Instance.NextLevelButtonLabel;
    }

    private void OnNextClicked()
    {
        PlayClick();
        if (LevelLoader.Instance != null) LevelLoader.Instance.LoadNextLevel();
    }

    private void OnRetryClicked()
    {
        PlayClick();
        // Reload the active scene; level data is retained in LevelLoader.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnHomeClicked()
    {
        PlayClick();
        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelType == LevelType.DailyChallenge
            && DailyChallengeManager.Instance != null)
        {
            DailyChallengeManager.Instance.StopDailyTimer();
        }
        SceneManager.LoadScene("Home");
    }
}
