using PuppyPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// All Game-scene UI visuals and operations (replaces the old HUDManager):
/// level label, progress, daily timer, currency bar (live coins/gems + '+'
/// buttons into the Shop popup), settings button, and the life counter via
/// <see cref="GameLivesManager"/>. GameManager pushes gameplay state
/// (progress, lives, labels) through the public methods.
/// </summary>
public class GameSceneUI : MonoBehaviour
{
    [Header("Level")]
    [Tooltip("Current level label, e.g. \"Level 3\" / \"Daily 2\".")]
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Daily Timer")]
    [Tooltip("Whole timer widget (icon + text). Active only during a daily challenge level.")]
    [SerializeField] private GameObject dailyTimerVisual;
    [SerializeField] private TextMeshProUGUI dailyTimerText;

    [Header("Lives")]
    [SerializeField] private GameLivesManager livesManager;

    [Header("Currency Bar")]
    [SerializeField] private TextMeshProUGUI coinsTxt;
    [SerializeField] private TextMeshProUGUI gemsTxt;
    [Tooltip("The '+' next to the coin balance — opens the Shop popup.")]
    [SerializeField] private Button coinsPlusBtn;
    [Tooltip("The '+' next to the pawgem balance — opens the Shop popup.")]
    [SerializeField] private Button gemsPlusBtn;

    [Header("Settings")]
    [Tooltip("Opens the Settings popup.")]
    [SerializeField] private Button settingsBtn;

    [Header("Game Rules")]
    [Tooltip("Rules bar under the HUD — blinks the broken rule's outline on a wrong move.")]
    [SerializeField] private GameRulesHandler rulesHandler;

    // Last MM:SS pushed to the timer label, so we only format + reassign on a real change
    // (UpdateDailyTimer is called every frame from GameManager.Update).
    private int _lastTimerMinutes = -1;
    private int _lastTimerSeconds = -1;

    /// <summary>Life counter — gameplay calls SpendLife/SetLives through this.</summary>
    public GameLivesManager Lives => livesManager;

    private void OnEnable()
    {
        if (coinsPlusBtn != null) coinsPlusBtn.onClick.AddListener(OnOpenShopClicked);
        if (gemsPlusBtn != null) gemsPlusBtn.onClick.AddListener(OnOpenShopClicked);
        if (settingsBtn != null) settingsBtn.onClick.AddListener(OnSettingsClicked);

        EconomyHandler.OnCurrencyChanged += HandleCurrencyChanged;

        RefreshCurrencies();
    }

    private void OnDisable()
    {
        if (coinsPlusBtn != null) coinsPlusBtn.onClick.RemoveListener(OnOpenShopClicked);
        if (gemsPlusBtn != null) gemsPlusBtn.onClick.RemoveListener(OnOpenShopClicked);
        if (settingsBtn != null) settingsBtn.onClick.RemoveListener(OnSettingsClicked);

        EconomyHandler.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    // ---- Currency bar ----

    private void RefreshCurrencies()
    {
        if (coinsTxt != null) coinsTxt.text = EconomyHandler.Coins.ToString();
        if (gemsTxt != null) gemsTxt.text = EconomyHandler.PawGems.ToString();
    }

    private void HandleCurrencyChanged(CurrencyType type, int newBalance)
    {
        switch (type)
        {
            case CurrencyType.Coins:
                if (coinsTxt != null) coinsTxt.text = newBalance.ToString();
                break;
            case CurrencyType.PawGems:
                if (gemsTxt != null) gemsTxt.text = newBalance.ToString();
                break;
        }
    }

    // ---- Buttons ----

    private void OnOpenShopClicked()
    {
        PlayClick();
        if (PopupCanvasManager.Instance != null)
            PopupCanvasManager.Instance.Show(PopupType.ShopPanel);
        else
            Debug.LogError("PopupCanvasManager instance not found!", this);
    }

    private void OnSettingsClicked()
    {
        PlayClick();
        if (PopupCanvasManager.Instance != null)
            PopupCanvasManager.Instance.Show(PopupType.Settings);
        else
            Debug.LogError("PopupCanvasManager instance not found!", this);
    }

    private static void PlayClick()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.ButtonClick);
    }

    // ---- Gameplay-driven state (called by GameManager) ----

    /// <summary>Syncs the life visuals to an absolute count (level load / revive).</summary>
    public void SetLives(int currentLives)
    {
        if (livesManager != null)
            livesManager.SetLives(currentLives);
    }

    /// <summary>Marks one life as lost.</summary>
    public void SpendLife()
    {
        if (livesManager != null)
            livesManager.SpendLife();
    }

    /// <summary>Blinks the rules-bar tab for the rule a wrong move broke.</summary>
    public void FlashRuleViolation(GameRuleType rule)
    {
        if (rulesHandler != null)
            rulesHandler.FlashViolatedRule(rule);
    }

    public void UpdateProgress(int placed, int total)
    {
        if (progressText != null)
            progressText.text = $"{placed}/{total}";
    }

    public void SetLevelLabel(string label)
    {
        if (levelNumberText != null)
            levelNumberText.text = label;
    }

    public void UpdateDailyTimer(float elapsedSeconds)
    {
        if (dailyTimerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);

        // Skip the string allocation + TMP mesh rebuild when the displayed value is unchanged.
        if (minutes == _lastTimerMinutes && seconds == _lastTimerSeconds)
            return;

        _lastTimerMinutes = minutes;
        _lastTimerSeconds = seconds;
        dailyTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>Shows the daily timer widget on daily challenge levels, hides it otherwise.</summary>
    public void SetDailyTimerVisible(bool visible)
    {
        if (dailyTimerVisual != null)
            dailyTimerVisual.SetActive(visible);
    }
}
