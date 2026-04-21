using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoiceData
{
    // choiceText 组件或配置引用。
    public string choiceText;

    // nextNodeId 标识。
    public string nextNodeId;
}

[Serializable]
public class DialogueNodeData
{
    // nodeId 标识。
    public string nodeId;

    // speakerId 标识。
    public string speakerId;

    // speakerName 运行时字段。
    public string speakerName;

    // speakerPortrait 运行时字段。
    public Sprite speakerPortrait;

    [TextArea]
    // content 运行时字段。
    public string content;

    // nextNodeId 标识。
    public string nextNodeId;

    // isEndNode 状态开关。
    public bool isEndNode;

    // 当前节点可选分支列表。
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
}

public class DialogueGraph
{
    public string DialogueId { get; }

    public string StartNodeId { get; }

    // 节点字典，键为节点 ID。
    private readonly Dictionary<string, DialogueNodeData> _nodes;

    // 只读节点视图，供外部查询节点内容。
    public IReadOnlyDictionary<string, DialogueNodeData> Nodes => _nodes;

    /// <summary>
    /// 构建对话图并缓存节点映射。
    /// </summary>
    public DialogueGraph(string dialogueId, string startNodeId, Dictionary<string, DialogueNodeData> nodes)
    {
        DialogueId = dialogueId;
        StartNodeId = startNodeId;
        _nodes = nodes ?? new Dictionary<string, DialogueNodeData>();
    }

    /// <summary>
    /// 按节点 ID 尝试获取节点数据。
    /// </summary>
    public bool TryGetNode(string nodeId, out DialogueNodeData node)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            node = null;
            return false;
        }

        return _nodes.TryGetValue(nodeId, out node);
    }
}
