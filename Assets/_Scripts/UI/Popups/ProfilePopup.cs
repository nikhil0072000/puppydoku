using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Profile editing popup: rename (2–12 chars, live counter) and pick a profile
/// icon from a grid of <see cref="ProfileListing"/> entries built from
/// <see cref="ProfileIconsSO"/>. Done validates and persists through
/// <see cref="ProfileStore"/>; <see cref="UserProfile"/> widgets refresh via its event.
///
/// Icons source: the ProfileIconsSO assigned on PopupCanvasManager (single source).
/// </summary>
public class ProfilePopup : Popup
{
    [Header("Name")]
    [Tooltip("Input field for the player name. Character limit is enforced from ProfileStore.")]
    [SerializeField] private TMP_InputField profileName;

    [Tooltip("Pencil/edit button that focuses the name input field.")]
    [SerializeField] private Button nameEditButton;

    [Tooltip("Live counter, e.g. 3/12. Turns red while the name length is invalid.")]
    [SerializeField] private TextMeshProUGUI characterLimitText;

    [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Icons")]
    [Tooltip("Grid (with GridLayoutGroup) that receives the instantiated listings.")]
    [SerializeField] private Transform iconGrid;

    [SerializeField] private ProfileListing profileListingPrefab;

    [Header("Buttons")]
    [SerializeField] private Button doneButton;
    [SerializeField] private Button closeButton;

    private readonly List<ProfileListing> _listings = new List<ProfileListing>();
    private Color _validColor = Color.white;
    private int _selectedIconIndex;
    private bool _gridBuilt;

    /// <summary>Icon set comes exclusively from PopupCanvasManager (single source).</summary>
    private ProfileIconsSO Icons =>
        PopupCanvasManager.Instance != null ? PopupCanvasManager.Instance.ProfileIcons : null;

    protected override void Awake()
    {
        base.Awake();

        if (characterLimitText != null)
            _validColor = characterLimitText.color;

        if (profileName != null)
        {
            profileName.characterLimit = ProfileStore.MaxNameLength;
            profileName.onValueChanged.AddListener(OnNameChanged);
        }

        if (nameEditButton != null) nameEditButton.onClick.AddListener(OnNameEditClicked);
        if (doneButton != null) doneButton.onClick.AddListener(OnDoneClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (profileName != null) profileName.onValueChanged.RemoveListener(OnNameChanged);
        if (nameEditButton != null) nameEditButton.onClick.RemoveListener(OnNameEditClicked);
        if (doneButton != null) doneButton.onClick.RemoveListener(OnDoneClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    protected override void OnOpened()
    {
        BuildGridIfNeeded();

        // Load saved profile into the UI.
        if (profileName != null)
            profileName.SetTextWithoutNotify(ProfileStore.PlayerName);

        SelectIcon(ProfileStore.IconIndex);
        UpdateCharacterCounter();
    }

    // ---------------- Name ----------------

    private void OnNameEditClicked()
    {
        PlayClick();
        if (profileName == null) return;
        profileName.Select();
        profileName.ActivateInputField();
    }

    private void OnNameChanged(string _)
    {
        UpdateCharacterCounter();
    }

    private void UpdateCharacterCounter()
    {
        if (characterLimitText == null) return;

        int length = CurrentName.Length;
        characterLimitText.text = $"{length}/{ProfileStore.MaxNameLength}";

        bool valid = length >= ProfileStore.MinNameLength && length <= ProfileStore.MaxNameLength;
        characterLimitText.color = valid ? _validColor : invalidColor;
    }

    private string CurrentName => profileName != null ? profileName.text.Trim() : string.Empty;

    // ---------------- Icon grid ----------------

    private void BuildGridIfNeeded()
    {
        if (_gridBuilt) return;

        ProfileIconsSO icons = Icons;
        if (icons == null || iconGrid == null || profileListingPrefab == null)
        {
            Debug.LogWarning("[ProfilePopup] Missing ProfileIconsSO, icon grid, or ProfileListing prefab.");
            return;
        }

        // Default icon leads the grid (index = NoIconIndex), so the "current"
        // selection is always visible and tickable, including on a fresh save.
        if (icons.DefaultIcon != null)
        {
            ProfileListing defaultListing = Instantiate(profileListingPrefab, iconGrid);
            defaultListing.Init(icons.DefaultIcon, ProfileStore.NoIconIndex, OnListingClicked);
            _listings.Add(defaultListing);
        }

        for (int i = 0; i < icons.Count; i++)
        {
            ProfileListing listing = Instantiate(profileListingPrefab, iconGrid);
            listing.Init(icons.GetIcon(i), i, OnListingClicked);
            _listings.Add(listing);
        }

        _gridBuilt = true;
    }

    private void OnListingClicked(ProfileListing listing)
    {
        PlayClick();
        SelectIcon(listing.IconIndex);
    }

    /// <summary>Single-selection: enables tick+border on the listing whose IconIndex matches,
    /// disables all others. Unknown indices fall back to <see cref="ProfileStore.NoIconIndex"/>
    /// (the Default listing), so the current icon is always ticked on open.</summary>
    private void SelectIcon(int iconIndex)
    {
        bool found = false;
        for (int i = 0; i < _listings.Count; i++)
        {
            if (_listings[i].IconIndex == iconIndex)
            {
                found = true;
                break;
            }
        }

        if (!found)
            iconIndex = ProfileStore.NoIconIndex;

        _selectedIconIndex = iconIndex;

        for (int i = 0; i < _listings.Count; i++)
            _listings[i].SetSelected(_listings[i].IconIndex == iconIndex);
    }

    // ---------------- Done / Close ----------------

    private void OnDoneClicked()
    {
        PlayClick();

        if (!ProfileStore.IsValidName(CurrentName))
        {
            // Invalid name — flash the counter red and keep the popup open.
            UpdateCharacterCounter();
            if (characterLimitText != null)
                characterLimitText.color = invalidColor;
            if (SFXManager.Instance != null)
                SFXManager.Instance.Play(SFXType.Error);
            return;
        }

        ProfileStore.Save(CurrentName, _selectedIconIndex);
        RequestClose();
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }
}
