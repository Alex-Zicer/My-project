using System;

[Serializable]
// State write-back entry applied after dialogue completion.
public class DialogueStateMutation
{
    // Target key to write.
    public string key;

    // Target value type.
    public DialogueConditionValueType valueType = DialogueConditionValueType.Bool;

    // Bool value when valueType is Bool.
    public bool boolValue = true;

    // Int value when valueType is Int.
    public int intValue;

    // String value when valueType is String.
    public string stringValue = string.Empty;

    /// <summary>
    /// Checks whether this mutation is configured.
    /// </summary>
    /// <returns>True when key is valid.</returns>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>
    /// Applies this mutation into a game-state writer.
    /// </summary>
    /// <param name="stateWriter">State writer.</param>
    public void Apply(IDialogueGameStateWriter stateWriter)
    {
        if (stateWriter == null || !IsConfigured())
        {
            return;
        }

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
