// 剧情状态写入接口：用于对话结束后的状态回写。
public interface IDialogueGameStateWriter
{
    // 写入布尔状态。
    void SetBool(string key, bool value);
    // 写入整型状态。
    void SetInt(string key, int value);
    // 写入字符串状态。
    void SetString(string key, string value);
    // 删除状态。
    void Remove(string key);
}
