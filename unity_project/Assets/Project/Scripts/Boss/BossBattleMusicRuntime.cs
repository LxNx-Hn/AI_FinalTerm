using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossBattleMusicRuntime : MonoBehaviour
{
    private const string BossSceneName = "Boss01_Elevator";
    private const string RooftopSceneName = "RooftopEndingWalk";

    public static BossBattleMusicRuntime Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private string introClipPath = "Boss/PreBgm";
    [SerializeField] private string battleClipPath = "Boss/BossBattleBGM";
    [SerializeField] private string rooftopMemoryClipPath = "Boss/Memory";
    [SerializeField] private string phase3AmbienceResourcesPath = "Boss/BGM";
    [SerializeField] private float introVolume = 0.18f;
    [SerializeField] private float battleVolume = 0.34f;
    [SerializeField] private float phase3BattleVolume = 0.95f;
    [SerializeField] private float phase3AmbienceVolume = 0.22f;
    [SerializeField] private float rooftopMemoryVolume = 0.35f;
    [SerializeField] private Vector2 phase3AmbienceDelayRange = new Vector2(6f, 14f);
    [SerializeField] private float phase3MainFadeDuration = 2.4f;
    [SerializeField] private float fadeOutDuration = 2.2f;

    private AudioSource mainAudioSource;
    private AudioSource ambienceAudioSource;
    private BossHealth bossHealth;
    private bool fadingOut;
    private bool battleMusicStarted;
    private bool phase3AmbienceEnabled;
    private Coroutine ambienceLoopRoutine;
    private Coroutine mainVolumeRoutine;
    private Coroutine fadeOutRoutine;
    private AudioClip[] phase3AmbienceClips;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == BossSceneName || scene.name == RooftopSceneName)
        {
            BossBattleMusicRuntime runtime = EnsureInScene();
            if (scene.name == BossSceneName)
                runtime.BindBossHealth();
            return;
        }

        // 보스·옥상 이외의 씬(타이틀·스테이지 등)으로 이동 시 BGM 즉시 정리
        if (Instance != null)
        {
            if (Instance.mainAudioSource     != null) Instance.mainAudioSource.Stop();
            if (Instance.ambienceAudioSource != null) Instance.ambienceAudioSource.Stop();
            Destroy(Instance.gameObject);
        }
    }

    public static BossBattleMusicRuntime EnsureInScene()
    {
        if (Instance != null)
            return Instance;

        BossBattleMusicRuntime existing = FindFirstObjectByType<BossBattleMusicRuntime>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("BossBattleMusicRuntime");
        return go.AddComponent<BossBattleMusicRuntime>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureAudioSources();

        BindBossHealth();

        if (SceneManager.GetActiveScene().name == RooftopSceneName)
            BeginRooftopMemory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (bossHealth != null)
            bossHealth.onDead.RemoveListener(HandleBossDead);
    }

    public void SetMainVolume(float volume)
    {
        EnsureAudioSources();
        battleVolume = Mathf.Max(0f, volume);

        if (mainAudioSource != null && battleMusicStarted && !fadingOut && !phase3AmbienceEnabled)
            mainAudioSource.volume = battleVolume;
    }

    public void SetPhase3MainVolume(float volume)
    {
        EnsureAudioSources();
        phase3BattleVolume = Mathf.Max(0f, volume);

        if (mainAudioSource != null && battleMusicStarted && phase3AmbienceEnabled && !fadingOut)
            FadeMainVolumeTo(phase3BattleVolume, phase3MainFadeDuration);
    }

    public void SetPhase3AmbienceVolume(float volume)
    {
        EnsureAudioSources();
        phase3AmbienceVolume = Mathf.Max(0f, volume);
    }

    public void BeginIntroMusic()
    {
        EnsureAudioSources();
        ResetBattleLoops();
        PlayLoopingMain(introClipPath, introVolume);
    }

    public void BeginBattleMusic()
    {
        EnsureAudioSources();
        if (battleMusicStarted)
            return;

        if (PlayLoopingMain(battleClipPath, battleVolume))
            battleMusicStarted = true;
    }

    public void BeginRooftopMemory()
    {
        EnsureAudioSources();
        ResetBattleLoops();
        PlayLoopingMain(rooftopMemoryClipPath, rooftopMemoryVolume);
    }

    public void SetPhase3AmbienceEnabled(bool enabled)
    {
        EnsureAudioSources();
        if (phase3AmbienceEnabled == enabled || ambienceAudioSource == null)
            return;

        phase3AmbienceEnabled = enabled;

        if (mainAudioSource != null && battleMusicStarted && !fadingOut)
        {
            float targetVolume = phase3AmbienceEnabled ? phase3BattleVolume : battleVolume;
            float duration = phase3AmbienceEnabled ? phase3MainFadeDuration : 0.35f;
            FadeMainVolumeTo(targetVolume, duration);
        }

        if (phase3AmbienceEnabled)
        {
            if (phase3AmbienceClips == null || phase3AmbienceClips.Length == 0)
            {
                Debug.LogWarning("[BossBattleMusicRuntime] Missing ambience clips under Resources/" + phase3AmbienceResourcesPath);
                phase3AmbienceEnabled = false;
                return;
            }

            ambienceLoopRoutine = StartCoroutine(Phase3AmbienceLoop());
            return;
        }

        if (ambienceLoopRoutine != null)
        {
            StopCoroutine(ambienceLoopRoutine);
            ambienceLoopRoutine = null;
        }

        ambienceAudioSource.Stop();
    }

    private bool PlayLoopingMain(string resourcesPath, float volume)
    {
        if (mainAudioSource == null)
            return false;

        AudioClip clip = Resources.Load<AudioClip>(resourcesPath);
        if (clip == null)
        {
            Debug.LogWarning("[BossBattleMusicRuntime] Missing clip at Resources/" + resourcesPath);
            return false;
        }

        fadingOut = false;
        mainAudioSource.clip = clip;
        mainAudioSource.volume = Mathf.Max(0f, volume);
        mainAudioSource.loop = true;
        mainAudioSource.Play();
        return true;
    }

    private void EnsureAudioSources()
    {
        if (mainAudioSource == null)
            mainAudioSource = CreateAudioSource();

        if (ambienceAudioSource == null)
            ambienceAudioSource = CreateAudioSource();

        if (phase3AmbienceClips == null)
            phase3AmbienceClips = Resources.LoadAll<AudioClip>(phase3AmbienceResourcesPath);

        // 씬에 AudioListener가 없으면 직접 추가 (RooftopEndingWalk 등 카메라 없는 씬 대응)
        if (FindFirstObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();
    }

    private void HandleBossDead()
    {
        if (ambienceLoopRoutine != null)
        {
            StopCoroutine(ambienceLoopRoutine);
            ambienceLoopRoutine = null;
        }
        phase3AmbienceEnabled = false;

        StartCutsceneFadeOut(5f);
    }

    // 컷씬 중(timeScale=0 포함)에도 동작하도록 unscaledDeltaTime 사용
    public void StartCutsceneFadeOut(float duration = 5f)
    {
        if (fadeOutRoutine != null)
            StopCoroutine(fadeOutRoutine);

        fadeOutRoutine = StartCoroutine(FadeOutAndStop(duration));
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        fadingOut = true;

        if (mainVolumeRoutine != null)
        {
            StopCoroutine(mainVolumeRoutine);
            mainVolumeRoutine = null;
        }

        float initialMainVolume = mainAudioSource != null ? mainAudioSource.volume : 0f;
        float initialAmbienceVolume = ambienceAudioSource != null ? ambienceAudioSource.volume : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));

            if (mainAudioSource != null)
                mainAudioSource.volume = Mathf.Lerp(initialMainVolume, 0f, t);

            if (ambienceAudioSource != null)
                ambienceAudioSource.volume = Mathf.Lerp(initialAmbienceVolume, 0f, t);

            yield return null;
        }

        if (mainAudioSource != null)
            mainAudioSource.Stop();

        if (ambienceAudioSource != null)
            ambienceAudioSource.Stop();

        phase3AmbienceEnabled = false;
        battleMusicStarted = false;
        fadingOut = false;
        fadeOutRoutine = null;
    }

    private void BindBossHealth()
    {
        if (bossHealth != null)
            bossHealth.onDead.RemoveListener(HandleBossDead);

        bossHealth = FindFirstObjectByType<BossHealth>();
        if (bossHealth != null)
            bossHealth.onDead.AddListener(HandleBossDead);
    }

    private void ResetBattleLoops()
    {
        if (mainVolumeRoutine != null)
        {
            StopCoroutine(mainVolumeRoutine);
            mainVolumeRoutine = null;
        }

        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
            fadeOutRoutine = null;
        }

        if (ambienceLoopRoutine != null)
        {
            StopCoroutine(ambienceLoopRoutine);
            ambienceLoopRoutine = null;
        }

        phase3AmbienceEnabled = false;
        battleMusicStarted = false;
        fadingOut = false;

        if (ambienceAudioSource != null)
            ambienceAudioSource.Stop();
    }

    private IEnumerator Phase3AmbienceLoop()
    {
        int lastIndex = -1;

        while (phase3AmbienceEnabled && !fadingOut)
        {
            if (phase3AmbienceClips == null || phase3AmbienceClips.Length == 0)
                yield break;

            float minDelay = Mathf.Min(phase3AmbienceDelayRange.x, phase3AmbienceDelayRange.y);
            float maxDelay = Mathf.Max(phase3AmbienceDelayRange.x, phase3AmbienceDelayRange.y);
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            int index = PickAmbienceIndex(lastIndex);
            AudioClip clip = phase3AmbienceClips[index];
            lastIndex = index;

            if (clip == null)
                continue;

            ambienceAudioSource.clip = clip;
            ambienceAudioSource.volume = phase3AmbienceVolume;
            ambienceAudioSource.loop = false;
            ambienceAudioSource.Play();

            while (phase3AmbienceEnabled && !fadingOut && ambienceAudioSource.isPlaying)
                yield return null;
        }
    }

    private int PickAmbienceIndex(int lastIndex)
    {
        if (phase3AmbienceClips.Length <= 1)
            return 0;

        int index = Random.Range(0, phase3AmbienceClips.Length);
        if (index == lastIndex)
            index = (index + Random.Range(1, phase3AmbienceClips.Length)) % phase3AmbienceClips.Length;

        return index;
    }

    private void FadeMainVolumeTo(float targetVolume, float duration)
    {
        if (mainAudioSource == null)
            return;

        if (mainVolumeRoutine != null)
            StopCoroutine(mainVolumeRoutine);

        mainVolumeRoutine = StartCoroutine(FadeMainVolumeRoutine(Mathf.Max(0f, targetVolume), Mathf.Max(0f, duration)));
    }

    private IEnumerator FadeMainVolumeRoutine(float targetVolume, float duration)
    {
        if (mainAudioSource == null)
            yield break;

        float startVolume = mainAudioSource.volume;

        if (duration <= 0f)
        {
            mainAudioSource.volume = targetVolume;
            mainVolumeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mainAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        mainAudioSource.volume = targetVolume;
        mainVolumeRoutine = null;
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.volume = 1f;
        return source;
    }
}
