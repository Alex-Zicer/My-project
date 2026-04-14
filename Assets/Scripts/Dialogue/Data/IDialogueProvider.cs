public interface IDialogueProvider
{
/// <summary>
/// 判断提供器是否支持该数据源引用。
/// </summary>
bool CanHandle(DialogueReference reference);

/// <summary>
/// 尝试加载对话数据并输出对话图。
/// </summary>
bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error);
}
