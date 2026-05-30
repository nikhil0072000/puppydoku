using DG.Tweening;
using UnityEngine;

/// <summary>
/// Drives the Home scene's three-tab navigation (Home / Shop / DailyChallenge).
/// Lives on the same GameObject as <see cref="HomeSceneUI"/> (MainMenuCanvas).
///
/// Each panel is full-screen-stretched and parked at an off-screen rest position in
/// the scene (captured at Awake). Selecting a tab slides its panel to centre (0,0)
/// via DOTween, focuses that tab, and deactivates the other two panels.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    public enum PanelState
    {
        Home,
        Shop,
        DailyChallenge
    }

    [Header("Tab Buttons")]
    [SerializeField] private HomeTabButton homeBtn;
    [SerializeField] private HomeTabButton shopBtn;
    [SerializeField] private HomeTabButton dailyChallengeBtn;

    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject dailyChallengePanel;

    [Header("Slide Animation")]
    [Min(0f)]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    /// <summary>The panel currently shown.</summary>
    public PanelState CurrentPanel { get; private set; }

    private RectTransform _homeRect;
    private RectTransform _shopRect;
    private RectTransform _dailyRect;

    private Vector2 _homeOffset;
    private Vector2 _shopOffset;
    private Vector2 _dailyOffset;

    private void Awake()
    {
        CacheRect(homePanel, ref _homeRect, ref _homeOffset);
        CacheRect(shopPanel, ref _shopRect, ref _shopOffset);
        CacheRect(dailyChallengePanel, ref _dailyRect, ref _dailyOffset);
    }

    private void OnEnable()
    {
        if (homeBtn != null) homeBtn.Button.onClick.AddListener(OnHomeTab);
        if (shopBtn != null) shopBtn.Button.onClick.AddListener(OnShopTab);
        if (dailyChallengeBtn != null) dailyChallengeBtn.Button.onClick.AddListener(OnDailyTab);
    }

    private void OnDisable()
    {
        if (homeBtn != null) homeBtn.Button.onClick.RemoveListener(OnHomeTab);
        if (shopBtn != null) shopBtn.Button.onClick.RemoveListener(OnShopTab);
        if (dailyChallengeBtn != null) dailyChallengeBtn.Button.onClick.RemoveListener(OnDailyTab);
    }

    private void Start()
    {
        // Home is the default panel; apply instantly (no slide) at scene start.
        SetPanel(PanelState.Home, instant: true);
    }

    private void OnHomeTab() => SetPanel(PanelState.Home);
    private void OnShopTab() => SetPanel(PanelState.Shop);
    private void OnDailyTab() => SetPanel(PanelState.DailyChallenge);

    /// <summary>Navigates to <paramref name="target"/>: focuses its tab, slides its panel
    /// to centre and deactivates the others. Re-selecting the active tab is a no-op.</summary>
    public void SetPanel(PanelState target, bool instant = false)
    {
        if (!instant && target == CurrentPanel)
            return;

        // Focus the chosen tab; all others go to their normal state — all at once.
        if (homeBtn != null) homeBtn.SetFocused(target == PanelState.Home);
        if (shopBtn != null) shopBtn.SetFocused(target == PanelState.Shop);
        if (dailyChallengeBtn != null) dailyChallengeBtn.SetFocused(target == PanelState.DailyChallenge);

        // Activate the incoming panel FIRST so it's on-screen before anything moves —
        // then slide. The outgoing panel slides off to reveal what's underneath.
        ShowPanel(target, instant);

        if (target != PanelState.Home)
            HidePanel(PanelState.Home, instant);
        if (target != PanelState.Shop)
            HidePanel(PanelState.Shop, instant);
        if (target != PanelState.DailyChallenge)
            HidePanel(PanelState.DailyChallenge, instant);

        CurrentPanel = target;
    }

    private static void CacheRect(GameObject panel, ref RectTransform rect, ref Vector2 offset)
    {
        if (panel == null)
            return;

        rect = panel.GetComponent<RectTransform>();
        if (rect != null)
            offset = rect.anchoredPosition; // scene-authored off-screen rest position
    }

    /// <summary>Activates the panel and slides it to centre. Hierarchy order is left
    /// untouched — set the panels' sibling order in the scene as you want them layered.</summary>
    private void ShowPanel(PanelState state, bool instant)
    {
        GameObject panel = PanelFor(state);
        RectTransform rect = RectFor(state);
        if (panel == null || rect == null)
            return;

        rect.DOKill();
        panel.SetActive(true);

        if (instant)
        {
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            // Slide from wherever it currently sits (its rest offset, or a killed
            // mid-slide position) to centre — no snap, so reversals stay smooth.
            rect.DOAnchorPos(Vector2.zero, slideDuration)
                .SetEase(slideEase)
                .SetLink(panel, LinkBehaviour.KillOnDestroy);
        }
    }

    /// <summary>Slides the panel out to its rest offset, then deactivates it. Instant
    /// path deactivates immediately. No-op if the panel is already hidden.</summary>
    private void HidePanel(PanelState state, bool instant)
    {
        GameObject panel = PanelFor(state);
        RectTransform rect = RectFor(state);
        if (panel == null || rect == null || !panel.activeSelf)
            return;

        rect.DOKill();
        Vector2 offset = OffsetFor(state);

        if (instant)
        {
            rect.anchoredPosition = offset;
            panel.SetActive(false);
        }
        else
        {
            rect.DOAnchorPos(offset, slideDuration)
                .SetEase(slideEase)
                .SetLink(panel, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => panel.SetActive(false));
        }
    }

    private GameObject PanelFor(PanelState state)
    {
        switch (state)
        {
            case PanelState.Shop: return shopPanel;
            case PanelState.DailyChallenge: return dailyChallengePanel;
            default: return homePanel;
        }
    }

    private RectTransform RectFor(PanelState state)
    {
        switch (state)
        {
            case PanelState.Shop: return _shopRect;
            case PanelState.DailyChallenge: return _dailyRect;
            default: return _homeRect;
        }
    }

    private Vector2 OffsetFor(PanelState state)
    {
        switch (state)
        {
            case PanelState.Shop: return _shopOffset;
            case PanelState.DailyChallenge: return _dailyOffset;
            default: return _homeOffset;
        }
    }
}
