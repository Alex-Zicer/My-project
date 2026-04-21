using System;
using UnityEngine;

/// <summary>
/// DialogueSourceType 枚举定义。
/// </summary>
public enum DialogueSourceType
{
    So,
    Json,
    Csv,
    Custom
}

/// <summary>
/// 对话引用配置：描述来源与首次/重复入口节点。
/// </summary>
[Serializable]
public class DialogueReference
{
    // sourceType 运行时字段。
    public DialogueSourceType sourceType = DialogueSourceType.So;

    // primarySO 运行时字段。
    public DialogueDataSO primarySO;

    // keyOrPath 运行时字段。
    public string keyOrPath;

    // fallbackSO 运行时字段。
    public DialogueDataSO fallbackSO;

    [Header("起始节点")]
    [Tooltip("首次播放时的起始节点，为空则使用对话图默认起始节点")]
    // firstStartNodeId 标识。
    public string firstStartNodeId;

    [Tooltip("重复播放时的起始节点，为空则使用 firstStartNodeId")]
    // repeatStartNodeId 标识。
    public string repeatStartNodeId;

    /// <summary>
    /// 判断当前引用是否至少配置了一个可用数据源。
    /// </summary>
    public bool IsConfigured()
    {
        if (primarySO != null) return true;
        if (fallbackSO != null) return true;
        return !string.IsNullOrWhiteSpace(keyOrPath);
    }

    /// <summary>
    /// 从 ScriptableObject 快速构造对话引用。
    /// </summary>
    public static DialogueReference FromSo(DialogueDataSO so, string startNodeId = null)
    {
        return new DialogueReference
        {
            sourceType = DialogueSourceType.So,
            primarySO = so,
            firstStartNodeId = startNodeId
        };
    }

    /// <summary>
    /// 从 JSON 路径快速构造对话引用。
    /// </summary>
    public static DialogueReference FromJson(string path, string startNodeId = null)
    {
        return new DialogueReference
        {
            sourceType = DialogueSourceType.Json,
            keyOrPath = path,
            firstStartNodeId = startNodeId
        };
    }
}
