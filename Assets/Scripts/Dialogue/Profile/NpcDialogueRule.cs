using System;
using System.Collections.Generic;

// NPC 对话规则：用于根据剧情状态选择首播/重复对话，并在完成后回写状态。
[Serializable]
public class NpcDialogueRule
{
    // 规则标识（建议在同一 Profile 内唯一）。
    public string ruleId = "rule";
    // 是否启用该规则。
    public bool enabled = true;
    // 规则优先级（越大越先匹配）。
    public int priority;

    // 条件组合模式（All/Any）。
    public DialogueConditionMode conditionMode = DialogueConditionMode.All;
    // 命中该规则需要满足的条件列表。
    public List<DialogueCondition> conditions = new List<DialogueCondition>();

    // 首次命中时使用的对话引用。
    public DialogueReference firstDialogueReference = new DialogueReference();
    // 首次对话在“首次之后”是否还能继续使用（当未配置 repeat 对话时生效）。
    public DialogueRepeatPolicy firstRepeatPolicy = DialogueRepeatPolicy.Once;

    // 首次之后使用的重复对话引用（可选）。
    public DialogueReference repeatDialogueReference = new DialogueReference();
    // 重复对话是否允许再次重复。
    public DialogueRepeatPolicy repeatRepeatPolicy = DialogueRepeatPolicy.Repeatable;

    // 首次对话结束后的状态写回列表（可选）。
    public List<DialogueStateMutation> onFirstCompleted = new List<DialogueStateMutation>();
    // 重复对话结束后的状态写回列表（可选）。
    public List<DialogueStateMutation> onRepeatCompleted = new List<DialogueStateMutation>();

    // 根据 conditionMode 评估本规则是否命中当前游戏状态。
    public bool IsMatch(IDialogueGameStateReader stateReader)
    {
        if (!enabled) return false;
        if (conditions == null || conditions.Count == 0) return true;

        bool hasEvaluatedAny = false;

        if (conditionMode == DialogueConditionMode.All)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                DialogueCondition condition = conditions[i];
                if (condition == null || !condition.enabled) continue;
                hasEvaluatedAny = true;
                if (!condition.Evaluate(stateReader)) return false;
            }
            return true;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            DialogueCondition condition = conditions[i];
            if (condition == null || !condition.enabled) continue;
            hasEvaluatedAny = true;
            if (condition.Evaluate(stateReader)) return true;
        }

        return !hasEvaluatedAny;
    }
}
