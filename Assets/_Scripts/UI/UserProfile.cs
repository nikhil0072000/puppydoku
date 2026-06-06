using PuppyPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Profile + wallet widget (e.g. on the Home scene header): shows the saved
/// player name, profile icon, and current coin/pawgem balances. Refreshes on
/// enable and live via <see cref="ProfileStore.OnProfileChanged"/> and
/// <see cref="EconomyHandler.OnCurrencyChanged"/>. The profile button opens
/// the ProfilePopup.
/// </summary>
public class UserProfile : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Opens the ProfilePopup.")]
    [SerializeField] private Button profileBtn;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI coinsTxt;
    [SerializeField] private TextMeshProUGUI pawGemsTxt;

    /// <summary>Icon set comes exclusively from PopupCanvasManager (single source).</summary>
    private ProfileIconsSO Icons =>
        PopupCanvasManager.Instance != null ? PopupCanvasManager.Instance.ProfileIcons : null;

    private void Awake()
    {
        if (profileBtn != null)
            profileBtn.onClick.AddListener(OnProfileClicked);
    }

    private void OnDestroy()
    {
        if (profileBtn != null)
            profileBtn.onClick.RemoveListener(OnProfileClicked);
    }

    private void OnEnable()
    {
        ProfileStore.OnProfileChanged += Refresh;
        EconomyHandler.OnCurrencyChanged += OnCurrencyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        ProfileStore.OnProfileChanged -= Refresh;
        EconomyHandler.OnCurrencyChanged -= OnCurrencyChanged;
    }

    private void Start()
    {
        // OnEnable can fire before PopupCanvasManager.Awake during the same scene
        // load; by Start all Awakes have run, so the icon SO is resolvable.
        Refresh();
    }

    /// <summary>Loads the saved name, icon, and currency balances into the widget.</summary>
    public void Refresh()
    {
        if (nameText != null)
            nameText.text = ProfileStore.PlayerName;

        if (icon != null)
        {
            ProfileIconsSO icons = Icons;
            Sprite sprite = icons != null ? icons.GetIcon(ProfileStore.IconIndex) : null;
            if (sprite != null)
                icon.sprite = sprite;
        }

        if (coinsTxt != null)
            coinsTxt.text = EconomyHandler.Coins.ToString();

        if (pawGemsTxt != null)
            pawGemsTxt.text = EconomyHandler.PawGems.ToString();
    }

    private void OnCurrencyChanged(CurrencyType type, int newBalance)
    {
        switch (type)
        {
            case CurrencyType.Coins:
                if (coinsTxt != null) coinsTxt.text = newBalance.ToString();
                break;
            case CurrencyType.PawGems:
                if (pawGemsTxt != null) pawGemsTxt.text = newBalance.ToString();
                break;
        }
    }

    private void OnProfileClicked()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.ButtonClick);

        if (PopupCanvasManager.Instance != null)
            PopupCanvasManager.Instance.Show(PopupType.ProfilePopup);
        else
            Debug.LogWarning("[UserProfile] No PopupCanvasManager in scene to show ProfilePopup.");
    }
}
