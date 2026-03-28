using UnityEngine;

/// <summary>
/// 通用动作音效发射器（Catalog-only）：
/// 动画事件传入 eventId，统一通过 AudioCatalog 查找并播放。
/// </summary>
public class ActionSfxEmitter : MonoBehaviour, IActionSfxEmitter
{
    // 是否静音当前发射器。
    [Header("Flags")]
    [SerializeField] private bool muteThisEmitter;

    // 未找到 eventId 时是否打印警告。
    [SerializeField] private bool logWarningWhenMissingKey = true;

    /// <summary>
    /// 按动作键播放动作音效（供 Animation Event 调用）。
    /// 在 Catalog-only 模式下，actionKey 即 eventId。
    /// </summary>
    /// <param name="actionKey">动作键/事件 ID。</param>
    public void PlayActionSfx(string actionKey)
    {
        TryPlayActionSfx(actionKey);
    }

    /// <summary>
    /// 尝试按动作键播放动作音效。
    /// </summary>
    /// <param name="actionKey">动作键/事件 ID。</param>
    /// <returns>播放成功返回 true。</returns>
    public bool TryPlayActionSfx(string actionKey)
    {
        if (muteThisEmitter || string.IsNullOrWhiteSpace(actionKey))
        {
            return false;
        }

        AudioEventSO evt = AudioService.Instance.GetEventOrNull(actionKey);
        if (evt == null)
        {
            if (logWarningWhenMissingKey)
            {
                Debug.LogWarning($"[ActionSfxEmitter] 未找到动作音效事件：{actionKey}", this);
            }
            return false;
        }

        // 动作音效应走 SFX 通道，避免误配到 BGM/UI 事件。
        if (evt.Category != AudioEventCategory.Sfx)
        {
            Debug.LogWarning(
                $"[ActionSfxEmitter] 事件 '{evt.EventId}' 分类为 {evt.Category}，动作音效应使用 Sfx 分类。",
                this);
            return false;
        }

        AudioService.Instance.PlaySfx2D(evt);
        return true;
    }
}
