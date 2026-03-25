// 对话数据提供者接口：
// 负责把任意来源的数据（SO/JSON/CSV/远端等）转换为统一的 DialogueGraph。
public interface IDialogueProvider
{
    // 判断当前 Provider 是否能处理这条引用。
    bool CanHandle(DialogueReference reference);

    // 尝试加载并转换为运行时对话图。
    // 返回 false 时必须提供可读错误信息，便于日志定位与回退处理。
    bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error);
}
