using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 对话 Demo 触发器。
/// 挂在场景交互物体上，用于在玩家进入范围后显示提示，并通过 Submit 输入触发一段固定对话。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueDemoTrigger : MonoBehaviour
{
    [Header("Demo Dialogue")]
    // Demo 使用的对话引用，默认指向一个 JSON 对话文件。
    [SerializeField] private DialogueReference dialogueReference = new DialogueReference
    {
        sourceType = DialogueSourceType.Json,
        keyOrPath = "Dialogue/sample_dialogue.json"
    };

    // 是否只允许触发一次。
    [SerializeField] private bool oneShot;

    [Header("Interaction")]
    // 交互提示中显示的输入名称。
    [SerializeField] private string interactInputLabel = "E / Submit";

    // 提示跟随的参考点；未配置时默认使用当前物体。
    [SerializeField] private Transform interactOrigin;

    // 用于检测玩家进入范围的触发器。
    [SerializeField] private BoxCollider2D triggerZone;

    [Header("Hint (Optional)")]
    // 交互提示根节点；为空时表示不显示提示。
    [SerializeField] private GameObject interactHintRoot;

    // 是否让提示跟随交互参考点移动。
    [SerializeField] private bool followInteractOrigin = true;

    // 提示相对交互参考点的偏移。
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 2f, 0f);

    // 提示文本格式，{0} 会被替换为输入名称。
    [SerializeField] private string hintFormat = "[{0}] Talk";

    // 实际显示提示内容的文本组件。
    [SerializeField] private TMP_Text hintText;

    // one-shot 模式下是否已经成功触发过。
    private bool _hasTriggered;

    // 玩家当前是否位于触发范围内。
    private bool _playerInRange;

    // 当前提示是否处于显示状态。
    private bool _hintVisible;

    // Input System 生成的输入动作资源。
    private PlayerControls _inputActions;

    /// <summary>
    /// 初始化输入、交互点和触发器配置，并同步提示初始状态。
    /// </summary>
    private void Awake()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerControls();
        }

        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider2D>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        RefreshHintText();
        SetHintVisible(false, true);
    }

    /// <summary>
    /// 启用输入并重置提示显示状态。
    /// </summary>
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerControls();
        }

        _inputActions.UI.Enable();
        SetHintVisible(false, true);
    }

    /// <summary>
    /// 关闭输入并清理范围与提示状态。
    /// </summary>
    private void OnDisable()
    {
        if (_inputActions != null)
        {
            _inputActions.UI.Disable();
        }

        _playerInRange = false;
        SetHintVisible(false, true);
    }

    /// <summary>
    /// 处理玩家是否可交互、提示显隐以及提交输入触发对话。
    /// </summary>
    private void Update()
    {
        // one-shot 已触发后直接退出，并确保提示隐藏。
        if (oneShot && _hasTriggered)
        {
            SetHintVisible(false);
            return;
        }

        // 玩家不在范围内时不允许交互。
        if (!_playerInRange)
        {
            SetHintVisible(false);
            return;
        }

        // 对话播放中隐藏提示，避免出现重复触发入口。
        if (DialogueService.HasInstance && DialogueService.Instance.IsRunning)
        {
            SetHintVisible(false);
            return;
        }

        // 进入可交互状态后显示提示，并持续同步其位置。
        SetHintVisible(true);
        if (_hintVisible)
        {
            RefreshHintTransform();
        }

        // 仅在本帧收到 Submit 输入时才尝试启动对话。
        if (_inputActions == null || !_inputActions.UI.Submit.WasPressedThisFrame())
        {
            return;
        }

        if (dialogueReference == null || !dialogueReference.IsConfigured())
        {
            Debug.LogWarning($"[DialogueDemoTrigger] '{name}' 缺少对话引用配置。");
            return;
        }

        bool started = DialogueService.Instance.StartDialogue(dialogueReference);
        if (!started)
        {
            return;
        }

        if (oneShot)
        {
            _hasTriggered = true;
        }

        SetHintVisible(false);
    }

    /// <summary>
    /// 玩家进入触发区后标记为可交互。
    /// </summary>
    /// <param name="other">进入触发区的碰撞体。</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    /// <summary>
    /// 玩家离开触发区后取消可交互状态并隐藏提示。
    /// </summary>
    /// <param name="other">离开触发区的碰撞体。</param>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = false;
            SetHintVisible(false);
        }
    }

    /// <summary>
    /// 设置交互提示显隐。
    /// </summary>
    /// <param name="visible">是否显示提示。</param>
    /// <param name="force">是否忽略当前状态强制刷新。</param>
    private void SetHintVisible(bool visible, bool force = false)
    {
        if (interactHintRoot == null)
        {
            return;
        }

        if (!force && _hintVisible == visible)
        {
            return;
        }

        // 仅在状态确实变化时切换，避免重复 SetActive。
        _hintVisible = visible;
        interactHintRoot.SetActive(visible);

        if (visible)
        {
            RefreshHintTransform();
        }
    }

    /// <summary>
    /// 刷新交互提示的位置。
    /// </summary>
    private void RefreshHintTransform()
    {
        if (interactHintRoot == null || !followInteractOrigin)
        {
            return;
        }

        interactHintRoot.transform.position = interactOrigin.position + hintOffset;
    }

    /// <summary>
    /// 刷新提示文本内容。
    /// </summary>
    private void RefreshHintText()
    {
        if (hintText == null)
        {
            return;
        }

        string keyName = string.IsNullOrWhiteSpace(interactInputLabel)
            ? "Submit"
            : interactInputLabel;

        if (string.IsNullOrWhiteSpace(hintFormat))
        {
            hintText.text = keyName;
            return;
        }

        // 若格式中包含占位符，则替换输入标签；否则直接追加到末尾。
        hintText.text = hintFormat.Contains("{0}")
            ? hintFormat.Replace("{0}", keyName)
            : hintFormat + " " + keyName;
    }

    /// <summary>
    /// 在编辑器修改参数时自动补齐依赖并刷新提示文本。
    /// </summary>
    private void OnValidate()
    {
        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider2D>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        RefreshHintText();
    }

    /// <summary>
    /// 在 Scene 视图中绘制触发区域辅助线框。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D zone = triggerZone != null ? triggerZone : GetComponent<BoxCollider2D>();
        if (zone == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = zone.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(zone.offset, zone.size);
        Gizmos.matrix = old;
    }
}
