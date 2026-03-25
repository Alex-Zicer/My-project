using System.Collections.Generic;
using UnityEngine;

// 对话资源文件（ScriptableObject）：
// 设计师在 Inspector 中配置对话节点，运行时由 SoDialogueProvider 转换为 DialogueGraph。
[CreateAssetMenu(fileName = "DialogueData", menuName = "Data/Dialogue/DialogueData")]
public class DialogueDataSO : ScriptableObject
{
    // 对话唯一标识，可用于日志、统计或后续存档索引。
    public string dialogueId = "dialogue_001";

    // 对话入口节点 ID，运行时会从该节点开始推进。
    public string startNodeId = "start";

    // 节点列表：每一项代表一句台词/一个分支节点。
    // 注意 nodeId 必须唯一，重复会在加载校验阶段报错。
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
}
