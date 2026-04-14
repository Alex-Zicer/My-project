using System;

/// <summary>
/// 对话完成后的状态写回配置。
/// </summary>
[Serializable]
public class DialogueStateMutation
{
    // key 运行时字段。
    public string key;

    // value 运行时字段。
    public bool value;

    /// <summary>
    /// 判断状态写回项是否配置完整。
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>
    /// 将状态写回项应用到状态写入器。
    /// </summary>
    public void Apply(IDialogueGameStateWriter stateWriter)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (stateWriter == null || !IsConfigured())
        {
            return;
        }

        stateWriter.SetBool(key, value);
    }
}
