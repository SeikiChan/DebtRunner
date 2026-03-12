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
    [SerializeField] private AudioSource sourceExclusive2D;

    [Header("Master Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

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
            Destroy(gameObject);
            return;
        }

        instance = this;
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

    public bool PlayExclusive(AudioClip clip, float volumeScale = 1f)
    {
        EnsureSourceReady();
        if (clip == null || sourceExclusive2D == null)
            return false;

        if (sourceExclusive2D.isPlaying)
            return false;

        sourceExclusive2D.clip = clip;
        sourceExclusive2D.loop = false;
        sourceExclusive2D.volume = masterVolume * volumeScale;
        sourceExclusive2D.Play();
        return true;
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

    private void EnsureSourceReady()
    {
        if (source2D != null && sourceExclusive2D != null)
            return;

        AudioSource[] sources = GetComponents<AudioSource>();
        if (source2D == null && sources.Length > 0)
            source2D = sources[0];

        if (sourceExclusive2D == null)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i] != source2D)
                {
                    sourceExclusive2D = sources[i];
                    break;
                }
            }
        }

        if (source2D == null)
            source2D = gameObject.AddComponent<AudioSource>();

        if (sourceExclusive2D == null)
            sourceExclusive2D = gameObject.AddComponent<AudioSource>();

        source2D.playOnAwake = false;
        source2D.spatialBlend = 0f;
        sourceExclusive2D.playOnAwake = false;
        sourceExclusive2D.spatialBlend = 0f;
    }
}
