using System;
using PuppyPuzzle.Ads;
using PuppyPuzzle.Economy;
using UnityEngine;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// All booster functionality in one place (replaces PowerUpManager + the old
    /// PowerUpButton click logic). Counts persist in <see cref="EconomyHandler"/>
    /// (legacy "powerup_chances_" keys — old saves carry over).
    ///
    /// Use flow for a tap (see <see cref="TryUseBooster"/>):
    ///   locked            → ignored (button is disabled anyway)
    ///   has booster count → consume one and activate (refund on failure)
    ///   count == 0        → spend coins to use once (coins refunded on failure)
    ///   coins short       → rewarded ad (stub service) → activate on reward
    /// </summary>
    public class BoosterController : MonoBehaviour
    {
        public static BoosterController Instance { get; private set; }

        [Tooltip("Per-booster tuning: unlock level and coin price.")]
        [SerializeField] private BoosterConfigSO config;

        [Tooltip("Booster count a fresh save starts with (pushed into EconomyHandler).")]
        [Min(0)]
        [SerializeField] private int defaultBoosterCount = 5;

        /// <summary>Raised whenever a booster's remaining count changes (type, newCount).</summary>
        public event Action<BoosterType, int> OnBoosterCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                Debug.LogWarning("[BoosterController] No BoosterConfigSO assigned — using fallback unlock levels/prices.", this);

            EconomyHandler.DefaultBoosterCount = defaultBoosterCount;
            EconomyHandler.OnBoosterChanged += HandleBoosterChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                EconomyHandler.OnBoosterChanged -= HandleBoosterChanged;
                Instance = null;
            }
        }

        // ---- Config (BoosterConfigSO-backed) ----

        /// <summary>Normal level number at which the booster unlocks.</summary>
        public int GetUnlockLevel(BoosterType type) =>
            config != null ? config.GetUnlockLevel(type) : BoosterConfigSO.FallbackUnlockLevel;

        /// <summary>Coin price for a single use when the player has no boosters left.</summary>
        public int GetCoinPrice(BoosterType type) =>
            config != null ? config.GetCoinPrice(type) : BoosterConfigSO.FallbackCoinPrice;

        // ---- Counts (EconomyHandler-backed) ----

        public int GetCount(BoosterType type) => EconomyHandler.GetBoosterCount(type);
        public bool HasCount(BoosterType type) => EconomyHandler.HasBooster(type);
        public void Grant(BoosterType type, int count = 1) => EconomyHandler.GrantBooster(type, count);

        // ---- Unlocking ----

        /// <summary>True once the player's Normal-level progression reaches the booster's unlock level.</summary>
        public bool IsUnlocked(Booster booster)
        {
            if (booster == null) return false;
            int playerLevel = LevelLoader.Instance != null ? LevelLoader.Instance.CurrentNormalLevelNumber : 1;
            return playerLevel >= booster.UnlockAtLevel;
        }

        // ---- Use flow ----

        /// <summary>
        /// Runs the full tap flow for a booster. Returns true if the booster was
        /// activated (or an ad was opened on its behalf).
        /// </summary>
        public bool TryUseBooster(Booster booster)
        {
            if (booster == null || !IsUnlocked(booster))
                return false;

            BoosterType type = booster.Type;

            // 1) Spend a stored booster (optimistically; refund if it couldn't act).
            if (EconomyHandler.TryConsumeBooster(type))
            {
                if (!booster.TryActivate())
                {
                    EconomyHandler.GrantBooster(type);
                    Debug.Log($"[BoosterController] {type} could not act right now — booster refunded.");
                    return false;
                }
                return true;
            }

            // 2) Out of boosters → pay coins to use once.
            if (EconomyHandler.TrySpendCoins(booster.CoinPrice))
            {
                if (!booster.TryActivate())
                {
                    EconomyHandler.AddCoins(booster.CoinPrice);
                    Debug.Log($"[BoosterController] {type} could not act right now — coins refunded.");
                    return false;
                }
                return true;
            }

            // 3) Coins short → rewarded ad grants a single use.
            if (DummyRewardedAdService.Instance != null)
            {
                DummyRewardedAdService.Instance.ShowRewardedAd(
                    onReward: () =>
                    {
                        // Ad watched = one use. If the board state blocks it, bank a
                        // count instead so the watch is never wasted.
                        if (!booster.TryActivate())
                            EconomyHandler.GrantBooster(type);
                    },
                    onSkip: null);
                return true;
            }

            Debug.LogWarning("[BoosterController] No ad service in scene; cannot offer rewarded use.");
            return false;
        }

        private void HandleBoosterChanged(BoosterType type, int newCount)
        {
            OnBoosterCountChanged?.Invoke(type, newCount);
        }
    }
}
