using System;

/// <summary>
/// 规则条件定义：基于布尔状态判断规则是否命中。
/// </summary>
[Serializable]
public class DialogueCondition
{
    // enabled 运行时字段。
    public bool enabled = true;

    // key 运行时字段。
    public string key;

    // comparison 运行时字段。
    public DialogueConditionComparison comparison = DialogueConditionComparison.IsTrue;

    /// <summary>
    /// 评估单条对话条件是否成立。
    /// </summary>
    public bool Evaluate(IDialogueGameStateReader stateReader)
    {
        if (!enabled)
        {
            return true;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (stateReader == null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        // 按状态分支执行对应处理逻辑。
        switch (comparison)
        {
            case DialogueConditionComparison.IsTrue:
                return stateReader.TryGetBool(key, out bool v1) && v1;

            case DialogueConditionComparison.IsFalse:
                return !stateReader.TryGetBool(key, out bool v2) || !v2;

            default:
                return false;
        }
    }
}
