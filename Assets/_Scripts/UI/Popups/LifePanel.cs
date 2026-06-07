using PuppyPuzzle.Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives/hearts popup. Shows current lives out of <see cref="GameManager.MaxLifeCount"/>.
/// "Watch ad" is a placeholder refill hook — a real life-refill API on GameManager
/// does not exist yet, so it only refreshes the display after the ad.
/// </summary>
public class LifePanel : Popup
{
    [Header("Life Panel")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private Button closeButton;
    [Tooltip("Placeholder: watch a rewarded ad. Wire to a real refill API when one exists.")]
    [SerializeField] private Button watchAdButton;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAdClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
    }

    protected override void OnOpened()
    {
        RefreshLives();
    }

    private void RefreshLives()
    {
        if (livesText == null) return;

        if (GameManager.Instance != null)
            livesText.text = $"{GameManager.Instance.CurrentLives} / {GameManager.Instance.MaxLifeCount}";
        else
            livesText.text = "-";
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }

    private void OnWatchAdClicked()
    {
        PlayClick();
        if (DummyRewardedAdService.Instance == null) return;

        DummyRewardedAdService.Instance.ShowRewardedAd(
            onReward: RefreshLives,
            onSkip: () => { });
    }
}
