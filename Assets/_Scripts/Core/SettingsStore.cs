using System;
using UnityEngine;

/// <summary>
/// Single source of truth for the player's settings: music, sound (SFX) and haptics.
/// Static (like <see cref="ProfileStore"/>), persists via PlayerPrefs. Reuses the
/// legacy "Settings.Sound" / "Settings.Haptic" keys written by <see cref="SettingsToogle"/>
/// so existing saves carry over; "Settings.Music" is new.
///
/// Music/Sound are applied immediately through <see cref="SFXManager"/> (BGM vs SFX
/// pool mute). Haptics is persistence-only — vibration call sites read
/// <see cref="HapticsEnabled"/> before vibrating.
/// </summary>
public static class SettingsStore
{
    public const string MusicPrefKey = "Settings.Music";
    public const string SoundPrefKey = "Settings.Sound";   // legacy key, kept for save compatibility
    public const string HapticPrefKey = "Settings.Haptic"; // legacy key, kept for save compatibility

    /// <summary>Raised after any setting changes (settingKey, isOn).</summary>
    public static event Action<string, bool> OnSettingChanged;

    public static bool MusicEnabled
    {
        get => PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        set => Set(MusicPrefKey, value);
    }

    public static bool SoundEnabled
    {
        get => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
        set => Set(SoundPrefKey, value);
    }

    public static bool HapticsEnabled
    {
        get => PlayerPrefs.GetInt(HapticPrefKey, 1) == 1;
        set => Set(HapticPrefKey, value);
    }

    /// <summary>Pushes the current music/sound state into <see cref="SFXManager"/>.
    /// Call once after the SFXManager boots (it does this itself in Awake).</summary>
    public static void ApplyAudioSettings()
    {
        if (SFXManager.Instance == null)
            return;

        SFXManager.Instance.SetMusicMuted(!MusicEnabled);
        SFXManager.Instance.SetSfxMuted(!SoundEnabled);
    }

    private static void Set(string key, bool isOn)
    {
        PlayerPrefs.SetInt(key, isOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        OnSettingChanged?.Invoke(key, isOn);
    }
}
