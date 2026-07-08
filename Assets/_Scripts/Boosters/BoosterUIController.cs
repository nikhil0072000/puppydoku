using DG.Tweening;
using PuppyPuzzle.Economy;
using UnityEngine;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Drives the UI state of every booster button in the Game scene (replaces the
    /// per-button PowerUpButton). Reads counts/coins from <see cref="EconomyHandler"/>
    /// and unlock state from <see cref="BoosterController"/>, then pushes the visual
    /// state into each <see cref="Booster"/>:
    ///
    ///   locked → lockState on, button disabled, unlock level number shown
    ///   count &gt; 0 → count badge
    ///   count == 0 &amp;&amp; coins ≥ price → coin-price badge
    ///   count == 0 &amp;&amp; coins short → ad icon
    /// </summary>
    public class BoosterUIController : MonoBehaviour
    {
        [Tooltip("Every booster button in the scene (RevealBooster, HintBooster, ...).")]
        [SerializeField] private Booster[] boosters;

        [Header("Feedback")]
        [SerializeField] private float pressPunchScale = 0.12f;
        [SerializeField] private float pressPunchDuration = 0.2f;

        private void Awake()
        {
            if (boosters == null) return;
            foreach (Booster booster in boosters)
            {
                if (booster == null) continue;
                Booster captured = booster;
                booster.Button.onClick.AddListener(() => OnBoosterClicked(captured));
            }
        }

        private void OnEnable()
        {
            EconomyHandler.OnBoosterChanged += HandleBoosterChanged;
            EconomyHandler.OnCurrencyChanged += HandleCurrencyChanged;
            RefreshAll();
        }

        private void Start()
        {
            // OnEnable can run before BoosterController.Awake during scene load
            // (sibling init order is not guaranteed), in which case the first
            // RefreshAll saw a null Instance and fell back to unlock level 1 /
            // fallback prices. Start runs after every Awake, so refresh again
            // with the real config values.
            RefreshAll();
        }

        private void OnDisable()
        {
            EconomyHandler.OnBoosterChanged -= HandleBoosterChanged;
            EconomyHandler.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        // ---- Input ----

        private void OnBoosterClicked(Booster booster)
        {
            PlayPunch(booster.transform);

            if (BoosterController.Instance == null)
            {
                Debug.LogWarning("[BoosterUIController] No BoosterController in scene.");
                return;
            }

            BoosterController.Instance.TryUseBooster(booster);
            RefreshBooster(booster); // counts/coins events cover most cases; this catches ad-state flips
        }

        // ---- Refresh ----

        public void RefreshAll()
        {
            if (boosters == null) return;
            foreach (Booster booster in boosters)
                RefreshBooster(booster);
        }

        private void RefreshBooster(Booster booster)
        {
            if (booster == null) return;

            bool unlocked = BoosterController.Instance != null && BoosterController.Instance.IsUnlocked(booster);
            int count = EconomyHandler.GetBoosterCount(booster.Type);
            bool canAfford = EconomyHandler.CanAfford(CurrencyType.Coins, booster.CoinPrice);

            booster.RefreshVisuals(unlocked, count, canAfford);
        }

        private void HandleBoosterChanged(BoosterType type, int newCount)
        {
            if (boosters == null) return;
            foreach (Booster booster in boosters)
            {
                if (booster != null && booster.Type == type)
                    RefreshBooster(booster);
            }
        }

        private void HandleCurrencyChanged(CurrencyType type, int newBalance)
        {
            if (type == CurrencyType.Coins)
                RefreshAll(); // coin balance affects the coins-vs-ad state of every booster
        }

        private void PlayPunch(Transform target)
        {
            if (target == null) return;
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * pressPunchScale, pressPunchDuration, 6, 0.5f)
                  .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
