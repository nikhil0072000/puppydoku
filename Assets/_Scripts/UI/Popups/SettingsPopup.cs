using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup with three on/off buttons (music, sound, haptics). Each button
/// carries two GameObjects — its "on" and "off" visual — and tapping it flips the
/// setting in <see cref="SettingsStore"/> (persisted + applied immediately) and
/// swaps the visuals. Supersedes the old SettingsToogle-based SettingsPanel.
/// </summary>
public class SettingsPopup : Popup
{
    [Header("Music")]
    [SerializeField] private Button musicBtn;
    [SerializeField] private GameObject musicOn;
    [SerializeField] private GameObject musicOff;

    [Header("Sound")]
    [SerializeField] private Button soundBtn;
    [SerializeField] private GameObject soundOn;
    [SerializeField] private GameObject soundOff;

    [Header("Haptics")]
    [SerializeField] private Button hapticsBtn;
    [SerializeField] private GameObject hapticsOn;
    [SerializeField] private GameObject hapticsOff;

    [Header("Close")]
    [SerializeField] private Button closeBtn;

    protected override void Awake()
    {
        base.Awake();
        if (musicBtn != null) musicBtn.onClick.AddListener(OnMusicClicked);
        if (soundBtn != null) soundBtn.onClick.AddListener(OnSoundClicked);
        if (hapticsBtn != null) hapticsBtn.onClick.AddListener(OnHapticsClicked);
        if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseClicked);

        RefreshAll(); // correct visuals even before the open animation finishes
    }

    private void OnDestroy()
    {
        if (musicBtn != null) musicBtn.onClick.RemoveListener(OnMusicClicked);
        if (soundBtn != null) soundBtn.onClick.RemoveListener(OnSoundClicked);
        if (hapticsBtn != null) hapticsBtn.onClick.RemoveListener(OnHapticsClicked);
        if (closeBtn != null) closeBtn.onClick.RemoveListener(OnCloseClicked);
    }

    protected override void OnOpened()
    {
        RefreshAll();
    }

    // ---- Click handlers ----

    private void OnMusicClicked()
    {
        PlayClick();
        SettingsStore.MusicEnabled = !SettingsStore.MusicEnabled;
        SetVisual(musicOn, musicOff, SettingsStore.MusicEnabled);
    }

    private void OnSoundClicked()
    {
        PlayClick();
        SettingsStore.SoundEnabled = !SettingsStore.SoundEnabled;
        SetVisual(soundOn, soundOff, SettingsStore.SoundEnabled);
    }

    private void OnHapticsClicked()
    {
        PlayClick();
        SettingsStore.HapticsEnabled = !SettingsStore.HapticsEnabled;
        SetVisual(hapticsOn, hapticsOff, SettingsStore.HapticsEnabled);
    }

    private void OnCloseClicked()
    {
        PlayClick();
        RequestClose();
    }

    // ---- Visuals ----

    private void RefreshAll()
    {
        SetVisual(musicOn, musicOff, SettingsStore.MusicEnabled);
        SetVisual(soundOn, soundOff, SettingsStore.SoundEnabled);
        SetVisual(hapticsOn, hapticsOff, SettingsStore.HapticsEnabled);
    }

    private static void SetVisual(GameObject onGo, GameObject offGo, bool isOn)
    {
        if (onGo != null) onGo.SetActive(isOn);
        if (offGo != null) offGo.SetActive(!isOn);
    }
}
