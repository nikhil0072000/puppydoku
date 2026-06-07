using System;
using UnityEngine;

/// <summary>
/// Single source of truth for the player's profile (name + icon index).
/// Persists via PlayerPrefs, mirroring the project's other persistence
/// (EconomyHandler, DailyChallengeManager). Raises <see cref="OnProfileChanged"/>
/// so widgets like <see cref="UserProfile"/> refresh live after a save.
/// </summary>
public static class ProfileStore
{
    private const string NameKey = "Profile.Name";
    private const string IconKey = "Profile.IconIndex";

    public const int MinNameLength = 2;
    public const int MaxNameLength = 12;

    public const string DefaultName = "Player";

    /// <summary>Sentinel for "no icon picked yet" — resolves to ProfileIconsSO.DefaultIcon.</summary>
    public const int NoIconIndex = -1;

    /// <summary>Fired after <see cref="Save"/> writes a new name/icon.</summary>
    public static event Action OnProfileChanged;

    public static string PlayerName => PlayerPrefs.GetString(NameKey, DefaultName);

    public static int IconIndex => PlayerPrefs.GetInt(IconKey, NoIconIndex);

    /// <summary>True if <paramref name="name"/> (already trimmed) is an allowed profile name.</summary>
    public static bool IsValidName(string name)
    {
        return !string.IsNullOrEmpty(name)
               && name.Length >= MinNameLength
               && name.Length <= MaxNameLength;
    }

    /// <summary>Persists the profile and notifies listeners. Returns false if the name is invalid.</summary>
    public static bool Save(string name, int iconIndex)
    {
        string trimmed = name != null ? name.Trim() : string.Empty;
        if (!IsValidName(trimmed))
            return false;

        PlayerPrefs.SetString(NameKey, trimmed);
        PlayerPrefs.SetInt(IconKey, Mathf.Max(NoIconIndex, iconIndex));
        PlayerPrefs.Save();

        OnProfileChanged?.Invoke();
        return true;
    }
}
