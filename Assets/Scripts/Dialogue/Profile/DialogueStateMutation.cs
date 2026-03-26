using System;

// 状态写回描述：对话结束后把某个状态键写成指定值。
[Serializable]
public class DialogueStateMutation
{
    // 要写入的状态键。
    public string key;
    // 要写入的数据类型。
    public DialogueConditionValueType valueType = DialogueConditionValueType.Bool;

    // 要写入的布尔值（valueType=Bool 时使用）。
    public bool boolValue = true;
    // 要写入的整数值（valueType=Int 时使用）。
    public int intValue;
    // 要写入的字符串值（valueType=String 时使用）。
    public string stringValue = string.Empty;

    // 判断该写回项是否可执行（至少要有 key）。
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(key);
    }

    // 把配置值写入游戏状态。
    public void Apply(IDialogueGameStateWriter stateWriter)
    {
        if (stateWriter == null) return;
        if (!IsConfigured()) return;

        switch (valueType)
        {
            case DialogueConditionValueType.Bool:
                stateWriter.SetBool(key, boolValue);
                break;
            case DialogueConditionValueType.Int:
                stateWriter.SetInt(key, intValue);
                break;
            case DialogueConditionValueType.String:
                stateWriter.SetString(key, stringValue ?? string.Empty);
                break;
        }
    }
}
