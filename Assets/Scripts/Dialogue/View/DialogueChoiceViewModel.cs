// 选项展示模型：
// 这是运行层传给视图层的轻量数据，避免 UI 直接依赖 DialogueChoiceData。
public struct DialogueChoiceViewModel
{
    // 选项索引（回传给 DialogueService 用于定位被选中的分支）。
    public int Index { get; }
    // 按钮显示文本。
    public string Text { get; }

    public DialogueChoiceViewModel(int index, string text)
    {
        Index = index;
        Text = text;
    }
}
