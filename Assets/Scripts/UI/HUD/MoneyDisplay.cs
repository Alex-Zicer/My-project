using TMPro;
using UnityEngine;

/// <summary>
/// 金钱 HUD 显示控制器：负责文本刷新与 Get/Break/Appear 动画触发。
/// </summary>
public class MoneyDisplay : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Money money;                     // 金钱数据源。
    [SerializeField] private HUDManager hudManager;           // HUD 总管理器。
    [SerializeField] private Health playerHealth;             // 玩家生命组件（用于监听死亡事件）。
    [SerializeField] private Animator animator;               // 金钱图标动画机。
    [SerializeField] private TMP_Text moneyText;              // 金钱文本组件。

    [Header("动画状态")]
    [SerializeField] private string appearStateName = "Appear"; // HUD 显示时点播的状态名。

    [Header("动画触发器")]
    [SerializeField] private string getTriggerName = "Get";     // 加钱触发器。
    [SerializeField] private string breakTriggerName = "Break"; // 死亡触发器。

    private int _getTriggerHash;   // Get 触发器哈希缓存。
    private int _breakTriggerHash; // Break 触发器哈希缓存。

    private void Awake()
    {
        ResolveReferences();

        // 提前缓存 Trigger 哈希，避免运行时频繁字符串查找。
        _getTriggerHash = Animator.StringToHash(getTriggerName);
        _breakTriggerHash = Animator.StringToHash(breakTriggerName);

        if (money != null)
        {
            UpdateMoneyText(money.CurrentMoney);
        }
    }

    private void OnEnable()
    {
        if (money != null)
        {
            money.OnMoneyChanged += HandleMoneyChanged;
        }

        if (hudManager != null)
        {
            hudManager.OnHUDActiveChanged += HandleHUDActiveChanged;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDeath += HandlePlayerDeath;
        }
    }

    private void Start()
    {
        // 若 HUD 当前已显示，组件启用后立刻补一帧 Appear。
        if (hudManager != null && hudManager.IsHUDActive)
        {
            PlayAppear();
        }
    }

    private void OnDisable()
    {
        if (money != null)
        {
            money.OnMoneyChanged -= HandleMoneyChanged;
        }

        if (hudManager != null)
        {
            hudManager.OnHUDActiveChanged -= HandleHUDActiveChanged;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }

    /// <summary>
    /// 处理金钱变化：始终刷新文本，仅在加钱时触发 Get 动画。
    /// </summary>
    /// <param name="current">当前钱数</param>
    /// <param name="delta">变化量</param>
    private void HandleMoneyChanged(int current, int delta)
    {
        UpdateMoneyText(current);

        // 规则：消费仅更新数据与文本，不播放 Break。
        if (delta > 0)
        {
            PlayTrigger(_getTriggerHash);
        }
    }

    /// <summary>
    /// 处理 HUD 显隐事件：显示时播放 Appear。
    /// </summary>
    /// <param name="isActive">HUD 是否显示</param>
    private void HandleHUDActiveChanged(bool isActive)
    {
        if (!isActive)
        {
            return;
        }

        PlayAppear();
    }

    /// <summary>
    /// 处理玩家死亡事件：播放 Break 动画。
    /// </summary>
    private void HandlePlayerDeath()
    {
        PlayBreakOnDeath();
    }

    /// <summary>
    /// 播放死亡 Break 动画（可供外部手动调用）。
    /// </summary>
    public void PlayBreakOnDeath()
    {
        PlayTrigger(_breakTriggerHash);
    }

    /// <summary>
    /// 刷新金钱文本显示。
    /// </summary>
    /// <param name="value">当前钱数</param>
    private void UpdateMoneyText(int value)
    {
        if (moneyText != null)
        {
            moneyText.text = value.ToString();
        }
    }

    /// <summary>
    /// 点播 Appear 状态（无入口连线场景使用）。
    /// </summary>
    private void PlayAppear()
    {
        if (animator == null || string.IsNullOrEmpty(appearStateName))
        {
            return;
        }

        // 点播状态前清理 Trigger，避免与 Get/Break 条件冲突。
        ResetMoneyTriggers();
        animator.Play(appearStateName, 0, 0f);
    }

    /// <summary>
    /// 统一触发 Trigger 动画，触发前先清理历史 Trigger。
    /// </summary>
    /// <param name="triggerHash">触发器哈希</param>
    private void PlayTrigger(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        ResetMoneyTriggers();
        animator.SetTrigger(triggerHash);
    }

    /// <summary>
    /// 重置本组件使用到的 Trigger。
    /// </summary>
    private void ResetMoneyTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(_getTriggerHash);
        animator.ResetTrigger(_breakTriggerHash);
    }

    /// <summary>
    /// 自动补齐运行时引用，降低场景手动绑定成本。
    /// </summary>
    private void ResolveReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (money == null && player != null)
        {
            money = player.GetComponent<Money>();
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }

        if (animator == null)
        {
            // 兼容动画机挂在本节点或子节点两种结构。
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
