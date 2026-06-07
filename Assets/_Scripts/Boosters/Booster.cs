using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Base for a triggerable booster (lives on its bottom-bar button GameObject).
    /// Owns the UI pieces for every state; tuning (unlock level, coin price) comes
    /// from the <see cref="BoosterConfigSO"/> assigned to the BoosterController.
    /// <see cref="BoosterUIController"/> drives the visuals;
    /// <see cref="BoosterController"/> runs the use/spend/ad flow and calls
    /// <see cref="TryActivate"/>. Subclasses reference gameplay systems directly.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public abstract class Booster : MonoBehaviour
    {
        [Header("UI - Icon")]
        [SerializeField] private Image icn;

        [Header("UI - Count state")]
        [Tooltip("Badge shown while the player still has boosters (holds the count text).")]
        [SerializeField] private GameObject countHolder;
        [SerializeField] private TMP_Text boosterCountText;

        [Header("UI - Coins state (count == 0, affordable)")]
        [Tooltip("Badge shown when out of boosters but the coin price is affordable.")]
        [SerializeField] private GameObject coinsHolder;
        [SerializeField] private TMP_Text boosterCoinsPriceText;

        [Header("UI - Ad state (count == 0, coins short)")]
        [Tooltip("Shown when out of boosters AND out of coins — tap to watch a rewarded ad.")]
        [SerializeField] private GameObject adIcon;

        [Header("UI - Lock state")]
        [Tooltip("Whole locked look (e.g. greyed icon + padlock). Active while locked.")]
        [SerializeField] private GameObject lockState;
        [Tooltip("Shows the level number that unlocks this booster.")]
        [SerializeField] private TMP_Text boosterUnlockNumberText;
        [Tooltip("Whole unlocked look. Active while unlocked.")]
        [SerializeField] private GameObject unlockState;

        private Button _button;

        public abstract BoosterType Type { get; }

        /// <summary>Unlock level from the BoosterConfigSO (via BoosterController).</summary>
        public int UnlockAtLevel => BoosterController.Instance != null
            ? BoosterController.Instance.GetUnlockLevel(Type)
            : BoosterConfigSO.FallbackUnlockLevel;

        /// <summary>Coin price from the BoosterConfigSO (via BoosterController).</summary>
        public int CoinPrice => BoosterController.Instance != null
            ? BoosterController.Instance.GetCoinPrice(Type)
            : BoosterConfigSO.FallbackCoinPrice;
        public Image Icon => icn;
        public Button Button => _button != null ? _button : (_button = GetComponent<Button>());

        /// <summary>Runs the booster. Returns false if it could not act (caller refunds the cost).</summary>
        public abstract bool TryActivate();

        /// <summary>
        /// Applies a full visual state. Exactly one of count/coins/ad shows while
        /// unlocked; the lock visuals replace them all while locked.
        /// </summary>
        public void RefreshVisuals(bool unlocked, int count, bool canAffordCoins)
        {
            if (lockState != null) lockState.SetActive(!unlocked);
            if (unlockState != null) unlockState.SetActive(unlocked);
            if (boosterUnlockNumberText != null) boosterUnlockNumberText.text = UnlockAtLevel.ToString();
            Button.interactable = unlocked;

            bool showCount = unlocked && count > 0;
            bool showCoins = unlocked && count <= 0 && canAffordCoins;
            bool showAd = unlocked && count <= 0 && !canAffordCoins;

            if (countHolder != null) countHolder.SetActive(showCount);
            if (boosterCountText != null && showCount) boosterCountText.text = count.ToString();

            if (coinsHolder != null) coinsHolder.SetActive(showCoins);
            if (boosterCoinsPriceText != null && showCoins) boosterCoinsPriceText.text = CoinPrice.ToString();

            if (adIcon != null) adIcon.SetActive(showAd);
        }
    }
}
