using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonDialogueProvider : IDialogueProvider
{
    private const string SpeakerDatabaseResourcePath = "Dialogue/DialogueSpeakerDatabase";

    // 说话人资料表缓存。
    private static DialogueSpeakerDatabaseSO _speakerDatabase;

    [Serializable]
    private class DialogueJsonDocument
    {
        // dialogueId 标识。
        public string dialogueId;

        // startNodeId 标识。
        public string startNodeId;

        // nodes 运行时字段。
        public DialogueJsonNode[] nodes;
    }

    [Serializable]
    private class DialogueJsonNode
    {
        // nodeId 标识。
        public string nodeId;

        // speakerId 标识。
        public string speakerId;

        // content 运行时字段。
        public string content;

        // nextNodeId 标识。
        public string nextNodeId;

        // isEndNode 状态开关。
        public bool isEndNode;

        // choices 运行时字段。
        public DialogueJsonChoice[] choices;
    }

    [Serializable]
    private class DialogueJsonChoice
    {
        // choiceText 组件或配置引用。
        public string choiceText;

        // nextNodeId 标识。
        public string nextNodeId;
    }

    /// <summary>
    /// 判断提供器是否支持该数据源引用。
    /// </summary>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Json;
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

        string path = ResolvePath(reference.keyOrPath);
        // 守卫条件：不满足时直接返回，避免进入无效流程。
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

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (document == null || document.nodes == null || document.nodes.Length == 0)
        {
            error = $"JSON '{path}' has no node data.";
            return false;
        }

        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueJsonNode jsonNode in document.nodes)
        {
            // 守卫条件：不满足时直接返回，避免进入无效流程。
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
    /// 将外部节点数据转换为运行时节点。
    /// </summary>
    private static DialogueNodeData ConvertNode(DialogueJsonNode jsonNode)
    {
        ResolveSpeakerPresentation(jsonNode.speakerId, out string speakerName, out Sprite speakerPortrait);

        var node = new DialogueNodeData
        {
            nodeId = jsonNode.nodeId,
            speakerId = jsonNode.speakerId,
            speakerName = speakerName,
            content = jsonNode.content,
            nextNodeId = jsonNode.nextNodeId,
            isEndNode = jsonNode.isEndNode,
            speakerPortrait = speakerPortrait,
            choices = new List<DialogueChoiceData>()
        };

        if (jsonNode.choices != null)
        {
            // 遍历集合并逐项处理当前业务。
            for (int i = 0; i < jsonNode.choices.Length; i++)
            {
                DialogueJsonChoice jsonChoice = jsonNode.choices[i];
                // 守卫条件：不满足时直接返回，避免进入无效流程。
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
    /// 根据 speakerId 解析最终用于 UI 显示的名称和头像。
    /// </summary>
    /// <param name="speakerId">说话人标识。</param>
    /// <param name="speakerName">最终用于显示的名称。</param>
    /// <param name="speakerPortrait">最终用于显示的头像。</param>
    private static void ResolveSpeakerPresentation(string speakerId, out string speakerName, out Sprite speakerPortrait)
    {
        speakerName = speakerId;
        speakerPortrait = null;

        if (!TryGetSpeakerEntry(speakerId, out DialogueSpeakerEntry entry))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
        {
            speakerName = entry.DisplayName;
        }

        if (entry.Portrait != null)
        {
            speakerPortrait = entry.Portrait;
        }
    }

    /// <summary>
    /// 尝试从资料表中查找说话人信息。
    /// </summary>
    /// <param name="speakerId">说话人标识。</param>
    /// <param name="entry">查找到的资料项。</param>
    /// <returns>查找到返回 true，否则返回 false。</returns>
    private static bool TryGetSpeakerEntry(string speakerId, out DialogueSpeakerEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(speakerId))
        {
            return false;
        }

        DialogueSpeakerDatabaseSO database = LoadSpeakerDatabase();
        return database != null && database.TryGetSpeaker(speakerId, out entry);
    }

    /// <summary>
    /// 加载对话说话人资料表。
    /// </summary>
    /// <returns>资料表资源；未找到时返回 null。</returns>
    private static DialogueSpeakerDatabaseSO LoadSpeakerDatabase()
    {
        if (_speakerDatabase == null)
        {
            _speakerDatabase = Resources.Load<DialogueSpeakerDatabaseSO>(SpeakerDatabaseResourcePath);
        }

        return _speakerDatabase;
    }

    /// <summary>
    /// 将配置路径解析为可访问的绝对路径。
    /// </summary>
    private static string ResolvePath(string keyOrPath)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
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
