using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景默认 BGM 控制器：负责在场景激活后请求并播放对应 BGM。
/// </summary>
public class SceneDefaultBgmController : MonoBehaviour
{
    // 控制器单例。
    private static SceneDefaultBgmController instance;

    // 场景与 BGM 的映射配置。
    [Header("Config")]
    [SerializeField] private SceneBgmProfileSO sceneBgmProfile;

    // 场景切换时的 BGM 淡入淡出时长。
    [SerializeField, Min(0f)] private float fadeSeconds = 0.8f;

    // 场景未配置 BGM 时，是否停止当前 BGM。
    [SerializeField] private bool stopBgmWhenSceneNotMapped = true;

    // 场景未配置 BGM 时，是否打印提示日志。
    [SerializeField] private bool logWhenSceneNotMapped = true;

    // 是否已经提示过“未找到 SceneBgmProfile”。
    private bool hasWarnedMissingProfile;

    // 延迟播放协程：等待 SceneLoader 黑屏阶段结束后再处理 BGM。
    private Coroutine deferredPlayCoroutine;

    /// <summary>
    /// 自动补齐控制器：若场景里没有则创建一个常驻实例。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateIfMissing()
    {
        if (instance != null)
        {
            return;
        }

        SceneDefaultBgmController existing = FindFirstObjectByType<SceneDefaultBgmController>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject go = new GameObject(nameof(SceneDefaultBgmController));
        instance = go.AddComponent<SceneDefaultBgmController>();
    }

    /// <summary>
    /// 初始化单例并标记常驻。
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
    }

    /// <summary>
    /// 订阅场景加载事件。
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 取消订阅场景加载事件。
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 启动时对当前场景应用一次默认 BGM。
    /// </summary>
    private void Start()
    {
        ScheduleApplySceneDefaultBgm(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 手动刷新当前场景默认 BGM。
    /// </summary>
    public void RefreshCurrentSceneBgm()
    {
        ScheduleApplySceneDefaultBgm(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 场景加载回调：应用该场景的默认 BGM。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScheduleApplySceneDefaultBgm(scene.name);
    }

    /// <summary>
    /// 根据 SceneLoader 状态决定立即处理，或延后到黑屏流程结束后处理。
    /// </summary>
    private void ScheduleApplySceneDefaultBgm(string sceneName)
    {
        SceneLoader loader = FindFirstObjectByType<SceneLoader>();
        if (loader != null && loader.IsLoading)
        {
            if (deferredPlayCoroutine != null)
            {
                StopCoroutine(deferredPlayCoroutine);
            }

            deferredPlayCoroutine = StartCoroutine(WaitLoaderAndApplyBgm(loader, sceneName));
            return;
        }

        ApplySceneDefaultBgm(sceneName);
    }

    /// <summary>
    /// 等待 SceneLoader 完成后，再应用目标场景默认 BGM。
    /// </summary>
    private IEnumerator WaitLoaderAndApplyBgm(SceneLoader loader, string sceneName)
    {
        while (loader != null && loader.IsLoading)
        {
            yield return null;
        }

        deferredPlayCoroutine = null;
        ApplySceneDefaultBgm(sceneName);
    }

    /// <summary>
    /// 根据场景名请求并播放默认 BGM。
    /// </summary>
    private void ApplySceneDefaultBgm(string sceneName)
    {
        if (sceneBgmProfile == null)
        {
            if (!hasWarnedMissingProfile)
            {
                Debug.LogWarning(
                    "[SceneDefaultBgmController] 未配置 SceneBgmProfile，请在 Inspector 手动绑定。",
                    this);
                hasWarnedMissingProfile = true;
            }

            return;
        }

        if (!sceneBgmProfile.TryGetBgmEvent(sceneName, out AudioEventSO evt))
        {
            if (logWhenSceneNotMapped)
            {
                Debug.Log($"[SceneDefaultBgmController] 场景 '{sceneName}' 未配置默认 BGM。", this);
            }

            if (stopBgmWhenSceneNotMapped)
            {
                AudioService.Instance.StopBgm(fadeSeconds);
            }

            return;
        }

        if (evt.Category != AudioEventCategory.Bgm)
        {
            Debug.LogWarning(
                $"[SceneDefaultBgmController] 场景 '{sceneName}' 映射的事件 '{evt.EventId}' 不是 BGM 分类。",
                this);
            return;
        }

        if (AudioService.Instance.CurrentBgm == evt)
        {
            return;
        }

        // 先把场景 BGM 请求进缓存，再真正播放，尽量把首次加载开销前移。
        AudioService.Instance.RequestAudioClip(evt, clip =>
        {
            if (clip == null)
            {
                return;
            }

            // 回调返回时若当前场景已切走，则丢弃过期结果。
            if (!string.Equals(SceneManager.GetActiveScene().name, sceneName, System.StringComparison.Ordinal))
            {
                return;
            }

            // 回调返回时若当前目标已变化，则丢弃过期结果。
            if (AudioService.Instance.CurrentBgm == evt)
            {
                return;
            }

            AudioService.Instance.CrossFadeBgm(evt, fadeSeconds);
        });
    }
}
