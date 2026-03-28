using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景默认 BGM 控制器：仅负责“场景加载时播放对应默认 BGM”。
/// </summary>
public class SceneDefaultBgmController : MonoBehaviour
{
    // 控制器单例。
    private static SceneDefaultBgmController instance;

    // 场景与 BGM 映射配置。
    [Header("Config")]
    [SerializeField] private SceneBgmProfileSO sceneBgmProfile;

    // 场景切换时的 BGM 淡入淡出时长。
    [SerializeField, Min(0f)] private float fadeSeconds = 0.8f;

    // 场景未配置 BGM 时是否停止当前 BGM。
    [SerializeField] private bool stopBgmWhenSceneNotMapped = true;

    // 场景未配置 BGM 时是否打印日志。
    [SerializeField] private bool logWhenSceneNotMapped = true;

    // 是否已提示过“未找到 SceneBgmProfile”。
    private bool hasWarnedMissingProfile;

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

        // 子对象挂在已常驻根节点下时无需再次调用，避免 Unity 警告。
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
        ApplySceneDefaultBgm(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 手动刷新当前场景默认 BGM（可供调试按钮调用）。
    /// </summary>
    public void RefreshCurrentSceneBgm()
    {
        ApplySceneDefaultBgm(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 场景加载回调：应用该场景的默认 BGM。
    /// </summary>
    /// <param name="scene">已加载场景。</param>
    /// <param name="mode">加载模式。</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneDefaultBgm(scene.name);
    }

    /// <summary>
    /// 根据场景名播放默认 BGM。
    /// </summary>
    /// <param name="sceneName">场景名。</param>
    private void ApplySceneDefaultBgm(string sceneName)
    {
        if (sceneBgmProfile == null)
        {
            // 仅提示一次，避免每次切场景刷屏。
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

        // 目标与当前相同则不重复触发，避免无意义重播。
        if (AudioService.Instance.CurrentBgm == evt)
        {
            return;
        }

        AudioService.Instance.CrossFadeBgm(evt, fadeSeconds);
    }
}
