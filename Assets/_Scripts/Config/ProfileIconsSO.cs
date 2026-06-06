using UnityEngine;

/// <summary>
/// Designer-authored list of selectable profile icons. ProfilePopup builds one
/// ProfileListing per entry; UserProfile resolves the saved icon index through
/// this asset. Mirrors the lookup style of SFXConfig/PopupsConfig.
/// </summary>
[CreateAssetMenu(fileName = "ProfileIcons", menuName = "PuppyPuzzle/Profile Icons")]
public class ProfileIconsSO : ScriptableObject
{
    [Tooltip("Icon shown when the player hasn't picked one yet (or the saved index is invalid). Not part of the selectable grid.")]
    [SerializeField] private Sprite defaultIcon;

    [Tooltip("All selectable profile icons. Order matters — the saved profile stores an index into this array.")]
    [SerializeField] private Sprite[] icons;

    public Sprite DefaultIcon => defaultIcon;

    public int Count => icons != null ? icons.Length : 0;

    /// <summary>Returns the sprite at <paramref name="index"/>, falling back to <see cref="DefaultIcon"/> if out of range.</summary>
    public Sprite GetIcon(int index)
    {
        if (icons == null || index < 0 || index >= icons.Length)
            return defaultIcon;
        return icons[index];
    }
}
