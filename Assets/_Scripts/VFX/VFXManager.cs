using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private VFXConfig vfxConfig;

    [Tooltip("Optional parent transform for active VFX (keeps the hierarchy tidy). Leave null to spawn at scene root.")]
    [SerializeField] private Transform vfxParent;

    // Idle pooled instances per type. They live under this (DontDestroyOnLoad) transform while
    // inactive so they survive scene loads instead of being destroyed and re-instantiated.
    private readonly Dictionary<VFXType, Queue<GameObject>> pool = new();
    private readonly Dictionary<VFXType, WaitForSeconds> waitCache = new();

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
        if (vfxConfig == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[VFXManager] Play({type}) ignored — vfxConfig is null.", this);
#endif
            return;
        }
        if (!vfxConfig.TryGetEntry(type, out VFXConfig.VFXEntry entry) || entry.prefab == null)
        {
            Debug.LogWarning($"[VFXManager] No prefab for {type}.", this);
            return;
        }

        GameObject instance = Rent(type, entry.prefab);
        Transform t = instance.transform;
        t.SetParent(vfxParent, worldPositionStays: false);
        t.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        RestartParticles(instance);

        // lifetime <= 0 means "persistent" — leave it active; the caller owns its lifetime.
        if (entry.lifetime > 0f)
            StartCoroutine(ReturnAfter(type, instance, entry.lifetime));

#if UNITY_EDITOR
        Debug.Log($"[VFXManager] Play {type} → prefab='{entry.prefab.name}', spawned='{instance.name}', pos={position}, parent='{(vfxParent != null ? vfxParent.name : "<root>")}', lifetime={entry.lifetime:0.00}s", instance);
#endif
    }

    private GameObject Rent(VFXType type, GameObject prefab)
    {
        if (pool.TryGetValue(type, out Queue<GameObject> queue))
        {
            while (queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null) // skip instances destroyed by a scene unload
                    return pooled;
            }
        }
        return Instantiate(prefab);
    }

    private IEnumerator ReturnAfter(VFXType type, GameObject instance, float lifetime)
    {
        if (!waitCache.TryGetValue(type, out WaitForSeconds wait))
        {
            wait = new WaitForSeconds(lifetime);
            waitCache[type] = wait;
        }
        yield return wait;

        if (instance == null)
            yield break;

        instance.SetActive(false);
        instance.transform.SetParent(transform, worldPositionStays: false);

        if (!pool.TryGetValue(type, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pool[type] = queue;
        }
        queue.Enqueue(instance);
    }

    private static void RestartParticles(GameObject instance)
    {
        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }
}
