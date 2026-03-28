// 鍓ф儏鐘舵€佸啓鍏ユ帴鍙ｏ細鐢ㄤ簬瀵硅瘽缁撴潫鍚庣殑鐘舵€佸洖鍐欍€?
public interface IDialogueGameStateWriter
{
    // 鍐欏叆甯冨皵鐘舵€併€?
/// <summary>
/// SetBool。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
void SetBool(string key, bool value);
    // 鍐欏叆鏁村瀷鐘舵€併€?
/// <summary>
/// SetInt。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
void SetInt(string key, int value);
    // 鍐欏叆瀛楃涓茬姸鎬併€?
/// <summary>
/// SetString。
/// </summary>
/// <param name="key">参数。</param>
/// <param name="value">参数。</param>
void SetString(string key, string value);
    // 鍒犻櫎鐘舵€併€?
/// <summary>
/// Remove。
/// </summary>
/// <param name="key">参数。</param>
void Remove(string key);
}
