// Comparison mode used by dialogue conditions.
public enum DialogueConditionComparison
{
    // Equal to expected value.
    Equals,

    // Not equal to expected value.
    NotEquals,

    // Greater than expected value.
    Greater,

    // Greater than or equal to expected value.
    GreaterOrEqual,

    // Less than expected value.
    Less,

    // Less than or equal to expected value.
    LessOrEqual,

    // Current bool is true.
    IsTrue,

    // Current bool is false.
    IsFalse,

    // Key exists in state.
    Exists,

    // Key does not exist in state.
    NotExists
}
