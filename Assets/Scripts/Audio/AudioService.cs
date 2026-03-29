using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 全局音频服务：统一提供 UI、SFX 与 BGM 的播放入口。
/// </summary>
[RequireComponent(typeof(AudioVolumePresenter))]
public class AudioService : MonoBehaviour
{
    // 默认的 AudioCatalog 资源路径。
    private const string DefaultCatalogResourcePath = "Audio/AudioCatalog";

    // 默认的 UI 音效配置资源路径。
    private const string DefaultUiProfileResourcePath = "Audio/UIDefaultAudioProfile";

    // 单例实例。
    private static AudioService instance;

    /// <summary>
    /// 获取音频服务单例；若场景中不存在则自动创建。
    /// </summary>
    public static AudioService Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<AudioService>();
            if (instance != null)
            {
                return instance;
            }

            // 场景中不存在时自举创建，避免依赖特定场景初始化。
            GameObject go = new GameObject(nameof(AudioService));
            instance = go.AddComponent<AudioService>();
            return instance;
        }
    }

    // 全局音效事件目录。
    [Header("Config")]
    [SerializeField] private AudioCatalogSO catalog;

    // UI 默认音效配置。
    [SerializeField] private UIDefaultAudioProfileSO uiDefaultProfile;

    // SFX 池初始化数量。
    [Header("SFX Pool")]
    [SerializeField] private int initialSfxPoolSize = 8;

    // SFX 池最大数量。
    [SerializeField] private int maxSfxPoolSize = 24;

    // 音频路由：UI/SFX 统一走 SFX 组，BGM 走 BGM 组。
    [Header("Mixer Routing")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    // 复用的 SFX AudioSource 池。
    private readonly List<AudioSource> sfxSourcePool = new List<AudioSource>();

    // 事件冷却缓存：eventId -> 冷却结束时间。
    private readonly Dictionary<string, float> cooldownUntilByEventId = new Dictionary<string, float>();

    // UI 专用音频源。
    private AudioSource uiSource;

    // BGM 专用音频源。
    private AudioSource bgmSource;

    // 当前播放中的 BGM 事件。
    private AudioEventSO currentBgm;

    // BGM 渐变协程句柄。
    private Coroutine bgmFadeCoroutine;

    // 当前 BGM 事件只读暴露。
    public AudioEventSO CurrentBgm => currentBgm;

    // SFX 混音组只读暴露（供音量模块解析 Mixer）。
    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

    // BGM 混音组只读暴露（供音量模块解析 Mixer）。
    public AudioMixerGroup BgmMixerGroup => bgmMixerGroup;

    /// <summary>
    /// 单例初始化与核心资源准备。
    /// </summary>
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        // 子对象挂在已常驻根节点下时无需再次调用，避免 Unity 警告。
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureConfigLoaded();
        EnsureCoreSources();
        WarmUpSfxPool();
    }

    /// <summary>
    /// 确保配置已加载；若未注入则尝试从 Resources 加载。
    /// </summary>
    private void EnsureConfigLoaded()
    {
        if (catalog == null)
        {
            catalog = Resources.Load<AudioCatalogSO>(DefaultCatalogResourcePath);
        }

        if (uiDefaultProfile == null)
        {
            uiDefaultProfile = Resources.Load<UIDefaultAudioProfileSO>(DefaultUiProfileResourcePath);
        }
    }

    /// <summary>
    /// 创建并初始化 UI/BGM 核心音频源。
    /// </summary>
    private void EnsureCoreSources()
    {
        uiSource = GetComponent<AudioSource>();
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
        }

        uiSource.playOnAwake = false;
        uiSource.loop = false;
        uiSource.spatialBlend = 0f;
        uiSource.outputAudioMixerGroup = sfxMixerGroup;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    /// <summary>
    /// 预热 SFX 池，减少首次播放时的分配开销。
    /// </summary>
    private void WarmUpSfxPool()
    {
        int count = Mathf.Max(1, initialSfxPoolSize);
        for (int i = 0; i < count; i++)
        {
            sfxSourcePool.Add(CreateSfxSource());
        }
    }

    /// <summary>
    /// 创建一个用于 SFX 的 2D AudioSource。
    /// </summary>
    private AudioSource CreateSfxSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = sfxMixerGroup;
        return source;
    }

    /// <summary>
    /// 通过事件 ID 从目录中获取音效事件。
    /// </summary>
    /// <param name="eventId">事件唯一标识。</param>
    /// <returns>找到则返回事件，否则返回 null。</returns>
    public AudioEventSO GetEventOrNull(string eventId)
    {
        if (catalog == null || string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        return catalog.GetEventOrNull(eventId);
    }

    /// <summary>
    /// 获取指定 UI 角色的默认点击音效。
    /// </summary>
    /// <param name="role">UI 音效角色。</param>
    /// <returns>点击事件或 null。</returns>
    public AudioEventSO GetDefaultUiClickEvent(UIAudioRole role)
    {
        return uiDefaultProfile == null ? null : uiDefaultProfile.GetClickEvent(role);
    }

    /// <summary>
    /// 获取指定 UI 角色的默认悬停音效。
    /// </summary>
    /// <param name="role">UI 音效角色。</param>
    /// <returns>悬停事件或 null。</returns>
    public AudioEventSO GetDefaultUiHoverEvent(UIAudioRole role)
    {
        return uiDefaultProfile == null ? null : uiDefaultProfile.GetHoverEvent(role);
    }

    /// <summary>
    /// 播放 UI 音效事件。
    /// </summary>
    /// <param name="evt">目标音效事件。</param>
    public void PlayUI(AudioEventSO evt)
    {
        PlayWithSource(evt, uiSource);
    }

    /// <summary>
    /// 播放 2D SFX 音效事件。
    /// </summary>
    /// <param name="evt">目标音效事件。</param>
    public void PlaySfx2D(AudioEventSO evt)
    {
        AudioSource source = GetAvailableSfxSource();
        if (source == null)
        {
            return;
        }

        PlayWithSource(evt, source);
    }

    /// <summary>
    /// 播放 BGM；当前版本仅提供基础播放与渐变能力。
    /// </summary>
    /// <param name="evt">目标 BGM 事件。</param>
    /// <param name="fadeSeconds">渐变时长（秒）。</param>
    public void PlayBgm(AudioEventSO evt, float fadeSeconds = 0.5f)
    {
        currentBgm = evt;
        if (evt == null)
        {
            Debug.Log("[AudioService] 跳过播放 BGM：事件为空。");
            return;
        }

        if (!evt.TryPickClip(out AudioClip clip))
        {
            Debug.Log($"[AudioService] 跳过播放 BGM：事件 '{evt.EventId}' 未配置音频片段。");
            return;
        }

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        float targetVolume = evt.GetRuntimeVolume();
        bgmSource.pitch = 1f;
        bgmSource.loop = true;

        // 无需渐变或当前未播放时，直接切换。
        if (fadeSeconds <= 0f || !bgmSource.isPlaying)
        {
            bgmSource.clip = clip;
            bgmSource.volume = targetVolume;
            bgmSource.Play();
            return;
        }

        bgmFadeCoroutine = StartCoroutine(CrossFadeRoutine(clip, targetVolume, fadeSeconds));
    }

    /// <summary>
    /// 停止当前 BGM。
    /// </summary>
    /// <param name="fadeSeconds">淡出时长（秒）。</param>
    public void StopBgm(float fadeSeconds = 0.5f)
    {
        currentBgm = null;

        if (!bgmSource.isPlaying)
        {
            return;
        }

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        // 无淡出时直接停止。
        if (fadeSeconds <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        bgmFadeCoroutine = StartCoroutine(FadeOutAndStopRoutine(fadeSeconds));
    }

    /// <summary>
    /// BGM 交叉淡入淡出。
    /// </summary>
    /// <param name="evt">目标 BGM 事件。</param>
    /// <param name="fadeSeconds">渐变时长（秒）。</param>
    public void CrossFadeBgm(AudioEventSO evt, float fadeSeconds = 0.5f)
    {
        PlayBgm(evt, fadeSeconds);
    }

    /// <summary>
    /// 获取一个可用的 SFX 音频源；必要时扩容池。
    /// </summary>
    /// <returns>可用的 AudioSource。</returns>
    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSourcePool.Count; i++)
        {
            if (!sfxSourcePool[i].isPlaying)
            {
                return sfxSourcePool[i];
            }
        }

        // 池已满时复用第一个，避免无上限增长。
        if (sfxSourcePool.Count >= Mathf.Max(1, maxSfxPoolSize))
        {
            return sfxSourcePool[0];
        }

        AudioSource source = CreateSfxSource();
        sfxSourcePool.Add(source);
        return source;
    }

    /// <summary>
    /// 以指定音频源执行事件播放。
    /// </summary>
    /// <param name="evt">音效事件。</param>
    /// <param name="source">目标音频源。</param>
    private void PlayWithSource(AudioEventSO evt, AudioSource source)
    {
        if (source == null || evt == null)
        {
            return;
        }

        if (!CanPassCooldown(evt))
        {
            return;
        }

        if (!evt.TryPickClip(out AudioClip clip))
        {
            return;
        }

        source.loop = evt.Loop;
        source.pitch = evt.GetRuntimePitch();

        // 循环事件使用 clip 播放，非循环事件使用 one-shot。
        if (evt.Loop)
        {
            source.clip = clip;
            source.volume = evt.GetRuntimeVolume();
            source.Play();
        }
        else
        {
            source.PlayOneShot(clip, evt.GetRuntimeVolume());
        }
    }

    /// <summary>
    /// 检查事件冷却是否通过。
    /// </summary>
    /// <param name="evt">音效事件。</param>
    /// <returns>可播放返回 true，否则 false。</returns>
    private bool CanPassCooldown(AudioEventSO evt)
    {
        float cooldown = evt.CooldownSeconds;
        if (cooldown <= 0f || string.IsNullOrWhiteSpace(evt.EventId))
        {
            return true;
        }

        if (cooldownUntilByEventId.TryGetValue(evt.EventId, out float readyAt) && Time.unscaledTime < readyAt)
        {
            return false;
        }

        cooldownUntilByEventId[evt.EventId] = Time.unscaledTime + cooldown;
        return true;
    }

    /// <summary>
    /// 在两个 BGM 之间执行交叉淡入淡出。
    /// </summary>
    /// <param name="nextClip">目标音频片段。</param>
    /// <param name="targetVolume">目标音量。</param>
    /// <param name="duration">渐变时长。</param>
    /// <returns>协程枚举器。</returns>
    private IEnumerator CrossFadeRoutine(AudioClip nextClip, float targetVolume, float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = nextClip;
        bgmSource.Play();

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    /// <summary>
    /// 淡出并停止当前 BGM。
    /// </summary>
    /// <param name="duration">淡出时长。</param>
    /// <returns>协程枚举器。</returns>
    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.volume = startVolume;
        bgmFadeCoroutine = null;
    }
}

