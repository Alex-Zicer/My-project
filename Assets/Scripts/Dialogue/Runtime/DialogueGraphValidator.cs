using System.Collections.Generic;
using System.Text;

// Static validator for dialogue graph integrity.
public static class DialogueGraphValidator
{
    /// <summary>
    /// Validates a dialogue graph and returns human-readable errors.
    /// </summary>
    /// <param name="graph">Graph to validate.</param>
    /// <param name="error">Validation error output.</param>
    /// <returns>True when graph is valid.</returns>
    public static bool TryValidate(DialogueGraph graph, out string error)
    {
        var issues = new StringBuilder();

        if (graph == null)
        {
            error = "Dialogue graph is null.";
            return false;
        }

        if (graph.Nodes == null || graph.Nodes.Count == 0)
        {
            error = "Dialogue graph has no nodes.";
            return false;
        }

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

            if (node == null)
            {
                issues.AppendLine($"Node '{id}' is null.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(node.nextNodeId) && !graph.Nodes.ContainsKey(node.nextNodeId))
            {
                issues.AppendLine($"Node '{id}' references missing nextNodeId '{node.nextNodeId}'.");
            }

            if (node.choices != null)
            {
                for (int i = 0; i < node.choices.Count; i++)
                {
                    DialogueChoiceData choice = node.choices[i];
                    if (choice == null)
                    {
                        issues.AppendLine($"Node '{id}' has a null choice at index {i}.");
                        continue;
                    }

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

            // Non-end nodes should have at least one outgoing path.
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
