using System.Collections.Generic;
using System.Text;

// 对话图静态校验器：
// 在“进入运行时前”尽早发现断链、缺失节点和无出口节点，避免对话进行中崩流程。
public static class DialogueGraphValidator
{
    // 返回 true 表示图结构可运行；false 时 error 含可读错误详情（多行）。
    public static bool TryValidate(DialogueGraph graph, out string error)
    {
        var issues = new StringBuilder();

        if (graph == null)
        {
            error = "对话图为空。";
            return false;
        }

        if (graph.Nodes == null || graph.Nodes.Count == 0)
        {
            error = "对话图没有节点数据。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(graph.StartNodeId))
        {
            issues.AppendLine("StartNodeId 为空。");
        }
        else if (!graph.Nodes.ContainsKey(graph.StartNodeId))
        {
            issues.AppendLine($"StartNodeId '{graph.StartNodeId}' 不存在。");
        }

        foreach (KeyValuePair<string, DialogueNodeData> pair in graph.Nodes)
        {
            string id = pair.Key;
            DialogueNodeData node = pair.Value;

            // 节点值为空说明数据映射阶段出现了脏数据。
            if (node == null)
            {
                issues.AppendLine($"节点 '{id}' 为空。");
                continue;
            }

            // 校验线性跳转引用合法性。
            if (!string.IsNullOrWhiteSpace(node.nextNodeId) && !graph.Nodes.ContainsKey(node.nextNodeId))
            {
                issues.AppendLine($"节点 '{id}' 指向缺失的 nextNodeId '{node.nextNodeId}'。");
            }

            // 校验分支跳转引用合法性。
            if (node.choices != null)
            {
                for (int i = 0; i < node.choices.Count; i++)
                {
                    DialogueChoiceData choice = node.choices[i];
                    if (choice == null)
                    {
                        issues.AppendLine($"节点 '{id}' 在索引 {i} 处有空选项。");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(choice.nextNodeId))
                    {
                        issues.AppendLine($"节点 '{id}' 的 choice[{i}] nextNodeId 为空。");
                    }
                    else if (!graph.Nodes.ContainsKey(choice.nextNodeId))
                    {
                        issues.AppendLine(
                            $"节点 '{id}' 的 choice[{i}] 指向缺失的 nextNodeId '{choice.nextNodeId}'。");
                    }
                }
            }

            // 非结束节点必须有可继续路径（线性 next 或分支 choices 之一）。
            bool hasChoices = node.choices != null && node.choices.Count > 0;
            bool hasLinearNext = !string.IsNullOrWhiteSpace(node.nextNodeId);
            if (!node.isEndNode && !hasChoices && !hasLinearNext)
            {
                issues.AppendLine($"节点 '{id}' 不是结束节点，但没有 nextNodeId 且没有选项。");
            }
        }

        error = issues.ToString().Trim();
        return string.IsNullOrEmpty(error);
    }
}
