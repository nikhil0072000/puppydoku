using System;
using UnityEngine;

/// <summary>
/// Designer-authored registry mapping each <see cref="PopupType"/> to the popup
/// prefab that implements it. The <see cref="PopupCanvasManager"/> reads this to
/// instantiate popups on demand. Mirrors the lookup style of SFXConfig/GridConfig.
/// </summary>
[CreateAssetMenu(fileName = "PopupsConfig", menuName = "PuppyPuzzle/Popups Config")]
public class PopupsConfig : ScriptableObject
{
    [Serializable]
    public struct PopupEntry
    {
        public PopupType type;

        [Tooltip("Prefab whose root has the matching Popup-derived controller.")]
        public Popup prefab;
    }

    [SerializeField] private PopupEntry[] entries;

    /// <summary>Finds the prefab registered for <paramref name="type"/>.</summary>
    /// <returns>True if a non-null prefab is registered for the type.</returns>
    public bool TryGetPrefab(PopupType type, out Popup prefab)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].type == type)
                {
                    prefab = entries[i].prefab;
                    return prefab != null;
                }
            }
        }
        prefab = null;
        return false;
    }
}
