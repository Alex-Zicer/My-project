// 条件比较方式：用于条件评估时的比较规则。
public enum DialogueConditionComparison
{
    // 等于。
    Equals,
    // 不等于。
    NotEquals,
    // 大于。
    Greater,
    // 大于等于。
    GreaterOrEqual,
    // 小于。
    Less,
    // 小于等于。
    LessOrEqual,
    // 为真（布尔专用）。
    IsTrue,
    // 为假（布尔专用）。
    IsFalse,
    // 键存在。
    Exists,
    // 键不存在。
    NotExists
}
