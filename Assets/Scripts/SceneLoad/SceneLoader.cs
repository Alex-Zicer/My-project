using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    // 默认场景预热配置资源路径。
    private const string DefaultWarmupProfilePath = "SceneLoad/SceneWarmupProfile";

    private static SceneLoader _instance;

    public static SceneLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SceneLoader>();
                if (_instance == null)
                {
                    var prefab = Resources.Load<SceneLoader>("Prefabs/Scene/SceneManager");
                    if (prefab == null)
                    {
                        Debug.LogError("未找到 SceneLoader 预制体：Resources/Prefabs/Scene/SceneManager");
                        return null;
                    }

                    _instance = Instantiate(prefab);
                }
            }

            return _instance;
        }
    }

    [Header("UI组件")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float fadeDuration = 0.5f; // 渐变时间
    [SerializeField] private float minBlackScreenSeconds = 0.1f; // 黑屏最短停留时间
    [SerializeField] private bool logWhenBusy = true; // 正在加载时是否打印提示
    [SerializeField] private SceneWarmupProfileSO warmupProfile; // 场景预热任务配置

    // 当前是否处于场景切换流程中，用于防止重复触发加载。
    private bool _isLoading;
    // 当前加载协程引用。
    private Coroutine _loadingCoroutine;

    // 当前是否正在加载（只读）。
    public bool IsLoading => _isLoading;

    // 运行时复用的任务列表缓冲，避免重复分配。
    private readonly List<SceneWarmupTaskSO> _runtimeWarmupTasks = new List<SceneWarmupTaskSO>();

    // 加载流程事件：开始/场景激活/流程结束。
    public event Action<string> LoadStarted;
    public event Action<string> SceneActivated;
    public event Action<string> LoadFinished;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(_instance.gameObject);
        }

        // 初始化加载幕布。
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }

        // 若未在 Inspector 指定，则尝试从 Resources 加载默认预热配置。
        if (warmupProfile == null)
        {
            warmupProfile = Resources.Load<SceneWarmupProfileSO>(DefaultWarmupProfilePath);
        }
    }

    /// <summary>
    /// 尝试加载目标场景；若当前正在加载则忽略本次请求。
    /// </summary>
    /// <param name="sceneName">目标场景名。</param>
    /// <returns>成功发起返回 true。</returns>
    public bool TryLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("场景名为空，取消加载。");
            return false;
        }

        if (_isLoading)
        {
            if (logWhenBusy)
            {
                Debug.LogWarning($"正在加载场景中，忽略新的请求：{sceneName}", this);
            }
            return false;
        }

        _loadingCoroutine = StartCoroutine(LoadCoroutine(sceneName));
        return true;
    }

    /// <summary>
    /// 对外加载入口。
    /// </summary>
    /// <param name="sceneName">目标场景名。</param>
    public void LoadScene(string sceneName)
    {
        TryLoadScene(sceneName);
    }

    /// <summary>
    /// 场景加载总流程：FadeOut -> 场景异步加载 -> 场景激活 -> 黑屏预热 -> FadeIn。
    /// </summary>
    /// <param name="sceneName">目标场景名。</param>
    /// <returns></returns>
    private IEnumerator LoadCoroutine(string sceneName)
    {
        _isLoading = true;

        try
        {
            LoadStarted?.Invoke(sceneName);

            // 遮罩淡入（屏幕变黑），并阻止点击穿透。
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.blocksRaycasts = true;
            }

            yield return StartCoroutine(FadeTo(1f));
            float blackScreenStartTime = Time.unscaledTime;

            // 显示进度条。
            SetProgressVisible(true);
            SetProgressValue(0f);

            // 第一阶段：异步加载场景到 90%。
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"无法加载场景：{sceneName}");
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                float stageProgress = Mathf.Clamp01(operation.progress / 0.9f);
                // 0~0.85 用于场景异步加载阶段。
                SetProgressValue(stageProgress * 0.85f);
                yield return null;
            }

            SetProgressValue(0.9f);

            // 激活场景，进入新场景生命周期。
            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            SceneActivated?.Invoke(sceneName);

            // 第二阶段：黑屏预热任务。
            yield return StartCoroutine(RunPostActivationWarmup(sceneName));

            // 保证黑屏至少停留一小段时间，避免闪烁感。
            float blackElapsed = Time.unscaledTime - blackScreenStartTime;
            if (blackElapsed < minBlackScreenSeconds)
            {
                yield return new WaitForSecondsRealtime(minBlackScreenSeconds - blackElapsed);
            }

            SetProgressValue(1f);
            SetProgressVisible(false);

            // 遮罩淡出（屏幕变亮）。
            yield return StartCoroutine(FadeTo(0f));
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.blocksRaycasts = false;
            }

            LoadFinished?.Invoke(sceneName);
        }
        finally
        {
            _isLoading = false;
            _loadingCoroutine = null;
        }
    }

    /// <summary>
    /// 场景激活后的黑屏预热阶段。
    /// </summary>
    /// <param name="sceneName">已激活的场景名。</param>
    /// <returns></returns>
    private IEnumerator RunPostActivationWarmup(string sceneName)
    {
        if (warmupProfile == null)
        {
            SetProgressValue(0.96f);
            yield return null;
            yield break;
        }

        warmupProfile.GetTasksForScene(sceneName, _runtimeWarmupTasks);
        if (_runtimeWarmupTasks.Count == 0)
        {
            SetProgressValue(0.96f);
            yield return null;
            yield break;
        }

        float taskWeight = 1f / _runtimeWarmupTasks.Count;
        float completedWeight = 0f;

        for (int i = 0; i < _runtimeWarmupTasks.Count; i++)
        {
            SceneWarmupTaskSO task = _runtimeWarmupTasks[i];
            if (task == null)
            {
                completedWeight += taskWeight;
                SetProgressValue(Mathf.Lerp(0.9f, 1f, completedWeight));
                continue;
            }

            float taskStart = completedWeight;
            float taskEnd = taskStart + taskWeight;

            // 每个任务上报自己的 0~1 进度，统一映射到 0.9~1.0 区间。
            yield return StartCoroutine(task.RunWarmup(sceneName, taskProgress =>
            {
                float normalized = Mathf.Clamp01(taskProgress);
                float mapped = Mathf.Lerp(taskStart, taskEnd, normalized);
                SetProgressValue(Mathf.Lerp(0.9f, 1f, mapped));
            }));

            completedWeight = taskEnd;
            SetProgressValue(Mathf.Lerp(0.9f, 1f, completedWeight));
        }

        _runtimeWarmupTasks.Clear();
    }

    /// <summary>
    /// 实现遮罩 alpha 平滑转换的协程。
    /// </summary>
    /// <param name="targetAlpha">目标 alpha。</param>
    /// <returns></returns>
    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeCanvasGroup.alpha;
        if (Mathf.Approximately(startAlpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float timer = 0f;
        float duration = Mathf.Max(0.0001f, fadeDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 设置进度条显隐。
    /// </summary>
    /// <param name="visible">是否显示。</param>
    private void SetProgressVisible(bool visible)
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 设置进度条值（自动夹取到 0~1）。
    /// </summary>
    /// <param name="value">进度值。</param>
    private void SetProgressValue(float value)
    {
        if (progressBar != null)
        {
            progressBar.value = Mathf.Clamp01(value);
        }
    }
}

