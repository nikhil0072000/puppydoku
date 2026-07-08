using System;
using UnityEngine;
using PuppyPuzzle.Boosters;

namespace PuppyPuzzle.Economy
{
    /// <summary>
    /// The soft currencies the player can earn and spend.
    /// The enum's string name is used as the PlayerPrefs key suffix,
    /// so DO NOT rename or reorder existing entries.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>Common soft currency earned through play.</summary>
        Coins,

        /// <summary>Premium currency (IAP / rare rewards).</summary>
        PawGems
    }

    /// <summary>
    /// Single source of truth for the player's economy: coins, pawgems and
    /// power-up/booster counts. Static (like <see cref="ProfileStore"/>) so it
    /// needs no scene wiring; persists via PlayerPrefs, matching the project's
    /// other persistence.
    ///
    /// Booster counts reuse the legacy "powerup_chances_&lt;Type&gt;" keys that
    /// the old PowerUpManager wrote before this class existed, so existing saves
    /// carry over with no migration.
    /// </summary>
    public static class EconomyHandler
    {
        // ---- Keys -----------------------------------------------------------

        private const string CurrencyKeyPrefix = "economy_";          // economy_Coins / economy_PawGems
        private const string BoosterKeyPrefix = "powerup_chances_";   // legacy key, kept for save compatibility

        // ---- Defaults (fresh save) ------------------------------------------

        public const int DefaultCoins = 0;
        public const int DefaultPawGems = 0;

        /// <summary>
        /// Booster count a fresh save starts with. <see cref="PuppyPuzzle.Boosters.BoosterController"/>
        /// overrides this on Awake from its BoosterConfigSO's InitialBoosters value.
        /// </summary>
        public static int DefaultBoosterCount { get; set; } = 5;

        // ---- Events ----------------------------------------------------------

        /// <summary>Raised whenever a currency balance changes (type, newBalance).</summary>
        public static event Action<CurrencyType, int> OnCurrencyChanged;

        /// <summary>Raised whenever a booster's remaining count changes (type, newCount).</summary>
        public static event Action<BoosterType, int> OnBoosterChanged;

        // ---- Currency: generic -----------------------------------------------

        /// <summary>Current balance for a currency (initialised to its default on first read).</summary>
        public static int GetBalance(CurrencyType type)
        {
            string key = CurrencyKey(type);
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, DefaultFor(type));
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetInt(key, DefaultFor(type));
        }

        /// <summary>Adds <paramref name="amount"/> (must be &gt;= 0) to a currency balance.</summary>
        public static void Add(CurrencyType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[EconomyHandler] Add called with negative amount ({amount}) for {type}; use TrySpend instead.");
                return;
            }
            SetBalance(type, GetBalance(type) + amount);
        }

        /// <summary>Returns true if the balance covers <paramref name="amount"/>.</summary>
        public static bool CanAfford(CurrencyType type, int amount) => GetBalance(type) >= amount;

        /// <summary>Deducts <paramref name="amount"/>. Returns false (and changes nothing) if unaffordable.</summary>
        public static bool TrySpend(CurrencyType type, int amount)
        {
            if (amount < 0) return false;
            int current = GetBalance(type);
            if (current < amount) return false;
            SetBalance(type, current - amount);
            return true;
        }

        // ---- Currency: convenience shortcuts ---------------------------------

        public static int Coins => GetBalance(CurrencyType.Coins);
        public static int PawGems => GetBalance(CurrencyType.PawGems);

        public static void AddCoins(int amount) => Add(CurrencyType.Coins, amount);
        public static void AddPawGems(int amount) => Add(CurrencyType.PawGems, amount);

        public static bool TrySpendCoins(int amount) => TrySpend(CurrencyType.Coins, amount);
        public static bool TrySpendPawGems(int amount) => TrySpend(CurrencyType.PawGems, amount);

        // ---- Boosters (power-up chances) -------------------------------------

        /// <summary>
        /// Remaining count for a booster. The default is applied lazily (no eager
        /// PlayerPrefs write): a read that happens before BoosterController.Awake
        /// pushes the config value must not bake the wrong default into the save.
        /// The key is first written when the count actually changes (consume/grant).
        /// </summary>
        public static int GetBoosterCount(BoosterType type)
        {
            return PlayerPrefs.GetInt(BoosterKey(type), DefaultBoosterCount);
        }

        public static bool HasBooster(BoosterType type) => GetBoosterCount(type) > 0;

        /// <summary>Spends one booster. Returns false (and changes nothing) if none remain.</summary>
        public static bool TryConsumeBooster(BoosterType type)
        {
            int current = GetBoosterCount(type);
            if (current <= 0) return false;
            SetBoosterCount(type, current - 1);
            return true;
        }

        /// <summary>Adds boosters — rewarded-ad grants, hint refunds, shop purchases.</summary>
        public static void GrantBooster(BoosterType type, int count = 1)
        {
            if (count <= 0) return;
            SetBoosterCount(type, GetBoosterCount(type) + count);
        }

        // ---- Internals --------------------------------------------------------

        private static void SetBalance(CurrencyType type, int value)
        {
            value = Mathf.Max(0, value);
            PlayerPrefs.SetInt(CurrencyKey(type), value);
            PlayerPrefs.Save();
            OnCurrencyChanged?.Invoke(type, value);
#if UNITY_EDITOR
            Debug.Log($"[EconomyHandler] {type} = {value}");
#endif
        }

        private static void SetBoosterCount(BoosterType type, int value)
        {
            value = Mathf.Max(0, value);
            PlayerPrefs.SetInt(BoosterKey(type), value);
            PlayerPrefs.Save();
            OnBoosterChanged?.Invoke(type, value);
#if UNITY_EDITOR
            Debug.Log($"[EconomyHandler] {type} boosters = {value}");
#endif
        }

        private static int DefaultFor(CurrencyType type) =>
            type == CurrencyType.PawGems ? DefaultPawGems : DefaultCoins;

        private static string CurrencyKey(CurrencyType type) => CurrencyKeyPrefix + type;
        private static string BoosterKey(BoosterType type) => BoosterKeyPrefix + type;
    }
}
