using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueService : MonoBehaviour
{
    // 单例实例引用。
    private static DialogueService _instance;

    [Header("Typing Settings")]
    [SerializeField] private float charactersPerSecond = 40f;

    // HasInstance 状态开关。
    public static bool HasInstance => _instance != null;

    public static DialogueService Instance
    {
        get
        {
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (_instance == null)
            {
                CreateInstance();
            }

            return _instance;
        }
    }

    // IsRunning 状态开关。
    public bool IsRunning => _state != DialogueRunState.Idle;

    // OnDialogueStarted 事件。
    public event Action OnDialogueStarted;

    // OnDialogueEnded 事件。
    public event Action OnDialogueEnded;

    // _providerRegistry 组件或配置引用。
    private DialogueProviderRegistry _providerRegistry;

    // _view 运行时字段。
    private IDialogueView _view;

    // _graph 运行时字段。
    private DialogueGraph _graph;

    // _currentNode 运行时字段。
    private DialogueNodeData _currentNode;

    // _state 运行时字段。
    private DialogueRunState _state = DialogueRunState.Idle;

    // _typingCoroutine 运行时字段。
    private Coroutine _typingCoroutine;

    // _fullLine 运行时字段。
    private string _fullLine = string.Empty;

    // _skipTypingRequested 运行时字段。
    private bool _skipTypingRequested;

    // _hasLockedInput 状态开关。
    private bool _hasLockedInput;

    // _isEnding 状态开关。
    private bool _isEnding;

    // _activeRouteResult 运行时字段。
    private DialogueRouteResult _activeRouteResult;

    // _pendingRouteResult 运行时字段。
    private DialogueRouteResult _pendingRouteResult;

    /// <summary>
    /// 创建单例实例并设置为跨场景持久对象。
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueService");
        _instance = go.AddComponent<DialogueService>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// 初始化组件并确保运行时状态有效。
    /// </summary>
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

    /// <summary>
    /// 清理实例引用与事件绑定，防止悬挂回调。
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        UnsubscribeViewEvents(_view);
    }

    /// <summary>
    /// 绑定视图并注册输入事件。
    /// </summary>
    public void BindView(IDialogueView view)
    {
        if (ReferenceEquals(_view, view))
        {
            return;
        }

        UnsubscribeViewEvents(_view);
        _view = view;
        SubscribeViewEvents(_view);
    }

    /// <summary>
    /// 解绑视图并取消输入事件注册。
    /// </summary>
    public void UnbindView(IDialogueView view)
    {
        if (!ReferenceEquals(_view, view))
        {
            return;
        }

        UnsubscribeViewEvents(_view);
        _view = null;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (IsRunning && !_isEnding)
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 启动对话流程并进入起始节点。
    /// </summary>
    public bool StartDialogue(DialogueReference reference)
    {
        return StartDialogue(reference, null);
    }

    /// <summary>
    /// 启动对话流程并进入起始节点。
    /// </summary>
    public bool StartDialogue(DialogueReference reference, DialogueRouteResult routeResult)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (IsRunning)
        {
            return false;
        }

        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null && UIManager.Instance.InGamePage.IsPause)
        {
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (reference == null)
        {
            Debug.LogWarning("[DialogueService] Cannot start dialogue: reference is null.");
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_providerRegistry == null)
        {
            _providerRegistry = new DialogueProviderRegistry();
        }

        if (!_providerRegistry.TryLoad(reference, out DialogueGraph graph, out string error))
        {
            Debug.LogWarning($"[DialogueService] Failed to load dialogue: {error}");
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

    /// <summary>
    /// 启动对话流程并进入起始节点。
    /// </summary>
    public bool StartDialogue(DialogueGraph graph)
    {
        DialogueRouteResult routeContext = _pendingRouteResult;
        _pendingRouteResult = null;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (IsRunning)
        {
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (graph == null)
        {
            Debug.LogWarning("[DialogueService] Cannot start dialogue: graph is null.");
            return false;
        }

        if (!EnsureView(out string viewError))
        {
            Debug.LogWarning($"[DialogueService] Cannot start dialogue: {viewError}");
            return false;
        }

        _graph = graph;
        _activeRouteResult = routeContext;
        _state = DialogueRunState.WaitingNext;

        SetPlayerInputEnabled(false);
        _hasLockedInput = true;
        OpenDialoguePage();

        OnDialogueStarted?.Invoke();

        string startNodeId = routeContext?.StartNodeId;
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(startNodeId))
        {
            startNodeId = _graph.StartNodeId;
        }
        EnterNodeById(startNodeId);
        return true;
    }

    /// <summary>
    /// 结束当前对话并回收运行时状态。
    /// </summary>
    public void EndDialogue()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (!IsRunning || _isEnding)
        {
            return;
        }

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

    /// <summary>
    /// 确保当前场景中存在可用的对话视图。
    /// </summary>
    private bool EnsureView(out string error)
    {
        if (_view != null)
        {
            error = string.Empty;
            return true;
        }

        MonoBehaviour[] allBehaviours =
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = allBehaviours[i];
            if (behaviour is IDialogueView dialogueView)
            {
                BindView(dialogueView);
                break;
            }
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_view == null)
        {
            error = "No IDialogueView implementation found in current scene.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// 切换到指定节点并刷新说话人与文本。
    /// </summary>
    private void EnterNodeById(string nodeId)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_graph == null || !_graph.TryGetNode(nodeId, out DialogueNodeData node))
        {
            Debug.LogWarning($"[DialogueService] Missing node '{nodeId}', ending dialogue.");
            EndDialogue();
            return;
        }

        _currentNode = node;
        _view?.ClearChoices();
        _view?.SetSpeaker(node.speakerName, node.speakerPortrait);
        BeginTyping(node.content ?? string.Empty);
    }

    /// <summary>
    /// 开始当前文本的打字机播放。
    /// </summary>
    private void BeginTyping(string line)
    {
        StopTypingCoroutine();

        _fullLine = line ?? string.Empty;
        _skipTypingRequested = false;
        _state = DialogueRunState.Typing;

        _typingCoroutine = StartCoroutine(TypeLineRoutine());
    }

    /// <summary>
    /// 按打字机速度逐步输出文本内容。
    /// </summary>
    private IEnumerator TypeLineRoutine()
    {
        if (string.IsNullOrEmpty(_fullLine))
        {
            _view?.SetContent(string.Empty, false);
            OnTypingCompleted();
            yield break;
        }

        float safeCps = Mathf.Max(1f, charactersPerSecond);
        float interval = 1f / safeCps;
        float timer = 0f;
        int visible = 0;
        int total = _fullLine.Length;

        while (visible < total)
        {
            if (_skipTypingRequested)
            {
                visible = total;
            }
            else
            {
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

    /// <summary>
    /// 文本播放完成后切换到下一交互状态。
    /// </summary>
    private void OnTypingCompleted()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        bool hasChoices = _currentNode.choices != null && _currentNode.choices.Count > 0;
        if (hasChoices)
        {
            _state = DialogueRunState.WaitingChoice;
            var vm = new List<DialogueChoiceViewModel>(_currentNode.choices.Count);
            // 遍历集合并逐项处理当前业务。
            for (int i = 0; i < _currentNode.choices.Count; i++)
            {
                DialogueChoiceData choice = _currentNode.choices[i];
                vm.Add(new DialogueChoiceViewModel(i, choice != null ? choice.choiceText : string.Empty));
            }

            _view?.ShowChoices(vm);
            return;
        }

        _state = DialogueRunState.WaitingNext;
        _view?.ShowContinueButton();
    }

    /// <summary>
    /// 响应继续输入并推进对话流程。
    /// </summary>
    private void HandleNextRequested()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (!IsRunning)
        {
            return;
        }

        if (_state == DialogueRunState.Typing)
        {
            _skipTypingRequested = true;
            return;
        }

        if (_state != DialogueRunState.WaitingNext)
        {
            return;
        }

        AdvanceToNextNode();
    }

    /// <summary>
    /// 处理选项点击并跳转目标节点。
    /// </summary>
    private void HandleChoiceSelected(int choiceIndex)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (!IsRunning || _state != DialogueRunState.WaitingChoice || _currentNode == null)
        {
            return;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_currentNode.choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
        {
            Debug.LogWarning($"[DialogueService] Invalid choice index: {choiceIndex}");
            return;
        }

        DialogueChoiceData choice = _currentNode.choices[choiceIndex];
        _view?.ClearChoices();
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (choice == null || string.IsNullOrWhiteSpace(choice.nextNodeId))
        {
            EndDialogue();
            return;
        }

        EnterNodeById(choice.nextNodeId);
    }

    /// <summary>
    /// 按线性后继节点推进对话。
    /// </summary>
    private void AdvanceToNextNode()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        if (_currentNode.isEndNode)
        {
            EndDialogue();
            return;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(_currentNode.nextNodeId))
        {
            EndDialogue();
            return;
        }

        EnterNodeById(_currentNode.nextNodeId);
    }

    /// <summary>
    /// 打开对话页面容器。
    /// </summary>
    private void OpenDialoguePage()
    {
        bool openedByPageManager = false;
        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null)
        {
            UIManager.Instance.InGamePage.OpenDialoguePage();
            openedByPageManager = true;
        }

        if (!openedByPageManager)
        {
            _view?.Open();
        }
    }

    /// <summary>
    /// 关闭对话页面容器。
    /// </summary>
    private void CloseDialoguePage()
    {
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

    /// <summary>
    /// 启用或禁用玩家控制输入。
    /// </summary>
    private void SetPlayerInputEnabled(bool enabled)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (player == null)
        {
            return;
        }

        player.SetInputEnabled(enabled);
    }

    /// <summary>
    /// 安全停止当前打字协程。
    /// </summary>
    private void StopTypingCoroutine()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    /// <summary>
    /// 订阅视图层输入事件。
    /// </summary>
    private void SubscribeViewEvents(IDialogueView view)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (view == null)
        {
            return;
        }

        view.OnNextRequested += HandleNextRequested;
        view.OnChoiceSelected += HandleChoiceSelected;
    }

    /// <summary>
    /// 取消订阅视图层输入事件。
    /// </summary>
    private void UnsubscribeViewEvents(IDialogueView view)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (view == null)
        {
            return;
        }

        view.OnNextRequested -= HandleNextRequested;
        view.OnChoiceSelected -= HandleChoiceSelected;
    }
}
