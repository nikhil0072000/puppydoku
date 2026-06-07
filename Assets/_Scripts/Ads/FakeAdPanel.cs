using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppyPuzzle.Ads
{
    /// <summary>
    /// Stand-in for a real rewarded-ad view. Shows a fullscreen overlay with a
    /// short countdown; the Close/Claim button is disabled until the countdown
    /// finishes, then closing it grants the reward. Purely placeholder visuals —
    /// swap <see cref="DummyRewardedAdService"/> for a real SDK adapter later.
    /// </summary>
    public class FakeAdPanel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root object toggled on/off. If null, this GameObject is used.")]
        [SerializeField] private GameObject root;
        [Tooltip("Shows the countdown, then the 'Ad finished' prompt.")]
        [SerializeField] private TMP_Text countdownText;
        [Tooltip("Close/Claim button — disabled during the countdown, enabled when it ends.")]
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [Tooltip("Seconds the fake ad 'plays' before the reward can be claimed.")]
        [Min(0f)]
        [SerializeField] private float adDuration = 5f;

        private Action _onWatched;
        private Action _onSkipped;
        private Coroutine _countdownRoutine;

        private void Awake()
        {
            if (root == null) root = gameObject;
            root.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>Opens the fake ad. <paramref name="onWatched"/> fires when the
        /// countdown completes and the player closes; <paramref name="onSkipped"/>
        /// is reserved for an early-exit path (not wired by default).</summary>
        public void Show(Action onWatched, Action onSkipped)
        {
            _onWatched = onWatched;
            _onSkipped = onSkipped;

            root.SetActive(true);
            if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);
            _countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            if (closeButton != null) closeButton.interactable = false;

            float remaining = adDuration;
            while (remaining > 0f)
            {
                if (countdownText != null)
                    countdownText.text = $"Ad playing… {Mathf.CeilToInt(remaining)}s";
                remaining -= Time.unscaledDeltaTime; // works even if gameplay is paused
                yield return null;
            }

            if (countdownText != null) countdownText.text = "Tap to claim your reward!";
            if (closeButton != null) closeButton.interactable = true;
            _countdownRoutine = null;
        }

        private void OnCloseClicked()
        {
            // Guard: ignore clicks while the countdown is still running.
            if (_countdownRoutine != null) return;

            root.SetActive(false);
            Action cb = _onWatched;
            _onWatched = null;
            _onSkipped = null;
            cb?.Invoke();
        }
    }
}
