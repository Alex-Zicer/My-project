public class CsvDialogueProvider : IDialogueProvider
{
    /// <summary>
    /// 判断提供器是否支持该数据源引用。
    /// </summary>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Csv;
    }

    /// <summary>
    /// 尝试加载对话数据并输出对话图。
    /// </summary>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = "CSV provider is not implemented yet.";
        return false;
    }
}
