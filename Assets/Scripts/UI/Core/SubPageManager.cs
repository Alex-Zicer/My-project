using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子页面管理器。用于管理某一“大页”下的多个子页（如设置下的音效/界面，背包下的武器/材料/道具等）。
/// 每个大页挂一个 SubPageManager，彼此互不影响（不再由 UIManager 全局分发）。
/// </summary>
public class SubPageManager : MonoBehaviour
{
    [Header("子页面配置")]
    [Tooltip("默认打开的子页面 Key。为空时会尝试按索引 0 打开 orderedSubPageKeys 对应页面。")]
    [SerializeField] private string defaultSubPageKey = "";
    [Tooltip("按 Tab 顺序排列的子页面 Key 列表，ShowSubPage(int) 会按索引取这里的 Key。")]
    [SerializeField] private List<string> orderedSubPageKeys = new List<string>();
    [Tooltip("当本物体启用时是否自动初始化并打开默认子页面。")]
    [SerializeField] private bool initializeOnEnable = true;

    // 仅缓存当前 SubPageManager 子层级下的子页面（不会跨页面组）
    private readonly Dictionary<string, UIPage> subPageMap = new Dictionary<string, UIPage>();
    private UIPage currentSubPage;

    private void OnEnable()
    {
        if (initializeOnEnable)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 初始化当前子页面组：收集本组子页面，关闭全部后打开默认子页面。
    /// </summary>
    public void Initialize()
    {
        RebuildSubPageMap();
        CloseAllSubPages();

        if (!string.IsNullOrWhiteSpace(defaultSubPageKey))
        {
            ShowSubPage(defaultSubPageKey);
            return;
        }

        if (orderedSubPageKeys.Count > 0)
        {
            ShowSubPage(0);
        }
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

        ShowSubPage(orderedSubPageKeys[index]);
    }

    /// <summary>
    /// 按页面 Key 显示子页面。适合按钮直接绑定到某个具体子页。
    /// </summary>
    /// <param name="subPageKey">子页的 PageKey</param>
    public void ShowSubPage(string subPageKey)
    {
        RebuildSubPageMap();

        if (!subPageMap.TryGetValue(subPageKey, out UIPage targetSubPage))
        {
            Debug.LogWarning($"未找到子页面：{subPageKey}");
            return;
        }

        // 仅关闭当前组里正在显示的子页，不影响父页面与其他子页面组
        if (currentSubPage != null && currentSubPage != targetSubPage)
        {
            currentSubPage.Close();
        }

        targetSubPage.Open();
        currentSubPage = targetSubPage;
        targetSubPage.SetSelectedUIToDefault();
    }

    /// <summary>
    /// 关闭当前页面组中的所有子页面。
    /// </summary>
    public void CloseAllSubPages()
    {
        RebuildSubPageMap();
        foreach (var page in subPageMap.Values)
        {
            if (page != null) page.Close();
        }
        currentSubPage = null;
    }

    /// <summary>
    /// 只收集当前物体子层级中的 UIPage，保证每组 SubPageManager 只管理自己那组页面。
    /// </summary>
    private void RebuildSubPageMap()
    {
        subPageMap.Clear();
        UIPage[] childPages = GetComponentsInChildren<UIPage>(true);

        foreach (var page in childPages)
        {
            if (page == null) continue;

            // 若父页面本身也挂了 UIPage，这里排除掉父页面，避免把“大页”当子页管理
            if (page.gameObject == gameObject) continue;

            string key = page.PageKey;
            if (subPageMap.ContainsKey(key))
            {
                Debug.LogWarning($"子页面 Key 重复：{key}，后续页面将被忽略。");
                continue;
            }

            subPageMap.Add(key, page);
        }
    }
}
