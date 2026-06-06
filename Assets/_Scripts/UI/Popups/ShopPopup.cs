using PuppyPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop popup shell: shows the player's Coins and PawGems balances from
/// <see cref="EconomyHandler"/> and keeps them live while open. Pairs with
/// <see cref="ShopController"/> (IAP stubs + coin bundles) on the same prefab.
/// </summary>
public class ShopPopup : Popup
{
    [Header("Shop Popup")]
    [SerializeField] private TextMeshProUGUI coinsTxt;
    [SerializeField] private TextMeshProUGUI pawGemsTxt;
    [SerializeField] private Button closeBtn;

    protected override void Awake()
    {
        base.Awake();
        if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (closeBtn != null) closeBtn.onClick.RemoveListener(OnCloseClicked);
        EconomyHandler.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    protected override void OnOpened()
    {
        RefreshBalances();
        EconomyHandler.OnCurrencyChanged += HandleCurrencyChanged;
    }

    protected override void OnClosed()
    {
        EconomyHandler.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }

    private void RefreshBalances()
    {
        if (coinsTxt != null) coinsTxt.text = EconomyHandler.Coins.ToString();
        if (pawGemsTxt != null) pawGemsTxt.text = EconomyHandler.PawGems.ToString();
    }

    private void HandleCurrencyChanged(CurrencyType type, int newBalance)
    {
        switch (type)
        {
            case CurrencyType.Coins:
                if (coinsTxt != null) coinsTxt.text = newBalance.ToString();
                break;
            case CurrencyType.PawGems:
                if (pawGemsTxt != null) pawGemsTxt.text = newBalance.ToString();
                break;
        }
    }
}
