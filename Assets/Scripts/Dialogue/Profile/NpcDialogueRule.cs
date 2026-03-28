using System;
using System.Collections.Generic;

[Serializable]
// Rule item inside an NPC dialogue profile.
public class NpcDialogueRule
{
    // Rule identifier.
    public string ruleId = "rule";

    // Whether this rule is enabled.
    public bool enabled = true;

    // Higher value means higher priority.
    public int priority;

    // Condition combiner mode.
    public DialogueConditionMode conditionMode = DialogueConditionMode.All;

    // Condition list.
    public List<DialogueCondition> conditions = new List<DialogueCondition>();

    // First-time dialogue reference.
    public DialogueReference firstDialogueReference = new DialogueReference();

    // First dialogue repeat policy when repeat reference is not configured.
    public DialogueRepeatPolicy firstRepeatPolicy = DialogueRepeatPolicy.Once;

    // Repeat dialogue reference.
    public DialogueReference repeatDialogueReference = new DialogueReference();

    // Repeat dialogue repeat policy.
    public DialogueRepeatPolicy repeatRepeatPolicy = DialogueRepeatPolicy.Repeatable;

    // State mutations applied after first dialogue completion.
    public List<DialogueStateMutation> onFirstCompleted = new List<DialogueStateMutation>();

    // State mutations applied after repeat dialogue completion.
    public List<DialogueStateMutation> onRepeatCompleted = new List<DialogueStateMutation>();

    /// <summary>
    /// Evaluates whether current game state matches this rule.
    /// </summary>
    /// <param name="stateReader">State reader.</param>
    /// <returns>True when rule matches.</returns>
    public bool IsMatch(IDialogueGameStateReader stateReader)
    {
        if (!enabled)
        {
            return false;
        }

        if (conditions == null || conditions.Count == 0)
        {
            return true;
        }

        bool hasEvaluatedAny = false;
        if (conditionMode == DialogueConditionMode.All)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                DialogueCondition condition = conditions[i];
                if (condition == null || !condition.enabled)
                {
                    continue;
                }

                hasEvaluatedAny = true;
                if (!condition.Evaluate(stateReader))
                {
                    return false;
                }
            }

            return true;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            DialogueCondition condition = conditions[i];
            if (condition == null || !condition.enabled)
            {
                continue;
            }

            hasEvaluatedAny = true;
            if (condition.Evaluate(stateReader))
            {
                return true;
            }
        }

        return !hasEvaluatedAny;
    }
}
