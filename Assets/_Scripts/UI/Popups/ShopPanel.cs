using PuppyPuzzle.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop popup. NOTE: the project has no currency/IAP economy yet. This shell models
/// the existing "refill via rewarded ad" pattern: it shows hint chances and grants
/// one on a watched ad through <see cref="PowerUpManager"/>. Replace with a real
/// store/economy when one is designed.
/// </summary>
public class ShopPanel : Popup
{
    [Header("Shop Panel (placeholder economy)")]
    [SerializeField] private TextMeshProUGUI hintChancesText;
    [Tooltip("Watch a rewarded ad to grant one Hint power-up chance.")]
    [SerializeField] private Button buyHintButton;
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        if (buyHintButton != null) buyHintButton.onClick.AddListener(OnBuyHintClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (buyHintButton != null) buyHintButton.onClick.RemoveListener(OnBuyHintClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    protected override void OnOpened()
    {
        RefreshChances();
    }

    private void RefreshChances()
    {
        if (hintChancesText == null || PowerUpManager.Instance == null) return;
        hintChancesText.text = $"Hints: {PowerUpManager.Instance.GetChances(PowerUpType.Hint)}";
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }

    private void OnBuyHintClicked()
    {
        PlayClick();
        if (DummyRewardedAdService.Instance == null || PowerUpManager.Instance == null) return;

        DummyRewardedAdService.Instance.ShowRewardedAd(
            onReward: () =>
            {
                PowerUpManager.Instance.GrantChance(PowerUpType.Hint);
                RefreshChances();
            },
            onSkip: () => { });
    }
}
