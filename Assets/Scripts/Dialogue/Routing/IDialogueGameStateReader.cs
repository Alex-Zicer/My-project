// 鍓ф儏鐘舵€佽鍙栨帴鍙ｏ細璺敱鏉′欢璇勪及渚濊禆璇ユ帴鍙ｏ紝涓嶇洿鎺ヨ€﹀悎浠诲姟绯荤粺瀹炵幇銆?
public interface IDialogueGameStateReader
{
    // 鏄惁瀛樺湪璇ョ姸鎬侀敭銆?
/// <summary>
/// HasKey。
/// </summary>
/// <param name="key">参数。</param>
bool HasKey(string key);
    // 璇诲彇甯冨皵鐘舵€併€?
/// <summary>
/// TryGetBool。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
bool TryGetBool(string key, out bool value);
    // 璇诲彇鏁村瀷鐘舵€併€?
/// <summary>
/// TryGetInt。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
bool TryGetInt(string key, out int value);
    // 璇诲彇瀛楃涓茬姸鎬併€?
/// <summary>
/// TryGetString。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
bool TryGetString(string key, out string value);
}
