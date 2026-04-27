/// <summary>
/// 音效发射接口：供动画关键帧统一触发。
/// </summary>
public interface IActionSfxEmitter
{
    /// <summary>
    /// 按事件 ID 播放一次性音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    void PlaySfx(string eventId);

    /// <summary>
    /// 尝试按事件 ID 播放一次性音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    /// <returns>播放成功返回 true。</returns>
    bool TryPlaySfx(string eventId);

    /// <summary>
    /// 开始播放循环音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    void PlayLoopSfx(string eventId);

    /// <summary>
    /// 尝试开始播放循环音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    /// <returns>开始请求成功返回 true。</returns>
    bool TryPlayLoopSfx(string eventId);

    /// <summary>
    /// 停止当前循环音效；若传入 eventId，则只在匹配当前循环时停止。
    /// </summary>
    /// <param name="eventId">要停止的循环音效 ID。</param>
    void StopLoopSfx(string eventId);
}
