using UnityEngine;
using UnityEngine.Audio;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private SFXConfig sfxConfig;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("SFX Pool (assign exactly 10 AudioSources)")]
    [SerializeField] private AudioSource[] sfxSources = new AudioSource[10];

    [Header("BGM")]
    [SerializeField] private AudioSource bgm1;
    [SerializeField] private AudioSource bgm2;

    [Tooltip("Weight of BGM1 (0..1). BGM2 gets (1 - weight) so the pair never exceeds full volume.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgm1Weight = 0.5f;

    [Tooltip("Master multiplier applied on top of the blended BGM weights.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmMasterVolume = 1f;

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
            return;
        }

        if (sfxSources == null || sfxSources.Length == 0)
            Debug.LogError("SFXManager: SFX pool is empty. Assign AudioSources in the Inspector.");

        ApplyBGMVolumes();
        SettingsStore.ApplyAudioSettings(); // restore persisted music/sound mute state
    }

    void OnValidate()
    {
        // Reflect inspector changes in the editor without entering play mode.
        ApplyBGMVolumes();
    }

    public void Play(SFXType type)
    {
        if (sfxConfig == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[SFXManager] Play({type}) ignored — sfxConfig is null.", this);
#endif
            return;
        }
        if (!sfxConfig.TryGetEntry(type, out SFXConfig.SFXEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SFXManager] No clip for {type}.", this);
            return;
        }

        AudioSource src = FindFreeSource();
        if (src == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[SFXManager] Pool exhausted, dropping {type} (clip='{entry.clip.name}').", this);
#endif
            // Pool exhausted — skip rather than thrash performance.
            return;
        }

        src.clip = entry.clip;
        src.volume = Mathf.Clamp01(entry.volume <= 0f ? 1f : entry.volume);
        if (sfxMixerGroup != null)
            src.outputAudioMixerGroup = sfxMixerGroup;
        src.Play();

#if UNITY_EDITOR
        Debug.Log($"[SFXManager] Play {type} → clip='{entry.clip.name}', source='{src.gameObject.name}', vol={src.volume:0.00}, mixer='{(sfxMixerGroup != null ? sfxMixerGroup.name : "none")}'", src);
#endif
    }

    private AudioSource FindFreeSource()
    {
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource src = sfxSources[i];
            if (src != null && !src.isPlaying)
                return src;
        }
        return null;
    }

    // ---------- BGM ----------

    private void ApplyBGMVolumes()
    {
        float w1 = Mathf.Clamp01(bgm1Weight);
        float w2 = 1f - w1;
        float master = Mathf.Clamp01(bgmMasterVolume);

        if (bgm1 != null) bgm1.volume = w1 * master;
        if (bgm2 != null) bgm2.volume = w2 * master;
    }

    public void PlayBGM()
    {
        if (bgm1 != null && !bgm1.isPlaying) bgm1.Play();
        if (bgm2 != null && !bgm2.isPlaying) bgm2.Play();
    }

    public void StopBGM()
    {
        if (bgm1 != null) bgm1.Stop();
        if (bgm2 != null) bgm2.Stop();
    }

    public void SetBGMBlend(float bgm1WeightValue)
    {
        bgm1Weight = Mathf.Clamp01(bgm1WeightValue);
        ApplyBGMVolumes();
    }

    public void SetBGMMasterVolume(float masterVolume)
    {
        bgmMasterVolume = Mathf.Clamp01(masterVolume);
        ApplyBGMVolumes();
    }

    // ---------- Settings mute (driven by SettingsStore) ----------

    /// <summary>Mutes/unmutes the BGM sources without touching their tuned volumes.</summary>
    public void SetMusicMuted(bool muted)
    {
        if (bgm1 != null) bgm1.mute = muted;
        if (bgm2 != null) bgm2.mute = muted;
    }

    /// <summary>Mutes/unmutes the whole SFX pool without touching per-clip volumes.</summary>
    public void SetSfxMuted(bool muted)
    {
        if (sfxSources == null) return;
        foreach (AudioSource src in sfxSources)
        {
            if (src != null)
                src.mute = muted;
        }
    }
}
