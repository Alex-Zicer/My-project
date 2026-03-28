using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// JSON dialogue data provider.
public class JsonDialogueProvider : IDialogueProvider
{
    [Serializable]
    private class DialogueJsonDocument
    {
        // Dialogue identifier.
        public string dialogueId;

        // Start node identifier.
        public string startNodeId;

        // Node array payload.
        public DialogueJsonNode[] nodes;
    }

    [Serializable]
    private class DialogueJsonNode
    {
        // Node identifier.
        public string nodeId;

        // Speaker display name.
        public string speakerName;

        // Portrait resource path in Resources.
        public string portraitResourcePath;

        // Content text.
        public string content;

        // Linear next node id.
        public string nextNodeId;

        // End node flag.
        public bool isEndNode;

        // Choice payload.
        public DialogueJsonChoice[] choices;
    }

    [Serializable]
    private class DialogueJsonChoice
    {
        // Choice text.
        public string choiceText;

        // Choice target node id.
        public string nextNodeId;
    }

    /// <summary>
    /// Checks whether this provider can handle the input reference.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <returns>True when source type is JSON.</returns>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Json;
    }

    /// <summary>
    /// Loads a JSON dialogue file and converts it into a dialogue graph.
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

        string path = ResolvePath(reference.keyOrPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Source type is JSON but keyOrPath is empty.";
            return false;
        }

        if (!File.Exists(path))
        {
            error = $"JSON file not found: {path}";
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            error = $"Failed to read JSON file '{path}': {ex.Message}";
            return false;
        }

        DialogueJsonDocument document;
        try
        {
            document = JsonUtility.FromJson<DialogueJsonDocument>(json);
        }
        catch (Exception ex)
        {
            error = $"Failed to parse JSON file '{path}': {ex.Message}";
            return false;
        }

        if (document == null || document.nodes == null || document.nodes.Length == 0)
        {
            error = $"JSON '{path}' has no node data.";
            return false;
        }

        // Build node map and validate unique node ids.
        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueJsonNode jsonNode in document.nodes)
        {
            if (jsonNode == null || string.IsNullOrWhiteSpace(jsonNode.nodeId))
            {
                error = $"JSON '{path}' contains a node with empty nodeId.";
                return false;
            }

            if (nodeMap.ContainsKey(jsonNode.nodeId))
            {
                error = $"JSON '{path}' contains duplicate nodeId '{jsonNode.nodeId}'.";
                return false;
            }

            nodeMap.Add(jsonNode.nodeId, ConvertNode(jsonNode));
        }

        graph = new DialogueGraph(document.dialogueId, document.startNodeId, nodeMap);
        if (!DialogueGraphValidator.TryValidate(graph, out error))
        {
            graph = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts one JSON node payload into runtime node data.
    /// </summary>
    /// <param name="jsonNode">JSON node payload.</param>
    /// <returns>Converted runtime node.</returns>
    private static DialogueNodeData ConvertNode(DialogueJsonNode jsonNode)
    {
        var node = new DialogueNodeData
        {
            nodeId = jsonNode.nodeId,
            speakerName = jsonNode.speakerName,
            content = jsonNode.content,
            nextNodeId = jsonNode.nextNodeId,
            isEndNode = jsonNode.isEndNode,
            speakerPortrait = TryLoadPortrait(jsonNode.portraitResourcePath),
            choices = new List<DialogueChoiceData>()
        };

        if (jsonNode.choices != null)
        {
            for (int i = 0; i < jsonNode.choices.Length; i++)
            {
                DialogueJsonChoice jsonChoice = jsonNode.choices[i];
                if (jsonChoice == null)
                {
                    continue;
                }

                node.choices.Add(new DialogueChoiceData
                {
                    choiceText = jsonChoice.choiceText,
                    nextNodeId = jsonChoice.nextNodeId
                });
            }
        }

        return node;
    }

    /// <summary>
    /// Tries to load a portrait from the Resources path.
    /// </summary>
    /// <param name="resourcePath">Resource path under Resources.</param>
    /// <returns>Loaded sprite or null.</returns>
    private static Sprite TryLoadPortrait(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    /// <summary>
    /// Resolves keyOrPath to an absolute file path.
    /// </summary>
    /// <param name="keyOrPath">Configured key or path.</param>
    /// <returns>Absolute path or empty string.</returns>
    private static string ResolvePath(string keyOrPath)
    {
        if (string.IsNullOrWhiteSpace(keyOrPath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(keyOrPath))
        {
            return keyOrPath;
        }

        return Path.Combine(Application.streamingAssetsPath, keyOrPath);
    }
}
