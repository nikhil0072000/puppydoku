using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One selectable profile-icon entry inside ProfilePopup's grid. Lives on the
/// ProfileListing prefab (root must have a Button). ProfilePopup instantiates one
/// per icon in <see cref="ProfileIconsSO"/>, then drives the single-selection state:
/// the selected listing shows its tick + border, all others hide theirs.
/// </summary>
[RequireComponent(typeof(Button))]
public class ProfileListing : MonoBehaviour
{
    [Header("Profile Listing")]
    [Tooltip("Checkmark shown only on the selected listing.")]
    [SerializeField] private GameObject tick;

    [Tooltip("Highlight border shown only on the selected listing.")]
    [SerializeField] private GameObject border;

    [Tooltip("Image that displays this listing's profile icon sprite.")]
    [SerializeField] private Image icon;

    private Button _button;
    private Action<ProfileListing> _onClicked;

    /// <summary>Index of this listing's sprite inside ProfileIconsSO.</summary>
    public int IconIndex { get; private set; }

    public bool IsSelected { get; private set; }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClicked);
    }

    /// <summary>Called by ProfilePopup right after instantiation.</summary>
    public void Init(Sprite sprite, int iconIndex, Action<ProfileListing> onClicked)
    {
        IconIndex = iconIndex;
        _onClicked = onClicked;

        if (icon != null)
            icon.sprite = sprite;

        SetSelected(false);
    }

    /// <summary>Shows/hides the tick and border. ProfilePopup calls this so only one listing is selected.</summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (tick != null) tick.SetActive(selected);
        if (border != null) border.SetActive(selected);
    }

    private void HandleClicked()
    {
        _onClicked?.Invoke(this);
    }
}
