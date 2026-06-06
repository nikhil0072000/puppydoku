using System;
using UnityEngine;
using PuppyPuzzle.Economy;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Thin scene-facing facade over <see cref="EconomyHandler"/>, which now owns
    /// all booster-count storage (same PlayerPrefs keys, so old saves carry over).
    /// Kept so existing consumers (PowerUpButton, HintController, ShopPanel) and
    /// scene setups keep working, and so the fresh-save default stays designer
    /// tweakable in the inspector. Singleton that survives scene loads; lives in
    /// the first-loaded (bootstrap/Loading) scene.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        [Tooltip("Starting (and maximum-by-default) chances each power-up has on a fresh save.")]
        [Min(0)]
        [SerializeField] private int defaultChances = 5;

        /// <summary>Raised whenever a power-up's remaining count changes (type, newCount).</summary>
        public event Action<PowerUpType, int> OnChancesChanged;

        public int DefaultChances => defaultChances;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EconomyHandler.DefaultBoosterCount = defaultChances;
            EconomyHandler.OnBoosterChanged += HandleBoosterChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                EconomyHandler.OnBoosterChanged -= HandleBoosterChanged;
        }

        /// <summary>Remaining chances for a power-up (initialised to default on first read).</summary>
        public int GetChances(PowerUpType type) => EconomyHandler.GetBoosterCount(type);

        public bool HasChance(PowerUpType type) => EconomyHandler.HasBooster(type);

        /// <summary>Spends one chance. Returns false (and changes nothing) if none remain.</summary>
        public bool TryConsume(PowerUpType type) => EconomyHandler.TryConsumeBooster(type);

        /// <summary>Adds one chance — used by the rewarded-ad reward and by hint refunds.</summary>
        public void GrantChance(PowerUpType type) => EconomyHandler.GrantBooster(type);

        private void HandleBoosterChanged(PowerUpType type, int newCount)
        {
            OnChancesChanged?.Invoke(type, newCount);
        }
    }
}
