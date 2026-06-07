using System;
using UnityEngine;

namespace PuppyPuzzle.Ads
{
    /// <summary>
    /// Abstraction over a rewarded-ad provider so the real SDK (Unity Ads / AdMob)
    /// can be dropped in later without touching booster code. Callers get a reward
    /// callback on a completed watch and a skip callback if the player bailed.
    /// </summary>
    public interface IRewardedAdService
    {
        void ShowRewardedAd(Action onReward, Action onSkip);
    }

    /// <summary>
    /// Placeholder ad provider used until a real SDK is integrated. Drives a simple
    /// <see cref="FakeAdPanel"/> (a fullscreen "Ad playing…" overlay). When the panel
    /// closes after its countdown, the reward is granted. Singleton so any caller
    /// (boosters, revive, shop) can reach it via <see cref="Instance"/>.
    /// </summary>
    public class DummyRewardedAdService : MonoBehaviour, IRewardedAdService
    {
        public static DummyRewardedAdService Instance { get; private set; }

        [Tooltip("Fake fullscreen ad overlay. If left null, the reward is granted immediately with only a console log.")]
        [SerializeField] private FakeAdPanel adPanel;

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

        public void ShowRewardedAd(Action onReward, Action onSkip)
        {
            Debug.Log("[Ad] Rewarded ad requested (dummy provider).");

            if (adPanel == null)
            {
                // No panel wired — behave like an instant-complete ad.
                Debug.Log("[Ad] No FakeAdPanel assigned; granting reward immediately.");
                onReward?.Invoke();
                return;
            }

            adPanel.Show(
                onWatched: () =>
                {
                    Debug.Log("[Ad] Rewarded ad watched → granting reward.");
                    onReward?.Invoke();
                },
                onSkipped: () =>
                {
                    Debug.Log("[Ad] Rewarded ad skipped → no reward.");
                    onSkip?.Invoke();
                });
        }
    }
}
