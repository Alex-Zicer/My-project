using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialoguePageController : MonoBehaviour, IDialogueView
{
    [Header("界面引用")]
    [SerializeField] private UIPage page;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Transform choicesRoot;
    [SerializeField] private DialogueChoiceButtonView choiceButtonPrefab;

    [Header("输入设置")]
    [SerializeField] private float navigateDeadzone = 0.5f;
    [SerializeField] private float firstNavigateRepeatDelay = 0.25f;
    [SerializeField] private float navigateRepeatInterval = 0.12f;

    // 收到“下一句”输入时抛出的事件。
    public event Action OnNextRequested;

    // 选项点击后抛出的事件，参数为选项索引。
    public event Action<int> OnChoiceSelected;

    // 当前页面生成的选项按钮实例缓存。
    private readonly List<DialogueChoiceButtonView> _spawnedChoices = new List<DialogueChoiceButtonView>();

    // 当前页面是否处于打开状态。
    private bool _isOpen;

    // 当前节点是否正在显示选项。
    private bool _showingChoices;

    // 对话页 UI 输入动作集合。
    private PlayerControls _inputActions;

    // 当前是否已启用 UI 输入。
    private bool _uiInputEnabled;

    // 当前选中的选项索引。
    private int _selectedChoiceIndex = -1;

    // 下一次允许连续导航的时间点。
    private float _nextNavigateTime;

    // 上一次导航方向。
    private int _lastNavigateDirection;

    /// <summary>
    /// 在编辑器中自动补齐默认引用。
    /// </summary>
    private void Reset()
    {
        // 当未手动配置时，自动从同物体上获取 UIPage。
        if (page == null)
        {
            page = GetComponent<UIPage>();
        }
    }

    /// <summary>
    /// 初始化组件并确保运行时状态有效。
    /// </summary>
    private void Awake()
    {
        // 当未手动配置时，自动从同物体上获取 UIPage。
        if (page == null)
        {
            page = GetComponent<UIPage>();
        }

        if (_inputActions == null)
        {
            _inputActions = new PlayerControls();
        }
    }

    /// <summary>
    /// 组件启用时重置必要状态并注册依赖。
    /// </summary>
    private void OnEnable()
    {
        _isOpen = true;
        DialogueService.Instance.BindView(this);
        EnableUiInput();
    }

    /// <summary>
    /// 组件停用时清理临时状态并解除依赖。
    /// </summary>
    private void OnDisable()
    {
        _isOpen = false;
        _showingChoices = false;
        DisableUiInput();
        ClearChoices();

        // 服务存在时才执行解绑，避免在退出阶段触发隐式创建。
        if (DialogueService.HasInstance)
        {
            DialogueService.Instance.UnbindView(this);
        }
    }

    /// <summary>
    /// 轮询推进按键并转发“下一句”输入事件。
    /// </summary>
    private void Update()
    {
        if (!_isOpen || !_uiInputEnabled)
        {
            return;
        }

        if (_inputActions.UI.Submit.WasPressedThisFrame())
        {
            HandleSubmitRequested();
        }

        if (_showingChoices)
        {
            HandleChoiceNavigation();
        }
    }

    /// <summary>
    /// 打开界面并同步内部状态。
    /// </summary>
    public void Open()
    {
        if (page != null)
        {
            page.Open();
        }
        else
        {
            gameObject.SetActive(true);
        }

        _isOpen = true;
        EnableUiInput();
    }

    /// <summary>
    /// 关闭界面并清理展示数据。
    /// </summary>
    public void Close()
    {
        ClearChoices();
        DisableUiInput();

        if (page != null)
        {
            page.Close();
        }
        else
        {
            gameObject.SetActive(false);
        }

        _isOpen = false;
        _showingChoices = false;
    }

    /// <summary>
    /// 设置说话者名称与头像显示。
    /// </summary>
    public void SetSpeaker(string name, Sprite portrait)
    {
        if (speakerText != null)
        {
            speakerText.text = name ?? string.Empty;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    /// <summary>
    /// 刷新当前对话文本内容。
    /// </summary>
    public void SetContent(string text, bool isTyping)
    {
        if (contentText != null)
        {
            contentText.text = text ?? string.Empty;
        }
    }

    /// <summary>
    /// 生成并显示当前节点选项列表。
    /// </summary>
    public void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices)
    {
        ClearChoices();
        _showingChoices = choices != null && choices.Count > 0;

        if (!_showingChoices || choicesRoot == null || choiceButtonPrefab == null)
        {
            return;
        }

        // 按顺序实例化选项按钮，并绑定索引回调。
        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoiceViewModel choice = choices[i];
            DialogueChoiceButtonView buttonView = Instantiate(choiceButtonPrefab, choicesRoot);
            buttonView.Setup(choice.Index, choice.Text, HandleChoiceClicked);
            _spawnedChoices.Add(buttonView);
        }

        ConfigureChoiceNavigation();
        SelectChoice(0);
    }

    /// <summary>
    /// 销毁并清空当前选项按钮。
    /// </summary>
    public void ClearChoices()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 清理旧选项按钮，避免跨节点残留。
        for (int i = 0; i < _spawnedChoices.Count; i++)
        {
            if (_spawnedChoices[i] != null)
            {
                Destroy(_spawnedChoices[i].gameObject);
            }
        }

        _spawnedChoices.Clear();
        _selectedChoiceIndex = -1;
        _nextNavigateTime = 0f;
        _lastNavigateDirection = 0;
        _showingChoices = false;
    }

    /// <summary>
    /// 将选项索引回传给运行层。
    /// </summary>
    private void HandleChoiceClicked(int index)
    {
        _selectedChoiceIndex = index;
        OnChoiceSelected?.Invoke(index);
    }

    /// <summary>
    /// 启用对话页 UI ActionMap。
    /// </summary>
    private void EnableUiInput()
    {
        if (_uiInputEnabled)
        {
            return;
        }

        if (_inputActions == null)
        {
            _inputActions = new PlayerControls();
        }

        _inputActions.UI.Enable();
        _uiInputEnabled = true;
    }

    /// <summary>
    /// 禁用对话页 UI ActionMap。
    /// </summary>
    private void DisableUiInput()
    {
        if (!_uiInputEnabled || _inputActions == null)
        {
            return;
        }

        _inputActions.UI.Disable();
        _uiInputEnabled = false;
    }

    /// <summary>
    /// 处理提交键请求：无选项时继续，有选项时提交当前选择。
    /// </summary>
    private void HandleSubmitRequested()
    {
        if (_showingChoices)
        {
            SubmitCurrentChoice();
            return;
        }

        OnNextRequested?.Invoke();
    }

    /// <summary>
    /// 处理选项导航输入。
    /// </summary>
    private void HandleChoiceNavigation()
    {
        Vector2 navigate = _inputActions.UI.Navigate.ReadValue<Vector2>();
        int direction = ResolveNavigateDirection(navigate);
        if (direction == 0)
        {
            _lastNavigateDirection = 0;
            return;
        }

        float now = Time.unscaledTime;
        bool isNewDirection = direction != _lastNavigateDirection;
        if (!isNewDirection && now < _nextNavigateTime)
        {
            return;
        }

        MoveSelection(direction);
        _lastNavigateDirection = direction;
        _nextNavigateTime = now + (isNewDirection ? firstNavigateRepeatDelay : navigateRepeatInterval);
    }

    /// <summary>
    /// 根据导航轴值解析上下移动方向。
    /// </summary>
    private int ResolveNavigateDirection(Vector2 navigate)
    {
        if (_spawnedChoices.Count == 0)
        {
            return 0;
        }

        float vertical = navigate.y;
        if (Mathf.Abs(vertical) < navigateDeadzone)
        {
            return 0;
        }

        return vertical > 0f ? -1 : 1;
    }

    /// <summary>
    /// 按方向移动当前选项焦点。
    /// </summary>
    private void MoveSelection(int direction)
    {
        if (_spawnedChoices.Count == 0)
        {
            return;
        }

        int nextIndex = _selectedChoiceIndex;
        if (nextIndex < 0)
        {
            nextIndex = direction > 0 ? 0 : _spawnedChoices.Count - 1;
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex + direction, 0, _spawnedChoices.Count - 1);
        }

        SelectChoice(nextIndex);
    }

    /// <summary>
    /// 选中指定索引的选项按钮。
    /// </summary>
    private void SelectChoice(int index)
    {
        if (index < 0 || index >= _spawnedChoices.Count)
        {
            return;
        }

        _selectedChoiceIndex = index;
        DialogueChoiceButtonView selectedButton = _spawnedChoices[index];
        selectedButton?.Select();
    }

    /// <summary>
    /// 提交当前选中的选项按钮。
    /// </summary>
    private void SubmitCurrentChoice()
    {
        if (_spawnedChoices.Count == 0)
        {
            return;
        }

        if (_selectedChoiceIndex < 0 || _selectedChoiceIndex >= _spawnedChoices.Count)
        {
            SelectChoice(0);
        }

        if (_selectedChoiceIndex < 0 || _selectedChoiceIndex >= _spawnedChoices.Count)
        {
            return;
        }

        _spawnedChoices[_selectedChoiceIndex]?.Submit();
    }

    /// <summary>
    /// 为动态生成的选项按钮配置上下导航关系。
    /// </summary>
    private void ConfigureChoiceNavigation()
    {
        for (int i = 0; i < _spawnedChoices.Count; i++)
        {
            DialogueChoiceButtonView current = _spawnedChoices[i];
            Selectable up = i > 0 ? _spawnedChoices[i - 1].GetSelectable() : null;
            Selectable down = i < _spawnedChoices.Count - 1 ? _spawnedChoices[i + 1].GetSelectable() : null;
            current?.SetNavigation(up, down);
        }
    }
}
