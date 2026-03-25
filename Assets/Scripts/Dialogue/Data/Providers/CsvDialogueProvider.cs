// CSV 数据提供者（占位实现）：
// 当前版本仅保留接口形态，后续可按项目表头规范补齐解析与校验逻辑。
public class CsvDialogueProvider : IDialogueProvider
{
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Csv;
    }

    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        // 明确返回失败原因，提醒调用方走 fallbackSO 或其他来源。
        error = "CSV Provider 预留为扩展点，请按项目需求实现解析器。";
        return false;
    }
}
