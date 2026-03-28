using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runtime dialogue service that drives dialogue flow and UI interaction.
public class DialogueService : MonoBehaviour
{
    // Singleton instance.
    private static DialogueService _instance;

    [Header("Typing Settings")]
    // Characters shown per second while typing.
    [SerializeField] private float charactersPerSecond = 40f;

    // Whether singleton already exists.
    public static bool HasInstance => _instance != null;

    // Singleton access point.
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

    // Whether dialogue flow is currently running.
    public bool IsRunning => _state != DialogueRunState.Idle;

    // Raised when dialogue starts.
    public event Action OnDialogueStarted;

    // Raised when dialogue ends.
    public event Action OnDialogueEnded;

    // Dialogue data provider registry.
    private DialogueProviderRegistry _providerRegistry;

    // Bound dialogue view.
    private IDialogueView _view;

    // Current dialogue graph.
    private DialogueGraph _graph;

    // Current node in graph.
    private DialogueNodeData _currentNode;

    // Runtime state.
    private DialogueRunState _state = DialogueRunState.Idle;

    // Active typing coroutine.
    private Coroutine _typingCoroutine;

    // Full text line currently being typed.
    private string _fullLine = string.Empty;

    // Skip typing flag requested by player input.
    private bool _skipTypingRequested;

    // Whether player input was locked by this service.
    private bool _hasLockedInput;

    // Re-entrancy guard for end flow.
    private bool _isEnding;

    // Route context used for completion callback.
    private DialogueRouteResult _activeRouteResult;

    // Pending route context captured before graph start.
    private DialogueRouteResult _pendingRouteResult;

    /// <summary>
    /// Creates singleton instance on demand.
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueService");
        _instance = go.AddComponent<DialogueService>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Ensures singleton uniqueness and initializes registry.
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
    /// Clears singleton reference and unbinds view events.
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
    /// Binds dialogue view and subscribes its input events.
    /// </summary>
    /// <param name="view">Dialogue view implementation.</param>
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
    /// Unbinds dialogue view. Ends running dialogue if needed.
    /// </summary>
    /// <param name="view">Dialogue view implementation.</param>
    public void UnbindView(IDialogueView view)
    {
        if (!ReferenceEquals(_view, view))
        {
            return;
        }

        UnsubscribeViewEvents(_view);
        _view = null;

        if (IsRunning && !_isEnding)
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// Starts dialogue from reference without route context.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <returns>True when dialogue starts successfully.</returns>
    public bool StartDialogue(DialogueReference reference)
    {
        return StartDialogue(reference, null);
    }

    /// <summary>
    /// Starts dialogue from reference with route context.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <param name="routeResult">Route context for completion callback.</param>
    /// <returns>True when dialogue starts successfully.</returns>
    public bool StartDialogue(DialogueReference reference, DialogueRouteResult routeResult)
    {
        if (IsRunning)
        {
            return false;
        }

        // Do not start dialogue when gameplay is paused.
        if (UIManager.Instance != null && UIManager.Instance.InGamePage != null && UIManager.Instance.InGamePage.IsPause)
        {
            return false;
        }

        if (reference == null)
        {
            Debug.LogWarning("[DialogueService] Cannot start dialogue: reference is null.");
            return false;
        }

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
    /// Starts dialogue directly from an already built graph.
    /// </summary>
    /// <param name="graph">Dialogue graph.</param>
    /// <returns>True when dialogue starts successfully.</returns>
    public bool StartDialogue(DialogueGraph graph)
    {
        DialogueRouteResult routeContext = _pendingRouteResult;
        _pendingRouteResult = null;

        if (IsRunning)
        {
            return false;
        }

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
        EnterNodeById(_graph.StartNodeId);
        return true;
    }

    /// <summary>
    /// Ends dialogue and clears runtime state.
    /// </summary>
    public void EndDialogue()
    {
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
    /// Ensures a dialogue view is available.
    /// </summary>
    /// <param name="error">Error message output.</param>
    /// <returns>True when view is ready.</returns>
    private bool EnsureView(out string error)
    {
        if (_view != null)
        {
            error = string.Empty;
            return true;
        }

        // Auto-discover any component that implements IDialogueView.
        MonoBehaviour[] allBehaviours =
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = allBehaviours[i];
            if (behaviour is IDialogueView dialogueView)
            {
                BindView(dialogueView);
                break;
            }
        }

        if (_view == null)
        {
            error = "No IDialogueView implementation found in current scene.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Enters target node and refreshes speaker/content.
    /// </summary>
    /// <param name="nodeId">Target node id.</param>
    private void EnterNodeById(string nodeId)
    {
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
    /// Starts typing flow for one line.
    /// </summary>
    /// <param name="line">Line content.</param>
    private void BeginTyping(string line)
    {
        StopTypingCoroutine();

        _fullLine = line ?? string.Empty;
        _skipTypingRequested = false;
        _state = DialogueRunState.Typing;

        _typingCoroutine = StartCoroutine(TypeLineRoutine());
    }

    /// <summary>
    /// Coroutine that reveals text over time.
    /// </summary>
    /// <returns>Enumerator instance.</returns>
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
                // Fast-forward current line when player requests next during typing.
                visible = total;
            }
            else
            {
                // Use unscaled time to keep speed stable under timeScale changes.
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
    /// Handles state transition after typing completes.
    /// </summary>
    private void OnTypingCompleted()
    {
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

    /// <summary>
    /// Handles next action request from view layer.
    /// </summary>
    private void HandleNextRequested()
    {
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
    /// Handles choice selection from view layer.
    /// </summary>
    /// <param name="choiceIndex">Selected choice index.</param>
    private void HandleChoiceSelected(int choiceIndex)
    {
        if (!IsRunning || _state != DialogueRunState.WaitingChoice || _currentNode == null)
        {
            return;
        }

        if (_currentNode.choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
        {
            Debug.LogWarning($"[DialogueService] Invalid choice index: {choiceIndex}");
            return;
        }

        DialogueChoiceData choice = _currentNode.choices[choiceIndex];
        _view?.ClearChoices();
        if (choice == null || string.IsNullOrWhiteSpace(choice.nextNodeId))
        {
            EndDialogue();
            return;
        }

        EnterNodeById(choice.nextNodeId);
    }

    /// <summary>
    /// Advances to next linear node or ends dialogue.
    /// </summary>
    private void AdvanceToNextNode()
    {
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

        if (string.IsNullOrWhiteSpace(_currentNode.nextNodeId))
        {
            EndDialogue();
            return;
        }

        EnterNodeById(_currentNode.nextNodeId);
    }

    /// <summary>
    /// Opens dialogue page.
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
    /// Closes dialogue page.
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
    /// Enables or disables player input.
    /// </summary>
    /// <param name="enabled">Whether input should be enabled.</param>
    private void SetPlayerInputEnabled(bool enabled)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.SetInputEnabled(enabled);
    }

    /// <summary>
    /// Stops active typing coroutine safely.
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
    /// Subscribes view events.
    /// </summary>
    /// <param name="view">Dialogue view.</param>
    private void SubscribeViewEvents(IDialogueView view)
    {
        if (view == null)
        {
            return;
        }

        view.OnNextRequested += HandleNextRequested;
        view.OnChoiceSelected += HandleChoiceSelected;
    }

    /// <summary>
    /// Unsubscribes view events.
    /// </summary>
    /// <param name="view">Dialogue view.</param>
    private void UnsubscribeViewEvents(IDialogueView view)
    {
        if (view == null)
        {
            return;
        }

        view.OnNextRequested -= HandleNextRequested;
        view.OnChoiceSelected -= HandleChoiceSelected;
    }
}
