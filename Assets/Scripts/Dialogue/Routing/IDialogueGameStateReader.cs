// 剧情状态读取接口：路由条件评估依赖该接口，不直接耦合任务系统实现。
public interface IDialogueGameStateReader
{
    // 是否存在该状态键。
    bool HasKey(string key);
    // 读取布尔状态。
    bool TryGetBool(string key, out bool value);
    // 读取整型状态。
    bool TryGetInt(string key, out int value);
    // 读取字符串状态。
    bool TryGetString(string key, out string value);
}
