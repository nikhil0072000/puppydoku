using System.Collections;
using DG.Tweening;
using GameVanilla.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Home panel: the scrollable level map, the current-level label, and the
/// animated Play button.
///
/// On load the map auto-scrolls so the current level is centred, then the Play button
/// pops in (scale 0→1). While the player scrolls the map, the Play button fades out and
/// is made non-interactive; when scrolling settles it fades back in. Pressing Play
/// re-centres on the current level, then launches it via <see cref="LevelLoader"/>.
///
/// Note: the level buttons self-manage their locked/current/played visuals (they read
/// the gamevanilla "next_level" PlayerPref in their own Start). To keep the centred
/// button identical to the one they highlight as "current", the scroll target is derived
/// from that same "next_level" value. The actual game launch goes through
/// <see cref="LevelLoader.LoadCurrentLevel"/> (the project's real load flow).
/// </summary>
public class HomeSceneUI : MonoBehaviour
{
    [Header("Play Button")]
    [SerializeField] private Button playButton;
    [Tooltip("CanvasGroup on the Play button — used to fade it in/out during scrolling.")]
    [SerializeField] private CanvasGroup playButtonGroup;
    [Tooltip("RectTransform on the Play button — used for the scale-in pop.")]
    [SerializeField] private RectTransform playButtonRect;

    [Header("Level Map")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("The ScrollRect's Content holding all the LevelMapButton instances.")]
    [SerializeField] private RectTransform content;

    [Header("Labels")]
    [Tooltip("Shows the current Normal level the Play button will load, e.g. \"Level 3\".")]
    [SerializeField] private TextMeshProUGUI levelLabelText;

    [Header("Tuning")]
    [Min(0f)][SerializeField] private float initialScrollDuration = 0.6f;
    [Min(0f)][SerializeField] private float playPopDuration = 0.15f;
    [Min(0f)][SerializeField] private float fadeDuration = 0.1f;
    [Tooltip("How long after the last scroll input (seconds) before the Play button fades back in.")]
    [Min(0f)][SerializeField] private float scrollStopDelay = 0.15f;
    [SerializeField] private Ease scrollEase = Ease.OutCubic;

    private LevelButton[] _levelButtons;
    private bool _isAutoScrolling; // true while a programmatic scroll is running (suppresses fade)
    private bool _userScrolling;   // true while the player is actively scrolling
    private bool _playHidden;      // true while the Play button is faded out
    private bool _isLaunching;     // guards against double Play taps
    private float _lastScrollTime;

    private void Awake()
    {
        if (content != null)
            _levelButtons = content.GetComponentsInChildren<LevelButton>(true);
    }

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScrollChanged);

        RefreshLevelLabel();
        HidePlayInstant();
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }

    private IEnumerator Start()
    {
        // Wait one frame so the layout/content size is final before measuring.
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;

        ScrollToCurrent(initialScrollDuration, ShowPlay);
    }

    private void Update()
    {
        if (!_userScrolling || _isAutoScrolling)
            return;

        // Settle purely on idle time since the last scroll callback. A velocity gate
        // proved unreliable (inertia could keep it from ever crossing the threshold),
        // which left the Play button faded out AND non-interactive — eating clicks.
        if (Time.unscaledTime - _lastScrollTime > scrollStopDelay)
        {
            _userScrolling = false;
            FadePlayIn();
        }
    }

    private void RefreshLevelLabel()
    {
        if (levelLabelText == null)
            return;

        levelLabelText.text = LevelLoader.Instance != null
            ? LevelLoader.Instance.CurrentLevelButtonLabel
            : "Level 1";
    }

    // ---- Play button ----

    public void OnPlayClicked()
    {
        if (_isLaunching)
            return;

        _isLaunching = true;
        FadePlayOut();

        ScrollToCurrent(initialScrollDuration, () =>
        {
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.LoadCurrentLevel();
            }
            else
            {
                Debug.LogError("LevelLoader instance not found!");
                _isLaunching = false; // allow another attempt
            }
        });
    }

    private void HidePlayInstant()
    {
        _playHidden = true;
        if (playButtonRect != null)
        {
            playButtonRect.DOKill();
            playButtonRect.localScale = Vector3.zero;
        }
        if (playButtonGroup != null)
        {
            playButtonGroup.DOKill();
            playButtonGroup.alpha = 0f;
            playButtonGroup.interactable = false;
            playButtonGroup.blocksRaycasts = false;
        }
    }

    private void ShowPlay()
    {
        _playHidden = false;
        if (playButtonRect != null)
        {
            playButtonRect.DOKill();
            playButtonRect.localScale = Vector3.zero;
            playButtonRect.DOScale(1f, playPopDuration).SetEase(Ease.OutBack)
                          .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
        if (playButtonGroup != null)
        {
            playButtonGroup.DOKill();
            playButtonGroup.DOFade(1f, playPopDuration)
                           .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            playButtonGroup.interactable = true;
            playButtonGroup.blocksRaycasts = true;
        }
    }

    private void FadePlayOut()
    {
        if (_playHidden || playButtonGroup == null)
            return;

        _playHidden = true;
        playButtonGroup.DOKill();
        playButtonGroup.interactable = false;
        playButtonGroup.blocksRaycasts = false;
        playButtonGroup.DOFade(0f, fadeDuration)
                       .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void FadePlayIn()
    {
        if (!_playHidden || playButtonGroup == null)
            return;

        _playHidden = false;
        playButtonGroup.DOKill();
        // Re-enable interactivity up front so the button is clickable the instant it
        // starts fading back in (no dead window mid-fade).
        playButtonGroup.interactable = true;
        playButtonGroup.blocksRaycasts = true;
        playButtonGroup.DOFade(1f, fadeDuration)
                       .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    // ---- Scrolling ----

    private void OnScrollChanged(Vector2 _)
    {
        if (_isAutoScrolling)
            return; // ignore our own programmatic scrolls

        _lastScrollTime = Time.unscaledTime;
        _userScrolling = true;
        FadePlayOut();
    }

    private void ScrollToCurrent(float duration, System.Action onComplete)
    {
        LevelButton target = FindCurrentButton();
        if (scrollRect == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }

        float targetPos = NormalizedForCentered((RectTransform)target.transform);
        _isAutoScrolling = true;
        scrollRect.DOKill();
        scrollRect.DOVerticalNormalizedPos(targetPos, duration)
                  .SetEase(scrollEase)
                  .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                  .OnComplete(() =>
                  {
                      _isAutoScrolling = false;
                      onComplete?.Invoke();
                  });
    }

    /// <summary>Vertical normalized position (1 = top, 0 = bottom) that centres
    /// <paramref name="child"/> in the viewport, clamped to the scrollable range.</summary>
    private float NormalizedForCentered(RectTransform child)
    {
        Canvas.ForceUpdateCanvases();

        float viewportH = scrollRect.viewport != null
            ? scrollRect.viewport.rect.height
            : ((RectTransform)scrollRect.transform).rect.height;
        float scrollable = content.rect.height - viewportH;
        if (scrollable <= 0f)
            return scrollRect.verticalNormalizedPosition;

        // Distance from the content's top edge down to the child's centre.
        float childYInContent = content.InverseTransformPoint(child.position).y;
        float distFromTop = content.rect.yMax - childYInContent;
        float targetTop = distFromTop - viewportH * 0.5f;

        return 1f - Mathf.Clamp01(targetTop / scrollable);
    }

    private LevelButton FindCurrentButton()
    {
        if (_levelButtons == null || _levelButtons.Length == 0)
            return null;

        int current = CurrentLevelNumber();
        LevelButton exact = null;
        LevelButton bestBelow = null;
        for (int i = 0; i < _levelButtons.Length; i++)
        {
            LevelButton b = _levelButtons[i];
            if (b == null)
                continue;
            if (b.numLevel == current)
            {
                exact = b;
                break;
            }
            if (b.numLevel < current && (bestBelow == null || b.numLevel > bestBelow.numLevel))
                bestBelow = b;
        }

        if (exact != null) return exact;
        if (bestBelow != null) return bestBelow;
        return _levelButtons[0];
    }

    /// <summary>The current level number, matching the gamevanilla LevelButton convention
    /// (PlayerPref "next_level", treating 0 as 1).</summary>
    private static int CurrentLevelNumber()
    {
        int n = PlayerPrefs.GetInt("next_level", 1);
        return n <= 0 ? 1 : n;
    }
}
