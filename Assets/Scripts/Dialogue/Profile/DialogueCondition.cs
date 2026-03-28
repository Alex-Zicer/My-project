using System;

[Serializable]
// One condition item inside a dialogue rule.
public class DialogueCondition
{
    // Whether this condition is enabled.
    public bool enabled = true;

    // State key to read.
    public string key;

    // Value type to read and compare.
    public DialogueConditionValueType valueType = DialogueConditionValueType.Bool;

    // Comparison mode.
    public DialogueConditionComparison comparison = DialogueConditionComparison.IsTrue;

    // Expected bool value when valueType is Bool.
    public bool boolValue = true;

    // Expected int value when valueType is Int.
    public int intValue;

    // Expected string value when valueType is String.
    public string stringValue = string.Empty;

    /// <summary>
    /// Evaluates this condition with a state reader.
    /// </summary>
    /// <param name="stateReader">State reader.</param>
    /// <returns>True when condition passes.</returns>
    public bool Evaluate(IDialogueGameStateReader stateReader)
    {
        if (!enabled)
        {
            return true;
        }

        if (stateReader == null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

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

    /// <summary>
    /// Evaluates bool-type condition.
    /// </summary>
    /// <param name="stateReader">State reader.</param>
    /// <returns>True when comparison passes.</returns>
    private bool EvaluateBool(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetBool(key, out bool currentValue))
        {
            return false;
        }

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

    /// <summary>
    /// Evaluates int-type condition.
    /// </summary>
    /// <param name="stateReader">State reader.</param>
    /// <returns>True when comparison passes.</returns>
    private bool EvaluateInt(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetInt(key, out int currentValue))
        {
            return false;
        }

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

    /// <summary>
    /// Evaluates string-type condition.
    /// </summary>
    /// <param name="stateReader">State reader.</param>
    /// <returns>True when comparison passes.</returns>
    private bool EvaluateString(IDialogueGameStateReader stateReader)
    {
        if (!stateReader.TryGetString(key, out string currentValue))
        {
            return false;
        }

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
