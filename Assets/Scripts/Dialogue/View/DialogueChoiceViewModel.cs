public struct DialogueChoiceViewModel
{
    public int Index { get; }
    public string Text { get; }

    /// <summary>
    /// 创建一个可供视图层渲染的选项数据模型。
    /// </summary>
    public DialogueChoiceViewModel(int index, string text)
    {
        Index = index;
        Text = text;
    }
}
