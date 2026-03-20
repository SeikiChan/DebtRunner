using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global one-shot SFX manager.
/// If none exists in scene, it auto-creates one at runtime.
/// </summary>
public class SFXManager : MonoBehaviour
{
    private static SFXManager instance;
    private static bool isCreatingInstance;

    public static SFXManager Instance
    {
        get
        {
            if (instance != null)
                return instance;

            if (isCreatingInstance)
                return null;

            instance = FindObjectOfType<SFXManager>();
            if (instance != null)
                return instance;

            isCreatingInstance = true;
            try
            {
                GameObject go = new GameObject("SFXManager_Auto");
                instance = go.AddComponent<SFXManager>();
            }
            finally
            {
                isCreatingInstance = false;
            }

            return instance;
        }
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource source2D;

    [Header("Master Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    [Header("Pickup Combo")]
    [SerializeField] private bool enablePickupComboPitchRamp = true;
    [SerializeField, Min(0.01f)] private float pickupComboResetSeconds = 0.33f;
    [SerializeField, Min(0f)] private float pickupComboPitchStep = 0.055f;
    [SerializeField, Min(1f)] private float pickupComboMaxPitch = 1.45f;
    [SerializeField, Min(1)] private int pooled2DOneShotSources = 8;

    private readonly List<AudioSource> extra2DOneShotPool = new List<AudioSource>(8);
    private int pickupComboChainCount;
    private float lastPickupCollectTime = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticReference()
    {
        instance = null;
        isCreatingInstance = false;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (CanOwnPersistentHost())
                Destroy(gameObject);
            else
                Destroy(this);
            return;
        }

        instance = this;
        if (CanOwnPersistentHost() && gameObject.scene.IsValid() && gameObject.scene.name != "DontDestroyOnLoad")
            DontDestroyOnLoad(gameObject);

        EnsureSourceReady();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); }
    public float GetMasterVolume() => masterVolume;

    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        EnsureSourceReady();
        if (clip == null || source2D == null)
            return;

        source2D.PlayOneShot(clip, masterVolume * volumeScale);
    }

    public void PlayPickupCollect(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        float pitch = 1f;
        if (enablePickupComboPitchRamp)
        {
            float now = Time.unscaledTime;
            if (now - lastPickupCollectTime > Mathf.Max(0.01f, pickupComboResetSeconds))
                pickupComboChainCount = 0;

            pickupComboChainCount++;
            lastPickupCollectTime = now;
            pitch = Mathf.Min(
                Mathf.Max(1f, pickupComboMaxPitch),
                1f + Mathf.Max(0f, pickupComboPitchStep) * Mathf.Max(0, pickupComboChainCount - 1));
        }

        Play2DOneShot(clip, volumeScale, pitch);
    }

    public void PlayAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, masterVolume * volumeScale);
    }

    public void PlayRandom(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0)
            return;

        Play(clips[Random.Range(0, clips.Length)], volumeScale);
    }

    public void PlayRandomAtPoint(AudioClip[] clips, Vector3 position, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0)
            return;

        PlayAtPoint(clips[Random.Range(0, clips.Length)], position, volumeScale);
    }

    private void Play2DOneShot(AudioClip clip, float volumeScale, float pitch)
    {
        EnsureSourceReady();
        if (clip == null)
            return;

        AudioSource source = GetAvailable2DOneShotSource();
        if (source == null)
            return;

        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.PlayOneShot(clip, masterVolume * Mathf.Max(0f, volumeScale));

        if (!extra2DOneShotPool.Contains(source))
        {
            float lifetime = clip.length > 0f
                ? (clip.length / Mathf.Max(0.1f, source.pitch)) + 0.1f
                : 1f;
            Destroy(source.gameObject, lifetime);
        }
    }

    private void EnsureSourceReady()
    {
        if (source2D != null)
            return;

        source2D = GetComponent<AudioSource>();
        if (source2D == null)
            source2D = gameObject.AddComponent<AudioSource>();

        Configure2DSource(source2D);
    }

    private AudioSource GetAvailable2DOneShotSource()
    {
        for (int i = 0; i < extra2DOneShotPool.Count; i++)
        {
            AudioSource pooled = extra2DOneShotPool[i];
            if (pooled == null)
                continue;

            if (!pooled.isPlaying)
                return pooled;
        }

        if (extra2DOneShotPool.Count < Mathf.Max(1, pooled2DOneShotSources))
        {
            AudioSource created = CreatePooled2DOneShotSource(extra2DOneShotPool.Count);
            if (created != null)
                extra2DOneShotPool.Add(created);
            return created;
        }

        return CreateTemporary2DOneShotSource();
    }

    private AudioSource CreatePooled2DOneShotSource(int index)
    {
        GameObject child = new GameObject($"SFX2D_OneShot_{index + 1}");
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        Configure2DSource(source);
        return source;
    }

    private AudioSource CreateTemporary2DOneShotSource()
    {
        GameObject child = new GameObject("SFX2D_OneShot_Temp");
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        Configure2DSource(source);
        return source;
    }

    private static void Configure2DSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    private bool CanOwnPersistentHost()
    {
        if (transform.parent != null)
            return false;

        Component[] components = GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component is Transform || component is AudioSource || component is SFXManager)
                continue;

            return false;
        }

        return true;
    }
}
