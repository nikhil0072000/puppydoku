using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Bottom-bar button for a single power-up. Shows a green count badge (top-right):
    /// the remaining chances, or a "+" when empty. Tapping with chances spends one and
    /// runs the power-up (refunding if it couldn't act); tapping at zero opens the
    /// rewarded-ad flow, which grants one chance back.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PowerUpButton : MonoBehaviour
    {
        [Header("Power-Up")]
        [Tooltip("The power-up this button triggers (PuppyRevealPowerUp or HintController).")]
        [SerializeField] private PowerUpBase powerUp;

        [Header("Badge")]
        [Tooltip("Text inside the green count circle. Shows the number, or '+' when empty.")]
        [SerializeField] private TMP_Text countText;
        [Tooltip("Glyph shown when no chances remain (tap = watch ad).")]
        [SerializeField] private string emptyGlyph = "+";

        [Header("Feedback")]
        [SerializeField] private float pressPunchScale = 0.12f;
        [SerializeField] private float pressPunchDuration = 0.2f;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnEnable()
        {
            if (PowerUpManager.Instance != null)
                PowerUpManager.Instance.OnChancesChanged += HandleChancesChanged;
            RefreshBadge();
        }

        private void OnDisable()
        {
            if (PowerUpManager.Instance != null)
                PowerUpManager.Instance.OnChancesChanged -= HandleChancesChanged;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void HandleChancesChanged(PowerUpType type, int newCount)
        {
            if (powerUp != null && type == powerUp.Type)
                RefreshBadge();
        }

        private void RefreshBadge()
        {
            if (countText == null || powerUp == null || PowerUpManager.Instance == null) return;
            int chances = PowerUpManager.Instance.GetChances(powerUp.Type);
            countText.text = chances > 0 ? chances.ToString() : emptyGlyph;
        }

        private void OnClicked()
        {
            PlayPunch();

            if (powerUp == null || PowerUpManager.Instance == null)
            {
                Debug.LogWarning("[PowerUpButton] Missing power-up or PowerUpManager reference.");
                return;
            }

            PowerUpType type = powerUp.Type;

            if (PowerUpManager.Instance.HasChance(type))
            {
                // Spend a chance optimistically; refund if the power-up couldn't act.
                if (!PowerUpManager.Instance.TryConsume(type)) return;

                bool acted = powerUp.TryActivate();
                if (!acted)
                {
                    PowerUpManager.Instance.GrantChance(type);
                    Debug.Log($"[PowerUpButton] {type} could not act right now — chance refunded.");
                }
            }
            else
            {
                // Out of chances → offer a rewarded ad for +1.
                if (DummyRewardedAdService.Instance != null)
                {
                    DummyRewardedAdService.Instance.ShowRewardedAd(
                        onReward: () => PowerUpManager.Instance.GrantChance(type),
                        onSkip: null);
                }
                else
                {
                    Debug.LogWarning("[PowerUpButton] No ad service in scene; cannot refill.");
                }
            }
        }

        private void PlayPunch()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * pressPunchScale, pressPunchDuration, 6, 0.5f)
                     .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
