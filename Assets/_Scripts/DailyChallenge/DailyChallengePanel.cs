using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Daily Challenge tab (Home scene). Shows today's daily level number
/// and gem reward; Play launches it via <see cref="LevelLoader.LoadDailyChallenge"/>.
///
/// Before completion: rewardsBanner is shown. After today's challenge is completed:
/// rewardsBanner hides, timerBanner shows a live countdown to the next UTC reset
/// (format 00h00m00sec). When the countdown hits zero the state rolls over and the
/// rewards banner returns with the next level.
/// </summary>
public class DailyChallengePanel : MonoBehaviour
{
    [Header("Labels")]
    [Tooltip("Shows today's daily level number, e.g. \"3\".")]
    [SerializeField] private TextMeshProUGUI dailyChallengeLevelNumber;
    [Tooltip("Shows the PawGem reward for completing today's challenge.")]
    [SerializeField] private TextMeshProUGUI rewardsGemCount;
    [SerializeField] private TextMeshProUGUI timerTxt;

    [Header("Buttons")]
    [SerializeField] private Button playBtn;

    [Header("Banners")]
    [Tooltip("Visible while today's challenge is still unplayed — shows the gem reward.")]
    [SerializeField] private GameObject rewardsBanner;
    [Tooltip("Visible after completion — shows the countdown to the next challenge.")]
    [SerializeField] private GameObject timerBanner;

    private bool countdownRunning;
    private int lastShownSecond = -1;

    private void OnEnable()
    {
        if (playBtn != null)
            playBtn.onClick.AddListener(OnPlayClicked);

        Refresh();
    }

    private void OnDisable()
    {
        if (playBtn != null)
            playBtn.onClick.RemoveListener(OnPlayClicked);
    }

    private void Update()
    {
        if (!countdownRunning)
            return;

        float remaining = TimerUtils.SecondsUntilUtcReset;

        // Rebuild the string only when the displayed second changes (no per-frame alloc).
        int second = Mathf.FloorToInt(remaining);
        if (second != lastShownSecond)
        {
            lastShownSecond = second;
            if (timerTxt != null)
                timerTxt.text = TimerUtils.FormatCountdown(remaining);
        }

        // UTC midnight passed — roll the state over and bring the rewards banner back.
        if (remaining <= 0f)
            Refresh();
    }

    /// <summary>Re-reads daily state and updates labels, banners and the Play button.</summary>
    public void Refresh()
    {
        DailyChallengeManager dcm = DailyChallengeManager.Instance;
        if (dcm == null)
        {
            Debug.LogError("DailyChallengePanel: DailyChallengeManager not found.", this);
            return;
        }

        dcm.EnsureDailyState();

        if (dailyChallengeLevelNumber != null)
            dailyChallengeLevelNumber.text = dcm.CurrentDailyLevelNumber.ToString();

        if (rewardsGemCount != null)
            rewardsGemCount.text = dcm.CurrentGemReward.ToString();

        bool completed = dcm.HasCompletedToday;

        if (rewardsBanner != null) rewardsBanner.SetActive(!completed);
        if (timerBanner != null) timerBanner.SetActive(completed);
        if (playBtn != null) playBtn.interactable = !completed;

        countdownRunning = completed;
        lastShownSecond = -1; // force a countdown repaint next frame
    }

    private void OnPlayClicked()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.ButtonClick);

        DailyChallengeManager dcm = DailyChallengeManager.Instance;
        if (dcm == null || !dcm.DailyAvailable)
            return;

        if (LevelLoader.Instance == null)
        {
            Debug.LogError("DailyChallengePanel: LevelLoader not found.", this);
            return;
        }

        LevelLoader.Instance.LoadDailyChallenge();
    }
}
