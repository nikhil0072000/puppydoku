using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private VFXConfig vfxConfig;

    [Tooltip("Optional parent transform for spawned VFX (keeps the hierarchy tidy). Leave null to spawn at scene root.")]
    [SerializeField] private Transform vfxParent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play(VFXType type, Vector3 position)
    {
        Play(type, position, Quaternion.identity);
    }

    public void Play(VFXType type, Vector3 position, Quaternion rotation)
    {
        if (vfxConfig == null) return;
        if (!vfxConfig.TryGetEntry(type, out VFXConfig.VFXEntry entry) || entry.prefab == null)
        {
            Debug.LogWarning($"VFXManager: No prefab for {type}.");
            return;
        }

        GameObject instance = Instantiate(entry.prefab, position, rotation, vfxParent);
        if (entry.lifetime > 0f)
            Destroy(instance, entry.lifetime);
    }
}
