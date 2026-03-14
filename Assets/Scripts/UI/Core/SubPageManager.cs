using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子页面管理器。用于管理某一“大页”下的多个子页（如设置下的音效/界面，背包下的武器/材料/道具等）。
/// 支持按 Key 或按 Tab 索引切换子页。
/// </summary>
public class SubPageManager : BasePageManager
{
    [Header("子页面配置")]
    [Tooltip("按 Tab 顺序排列的子页面 Key 列表，ShowSubPage(int) 按索引取这里的 Key。")]
    [SerializeField] private List<string> orderedSubPageKeys = new List<string>();

    /// <summary>
    /// Inspector 重置时设为管理子页面，不设默认打开页。
    /// </summary>
    private void Reset()
    {
        Configure(UIPageCategory.SubPage, "");
    }

    /// <summary>
    /// 按 Tab 索引显示子页面。索引对应 orderedSubPageKeys 中的顺序（如 0=音效，1=界面）。
    /// </summary>
    /// <param name="index">子页索引，从 0 开始</param>
    public void ShowSubPage(int index)
    {
        if (index < 0 || index >= orderedSubPageKeys.Count)
        {
            Debug.LogWarning($"子页面索引超出范围：{index}");
            return;
        }

        GoToPageByName(orderedSubPageKeys[index]);
    }

    /// <summary>
    /// 按页面 Key 显示子页面。适合按钮直接绑定到某个具体子页。
    /// </summary>
    /// <param name="subPageKey">子页的 PageKey</param>
    public void ShowSubPage(string subPageKey)
    {
        GoToPageByName(subPageKey);
    }
}
