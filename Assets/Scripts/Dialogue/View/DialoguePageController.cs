using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private bool allowSpaceAsNext = true;
    [SerializeField] private bool allowReturnAsNext = true;

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
    }

    /// <summary>
    /// 组件启用时重置必要状态并注册依赖。
    /// </summary>
    private void OnEnable()
    {
        _isOpen = true;
        DialogueService.Instance.BindView(this);
    }

    /// <summary>
    /// 组件停用时清理临时状态并解除依赖。
    /// </summary>
    private void OnDisable()
    {
        _isOpen = false;
        _showingChoices = false;
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
        if (!_isOpen || _showingChoices)
        {
            return;
        }

        bool pressed = Input.GetKeyDown(nextKey);
        if (allowSpaceAsNext) pressed |= Input.GetKeyDown(KeyCode.Space);
        if (allowReturnAsNext) pressed |= Input.GetKeyDown(KeyCode.Return);

        if (pressed)
        {
            OnNextRequested?.Invoke();
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
    }

    /// <summary>
    /// 关闭界面并清理展示数据。
    /// </summary>
    public void Close()
    {
        ClearChoices();

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
    }

    /// <summary>
    /// 销毁并清空当前选项按钮。
    /// </summary>
    public void ClearChoices()
    {
        // 清理旧选项按钮，避免跨节点残留。
        for (int i = 0; i < _spawnedChoices.Count; i++)
        {
            if (_spawnedChoices[i] != null)
            {
                Destroy(_spawnedChoices[i].gameObject);
            }
        }

        _spawnedChoices.Clear();
        _showingChoices = false;
    }

    /// <summary>
    /// 将选项索引回传给运行层。
    /// </summary>
    private void HandleChoiceClicked(int index)
    {
        OnChoiceSelected?.Invoke(index);
    }
}
