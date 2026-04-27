using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 全局音频服务：统一提供 UI、SFX 与 BGM 的播放入口。
/// </summary>
[RequireComponent(typeof(AudioVolumePresenter))]
public class AudioService : MonoBehaviour
{
    /// <summary>
    /// 单个音频事件的已加载缓存。
    /// </summary>
    private sealed class LoadedAudioEvent
    {
        // 该事件加载得到的 Addressables 句柄。
        public AsyncOperationHandle<AudioClip> handle;

        // 该事件当前可播放的音频片段。
        public AudioClip clip;
    }

    // 默认的 AudioCatalog 资源路径。
    private const string DefaultCatalogResourcePath = "Audio/AudioCatalog";

    // 默认的 UI 音效配置资源路径。
    private const string DefaultUiProfileResourcePath = "Audio/UIDefaultAudioProfile";

    // 单例实例。
    private static AudioService instance;

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

    // 音频路由：BGM 专用混音组。
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    // 复用的 SFX AudioSource 池。
    private readonly List<AudioSource> sfxSourcePool = new List<AudioSource>();

    // 已加载音频缓存：event -> loaded data。
    private readonly Dictionary<AudioEventSO, LoadedAudioEvent> loadedAudioByEvent =
        new Dictionary<AudioEventSO, LoadedAudioEvent>();

    // 正在加载中的回调队列：同一事件只发起一次加载，其他请求挂到队列里等待。
    private readonly Dictionary<AudioEventSO, List<Action<AudioClip>>> pendingCallbacksByEvent =
        new Dictionary<AudioEventSO, List<Action<AudioClip>>>();

    // UI 专用音频源。
    private AudioSource uiSource;

    // BGM 专用音频源。
    private AudioSource bgmSource;

    // 当前播放中的 BGM 事件。
    private AudioEventSO currentBgm;

    // BGM 渐变协程句柄。
    private Coroutine bgmFadeCoroutine;

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

            GameObject go = new GameObject(nameof(AudioService));
            instance = go.AddComponent<AudioService>();
            return instance;
        }
    }

    // 当前 BGM 只读暴露。
    public AudioEventSO CurrentBgm => currentBgm;

    // SFX 混音组只读暴露（供音量模块解析 Mixer）。
    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

    // BGM 混音组只读暴露（供音量模块解析 Mixer）。
    public AudioMixerGroup BgmMixerGroup => bgmMixerGroup;

    /// <summary>
    /// 初始化核心配置、音频源和默认 UI 音效的后台加载。
    /// </summary>
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureConfigLoaded();
        EnsureCoreSources();
        WarmUpSfxPool();
        PreloadDefaultUiAudio();
    }

    /// <summary>
    /// 服务销毁时释放所有已加载的 Addressables 音频。
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        ReleaseAllLoadedClips();
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
    /// 请求音频片段；若尚未加载则自动异步加载，完成后回调。
    /// </summary>
    /// <param name="evt">目标音频事件。</param>
    /// <param name="onLoaded">加载完成回调。</param>
    public void RequestAudioClip(AudioEventSO evt, Action<AudioClip> onLoaded)
    {
        if (evt == null)
        {
            onLoaded?.Invoke(null);
            return;
        }

        if (TryGetLoadedClip(evt, out AudioClip loadedClip))
        {
            // 已经缓存时直接返回，避免重复走异步。
            onLoaded?.Invoke(loadedClip);
            return;
        }

        if (pendingCallbacksByEvent.TryGetValue(evt, out List<Action<AudioClip>> callbacks))
        {
            if (onLoaded != null)
            {
                callbacks.Add(onLoaded);
            }

            return;
        }

        callbacks = new List<Action<AudioClip>>();
        if (onLoaded != null)
        {
            callbacks.Add(onLoaded);
        }

        // 首个请求负责真正发起加载；后续请求只排队等待结果。
        pendingCallbacksByEvent[evt] = callbacks;
        StartCoroutine(LoadAudioClipRoutine(evt));
    }

    /// <summary>
    /// 播放 UI 音效事件。
    /// </summary>
    /// <param name="evt">目标音效事件。</param>
    public void PlayUI(AudioEventSO evt)
    {
        RequestAudioClip(evt, clip =>
        {
            // MainMenu 直进 Play 时，默认 UI 音效也能在加载完成后正常播放。
            PlayOneShotClip(uiSource, clip);
        });
    }

    /// <summary>
    /// 播放 2D SFX 音效事件。
    /// </summary>
    /// <param name="evt">目标音效事件。</param>
    public void PlaySfx2D(AudioEventSO evt)
    {
        RequestAudioClip(evt, clip =>
        {
            if (clip == null)
            {
                return;
            }

            // 等片段真正可用时再取 AudioSource，避免异步期间源状态变化。
            AudioSource source = GetAvailableSfxSource();
            PlayOneShotClip(source, clip);
        });
    }

    /// <summary>
    /// 预热一组音效事件涉及的音频片段，减少首播卡顿。
    /// </summary>
    /// <param name="eventsToWarmup">需要预热的音效事件列表。</param>
    /// <param name="reportProgress">进度回调（0~1）。</param>
    /// <returns>协程枚举器。</returns>
    public IEnumerator PrewarmAudioEvents(IReadOnlyList<AudioEventSO> eventsToWarmup, Action<float> reportProgress = null)
    {
        List<AudioEventSO> uniqueEvents = CollectUniqueEvents(eventsToWarmup);
        int totalCount = CountValidEvents(uniqueEvents);
        if (totalCount == 0)
        {
            reportProgress?.Invoke(1f);
            yield break;
        }

        int completedCount = 0;
        for (int i = 0; i < uniqueEvents.Count; i++)
        {
            AudioEventSO evt = uniqueEvents[i];
            if (!IsEventLoadable(evt))
            {
                continue;
            }

            bool isCompleted = false;
            RequestAudioClip(evt, _ =>
            {
                // demo 版预热只负责提前把资源拉进缓存，不参与复杂状态控制。
                isCompleted = true;
                completedCount++;
                reportProgress?.Invoke(completedCount / (float)totalCount);
            });

            while (!isCompleted)
            {
                yield return null;
            }
        }

        reportProgress?.Invoke(1f);
    }

    /// <summary>
    /// 释放指定事件加载过的音频资源。
    /// </summary>
    /// <param name="evt">目标音频事件。</param>
    public void ReleaseAudioEvent(AudioEventSO evt)
    {
        if (evt == null || !loadedAudioByEvent.TryGetValue(evt, out LoadedAudioEvent loadedAudio))
        {
            return;
        }

        if (loadedAudio.handle.IsValid())
        {
            Addressables.Release(loadedAudio.handle);
        }

        loadedAudioByEvent.Remove(evt);
    }

    /// <summary>
    /// 播放 BGM；若尚未加载则自动异步加载后播放。
    /// </summary>
    /// <param name="evt">目标 BGM 事件。</param>
    /// <param name="fadeSeconds">渐变时长（秒）。</param>
    public void PlayBgm(AudioEventSO evt, float fadeSeconds = 0.5f)
    {
        AudioEventSO previousBgm = currentBgm;
        if (evt == null)
        {
            StopBgm(fadeSeconds);
            return;
        }

        currentBgm = evt;

        RequestAudioClip(evt, clip =>
        {
            if (clip == null)
            {
                if (currentBgm == evt)
                {
                    currentBgm = null;
                }

                Debug.LogWarning($"[AudioService] 事件 '{evt.EventId}' 加载失败，无法播放 BGM。");
                return;
            }

            // 如果异步返回时目标 BGM 已经被切走，就丢弃这次结果。
            if (currentBgm != evt)
            {
                return;
            }

            StartResolvedBgm(evt, clip, previousBgm, fadeSeconds);
        });
    }

    /// <summary>
    /// 停止当前 BGM，并在停止后释放其加载资源。
    /// </summary>
    /// <param name="fadeSeconds">淡出时长（秒）。</param>
    public void StopBgm(float fadeSeconds = 0.5f)
    {
        AudioEventSO previousBgm = currentBgm;
        currentBgm = null;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        if (!bgmSource.isPlaying)
        {
            ReleaseAudioEvent(previousBgm);
            return;
        }

        if (fadeSeconds <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            ReleaseAudioEvent(previousBgm);
            return;
        }

        bgmFadeCoroutine = StartCoroutine(FadeOutAndStopRoutine(fadeSeconds, previousBgm));
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
    /// 若未在 Inspector 注入配置，则回退到 Resources 默认资源。
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
    /// 创建并初始化 UI/BGM 两个核心音频源。
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
    /// 预热 SFX 池，减少首次播放时的组件创建开销。
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
    /// 启动时后台加载默认 UI 音效，避免主菜单首次交互无声。
    /// </summary>
    private void PreloadDefaultUiAudio()
    {
        UIAudioRole[] roles =
        {
            UIAudioRole.Default,
            UIAudioRole.Important,
            UIAudioRole.Back,
            UIAudioRole.Tab
        };

        for (int i = 0; i < roles.Length; i++)
        {
            RequestAudioClip(GetDefaultUiClickEvent(roles[i]), null);
            RequestAudioClip(GetDefaultUiHoverEvent(roles[i]), null);
        }
    }

    /// <summary>
    /// 创建一个用于 2D SFX 的 AudioSource。
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
    /// 获取一个可用的 SFX AudioSource；必要时扩容池。
    /// </summary>
    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSourcePool.Count; i++)
        {
            if (!sfxSourcePool[i].isPlaying)
            {
                return sfxSourcePool[i];
            }
        }

        if (sfxSourcePool.Count >= Mathf.Max(1, maxSfxPoolSize))
        {
            return sfxSourcePool[0];
        }

        AudioSource source = CreateSfxSource();
        sfxSourcePool.Add(source);
        return source;
    }

    /// <summary>
    /// 实际执行单个事件的 Addressables 异步加载。
    /// </summary>
    /// <param name="evt">目标音频事件。</param>
    /// <returns>协程枚举器。</returns>
    private IEnumerator LoadAudioClipRoutine(AudioEventSO evt)
    {
        AudioClip clip = null;
        AssetReference reference = evt == null ? null : evt.AddressableClip;

        if (reference != null && reference.RuntimeKeyIsValid())
        {
            AsyncOperationHandle<AudioClip> handle = reference.LoadAssetAsync<AudioClip>();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                // 加载成功后进入缓存，后续播放和释放都从这里走。
                clip = handle.Result;
                loadedAudioByEvent[evt] = new LoadedAudioEvent
                {
                    handle = handle,
                    clip = clip
                };
            }
            else if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        if (!pendingCallbacksByEvent.TryGetValue(evt, out List<Action<AudioClip>> callbacks))
        {
            yield break;
        }

        // 加载结束后统一通知等待这个事件的所有请求者。
        pendingCallbacksByEvent.Remove(evt);
        for (int i = 0; i < callbacks.Count; i++)
        {
            callbacks[i]?.Invoke(clip);
        }
    }

    /// <summary>
    /// 开始播放一个已完成解析的 BGM。
    /// </summary>
    /// <param name="evt">目标 BGM 事件。</param>
    /// <param name="clip">已加载音频片段。</param>
    /// <param name="previousBgm">上一个 BGM 事件。</param>
    /// <param name="fadeSeconds">渐变时长。</param>
    private void StartResolvedBgm(AudioEventSO evt, AudioClip clip, AudioEventSO previousBgm, float fadeSeconds)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        bgmSource.pitch = 1f;
        bgmSource.loop = true;

        if (fadeSeconds <= 0f || !bgmSource.isPlaying)
        {
            bgmSource.clip = clip;
            bgmSource.volume = 1f;
            bgmSource.Play();
            ReleasePreviousBgm(previousBgm, evt);
            return;
        }

        bgmFadeCoroutine = StartCoroutine(CrossFadeRoutine(clip, fadeSeconds, previousBgm, evt));
    }

    /// <summary>
    /// 用 one-shot 方式播放已加载音频片段。
    /// </summary>
    /// <param name="source">目标音频源。</param>
    /// <param name="clip">已加载音频片段。</param>
    private void PlayOneShotClip(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
        {
            return;
        }

        source.loop = false;
        source.pitch = 1f;
        source.PlayOneShot(clip, 1f);
    }

    /// <summary>
    /// 尝试从缓存中获取已加载音频片段。
    /// </summary>
    /// <param name="evt">目标音频事件。</param>
    /// <param name="clip">输出音频片段。</param>
    /// <returns>找到可用片段返回 true。</returns>
    private bool TryGetLoadedClip(AudioEventSO evt, out AudioClip clip)
    {
        clip = null;
        if (evt == null || !loadedAudioByEvent.TryGetValue(evt, out LoadedAudioEvent loadedAudio) || loadedAudio == null)
        {
            return false;
        }

        clip = loadedAudio.clip;
        return clip != null;
    }

    /// <summary>
    /// 判断音频事件是否具备可加载的 Addressable 引用。
    /// </summary>
    private bool IsEventLoadable(AudioEventSO evt)
    {
        return evt != null && evt.AddressableClip != null && evt.AddressableClip.RuntimeKeyIsValid();
    }

    /// <summary>
    /// 从输入列表中提取不重复的音频事件。
    /// </summary>
    private List<AudioEventSO> CollectUniqueEvents(IReadOnlyList<AudioEventSO> eventsToWarmup)
    {
        List<AudioEventSO> result = new List<AudioEventSO>();
        HashSet<AudioEventSO> seen = new HashSet<AudioEventSO>();

        if (eventsToWarmup == null)
        {
            return result;
        }

        for (int i = 0; i < eventsToWarmup.Count; i++)
        {
            AudioEventSO evt = eventsToWarmup[i];
            if (evt == null || !seen.Add(evt))
            {
                continue;
            }

            result.Add(evt);
        }

        return result;
    }

    /// <summary>
    /// 统计需要预热的有效音频事件数量。
    /// </summary>
    private int CountValidEvents(IReadOnlyList<AudioEventSO> eventsToWarmup)
    {
        int count = 0;
        if (eventsToWarmup == null)
        {
            return count;
        }

        for (int i = 0; i < eventsToWarmup.Count; i++)
        {
            if (IsEventLoadable(eventsToWarmup[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 切换到新 BGM 后释放上一首，避免无意义常驻。
    /// </summary>
    private void ReleasePreviousBgm(AudioEventSO previousBgm, AudioEventSO nextBgm)
    {
        if (previousBgm == null || previousBgm == nextBgm)
        {
            return;
        }

        // demo 里只强调 BGM 生命周期：切歌后释放上一首即可。
        ReleaseAudioEvent(previousBgm);
    }

    /// <summary>
    /// 统一释放全部已加载的 Addressables 音频。
    /// </summary>
    private void ReleaseAllLoadedClips()
    {
        foreach (KeyValuePair<AudioEventSO, LoadedAudioEvent> pair in loadedAudioByEvent)
        {
            if (pair.Value == null || !pair.Value.handle.IsValid())
            {
                continue;
            }

            Addressables.Release(pair.Value.handle);
        }

        loadedAudioByEvent.Clear();
        pendingCallbacksByEvent.Clear();
    }

    /// <summary>
    /// 在两个 BGM 之间执行交叉淡入淡出。
    /// </summary>
    /// <param name="nextClip">目标音频片段。</param>
    /// <param name="duration">渐变时长。</param>
    /// <param name="previousBgm">上一个 BGM 事件。</param>
    /// <param name="nextBgm">下一个 BGM 事件。</param>
    /// <returns>协程枚举器。</returns>
    private IEnumerator CrossFadeRoutine(AudioClip nextClip, float duration, AudioEventSO previousBgm, AudioEventSO nextBgm)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        // 先淡出当前 BGM，再切到新曲目。
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
            bgmSource.volume = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }

        bgmSource.volume = 1f;
        bgmFadeCoroutine = null;
        ReleasePreviousBgm(previousBgm, nextBgm);
    }

    /// <summary>
    /// 淡出并停止当前 BGM。
    /// </summary>
    /// <param name="duration">淡出时长。</param>
    /// <param name="previousBgm">被停止的 BGM 事件。</param>
    /// <returns>协程枚举器。</returns>
    private IEnumerator FadeOutAndStopRoutine(float duration, AudioEventSO previousBgm)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        // 停止前做一次简单淡出，避免听感突兀。
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.volume = 1f;
        bgmFadeCoroutine = null;
        ReleaseAudioEvent(previousBgm);
    }
}
