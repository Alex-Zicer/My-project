using System.Collections.Generic;
using System.Text;

public static class DialogueGraphValidator
{
    /// <summary>
    /// 校验对话图结构是否完整可运行。
    /// </summary>
    public static bool TryValidate(DialogueGraph graph, out string error)
    {
        var issues = new StringBuilder();

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (graph == null)
        {
            error = "Dialogue graph is null.";
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (graph.Nodes == null || graph.Nodes.Count == 0)
        {
            error = "Dialogue graph has no nodes.";
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(graph.StartNodeId))
        {
            issues.AppendLine("StartNodeId is empty.");
        }
        else if (!graph.Nodes.ContainsKey(graph.StartNodeId))
        {
            issues.AppendLine($"StartNodeId '{graph.StartNodeId}' does not exist.");
        }

        foreach (KeyValuePair<string, DialogueNodeData> pair in graph.Nodes)
        {
            string id = pair.Key;
            DialogueNodeData node = pair.Value;

            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (node == null)
            {
                issues.AppendLine($"Node '{id}' is null.");
                continue;
            }

            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (!string.IsNullOrWhiteSpace(node.nextNodeId) && !graph.Nodes.ContainsKey(node.nextNodeId))
            {
                issues.AppendLine($"Node '{id}' references missing nextNodeId '{node.nextNodeId}'.");
            }

            if (node.choices != null)
            {
                // 遍历集合并逐项处理当前业务。
                for (int i = 0; i < node.choices.Count; i++)
                {
                    DialogueChoiceData choice = node.choices[i];
                    // 守卫条件：不满足时直接返回，避免进入无效流程。
                    if (choice == null)
                    {
                        issues.AppendLine($"Node '{id}' has a null choice at index {i}.");
                        continue;
                    }

                    // 守卫条件：不满足时直接返回，避免进入无效流程。
                    if (string.IsNullOrWhiteSpace(choice.nextNodeId))
                    {
                        issues.AppendLine($"Node '{id}' choice[{i}] has empty nextNodeId.");
                    }
                    else if (!graph.Nodes.ContainsKey(choice.nextNodeId))
                    {
                        issues.AppendLine($"Node '{id}' choice[{i}] references missing nextNodeId '{choice.nextNodeId}'.");
                    }
                }
            }

            bool hasChoices = node.choices != null && node.choices.Count > 0;
            bool hasLinearNext = !string.IsNullOrWhiteSpace(node.nextNodeId);
            if (!node.isEndNode && !hasChoices && !hasLinearNext)
            {
                issues.AppendLine($"Node '{id}' is not an end node and has no outgoing path.");
            }
        }

        error = issues.ToString().Trim();
        return string.IsNullOrEmpty(error);
    }
}
