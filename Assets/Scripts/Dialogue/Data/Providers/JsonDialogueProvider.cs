using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// JSON 数据提供者：
// 读取 JSON 文档并映射到 DialogueGraph，便于接入表格导出或外部编辑工具链。
public class JsonDialogueProvider : IDialogueProvider
{
    // 与 JSON 结构对应的中间模型，只用于反序列化阶段。
    [Serializable]
    private class DialogueJsonDocument
    {
        public string dialogueId;
        public string startNodeId;
        public DialogueJsonNode[] nodes;
    }

    [Serializable]
    private class DialogueJsonNode
    {
        public string nodeId;
        public string speakerName;
        public string portraitResourcePath;
        public string content;
        public string nextNodeId;
        public bool isEndNode;
        public DialogueJsonChoice[] choices;
    }

    [Serializable]
    private class DialogueJsonChoice
    {
        public string choiceText;
        public string nextNodeId;
    }

    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Json;
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

        string path = ResolvePath(reference.keyOrPath);
        // JSON 模式下 keyOrPath 必填；支持相对路径和绝对路径。
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "当前来源为 JSON，但 keyOrPath 为空。";
            return false;
        }

        if (!File.Exists(path))
        {
            error = $"未找到对话 JSON 文件: {path}";
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            error = $"读取 JSON 文件失败 '{path}': {ex.Message}";
            return false;
        }

        DialogueJsonDocument document;
        try
        {
            document = JsonUtility.FromJson<DialogueJsonDocument>(json);
        }
        catch (Exception ex)
        {
            error = $"解析 JSON 文件失败 '{path}': {ex.Message}";
            return false;
        }

        if (document == null || document.nodes == null || document.nodes.Length == 0)
        {
            error = $"对话 JSON '{path}' 没有节点数据。";
            return false;
        }

        var nodeMap = new Dictionary<string, DialogueNodeData>();
        foreach (DialogueJsonNode jsonNode in document.nodes)
        {
            // 与 SO Provider 一致：nodeId 是必须且唯一的。
            if (jsonNode == null || string.IsNullOrWhiteSpace(jsonNode.nodeId))
            {
                error = $"对话 JSON '{path}' 存在 nodeId 为空的节点。";
                return false;
            }

            if (nodeMap.ContainsKey(jsonNode.nodeId))
            {
                error = $"对话 JSON '{path}' 存在重复 nodeId '{jsonNode.nodeId}'。";
                return false;
            }

            nodeMap.Add(jsonNode.nodeId, ConvertNode(jsonNode));
        }

        graph = new DialogueGraph(document.dialogueId, document.startNodeId, nodeMap);
        // 统一校验，确保不同来源数据具备一致运行质量。
        if (!DialogueGraphValidator.TryValidate(graph, out error))
        {
            graph = null;
            return false;
        }

        return true;
    }

    private static DialogueNodeData ConvertNode(DialogueJsonNode jsonNode)
    {
        // 把 JSON 节点映射为运行时节点结构。
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
            foreach (DialogueJsonChoice jsonChoice in jsonNode.choices)
            {
                // 允许过滤空选项，降低数据清洗成本。
                if (jsonChoice == null) continue;
                node.choices.Add(new DialogueChoiceData
                {
                    choiceText = jsonChoice.choiceText,
                    nextNodeId = jsonChoice.nextNodeId
                });
            }
        }

        return node;
    }

    private static Sprite TryLoadPortrait(string resourcePath)
    {
        // 头像按 Resources 路径加载；路径为空时返回 null（可无头像）。
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        return Resources.Load<Sprite>(resourcePath);
    }

    private static string ResolvePath(string keyOrPath)
    {
        // 相对路径默认以 StreamingAssets 为根，便于跨平台打包与热更数据管理。
        if (string.IsNullOrWhiteSpace(keyOrPath)) return string.Empty;
        if (Path.IsPathRooted(keyOrPath)) return keyOrPath;
        return Path.Combine(Application.streamingAssetsPath, keyOrPath);
    }
}
