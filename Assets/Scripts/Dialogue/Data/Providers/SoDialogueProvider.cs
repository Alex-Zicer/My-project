using System.Collections.Generic;

public class SoDialogueProvider : IDialogueProvider
{
    /// <summary>
    /// 判断提供器是否支持该数据源引用。
    /// </summary>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.So;
    }

    /// <summary>
    /// 尝试加载对话数据并输出对话图。
    /// </summary>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (reference == null)
        {
            error = "DialogueReference is null.";
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (reference.primarySO == null)
        {
            error = "Source type is SO but primarySO is not assigned.";
            return false;
        }

        DialogueDataSO so = reference.primarySO;
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (so.nodes == null || so.nodes.Count == 0)
        {
            error = $"Dialogue SO '{so.name}' has no node data.";
            return false;
        }

        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueNodeData node in so.nodes)
        {
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
            {
                error = $"Dialogue SO '{so.name}' contains a node with empty nodeId.";
                return false;
            }

            if (nodeMap.ContainsKey(node.nodeId))
            {
                error = $"Dialogue SO '{so.name}' contains duplicate nodeId '{node.nodeId}'.";
                return false;
            }

            nodeMap.Add(node.nodeId, CloneNode(node));
        }

        graph = new DialogueGraph(so.dialogueId, so.startNodeId, nodeMap);
        if (!DialogueGraphValidator.TryValidate(graph, out error))
        {
            graph = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 深拷贝节点数据，避免运行时污染源数据。
    /// </summary>
    private static DialogueNodeData CloneNode(DialogueNodeData source)
    {
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
            // 遍历集合并逐项处理当前业务。
            for (int i = 0; i < source.choices.Count; i++)
            {
                DialogueChoiceData choice = source.choices[i];
                // 守卫条件：不满足时直接返回，避免进入无效流程。
                if (choice == null)
                {
                    continue;
                }

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
