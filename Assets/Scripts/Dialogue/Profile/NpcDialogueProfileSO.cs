using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 对话配置：维护规则列表与默认对话。
/// </summary>
[CreateAssetMenu(fileName = "NpcDialogueProfile", menuName = "Data/Dialogue/NpcDialogueProfile")]
public class NpcDialogueProfileSO : ScriptableObject
{
    // profileId 标识。
    public string profileId = "npc_profile";

    // 按优先级匹配的规则列表。
    public List<NpcDialogueRule> rules = new List<NpcDialogueRule>();

    // 未命中任何规则时使用的默认对话引用。
    public DialogueReference defaultDialogueReference = new DialogueReference();

    /// <summary>
    /// 获取可用的 Profile 标识。
    /// </summary>
    public string GetProfileId()
    {
        return string.IsNullOrWhiteSpace(profileId) ? name : profileId;
    }
}
