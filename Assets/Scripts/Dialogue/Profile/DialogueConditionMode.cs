using System;

/// <summary>
/// 条件组合方式枚举（仅为兼容旧数据保留）。
/// </summary>
[Obsolete("路由已固定为“全部条件满足”，此枚举仅保留用于兼容旧序列化数据。")]
public enum DialogueConditionMode
{
    All = 0,
    Any = 1
}
