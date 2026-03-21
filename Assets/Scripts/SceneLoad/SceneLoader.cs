using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Timers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;

    public static SceneLoader Instance {
        get
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<SceneLoader>();
                if(_instance == null)
                {
                    var prefab = Resources.Load<SceneLoader>("Prefabs/Scene/SceneManager");
                    _instance = Instantiate(prefab);
                }
            }
            return _instance;
        }
    }
    [Header("UI组件")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float fadeDuration = 0.5f;// 渐变时间

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(_instance.gameObject);

        //初始化加载幕布，隐藏
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        if(progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 对外调用的唯一函数
    /// </summary>
    /// <param name="sceneName">目标场景的名字</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadCoroutine(sceneName));
    }

    /// <summary>
    /// 实现异步加载场景的协程
    /// </summary>
    /// <param name="sceneName">目标场景的名字</param>
    /// <returns></returns>
    private IEnumerator LoadCoroutine(string sceneName)
    {
        //遮罩淡入（屏幕变黑）
        fadeCanvasGroup.blocksRaycasts = true;
        yield return StartCoroutine(Fade(1f));

        //显示进度条
        if(progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0;
        }

        //开始异步加载
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        //
        while (!operation.isDone)
        {
            //allowSceneActivation为false时，进度条最多加载到90%
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if(progressBar != null)
            {
                progressBar.value = progress;
            }

            //加载完成后等在一会儿，防止突然切换让玩家突兀
            if(operation.progress >= 0.9f)
            {
                Time.timeScale = 1;
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        //先加载HUD
        UIManager.Instance.InitHUD();

        //隐藏进度条
        if (progressBar != null) progressBar.gameObject.SetActive(false);

        //遮罩淡出（屏幕变亮）
        yield return StartCoroutine(Fade(0f));
        fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 实现遮罩alpha平滑转换的协程
    /// </summary>
    /// <param name="targetAlpha">目标alpha</param>
    /// <returns></returns>
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0;

        while ( timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
