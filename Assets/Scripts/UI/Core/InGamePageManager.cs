using UnityEngine;

/// <summary>
/// 游戏内页面管理器（InGame 分类）。
/// 负责统一处理：
/// 1. 暂停页面的开关与 Time.timeScale 控制；
/// 2. 背包热键（B）的打开/关闭；
/// 3. 对话页面的打开/关闭（由 DialogueService 间接调用）；
/// 4. 对话运行期间屏蔽背包/暂停热键，避免交互冲突。
/// </summary>
public class InGamePageManager : BasePageManager
{
    // 暂停页面的 PageKey（需与 UIPage 配置一致）。
    [Header("Pause")]
    [SerializeField] private string pausePageKey = "PausePage";

    // 对话页面的 PageKey（需与 UIPage 配置一致）。
    [Header("Dialogue")]
    [SerializeField] private string dialoguePageKey = "DialoguePage";

    // 背包页面的 PageKey（需与 UIPage 配置一致）。
    [Header("Bag")]
    [SerializeField] private string bagPageKey = "BagPage";

    // 当前是否处于暂停状态。
    // true: 游戏暂停；false: 游戏运行。
    public bool IsPause { get; private set; }
    // 背包页输入锁标记：true 表示当前由“背包页面”触发了输入禁用。
    // 用于防止重复 Disable/Enable，并在离开背包时按条件恢复输入。
    private bool _hasLockedInputByBag;

    private void Reset()
    {
        // 组件重置时写入默认分类配置，确保只管理 InGame 页面。
        Configure(UIPageCategory.InGame, "");
    }

    public override void Initialize()
    {
        // 初始化时确保游戏恢复到正常运行态，防止场景重载后残留暂停状态。
        IsPause = false;
        _hasLockedInputByBag = false;
        Time.timeScale = 1;
        base.Initialize();
    }

    /// <summary>
    /// 跳转页面后同步背包输入锁状态，确保通过任意入口打开背包都能禁用玩家输入。
    /// </summary>
    /// <param name="pageName">目标页面 Key。</param>
    public override void GoToPageByName(string pageName)
    {
        base.GoToPageByName(pageName);
        SyncBagInputLock();
    }

    /// <summary>
    /// 返回上一页后同步背包输入锁状态。
    /// </summary>
    public override void Back()
    {
        base.Back();
        SyncBagInputLock();
    }

    /// <summary>
    /// 关闭当前页后同步背包输入锁状态。
    /// </summary>
    public override void CloseCurrentPage()
    {
        base.CloseCurrentPage();
        SyncBagInputLock();
    }

    // 暂停开关入口：
    // 对话进行中时不允许暂停，避免输入冲突和页面层级冲突。
    public void TogglePause()
    {
        if (IsDialogueRunning()) return;

        if (!IsPause) PauseGame();
        else ResumeGame();
    }

    // 执行暂停：
    // 1) 打开暂停页；2) 禁用玩家输入；3) 时间缩放置 0。
    private void PauseGame()
    {
        if (!HasPage(pausePageKey))
        {
            Debug.LogWarning($"未找到暂停页面: {pausePageKey}");
            return;
        }

        IsPause = true;
        SetPlayerInputEnabled(false);
        Time.timeScale = 0;
        GoToPageByName(pausePageKey);
    }

    // 恢复游戏：
    // 1) 时间缩放恢复；2) 玩家输入恢复；3) 返回上一页（关闭暂停页）。
    public void ResumeGame()
    {
        IsPause = false;
        Time.timeScale = 1;
        SetPlayerInputEnabled(true);
        CloseCurrentPage();
    }

    // 打开对话页：
    // 通常由 DialogueService 在开始对话时调用。
    public void OpenDialoguePage()
    {
        if (!HasPage(dialoguePageKey))
        {
            Debug.LogWarning($"未找到对话页面: {dialoguePageKey}");
            return;
        }

        GoToPageByName(dialoguePageKey);
    }

    // 关闭对话页：
    // 仅当当前页就是对话页时执行，避免误关其他页面。
    public void CloseDialoguePage()
    {
        if (CurrentPageKey == dialoguePageKey)
        {
            CloseCurrentPage();
        }
    }

    // 统一控制玩家输入开关，供暂停逻辑复用。
    private void SetPlayerInputEnabled(bool enabled)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        player.SetInputEnabled(enabled);
    }

    // 根据当前页面同步背包输入锁：
    // 在背包页时锁输入；离开背包页时仅在非暂停、非对话状态下恢复输入。
    private void SyncBagInputLock()
    {
        // 当前页面是背包页：仅在首次进入时执行一次输入禁用。
        bool shouldLock = CurrentPageKey == bagPageKey;
        if (shouldLock)
        {
            if (_hasLockedInputByBag) return;
            _hasLockedInputByBag = true;
            SetPlayerInputEnabled(false);
            return;
        }

        // 当前不在背包页：若此前由背包加过锁，则尝试释放该锁。
        if (!_hasLockedInputByBag) return;
        _hasLockedInputByBag = false;

        // 仅当不存在“暂停/对话”这两类更高优先级锁时，才恢复玩家输入。
        if (!IsPause && !IsDialogueRunning())
        {
            SetPlayerInputEnabled(true);
        }
    }

    private void Update()
    {
        // 对话运行期间屏蔽背包热键，避免与对话输入抢占。
        if (IsDialogueRunning()) return;

        // B 键切换背包：
        // 当前已在背包页 -> 关闭当前页；否则 -> 打开背包页。
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (CurrentPageKey == bagPageKey) CloseCurrentPage();
            else GoToPageByName(bagPageKey);
        }
    }

    // 检查对话系统是否正在运行。
    // 这里通过 DialogueService 的单例状态做只读判断，不触发任何副作用。
    private bool IsDialogueRunning()
    {
        return DialogueService.HasInstance && DialogueService.Instance.IsRunning;
    }
}
