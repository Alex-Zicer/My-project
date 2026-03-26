using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 对话运行服务：加载对话图、驱动状态机、控制 UI 刷新与输入锁定。
public class DialogueService : MonoBehaviour
{
    // 单例实例。
    private static DialogueService _instance;

    [Header("打字机设置")]
    // 打字机速度（每秒字符数）。
    [SerializeField] private float charactersPerSecond = 40f;

    // 是否已存在实例（避免外部误触发创建）。
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

    // 当前是否处于对话运行中。
    public bool IsRunning => _state != DialogueRunState.Idle;

    // 对话开始事件。
    public event Action OnDialogueStarted;
    // 对话结束事件。
    public event Action OnDialogueEnded;

    // 对话数据 Provider 注册表。
    private DialogueProviderRegistry _providerRegistry;
    // 当前绑定的对话视图。
    private IDialogueView _view;
    // 当前运行中的对话图。
    private DialogueGraph _graph;
    // 当前节点数据。
    private DialogueNodeData _currentNode;
    // 当前运行状态。
    private DialogueRunState _state = DialogueRunState.Idle;

    // 打字协程句柄。
    private Coroutine _typingCoroutine;
    // 当前完整句文本。
    private string _fullLine = string.Empty;
    // 打字期间收到“下一步”请求时的快进标记。
    private bool _skipTypingRequested;

    // 是否由本服务锁定了玩家输入。
    private bool _hasLockedInput;
    // 结束流程重入保护。
    private bool _isEnding;
    // 当前对话对应的路由结果（用于结束回写）。
    private DialogueRouteResult _activeRouteResult;
    // 启动前暂存的路由结果。
    private DialogueRouteResult _pendingRouteResult;

    // 延迟创建服务实例，支持场景零接线启动。
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueService");
        _instance = go.AddComponent<DialogueService>();
        DontDestroyOnLoad(go);
    }

    // 保障单例唯一并初始化 Provider 注册表。
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        _providerRegistry = new DialogueProviderRegistry();
    }

    // 清理单例与 View 事件订阅，避免悬挂回调。
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        UnsubscribeViewEvents(_view);
    }

    // 绑定对话视图并接入输入事件。
    public void BindView(IDialogueView view)
    {
        if (ReferenceEquals(_view, view)) return;

        UnsubscribeViewEvents(_view);
        _view = view;
        SubscribeViewEvents(_view);
    }

    // 解绑对话视图；若正在对话则安全收尾。
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

    // 用对话引用启动对话（不携带路由上下文）。
    public bool StartDialogue(DialogueReference reference)
    {
        return StartDialogue(reference, null);
    }

    // 用对话引用启动对话，并携带路由上下文用于结束回写。
    public bool StartDialogue(DialogueReference reference, DialogueRouteResult routeResult)
    {
        if (IsRunning) return false;
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

        _pendingRouteResult = routeResult;
        bool started = StartDialogue(graph);
        if (!started)
        {
            _pendingRouteResult = null;
        }
        return started;
    }

    // 直接用已构建的对话图启动运行流程。
    public bool StartDialogue(DialogueGraph graph)
    {
        DialogueRouteResult routeContext = _pendingRouteResult;
        _pendingRouteResult = null;

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

        _graph = graph;
        _activeRouteResult = routeContext;
        _state = DialogueRunState.WaitingNext;

        SetPlayerInputEnabled(false);
        _hasLockedInput = true;
        OpenDialoguePage();

        OnDialogueStarted?.Invoke();
        EnterNodeById(_graph.StartNodeId);
        return true;
    }

    // 结束对话并清理运行时上下文与输入锁。
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
                SetPlayerInputEnabled(true);
                _hasLockedInput = false;
            }

            _graph = null;
            _currentNode = null;
            _fullLine = string.Empty;
            _skipTypingRequested = false;
            _state = DialogueRunState.Idle;

            DialogueRouteResult completedRoute = _activeRouteResult;
            _activeRouteResult = null;
            _pendingRouteResult = null;
            if (completedRoute != null && completedRoute.IsValid)
            {
                DialogueRouterService.Instance.NotifyDialogueCompleted(completedRoute);
            }

            OnDialogueEnded?.Invoke();
        }
        finally
        {
            _isEnding = false;
        }
    }

    // 确保当前场景有可用对话视图；若未绑定则尝试自动查找并绑定。
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

    // 跳转到指定节点并刷新说话人与文本显示。
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

    // 启动当前节点文本的打字机流程。
    private void BeginTyping(string line)
    {
        // 启动新句前先停止旧协程，避免并发写 UI。
        StopTypingCoroutine();

        _fullLine = line ?? string.Empty;
        _skipTypingRequested = false;
        _state = DialogueRunState.Typing;

        _typingCoroutine = StartCoroutine(TypeLineRoutine());
    }

    // 按配置速度逐字显示当前句，支持“下一步”快进补全。
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

    // 处理打字完成后的状态迁移（进入选项或等待下一句）。
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

    // 处理视图层发来的“下一步”输入请求。
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

    // 处理视图层发来的选项点击请求。
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

    // 按线性 nextNodeId 推进到下一节点或结束对话。
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

    // 打开对话页面（优先交给页面管理器）。
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

    // 关闭对话页面（优先交给页面管理器）。
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

    // 统一控制玩家输入开关（对话期间锁定，结束后恢复）。
    private void SetPlayerInputEnabled(bool enabled)
    {
        // 通过 PlayerController 统一控制输入开关。
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        player.SetInputEnabled(enabled);
    }

    // 停止当前打字协程并清空句柄。
    private void StopTypingCoroutine()
    {
        // 协程句柄清空可避免重复 Stop 引发歧义。
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    // 订阅视图输入事件。
    private void SubscribeViewEvents(IDialogueView view)
    {
        if (view == null) return;
        view.OnNextRequested += HandleNextRequested;
        view.OnChoiceSelected += HandleChoiceSelected;
    }

    // 取消订阅视图输入事件。
    private void UnsubscribeViewEvents(IDialogueView view)
    {
        if (view == null) return;
        view.OnNextRequested -= HandleNextRequested;
        view.OnChoiceSelected -= HandleChoiceSelected;
    }
}
