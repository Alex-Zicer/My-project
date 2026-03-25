using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 对话页面控制器（IDialogueView 的默认实现）：
// 负责把 DialogueService 的指令映射到具体 UI 组件，并把用户输入回传给 Service。
public class DialoguePageController : MonoBehaviour, IDialogueView
{
    [Header("界面引用")]
    // 可选：如果项目使用 UIPage 管理，这里绑定页面对象。
    [SerializeField] private UIPage page;
    // 说话人名文本。
    [SerializeField] private TextMeshProUGUI speakerText;
    // 正文文本。
    [SerializeField] private TextMeshProUGUI contentText;
    // 头像图片（可为空）。
    [SerializeField] private Image portraitImage;
    // 动态选项按钮挂载根节点。
    [SerializeField] private Transform choicesRoot;
    // 选项按钮预制体。
    [SerializeField] private DialogueChoiceButtonView choiceButtonPrefab;

    [Header("输入设置")]
    // 推进对话的主按键。
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    // 是否允许空格键作为推进键。
    [SerializeField] private bool allowSpaceAsNext = true;
    // 是否允许回车键作为推进键。
    [SerializeField] private bool allowReturnAsNext = true;

    // 向运行层发送“下一步请求”。
    public event Action OnNextRequested;
    // 向运行层发送“选项被选择”。
    public event Action<int> OnChoiceSelected;

    // 当前实例化出的全部选项按钮，用于统一回收。
    private readonly List<DialogueChoiceButtonView> _spawnedChoices = new List<DialogueChoiceButtonView>();
    // 页面是否处于打开状态。
    private bool _isOpen;
    // 是否正在展示选项（展示选项时不响应 Next 键）。
    private bool _showingChoices;

    private void Reset()
    {
        // 编辑器下自动填充 page 引用。
        if (page == null) page = GetComponent<UIPage>();
    }

    private void Awake()
    {
        // 运行时兜底自动获取 page。
        if (page == null) page = GetComponent<UIPage>();
    }

    private void OnEnable()
    {
        // 页面启用即绑定为当前 View。
        _isOpen = true;
        DialogueService.Instance.BindView(this);
    }

    private void OnDisable()
    {
        // 页面禁用时清理本地状态，防止旧选项残留。
        _isOpen = false;
        _showingChoices = false;
        ClearChoices();

        if (DialogueService.HasInstance)
        {
            // 解绑时若对话在进行中，Service 会安全结束流程。
            DialogueService.Instance.UnbindView(this);
        }
    }

    private void Update()
    {
        // 未打开或正在选项阶段时，不处理“下一步”按键。
        if (!_isOpen || _showingChoices) return;

        bool pressed = Input.GetKeyDown(nextKey);
        if (allowSpaceAsNext) pressed |= Input.GetKeyDown(KeyCode.Space);
        if (allowReturnAsNext) pressed |= Input.GetKeyDown(KeyCode.Return);

        if (pressed)
        {
            OnNextRequested?.Invoke();
        }
    }

    public void Open()
    {
        // 优先走 UIPage 的开页逻辑，未绑定 page 时退化为 SetActive。
        if (page != null) page.Open();
        else gameObject.SetActive(true);
        _isOpen = true;
    }

    public void Close()
    {
        // 关页前先清理选项实例，避免下次打开出现重复。
        ClearChoices();
        if (page != null) page.Close();
        else gameObject.SetActive(false);
        _isOpen = false;
        _showingChoices = false;
    }

    public void SetSpeaker(string name, Sprite portrait)
    {
        if (speakerText != null) speakerText.text = name ?? string.Empty;

        if (portraitImage != null)
        {
            // 无头像时隐藏图片组件，避免显示旧头像。
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    public void SetContent(string text, bool isTyping)
    {
        // 当前实现不区分 isTyping 样式；若后续要做闪烁光标可用该参数扩展。
        if (contentText != null) contentText.text = text ?? string.Empty;
    }

    public void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices)
    {
        ClearChoices();
        _showingChoices = choices != null && choices.Count > 0;

        // 缺少必要引用时直接返回，避免空引用异常。
        if (!_showingChoices || choicesRoot == null || choiceButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoiceViewModel choice = choices[i];
            DialogueChoiceButtonView buttonView = Instantiate(choiceButtonPrefab, choicesRoot);
            buttonView.Setup(choice.Index, choice.Text, HandleChoiceClicked);
            _spawnedChoices.Add(buttonView);
        }
    }

    public void ClearChoices()
    {
        // 销毁旧按钮实例，确保每次展示的选项与当前节点一致。
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

    private void HandleChoiceClicked(int index)
    {
        // 将选择结果回传给运行层处理跳转。
        OnChoiceSelected?.Invoke(index);
    }
}
