using System;
using System.Collections.Generic;

/// <summary>
/// NPC 对话规则：包含优先级、条件与对应对话引用。
/// </summary>
[Serializable]
public class NpcDialogueRule
{
    // ruleId 标识。
    public string ruleId = "rule";

    // enabled 运行时字段。
    public bool enabled = true;

    // priority 运行时字段。
    public int priority;

    // 命中此规则所需满足的布尔条件列表。
    public List<DialogueCondition> conditions = new List<DialogueCondition>();

    // 规则命中后要播放的对话引用。
    public DialogueReference dialogueReference = new DialogueReference();

    // 对话完成后要执行的状态写回列表。
    public List<DialogueStateMutation> onCompleted = new List<DialogueStateMutation>();

    /// <summary>
    /// 判断当前规则是否满足状态条件。
    /// </summary>
    public bool IsMatch(IDialogueGameStateReader stateReader)
    {
        if (!enabled)
        {
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (conditions == null || conditions.Count == 0)
        {
            return true;
        }

        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < conditions.Count; i++)
        {
            DialogueCondition condition = conditions[i];
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (condition == null || !condition.enabled)
            {
                continue;
            }

            if (!condition.Evaluate(stateReader))
            {
                return false;
            }
        }

        return true;
    }
}
