using System.Collections;
using UnityEngine;

/// <summary>
/// Global BGM controller with cross-fade.
/// GameFlowController calls OnGameStateChanged() during state transitions.
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioClip bgmTitle;
    [SerializeField] private AudioClip bgmBattle;
    [SerializeField] private AudioClip bgmShop;
    [SerializeField] private AudioClip bgmVictory;
    [SerializeField] private AudioClip bgmBoss;
    [SerializeField] private AudioClip bgmStoryIntro;
    [SerializeField] private AudioClip bgmLevelUp;
    [SerializeField] private AudioClip bgmGameOver;

    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.8f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool storyIntroLoop = true;
    [SerializeField] private bool levelUpLoop = false;
    [SerializeField] private bool gameOverLoop = false;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool usingA = true;
    private Coroutine fadeRoutine;
    private AudioClip gameplayResumeClip;
    private float gameplayResumeTime;

    public float Volume => volume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureSourcesReady();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnGameStateChanged(GameFlowController.GameState newState)
    {
        AudioClip clip = newState switch
        {
            GameFlowController.GameState.Title => bgmTitle,
            GameFlowController.GameState.Gameplay => bgmBattle,
            GameFlowController.GameState.Settlement => null,
            GameFlowController.GameState.Shop => bgmShop,
            GameFlowController.GameState.Victory => bgmVictory,
            GameFlowController.GameState.GameOver => bgmGameOver,
            _ => null
        };

        if (clip == null)
            return;

        EnsureSourcesReady();

        AudioSource current = usingA ? sourceA : sourceB;
        if (current != null && current.clip == clip && current.isPlaying)
            return;

        CrossFadeTo(clip, ConsumeResumeTimeIfMatching(clip));
    }

    /// <summary>
    /// Switch to dedicated boss track, or battle track as fallback.
    /// </summary>
    public void PlayBossBGM()
    {
        AudioClip bossClip = bgmBoss != null ? bgmBoss : bgmBattle;
        if (bossClip == null)
            return;

        EnsureSourcesReady();

        AudioSource current = usingA ? sourceA : sourceB;
        if (current != null && current.clip == bossClip && current.isPlaying)
            return;

        CrossFadeTo(bossClip, ConsumeResumeTimeIfMatching(bossClip));
    }

    public void PlayLevelUpBGM()
    {
        if (bgmLevelUp == null)
            return;

        EnsureSourcesReady();

        AudioSource current = usingA ? sourceA : sourceB;
        if (current != null && current.clip == bgmLevelUp && current.isPlaying)
            return;

        CacheGameplayResumePoint();
        CrossFadeTo(bgmLevelUp);
    }

    public void PlayStoryIntroBGM()
    {
        if (bgmStoryIntro == null)
            return;

        EnsureSourcesReady();

        AudioSource current = usingA ? sourceA : sourceB;
        if (current != null && current.clip == bgmStoryIntro && current.isPlaying)
            return;

        CrossFadeTo(bgmStoryIntro);
    }

    public void PlayVictoryBGM()
    {
        if (bgmVictory == null)
            return;

        EnsureSourcesReady();

        AudioSource current = usingA ? sourceA : sourceB;
        if (current != null && current.clip == bgmVictory && current.isPlaying)
            return;

        CrossFadeTo(bgmVictory);
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);

        if (sourceA != null)
            sourceA.volume = usingA ? volume : 0f;

        if (sourceB != null)
            sourceB.volume = usingA ? 0f : volume;
    }

    private void CrossFadeTo(AudioClip newClip, float startTime = 0f)
    {
        if (newClip == null)
            return;

        EnsureSourcesReady();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        AudioSource from = usingA ? sourceA : sourceB;
        AudioSource to = usingA ? sourceB : sourceA;
        usingA = !usingA;

        if (to == null)
            return;

        to.clip = newClip;
        to.volume = 0f;
        to.loop = ShouldLoop(newClip);
        if (newClip.length > 0.01f)
            to.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, newClip.length - 0.01f));
        to.Play();

        if (from == null || !from.isPlaying || fadeDuration <= 0.001f)
        {
            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }

            to.volume = volume;
            fadeRoutine = null;
            return;
        }

        fadeRoutine = StartCoroutine(FadeRoutine(from, to));
    }

    private IEnumerator FadeRoutine(AudioSource from, AudioSource to)
    {
        float elapsed = 0f;
        float fromStartVol = from.volume;
        float dur = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            from.volume = Mathf.Lerp(fromStartVol, 0f, t);
            to.volume = Mathf.Lerp(0f, volume, t);
            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        to.volume = volume;
        fadeRoutine = null;
    }

    private void EnsureSourcesReady()
    {
        if (sourceA != null && sourceB != null)
            return;

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            sourceA = sources[0];
            sourceB = sources[1];
        }
        else
        {
            sourceA = sourceA ?? gameObject.AddComponent<AudioSource>();
            sourceB = sourceB ?? gameObject.AddComponent<AudioSource>();
        }

        SetupSource(sourceA);
        SetupSource(sourceB);

        if (usingA)
        {
            sourceA.volume = volume;
            sourceB.volume = 0f;
        }
        else
        {
            sourceA.volume = 0f;
            sourceB.volume = volume;
        }
    }

    private void SetupSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    private bool ShouldLoop(AudioClip clip)
    {
        if (clip == null)
            return loop;

        if (clip == bgmLevelUp)
            return levelUpLoop;

        if (clip == bgmStoryIntro)
            return storyIntroLoop;

        if (clip == bgmGameOver)
            return gameOverLoop;

        return loop;
    }

    private void CacheGameplayResumePoint()
    {
        AudioSource current = usingA ? sourceA : sourceB;
        if (current == null || current.clip == null || !current.isPlaying)
            return;

        AudioClip bossClip = bgmBoss != null ? bgmBoss : bgmBattle;
        if (current.clip != bgmBattle && current.clip != bossClip)
            return;

        gameplayResumeClip = current.clip;
        gameplayResumeTime = Mathf.Max(0f, current.time);
    }

    private float ConsumeResumeTimeIfMatching(AudioClip clip)
    {
        if (clip == null || gameplayResumeClip != clip)
            return 0f;

        float resumeTime = gameplayResumeTime;
        gameplayResumeClip = null;
        gameplayResumeTime = 0f;
        return resumeTime;
    }
}
