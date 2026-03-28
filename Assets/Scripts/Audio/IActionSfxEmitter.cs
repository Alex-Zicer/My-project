/// <summary>
/// 动作音效发射接口：供动画关键帧统一触发。
/// </summary>
public interface IActionSfxEmitter
{
    /// <summary>
    /// 按动作键播放动作音效。
    /// </summary>
    /// <param name="actionKey">动作键。</param>
    void PlayActionSfx(string actionKey);

    /// <summary>
    /// 尝试按动作键播放动作音效。
    /// </summary>
    /// <param name="actionKey">动作键。</param>
    /// <returns>播放成功返回 true。</returns>
    bool TryPlayActionSfx(string actionKey);
}
