using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup. Sound/haptic are handled by the existing <see cref="SettingsToogle"/>
/// components placed on the prefab (they persist via PlayerPrefs "Settings.Sound" /
/// "Settings.Haptic"), so this controller only owns the close button. Add more wiring
/// here (e.g. theme switching) once a theme list/API exists on ThemeManager.
/// </summary>
public class SettingsPanel : Popup
{
    [Header("Settings Panel")]
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }
}
