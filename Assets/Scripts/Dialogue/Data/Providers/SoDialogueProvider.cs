using System.Collections.Generic;

// SO 数据提供者：
// 把 DialogueDataSO 转成运行时可消费的 DialogueGraph，并进行基础合法性校验。
public class SoDialogueProvider : IDialogueProvider
{
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.So;
    }

    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        if (reference == null)
        {
            error = "DialogueReference 为空。";
            return false;
        }

        if (reference.primarySO == null)
        {
            error = "当前来源为 SO，但 primarySO 未设置。";
            return false;
        }

        DialogueDataSO so = reference.primarySO;
        // 没有节点时无法构成可执行对话图。
        if (so.nodes == null || so.nodes.Count == 0)
        {
            error = $"对话 SO '{so.name}' 没有节点数据。";
            return false;
        }

        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueNodeData node in so.nodes)
        {
            // nodeId 是节点寻址键，缺失会导致跳转失败。
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
            {
                error = $"对话 SO '{so.name}' 存在 nodeId 为空的节点。";
                return false;
            }

            // 同一对话图内 nodeId 必须唯一。
            if (nodeMap.ContainsKey(node.nodeId))
            {
                error = $"对话 SO '{so.name}' 存在重复 nodeId '{node.nodeId}'。";
                return false;
            }

            // 运行时使用深拷贝，避免直接修改资产对象。
            nodeMap.Add(node.nodeId, CloneNode(node));
        }

        graph = new DialogueGraph(so.dialogueId, so.startNodeId, nodeMap);
        // 统一复用图校验器，提前发现断链节点与无效跳转。
        if (!DialogueGraphValidator.TryValidate(graph, out error))
        {
            graph = null;
            return false;
        }

        return true;
    }

    private static DialogueNodeData CloneNode(DialogueNodeData source)
    {
        // 仅拷贝运行时必需字段，保持加载过程简单可控。
        var node = new DialogueNodeData
        {
            nodeId = source.nodeId,
            speakerName = source.speakerName,
            speakerPortrait = source.speakerPortrait,
            content = source.content,
            nextNodeId = source.nextNodeId,
            isEndNode = source.isEndNode,
            choices = new List<DialogueChoiceData>()
        };

        if (source.choices != null)
        {
            foreach (DialogueChoiceData choice in source.choices)
            {
                // 允许跳过空选项，防止脏数据直接中断整个加载流程。
                if (choice == null) continue;
                node.choices.Add(new DialogueChoiceData
                {
                    choiceText = choice.choiceText,
                    nextNodeId = choice.nextNodeId
                });
            }
        }

        return node;
    }
}
