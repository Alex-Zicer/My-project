using UnityEngine;

/// <summary>
/// 主菜单页面管理器。只管理主菜单场景下的页面（如主菜单页、设置页等），由 UIManager 统一调度。
/// </summary>
public class MainMenuPageManager : BasePageManager
{
    /// <summary>
    /// 在 Inspector 中点击“重置”或首次添加组件时，自动设为管理主菜单且默认打开 MainMenuPage。
    /// </summary>
    private void Reset()
    {
        Configure(UIPageCategory.MainMenu, "MainMenuPage");
    }

    /// <summary>
    /// 主菜单初始化：先恢复时间尺度（避免从游戏内返回时仍暂停），再执行基类的关闭全部并打开默认页。
    /// </summary>
    public override void Initialize()
    {
        Time.timeScale = 1;
        base.Initialize();
    }

    /// <summary>
    /// 打开主菜单页。可用于“返回主菜单”等按钮事件。
    /// </summary>
    public void OpenMainMenu()
    {
        GoToPageByName("MainMenuPage");
    }
}
