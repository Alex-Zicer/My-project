using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Data/Dialogue/DialogueData")]
public class DialogueDataSO : ScriptableObject
{
    // dialogueId 标识。
    public string dialogueId = "dialogue_001";

    // startNodeId 标识。
    public string startNodeId = "start";

    // 对话图中的节点列表。
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
}
