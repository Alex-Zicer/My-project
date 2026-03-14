using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 页面管理器基类。负责通过 Key 管理一批 UIPage，支持跳转、返回、关闭全部等通用逻辑。
/// 主菜单与游戏内分别由 MainMenuPageManager、InGamePageManager 继承实现。
/// </summary>
public class BasePageManager : MonoBehaviour
{
    [Header("页面管理")]
    [Tooltip("该管理器只接管对应分类的页面。")]
    [SerializeField] private UIPageCategory managedCategory = UIPageCategory.MainMenu;
    [Tooltip("首次初始化时要打开的页面 Key,为空则不自动打开。")]
    [SerializeField] private string defaultPageKey = "MainMenuPage";
    [Tooltip("仅用于调试查看。运行时会由 UIManager 自动刷新。")]
    [SerializeField] private List<UIPage> pageList = new List<UIPage>();

    /// <summary> 页面 Key -> UIPage，用于按名称快速查找。 </summary>
    private readonly Dictionary<string, UIPage> pageMap = new Dictionary<string, UIPage>();
    /// <summary> 已打开页面的历史栈，用于 Back 返回上一页。 </summary>
    private readonly Stack<UIPage> historyStack = new Stack<UIPage>();

    /// <summary>
    /// 配置本管理器管理的页面分类与默认打开页。
    /// 由 UIManager 或子类在 Reset/Awake 时调用。
    /// </summary>
    /// <param name="category">页面分类（主菜单/游戏内/子页面等）</param>
    /// <param name="firstPageKey">首次初始化时打开的页面 Key，空则不自动打开</param>
    public void Configure(UIPageCategory category, string firstPageKey = "")
    {
        managedCategory = category;
        defaultPageKey = firstPageKey;
    }

    /// <summary>
    /// 注册一批页面到本管理器。会按 managedCategory 过滤，并构建 Key 字典。
    /// 场景加载后由 UIManager 调用。
    /// </summary>
    /// <param name="scenePages">当前场景中收集到的所有 UIPage（本管理器只保留匹配分类的）</param>
    public void RegisterPages(List<UIPage> scenePages)
    {
        BuildPageMap(scenePages, true);
    }

    /// <summary>
    /// 初始化：先确保页面字典已就绪，关闭全部页面，再打开默认页（若有）。
    /// 子类可重写以加入主菜单/游戏内特有逻辑（如恢复时间尺度）。
    /// </summary>
    public virtual void Initialize()
    {
        EnsurePageMapReady();
        CloseAll();
        if (!string.IsNullOrWhiteSpace(defaultPageKey))
        {
            GoToPageByName(defaultPageKey);
        }
    }

    /// <summary>
    /// 判断本管理器中是否包含指定 Key 的页面。
    /// </summary>
    /// <param name="pageKey">页面 Key</param>
    /// <returns>存在返回 true，否则 false</returns>
    public bool HasPage(string pageKey)
    {
        EnsurePageMapReady();
        return !string.IsNullOrWhiteSpace(pageKey) && pageMap.ContainsKey(pageKey);
    }

    /// <summary>
    /// 通过页面 Key 跳转到指定页面。当前顶页会关闭但保留在栈中，新页打开并入栈。
    /// </summary>
    /// <param name="pageName">目标页面的 Key（与 UIPage.PageKey 一致）</param>
    public virtual void GoToPageByName(string pageName)
    {
        EnsurePageMapReady();
        if (!pageMap.TryGetValue(pageName, out UIPage target))
        {
            Debug.Log($"未找到页面：{pageName}");
            return;
        }

        // 若有当前显示的页面，先关闭（保留在栈中）
        if (historyStack.Count > 0)
        {
            historyStack.Peek().Close();
        }

        target.Open();
        historyStack.Push(target);
        target.SetSelectedUIToDefault();
    }

    /// <summary>
    /// 返回上一页：弹出当前页并关闭，显示栈顶的前一页。
    /// 若栈内只有一页则不操作。
    /// </summary>
    public virtual void Back()
    {
        if (historyStack.Count <= 1) return;

        UIPage currentPage = historyStack.Pop();
        currentPage.Close();

        UIPage previousPage = historyStack.Peek();
        previousPage.Open();
        previousPage.SetSelectedUIToDefault();
    }

    /// <summary>
    /// 关闭本管理器下的所有页面，并清空历史栈。
    /// </summary>
    public virtual void CloseAll()
    {
        EnsurePageMapReady();
        foreach (var page in pageList)
        {
            if (page != null) page.Close();
        }
        historyStack.Clear();
    }

    /// <summary>
    /// 确保 pageMap 已构建。若尚未注册过页面，则尝试用 pageList 或当前场景中的 UIPage 构建。
    /// </summary>
    private void EnsurePageMapReady()
    {
        if (pageMap.Count > 0) return;

        if (pageList.Count > 0)
        {
            BuildPageMap(pageList, false);
            return;
        }

        var pagesInScene = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        BuildPageMap(new List<UIPage>(pagesInScene), false);
    }

    /// <summary>
    /// 根据源页面列表构建 pageMap：只保留与 managedCategory 匹配且同场景的页面；Key 为 PageKey，重复则警告并忽略。
    /// </summary>
    /// <param name="sourcePages">源页面列表（来自场景或 UIManager 传入）</param>
    /// <param name="clearSource">为 true 时先清空 pageList 再只加入本次匹配的页面；为 false 时不改 pageList</param>
    private void BuildPageMap(List<UIPage> sourcePages, bool clearSource)
    {
        if (clearSource) pageList.Clear();
        pageMap.Clear();
        historyStack.Clear();

        if (sourcePages == null || sourcePages.Count == 0) return;

        // 先按分类筛选本场景内的页面
        List<UIPage> matchedPages = new List<UIPage>();
        foreach (var page in sourcePages)
        {
            if (page == null || page.gameObject.scene != gameObject.scene) continue;

            if (page.Category == managedCategory)
            {
                matchedPages.Add(page);
            }
        }

        // 向后兼容：若没有任何页面带本分类，则把同场景的页面都当作本管理器管理
        if (matchedPages.Count == 0)
        {
            foreach (var page in sourcePages)
            {
                if (page != null && page.gameObject.scene == gameObject.scene)
                {
                    matchedPages.Add(page);
                }
            }
        }

        foreach (var page in matchedPages)
        {
            if (page == null) continue;

            if (clearSource) pageList.Add(page);
            string key = page.PageKey;

            if (pageMap.ContainsKey(key))
            {
                Debug.LogWarning($"页面 Key 重复：{key}，后续页面将被忽略。");
                continue;
            }

            pageMap.Add(key, page);
        }
    }
}
