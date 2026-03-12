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
        if (source2D != null)
            return;

        source2D = GetComponent<AudioSource>();
        if (source2D == null)
            source2D = gameObject.AddComponent<AudioSource>();

        source2D.playOnAwake = false;
        source2D.spatialBlend = 0f;
    }
}
