using System.Collections.Generic;

// ScriptableObject dialogue data provider.
public class SoDialogueProvider : IDialogueProvider
{
    /// <summary>
    /// Checks whether this provider can handle the input reference.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <returns>True when source type is SO.</returns>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.So;
    }

    /// <summary>
    /// Loads a dialogue graph from a ScriptableObject source.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <param name="graph">Loaded graph output.</param>
    /// <param name="error">Error message output.</param>
    /// <returns>True when load and validation both succeed.</returns>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        if (reference == null)
        {
            error = "DialogueReference is null.";
            return false;
        }

        if (reference.primarySO == null)
        {
            error = "Source type is SO but primarySO is not assigned.";
            return false;
        }

        DialogueDataSO so = reference.primarySO;
        if (so.nodes == null || so.nodes.Count == 0)
        {
            error = $"Dialogue SO '{so.name}' has no node data.";
            return false;
        }

        // Build node map and validate unique node ids.
        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueNodeData node in so.nodes)
        {
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
    /// Deep-copies one dialogue node into runtime data.
    /// </summary>
    /// <param name="source">Source node.</param>
    /// <returns>Cloned node.</returns>
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
            for (int i = 0; i < source.choices.Count; i++)
            {
                DialogueChoiceData choice = source.choices[i];
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
