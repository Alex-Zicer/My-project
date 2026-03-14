using UnityEngine;

/// <summary>
/// 游戏内页面管理器。管理游戏场景中的全屏页（如暂停页、背包页等），并处理暂停/恢复逻辑。
/// </summary>
public class InGamePageManager : BasePageManager
{
    [Header("暂停配置")]
    [SerializeField] private string pausePageKey = "PausePage";
    /// <summary> 当前是否处于暂停状态。 </summary>
    public bool IsPause { get; private set; }

    /// <summary>
    /// Inspector 重置时设为管理游戏内页面，不设默认打开页（游戏内默认无全屏页）。
    /// </summary>
    private void Reset()
    {
        Configure(UIPageCategory.InGame, "");
    }

    /// <summary>
    /// 游戏内初始化：重置暂停状态与时间尺度，再执行基类初始化（若有默认页则打开）。
    /// </summary>
    public override void Initialize()
    {
        IsPause = false;
        Time.timeScale = 1;
        base.Initialize();
    }

    /// <summary>
    /// 切换暂停状态：未暂停则进入暂停并打开暂停页，已暂停则恢复。
    /// </summary>
    public void TogglePause()
    {
        if (!IsPause)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// 进入暂停：时间停止，打开暂停页。
    /// </summary>
    private void PauseGame()
    {
        if (!HasPage(pausePageKey))
        {
            Debug.LogWarning($"未找到暂停页：{pausePageKey}");
            return;
        }

        IsPause = true;
        Time.timeScale = 0;
        GoToPageByName(pausePageKey);
    }

    /// <summary>
    /// 恢复游戏：时间恢复，并执行 Back 关闭暂停页。
    /// </summary>
    public void ResumeGame()
    {
        IsPause = false;
        Time.timeScale = 1;
        Back();
    }
}
