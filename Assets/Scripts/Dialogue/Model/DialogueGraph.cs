using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoiceData
{
    // 玩家在该选项按钮上看到的文案。
    public string choiceText;
    // 点击该选项后跳转到的目标节点 ID。
    public string nextNodeId;
}

[Serializable]
public class DialogueNodeData
{
    // 节点唯一 ID，供 nextNodeId/choice.nextNodeId 跳转引用。
    public string nodeId;
    // 说话人显示名（可为空）。
    public string speakerName;
    // 说话人头像（可为空）。
    public Sprite speakerPortrait;
    // 台词正文。可为空字符串（例如只展示头像变化）。
    [TextArea]
    public string content;
    // 线性推进目标节点：无选项时用于“下一句”跳转。
    public string nextNodeId;
    // 标记为结束节点时，运行层在读完当前句后直接结束对话。
    public bool isEndNode;
    // 分支选项列表；非空时由运行层进入 WaitingChoice 状态。
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
}

// 运行时对话图：
// DialogueService 只依赖该统一结构，不关心原始数据来源是 SO、JSON 还是 CSV。
public class DialogueGraph
{
    // 对话标识，可用于调试或埋点。
    public string DialogueId { get; }
    // 对话入口节点 ID。
    public string StartNodeId { get; }
    private readonly Dictionary<string, DialogueNodeData> _nodes;

    // 只读视图：外部可查询不可直接替换字典引用。
    public IReadOnlyDictionary<string, DialogueNodeData> Nodes => _nodes;

    public DialogueGraph(string dialogueId, string startNodeId, Dictionary<string, DialogueNodeData> nodes)
    {
        DialogueId = dialogueId;
        StartNodeId = startNodeId;
        _nodes = nodes ?? new Dictionary<string, DialogueNodeData>();
    }

    // 按节点 ID 查询节点。
    // 若 nodeId 为空或不存在，返回 false。
    public bool TryGetNode(string nodeId, out DialogueNodeData node)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            node = null;
            return false;
        }

        return _nodes.TryGetValue(nodeId, out node);
    }
}
