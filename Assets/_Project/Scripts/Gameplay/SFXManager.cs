using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global one-shot SFX manager.
/// If none exists in scene, it auto-creates one at runtime.
/// </summary>
public class SFXManager : MonoBehaviour
{
    private static readonly int[] PickupComboScaleSemitoneSteps = { 0, 3, 5, 7, 10, 12, 15 };

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
    [SerializeField, Min(0.01f)] private float pickupComboResetSeconds = 0.55f;
    [SerializeField, Min(0f)] private float pickupComboPitchStep = 0.06f;
    [SerializeField] private bool usePickupComboScaleSteps = true;
    [SerializeField, Min(1)] private int pickupComboStepRepeatCount = 1;
    [SerializeField, Min(0.8f)] private float pickupComboMaxPitch = 1.26f;
    [SerializeField, Min(0.5f)] private float pickupCollectBasePitch = 0.82f;
    [SerializeField, Min(0f)] private float pickupCollectPitchJitter = 0.001f;
    [SerializeField, Min(0f)] private float pickupCollectMinIntervalSeconds = 0.022f;
    [SerializeField, Min(0f)] private float pickupCollectAttackSeconds = 0.008f;
    [SerializeField, Min(0f)] private float pickupCollectReleaseSeconds = 0.09f;
    [SerializeField, Min(0.01f)] private float pickupCollectBurstWindowSeconds = 0.26f;
    [SerializeField, Range(0f, 1f)] private float pickupCollectBurstVolumeDuckPerLayer = 0.18f;
    [SerializeField, Range(0f, 1f)] private float pickupCollectMinBurstVolumeMultiplier = 0.42f;
    [SerializeField, Min(1)] private int pooled2DOneShotSources = 8;

    private readonly List<AudioSource> extra2DOneShotPool = new List<AudioSource>(8);
    private readonly List<float> recentPickupPlaybackTimes = new List<float>(8);
    private readonly Dictionary<AudioSource, Coroutine> active2DOneShotEnvelopes = new Dictionary<AudioSource, Coroutine>(8);
    private int pickupComboChainCount;
    private float lastPickupCollectTime = float.NegativeInfinity;
    private float lastPickupCollectPlaybackTime = float.NegativeInfinity;
    private bool pickupCollectQueued;
    private AudioClip queuedPickupCollectClip;
    private float queuedPickupCollectVolumeScale;
    private int queuedPickupCollectCount;

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

    private void Update()
    {
        FlushQueuedPickupCollect();
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

        float now = Time.unscaledTime;
        float minInterval = Mathf.Max(0f, pickupCollectMinIntervalSeconds);
        if (minInterval > 0f && now - lastPickupCollectPlaybackTime < minInterval)
        {
            pickupCollectQueued = true;
            queuedPickupCollectClip = clip;
            queuedPickupCollectVolumeScale = Mathf.Max(queuedPickupCollectVolumeScale, Mathf.Max(0f, volumeScale));
            queuedPickupCollectCount += 1;
            return;
        }

        PlayPickupCollectNow(clip, volumeScale, now, 1);
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
        Play2DOneShot(clip, volumeScale, pitch, 0f, 0f);
    }

    private void Play2DOneShot(AudioClip clip, float volumeScale, float pitch, float fadeInSeconds, float fadeOutSeconds)
    {
        EnsureSourceReady();
        if (clip == null)
            return;

        AudioSource source = GetAvailable2DOneShotSource();
        if (source == null)
            return;

        ResetOneShotSourceState(source);

        float clampedPitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.pitch = clampedPitch;
        bool useEnvelope = fadeInSeconds > 0f || fadeOutSeconds > 0f;
        source.volume = useEnvelope ? 0f : 1f;
        source.PlayOneShot(clip, masterVolume * Mathf.Max(0f, volumeScale));

        if (useEnvelope)
        {
            float lifetime = clip.length > 0f
                ? (clip.length / Mathf.Max(0.1f, clampedPitch))
                : 1f;
            Coroutine envelopeCo = StartCoroutine(AnimateOneShotEnvelope(source, lifetime, fadeInSeconds, fadeOutSeconds));
            active2DOneShotEnvelopes[source] = envelopeCo;
        }

        if (!extra2DOneShotPool.Contains(source))
        {
            float lifetime = clip.length > 0f
                ? (clip.length / Mathf.Max(0.1f, source.pitch)) + 0.1f
                : 1f;
            Destroy(source.gameObject, lifetime);
        }
    }

    private void FlushQueuedPickupCollect()
    {
        if (!pickupCollectQueued || queuedPickupCollectClip == null)
            return;

        float now = Time.unscaledTime;
        if (now - lastPickupCollectPlaybackTime < Mathf.Max(0f, pickupCollectMinIntervalSeconds))
            return;

        AudioClip clip = queuedPickupCollectClip;
        float volumeScale = queuedPickupCollectVolumeScale;
        int collectCount = Mathf.Max(1, queuedPickupCollectCount);

        pickupCollectQueued = false;
        queuedPickupCollectClip = null;
        queuedPickupCollectVolumeScale = 0f;
        queuedPickupCollectCount = 0;

        PlayPickupCollectNow(clip, volumeScale, now, collectCount);
    }

    private void PlayPickupCollectNow(AudioClip clip, float volumeScale, float now, int comboAdvanceCount)
    {
        if (clip == null)
            return;

        float pitch = GetPickupCollectPitch(now, comboAdvanceCount);
        float overlapVolumeMultiplier = GetPickupCollectOverlapVolumeMultiplier(now);
        float effectiveVolumeScale = Mathf.Max(0f, volumeScale) * overlapVolumeMultiplier;

        Play2DOneShot(
            clip,
            effectiveVolumeScale,
            pitch,
            pickupCollectAttackSeconds,
            pickupCollectReleaseSeconds);

        lastPickupCollectPlaybackTime = now;
        recentPickupPlaybackTimes.Add(now);
    }

    private float GetPickupCollectPitch(float now, int comboAdvanceCount)
    {
        float basePitch = Mathf.Max(0.1f, pickupCollectBasePitch);
        float maxPitch = Mathf.Max(basePitch, pickupComboMaxPitch);
        float pitch = basePitch;

        if (enablePickupComboPitchRamp)
        {
            if (now - lastPickupCollectTime > Mathf.Max(0.01f, pickupComboResetSeconds))
                pickupComboChainCount = 0;

            pickupComboChainCount += Mathf.Max(1, comboAdvanceCount);
            lastPickupCollectTime = now;
            int stepIndex = GetPickupComboStepIndex();
            if (usePickupComboScaleSteps)
            {
                int semitone = PickupComboScaleSemitoneSteps[Mathf.Min(stepIndex, PickupComboScaleSemitoneSteps.Length - 1)];
                pitch = basePitch * Mathf.Pow(2f, semitone / 12f);
            }
            else
            {
                pitch += Mathf.Max(0f, pickupComboPitchStep) * stepIndex;
            }

            pitch = Mathf.Min(maxPitch, pitch);
        }

        float jitter = Mathf.Max(0f, pickupCollectPitchJitter);
        if (jitter > 0f)
            pitch += Random.Range(-jitter, jitter);

        return Mathf.Clamp(pitch, 0.1f, maxPitch);
    }

    private int GetPickupComboStepIndex()
    {
        int repeatCount = Mathf.Max(1, pickupComboStepRepeatCount);
        int chainOffset = Mathf.Max(0, pickupComboChainCount - 1);
        return chainOffset / repeatCount;
    }

    private float GetPickupCollectOverlapVolumeMultiplier(float now)
    {
        TrimRecentPickupPlaybackTimes(now);

        if (recentPickupPlaybackTimes.Count <= 0)
            return 1f;

        float duckPerLayer = Mathf.Clamp01(pickupCollectBurstVolumeDuckPerLayer);
        float minMultiplier = Mathf.Clamp01(pickupCollectMinBurstVolumeMultiplier);
        float multiplier = 1f - duckPerLayer * recentPickupPlaybackTimes.Count;
        return Mathf.Max(minMultiplier, multiplier);
    }

    private void TrimRecentPickupPlaybackTimes(float now)
    {
        float window = Mathf.Max(0.01f, pickupCollectBurstWindowSeconds);
        for (int i = recentPickupPlaybackTimes.Count - 1; i >= 0; i--)
        {
            if (now - recentPickupPlaybackTimes[i] > window)
                recentPickupPlaybackTimes.RemoveAt(i);
        }
    }

    private void ResetOneShotSourceState(AudioSource source)
    {
        if (source == null)
            return;

        if (active2DOneShotEnvelopes.TryGetValue(source, out Coroutine activeEnvelope))
        {
            if (activeEnvelope != null)
                StopCoroutine(activeEnvelope);

            active2DOneShotEnvelopes.Remove(source);
        }

        source.volume = 1f;
        source.pitch = 1f;
    }

    private IEnumerator AnimateOneShotEnvelope(AudioSource source, float clipLifetime, float fadeInSeconds, float fadeOutSeconds)
    {
        if (source == null)
            yield break;

        float safeLifetime = Mathf.Max(0.01f, clipLifetime);
        float attack = Mathf.Clamp(fadeInSeconds, 0f, safeLifetime);
        float release = Mathf.Clamp(fadeOutSeconds, 0f, safeLifetime);

        if (attack + release > safeLifetime)
            release = Mathf.Max(0f, safeLifetime - attack);

        float fadeOutStart = Mathf.Max(attack, safeLifetime - release);
        float startedAt = Time.unscaledTime;

        while (source != null)
        {
            float elapsed = Time.unscaledTime - startedAt;
            if (elapsed >= safeLifetime)
                break;

            float volume = 1f;
            if (attack > 0f && elapsed < attack)
            {
                volume = Mathf.Clamp01(elapsed / attack);
            }
            else if (release > 0f && elapsed >= fadeOutStart)
            {
                volume = Mathf.Clamp01((safeLifetime - elapsed) / release);
            }

            source.volume = volume;
            yield return null;
        }

        if (source != null)
            source.volume = 1f;

        active2DOneShotEnvelopes.Remove(source);
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
