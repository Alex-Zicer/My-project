using TMPro;
using UnityEngine;

// 2D 对话触发器：负责范围检测、交互触发、提示显示，以及可选的 Profile 路由入口。
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("对话配置（路由模式）")]
    // 是否启用 Profile 路由模式。
    [SerializeField] private bool useProfileRouting = true;
    // 当前 NPC 标识（用于路由与进度记录）。
    [SerializeField] private string npcId = "npc_001";
    // NPC 对话规则配置。
    [SerializeField] private NpcDialogueProfileSO dialogueProfile;

    [Header("对话配置（兼容模式）")]
    // 旧版单引用对话（未启用路由时使用）。
    [SerializeField] private DialogueReference dialogueReference = new DialogueReference();
    // 旧版一次性触发开关（仅兼容模式生效）。
    [SerializeField] private bool oneShot;

    [Header("交互设置")]
    // 交互按键。
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // 提示跟随原点（为空时回退当前物体 transform）。
    [SerializeField] private Transform interactOrigin;
    // 触发范围碰撞体（必须为 Trigger）。
    [SerializeField] private BoxCollider2D triggerZone;

    [Header("交互提示（可选）")]
    // 交互提示根节点（可为 world-space UI 或普通物体）。
    [SerializeField] private GameObject interactHintRoot;
    // 是否跟随交互原点。
    [SerializeField] private bool followInteractOrigin = true;
    // 提示位置偏移。
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 2f, 0f);
    // 提示文本格式，{0} 会替换为按键名。
    [SerializeField] private string hintFormat = "[{0}] 交互";
    // 提示文本组件（可选）。
    [SerializeField] private TMP_Text hintText;

    // 兼容模式下是否已触发过。
    private bool _hasTriggered;
    // 玩家是否在触发范围内。
    private bool _playerInRange;
    // 当前提示是否可见。
    private bool _hintVisible;

    // 初始化触发器与提示文案。
    private void Awake()
    {
        if (interactOrigin == null) interactOrigin = transform;
        if (triggerZone == null) triggerZone = GetComponent<BoxCollider2D>();
        if (triggerZone != null) triggerZone.isTrigger = true;

        RefreshHintText();
        SetHintVisible(false, true);
    }

    // 启用组件时重置提示可见性。
    private void OnEnable()
    {
        SetHintVisible(false, true);
    }

    // 禁用组件时清理范围状态并隐藏提示。
    private void OnDisable()
    {
        _playerInRange = false;
        SetHintVisible(false, true);
    }

    // 每帧处理交互输入与提示显示。
    private void Update()
    {
        bool isDialogueRunning = DialogueService.HasInstance && DialogueService.Instance.IsRunning;
        bool useRouting = IsUsingProfileRouting();

        if (!useRouting && oneShot && _hasTriggered)
        {
            SetHintVisible(false);
            return;
        }
        if (!_playerInRange)
        {
            SetHintVisible(false);
            return;
        }

        if (isDialogueRunning)
        {
            SetHintVisible(false);
            return;
        }

        SetHintVisible(true);
        if (_hintVisible) RefreshHintTransform();

        if (!Input.GetKeyDown(interactKey)) return;

        bool started = useRouting ? TryStartByProfile() : DialogueService.Instance.StartDialogue(dialogueReference);
        if (started && !useRouting && oneShot)
        {
            _hasTriggered = true;
        }

        if (started)
        {
            SetHintVisible(false);
        }
    }

    // 判断当前是否应使用 Profile 路由模式。
    private bool IsUsingProfileRouting()
    {
        if (!useProfileRouting) return false;
        if (dialogueProfile == null) return false;
        return !string.IsNullOrWhiteSpace(npcId);
    }

    // 通过路由服务解析并启动本次对话。
    private bool TryStartByProfile()
    {
        if (!DialogueRouterService.Instance.TryResolve(npcId, dialogueProfile, out DialogueRouteResult route, out string error))
        {
            Debug.LogWarning($"[DialogueTrigger] NPC '{npcId}' 路由失败: {error}");
            return false;
        }

        return DialogueService.Instance.StartDialogue(route.DialogueReference, route);
    }

    // 玩家进入触发范围时标记可交互。
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    // 玩家离开触发范围时取消交互并隐藏提示。
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = false;
            SetHintVisible(false);
        }
    }

    // 统一控制提示显隐，避免重复 SetActive。
    private void SetHintVisible(bool visible, bool force = false)
    {
        if (interactHintRoot == null) return;
        if (!force && _hintVisible == visible) return;

        _hintVisible = visible;
        interactHintRoot.SetActive(visible);

        if (visible)
        {
            RefreshHintTransform();
        }
    }

    // 根据原点和偏移更新提示位置。
    private void RefreshHintTransform()
    {
        if (interactHintRoot == null) return;
        if (!followInteractOrigin) return;

        interactHintRoot.transform.position = interactOrigin.position + hintOffset;
    }

    // 根据交互键刷新提示文本。
    private void RefreshHintText()
    {
        if (hintText == null) return;

        string keyName = interactKey.ToString().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(hintFormat))
        {
            hintText.text = keyName;
            return;
        }

        hintText.text = hintFormat.Contains("{0}")
            ? hintFormat.Replace("{0}", keyName)
            : hintFormat + " " + keyName;
    }

    // 在编辑器参数变化时自动修正关键引用和配置。
    private void OnValidate()
    {
        if (interactOrigin == null) interactOrigin = transform;
        if (triggerZone == null) triggerZone = GetComponent<BoxCollider2D>();
        if (triggerZone != null) triggerZone.isTrigger = true;
        if (string.IsNullOrWhiteSpace(npcId)) npcId = gameObject != null ? gameObject.name : "npc_001";
        RefreshHintText();
    }

    // 选中物体时绘制 Trigger 范围，便于关卡调试。
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D zone = triggerZone != null ? triggerZone : GetComponent<BoxCollider2D>();
        if (zone == null) return;

        Gizmos.color = Color.cyan;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = zone.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(zone.offset, zone.size);
        Gizmos.matrix = old;
    }

}
