using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 对话运行服务（核心协调器）：
// 1) 负责从引用加载对话图；
// 2) 维护对话状态机（Typing / WaitingNext / WaitingChoice）；
// 3) 驱动 IDialogueView 更新展示；
// 4) 管理对话期间的玩家输入锁定与页面开关。
public class DialogueService : MonoBehaviour
{
    // 单例实例：保证场景中只有一个对话运行服务。
    private static DialogueService _instance;

    [Header("打字机设置")]
    // 每秒显示字符数（基于 unscaledDeltaTime，不受暂停/慢速影响）。
    [SerializeField] private float charactersPerSecond = 40f;

    // 供外部安全判断是否已经创建实例（避免访问 Instance 时隐式创建）。
    public static bool HasInstance => _instance != null;

    public static DialogueService Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    public bool IsRunning => _state != DialogueRunState.Idle;

    // 生命周期事件：可供其他系统监听“进入对话/退出对话”时机。
    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    // 数据加载入口（Provider 注册表）。
    private DialogueProviderRegistry _providerRegistry;
    // 当前绑定的展示层接口（可热切换）。
    private IDialogueView _view;
    // 当前运行中的对话图。
    private DialogueGraph _graph;
    // 当前所在节点。
    private DialogueNodeData _currentNode;
    // 状态机当前状态。
    private DialogueRunState _state = DialogueRunState.Idle;

    // 当前打字机协程句柄。
    private Coroutine _typingCoroutine;
    // 当前完整句文本（用于打字机逐字显示与一键补全）。
    private string _fullLine = string.Empty;
    // 玩家在打字中按了“下一步”时置 true，下一帧立即补全。
    private bool _skipTypingRequested;

    // 是否由本服务锁过玩家输入，用于结束时对称恢复。
    private bool _hasLockedInput;
    // 防重入保护：避免 EndDialogue 在同一帧被重复触发。
    private bool _isEnding;

    // 延迟创建单例：
    // 当外部首次访问 Instance 且场景尚未放置服务时，自动创建常驻对象。
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueService");
        _instance = go.AddComponent<DialogueService>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // 场景中若出现重复实例，仅保留先创建的那个。
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        _providerRegistry = new DialogueProviderRegistry();
    }

    private void OnDestroy()
    {
        // 对象销毁时清理单例引用和事件绑定，避免悬挂回调。
        if (_instance == this)
        {
            _instance = null;
        }
        UnsubscribeViewEvents(_view);
    }

    // 绑定展示层接口：
    // 运行时可以替换不同 View（正式 UI、调试 UI），Service 不关心具体实现。
    public void BindView(IDialogueView view)
    {
        if (ReferenceEquals(_view, view)) return;

        UnsubscribeViewEvents(_view);
        _view = view;
        SubscribeViewEvents(_view);
    }

    // 解绑展示层接口：
    // 若解绑发生在对话进行中，立即安全结束，避免无界面时继续推进状态机。
    public void UnbindView(IDialogueView view)
    {
        if (!ReferenceEquals(_view, view)) return;
        UnsubscribeViewEvents(_view);
        _view = null;

        if (IsRunning && !_isEnding)
        {
            EndDialogue();
        }
    }

    // 通过 DialogueReference 开启对话：
    // 会走 Provider 加载链与 fallback 逻辑，成功后进入统一运行流程。
    public bool StartDialogue(DialogueReference reference)
    {
        if (IsRunning) return false;
        // 与项目现有暂停系统对齐：暂停时禁止开始对话。
        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null && UIManager.Instance.InGamePage.IsPause)
        {
            return false;
        }
        if (reference == null)
        {
            Debug.LogWarning("[DialogueService] 无法开启对话: 引用为空。");
            return false;
        }

        if (_providerRegistry == null)
        {
            _providerRegistry = new DialogueProviderRegistry();
        }

        if (!_providerRegistry.TryLoad(reference, out DialogueGraph graph, out string error))
        {
            Debug.LogWarning($"[DialogueService] 加载对话失败: {error}");
            return false;
        }

        return StartDialogue(graph);
    }

    // 直接使用已构建好的对话图开启（常用于调试或自定义来源）。
    public bool StartDialogue(DialogueGraph graph)
    {
        if (IsRunning) return false;
        if (graph == null)
        {
            Debug.LogWarning("[DialogueService] 无法开启对话: 对话图为空。");
            return false;
        }

        if (!EnsureView(out string viewError))
        {
            Debug.LogWarning($"[DialogueService] 无法开启对话: {viewError}");
            return false;
        }

        // 初始化运行时上下文。
        _graph = graph;
        _state = DialogueRunState.WaitingNext;

        // 对话开始时锁输入，防止移动/战斗输入穿透到游戏逻辑。
        SetPlayerInputEnabled(false);
        _hasLockedInput = true;
        OpenDialoguePage();

        OnDialogueStarted?.Invoke();
        // 进入起始节点后会自动触发打字机流程。
        EnterNodeById(_graph.StartNodeId);
        return true;
    }

    // 结束对话并清理上下文：
    // 停止协程、清空选项、关闭页面、恢复输入、重置状态机。
    public void EndDialogue()
    {
        if (!IsRunning || _isEnding) return;

        _isEnding = true;
        try
        {
            StopTypingCoroutine();
            _view?.ClearChoices();
            CloseDialoguePage();

            if (_hasLockedInput)
            {
                // 只恢复本服务锁定过的输入，避免误改其他系统状态。
                SetPlayerInputEnabled(true);
                _hasLockedInput = false;
            }

            _graph = null;
            _currentNode = null;
            _fullLine = string.Empty;
            _skipTypingRequested = false;
            _state = DialogueRunState.Idle;

            OnDialogueEnded?.Invoke();
        }
        finally
        {
            _isEnding = false;
        }
    }

    /// <summary>
    /// 检查是否实现了 IDialogueView 接口
    /// </summary>
    /// <param name="error">是否报错</param>
    /// <returns></returns>
    private bool EnsureView(out string error)
    {
        // 已绑定时直接通过。
        if (_view != null)
        {
            error = string.Empty;
            return true;
        }

        // 尝试在场景中自动发现 IDialogueView 实现，降低手动接线成本。
        MonoBehaviour[] allBehaviours =
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is IDialogueView dialogueView)
            {
                BindView(dialogueView);
                break;
            }
        }

        if (_view == null)
        {
            error = "场景中未找到对话视图接口实现。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void EnterNodeById(string nodeId)
    {
        // 节点缺失属于数据错误，直接结束避免状态机悬挂。
        if (_graph == null || !_graph.TryGetNode(nodeId, out DialogueNodeData node))
        {
            Debug.LogWarning($"[DialogueService] 缺少节点 '{nodeId}'，对话已结束。");
            EndDialogue();
            return;
        }

        _currentNode = node;
        // 每次进新节点先清旧选项，再刷新说话人与正文。
        _view?.ClearChoices();
        _view?.SetSpeaker(node.speakerName, node.speakerPortrait);
        BeginTyping(node.content ?? string.Empty);
    }

    private void BeginTyping(string line)
    {
        // 启动新句前先停止旧协程，避免并发写 UI。
        StopTypingCoroutine();

        _fullLine = line ?? string.Empty;
        _skipTypingRequested = false;
        _state = DialogueRunState.Typing;

        _typingCoroutine = StartCoroutine(TypeLineRoutine());
    }

    private IEnumerator TypeLineRoutine()
    {
        // 空文本直接完成，避免无意义协程循环。
        if (string.IsNullOrEmpty(_fullLine))
        {
            _view?.SetContent(string.Empty, false);
            OnTypingCompleted();
            yield break;
        }

        // charactersPerSecond 下限保护，防止被配置为 0。
        float safeCps = Mathf.Max(1f, charactersPerSecond);
        float interval = 1f / safeCps;
        float timer = 0f;
        int visible = 0;
        int total = _fullLine.Length;

        while (visible < total)
        {
            if (_skipTypingRequested)
            {
                // 玩家请求快进：本帧直接补完全部字符。
                visible = total;
            }
            else
            {
                // 使用 unscaledDeltaTime，保证暂停菜单/时间缩放下打字机节奏稳定。
                timer += Time.unscaledDeltaTime;
                while (timer >= interval && visible < total)
                {
                    visible++;
                    timer -= interval;
                }
            }

            _view?.SetContent(_fullLine.Substring(0, visible), true);
            yield return null;
        }

        _view?.SetContent(_fullLine, false);
        _typingCoroutine = null;
        OnTypingCompleted();
    }

    private void OnTypingCompleted()
    {
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        // 有分支则进入 WaitingChoice；否则等待下一步输入推进线性流程。
        bool hasChoices = _currentNode.choices != null && _currentNode.choices.Count > 0;
        if (hasChoices)
        {
            _state = DialogueRunState.WaitingChoice;
            var vm = new List<DialogueChoiceViewModel>(_currentNode.choices.Count);
            for (int i = 0; i < _currentNode.choices.Count; i++)
            {
                DialogueChoiceData choice = _currentNode.choices[i];
                vm.Add(new DialogueChoiceViewModel(i, choice != null ? choice.choiceText : string.Empty));
            }
            _view?.ShowChoices(vm);
            return;
        }

        _state = DialogueRunState.WaitingNext;
    }

    private void HandleNextRequested()
    {
        if (!IsRunning) return;

        if (_state == DialogueRunState.Typing)
        {
            // 打字中按下一步：不跳节点，只先补全文字。
            _skipTypingRequested = true;
            return;
        }

        if (_state != DialogueRunState.WaitingNext) return;
        AdvanceToNextNode();
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        if (!IsRunning || _state != DialogueRunState.WaitingChoice || _currentNode == null) return;
        if (_currentNode.choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
        {
            Debug.LogWarning($"[DialogueService] 选项索引无效: {choiceIndex}");
            return;
        }

        DialogueChoiceData choice = _currentNode.choices[choiceIndex];
        _view?.ClearChoices();
        if (choice == null || string.IsNullOrWhiteSpace(choice.nextNodeId))
        {
            // 选项未配置跳转时，按“安全结束”处理。
            EndDialogue();
            return;
        }

        EnterNodeById(choice.nextNodeId);
    }

    private void AdvanceToNextNode()
    {
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        // 结束节点不再继续跳转。
        if (_currentNode.isEndNode)
        {
            EndDialogue();
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentNode.nextNodeId))
        {
            // 非结束节点却没有 nextNodeId 时，以安全结束兜底。
            EndDialogue();
            return;
        }

        EnterNodeById(_currentNode.nextNodeId);
    }

    private void OpenDialoguePage()
    {
        // 优先走项目统一页面管理，保证层级与页面状态一致。
        bool openedByPageManager = false;
        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null)
        {
            UIManager.Instance.InGamePage.OpenDialoguePage();
            openedByPageManager = true;
        }

        // 若未接入页面管理，则直接调用 View 打开，保证最小可运行。
        if (!openedByPageManager)
        {
            _view?.Open();
        }
    }

    private void CloseDialoguePage()
    {
        // 与打开逻辑对称：优先交给页面管理器收口。
        bool closedByPageManager = false;
        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null)
        {
            UIManager.Instance.InGamePage.CloseDialoguePage();
            closedByPageManager = true;
        }

        if (!closedByPageManager)
        {
            _view?.Close();
        }
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        // 通过 PlayerController 统一控制输入开关。
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        player.SetInputEnabled(enabled);
    }

    private void StopTypingCoroutine()
    {
        // 协程句柄清空可避免重复 Stop 引发歧义。
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    private void SubscribeViewEvents(IDialogueView view)
    {
        if (view == null) return;
        view.OnNextRequested += HandleNextRequested;
        view.OnChoiceSelected += HandleChoiceSelected;
    }

    private void UnsubscribeViewEvents(IDialogueView view)
    {
        if (view == null) return;
        view.OnNextRequested -= HandleNextRequested;
        view.OnChoiceSelected -= HandleChoiceSelected;
    }
}
