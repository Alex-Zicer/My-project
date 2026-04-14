public interface IDialogueGameStateWriter
{
/// <summary>
/// 写入指定状态键的布尔值。
/// </summary>
void SetBool(string key, bool value);
/// <summary>
/// 移除指定状态键。
/// </summary>
void Remove(string key);
}
