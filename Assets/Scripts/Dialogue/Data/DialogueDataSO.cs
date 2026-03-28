using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Data/Dialogue/DialogueData")]
// ScriptableObject asset for dialogue graph data.
public class DialogueDataSO : ScriptableObject
{
    // Dialogue identifier.
    public string dialogueId = "dialogue_001";

    // Start node id.
    public string startNodeId = "start";

    // Node list.
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
}
