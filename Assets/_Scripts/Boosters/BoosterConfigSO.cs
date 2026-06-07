using System;
using UnityEngine;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Designer-authored booster tuning: per <see cref="BoosterType"/>, the Normal
    /// level at which it unlocks and the coin price for a single use when the player
    /// is out of boosters. Assigned to <see cref="BoosterController"/>; boosters read
    /// their values through it. Mirrors the lookup style of SFXConfig/PopupsConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "BoosterConfig", menuName = "PuppyPuzzle/Booster Config")]
    public class BoosterConfigSO : ScriptableObject
    {
        [Serializable]
        public class BoosterEntry
        {
            public BoosterType type;

            [Tooltip("Normal level number at which this booster unlocks (1 = available from the start).")]
            [Min(1)] public int unlockAtLevel = 1;

            [Tooltip("Coin price to use the booster once when the player has no booster count left.")]
            [Min(0)] public int coinPrice = 100;
        }

        public const int FallbackUnlockLevel = 1;
        public const int FallbackCoinPrice = 100;

        [SerializeField] private BoosterEntry[] entries;

        public bool TryGetEntry(BoosterType type, out BoosterEntry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && entries[i].type == type)
                    {
                        entry = entries[i];
                        return true;
                    }
                }
            }
            entry = null;
            return false;
        }

        public int GetUnlockLevel(BoosterType type) =>
            TryGetEntry(type, out BoosterEntry entry) ? entry.unlockAtLevel : FallbackUnlockLevel;

        public int GetCoinPrice(BoosterType type) =>
            TryGetEntry(type, out BoosterEntry entry) ? entry.coinPrice : FallbackCoinPrice;
    }
}
