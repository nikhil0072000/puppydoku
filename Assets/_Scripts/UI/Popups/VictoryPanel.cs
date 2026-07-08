using DG.Tweening;
using PuppyPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Victory popup for Tutorial/Normal level wins (daily challenges use
/// <see cref="DailyChallengeVictoryPanel"/>). Shows the completed level's number,
/// the win reward, and a three-star fly-in animation; a single Next button grants
/// the reward and continues the progression.
///
/// Star hierarchy: three base sockets (Star1..3, always visible) and three fill
/// stars (Star1Fill..3Fill, inactive by default). The fills are AUTHORED at their
/// spawn spots (scattered around the panel); when the popup opens each one pops in
/// there, hops onto its socket in a playful arc, and snaps with a punch — all
/// within <see cref="starsDuration"/>. Every win is three stars for now —
/// move-based star ratings come later.
/// </summary>
public class VictoryPanel : Popup
{
    [Header("Level")]
    [Tooltip("Displays the completed level's number, e.g. \"3\".")]
    [SerializeField] private TextMeshProUGUI levelNumberTxt;

    [Header("Reward")]
    [Tooltip("Displays the reward value for the level win.")]
    [SerializeField] private TextMeshProUGUI rewardsTxt;
    [Tooltip("Flat reward per level win for now; later driven by the level JSON.")]
    [Min(0)]
    [SerializeField] private int defaultRewardAmount = 20;

    [Header("Buttons")]
    [Tooltip("Grants the reward and loads the next level in the progression.")]
    [SerializeField] private Button nextButton;

    [Header("Stars")]
    [Tooltip("Base (empty) star sockets, left to right. Always visible.")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;
    [Tooltip("Fill stars, inactive by default, authored at their SPAWN positions — they fly onto their socket when the popup opens.")]
    [SerializeField] private GameObject star1Fill;
    [SerializeField] private GameObject star2Fill;
    [SerializeField] private GameObject star3Fill;

    [Header("Stars Animation")]
    [Tooltip("Total time for all three fill stars to pop in, fly to their socket and snap.")]
    [Min(0.1f)]
    [SerializeField] private float starsDuration = 1.5f;
    [Tooltip("Height of the hop arc the fill stars travel on, in canvas units.")]
    [Min(0f)]
    [SerializeField] private float starJumpPower = 120f;

    private GameObject[] _fills;
    private GameObject[] _bases;
    private Vector3[] _fillSpawnPositions;
    private Vector3[] _fillSpawnScales;
    private Vector3[] _fillSpawnEulers;
    private Sequence _starsSequence;
    private bool _rewardGranted; // one grant per win, even if Next is spammed

    protected override void Awake()
    {
        base.Awake();
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        _bases = new[] { star1, star2, star3 };
        _fills = new[] { star1Fill, star2Fill, star3Fill };
        _fillSpawnPositions = new Vector3[_fills.Length];
        _fillSpawnScales = new Vector3[_fills.Length];
        _fillSpawnEulers = new Vector3[_fills.Length];
        for (int i = 0; i < _fills.Length; i++)
        {
            if (_fills[i] == null) continue;
            Transform fillTransform = _fills[i].transform;
            _fillSpawnPositions[i] = fillTransform.localPosition;
            _fillSpawnScales[i] = fillTransform.localScale;
            _fillSpawnEulers[i] = fillTransform.localEulerAngles;
            _fills[i].SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
        _starsSequence?.Kill();
    }

    protected override void OnOpened()
    {
        if (levelNumberTxt != null)
        {
            levelNumberTxt.text = LevelLoader.Instance != null
                ? LevelLoader.Instance.CurrentNormalLevelNumber.ToString()
                : "1";
        }

        if (rewardsTxt != null)
            rewardsTxt.text = defaultRewardAmount.ToString();

        _rewardGranted = false;
        PlayStarsAnimation();
    }

    protected override void OnClosed()
    {
        ResetStars(); // cached instance — arm the animation for the next win
    }

    // ---- Stars ----

    private void PlayStarsAnimation()
    {
        ResetStars();

        int starCount = 0;
        foreach (GameObject fill in _fills)
            if (fill != null) starCount++;
        if (starCount == 0) return;

        // Per-star slot: quick pop at the spawn spot, hop onto the socket, snap.
        float slot = starsDuration / starCount;
        float popDuration = slot * 0.2f;
        float flyDuration = slot * 0.5f;
        float snapDuration = slot * 0.3f;

        _starsSequence = DOTween.Sequence();
        _starsSequence.SetUpdate(true); // unscaled — matches the popup's own tweens
        _starsSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        for (int i = 0; i < _fills.Length; i++)
        {
            GameObject fill = _fills[i];
            if (fill == null) continue;

            GameObject baseStar = i < _bases.Length ? _bases[i] : null;
            if (baseStar == null) continue; // no socket to fly to

            Transform fillTransform = fill.transform;
            Transform socket = baseStar.transform;
            Vector3 spawnScale = _fillSpawnScales[i];

            // Pop in at the authored spawn spot.
            _starsSequence.AppendCallback(() =>
            {
                fill.SetActive(true);
                fillTransform.localScale = Vector3.zero;
            });
            _starsSequence.Append(fillTransform.DOScale(spawnScale, popDuration).SetEase(Ease.OutBack));

            // Hop onto the socket in an arc, straightening on the way.
            // World-space targets: fills and sockets may live under different parents.
            _starsSequence.Append(fillTransform.DOJump(socket.position, starJumpPower, 1, flyDuration)
                                               .SetEase(Ease.Linear));
            _starsSequence.Join(fillTransform.DORotateQuaternion(socket.rotation, flyDuration)
                                             .SetEase(Ease.OutQuad));

            // Jump snap: playful overshoot on the fill plus a kick on the socket under it.
            _starsSequence.Append(fillTransform.DOPunchScale(spawnScale * 0.35f, snapDuration, 8, 0.6f));
            _starsSequence.Join(socket.DOPunchScale(socket.localScale * 0.15f, snapDuration, 6, 0.5f));
        }
    }

    private void ResetStars()
    {
        _starsSequence?.Kill();
        _starsSequence = null;

        for (int i = 0; i < _fills.Length; i++)
        {
            GameObject fill = _fills[i];
            if (fill == null) continue;

            fill.SetActive(false);
            Transform fillTransform = fill.transform;
            fillTransform.localPosition = _fillSpawnPositions[i];
            fillTransform.localScale = _fillSpawnScales[i];
            fillTransform.localEulerAngles = _fillSpawnEulers[i];
        }
    }

    // ---- Buttons ----

    private void OnNextClicked()
    {
        PlayClick();
        if (LevelLoader.Instance == null)
        {
            Debug.LogError("LevelLoader instance not found!");
            return;
        }

        // Claim the win reward exactly once, then move on.
        if (!_rewardGranted)
        {
            _rewardGranted = true;
            EconomyHandler.AddCoins(defaultRewardAmount);
        }

        // The popup canvas persists across scenes — close before leaving.
        RequestClose();

        if (LevelLoader.Instance.CurrentLevelType == LevelType.Tutorial)
        {
            // A completed tutorial already moved the progression pointer to the
            // first normal level — load it directly instead of skipping past it.
            LevelLoader.Instance.LoadCurrentLevel();
        }
        else if (LevelLoader.Instance.HasNextLevel)
        {
            LevelLoader.Instance.LoadNextLevel();
        }
        else
        {
            // Last authored level cleared — nothing to advance to, go home.
            SceneManager.LoadScene("Home");
        }
    }
}
