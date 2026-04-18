using UnityEngine;

public class HealthMaskCell : MonoBehaviour
{
    [SerializeField] private Animator animator; // 单格动画机引用。

    [Header("动画状态名")]
    [SerializeField] private string idleStateName = "Idle";       // 满格常驻状态。
    [SerializeField] private string emptyStateName = "Empty";     // 空格常驻状态。
    [SerializeField] private string appearStateName = "Appear";   // HUD 显示时点播状态。
    [SerializeField] private string maxUpStateName = "MaxUp";     // 增加上限时点播状态。

    [Header("动画触发器")]
    [SerializeField] private string breakTriggerName = "Break";   // 受伤触发器。
    [SerializeField] private string refillTriggerName = "Refill"; // 回血触发器。

    private int _cellIndex; // 单格索引：0 最左，越大越靠右。
    private bool _isFilled; // 当前是否为满格状态。

    private int _breakTriggerHash;  // Break 触发器哈希缓存。
    private int _refillTriggerHash; // Refill 触发器哈希缓存。

    private void Awake()
    {
        if (animator == null)
        {
            // 兜底：允许直接挂在预制体根节点时自动取 Animator。
            animator = GetComponent<Animator>();
        }

        // 提前缓存 Trigger 哈希，避免运行时频繁字符串查找。
        _breakTriggerHash = Animator.StringToHash(breakTriggerName);
        _refillTriggerHash = Animator.StringToHash(refillTriggerName);
    }

    /// <summary>
    /// 设置当前血格索引（从 0 开始，越大越靠右）。
    /// </summary>
    /// <param name="cellIndex">血格索引</param>
    public void SetCellIndex(int cellIndex)
    {
        _cellIndex = Mathf.Max(0, cellIndex);
    }

    /// <summary>
    /// 立即设置血格为满/空，用于初始化和强制同步，不播放过渡动画。
    /// </summary>
    /// <param name="isFilled">是否满格</param>
    public void SetFilledInstant(bool isFilled)
    {
        _isFilled = isFilled;
        if (animator == null)
        {
            return;
        }

        ResetAllTriggers();
        animator.Play(isFilled ? idleStateName : emptyStateName, 0, 0f);
    }

    /// <summary>
    /// 根据前后血量变化决定本格动画：满到空播 Break，空到满播 Refill。
    /// </summary>
    /// <param name="oldCurrent">变化前血量</param>
    /// <param name="newCurrent">变化后血量</param>
    public void ApplyHealthChange(float oldCurrent, float newCurrent)
    {
        // 规则：第 i 格在 currentHealth >= i + 1 时视为满格。
        bool oldFilled = oldCurrent >= _cellIndex + 1;
        bool newFilled = newCurrent >= _cellIndex + 1;

        if (oldFilled && !newFilled)
        {
            // 满 -> 空：播放受伤 Break，后续由 Animator 自动切到 Empty。
            _isFilled = false;
            PlayBreak();
            return;
        }

        if (!oldFilled && newFilled)
        {
            // 空 -> 满：播放回血 Refill，后续由 Animator 自动切到 Idle。
            _isFilled = true;
            PlayRefill();
            return;
        }

        _isFilled = newFilled;
    }

    /// <summary>
    /// 播放 HUD 显示时的 Appear 动画。
    /// </summary>
    public void PlayAppear()
    {
        PlayState(appearStateName);
    }

    /// <summary>
    /// 播放最大血量增加时的 MaxUp 动画。
    /// </summary>
    public void PlayMaxUp()
    {
        PlayState(maxUpStateName);
    }

    /// <summary>
    /// 获取当前格是否为满格，仅用于调试与可视化检查。
    /// </summary>
    /// <returns>满格返回 true</returns>
    public bool IsFilled()
    {
        return _isFilled;
    }

    /// <summary>
    /// 播放受伤 Break 动画；后续转 Empty 由 Animator 状态机负责。
    /// </summary>
    private void PlayBreak()
    {
        PlayTrigger(_breakTriggerHash);
    }

    /// <summary>
    /// 播放回血 Refill 动画；后续转 Idle 由 Animator 状态机负责。
    /// </summary>
    private void PlayRefill()
    {
        PlayTrigger(_refillTriggerHash);
    }

    /// <summary>
    /// 直接点播指定动画状态，用于无入口连线的状态（如 Appear/MaxUp）。
    /// </summary>
    /// <param name="stateName">状态名</param>
    private void PlayState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        // 点播状态前先清 Trigger，避免动画条件相互干扰。
        ResetAllTriggers();
        animator.Play(stateName, 0, 0f);
    }

    /// <summary>
    /// 统一触发动画，触发前先清理其他 Trigger，避免串动画。
    /// </summary>
    /// <param name="triggerHash">触发器哈希</param>
    private void PlayTrigger(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        ResetAllTriggers();
        animator.SetTrigger(triggerHash);
    }

    /// <summary>
    /// 重置所有本血格使用到的 Trigger。
    /// </summary>
    private void ResetAllTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(_breakTriggerHash);
        animator.ResetTrigger(_refillTriggerHash);
    }
}
