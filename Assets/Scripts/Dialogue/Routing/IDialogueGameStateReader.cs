public interface IDialogueGameStateReader
{
/// <summary>
/// 判断指定状态键是否存在。
/// </summary>
bool HasKey(string key);
/// <summary>
/// 尝试读取指定状态键的布尔值。
/// </summary>
bool TryGetBool(string key, out bool value);
}
