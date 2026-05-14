using System;
using UnityEngine;

[CreateAssetMenu(fileName = "VFXConfig", menuName = "PuppyPuzzle/VFX Config")]
public class VFXConfig : ScriptableObject
{
    [Serializable]
    public struct VFXEntry
    {
        public VFXType type;
        public GameObject prefab;

        [Tooltip("Auto-destroy lifetime in seconds. Use 0 if the prefab destroys itself (e.g. Particle System Stop Action = Destroy).")]
        [Min(0f)] public float lifetime;
    }

    [SerializeField] private VFXEntry[] entries;

    public bool TryGetEntry(VFXType type, out VFXEntry entry)
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
