/// <summary>
/// 可存档对象接口：用于统一捕获与恢复对象状态。
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 获取对象唯一 ID（用于存档内匹配对象）。
    /// </summary>
    /// <returns>对象唯一 ID。</returns>
    string GetUniqueId();

    /// <summary>
    /// 捕获当前对象状态。
    /// </summary>
    /// <returns>可序列化状态对象。</returns>
    object CaptureState();

    /// <summary>
    /// 使用状态对象恢复当前对象。
    /// </summary>
    /// <param name="state">状态对象。</param>
    void RestoreState(object state);
}
