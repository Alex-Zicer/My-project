// 閫夐」灞曠ず妯″瀷锛?// 杩欐槸杩愯灞備紶缁欒鍥惧眰鐨勮交閲忔暟鎹紝閬垮厤 UI 鐩存帴渚濊禆 DialogueChoiceData銆?
public struct DialogueChoiceViewModel
{
    // 閫夐」绱㈠紩锛堝洖浼犵粰 DialogueService 鐢ㄤ簬瀹氫綅琚€変腑鐨勫垎鏀級銆?
    public int Index { get; }
    // 鎸夐挳鏄剧ず鏂囨湰銆?
    public string Text { get; }

    /// <summary>
    /// DialogueChoiceViewModel。
    /// </summary>
    /// <param name="index">参数。</param>
    /// <param name="text">参数。</param>
    public DialogueChoiceViewModel(int index, string text)
    {
        Index = index;
        Text = text;
    }
}
