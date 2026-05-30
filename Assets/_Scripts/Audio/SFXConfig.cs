using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SFXConfig", menuName = "PuppyPuzzle/SFX Config")]
public class SFXConfig : ScriptableObject
{
    [Serializable]
    public struct SFXEntry
    {
        public SFXType type;
        public AudioClip clip;

        [Tooltip("Per-clip volume multiplier (0..1). Multiplied by AudioSource volume at play time.")]
        [Range(0f, 1f)] public float volume;
    }

    [SerializeField] private SFXEntry[] entries;

    public bool TryGetEntry(SFXType type, out SFXEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].type == type)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }
        entry = default;
        return false;
    }
}
