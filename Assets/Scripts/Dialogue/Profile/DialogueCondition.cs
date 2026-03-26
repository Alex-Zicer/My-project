using System;

// 单条规则条件：描述“读哪个状态键、用什么方式比较、目标值是什么”。
[Serializable]
public class DialogueCondition
{
    // 是否启用该条件。
    public bool enabled = true;
    // 状态键（例如 quest.main_001.started / chapter）。
    public string key;
    // 值类型（决定使用 bool/int/string 的读取与比较逻辑）。
    public DialogueConditionValueType valueType = DialogueConditionValueType.Bool;
    // 比较方式（等于、大于、存在等）。
    public DialogueConditionComparison comparison = DialogueConditionComparison.IsTrue;

    // 目标布尔值（valueType=Bool 时使用）。
    public bool boolValue = true;
    // 目标整数值（valueType=Int 时使用）。
    public int intValue;
    // 目标字符串值（valueType=String 时使用）。
    public string stringValue = string.Empty;

    // 评估当前条件在给定状态读取器上是否成立。
    public bool Evaluate(IDialogueGameStateReader stateReader)
    {
        if (!enabled) return true;
        if (stateReader == null) return false;
        if (string.IsNullOrWhiteSpace(key)) return false;

        if (comparison == DialogueConditionComparison.Exists)
        {
            return stateReader.HasKey(key);
        }

        if (comparison == DialogueConditionComparison.NotExists)
        {
            return !stateReader.HasKey(key);
        }

        switch (valueType)
        {
            case DialogueConditionValueType.Bool:
                return EvaluateBool(stateReader);
            case DialogueConditionValueType.Int:
                return EvaluateInt(stateReader);
            case DialogueConditionValueType.String:
                return EvaluateString(stateReader);
            default:
                return false;
        }
    }

    // 执行布尔条件比较。
    private bool EvaluateBool(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetBool(key, out bool currentValue)) return false;

        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return currentValue == boolValue;
            case DialogueConditionComparison.NotEquals:
                return currentValue != boolValue;
            case DialogueConditionComparison.IsTrue:
                return currentValue;
            case DialogueConditionComparison.IsFalse:
                return !currentValue;
            default:
                return false;
        }
    }

    // 执行整型条件比较。
    private bool EvaluateInt(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetInt(key, out int currentValue)) return false;

        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return currentValue == intValue;
            case DialogueConditionComparison.NotEquals:
                return currentValue != intValue;
            case DialogueConditionComparison.Greater:
                return currentValue > intValue;
            case DialogueConditionComparison.GreaterOrEqual:
                return currentValue >= intValue;
            case DialogueConditionComparison.Less:
                return currentValue < intValue;
            case DialogueConditionComparison.LessOrEqual:
                return currentValue <= intValue;
            default:
                return false;
        }
    }

    // 执行字符串条件比较。
    private bool EvaluateString(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetString(key, out string currentValue)) return false;

        switch (comparison)
        {
            case DialogueConditionComparison.Equals:
                return string.Equals(currentValue, stringValue, StringComparison.Ordinal);
            case DialogueConditionComparison.NotEquals:
                return !string.Equals(currentValue, stringValue, StringComparison.Ordinal);
            default:
                return false;
        }
    }
}
