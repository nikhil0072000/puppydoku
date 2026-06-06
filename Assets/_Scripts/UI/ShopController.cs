using System;
using PuppyPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the shop UI. Two halves:
/// 1. IAP packages (remove-ads, starter pack, six gem tiers) — STUBS for now.
///    They just log; wire them to the IAP service once it exists.
/// 2. Coin bundles — three variants bought with PawGems through
///    <see cref="EconomyHandler"/>: spend gemsPrice, reward coinsValue.
/// </summary>
public class ShopController : MonoBehaviour
{
    /// <summary>
    /// One purchasable coin bundle: price in PawGems, payout in Coins,
    /// plus the labels and buy button that represent it in the UI.
    /// </summary>
    [Serializable]
    public class CoinBundle
    {
        [Tooltip("Label showing how many coins this bundle grants.")]
        public TextMeshProUGUI coinsTxt;
        [Tooltip("Label showing the PawGem price.")]
        public TextMeshProUGUI gemsTxt;
        public Button buyBtn;
        [Min(0)] public int coinsValue;
        [Min(0)] public int gemsPrice;
    }

    [Header("IAP Packages (stubs until IAP is integrated)")]
    [SerializeField] private Button adsRemovePkgBtn;
    [SerializeField] private Button startPkgBtn;
    [SerializeField] private Button handfulGemsBtn;
    [SerializeField] private Button smallPileGemsBtn;
    [SerializeField] private Button pouchGemsBtn;
    [SerializeField] private Button trophyGemsBtn;
    [SerializeField] private Button bagGemsBtn;
    [SerializeField] private Button vaultGemsBtn;

    [Header("Coin Bundles (bought with PawGems)")]
    [SerializeField] private CoinBundle variant1;
    [SerializeField] private CoinBundle variant2;
    [SerializeField] private CoinBundle variant3;

    /// <summary>Raised when a coin purchase succeeds (coinsGained, gemsSpent).</summary>
    public event Action<int, int> OnCoinsPurchased;

    /// <summary>Raised when a coin purchase fails because the player lacks gems (gemsPrice, gemsOwned).</summary>
    public event Action<int, int> OnInsufficientGems;

    private CoinBundle[] bundles;

    private void Awake()
    {
        bundles = new[] { variant1, variant2, variant3 };

        // --- IAP stubs ---
        WireStub(adsRemovePkgBtn, "AdsRemovePkg");
        WireStub(startPkgBtn, "StartPkg");
        WireStub(handfulGemsBtn, "HandfulGems");
        WireStub(smallPileGemsBtn, "SmallPileGems");
        WireStub(pouchGemsBtn, "PouchGems");
        WireStub(trophyGemsBtn, "TrophyGems");
        WireStub(bagGemsBtn, "BagGems");
        WireStub(vaultGemsBtn, "VaultGems");

        // --- Coin bundles ---
        foreach (CoinBundle bundle in bundles)
        {
            if (bundle?.buyBtn == null) continue;
            CoinBundle captured = bundle;
            bundle.buyBtn.onClick.AddListener(() => OnBuyCoinBundle(captured));
        }
    }

    private void OnEnable()
    {
        RefreshBundleLabels();
        RefreshAffordability();
        EconomyHandler.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        EconomyHandler.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    // ---- Coin bundles ----------------------------------------------------------

    private void OnBuyCoinBundle(CoinBundle bundle)
    {
        if (bundle == null) return;

        if (!EconomyHandler.TrySpendPawGems(bundle.gemsPrice))
        {
            Debug.Log($"[ShopController] Not enough PawGems for {bundle.coinsValue} coins " +
                      $"(need {bundle.gemsPrice}, have {EconomyHandler.PawGems}).");
            OnInsufficientGems?.Invoke(bundle.gemsPrice, EconomyHandler.PawGems);
            return;
        }

        EconomyHandler.AddCoins(bundle.coinsValue);
        OnCoinsPurchased?.Invoke(bundle.coinsValue, bundle.gemsPrice);
#if UNITY_EDITOR
        Debug.Log($"[ShopController] Bought {bundle.coinsValue} coins for {bundle.gemsPrice} gems. " +
                  $"Coins={EconomyHandler.Coins}, PawGems={EconomyHandler.PawGems}", this);
#endif
    }

    /// <summary>Pushes coinsValue/gemsPrice into the bundle labels.</summary>
    private void RefreshBundleLabels()
    {
        foreach (CoinBundle bundle in bundles)
        {
            if (bundle == null) continue;
            if (bundle.coinsTxt != null) bundle.coinsTxt.text = bundle.coinsValue.ToString();
            if (bundle.gemsTxt != null) bundle.gemsTxt.text = bundle.gemsPrice.ToString();
        }
    }

    /// <summary>Greys out buy buttons the player can't afford.</summary>
    private void RefreshAffordability()
    {
        int gems = EconomyHandler.PawGems;
        foreach (CoinBundle bundle in bundles)
        {
            if (bundle?.buyBtn != null)
                bundle.buyBtn.interactable = gems >= bundle.gemsPrice;
        }
    }

    private void HandleCurrencyChanged(CurrencyType type, int newBalance)
    {
        if (type == CurrencyType.PawGems)
            RefreshAffordability();
    }

    // ---- IAP stubs ---------------------------------------------------------------

    private void WireStub(Button button, string packageName)
    {
        if (button == null) return;
        button.onClick.AddListener(() => OnIapPackageClicked(packageName));
    }

    /// <summary>
    /// STUB — replace the body with a call into the IAP service once integrated.
    /// </summary>
    private void OnIapPackageClicked(string packageName)
    {
        Debug.Log($"[ShopController] '{packageName}' tapped — IAP not integrated yet.", this);
    }
}
