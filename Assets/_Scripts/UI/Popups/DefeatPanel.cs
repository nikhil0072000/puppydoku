using PuppyPuzzle.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Defeat popup. Retry / Home / Revive. Revive plays a rewarded ad via
/// <see cref="DummyRewardedAdService"/> and grants a life through
/// <see cref="GameManager.ReviveAfterAd"/>. Standalone for now — not yet called
/// from GameManager.Lose().
/// </summary>
public class DefeatPanel : Popup
{
    [Header("Defeat Panel")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button reviveButton;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
        if (reviveButton != null) reviveButton.onClick.AddListener(OnReviveClicked);
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
        if (reviveButton != null) reviveButton.onClick.RemoveListener(OnReviveClicked);
    }

    protected override void OnOpened()
    {
        if (titleText != null)
            titleText.text = "Out of Lives!";

        bool canRevive = GameManager.Instance != null && GameManager.Instance.CanRevive;
        if (reviveButton != null)
            reviveButton.gameObject.SetActive(canRevive);
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

    private void OnReviveClicked()
    {
        PlayClick();
        if (DummyRewardedAdService.Instance == null)
        {
            Debug.LogWarning("[DefeatPanel] No ad service available for revive.");
            return;
        }

        RequestClose();
        DummyRewardedAdService.Instance.ShowRewardedAd(
            onReward: () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ReviveAfterAd();
            },
            onSkip: () =>
            {
                if (PopupCanvasManager.Instance != null)
                    PopupCanvasManager.Instance.Show(PopupType.Defeat);
            });
    }
}
