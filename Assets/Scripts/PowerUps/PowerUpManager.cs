using System;
using UnityEngine;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Tracks how many chances remain for each power-up and persists them across
    /// sessions via PlayerPrefs. Singleton that survives scene loads, so it should
    /// live in the first-loaded (bootstrap/Loading) scene.
    ///
    /// Chances default to <see cref="DefaultChances"/> the first time the game runs.
    /// Consuming decrements; watching the rewarded ad grants one back.
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

        private const string KeyPrefix = "powerup_chances_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Remaining chances for a power-up (initialised to default on first read).</summary>
        public int GetChances(PowerUpType type)
        {
            string key = KeyFor(type);
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, defaultChances);
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetInt(key, defaultChances);
        }

        public bool HasChance(PowerUpType type) => GetChances(type) > 0;

        /// <summary>Spends one chance. Returns false (and changes nothing) if none remain.</summary>
        public bool TryConsume(PowerUpType type)
        {
            int current = GetChances(type);
            if (current <= 0) return false;
            SetChances(type, current - 1);
            return true;
        }

        /// <summary>Adds one chance — used by the rewarded-ad reward and by hint refunds.</summary>
        public void GrantChance(PowerUpType type)
        {
            SetChances(type, GetChances(type) + 1);
        }

        private void SetChances(PowerUpType type, int value)
        {
            value = Mathf.Max(0, value);
            PlayerPrefs.SetInt(KeyFor(type), value);
            PlayerPrefs.Save();
            OnChancesChanged?.Invoke(type, value);
#if UNITY_EDITOR
            Debug.Log($"[PowerUpManager] {type} chances = {value}", this);
#endif
        }

        private static string KeyFor(PowerUpType type) => KeyPrefix + type;
    }
}
