using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Config (Routing)")]
    [SerializeField] private bool useProfileRouting = true;

    [SerializeField] private string npcId = "npc_001";

    [SerializeField] private NpcDialogueProfileSO dialogueProfile;

    [Header("Dialogue Config (Legacy)")]
    [SerializeField] private DialogueReference dialogueReference = new DialogueReference();

    [SerializeField] private bool oneShot;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [SerializeField] private Transform interactOrigin;

    [SerializeField] private BoxCollider2D triggerZone;

    [Header("Hint (Optional)")]
    [SerializeField] private GameObject interactHintRoot;

    [SerializeField] private bool followInteractOrigin = true;

    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 2f, 0f);

    [SerializeField] private string hintFormat = "[{0}] Interact";

    [SerializeField] private TMP_Text hintText;

    // _hasTriggered 状态开关。
    private bool _hasTriggered;

    // _playerInRange 运行时字段。
    private bool _playerInRange;

    // _hintVisible 运行时字段。
    private bool _hintVisible;

    /// <summary>
    /// 初始化组件并确保运行时状态有效。
    /// </summary>
    private void Awake()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
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
    /// 组件启用时重置必要状态并注册依赖。
    /// </summary>
    private void OnEnable()
    {
        SetHintVisible(false, true);
    }

    /// <summary>
    /// 组件停用时清理临时状态并解除依赖。
    /// </summary>
    private void OnDisable()
    {
        _playerInRange = false;
        SetHintVisible(false, true);
    }

    /// <summary>
    /// 处理每帧输入与状态推进逻辑。
    /// </summary>
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
        if (_hintVisible)
        {
            RefreshHintTransform();
        }

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        bool started = useRouting
            ? TryStartByProfile()
            : DialogueService.Instance.StartDialogue(dialogueReference);

        if (started && !useRouting && oneShot)
        {
            _hasTriggered = true;
        }

        if (started)
        {
            SetHintVisible(false);
        }
    }

    /// <summary>
    /// 判断是否启用基于 Profile 的路由模式。
    /// </summary>
    private bool IsUsingProfileRouting()
    {
        if (!useProfileRouting)
        {
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (dialogueProfile == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(npcId);
    }

    /// <summary>
    /// 通过 Profile 路由解析并启动对话。
    /// </summary>
    private bool TryStartByProfile()
    {
        if (!DialogueRouterService.Instance.TryResolve(npcId, dialogueProfile, out DialogueRouteResult route, out string error))
        {
            Debug.LogWarning($"[DialogueTrigger] Failed to resolve NPC '{npcId}': {error}");
            return false;
        }

        return DialogueService.Instance.StartDialogue(route.DialogueReference, route);
    }

    /// <summary>
    /// 玩家进入触发区域时标记可交互状态。
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    /// <summary>
    /// 玩家离开触发区域时清理交互状态。
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            _playerInRange = false;
            SetHintVisible(false);
        }
    }

    /// <summary>
    /// 设置交互提示显隐状态。
    /// </summary>
    private void SetHintVisible(bool visible, bool force = false)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (interactHintRoot == null)
        {
            return;
        }

        if (!force && _hintVisible == visible)
        {
            return;
        }

        _hintVisible = visible;
        interactHintRoot.SetActive(visible);

        if (visible)
        {
            RefreshHintTransform();
        }
    }

    /// <summary>
    /// 刷新交互提示在世界空间的位置。
    /// </summary>
    private void RefreshHintTransform()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (interactHintRoot == null || !followInteractOrigin)
        {
            return;
        }

        interactHintRoot.transform.position = interactOrigin.position + hintOffset;
    }

    /// <summary>
    /// 刷新交互提示文本。
    /// </summary>
    private void RefreshHintText()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (hintText == null)
        {
            return;
        }

        string keyName = interactKey.ToString().ToUpperInvariant();
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(hintFormat))
        {
            hintText.text = keyName;
            return;
        }

        hintText.text = hintFormat.Contains("{0}")
            ? hintFormat.Replace("{0}", keyName)
            : hintFormat + " " + keyName;
    }

    /// <summary>
    /// 在编辑器参数变更时校正序列化配置。
    /// </summary>
    private void OnValidate()
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (interactOrigin == null)
        {
            interactOrigin = transform;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider2D>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(npcId))
        {
            npcId = gameObject != null ? gameObject.name : "npc_001";
        }

        RefreshHintText();
    }

    /// <summary>
    /// 在场景视图绘制触发区域辅助线框。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D zone = triggerZone != null ? triggerZone : GetComponent<BoxCollider2D>();
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (zone == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = zone.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(zone.offset, zone.size);
        Gizmos.matrix = old;
    }
}
