using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoiceData
{
    // Choice text shown to player.
    public string choiceText;

    // Target node id after selection.
    public string nextNodeId;
}

[Serializable]
public class DialogueNodeData
{
    // Unique node id.
    public string nodeId;

    // Speaker name.
    public string speakerName;

    // Speaker portrait.
    public Sprite speakerPortrait;

    [TextArea]
    // Dialogue content text.
    public string content;

    // Next node id for linear flow.
    public string nextNodeId;

    // Whether this is an end node.
    public bool isEndNode;

    // Branch choice list.
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
}

// Runtime dialogue graph model.
public class DialogueGraph
{
    // Dialogue identifier.
    public string DialogueId { get; }

    // Start node id.
    public string StartNodeId { get; }

    // Node map.
    private readonly Dictionary<string, DialogueNodeData> _nodes;

    // Read-only node dictionary.
    public IReadOnlyDictionary<string, DialogueNodeData> Nodes => _nodes;

    /// <summary>
    /// Creates a dialogue graph.
    /// </summary>
    /// <param name="dialogueId">Dialogue identifier.</param>
    /// <param name="startNodeId">Start node id.</param>
    /// <param name="nodes">Node map.</param>
    public DialogueGraph(string dialogueId, string startNodeId, Dictionary<string, DialogueNodeData> nodes)
    {
        DialogueId = dialogueId;
        StartNodeId = startNodeId;
        _nodes = nodes ?? new Dictionary<string, DialogueNodeData>();
    }

    /// <summary>
    /// Tries to get one node by id.
    /// </summary>
    /// <param name="nodeId">Node id.</param>
    /// <param name="node">Node output.</param>
    /// <returns>True when node exists.</returns>
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
