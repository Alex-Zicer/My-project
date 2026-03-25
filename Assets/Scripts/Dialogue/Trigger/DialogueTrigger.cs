using UnityEngine;

// 对话触发器：
// 挂在可交互对象上（例如 NPC、路牌、终端等），满足“距离 + 按键”条件后尝试开启对话。
// 该组件只负责触发，不负责具体对话运行逻辑（由 DialogueService 处理）。
public class DialogueTrigger : MonoBehaviour
{
    [Header("对话配置")]
    // 指向要触发的对话来源引用（SO/JSON/CSV/自定义 Provider）。
    [SerializeField] private DialogueReference dialogueReference = new DialogueReference();
    // 一次性触发：成功开启一次后不再触发。
    [SerializeField] private bool oneShot;

    [Header("交互设置")]
    // 交互按键。
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // 允许触发的最大距离。
    [SerializeField] private float interactRange = 2f;
    // 距离计算原点；为空时默认使用当前物体的 Transform。
    [SerializeField] private Transform interactOrigin;

    // 缓存玩家 Transform，减少每帧查找开销。
    private Transform _player;
    // oneShot 模式下记录是否已触发成功。
    private bool _hasTriggered;

    private void Awake()
    {
        // 未指定交互原点时，默认使用当前物体
        if (interactOrigin == null) interactOrigin = transform;
    }

    private void Update()
    {
        // 一次性触发已消耗后直接返回。
        if (oneShot && _hasTriggered) return;
        // 对话进行中不允许重复开启。
        if (DialogueService.HasInstance && DialogueService.Instance.IsRunning) return;

        Transform player = GetPlayerTransform();
        if (player == null) return;

        // 仅在按键按下当帧检测，避免长按重复触发。
        if (!Input.GetKeyDown(interactKey)) return;

        float distance = Vector3.Distance(interactOrigin.position, player.position);
        if (distance > interactRange) return;

        // 触发成功且 oneShot=true 时，标记为已触发。
        bool started = DialogueService.Instance.StartDialogue(dialogueReference);
        if (started && oneShot)
        {
            _hasTriggered = true;
        }
    }

    private Transform GetPlayerTransform()
    {
        // 约定玩家对象使用 "Player" 标签。
        if (_player != null) return _player;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        return _player;
    }

    private void OnDrawGizmosSelected()
    {
        // 选中物体时绘制交互范围，便于关卡调试。
        Transform origin = interactOrigin != null ? interactOrigin : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin.position, interactRange);
    }
}
